namespace RocksmithFontGenerator.Models
{
    public class ProgramState
    {
        public string FontFamilySource { get; set; }
        public string FontWeight { get; set; }
        public int FontSize { get; set; }
        public int KanjiFontSize { get; set; }

        public double BlurRadius { get; set; }
        public double Direction { get; set; }
        public double Opacity { get; set; }
        public double ShadowDepth { get; set; }

        public bool AdvancedExpanded { get; set; }
        public bool DisplayBoundingRectangles { get; set; }

        public int SpacingAdjustment { get; set; }
        public int HorizontalMargin { get; set; } = Defaults.GlyphHorizontalMargin;

        public string SelectedLanguage { get; set; }
        public bool UseDarkTheme { get; set; }

        public ProgramState() { }

        public ProgramState(MainWindowViewModel viewModel)
        {
            BlurRadius = viewModel.DropShadowSettings.BlurRadius;
            Direction = viewModel.DropShadowSettings.Direction;
            Opacity = viewModel.DropShadowSettings.Opacity;
            ShadowDepth = viewModel.DropShadowSettings.Depth;

            FontFamilySource = viewModel.SelectedFont.Source;
            FontWeight = viewModel.SelectedFontWeight.EnglishName;
            FontSize = viewModel.SelectedFontSize;
            KanjiFontSize = viewModel.SelectedKanjiFontSize;
            SpacingAdjustment = viewModel.SpacingAdjustment;
            HorizontalMargin = viewModel.GlyphHorizontalMargin;

            AdvancedExpanded = viewModel.AdvancedExpanded;
            DisplayBoundingRectangles = viewModel.DisplayBoundingRectanglesChecked;

            SelectedLanguage = viewModel.SelectedLanguage.Culture.Name;
            UseDarkTheme = viewModel.UseDarkTheme;
        }
    }
}
