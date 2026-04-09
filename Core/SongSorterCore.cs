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
        "03 ボーカロイド™曲",
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

        var resolvedTempSongsDir = ResolveSongsRoot(tempSongsDir);
        if (!Directory.Exists(resolvedTempSongsDir))
            throw new DirectoryNotFoundException("TempSongs フォルダーが見つかりません: " + resolvedTempSongsDir);

        var songsRoot = ResolveSongsRoot(destRootDir);
        Directory.CreateDirectory(songsRoot);

        int totalCopied = 0;
        int totalSkipped = 0;
        int totalUnmatched = 0;
        var copyPathClaims = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        
        var mappings = new[]
        {
            new { Category = "00 ポップス",           Source = "01 Pop",               Dest = "00 ポップス",           Export = "pops.php",      BoxTitle = "ポップス",           BoxGenre = "ポップス",           BoxExplanation = "ポップスの曲をあつめたよ!" },
            new { Category = "01 キッズ",             Source = "04 Children and Folk", Dest = "01 キッズ",             Export = "kids.php",      BoxTitle = "キッズ",             BoxGenre = "キッズ",             BoxExplanation = "キッズの曲をあつめたよ!" },
            new { Category = "02 アニメ",             Source = "02 Anime",             Dest = "02 アニメ",             Export = "anime.php",     BoxTitle = "アニメ",             BoxGenre = "アニメ",             BoxExplanation = "アニメの曲をあつめたよ!" },
            new { Category = "03 ボーカロイド™曲",    Source = "03 Vocaloid",          Dest = "03 ボーカロイド™曲",    Export = "vocaloid.php",  BoxTitle = "ボーカロイド™曲",   BoxGenre = "ボーカロイド",        BoxExplanation = "ボーカロイド™の曲をあつめたよ!" },
            new { Category = "04 ゲームミュージック", Source = "07 Game Music",        Dest = "04 ゲームミュージック", Export = "game.php",      BoxTitle = "ゲームミュージック", BoxGenre = "ゲームミュージック", BoxExplanation = "ゲームミュージックの曲をあつめたよ!" },
            new { Category = "05 バラエティ",         Source = "05 Variety",           Dest = "05 バラエティ",         Export = "variety.php",   BoxTitle = "バラエティ",         BoxGenre = "バラエティ",         BoxExplanation = "バラエティの曲をあつめたよ!" },
            new { Category = "06 クラシック",         Source = "06 Classical",         Dest = "06 クラシック",         Export = "classic.php",   BoxTitle = "クラシック",         BoxGenre = "クラシック",         BoxExplanation = "クラシックの曲をあつめたよ!" },
            new { Category = "07 ナムコオリジナル",   Source = "09 Namco Original",    Dest = "07 ナムコオリジナル",   Export = "namco.php",     BoxTitle = "ナムコオリジナル",   BoxGenre = "ナムコオリジナル",   BoxExplanation = "ナムコオリジナルの曲をあつめたよ!" },
        };

        var selectedSet = selectedSourceCategories == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(selectedSourceCategories.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);

        var activeSourceMappings = selectedSet.Count == 0
            ? mappings
            : mappings.Where(m => selectedSet.Contains(m.Category)).ToArray();

        var totalFolders = activeSourceMappings.Sum(m =>
        {
            var srcPath = Path.Combine(resolvedTempSongsDir, m.Source);
            return Directory.Exists(srcPath) ? Directory.GetFiles(srcPath, "*.tja", SearchOption.AllDirectories).Select(f => Path.GetDirectoryName(f)!).Distinct().Count() : 0;
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

            var srcCatDir = Path.Combine(resolvedTempSongsDir, sourceMap.Source);
            if (!Directory.Exists(srcCatDir)) continue;

            var tjaFiles = Directory.GetFiles(srcCatDir, "*.tja", SearchOption.AllDirectories);
            var songDirs = tjaFiles.Select(f => Path.GetDirectoryName(f)!).Distinct().ToArray();
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

                var preferredMappings = mappings
                    .OrderBy(m => m.Source == sourceMap.Source ? 0 : 1)
                    .ToArray();

                foreach (var target in preferredMappings)
                {
                    ct.ThrowIfCancellationRequested();

                    bool matchedInThisCategory = false;
                    if (exportGroups.TryGetValue(target.Export, out var songsByTitle))
                    {
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

                            if (versions == null)
                            {
                                var looseKey = FindLooseTitleMatchKey(songsByTitle, lookupKeys);
                                if (looseKey != null && songsByTitle.TryGetValue(looseKey, out var looseFound))
                                {
                                    versions = looseFound;
                                    foundTitleKey = looseKey;
                                }
                            }

                            if (versions == null) continue;
                            titleMatched = true;
                            matchedCategory = target.Category;
                            matchedKey = foundTitleKey;

                            var match = versions.FirstOrDefault(v => v.SubtitleNorm == candidate.SubtitleNorm);
                            if (match.Index == 0 && !string.IsNullOrEmpty(candidate.SubtitleNorm))
                            {
                                match = versions.FirstOrDefault(v =>
                                    v.SubtitleNorm.Contains(candidate.SubtitleNorm, StringComparison.Ordinal) ||
                                    candidate.SubtitleNorm.Contains(v.SubtitleNorm, StringComparison.Ordinal));
                            }
                            if (match.Index == 0 && versions.Count == 1) match = versions[0];
                            if (match.Index == 0) continue;

                            subtitleMatched = true;
                            indexResolved = true;
                            matchedInThisCategory = true;

                            var num = match.Index.ToString("000");
                            var titleForFolder = string.IsNullOrWhiteSpace(candidate.Info.FolderTitle)
                                ? Path.GetFileNameWithoutExtension(candidate.Path)
                                : candidate.Info.FolderTitle;
                            var dstGenreDir = Path.Combine(songsRoot, target.Dest);
                            var folderNameCandidates = BuildFolderNameCandidates(num, titleForFolder, candidate.Info.Subtitle, candidate.Path);

                            // 既存フォルダの確認（再実行時のべき等性のため）
                            if (TryFindExistingSongFolder(dstGenreDir, folderNameCandidates[0], out _))
                            {
                                Interlocked.Increment(ref totalSkipped);
                                skipped = true;
                                continue;
                            }

                            // 未使用のパスを確定する
                            string? dstSongDir = null;
                            foreach (var folderName in folderNameCandidates)
                            {
                                var candidate2 = Path.Combine(dstGenreDir, folderName);
                                if (!copyPathClaims.TryAdd(candidate2, 0)) continue;
                                if (Directory.Exists(candidate2)) continue;
                                dstSongDir = candidate2;
                                break;
                            }

                            if (dstSongDir == null)
                            {
                                Interlocked.Increment(ref totalSkipped);
                                skipped = true;
                                continue;
                            }

                            EnsureBoxDef(dstGenreDir, target.BoxTitle, target.BoxGenre, target.BoxExplanation);
                            CopyDirectory(songDir, dstSongDir, candidate.Path);
                            Interlocked.Increment(ref totalCopied);
                            copied = true;
                            reportTitle = candidate.Info.Title;
                            reportSubtitle = candidate.Info.Subtitle;
                        }
                    }

                    // カスタムマッピング（公式リストにないが特定ジャンルに入れたい曲）の処理
                    if (!matchedInThisCategory && target.Category == "07 ナムコオリジナル")
                    {
                        foreach (var candidate in candidates)
                        {
                            if (candidate.TitleNorm.Contains("クラシックメドレー"))
                            {
                                // クラシックカテゴリでのIndexを流用するか、000とする
                                // ここでは簡易的に000、またはクラシック側のIndexを探す処理を入れる
                                // 今回はユーザーの利便性を優先し、クラシック側からIndexを引っ張る
                                if (exportGroups.TryGetValue("classic.php", out var classicSongs))
                                {
                                    var lookupKeys = BuildTitleLookupKeys(candidate.TitleNorm, candidate.SubtitleNorm, candidate.FullTitleNorm);
                                    foreach (var key in lookupKeys)
                                    {
                                        if (classicSongs.TryGetValue(key, out var versions))
                                        {
                                            var m = versions[0];
                                            var num = m.Index.ToString("000");
                                            var dstGenreDir = Path.Combine(songsRoot, target.Dest);
                                            var folderNameCandidates = BuildFolderNameCandidates(num, candidate.Info.FolderTitle ?? candidate.Info.Title, candidate.Info.Subtitle, candidate.Path);
                                            
                                            if (TryFindExistingSongFolder(dstGenreDir, folderNameCandidates[0], out _)) { skipped = true; continue; }
                                            
                                            string? dstSongDir = null;
                                            foreach (var folderName in folderNameCandidates)
                                            {
                                                var c2 = Path.Combine(dstGenreDir, folderName);
                                                if (!copyPathClaims.TryAdd(c2, 0)) continue;
                                                if (Directory.Exists(c2)) continue;
                                                dstSongDir = c2;
                                                break;
                                            }
                                            if (dstSongDir == null) { skipped = true; continue; }

                                            EnsureBoxDef(dstGenreDir, target.BoxTitle, target.BoxGenre, target.BoxExplanation);
                                            CopyDirectory(songDir, dstSongDir, candidate.Path);
                                            Interlocked.Increment(ref totalCopied);
                                            copied = true;
                                            matchedCategory = target.Category;
                                            matchedKey = key;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
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

        if (!string.IsNullOrEmpty(subtitleNorm))
        {
            var combined = $"{titleNorm}{subtitleNorm}";
            foreach (var key in NormalizationUtils.ExpandTitleMatchKeys(combined)) keys.Add(key);

            if (!string.IsNullOrEmpty(fullTitleNorm) && fullTitleNorm != titleNorm)
            {
                var fullCombined = $"{fullTitleNorm}{subtitleNorm}";
                foreach (var key in NormalizationUtils.ExpandTitleMatchKeys(fullCombined)) keys.Add(key);
            }
        }

        return keys.Distinct().ToArray();
    }

    private static string? FindLooseTitleMatchKey(
        Dictionary<string, List<(string SubtitleNorm, int Index)>> songsByTitle,
        IEnumerable<string> lookupKeys)
    {
        var hits = new HashSet<string>(StringComparer.Ordinal);
        var keys = lookupKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal).ToArray();

        foreach (var lookup in keys)
        {
            foreach (var songKey in songsByTitle.Keys)
            {
                if (IsLooseTitleMatch(lookup, songKey)) hits.Add(songKey);
            }
        }

        return hits.Count == 1 ? hits.First() : null;
    }

    private static bool IsLooseTitleMatch(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        if (string.Equals(a, b, StringComparison.Ordinal)) return true;
        
        var minLen = Math.Min(a.Length, b.Length);
        if (minLen < 4) return false;

        // 短いタイトルほど厳密に: 6文字以下は10%、7文字以上は30%の許容
        var maxRatio = minLen <= 6 ? 0.10 : 0.30;

        if (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal))
        {
            // 短い方の長さに対して、長さの差が許容範囲内の場合のみマッチングを許可
            // これにより「カゲロウ」が「カゲロウデイズ」に誤マッチングするのを防ぐ
            var longerLen = Math.Max(a.Length, b.Length);
            var diff = longerLen - minLen;
            var diffRatio = (double)diff / minLen;
            return diffRatio <= maxRatio;
        }

        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))
        {
            // 短い方の長さに対して、長さの差が許容範囲内の場合のみマッチングを許可
            // これにより「カラフル」が「カラフルボイス」に誤マッチングするのを防ぐ
            var longerLen = Math.Max(a.Length, b.Length);
            var diff = longerLen - minLen;
            var diffRatio = (double)diff / minLen;
            return minLen >= 6 && diffRatio <= maxRatio;
        }

        return false;
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

                if (subJa != null && subJa.Contains("旧譜面")) return null;

                var resT = titleJa ?? title;
                if (resT == null) return null;
                var resSub = subJa ?? sub ?? "";

                if (TryExtractInlineSubtitleFromTitle(resT, out var mainTitle, out var inlineSubtitle))
                {
                    resT = mainTitle;
                    if (string.IsNullOrWhiteSpace(resSub) || resSub.StartsWith("--", StringComparison.Ordinal) || resSub.StartsWith("++", StringComparison.Ordinal))
                    {
                        resSub = inlineSubtitle;
                    }
                }

                return new SongDetail(resT, resSub, titleJa ?? title ?? "", titleJa ?? resT);
            }
            catch (IOException) { Thread.Sleep(50); }
            catch { return null; }
        }
        return null;
    }

    private static bool TryExtractInlineSubtitleFromTitle(string title, out string mainTitle, out string inlineSubtitle)
    {
        mainTitle = title.Trim();
        inlineSubtitle = string.Empty;

        if (string.IsNullOrWhiteSpace(title)) return false;

        var trimmed = title.Trim();
        if (trimmed.Length < 3) return false;

        var end = trimmed.Length - 1;
        if (!IsWaveDash(trimmed[end])) return false;

        var start = -1;
        for (int i = end - 1; i >= 0; i--)
        {
            if (IsWaveDash(trimmed[i]))
            {
                start = i;
                break;
            }
        }
        if (start <= 0 || start >= end - 1) return false;

        if (!char.IsWhiteSpace(trimmed[start - 1])) return false;

        var titlePart = trimmed[..start].TrimEnd();
        var subtitlePart = trimmed[(start + 1)..end].Trim();
        if (string.IsNullOrWhiteSpace(titlePart) || string.IsNullOrWhiteSpace(subtitlePart)) return false;

        mainTitle = titlePart;
        inlineSubtitle = subtitlePart;
        return true;
    }

    private static bool IsWaveDash(char c) => c == '～' || c == '〜' || c == '~';

    private static bool IsAlreadyCopied(string srcDir, string destDir)
    {
        try
        {
            if (!Directory.Exists(destDir)) return false;

            var srcFiles = Directory.GetFiles(srcDir, "*.tja", SearchOption.AllDirectories);
            if (srcFiles.Length == 0) return false;

            foreach (var srcFile in srcFiles)
            {
                var relPath = Path.GetRelativePath(srcDir, srcFile);
                var destFile = Path.Combine(destDir, relPath);
                
                if (!File.Exists(destFile)) return false;

                var srcInfo = new FileInfo(srcFile);
                var destInfo = new FileInfo(destFile);
                if (srcInfo.Length != destInfo.Length) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static readonly object BoxDefLock = new();

    private static void EnsureBoxDef(string dir, string title, string genre, string explanation)
    {
        var path = Path.Combine(dir, "box.def");
        lock (BoxDefLock)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllLines(path, new[] { "#TITLE:" + title, "#GENRE:" + genre, "#EXPLANATION:" + explanation, "#BGCOLOR:#ff0000", "#TEXTCOLOR:#ffffff" });
        }
    }

    private static void CopyDirectory(string src, string dest, string? targetTjaPath = null)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
        {
            var name = Path.GetFileName(file);
            // TJAファイルが複数ある場合は、マッチしたもの以外はコピーしない
            if (targetTjaPath != null && name.EndsWith(".tja", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(file, targetTjaPath, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            var dFile = Path.Combine(dest, name);
            File.Copy(file, dFile, overwrite: false);
        }
        foreach (var d in Directory.GetDirectories(src))
        {
            CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)), targetTjaPath);
        }
    }

    private static string[] BuildFolderNameCandidates(string num, string titleForFolder, string subtitle, string tjaPath)
    {
        var baseTitle = SanitizeFolderName(titleForFolder);
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string rawName)
        {
            var cleaned = SanitizeFolderName(rawName);
            if (seen.Add(cleaned)) candidates.Add(cleaned);
        }

        Add($"{num} {baseTitle}");

        var subtitlePart = NormalizeSubtitleForFolderName(subtitle);
        if (!string.IsNullOrWhiteSpace(subtitlePart))
            Add($"{num} {baseTitle} {subtitlePart}");

        var tjaName = Path.GetFileNameWithoutExtension(tjaPath);
        if (!string.IsNullOrWhiteSpace(tjaName))
            Add($"{num} {baseTitle} [{tjaName}]");

        for (var i = 2; i <= 9; i++)
            Add($"{num} {baseTitle} ({i})");

        return candidates.ToArray();
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(name);
        foreach (var c in invalid) sb.Replace(c.ToString(), "");
        sb.Replace("*", "").Replace("?", "").Replace(":", "").Replace("|", "");
        var result = sb.ToString().Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(result) ? "NoName" : result;
    }

    private static string NormalizeSubtitleForFolderName(string subtitle)
    {
        if (string.IsNullOrWhiteSpace(subtitle)) return string.Empty;
        var work = subtitle.Trim();
        work = work.TrimStart('-', '+', '\uFF0D', '\uFF0B', '\u2014', '\u2013').Trim();
        return SanitizeFolderName(work);
    }

    private static bool TryFindExistingSongFolder(string genreDir, string primaryFolderName, out string existingDirPath)
    {
        existingDirPath = Path.Combine(genreDir, primaryFolderName);
        if (!Directory.Exists(genreDir)) return false;

        try
        {
            foreach (var dir in Directory.GetDirectories(genreDir))
            {
                var name = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (IsSameSongFolderName(primaryFolderName, name))
                {
                    existingDirPath = dir;
                    return true;
                }
            }
        }
        catch { /* ディレクトリ列挙に失敗した場合は通常フローへ */ }

        return false;
    }

    private static bool IsSameSongFolderName(string primaryFolderName, string existingFolderName)
    {
        if (string.Equals(primaryFolderName, existingFolderName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!existingFolderName.StartsWith(primaryFolderName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (existingFolderName.Length <= primaryFolderName.Length)
            return false;

        var next = existingFolderName[primaryFolderName.Length];
        return char.IsWhiteSpace(next) || next == '[' || next == '(' || next == '\uFF3B' || next == '\uFF08';
    }
}

