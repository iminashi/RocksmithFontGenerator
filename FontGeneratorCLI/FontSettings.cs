using System.Windows;
using System.Windows.Media;

namespace FontGeneratorCLI
{
    public sealed class FontSettings
    {
        public FontFamily FontFamily { get; set; }
        public FontWeight FontWeight { get; set; }
        public int FontSize { get; set; } = Defaults.FontSize;
        public int KanjiFontSize { get; set; } = Defaults.KanjiFontSize;
    }
}
