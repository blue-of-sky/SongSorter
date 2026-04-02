using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SongConverter.Utils;

namespace SongConverter.Core;

public class DanConvertorCore
{
    private static readonly string[] MusicExtensions = { ".ogg", ".mp3", ".wav", ".wma", ".xa" };

    public static async Task ConvertAsync(string tjaPath, string outputRoot, string simuFolder, Action<string>? logAction = null, CancellationToken ct = default)
    {
        if (!File.Exists(tjaPath)) return;

        string tjaContent = await File.ReadAllTextAsync(tjaPath, Encoding.GetEncoding(932), ct);
        string[] lines = tjaContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var globalMeta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (line.StartsWith("#NEXTSONG", StringComparison.OrdinalIgnoreCase)) break;
            if (line.StartsWith("#START", StringComparison.OrdinalIgnoreCase)) break;
            var match = Regex.Match(line, @"^([A-Z0-9]+):\s*(.*)$", RegexOptions.IgnoreCase);
            if (match.Success) globalMeta[match.Groups[1].Value] = match.Groups[2].Value.Trim();
        }

        string? courseTitle = globalMeta.GetValueOrDefault("TITLE") ?? Path.GetFileNameWithoutExtension(tjaPath);
        string safeTitle = string.Join("_", courseTitle.Split(Path.GetInvalidFileNameChars()));
        string outputDir = Path.Combine(outputRoot, safeTitle);
        Directory.CreateDirectory(outputDir);

        logAction?.Invoke($"分割優先変換を開始: {courseTitle} -> {outputDir}");

        var danJson = new DanJson { title = courseTitle };
        foreach (var line in lines)
        {
            if (line.StartsWith("#NEXTSONG", StringComparison.OrdinalIgnoreCase)) break;
            var trimmed = line.Trim();
            if (trimmed.StartsWith("EXAM", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(trimmed, @"^EXAM\d*:\s*(.*)$", RegexOptions.IgnoreCase);
                if (match.Success) ParseExam(match.Groups[1].Value, danJson.conditions, danJson);
            }
            else if (trimmed.StartsWith("DANTICK:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(trimmed.Substring(8).Trim(), out int idx)) danJson.danIndex = idx;
            }
        }

        var sections = SplitIntoSections(lines, globalMeta);
        var finalSongs = new List<DanSong>();
        
        string localDir = Path.GetDirectoryName(tjaPath)!;
        string[]? allSimuFiles = !string.IsNullOrEmpty(simuFolder) && Directory.Exists(simuFolder)
                               ? await Task.Run(() => Directory.GetFiles(simuFolder, "*.*", SearchOption.AllDirectories), ct)
                               : null;

        foreach (var section in sections)
        {
            ct.ThrowIfCancellationRequested();
            bool processed = false;
            string targetTjaName = Path.ChangeExtension(section.Wave, ".tja");

            // --- Priority Level 1: Search Standalone TJA in Selected Local Folder ---
            string localTja = Path.Combine(localDir, targetTjaName);
            string localMusic = Path.Combine(localDir, section.Wave);
            
            if (File.Exists(localTja) && File.Exists(localMusic))
            {
                File.Copy(localTja, Path.Combine(outputDir, targetTjaName), true);
                File.Copy(localMusic, Path.Combine(outputDir, section.Wave), true);
                finalSongs.Add(new DanSong { path = targetTjaName, genre = section.Genre, difficulty = 3 });
                logAction?.Invoke($"  選択フォルダから反映: {section.Title}");
                processed = true;
            }

            // --- Priority Level 2: Split from Dan TJA ---
            if (!processed)
            {
                string tjaPathOut = Path.Combine(outputDir, targetTjaName);
                var sb = new StringBuilder();
                sb.AppendLine("//TJADB Project");
                sb.AppendLine($"TITLE:{section.Title}");
                sb.AppendLine($"TITLEJA:{section.Title}");
                sb.AppendLine($"SUBTITLE:{section.Subtitle}");
                sb.AppendLine($"SUBTITLEJA:{section.Subtitle}");
                sb.AppendLine($"BPM:{section.BPM}");
                sb.AppendLine($"WAVE:{section.Wave}");
                sb.AppendLine($"OFFSET:{section.Offset}");
                if (!string.IsNullOrEmpty(section.DemoStart)) sb.AppendLine($"DEMOSTART:{section.DemoStart}");
                sb.AppendLine("");

                sb.AppendLine("COURSE:Oni");
                sb.AppendLine($"LEVEL:{globalMeta.GetValueOrDefault("LEVEL", "10")}");
                if (globalMeta.ContainsKey("BALLOON")) sb.AppendLine($"BALLOON:{globalMeta["BALLOON"]}");
                if (!string.IsNullOrEmpty(section.ScoreInit)) sb.AppendLine($"SCOREINIT:{section.ScoreInit}");
                if (!string.IsNullOrEmpty(section.ScoreDiff)) sb.AppendLine($"SCOREDIFF:{section.ScoreDiff}");
                sb.AppendLine("");
                sb.AppendLine("#START");
                foreach (var l in section.Content)
                {
                    if (l.Trim().StartsWith("COURSE:", StringComparison.OrdinalIgnoreCase)) continue;
                    sb.AppendLine(l);
                    if (l.Trim().EndsWith("#END", StringComparison.OrdinalIgnoreCase)) break;
                }
                sb.AppendLine("#END");

                await File.WriteAllTextAsync(tjaPathOut, sb.ToString(), new UTF8Encoding(false), ct);
                
                // Find music file (Local folder first, then Simulator folder)
                string? waveFallback = File.Exists(localMusic) ? localMusic 
                                      : allSimuFiles?.FirstOrDefault(f => Path.GetFileName(f).Equals(section.Wave, StringComparison.OrdinalIgnoreCase));
                
                if (waveFallback != null)
                {
                    File.Copy(waveFallback, Path.Combine(outputDir, section.Wave), true);
                    logAction?.Invoke($"  分割譜面を生成 + 音源採取({(waveFallback.Contains(localDir) ? "選択フォルダ" : "シミュ")}): {section.Title}");
                }
                else
                {
                    logAction?.Invoke($"  警告: 音源が見つかりませんでした (要確認): {section.Title}");
                }

                finalSongs.Add(new DanSong { path = targetTjaName, genre = section.Genre, difficulty = 3 });
            }
        }

        danJson.danSongs = finalSongs.ToArray();
        string jsonPath = Path.Combine(outputDir, "Dan.json");
        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(danJson, options), new UTF8Encoding(false), ct);
        logAction?.Invoke($"完了: {jsonPath}");
    }

    private static List<SplitSection> SplitIntoSections(string[] lines, Dictionary<string, string> globalMeta)
    {
        var sections = new List<SplitSection>();
        SplitSection? current = null;
        string lastBpm = globalMeta.GetValueOrDefault("BPM", "120");
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#NEXTSONG", StringComparison.OrdinalIgnoreCase))
            {
                if (current != null) sections.Add(current);
                var parts = trimmed.Substring(9).Split(',');
                current = new SplitSection
                {
                    Title = parts.Length > 0 ? parts[0].Trim() : "Untitled",
                    Subtitle = parts.Length > 1 ? parts[1].Trim() : "",
                    Genre = parts.Length > 2 ? parts[2].Trim() : "",
                    Wave = parts.Length > 3 ? parts[3].Trim() : "song.ogg",
                    ScoreInit = parts.Length > 4 ? parts[4].Trim() : "",
                    ScoreDiff = parts.Length > 5 ? parts[5].Trim() : "",
                    BPM = lastBpm
                };
            }
            else if (trimmed.StartsWith("#BPMCHANGE", StringComparison.OrdinalIgnoreCase))
            {
                lastBpm = trimmed.Substring(10).Trim();
                if (current != null) { current.Content.Add(line); if (current.Content.Count < 20) current.BPM = lastBpm; }
            }
            else if (trimmed.StartsWith("#DELAY", StringComparison.OrdinalIgnoreCase))
            {
                if (current != null && double.TryParse(trimmed.Substring(6).Trim(), out double d)) current.Offset = (-d).ToString("F3");
            }
            else if (current != null) { current.Content.Add(line); }
        }
        if (current != null) sections.Add(current);
        return sections;
    }

    private static void ParseExam(string content, List<Condition> targetList, DanJson? root)
    {
        var parts = content.Split(',');
        if (parts.Length < 3) return;
        string typeCode = parts[0].Trim().ToLower();
        if (!int.TryParse(parts[1], out int red) || !int.TryParse(parts[2], out int gold)) return;
        bool more = true;
        if (parts.Length >= 4) { if (parts[3].Trim().ToLower() == "l") more = false; } else if (typeCode == "jb") more = false;
        if (typeCode == "g" && root != null) root.conditionGauge = new ConditionGauge { red = red, gold = gold };
        else
        {
            string typeName = typeCode switch { "jp" => "Perfect", "jg" => "Good", "jb" => "Miss", "s" => "Score", "r" => "Roll", "h" => "Hit", "c" => "Combo", _ => "Other" };
            targetList.Add(new Condition { type = typeName, threshold = new List<Threshold> { new Threshold { red = red, gold = gold } }, more = more });
        }
    }

    private class SplitSection
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Wave { get; set; } = "";
        public string ScoreInit { get; set; } = "";
        public string ScoreDiff { get; set; } = "";
        public string BPM { get; set; } = "";
        public string Offset { get; set; } = "0.000";
        public string DemoStart { get; set; } = "";
        public List<string> Content { get; set; } = new();
        public List<Condition> Conditions { get; set; } = new();
    }

    private class DanJson
    {
        public string title { get; set; } = "";
        public int danIndex { get; set; } = 0;
        public string danPlatePath { get; set; } = "Plate.png";
        public string danPanelSidePath { get; set; } = "panelside.png";
        public string danTitlePlatePath { get; set; } = "titleplate.png";
        public string danMiniPlatePath { get; set; } = "miniplate.png";
        public DanSong[] danSongs { get; set; } = Array.Empty<DanSong>();
        public ConditionGauge? conditionGauge { get; set; }
        public List<Condition> conditions { get; set; } = new();
    }

    private class DanSong
    {
        public string path { get; set; } = "";
        public int difficulty { get; set; } = 3;
        public string genre { get; set; } = "";
        public bool isHidden { get; set; } = true;
        public List<Condition>? conditions { get; set; }
    }

    private class ConditionGauge { public int red { get; set; } public int gold { get; set; } }
    private class Condition { public string type { get; set; } = ""; public List<Threshold> threshold { get; set; } = new(); public bool more { get; set; } = true; }
    private class Threshold { public int red { get; set; } public int gold { get; set; } }
}
