using DynamicData.Binding;
using System;
using System.Windows;

namespace RocksmithFontGenerator.Localization
{
    public static class LocalizedFontWeights
    {
        public static LocalizedFontWeight Light = new LocalizedFontWeight(FontWeights.Light);
        public static LocalizedFontWeight Normal = new LocalizedFontWeight(FontWeights.Normal);
        public static LocalizedFontWeight Medium = new LocalizedFontWeight(FontWeights.Medium);
        public static LocalizedFontWeight Bold = new LocalizedFontWeight(FontWeights.Bold);
        public static LocalizedFontWeight Black = new LocalizedFontWeight(FontWeights.Black);

        public static ObservableCollectionExtended<LocalizedFontWeight> All = new ObservableCollectionExtended<LocalizedFontWeight>
        {
            Light,
            Normal,
            Medium,
            Bold,
            Black
        };

        public static void RefreshAll()
        {
            foreach (var weight in All)
            {
                weight.Refresh();
            }
        }
    }
}
