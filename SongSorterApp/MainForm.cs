using System.Text;
using System.Text.Json;

namespace SongSorterApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        SetStatus("準備完了", showProgress: false);
    }

    async Task<(int fileCount, int totalTitles)> ExportSongListsAsync()
    {
        var exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
        var exportDir = Path.Combine(exeDir, "Export");
        Directory.CreateDirectory(exportDir);

        int fileCount = 0;
        int totalTitles = 0;

        var totalCats = SongListFetcher.Categories.Length;
        SetStatus("曲リスト取得中…", showProgress: true, progressStyle: ProgressBarStyle.Blocks, progressMax: totalCats, progressValue: 0);

        var tasks = SongListFetcher.Categories.Select(async cat =>
        {
            var songs = await SongListFetcher.FetchSongsAsync(cat.FileName);
            if (songs.Count == 0) return (0, 0);

            var fileName = $"songlist_{cat.DisplayName}.txt";
            var filePath = Path.Combine(exportDir, fileName);
            var outputLines = songs.Select((s, i) =>
                $"{i + 1:000}\t{SanitizeTsvCell(s.Title)}\t{SanitizeTsvCell(s.Subtitle)}");
            await File.WriteAllLinesAsync(filePath, outputLines, Encoding.UTF8);

            return (1, songs.Count);
        });

        var results = await Task.WhenAll(tasks);
        foreach (var (f, t) in results)
        {
            fileCount += f;
            totalTitles += t;
        }

        SetStatus($"曲リスト取得完了（{fileCount} 件 / {totalTitles} 曲）", showProgress: false);
        return (fileCount, totalTitles);
    }

    async void btnOrganize_Click(object? sender, EventArgs e)
    {
        var recent = LoadRecentPaths();
        using var dlgTemp = new FolderBrowserDialog
        {
            Description = "コピー元のフォルダを選択",
            SelectedPath = GetExistingPathOrEmpty(recent.TempSongs)
        };
        if (dlgTemp.ShowDialog() != DialogResult.OK)
            return;

        using var dlgRoot = new FolderBrowserDialog
        {
            Description = "コピー後のフォルダを選択",
            SelectedPath = GetExistingPathOrEmpty(recent.TaikoRoot)
        };
        if (dlgRoot.ShowDialog() != DialogResult.OK)
            return;

        var tempSongsDir = dlgTemp.SelectedPath;
        var destRootDir = dlgRoot.SelectedPath;
        SaveRecentPaths(new RecentPaths
        {
            TempSongs = tempSongsDir,
            TaikoRoot = destRootDir
        });

        btnOrganize.Enabled = false;
        try
        {
            await ExportSongListsAsync();

            SetStatus("Songs フォルダへコピー中…", showProgress: true, progressStyle: ProgressBarStyle.Marquee);
            var summary = await Task.Run(() => OrganizeSongs(tempSongsDir, destRootDir));
            SetStatus(summary, showProgress: false);
        }
        catch (Exception ex)
        {
            SetStatus("エラー: " + ex.Message, showProgress: false);
            MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnOrganize.Enabled = true;
        }
    }

    static string OrganizeSongs(string tempSongsDir, string destRootDir)
    {
        var exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
        var exportDir = Path.Combine(exeDir, "Export");
        if (!Directory.Exists(exportDir))
            throw new InvalidOperationException("Export フォルダが見つかりません。先に曲名リストを出力してください。");

        if (!Directory.Exists(tempSongsDir))
            throw new DirectoryNotFoundException("TempSongs フォルダが見つかりません: " + tempSongsDir);

        var songsRoot = ResolveSongsRoot(destRootDir);
        Directory.CreateDirectory(songsRoot);

        int totalCopied = 0;
        int totalSkipped = 0;
        int totalUnmatched = 0;
        var unmatchedLogs = new List<string>();

        var mappings = new[]
        {
            new { Source = "01 Pop",               Dest = "00 ポップス",           Export = "ポップス",           BoxTitle = "ポップス",           BoxGenre = "ポップス",           BoxExplanation = "ポップスの曲をあつめたよ!" },
            new { Source = "04 Children and Folk", Dest = "01 キッズ",             Export = "キッズ",             BoxTitle = "キッズ",             BoxGenre = "キッズ",             BoxExplanation = "キッズの曲をあつめたよ!" },
            new { Source = "02 Anime",             Dest = "02 アニメ",             Export = "アニメ",             BoxTitle = "アニメ",             BoxGenre = "アニメ",             BoxExplanation = "アニメの曲をあつめたよ!" },
            new { Source = "03 Vocaloid",          Dest = "03 ボーカロイド曲",     Export = "ボーカロイド曲",     BoxTitle = "ボーカロイド™曲", BoxGenre = "ボーカロイド",     BoxExplanation = "ボーカロイド™の曲をあつめたよ!" },
            new { Source = "07 Game Music",        Dest = "04 ゲームミュージック", Export = "ゲームミュージック", BoxTitle = "ゲームミュージック", BoxGenre = "ゲームミュージック", BoxExplanation = "ゲームミュージックの曲をあつめたよ!" },
            new { Source = "05 Variety",           Dest = "05 バラエティ",         Export = "バラエティ",         BoxTitle = "バラエティ",         BoxGenre = "バラエティ",         BoxExplanation = "バラエティの曲をあつめたよ!" },
            new { Source = "09 Namco Original",    Dest = "07 ナムコオリジナル",   Export = "ナムコオリジナル",   BoxTitle = "ナムコオリジナル",   BoxGenre = "ナムコオリジナル",   BoxExplanation = "ナムコオリジナルの曲をあつめたよ!" },
            new { Source = "06 Classical",         Dest = "06 クラシック",         Export = "クラシック",         BoxTitle = "クラシック",         BoxGenre = "クラシック",         BoxExplanation = "クラシックの曲をあつめたよ!" },
        };

        var exportGroups = LoadExportIndexes(exportDir);

        foreach (var sourceMap in mappings)
        {
            var srcCatDir = Path.Combine(tempSongsDir, sourceMap.Source);
            if (!Directory.Exists(srcCatDir))
                continue;

            var srcCategoryName = sourceMap.Source;
            // 同名曲が複数カテゴリに存在する場合は、元フォルダと同じカテゴリを優先する
            var preferredMappings = mappings
                .OrderBy(m => string.Equals(m.Source, srcCategoryName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ToArray();

            foreach (var songDir in Directory.GetDirectories(srcCatDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var tjaPaths = Directory
                    .GetFiles(songDir, "*.tja", SearchOption.AllDirectories)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (tjaPaths.Length == 0)
                {
                    totalUnmatched++;
                    unmatchedLogs.Add($"[NO_TJA] {songDir}");
                    continue;
                }

                var tjaCandidates = new List<(string Path, SongDetail Info, string TitleNorm, string SubtitleNorm)>();
                foreach (var path in tjaPaths)
                {
                    var info = ReadSongInfo(path);
                    if (info == null)
                        continue;

                    tjaCandidates.Add((
                        path,
                        info,
                        NormalizationUtils.NormalizeTitle(info.Title),
                        NormalizationUtils.NormalizeSubtitle(info.Subtitle)
                    ));
                }

                if (tjaCandidates.Count == 0)
                {
                    totalUnmatched++;
                    unmatchedLogs.Add($"[NO_VALID_TJA] {songDir}");
                    continue;
                }

                var matchedAnyInFolder = false;
                foreach (var target in preferredMappings)
                {
                    if (!exportGroups.TryGetValue(target.Export, out var songsByTitle))
                        continue;

                    foreach (var candidate in tjaCandidates)
                    {
                        if (!songsByTitle.TryGetValue(candidate.TitleNorm, out var versions))
                            continue;

                        // マッチングロジック (完全一致 -> 部分一致 -> タイトルのみ一致)
                        var match = versions.FirstOrDefault(v => v.SubtitleNorm == candidate.SubtitleNorm);
                        if (match.Index == 0 && !string.IsNullOrEmpty(candidate.SubtitleNorm))
                        {
                            match = versions.FirstOrDefault(v => 
                                v.SubtitleNorm.Contains(candidate.SubtitleNorm) || candidate.SubtitleNorm.Contains(v.SubtitleNorm));
                        }
                        if (match.Index == 0 && versions.Count == 1)
                        {
                            match = versions[0];
                        }

                        if (match.Index == 0) continue;
                        matchedAnyInFolder = true;

                        // フォルダ名生成
                        var num = match.Index.ToString("000");
                        var tjaBaseName = Path.GetFileNameWithoutExtension(candidate.Path);
                        var safeFolderName = SanitizeFolderName(tjaBaseName);
                        var newFolderName = $"{num} {safeFolderName}";
                        var dstSongDir = Path.Combine(songsRoot, target.Dest, newFolderName);

                        // ディレクトリの有無チェックと作成
                        var dstGenreDir = Path.Combine(songsRoot, target.Dest);
                        if (Directory.Exists(dstSongDir)) {
                            totalSkipped++;
                            break;
                        }

                        EnsureBoxDef(dstGenreDir, target.BoxTitle, target.BoxGenre, target.BoxExplanation);

                        CopyDirectory(songDir, dstSongDir, candidate.Path);
                        totalCopied++;
                        break; // このカテゴリへのコピーは完了
                    }
                }

                if (!matchedAnyInFolder)
                {
                    totalUnmatched++;
                    
                    // 最も近い候補を探してログに残す（デバッグ用）
                    var first = tjaCandidates[0];
                    var titleNorm = first.TitleNorm;
                    var candidates = exportGroups.Values
                        .SelectMany(g => g.TryGetValue(titleNorm, out var v) ? v : Enumerable.Empty<(string Sub, int Idx)>())
                        .Select(v => v.Sub)
                        .ToList();

                    if (candidates.Count > 0)
                    {
                        var tjaSub = first.SubtitleNorm;
                        unmatchedLogs.Add($"[SUBTITLE_MISMATCH] {songDir} (タイトル一致: [{titleNorm}] \n Web側候補: {string.Join(", ", candidates.Select(c => $"[{c}] (Hex: {ToHex(c)})"))} \n TJA側: [{tjaSub}] (Hex: {ToHex(tjaSub)}))");
                    }
                    else
                    {
                        unmatchedLogs.Add($"[TITLE_NOT_FOUND] {songDir} (タイトル [{titleNorm}] (Hex: {ToHex(titleNorm)}) 自体が見つかりません)");
                    }
                }
            }
        }

        if (unmatchedLogs.Count > 0)
        {
            var logPath = Path.Combine(exportDir, "log.txt");
            File.WriteAllLines(logPath, unmatchedLogs, Encoding.UTF8);
        }

        return $"コピー完了: {totalCopied} 曲 / 既設: {totalSkipped} 曲 (未マッチ {totalUnmatched} 件 / log.txt を確認してください)";
    }

    static string ToHex(string s) => string.Join("", s.Select(c => $"{(int)c:X4}"));

    static Dictionary<string, Dictionary<string, List<(string SubtitleNorm, int Index)>>> LoadExportIndexes(string exportDir)
    {
        var result = new Dictionary<string, Dictionary<string, List<(string SubtitleNorm, int Index)>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in SongListFetcher.Categories)
        {
            var fileName = $"songlist_{cat.DisplayName}.txt";
            var filePath = Path.Combine(exportDir, fileName);
            if (!File.Exists(filePath))
                continue;

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            var songsByTitle = new Dictionary<string, List<(string SubtitleNorm, int Index)>>(StringComparer.Ordinal);
            for (int i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split('\t');
                if (parts.Length < 2) continue; // 不正な行
                
                // 形式: "連番\t曲名\tサブタイトル"
                var idStr = parts[0];
                var title = parts[1];
                var subtitle = parts.Length > 2 ? parts[2] : string.Empty;

                var titleNorm = NormalizationUtils.NormalizeTitle(title);
                var subtitleNorm = NormalizationUtils.NormalizeSubtitle(subtitle);
                var officialIdx = int.TryParse(idStr, out var n) ? n : i + 1;

                if (!songsByTitle.TryGetValue(titleNorm, out var versions))
                {
                    versions = new List<(string SubtitleNorm, int Index)>();
                    songsByTitle[titleNorm] = versions;
                }
                versions.Add((subtitleNorm, officialIdx));
            }

            result[cat.DisplayName] = songsByTitle;
        }

        return result;
    }

    public record SongDetail(string Title, string Subtitle);

    static SongDetail? ReadSongInfo(string tjaPath)
    {
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return ReadSongInfoCore(tjaPath);
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(20 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(20 * attempt);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    static SongDetail? ReadSongInfoCore(string tjaPath)
    {
        // まずは UTF-8 (BOMあり/なし両対応) で読み込んでみる
        var lines = File.ReadAllLines(tjaPath, Encoding.UTF8);
        
        // UTF-8 で読み込んだ際に「」(U+FFFD) が含まれている場合、または
        // 日本語が含まれているはずなのに UTF-8 として不正なバイトシーケンスだった場合、
        // Shift-JIS (CP932) として再読み込みを試行する
        if (lines.Any(l => l.Contains('\uFFFD')))
        {
            lines = File.ReadAllLines(tjaPath, Encoding.GetEncoding(932));
        }
        
        string? title = null, titleJa = null;
        string? subtitle = null, subtitleJa = null;
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("TITLEJA:", StringComparison.OrdinalIgnoreCase)) titleJa = line["TITLEJA:".Length..].Trim();
            else if (line.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase)) title = line["TITLE:".Length..].Trim();
            else if (line.StartsWith("SUBTITLEJA:", StringComparison.OrdinalIgnoreCase)) subtitleJa = line["SUBTITLEJA:".Length..].Trim();
            else if (line.StartsWith("SUBTITLE:", StringComparison.OrdinalIgnoreCase)) subtitle = line["SUBTITLE:".Length..].Trim();
        }
        
        var resTitle = titleJa ?? title;
        if (resTitle == null) return null;

        var resSubtitle = subtitleJa ?? subtitle ?? string.Empty;

        // TITLEに「曲名 ～サブタイトル～」形式で入っているケースを補正
        // 例: 「白鳥の湖 ～still a duckling～」
        if (TryExtractInlineSubtitleFromTitle(resTitle, out var mainTitle, out var inlineSubtitle))
        {
            resTitle = mainTitle;

            // TJAのSUBTITLEが "--" / "++" で始まる場合は作曲者クレジットであることが多いので、
            // 曲照合にはTITLE由来のサブタイトルを優先する
            if (string.IsNullOrWhiteSpace(resSubtitle)
                || resSubtitle.StartsWith("--", StringComparison.Ordinal)
                || resSubtitle.StartsWith("++", StringComparison.Ordinal))
            {
                resSubtitle = inlineSubtitle;
            }
        }

        return new SongDetail(resTitle, resSubtitle);
    }

    static bool TryExtractInlineSubtitleFromTitle(string title, out string mainTitle, out string inlineSubtitle)
    {
        mainTitle = title.Trim();
        inlineSubtitle = string.Empty;

        if (string.IsNullOrWhiteSpace(title))
            return false;

        var trimmed = title.Trim();
        if (trimmed.Length < 3)
            return false;

        // 末尾が波ダッシュ系（～/〜/~）で閉じられている場合のみ対象
        var end = trimmed.Length - 1;
        if (!IsWaveDash(trimmed[end]))
            return false;

        // 終端の1つ手前までで、最後の波ダッシュを開始位置として探す
        var start = -1;
        for (int i = end - 1; i >= 0; i--)
        {
            if (IsWaveDash(trimmed[i]))
            {
                start = i;
                break;
            }
        }
        if (start <= 0 || start >= end - 1)
            return false;

        // 「曲名 ～サブタイトル～」のように開始側の前に区切り空白がある場合のみ分離する
        if (!char.IsWhiteSpace(trimmed[start - 1]))
            return false;

        var titlePart = trimmed[..start].TrimEnd();
        var subtitlePart = trimmed[(start + 1)..end].Trim();
        if (string.IsNullOrWhiteSpace(titlePart) || string.IsNullOrWhiteSpace(subtitlePart))
            return false;

        mainTitle = titlePart;
        inlineSubtitle = subtitlePart;
        return true;
    }

    static bool IsWaveDash(char c) => c == '～' || c == '〜' || c == '~';



    static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name);
        foreach (var c in invalid)
        {
            sb.Replace(c.ToString(), "");
        }

        // '*' など特定の記号が残る可能性があるため、明示的に除去（環境依存の回避）
        sb.Replace("*", "");
        sb.Replace("?", "");
        sb.Replace(":", "");
        sb.Replace("|", "");

        var result = sb.ToString().Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(result) ? "NoName" : result;
    }

    private static readonly object _boxLock = new();
    static void EnsureBoxDef(string dir, string title, string genre, string explanation)
    {
        lock (_boxLock)
        {
            var destBox = Path.Combine(dir, "box.def");

            var removePrefixes = new[] { "#SELECTBG:", "#BOXCOLOR:", "#FONTCOLOR:" };
            if (File.Exists(destBox))
            {
                var currentLines = File.ReadAllLines(destBox, Encoding.UTF8);
                var filtered = currentLines
                    .Where(line => !removePrefixes.Any(prefix =>
                        line.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                if (filtered.Length != currentLines.Length)
                    File.WriteAllLines(destBox, filtered, Encoding.UTF8);
                return;
            }

            Directory.CreateDirectory(dir);
            var lines = new[]
            {
                $"#TITLE:{title}",
                $"#GENRE:{genre}",
                $"#EXPLANATION:{explanation}",
            };
            File.WriteAllLines(destBox, lines, Encoding.UTF8);
        }
    }

    static void CopyDirectory(string sourceDir, string destDir, string? targetTjaPath = null)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            
            // TJAファイルの場合、ターゲットではないTJAはコピーしない
            if (targetTjaPath != null && name.EndsWith(".tja", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(file, targetTjaPath, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var dest = Path.Combine(destDir, name);
            File.Copy(file, dest, overwrite: false);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(dir);
            var destSub = Path.Combine(destDir, name);
            CopyDirectory(dir, destSub, targetTjaPath);
        }
    }

    sealed class RecentPaths
    {
        public string? TempSongs { get; set; }
        public string? TaikoRoot { get; set; }
    }

    static string GetRecentPathsFile()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SongSorterApp");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "recent_paths.json");
    }

    static RecentPaths LoadRecentPaths()
    {
        var path = GetRecentPathsFile();
        if (!File.Exists(path))
            return new RecentPaths();
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<RecentPaths>(json) ?? new RecentPaths();
        }
        catch
        {
            return new RecentPaths();
        }
    }

    static void SaveRecentPaths(RecentPaths paths)
    {
        var path = GetRecentPathsFile();
        var json = JsonSerializer.Serialize(paths, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    static string GetExistingPathOrEmpty(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : string.Empty;
    }

    static string SanitizeTsvCell(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        return string.Join(" ", normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    static string ResolveSongsRoot(string selectedFolder)
    {
        try
        {
            var name = new DirectoryInfo(selectedFolder).Name;
            if (string.Equals(name, "Songs", StringComparison.OrdinalIgnoreCase))
                return selectedFolder;
        }
        catch
        {
            // fallback to combine below
        }

        return Path.Combine(selectedFolder, "Songs");
    }

    void SetStatus(string text, bool showProgress, ProgressBarStyle progressStyle = ProgressBarStyle.Marquee, int? progressMax = null, int? progressValue = null)
    {
        statusLabel.Text = text;
        statusProgress.Visible = showProgress;
        statusProgress.Style = progressStyle;
        if (progressMax.HasValue)
            statusProgress.Maximum = Math.Max(1, progressMax.Value);
        if (progressValue.HasValue)
            statusProgress.Value = Math.Min(statusProgress.Maximum, Math.Max(0, progressValue.Value));
    }
}
