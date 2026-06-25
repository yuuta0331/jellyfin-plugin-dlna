using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Classifies media titles into gojuon browse rows.
/// </summary>
internal static class KanaTitleClassifier
{
    private const int MaxPrefixStripIterations = 5;

    private static readonly Dictionary<char, int> RowByBaseKana = BuildRowMap();

    /// <summary>
    /// Classifies a title into a kana browse row index.
    /// </summary>
    /// <returns>Row index 0-11, or null when input is invalid.</returns>
    public static int? Classify(string? sortName, string? name, KanaClassificationOptions options)
    {
        var sortNameRow = ClassifySingle(sortName, options, out var sortNameStartsWithKanji);
        var nameRow = ClassifySingle(name, options, out var nameStartsWithKanji);
        return ChooseBestRow(sortNameRow, nameRow, sortNameStartsWithKanji, nameStartsWithKanji);
    }

    private static int ChooseBestRow(
        int? sortNameRow,
        int? nameRow,
        bool sortNameStartsWithKanji,
        bool nameStartsWithKanji)
    {
        // Use the first classifiable character only (頭文字). Prefer an explicit kana reading.
        if (sortNameRow is >= 0 and <= 9)
        {
            return sortNameRow.Value;
        }

        if (nameRow is >= 0 and <= 9)
        {
            return nameRow.Value;
        }

        // Do not map kanji-first Japanese titles to romanized SortName buckets.
        if (sortNameRow == KanaRowHelper.AlphanumericRowIndex && !nameStartsWithKanji)
        {
            return KanaRowHelper.AlphanumericRowIndex;
        }

        if (nameRow == KanaRowHelper.AlphanumericRowIndex)
        {
            return KanaRowHelper.AlphanumericRowIndex;
        }

        if (nameStartsWithKanji)
        {
            return nameRow ?? KanaRowHelper.OtherRowIndex;
        }

        return sortNameRow ?? nameRow ?? KanaRowHelper.OtherRowIndex;
    }

    private static int? ClassifySingle(string? source, KanaClassificationOptions options, out bool startsWithKanji)
    {
        startsWithKanji = false;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var normalized = NormalizeString(source);
        if (options.EnablePrefixStripping)
        {
            normalized = StripPrefixes(normalized, options.Prefixes);
        }

        return ClassifyFirstCharacter(normalized, out startsWithKanji);
    }

    private static int ClassifyFirstCharacter(string normalized, out bool startsWithKanji)
    {
        startsWithKanji = false;
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = NormalizeKanaChar(normalized[i]);
            if (IsLeadingSkippable(c))
            {
                continue;
            }

            if (IsKanji(c))
            {
                startsWithKanji = true;
                return KanaRowHelper.OtherRowIndex;
            }

            if (IsAlphanumeric(c))
            {
                return KanaRowHelper.AlphanumericRowIndex;
            }

            if (IsHiragana(c))
            {
                var baseKana = ToBaseKana(c);
                if (RowByBaseKana.TryGetValue(baseKana, out var rowIndex))
                {
                    return rowIndex;
                }
            }

            return KanaRowHelper.OtherRowIndex;
        }

        return KanaRowHelper.OtherRowIndex;
    }

    private static string NormalizeString(string input)
    {
        var nfkc = input.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(nfkc.Length);
        foreach (var c in nfkc)
        {
            builder.Append(NormalizeKanaChar(c));
        }

        return builder.ToString();
    }

    private static string StripPrefixes(string text, IReadOnlyList<string> prefixes)
    {
        if (prefixes.Count == 0)
        {
            return text;
        }

        var result = text;
        for (var iteration = 0; iteration < MaxPrefixStripIterations; iteration++)
        {
            var index = 0;
            while (index < result.Length && IsLeadingSkippable(result[index]))
            {
                index++;
            }

            if (index > 0)
            {
                result = result[index..];
            }

            if (result.Length == 0)
            {
                break;
            }

            var stripped = false;
            foreach (var prefix in prefixes)
            {
                if (TryMatchPrefix(result, prefix, out var prefixLength))
                {
                    result = result[prefixLength..];
                    stripped = true;
                    break;
                }
            }

            if (!stripped)
            {
                break;
            }
        }

        return result;
    }

    private static bool TryMatchPrefix(string text, string prefix, out int prefixLength)
    {
        prefixLength = 0;
        if (string.IsNullOrEmpty(prefix))
        {
            return false;
        }

        if (text.StartsWith(prefix, StringComparison.Ordinal))
        {
            prefixLength = prefix.Length;
            return true;
        }

        if (ContainsAsciiLetter(prefix)
            && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            prefixLength = prefix.Length;
            return true;
        }

        return false;
    }

    private static bool ContainsAsciiLetter(string value)
    {
        foreach (var c in value)
        {
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
            {
                return true;
            }
        }

        return false;
    }

    private static char NormalizeKanaChar(char c)
    {
        return c switch
        {
            'ゑ' or 'ヱ' => 'え',
            'ゐ' or 'ヰ' => 'い',
            '\u30F4' or '\u3094' => 'う',
            >= '\u30A1' and <= '\u30F6' => (char)(c - 0x60),
            _ => c
        };
    }

    private static char ToBaseKana(char c)
    {
        c = c switch
        {
            'ぁ' => 'あ',
            'ぃ' => 'い',
            'ぅ' => 'う',
            'ぇ' => 'え',
            'ぉ' => 'お',
            'っ' => 'つ',
            'ゃ' => 'や',
            'ゅ' => 'ゆ',
            'ょ' => 'よ',
            'ゎ' => 'わ',
            'が' or 'ぎ' or 'ぐ' or 'げ' or 'ご' => (char)(c - 1),
            'ざ' or 'じ' or 'ず' or 'ぜ' or 'ぞ' => (char)(c - 1),
            'だ' or 'ぢ' or 'づ' or 'で' or 'ど' => (char)(c - 1),
            'ば' or 'び' or 'ぶ' or 'べ' or 'ぼ' => (char)(c - 1),
            'ぱ' or 'ぴ' or 'ぷ' or 'ぺ' or 'ぽ' => (char)(c - 2),
            _ => c
        };

        return c;
    }

    private static bool IsLeadingSkippable(char c)
    {
        if (char.IsWhiteSpace(c))
        {
            return true;
        }

        return CharUnicodeInfo.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.OpenPunctuation => true,
            UnicodeCategory.ClosePunctuation => true,
            UnicodeCategory.InitialQuotePunctuation => true,
            UnicodeCategory.FinalQuotePunctuation => true,
            UnicodeCategory.OtherPunctuation => true,
            UnicodeCategory.DashPunctuation => true,
            UnicodeCategory.MathSymbol => true,
            UnicodeCategory.CurrencySymbol => true,
            UnicodeCategory.ModifierSymbol => true,
            UnicodeCategory.OtherSymbol => true,
            _ => false
        };
    }

    private static bool IsKanji(char c)
        => c is >= '\u4E00' and <= '\u9FFF';

    private static bool IsAlphanumeric(char c)
        => (c >= 'A' && c <= 'Z')
           || (c >= 'a' && c <= 'z')
           || (c >= '0' && c <= '9');

    private static bool IsHiragana(char c)
        => c is >= '\u3041' and <= '\u309F';

    private static Dictionary<char, int> BuildRowMap()
    {
        var map = new Dictionary<char, int>();
        AddRow(map, 0, "あいうえお");
        AddRow(map, 1, "かきくけこ");
        AddRow(map, 2, "さしすせそ");
        AddRow(map, 3, "たちつてと");
        AddRow(map, 4, "なにぬねの");
        AddRow(map, 5, "はひふへほ");
        AddRow(map, 6, "まみむめも");
        AddRow(map, 7, "やゆよ");
        AddRow(map, 8, "らりるれろ");
        AddRow(map, 9, "わをん");
        return map;
    }

    private static void AddRow(Dictionary<char, int> map, int rowIndex, string chars)
    {
        foreach (var c in chars)
        {
            map[c] = rowIndex;
        }
    }
}
