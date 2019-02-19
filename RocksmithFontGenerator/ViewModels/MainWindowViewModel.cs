using DynamicData.Binding;
using MahApps.Metro;
using Microsoft.Win32;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RocksmithFontGenerator.Localization;
using RocksmithFontGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RocksmithFontGenerator
{
    public sealed class MainWindowViewModel : ReactiveObject
    {
        public readonly RS2014FontGenerator FontGenerator;

        public List<FontFamily> FontList { get; }

        public ObservableCollectionExtended<LocalizedFontWeight> FontWeightList { get; }

        public const string ProgramName = "Rocksmith 2014 Font Generator";
        private const string SavedStateFileName = "state.xml";

        public string ProgramVersion { get; set; }

        public DropShadowSettings DropShadowSettings { get; } = new DropShadowSettings();

        private readonly Canvas FontCanvas;
        private readonly FontFamily DefaultFontFamily;

        public ReactiveCommand<Unit, Unit> OpenFile { get; private set; }
        public ReactiveCommand<Unit, Unit> GenerateFont { get; private set; }
        public ReactiveCommand<Unit, Unit> SaveFont { get; private set; }
        public ReactiveCommand<Unit, Unit> ResetTextureSize { get; private set; }
        public ReactiveCommand<Unit, Unit> ResetToDefaults { get; private set; }

        [Reactive]
        public int GlyphHorizontalMargin { get; set; } = Defaults.GlyphHorizontalMargin;

        [Reactive]
        public int SpacingAdjustment { get; set; }

        //[Reactive]
        //public bool ReverseColors { get; set; }

        [Reactive]
        public bool UseAccurateInnerRects { get; set; } = true;

        [Reactive]
        public FontFamily SelectedFont { get; set; }

        [Reactive]
        public LocalizedFontWeight SelectedFontWeight { get; set; } = Defaults.FontWeight;

        [Reactive]
        public int SelectedFontSize { get; set; } = Defaults.FontSize;

        [Reactive]
        public int SelectedKanjiFontSize { get; set; } = Defaults.KanjiFontSize;

        [Reactive]
        public string WindowTitle { get; set; }

        [Reactive]
        public int TextureHeight { get; set; } = Defaults.TextureHeight;

        [Reactive]
        public int TextureWidth { get; set; } = Defaults.TextureWidth;

        [Reactive]
        public bool CanSave { get; set; }

        [Reactive]
        public bool CanGenerate { get; set; }

        [Reactive]
        public bool AdvancedExpanded { get; set; }

        [Reactive]
        public bool DisplayBoundingRectanglesChecked { get; set; }

        [Reactive]
        public Language SelectedLanguage { get; set; }

        [Reactive]
        public bool UseDarkTheme { get; set; }

        public List<Language> Languages { get; }

        public MainWindowViewModel(Canvas fontCanvas, Canvas rectCanvas, FontFamily defaultFontFamily)
        {
            CultureResources.EnumerateAvailableCultures();
            Languages = CultureResources.AvailableCultures.Select(culture => new Language(culture)).ToList();

            var currentLanguage = Languages.Find(l => l.Culture.TwoLetterISOLanguageName == Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
            if (currentLanguage == null)
                currentLanguage = Languages.Find(l => l.Culture == CultureResources.AvailableCultures[0]);

            SelectedLanguage = currentLanguage;
            FontWeightList = LocalizedFontWeights.All;

            this.ObservableForProperty(x => x.SelectedLanguage)
                .Subscribe(lang => CultureResources.ChangeCulture(lang.Value.Culture));

            FontGenerator = new RS2014FontGenerator(fontCanvas, rectCanvas);
            FontCanvas = fontCanvas;
            DefaultFontFamily = defaultFontFamily;

            FontList = Fonts.SystemFontFamilies.OrderBy(x => x.Source).ToList();

            LoadProgramState();

            WindowTitle = ProgramName;

            var thisAsm = Assembly.GetExecutingAssembly();
            ProgramVersion = thisAsm.GetName().Version.ToString();

            CreateReactiveCommands();

            this.WhenAnyValue(x => x.UseDarkTheme)
                .Subscribe(dt =>
                {
                    string theme = dt ? "BaseDark" : "BaseLight";

                    ThemeManager.ChangeAppStyle(Application.Current,
                            ThemeManager.GetAccent("Steel"),
                            ThemeManager.GetAppTheme(theme));
                });

            // Generate font after a delay when font size, glyph margin or spacing is changed
            this.WhenAnyValue(
                x => x.SelectedFontSize,
                x => x.SelectedKanjiFontSize,
                x => x.GlyphHorizontalMargin,
                x => x.SpacingAdjustment)
                .Where(_ => CanGenerate)   // Don't do anything if generation is currently disabled
                .Throttle(TimeSpan.FromMilliseconds(650), RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .Select(_ => Unit.Default)
                .InvokeCommand(GenerateFont);

            // Generate font immediately when font family or weight or "accurate inner rects" is changed
            this.WhenAnyValue(
                x => x.UseAccurateInnerRects,
                x => x.SelectedFont,
                x => x.SelectedFontWeight)
                .DistinctUntilChanged()
                .Select(_ => Unit.Default)
                .InvokeCommand(GenerateFont);
        }

        private void CreateReactiveCommands()
        {
            OpenFile = ReactiveCommand.CreateFromTask(OpenFile_Impl);

            var generateCanExecute = this.WhenAnyValue(x => x.CanGenerate);

            GenerateFont = ReactiveCommand.CreateFromTask(GenerateFont_Impl, generateCanExecute);

            var saveCanExecute = this.WhenAnyValue<MainWindowViewModel, bool, bool>(x => x.CanSave,
                (cansave) => cansave);

            SaveFont = ReactiveCommand.Create(SaveFont_Impl, saveCanExecute);
            ResetTextureSize = ReactiveCommand.CreateFromTask(ResetTextureSize_Impl, generateCanExecute);

            ResetToDefaults = ReactiveCommand.CreateFromTask(ResetToDefaults_Impl);
        }

        public void SaveProgramState()
        {
            var isoStore = IsolatedStorageFile.GetUserStoreForAssembly();

            using (var isoStream = new IsolatedStorageFileStream(SavedStateFileName, FileMode.Create, isoStore))
            using (var writer = new StreamWriter(isoStream))
            {
                var serializer = new XmlSerializer(typeof(ProgramState));

                var state = new ProgramState(this);

                serializer.Serialize(writer, state);
            }
        }

        private void LoadProgramState()
        {
            try
            {
                var isoStore = IsolatedStorageFile.GetUserStoreForAssembly();

                if (isoStore.FileExists(SavedStateFileName))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ProgramState));

                    using (var isoStream = new IsolatedStorageFileStream(SavedStateFileName, FileMode.Open, isoStore))
                    using (var reader = new StreamReader(isoStream))
                    {
                        var state = (ProgramState)serializer.Deserialize(reader);
                        var savedFontFamily = new FontFamily(state.FontFamilySource);

                        // Check if the saved font exists in the collection
                        // in case the user has deleted the font that was saved
                        if (FontList.Contains(savedFontFamily))
                            SelectedFont = savedFontFamily;
                        else
                            SelectedFont = DefaultFontFamily;

                        SelectedFontWeight = LocalizedFontWeight.FromEnglishName(state.FontWeight);
                        SelectedFontSize = state.FontSize;
                        SelectedKanjiFontSize = state.KanjiFontSize;
                        SpacingAdjustment = state.SpacingAdjustment;
                        GlyphHorizontalMargin = state.HorizontalMargin;

                        DropShadowSettings.BlurRadius = state.BlurRadius;
                        DropShadowSettings.Direction = state.Direction;
                        DropShadowSettings.Depth = state.ShadowDepth;
                        DropShadowSettings.Opacity = state.Opacity;

                        AdvancedExpanded = state.AdvancedExpanded;
                        DisplayBoundingRectanglesChecked = state.DisplayBoundingRectangles;
                        UseDarkTheme = state.UseDarkTheme;

                        if (!string.IsNullOrEmpty(state.SelectedLanguage))
                        {
                            CultureResources.ChangeCulture(new CultureInfo(state.SelectedLanguage));
                            var lang = Languages.Find(l => l.Culture.Name == state.SelectedLanguage);
                            if (lang != null)
                                SelectedLanguage = lang;
                        }
                    }
                }
                else
                {
                    SelectedFont = DefaultFontFamily;
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Loading state failed: " + ex.Message);
            }
        }

        private async Task OpenFile_Impl()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = $"{Properties.Resources.FileFilter_XML}|*.xml|{Properties.Resources.FileFilter_TXT}|*.txt",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FontGenerator.ClearGlyphs();
                FontGenerator.ResetTextureSize();

                if (openFileDialog.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    ReadVocalsXML(openFileDialog.FileName);
                else
                    ReadTextFile(openFileDialog.FileName);

                await GenerateFontIfReady().ConfigureAwait(false);
            }
        }

        public async Task GenerateFontIfReady()
        {
            if (CanGenerate)
                await GenerateFont.Execute();
        }

        private static bool IsValidFile(string filename)
        {
            using (var reader = XmlReader.Create(filename))
            {
                reader.MoveToContent();
                return reader.Name == "vocals";
            }
        }

        public void ReadTextFile(string filename)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                char[] stripChars = new[] { '\t', '\r', '\n' };

                using (StreamReader reader = new StreamReader(filename))
                {
                    while(true)
                    {
                        int c = reader.Read();
                        if (c == -1)
                            break;

                        char readChar = (char)c;
                        if (Array.IndexOf(stripChars, readChar) < 0)
                            stringBuilder.Append(readChar);
                    }
                }

                FontGenerator.AddGlyphsFromWord(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                OpenFailed(ex.Message);
                return;
            }

            WindowTitle = filename + " - " + ProgramName;

            CanGenerate = true;
        }

        private void OpenFailed(string message)
        {
            MessageBox.Show(
                $"{Properties.Resources.Error_OpenFile_Failure}\n\n{message}",
                Properties.Resources.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            FontGenerator.ClearGlyphs();
            WindowTitle = ProgramName;

            CanGenerate = false;
        }

        public void ReadVocalsXML(string filename)
        {
            try
            {
                if (!IsValidFile(filename))
                    throw new FileFormatException(Properties.Resources.Error_OpenFile_WrongFormat);

                XElement jpXML = XElement.Load(filename);

                foreach (var vocal in jpXML.Elements("vocal"))
                {
                    string lyric = vocal.Attribute("lyric").Value;
                    string lyricTrimmed = lyric.EndsWith("+") || lyric.EndsWith("-") ? lyric.Substring(0, lyric.Length - 1) : lyric;

                    FontGenerator.AddGlyphsFromWord(lyricTrimmed);
                }

                // Space is always included in official Japanese lyrics
                FontGenerator.AddGlyph(" ");
            }
            catch (Exception ex)
            {
                OpenFailed(ex.Message);
                return;
            }

            WindowTitle = filename + " - " + ProgramName;

            CanGenerate = true;
        }

        private async Task GenerateFont_Impl()
        {
            FontFamily fontFamily = DefaultFontFamily;

            if (SelectedFont != null)
                fontFamily = SelectedFont;

            //FontGenerator.ReverseColors = (bool)reverseColorsCheckBox.IsChecked;

            // Validate font sizes
            int fontSize = RS2014FontGenerator.GetValidFontSize(SelectedFontSize);
            int jpFontSize = RS2014FontGenerator.GetValidFontSize(SelectedKanjiFontSize);

            FontGenerator.GlyphHorizontalMargin = GlyphHorizontalMargin;
            FontGenerator.UseAccurateInnerRects = UseAccurateInnerRects;
            FontGenerator.SpacingAdjustment = SpacingAdjustment;

            FontGenerator.SetFont(fontFamily, SelectedFontWeight.Weight, fontSize, jpFontSize);

            CanSave = false;

            // Generating the font will block the thread
            // But invoke it anyway so that the UI has time to update (change mouse cursor etc)
            GenerationResult result = await Application.Current.Dispatcher.InvokeAsync(
                () => FontGenerator.TryGenerateFont(),
                DispatcherPriority.Background);

            switch (result)
            {
                case GenerationResult.Success:
                    CanSave = true;
                    TextureHeight = FontGenerator.TextureHeight;
                    TextureWidth = FontGenerator.TextureWidth;
                    break;

                case GenerationResult.DidNotFitIntoMaxSize:
                    MessageBox.Show($"{Properties.Resources.Error_Generation_TooManyGlyphs} (1024x1024)");
                    break;

                case GenerationResult.UserCanceled:
                    break;

                default:
                    Debug.Print("Unexpected generation result!");
                    break;
            }
        }

        private void SaveFont_Impl()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = $"{Properties.Resources.FileFilter_DDS}|*.dds|{Properties.Resources.FileFilter_PNGandDDS}|*.png",
                FileName = "lyrics",
                AddExtension = true
            };

            try
            {
                if (saveFileDialog.ShowDialog() == true)
                {
                    string tempPath = Path.GetTempPath();
                    string tempPngFile = Path.Combine(tempPath, "temptexture.png");
                    string tempDdsFile = Path.Combine(tempPath, "temptexture.dds");

                    string filename = saveFileDialog.FileName;
                    string ddsFilename = filename;
                    string pngFilename = filename;
                    bool keepPngFile = false;

                    if (filename.EndsWith(".dds"))
                    {
                        pngFilename = Path.ChangeExtension(filename, ".png");
                    }
                    else
                    {
                        ddsFilename = Path.ChangeExtension(filename, ".dds");
                        keepPngFile = true;
                    }

                    string definitionFilename = Path.ChangeExtension(filename, ".glyphs.xml");

                    GlyphDefinitions.Save(definitionFilename, FontGenerator);

                    BitmapFunctions.SaveImage(FontCanvas, tempPngFile, TextureWidth, TextureHeight);

                    using (Process nvdxtProcess = new Process())
                    {
                        nvdxtProcess.StartInfo.UseShellExecute = false;
                        nvdxtProcess.StartInfo.CreateNoWindow = true;
                        nvdxtProcess.StartInfo.FileName = "nvdxt.exe";
                        nvdxtProcess.StartInfo.Arguments = $"-file \"{tempPngFile}\" -output \"{tempDdsFile}\" -quality_highest -dxt5 -nomipmap -overwrite -forcewrite";

                        nvdxtProcess.Start();
                        nvdxtProcess.WaitForExit();
                    }

                    File.Delete(ddsFilename);
                    File.Move(tempDdsFile, ddsFilename);

                    if (keepPngFile)
                    {
                        File.Delete(pngFilename);
                        File.Move(tempPngFile, pngFilename);
                    }
                    else
                    {
                        File.Delete(tempPngFile);
                    }
                }
            }
            catch (Exception ex)
            {
                string message =
                    Properties.Resources.Error_General
                    + Environment.NewLine + Environment.NewLine
                    + ex.Message;

                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task ResetTextureSize_Impl()
        {
            FontGenerator.ResetTextureSize();

            return GenerateFontIfReady();
        }

        private Task ResetToDefaults_Impl()
        {
            using (DisableGeneration())
            {
                DropShadowSettings.BlurRadius = Defaults.ShadowBlurRadius;
                DropShadowSettings.Direction = Defaults.ShadowDirection;
                DropShadowSettings.Opacity = Defaults.ShadowOpacity;
                DropShadowSettings.Depth = Defaults.ShadowDepth;

                SelectedFontSize = Defaults.FontSize;
                SelectedKanjiFontSize = Defaults.KanjiFontSize;
                SelectedFontWeight = Defaults.FontWeight;
                GlyphHorizontalMargin = Defaults.GlyphHorizontalMargin;
                UseAccurateInnerRects = Defaults.UseAccurateInnerRects;
                SpacingAdjustment = Defaults.SpacingAdjustment;
            }

            return GenerateFontIfReady();
        }

        public IDisposable DisableGeneration()
        {
            bool oldCanGenerate = CanGenerate;
            CanGenerate = false;

            return Disposable.Create(oldCanGenerate, old => CanGenerate = old);
        }
    }
}
