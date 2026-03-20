using System.Collections.Immutable;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;

namespace SongSorterApp;

public record SongInfo(string Title, string Subtitle);

/// <summary>
/// 太鼓の達人 楽曲リストページから曲名とサブタイトル（アーティスト名など）を抽出する
/// </summary>
public static class SongListFetcher
{
    const string BaseUrl = "https://taiko.namco-ch.net/taiko/songlist/";
    static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" } }
    };

    static readonly HashSet<string> SkipTitles = new(StringComparer.Ordinal) { "曲名", "難易度" };

    /// <summary>
    /// カテゴリ一覧（表示名, ファイル名）
    /// </summary>
    public static ImmutableArray<(string DisplayName, string FileName)> Categories { get; } = ImmutableArray.Create(
        ("ナムコオリジナル", "namco.php"),
        ("ポップス", "pops.php"),
        ("キッズ", "kids.php"),
        ("アニメ", "anime.php"),
        ("ボーカロイド曲", "vocaloid.php"),
        ("ゲームミュージック", "game.php"),
        ("バラエティ", "variety.php"),
        ("クラシック", "classic.php")
    );

    public static string GetUrl(string fileName) => BaseUrl + fileName;

    /// <summary>
    /// 指定カテゴリのHTMLを取得し、曲情を報リストを返す
    /// </summary>
    public static async Task<List<SongInfo>> FetchSongsAsync(string fileName, CancellationToken ct = default)
    {
        var url = GetUrl(fileName);
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var encoding = response.Content.Headers.ContentType?.CharSet != null
            ? Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet)
            : Encoding.UTF8;
        var html = encoding.GetString(bytes);

        return ExtractSongs(html);
    }

    /// <summary>
    /// HTMLから &lt;th&gt; から曲名とサブタイトルを抽出
    /// </summary>
    public static List<SongInfo> ExtractSongs(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        var songs = new List<SongInfo>();
        var seenNormalized = new HashSet<string>(StringComparer.Ordinal);

        foreach (var th in doc.DocumentNode.SelectNodes("//th") ?? Enumerable.Empty<HtmlNode>())
        {
            var title = GetTitleFromTh(th);
            if (string.IsNullOrWhiteSpace(title)) continue;
            title = NormalizeCellText(title);
             
            // 明らかな「曲名」ではない行をスキップ
            if (SkipTitles.Contains(title)) continue;
            if (title.Contains("ショップ") || title.Contains("AIバトル") || title.Contains("アイコンの説明") || title.Contains("どんメダル")) continue;
            if (title == "曲名" || title == "難易度" || title.StartsWith("各アイコン")) continue;

            // <p> 内のテキストをサブタイトルとして取得
            var subtitleNode = th.SelectSingleNode(".//p");
            var subtitle = subtitleNode != null ? NormalizeCellText(subtitleNode.InnerText) : string.Empty;

            // 重複チェック（正規化したキーで判定）
            var normKey = $"{NormalizationUtils.NormalizeTitle(title)}|{NormalizationUtils.NormalizeSubtitle(subtitle)}";
             
            if (seenNormalized.Add(normKey))
            {
                songs.Add(new SongInfo(title, subtitle));
            }
        }

        return songs;
    }

    static string? GetTitleFromTh(HtmlNode th)
    {
        var pieces = new List<string>();
        foreach (var node in th.ChildNodes)
        {
            if (node.Name.Equals("p", StringComparison.OrdinalIgnoreCase))
                break; // サブタイトル開始

            if (ShouldSkipTitleNode(node))
                continue;

            var text = NormalizeCellText(node.InnerText);
            if (string.IsNullOrEmpty(text))
                continue;

            pieces.Add(text);
        }

        var res = NormalizeCellText(string.Join(" ", pieces));
        return string.IsNullOrEmpty(res) ? null : res;
    }

    static bool ShouldSkipTitleNode(HtmlNode node)
    {
        if (!node.Name.Equals("span", StringComparison.OrdinalIgnoreCase))
            return false;

        var className = node.GetAttributeValue("class", string.Empty);
        if (string.IsNullOrWhiteSpace(className))
            return false;

        return className.Contains("new", StringComparison.OrdinalIgnoreCase)
               || className.Contains("ico", StringComparison.OrdinalIgnoreCase);
    }

    static string NormalizeCellText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var decoded = System.Net.WebUtility.HtmlDecode(text).Replace('\u00A0', ' ');
        return string.Join(" ", decoded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
