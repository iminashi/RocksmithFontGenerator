using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Xml;
using System.Xml.Linq;

namespace FontGeneratorCLI
{
    static class Program
    {
        private static RS2014FontGenerator FontGenerator { get; set; }
        private static Canvas FontCanvas { get; set; }
        private static int TextureWidth { get; set; } = Defaults.TextureWidth;
        private static int TextureHeight { get; set; } = Defaults.TextureHeight;

        private static bool IsValidFile(string filename)
        {
            using (var reader = XmlReader.Create(filename))
            {
                reader.MoveToContent();
                return reader.Name == "vocals";
            }
        }

        private static bool LoadXMLFile(string filename)
        {
            try
            {
                if (!IsValidFile(filename))
                {
                    Console.WriteLine("Not a valid lyric file.");
                    return false;
                }

                XElement jpXML = XElement.Load(filename);

                foreach (var vocal in jpXML.Elements("vocal"))
                {
                    string lyric = vocal.Attribute("lyric").Value;
                    string lyricTrimmed = lyric.EndsWith("+") || lyric.EndsWith("-") ? lyric.Substring(0, lyric.Length - 1) : lyric;

                    FontGenerator.AddGlyphsFromWord(lyricTrimmed);
                }

                // Space is always included in official Japanese lyrics
                FontGenerator.AddGlyph(" ");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading the xml file.\n" + ex.Message);
                return false;
            }
        }

        public static bool LoadTextFile(string filename)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                char[] stripChars = new[] { '\t', '\r', '\n' };

                using (StreamReader reader = new StreamReader(filename))
                {
                    while (true)
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
                Console.WriteLine("Error reading the txt file.\n" + ex.Message);
                return false;
            }

            return true;
        }

        private static bool SaveFont()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                string tempPngFile = Path.Combine(tempPath, "temptexture.png");
                string tempDdsFile = Path.Combine(tempPath, "temptexture.dds");

                const string filename = "lyrics.dds";
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
            catch (Exception ex)
            {
                Console.WriteLine("Error saving texture file.\n" + ex.Message);
                return false;
            }

            return true;
        }

        private static void CreateCanvas()
        {
            FontCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                Width = TextureWidth,
                Height = TextureHeight,
                Effect = new DropShadowEffect()
                {
                    Color = Colors.Blue,
                    RenderingBias = RenderingBias.Quality,
                    BlurRadius = Defaults.ShadowBlurRadius,
                    Direction = Defaults.ShadowDirection,
                    ShadowDepth = Defaults.ShadowDepth,
                    Opacity = Defaults.ShadowOpacity
                }
            };

            FontCanvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            FontCanvas.Arrange(new Rect(FontCanvas.DesiredSize));
        }

        [STAThread]
        static int Main(string[] args)
        {
            string filename;
            if (args.Length == 1)
            {
                filename = args[0];
                if (!File.Exists(filename))
                {
                    Console.WriteLine("File not found: " + filename);
                    return 1;
                }
            }
            else
            {
                Console.WriteLine("Give a filename as a command line argument.");
                return 1;
            }

            CreateCanvas();

            FontGenerator = new RS2014FontGenerator(FontCanvas, new Canvas());

            if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && !LoadXMLFile(filename))
            {
                return 1;
            }

            if (filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && !LoadTextFile(filename))
            {
                return 1;
            }

            FontGenerator.SetFont(new TextBlock().FontFamily, FontWeights.Bold, Defaults.FontSize, Defaults.KanjiFontSize);

            switch (FontGenerator.TryGenerateFont())
            {
                case GenerationResult.Success:
                    FontCanvas.Width = TextureWidth = FontGenerator.TextureWidth;
                    FontCanvas.Height = TextureHeight = FontGenerator.TextureHeight;
                    FontCanvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    FontCanvas.Arrange(new Rect(FontCanvas.DesiredSize));

                    Console.WriteLine("Font generated.");
                    break;

                case GenerationResult.DidNotFitIntoMaxSize:
                    Console.WriteLine("The glyphs did not fit into the maximum size texture.");
                    return 1;

                case GenerationResult.UserCanceled:
                    break;

                default:
                    Debug.Print("Unexpected generation result!");
                    break;
            }

            if (!SaveFont())
                return 1;

            return 0;
        }
    }
}
