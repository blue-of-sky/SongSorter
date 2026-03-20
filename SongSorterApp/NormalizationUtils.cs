using System.Text;
using System.Globalization;

namespace SongSorterApp;

public static class NormalizationUtils
{
    public static string NormalizeTitle(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        // 全角英数字を半角に変換、記号を統一
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormKC))
        {
            var work = c;
            // 全角英数字 (0xFF01-0xFF5E) -> 半角 (0x0021-0x007E)
            if (work >= 0xFF01 && work <= 0xFF5E)
                work = (char)(work - 0xFEE0);
            
            // アポストロフィ系の表記ゆれを統一
            // 例: ' ’ ‘ ′ ‵ ʼ ＇
            if ("'’‘′‵ʼ＇".Contains(work)) work = '\'';
            if (work == '”' || work == '“') work = '"';

            // ローマ数字の統一 (Ⅰ-Ⅻ -> I, II, III...)
            // 簡易的に Ⅰ, Ⅱ, Ⅲ などの頻出文字に対応
            if (work == 'Ⅰ') { sb.Append("I"); continue; }
            if (work == 'Ⅱ') { sb.Append("II"); continue; }
            if (work == 'Ⅲ') { sb.Append("III"); continue; }
            if (work == 'Ⅳ') { sb.Append("IV"); continue; }
            if (work == 'Ⅴ') { sb.Append("V"); continue; }
            if (work == 'Ⅵ') { sb.Append("VI"); continue; }
            if (work == 'Ⅶ') { sb.Append("VII"); continue; }
            if (work == 'Ⅷ') { sb.Append("VIII"); continue; }
            if (work == 'Ⅸ') { sb.Append("IX"); continue; }
            if (work == 'Ⅹ') { sb.Append("X"); continue; }

            // 空白、制御、および特定記号の除去
            if (char.IsWhiteSpace(work) || char.IsControl(work)) continue;

            if (IsIgnorableSymbolOrPunctuation(work)) continue;
            
            sb.Append(work);
        }
        return sb.ToString().ToUpperInvariant();
    }

    public static string NormalizeSubtitle(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        // TJAのサブタイトルによくある接頭辞 "--", "++" を除去
        var work = s.TrimStart().TrimStart('-').TrimStart('+').Trim();
        return NormalizeTitle(work);
    }

    static bool IsIgnorableSymbolOrPunctuation(char c)
    {
        // 文字照合時に差分ノイズになりやすい記号類は網羅的に除外する
        // 例: -, －, ‐, ‑, –, —, ―, −, ~, ～, ☆, ★, !, ?, 記号括弧など
        // これにより表記ゆれに強くなる
        if (c == '\uFE0E' || c == '\uFE0F' || c == '\u200D') return true; // emoji variation / ZWJ

        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
        return cat is UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.DashPunctuation
            or UnicodeCategory.OpenPunctuation
            or UnicodeCategory.ClosePunctuation
            or UnicodeCategory.InitialQuotePunctuation
            or UnicodeCategory.FinalQuotePunctuation
            or UnicodeCategory.OtherPunctuation
            or UnicodeCategory.MathSymbol
            or UnicodeCategory.CurrencySymbol
            or UnicodeCategory.ModifierSymbol
            or UnicodeCategory.OtherSymbol;
    }
}
