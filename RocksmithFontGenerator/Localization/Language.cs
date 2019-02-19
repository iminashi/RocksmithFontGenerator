using System;
using System.Globalization;

namespace RocksmithFontGenerator.Localization
{
    public class Language
    {
        private readonly string name;

        public CultureInfo Culture { get; }

        public Language(CultureInfo culture)
        {
            Culture = culture;

            // Capitalize the language name
            var temp = culture.NativeName.ToCharArray();
            temp[0] = char.ToUpper(temp[0], culture);
            name = new string(temp);
        }

        public override string ToString() => name;
    }
}
