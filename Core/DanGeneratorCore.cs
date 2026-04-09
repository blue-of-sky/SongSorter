using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HtmlDoc = HtmlAgilityPack.HtmlDocument;
using SongConverter.Models;
using SongConverter.Utils;

namespace SongConverter.Core;

public class DanGeneratorCore
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task GenerateAsync(string inputSource, string outputDir, string songsFolder = "", string filter = "", Action<string>? logAction = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        string html;
        try 
        {
            if (inputSource.StartsWith("http"))
            {
                logAction?.Invoke($"URLからデータを取得中: {inputSource}");
                var response = await httpClient.GetAsync(inputSource, ct);
                response.EnsureSuccessStatusCode();
                html = await response.Content.ReadAsStringAsync(ct);
            }
            else
            {
                if (!File.Exists(inputSource)) {
                    logAction?.Invoke($"エラー: ファイルが見つかりません ({inputSource})");
                    return;
                }
                logAction?.Invoke($"{inputSource} を読み込んでいます...");
                html = await File.ReadAllTextAsync(inputSource, ct);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"データ取得エラー: {ex.Message}");
            return;
        }

        var doc = new HtmlDoc();
        doc.LoadHtml(html);

        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables == null)
        {
            logAction?.Invoke("エラー: テーブルが見つかりません。");
            return;
        }

        var rankNames = new[] { "五級", "四級", "三級", "二級", "一級", "初段", "二段", "三段", "四段", "五段", "六段", "七段", "八段", "九段", "十段", "玄人", "名人", "超人", "達人" };
        
        var excludeKeywords = new[] { "合格条件", "お題", "お品書き", "魂ゲージ", "たたけた数", "叩けた数", "総音符数", "ノーツ数", "不可", "連打数", "良", "可", "コンボ", "最低スコア", "スコア", "動画", "計", "楽曲名", "課題曲", "難易度", "難しさ", "むずかしさ", "強さ", "★", "レベル", "概要", "詳細", "備考", "リンク", "プレイ動画", "参照", "初出", "回数", "解放期間", "解放条件" };

        int foundOrder = 0;
        int totalProcessed = 0;
        var missingSongs = new List<string>();
        var danCoursesList = new List<(DanCourse course, string detectedRank, int rankIdx, HtmlNode row, Dictionary<int, string> colMap)>();

        foreach (var table in tables)
        {
            ct.ThrowIfCancellationRequested();
            if (table.InnerText.Contains("課題候補曲リスト")) continue;
            if (!table.InnerText.Contains("1st")) continue;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            string lastParsedRankName = "";

            for (int i = 0; i < rows.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var row = rows[i];
                var cellNodes = row.SelectNodes(".//td");
                if (cellNodes == null) continue;

                var cellTexts = cellNodes.Select(c => HtmlEntity.DeEntitize(c.InnerText.Trim())).ToList();

                if (!cellTexts.Any(t => t.Contains("魂ゲージ") || t.Contains("合格条件") || t.Contains("可") || t.Contains("不可") || t.Contains("叩けた数"))) continue;

                string detectedRank = FindRankNameFromRow(row, rankNames, excludeKeywords);

                if (string.IsNullOrEmpty(detectedRank) && i > 0)
                {
                    var aboveRow = rows[i - 1];
                    detectedRank = FindRankNameFromRow(aboveRow, rankNames, excludeKeywords);
                }

                if (string.IsNullOrEmpty(detectedRank) && cellTexts.Count > 0)
                {
                    foreach (var cellText in cellTexts) {
                        if (string.IsNullOrEmpty(cellText) || cellText.Length < 2) continue;
                        if (excludeKeywords.Any(k => cellText.Contains(k))) continue;
                        if (IsDatePattern(cellText)) continue;
                        detectedRank = cellText;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(detectedRank))
                {
                    detectedRank = detectedRank.Replace("(裏)", "").Replace("(おに)", "").Replace("(おに裏)", "").Trim();
                    detectedRank = Regex.Replace(detectedRank, @"^[（(]裏[）)]$","").Trim();
                }

                if (string.IsNullOrEmpty(detectedRank) || detectedRank == lastParsedRankName) continue;
                if (!string.IsNullOrEmpty(filter) && !detectedRank.Contains(filter)) continue;

                try
                {
                    int rankIdx = rankNames.ToList().FindIndex(r => detectedRank.Contains(r));
                    var dan = new DanCourse { title = detectedRank, danIndex = rankIdx >= 0 ? rankIdx : 0 };
                    logAction?.Invoke($"解析中: {detectedRank}");

                    var headerCells = row.SelectNodes(".//td");
                    var colMap = new Dictionary<int, string>();
                    if (headerCells != null)
                    {
                        int colOffset = 0;
                        foreach (var hc in headerCells)
                        {
                            string txt = HtmlEntity.DeEntitize(hc.InnerText.Trim());
                            int cs = hc.GetAttributeValue("colspan", 1);
                            if (txt.Contains("魂ゲージ")) colMap[colOffset] = "Gauge";
                            else if (txt.Contains("不可")) colMap[colOffset] = "Miss";
                            else if (txt.Contains("良")) colMap[colOffset] = "Great";
                            else if (txt.Contains("可")) colMap[colOffset] = "Good";
                            else if (txt.Contains("連打数")) colMap[colOffset] = "Roll";
                            else if (txt.Contains("たたけた数") || txt.Contains("叩けた数")) colMap[colOffset] = "Hit";
                            else if (txt.Contains("コンボ") || txt.Contains("最大コンボ数")) colMap[colOffset] = "MaxCombo";
                            else if (txt.Contains("最低スコア") || txt.Contains("スコア")) colMap[colOffset] = "Score";
                            colOffset += cs;
                        }
                    }

                    int songsAdded = 0;
                    for (int sIdx = 1; sIdx <= 6; sIdx++) 
                    {
                        if (i + sIdx >= rows.Count) break;
                        var sRow = rows[i + sIdx];
                        var sCells = sRow.SelectNodes(".//td");
                        if (sCells == null || sCells.Count < 2) continue;
                        if (sCells.Any(c => c.InnerText.Contains("魂ゲージ") || c.InnerText.Contains("合格条件"))) break;

                        var a = sRow.SelectSingleNode(".//a");
                        if (a == null) continue;
                        string songTitle = HtmlEntity.DeEntitize(a.InnerText.Trim());
                        if (excludeKeywords.Contains(songTitle)) continue;

                        string rowText = sRow.InnerText;
                        if (!rowText.Contains("st") && !rowText.Contains("nd") && !rowText.Contains("rd") && songsAdded >= 3) break;

                        // フォルダ名に使えない文字を削除（_ではなく削除）
                        string safeSongTitle = NormalizationUtils.SanitizeFileName(songTitle);
                        
                        string genre = "ナムコオリジナル";
                        var colorCell = sRow.SelectSingleNode(".//td[contains(@style, 'background-color:#')]");
                        if (colorCell != null) genre = MapGenreColor(GetStyleValue(colorCell, "background-color"));

                        string diffText = sRow.InnerText;
                        var diffCell = sCells.FirstOrDefault(c => c.InnerText.Contains("★"));
                        if (diffCell != null) diffText = diffCell.InnerText;
                        
                        // (裏)が含まれているかチェック（元のタイトルでチェック）
                        bool isUra = songTitle.Contains("(裏)") || diffText.Contains("裏") || diffText.Contains("(裏)");
                        
                        // pathから(裏)を削除
                        string pathTitle = safeSongTitle.Replace("(裏)", "").Replace("(裏譜面)", "").Trim();
                        
                        // 裏譜面の場合はdifficultyを4に、そうでなければDetectDifficultyの結果を使用
                        int difficulty = isUra ? 4 : DetectDifficulty(diffText);

                        dan.danSongs.Add(new DanSong { path = $"{pathTitle}.tja", difficulty = difficulty, genre = genre });
                        songsAdded++;

                        if (songsAdded == 1) ParseConditionsFromRow(sRow, colMap, dan);
                        if (songsAdded >= 3) break;
                    }

                    if (dan.danSongs.Count > 0)
                    {
                        // データをリストに保存（後でソートして出力）
                        danCoursesList.Add((dan, detectedRank, rankIdx, row, colMap));
                        lastParsedRankName = detectedRank;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { logAction?.Invoke($"  警告 ({detectedRank}): {ex.Message}"); }
            }
        }

        // ランク順にソート（達人→五級、つまりrankIdxの降順）
        danCoursesList = danCoursesList.OrderByDescending(d => d.rankIdx).ToList();

        // ソート済みデータを出力
        foreach (var item in danCoursesList)
        {
            ct.ThrowIfCancellationRequested();
            var dan = item.course;
            var detectedRank = item.detectedRank;
            var rankIdx = item.rankIdx;
            
            string prefix = foundOrder.ToString("D2");
            string safeRankName = NormalizationUtils.SanitizeFolderName(detectedRank);
            string rankFolder = Path.Combine(outputDir, $"{prefix} {safeRankName}");
            if (!Directory.Exists(rankFolder)) Directory.CreateDirectory(rankFolder);

            if (!string.IsNullOrEmpty(songsFolder) && Directory.Exists(songsFolder))
            {
                ct.ThrowIfCancellationRequested();
                var allDirs = Directory.GetDirectories(songsFolder, "*", SearchOption.AllDirectories);
                foreach (var s in dan.danSongs)
                {
                    ct.ThrowIfCancellationRequested();
                    string songNameRaw = Path.GetFileNameWithoutExtension(s.path); 
                    string songNameForSearch = songNameRaw.Replace("(裏譜面)", "").Replace("(裏)", "").Trim();
                    string? foundDir = FindDirectoryFuzzy(allDirs, songNameForSearch);
                    if (foundDir != null)
                    {
                        foreach (var file in Directory.GetFiles(foundDir))
                        {
                            string ext = Path.GetExtension(file).ToLower();
                            if (ext == ".tja") File.Copy(file, Path.Combine(rankFolder, s.path), true);
                            else if (ext == ".ogg" || ext == ".mp3") File.Copy(file, Path.Combine(rankFolder, Path.GetFileName(file)), true);
                        }
                    }
                    else { missingSongs.Add($"[{detectedRank}] {songNameRaw}"); }
                }
            }

            string json = JsonSerializer.Serialize(dan, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            await File.WriteAllTextAsync(Path.Combine(rankFolder, "Dan.json"), json, ct);
            totalProcessed++; foundOrder++;
        }

        logAction?.Invoke($"生成完了: {totalProcessed} 件の段位を処理しました。");
        if (missingSongs.Count > 0)
        {
            logAction?.Invoke(""); logAction?.Invoke("=========== 見つからなかった曲一覧 ===========");
            foreach (var ms in missingSongs.Distinct()) logAction?.Invoke(ms);
            logAction?.Invoke("==============================================");
        }
    }

    private static string FindRankNameFromRow(HtmlAgilityPack.HtmlNode row, string[] rankNames, string[] excludeKeywords)
    {
        var potentialNodes = row.SelectNodes(".//strong | .//span[contains(@style, 'font-size:16px')] | .//span[contains(@style, 'font-size:18px')] | .//span[contains(@style, 'font-size:20px')]");
        if (potentialNodes == null) return "";

        string bestMatch = "";
        foreach (var node in potentialNodes)
        {
            string txt = HtmlEntity.DeEntitize(node.InnerText.Trim());
            if (string.IsNullOrEmpty(txt) || txt.Length < 1) continue;
            if (excludeKeywords.Any(k => txt == k)) continue; 
            if (excludeKeywords.Any(k => txt.Contains(k)) && !rankNames.Any(rn => txt.Contains(rn))) continue; 
            if (IsDatePattern(txt)) continue;

            bestMatch = txt;
            if (rankNames.Any(rn => txt.Contains(rn))) return txt; 
        }
        return bestMatch;
    }

    private static bool IsDatePattern(string txt)
    {
        return Regex.IsMatch(txt, @"^\d+[\/\.]\d+") || Regex.IsMatch(txt, @"^\d+[\/\.]\d+[\/\.]\d+") || Regex.IsMatch(txt, @"^[\d\/\.～\-]+$");
    }

    private static string? FindDirectoryFuzzy(string[] dirs, string targetName)
    {
        string normalizedTarget = NormalizationUtils.NormalizeTitle(targetName);
        if (string.IsNullOrEmpty(normalizedTarget)) return null;
        var match = dirs.FirstOrDefault(d => NormalizationUtils.NormalizeTitle(Path.GetFileName(d)).EndsWith(normalizedTarget));
        if (match != null) return match;
        match = dirs.FirstOrDefault(d => NormalizationUtils.NormalizeTitle(Path.GetFileName(d)).Contains(normalizedTarget));
        if (match != null) return match;
        return null;
    }

    private static void ParseConditionsFromRow(HtmlAgilityPack.HtmlNode row, Dictionary<int, string> colMap, DanCourse dan)
    {
        var cells = row.SelectNodes(".//td");
        if (cells == null) return;
        int currentOriginalCol = 0;
        foreach (var cell in cells)
        {
            int cs = cell.GetAttributeValue("colspan", 1);
            if (cell.GetAttributeValue("rowspan", "") == "3")
            {
                if (colMap.TryGetValue(currentOriginalCol, out string? type))
                {
                    var redSpan = cell.SelectSingleNode(".//span[contains(@style, '#f23b08')]") ?? cell.SelectSingleNode(".//span[contains(@style, 'color:red')]");
                    var goldSpan = cell.SelectSingleNode(".//span[contains(@style, '#e8d03e')]") ?? cell.SelectSingleNode(".//strong");
                    if (redSpan != null && goldSpan != null)
                    {
                        int redV = ExtractNumber(redSpan.InnerText);
                        int goldV = ExtractNumber(goldSpan.InnerText);
                        if (type == "Gauge") { dan.conditionGauge.red = redV; dan.conditionGauge.gold = goldV; }
                        else { dan.conditions.Add(new Condition { type = type, threshold = new List<Threshold> { new Threshold { red = redV, gold = goldV } } }); }
                    }
                }
            }
            currentOriginalCol += cs;
        }
    }

    private static int ExtractNumber(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static int DetectDifficulty(string text)
    {
        if (text.Contains("裏") || text.Contains("(裏)")) return 4;
        if (text.Contains("おに")) return 3;
        if (text.Contains("むずかしい")) return 2;
        if (text.Contains("ふつう")) return 1;
        if (text.Contains("かんたん")) return 0;
        return 3; 
    }

    private static string MapGenreColor(string color)
    {
        color = color.ToLower();
        if (color.Contains("#ff7028")) return "ナムコオリジナル";
        if (color.Contains("#4aaaba")) return "アニメ";
        if (color.Contains("#9966cc")) return "ボーカロイド™曲";
        if (color.Contains("#0099ff")) return "ゲームミュージック";
        if (color.Contains("#ffbb00") || color.Contains("#ded523")) return "バラエティ";
        if (color.Contains("#bda600")) return "クラシック";
        if (color.Contains("#ff4400")) return "ポップス";
        if (color.Contains("#ff66ff")) return "キッズ";
        return "ナムコオリジナル";
    }

    private static string GetStyleValue(HtmlAgilityPack.HtmlNode node, string property)
    {
        string style = node.GetAttributeValue("style", "");
        var match = Regex.Match(style, property + @":\s*([^;]+)");
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }
}
