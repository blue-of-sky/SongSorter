using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SongConverter.Utils;

public static class NormalizationUtils
{
    static readonly Dictionary<string, string[]> _titleAliases = BuildTitleAliases();
    static readonly string[] _titlePrefixesToStrip = { "双打" };
    static readonly string[] _titleSuffixesToStrip =
    {
        "NEWAUDIO",
        "OLDAUDIO",
        "BEENAVERSION",
        "SHORTVERSION",
        "LONGVERSION",
        "COVERVERSION",
        "TVXQVERSION",
        "初代"
    };

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
            if ("'’‘′‵ʼ＇".Contains(work)) work = '\'';
            if (work == '”' || work == '“') work = '"';

            // ローマ数字の統一 (Ⅰ-Ⅻ -> I, II, III...)
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

    public static IEnumerable<string> ExpandTitleMatchKeys(string normalizedTitle)
    {
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            yield break;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(normalizedTitle);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            yield return current;

            if (_titleAliases.TryGetValue(current, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (!string.IsNullOrWhiteSpace(alias) && !visited.Contains(alias))
                        queue.Enqueue(alias);
                }
            }

            foreach (var variant in GetHeuristicVariants(current))
            {
                if (!string.IsNullOrWhiteSpace(variant) && !visited.Contains(variant))
                    queue.Enqueue(variant);
            }
        }
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

    static Dictionary<string, string[]> BuildTitleAliases()
    {
        var work = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        AddAliasPair(work, "自力本願レボリューション", "THE REVO");
        AddAliasPair(work, "スパート！", "スパートシンドローマー");
        AddAliasPair(work, "天使と悪魔", "カーマイン");

        return work.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToArray(),
            StringComparer.Ordinal);
    }

    static void AddAliasPair(Dictionary<string, HashSet<string>> map, string a, string b)
    {
        var na = NormalizeTitle(a);
        var nb = NormalizeTitle(b);
        if (string.IsNullOrWhiteSpace(na) || string.IsNullOrWhiteSpace(nb) || na == nb)
            return;

        if (!map.TryGetValue(na, out var setA))
        {
            setA = new HashSet<string>(StringComparer.Ordinal);
            map[na] = setA;
        }
        setA.Add(nb);

        if (!map.TryGetValue(nb, out var setB))
        {
            setB = new HashSet<string>(StringComparer.Ordinal);
            map[nb] = setB;
        }
        setB.Add(na);
    }

    static IEnumerable<string> GetHeuristicVariants(string normalizedTitle)
    {
        var folded = FoldLatinDiacritics(normalizedTitle);
        if (!string.Equals(folded, normalizedTitle, StringComparison.Ordinal))
            yield return folded;

        var noPrefix = StripKnownPrefixes(normalizedTitle);
        if (!string.Equals(noPrefix, normalizedTitle, StringComparison.Ordinal))
            yield return noPrefix;

        var noSuffix = StripKnownSuffixes(normalizedTitle);
        if (!string.Equals(noSuffix, normalizedTitle, StringComparison.Ordinal))
            yield return noSuffix;

        var noSuffixNoPrefix = StripKnownPrefixes(noSuffix);
        if (!string.Equals(noSuffixNoPrefix, normalizedTitle, StringComparison.Ordinal)
            && !string.Equals(noSuffixNoPrefix, noSuffix, StringComparison.Ordinal))
            yield return noSuffixNoPrefix;

        var noFeat = StripAfterKeyword(normalizedTitle, "FEAT");
        if (!string.Equals(noFeat, normalizedTitle, StringComparison.Ordinal))
            yield return noFeat;

        if (!string.Equals(folded, normalizedTitle, StringComparison.Ordinal))
        {
            var foldedNoSuffix = StripKnownSuffixes(folded);
            if (!string.Equals(foldedNoSuffix, folded, StringComparison.Ordinal))
                yield return foldedNoSuffix;
        }
    }

    static string StripKnownPrefixes(string value)
    {
        var work = value;
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var prefix in _titlePrefixesToStrip)
            {
                if (work.StartsWith(prefix, StringComparison.Ordinal) && work.Length > prefix.Length + 1)
                {
                    work = work[prefix.Length..];
                    changed = true;
                    break;
                }
            }
        }

        return work;
    }

    static string StripKnownSuffixes(string value)
    {
        var work = value;
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var suffix in _titleSuffixesToStrip)
            {
                if (work.EndsWith(suffix, StringComparison.Ordinal) && work.Length > suffix.Length + 1)
                {
                    work = work[..^suffix.Length];
                    changed = true;
                    break;
                }
            }
        }

        return work;
    }

    static string StripAfterKeyword(string value, string keyword)
    {
        var idx = value.IndexOf(keyword, StringComparison.Ordinal);
        if (idx <= 0)
            return value;

        var trimmed = value[..idx];
        return trimmed.Length >= 3 ? trimmed : value;
    }

    static string FoldLatinDiacritics(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (!IsLatinRange(c))
            {
                sb.Append(c);
                continue;
            }

            var decomp = c.ToString().Normalize(NormalizationForm.FormD);
            var appended = false;
            foreach (var d in decomp)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(d) == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(d);
                appended = true;
            }

            if (!appended)
                sb.Append(c);
        }

        return sb.ToString();
    }

    static bool IsLatinRange(char c)
    {
        return (c >= 'A' && c <= 'Z')
            || (c >= 'a' && c <= 'z')
            || (c >= '\u00C0' && c <= '\u024F')
            || (c >= '\u1E00' && c <= '\u1EFF');
    }

    public static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(invalid.Contains(c) ? '_' : c);
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// ファイル名に使えない文字を削除する（Dan.jsonのpath用）
    /// </summary>
    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (!invalid.Contains(c))
                sb.Append(c);
        }
        return sb.ToString().Trim();
    }
}
