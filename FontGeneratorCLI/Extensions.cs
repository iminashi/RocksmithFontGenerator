using System.Globalization;

namespace FontGeneratorCLI
{
    public static class Extensions
    {
        public static bool IsKanaOrKanji(this char c)
        {
            return (c >= 0x4e00 && c <= 0x9fff)  // CJK Unified Ideographs
                || (c >= 0x3000 && c <= 0x30ff)  // Symbols, Punctuation, Kana
                || (c >= 0x31f0 && c <= 0x4dbf); // Katakana Phonetic Extensions, Enclosed CJK Letters and Months,
                                                 // CJK Compatibility, CJK Unified Ideographs Extension A
        }

        public static bool IsCombiningCategory(this UnicodeCategory category)
        {
            return category == UnicodeCategory.NonSpacingMark
                || category == UnicodeCategory.SpacingCombiningMark;
        }
    }
}
