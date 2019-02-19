using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Data;

namespace RocksmithFontGenerator.Localization
{
    // After https://www.codeproject.com/Articles/22967/WPF-Runtime-Localization

    public static class CultureResources
    {
        public static List<CultureInfo> AvailableCultures = new List<CultureInfo>
        {
            new CultureInfo("en") // Default language
        };

        public static void EnumerateAvailableCultures()
        {
            CultureInfo culture = null;
            foreach (string dir in Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory))
            {
                try
                {
                    // See if this directory corresponds to a valid culture name
                    DirectoryInfo dirinfo = new DirectoryInfo(dir);
                    culture = CultureInfo.GetCultureInfo(dirinfo.Name);

                    // Determine if a resources DLL exists in this directory that
                    // matches the executable name
                    if (dirinfo.GetFiles(Assembly.GetExecutingAssembly().GetName().Name + ".resources.dll").Length > 0)
                    {
                        AvailableCultures.Add(culture);
                    }
                }
                // Ignore any ArgumentExceptions generated for non-culture directories in the bin folder
                catch { }
            }
        }

        public static Properties.Resources GetResourceInstance()
        {
            return new Properties.Resources();
        }

        public static void ChangeCulture(CultureInfo culture)
        {
            if (AvailableCultures.Contains(culture))
            {
                Properties.Resources.Culture = culture;
                var resourceProvider = (ObjectDataProvider)App.Current.FindResource("Localization");
                resourceProvider.Refresh();

                LocalizedFontWeights.RefreshAll();
            }
        }
    }
}
