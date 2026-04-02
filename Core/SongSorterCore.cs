using System.Collections.Concurrent;
using System.Text;
using SongConverter.Models;
using SongConverter.Utils;

namespace SongConverter.Core;

public sealed record SongSortProgress(int ProcessedFolders, int TotalFolders);

public sealed record SongSortRunResult(
    int TotalCopied,
    int TotalSkipped,
    int TotalUnmatched,
    string ReportPath)
{
    public string Summary => $"整理完了: コピー {TotalCopied} / スキップ {TotalSkipped} / 未一致 {TotalUnmatched}";
}

public sealed record SongSortReportRow(
    string SourceCategory,
    string SongDirectory,
    string Status,
    string Reason,
    string CandidateTitle,
    string CandidateSubtitle,
    string MatchedCategory,
    string MatchedKey);

public static class SongSorterCore
{
    public static readonly string[] SourceCategories =
    {
        "00 ポップス",
        "01 キッズ",
        "02 アニメ",
        "03 ボーカロイド曲",
        "04 ゲームミュージック",
        "05 バラエティ",
        "06 クラシック",
        "07 ナムコオリジナル"
    };

    public static string OrganizeSongs(string tempSongsDir, string destRootDir, string runId, Action<string>? logAction = null)
    {
        return OrganizeSongsDetailed(tempSongsDir, destRootDir, runId, null, logAction).Summary;
    }

    public static SongSortRunResult OrganizeSongsDetailed(
        string tempSongsDir,
        string destRootDir,
        string runId,
        IReadOnlyCollection<string>? selectedSourceCategories,
        Action<string>? logAction = null,
        CancellationToken ct = default,
        Action<SongSortProgress>? progressAction = null)
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var exportDir = Path.Combine(exeDir, "Export");
        if (!Directory.Exists(exportDir))
            throw new InvalidOperationException("Export フォルダーが見つかりません。先に「譜面リスト更新」を実行してください。");

        if (!Directory.Exists(tempSongsDir))
            throw new DirectoryNotFoundException("TempSongs フォルダーが見つかりません: " + tempSongsDir);

        var songsRoot = ResolveSongsRoot(destRootDir);
        Directory.CreateDirectory(songsRoot);

        int totalCopied = 0;
        int totalSkipped = 0;
        int totalUnmatched = 0;
        var copyPathClaims = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        
        var mappings = new[]
        {
            new { Category = "00 ポップス",           Source = "01 Pop",               Dest = "00 ポップス",           Export = "pops.php",      BoxTitle = "ポップス",           BoxGenre = "ポップス",           BoxExplanation = "ポップスの曲をあつめた箱。" },
            new { Category = "01 キッズ",             Source = "04 Children and Folk", Dest = "01 キッズ",             Export = "kids.php",      BoxTitle = "キッズ",             BoxGenre = "キッズ",             BoxExplanation = "キッズの曲をあつめた箱。" },
            new { Category = "02 アニメ",             Source = "02 Anime",             Dest = "02 アニメ",             Export = "anime.php",     BoxTitle = "アニメ",             BoxGenre = "アニメ",             BoxExplanation = "アニメの曲をあつめた箱。" },
            new { Category = "03 ボーカロイド曲",     Source = "03 Vocaloid",          Dest = "03 ボーカロイド曲",     Export = "vocaloid.php",  BoxTitle = "ボーカロイド曲",     BoxGenre = "ボーカロイド曲",     BoxExplanation = "ボーカロイド曲をあつめた箱。" },
            new { Category = "04 ゲームミュージック", Source = "07 Game Music",        Dest = "04 ゲームミュージック", Export = "game.php",      BoxTitle = "ゲームミュージック", BoxGenre = "ゲームミュージック", BoxExplanation = "ゲームミュージックの曲をあつめた箱。" },
            new { Category = "05 バラエティ",         Source = "05 Variety",           Dest = "05 バラエティ",         Export = "variety.php",   BoxTitle = "バラエティ",         BoxGenre = "バラエティ",         BoxExplanation = "バラエティの曲をあつめた箱。" },
            new { Category = "06 クラシック",         Source = "06 Classical",         Dest = "06 クラシック",         Export = "classic.php",   BoxTitle = "クラシック",         BoxGenre = "クラシック",         BoxExplanation = "クラシックの曲をあつめた箱。" },
            new { Category = "07 ナムコオリジナル",   Source = "09 Namco Original",    Dest = "07 ナムコオリジナル",   Export = "namco.php",     BoxTitle = "ナムコオリジナル",   BoxGenre = "ナムコオリジナル",   BoxExplanation = "ナムコオリジナルの曲をあつめた箱。" },
        };

        var selectedSet = selectedSourceCategories == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(selectedSourceCategories.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);

        var activeSourceMappings = selectedSet.Count == 0
            ? mappings
            : mappings.Where(m => selectedSet.Contains(m.Category)).ToArray();

        var totalFolders = activeSourceMappings.Sum(m =>
        {
            var srcPath = Path.Combine(tempSongsDir, m.Source);
            return Directory.Exists(srcPath) ? Directory.GetDirectories(srcPath).Length : 0;
        });

        int processedFolders = 0;
        progressAction?.Invoke(new SongSortProgress(0, totalFolders));

        var exportGroups = LoadExportIndexes(exportDir);
        var reportRows = new ConcurrentBag<SongSortReportRow>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(2, Math.Min(Environment.ProcessorCount * 2, 16)),
            CancellationToken = ct
        };

        foreach (var sourceMap in activeSourceMappings)
        {
            ct.ThrowIfCancellationRequested();

            var srcCatDir = Path.Combine(tempSongsDir, sourceMap.Source);
            if (!Directory.Exists(srcCatDir)) continue;

            var songDirs = Directory.GetDirectories(srcCatDir);
            Parallel.ForEach(songDirs, parallelOptions, songDir =>
            {
                ct.ThrowIfCancellationRequested();

                var reportTitle = string.Empty;
                var reportSubtitle = string.Empty;
                var matchedCategory = string.Empty;
                var matchedKey = string.Empty;
                var titleMatched = false;
                var subtitleMatched = false;
                var indexResolved = false;
                var copied = false;
                var skipped = false;

                var tjaPaths = Directory.GetFiles(songDir, "*.tja", SearchOption.AllDirectories);
                if (tjaPaths.Length == 0)
                {
                    Interlocked.Increment(ref totalUnmatched);
                    reportRows.Add(new SongSortReportRow(sourceMap.Category, songDir, "Unmatched", "NoTjaFile", "", "", "", ""));
                    var p0 = Interlocked.Increment(ref processedFolders);
                    progressAction?.Invoke(new SongSortProgress(p0, totalFolders));
                    return;
                }

                var candidates = new List<(string Path, SongDetail Info, string TitleNorm, string SubtitleNorm, string FullTitleNorm)>();
                foreach (var path in tjaPaths)
                {
                    ct.ThrowIfCancellationRequested();

                    var info = ReadSongInfo(path);
                    if (info == null) continue;
                    candidates.Add((path, info, NormalizationUtils.NormalizeTitle(info.Title), NormalizationUtils.NormalizeSubtitle(info.Subtitle), NormalizationUtils.NormalizeTitle(info.FullTitle ?? info.Title)));
                }

                if (candidates.Count == 0)
                {
                    Interlocked.Increment(ref totalUnmatched);
                    reportRows.Add(new SongSortReportRow(sourceMap.Category, songDir, "Unmatched", "NoReadableTja", "", "", "", ""));
                    var p1 = Interlocked.Increment(ref processedFolders);
                    progressAction?.Invoke(new SongSortProgress(p1, totalFolders));
                    return;
                }

                reportTitle = candidates[0].Info.Title;
                reportSubtitle = candidates[0].Info.Subtitle;

                foreach (var target in mappings)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!exportGroups.TryGetValue(target.Export, out var songsByTitle)) continue;

                    foreach (var candidate in candidates)
                    {
                        ct.ThrowIfCancellationRequested();

                        List<(string SubtitleNorm, int Index)>? versions = null;
                        string foundTitleKey = string.Empty;
                        var lookupKeys = BuildTitleLookupKeys(candidate.TitleNorm, candidate.SubtitleNorm, candidate.FullTitleNorm);
                        foreach (var titleKey in lookupKeys)
                        {
                            if (!songsByTitle.TryGetValue(titleKey, out var found)) continue;
                            versions = found;
                            foundTitleKey = titleKey;
                            break;
                        }

                        if (versions == null) continue;
                        titleMatched = true;
                        matchedCategory = target.Category;
                        matchedKey = foundTitleKey;

                        var match = versions.FirstOrDefault(v => v.SubtitleNorm == candidate.SubtitleNorm);
                        if (match.Index == 0 && versions.Count == 1) match = versions[0];
                        if (match.Index == 0) continue;

                        subtitleMatched = true;
                        indexResolved = true;

                        var num = match.Index.ToString("000");
                        var dstGenreDir = Path.Combine(songsRoot, target.Dest);
                        var folderName = NormalizationUtils.SanitizeFolderName($"{num} {candidate.Info.FolderTitle}");
                        var dstSongDir = Path.Combine(dstGenreDir, folderName);

                        if (Directory.Exists(dstSongDir))
                        {
                            Interlocked.Increment(ref totalSkipped);
                            skipped = true;
                            continue;
                        }

                        if (!copyPathClaims.TryAdd(dstSongDir, 0))
                        {
                            Interlocked.Increment(ref totalSkipped);
                            skipped = true;
                            continue;
                        }

                        EnsureBoxDef(dstGenreDir, target.BoxTitle, target.BoxGenre, target.BoxExplanation);
                        CopyDirectory(songDir, dstSongDir);
                        Interlocked.Increment(ref totalCopied);
                        copied = true;
                        reportTitle = candidate.Info.Title;
                        reportSubtitle = candidate.Info.Subtitle;
                        break;
                    }

                    if (copied) break;
                }

                if (copied)
                {
                    reportRows.Add(new SongSortReportRow(sourceMap.Category, songDir, "Copied", "Matched", reportTitle, reportSubtitle, matchedCategory, matchedKey));
                }
                else if (skipped)
                {
                    reportRows.Add(new SongSortReportRow(sourceMap.Category, songDir, "Skipped", "DestinationAlreadyExists", reportTitle, reportSubtitle, matchedCategory, matchedKey));
                }
                else
                {
                    Interlocked.Increment(ref totalUnmatched);
                    var reason = !titleMatched
                        ? "TitleNotFoundInExport"
                        : !subtitleMatched
                            ? "SubtitleMismatch"
                            : !indexResolved
                                ? "IndexNotResolved"
                                : "Unknown";
                    reportRows.Add(new SongSortReportRow(sourceMap.Category, songDir, "Unmatched", reason, reportTitle, reportSubtitle, matchedCategory, matchedKey));
                }

                var p2 = Interlocked.Increment(ref processedFolders);
                progressAction?.Invoke(new SongSortProgress(p2, totalFolders));
            });
        }

        var reportPath = WriteReportCsv(exportDir, runId, reportRows);
        return new SongSortRunResult(totalCopied, totalSkipped, totalUnmatched, reportPath);
    }

    private static string ResolveSongsRoot(string selectedFolder)
    {
        try
        {
            var name = new DirectoryInfo(selectedFolder).Name;
            if (string.Equals(name, "Songs", StringComparison.OrdinalIgnoreCase))
                return selectedFolder;
        }
        catch { }
        return Path.Combine(selectedFolder, "Songs");
    }

    private static Dictionary<string, Dictionary<string, List<(string SubtitleNorm, int Index)>>> LoadExportIndexes(string exportDir)
    {
        var result = new Dictionary<string, Dictionary<string, List<(string SubtitleNorm, int Index)>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in SongListBase.Categories)
        {
            var filePath = Path.Combine(exportDir, $"songlist_{cat.DisplayName}.txt");
            if (!File.Exists(filePath)) continue;

            var songsByTitle = new Dictionary<string, List<(string SubtitleNorm, int Index)>>(StringComparer.Ordinal);
            foreach (var line in ReadAllLinesWithFallback(filePath))
            {
                var parts = line.Split('\t');
                if (parts.Length < 2) continue;
                var idStr = parts[0];
                var title = parts[1];
                var subtitle = parts.Length > 2 ? parts[2] : "";

                var titleNorm = NormalizationUtils.NormalizeTitle(title);
                var subNorm = NormalizationUtils.NormalizeSubtitle(subtitle);
                var idx = int.TryParse(idStr, out var n) ? n : 0;

                foreach (var key in NormalizationUtils.ExpandTitleMatchKeys(titleNorm))
                {
                    if (!songsByTitle.TryGetValue(key, out var v)) { v = new(); songsByTitle[key] = v; }
                    v.Add((subNorm, idx));
                }
            }
            result[cat.FileName] = songsByTitle;
        }
        return result;
    }

    private static string[] BuildTitleLookupKeys(string titleNorm, string subtitleNorm, string fullTitleNorm)
    {
        var keys = new List<string>();
        foreach (var key in NormalizationUtils.ExpandTitleMatchKeys(titleNorm)) keys.Add(key);
        if (!string.IsNullOrEmpty(fullTitleNorm) && fullTitleNorm != titleNorm)
            foreach (var key in NormalizationUtils.ExpandTitleMatchKeys(fullTitleNorm)) keys.Add(key);
        return keys.Distinct().ToArray();
    }

    private static string[] ReadAllLinesWithFallback(string path)
    {
        try
        {
            return File.ReadAllLines(path, Encoding.UTF8);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return File.ReadAllLines(path, Encoding.GetEncoding(932));
        }
    }

    private static string WriteReportCsv(string exportDir, string runId, IEnumerable<SongSortReportRow> rows)
    {
        var reportPath = Path.Combine(exportDir, $"songsort_report_{runId}.csv");
        var ordered = rows
            .OrderBy(r => r.SourceCategory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.SongDirectory, StringComparer.OrdinalIgnoreCase);

        using var writer = new StreamWriter(reportPath, false, new UTF8Encoding(false));
        writer.WriteLine("source_category,song_directory,status,reason,candidate_title,candidate_subtitle,matched_category,matched_key");
        foreach (var row in ordered)
        {
            writer.WriteLine(string.Join(",",
                EscapeCsv(row.SourceCategory),
                EscapeCsv(row.SongDirectory),
                EscapeCsv(row.Status),
                EscapeCsv(row.Reason),
                EscapeCsv(row.CandidateTitle),
                EscapeCsv(row.CandidateSubtitle),
                EscapeCsv(row.MatchedCategory),
                EscapeCsv(row.MatchedKey)));
        }

        return reportPath;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static SongDetail? ReadSongInfo(string tjaPath)
    {
        // Retry logic for robustness
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var lines = File.ReadAllLines(tjaPath, Encoding.UTF8);
                if (lines.Any(l => l.Contains('\uFFFD'))) 
                {
                    // Fallback to Shift-JIS
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    lines = File.ReadAllLines(tjaPath, Encoding.GetEncoding(932));
                }
                
                string? title = null, titleJa = null, sub = null, subJa = null;
                foreach (var l in lines)
                {
                    var trim = l.Trim();
                    if (trim.StartsWith("TITLEJA:", StringComparison.OrdinalIgnoreCase)) titleJa = trim["TITLEJA:".Length..].Trim();
                    else if (trim.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase)) title = trim["TITLE:".Length..].Trim();
                    else if (trim.StartsWith("SUBTITLEJA:", StringComparison.OrdinalIgnoreCase)) subJa = trim["SUBTITLEJA:".Length..].Trim();
                    else if (trim.StartsWith("SUBTITLE:", StringComparison.OrdinalIgnoreCase)) sub = trim["SUBTITLE:".Length..].Trim();
                }
                var resT = titleJa ?? title;
                if (resT == null) return null;
                return new SongDetail(resT, subJa ?? sub ?? "", resT, titleJa ?? resT);
            }
            catch (IOException) { Thread.Sleep(50); }
            catch { return null; }
        }
        return null;
    }

    private static void EnsureBoxDef(string dir, string title, string genre, string explanation)
    {
        var path = Path.Combine(dir, "box.def");
        if (File.Exists(path)) return;
        Directory.CreateDirectory(dir);
        File.WriteAllLines(path, new[] { "#TITLE:" + title, "#GENRE:" + genre, "#EXPLANATION:" + explanation, "#BGCOLOR:#ff0000", "#TEXTCOLOR:#ffffff" });
    }

    private static void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
        {
            var dFile = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, dFile, true);
        }
        foreach (var d in Directory.GetDirectories(src))
        {
            CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }
    }
}

