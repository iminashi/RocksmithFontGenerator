using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace RocksmithFontGenerator.Localization
{
    public sealed class LocalizedFontWeight : INotifyPropertyChanged
    {
        public FontWeight Weight { get; }

        public string LocalizedName
            => Properties.Resources.ResourceManager.GetString("FontWeight_" + EnglishName, Properties.Resources.Culture);

        public string EnglishName
            => Weight.ToString();

        public event PropertyChangedEventHandler PropertyChanged;

        public LocalizedFontWeight(FontWeight weight)
        {
            Weight = weight;
        }

        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedName)));
        }

        public static LocalizedFontWeight FromEnglishName(string name)
        {
            return LocalizedFontWeights.All
                .FirstOrDefault(w => w.EnglishName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public override string ToString()
            => LocalizedName;

        public override bool Equals(object obj)
            => obj is LocalizedFontWeight other && Weight.Equals(other.Weight);

        public override int GetHashCode()
            => Weight.GetHashCode();

        public static bool operator ==(LocalizedFontWeight left, LocalizedFontWeight right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(LocalizedFontWeight left, LocalizedFontWeight right)
            => !(left == right);
    }
}
