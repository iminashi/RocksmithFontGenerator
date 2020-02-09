namespace FontGeneratorCLI
{
    public static class Defaults
    {
        public const int GlyphHeight = 52;

        public const int TextureWidth = 512;
        public const int TextureHeight = 512;

        public const int SpacingAdjustment = 0;

        // 4 in the default font, 8 in Japanese fonts
        // As long as it is constant across all glyphs,
        // It does not affect character spacing in game, e.g. values 1 and 15 look the same
        // Only the empty space inside the inner bounding rect matters
        public const int GlyphHorizontalMargin = 8;

        public const int FontSize = 31;
        public const int KanjiFontSize = 31;
        public const int MaxFontSize = 60;
        public const int MinFontSize = 18;
        //public static readonly LocalizedFontWeight FontWeight = LocalizedFontWeights.Bold;

        public const double ShadowBlurRadius = 2.0;
        public const double ShadowDirection = 310.0;
        public const double ShadowOpacity = 1.0;
        public const double ShadowDepth = 4.0;

        public const bool UseAccurateInnerRects = true;
    }
}
