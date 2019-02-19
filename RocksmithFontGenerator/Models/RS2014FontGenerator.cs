using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RocksmithFontGenerator.Models
{
    public enum GenerationResult
    {
        Failure = -1,
        Success,
        UserCanceled,
        ResizeRequired,
        DidNotFitIntoMaxSize
    }

    public sealed class RS2014FontGenerator
    {
        public int GlyphHorizontalMargin { get; set; } = Defaults.GlyphHorizontalMargin;
        public bool ReverseColors { get; set; }
        public bool UseAccurateInnerRects { get; set; } = Defaults.UseAccurateInnerRects;

        public int TextureWidth { get; private set; } = Defaults.TextureWidth;
        public int TextureHeight { get; private set; } = Defaults.TextureHeight;

        public int SpacingAdjustment { get; set; }

        // Use string instead of char because of surrogate pairs and combining characters
        public readonly SortedSet<string> Glyphs = new SortedSet<string>(StringComparer.Ordinal);

        public readonly Dictionary<string, Rect> OuterRects = new Dictionary<string, Rect>();
        public readonly Dictionary<string, Rect> InnerRects = new Dictionary<string, Rect>();

        public readonly FontSettings FontSettings = new FontSettings();

        private readonly Canvas FontCanvas;
        private readonly Canvas RectCanvas;

        private int GlyphHeight = Defaults.GlyphHeight;
        private bool RectErrorMessageShown;

        public RS2014FontGenerator(Canvas fontCanvas, Canvas rectCanvas)
        {
            FontCanvas = fontCanvas;
            RectCanvas = rectCanvas;
        }

        public static int GetValidFontSize(int fontSize)
        {
            if (fontSize < Defaults.MinFontSize)
                return Defaults.MinFontSize;

            if (fontSize > Defaults.MaxFontSize)
                return Defaults.MaxFontSize;

            return fontSize;
        }

        public void SetFont(FontFamily fontFamily, FontWeight fontWeight, int fontSize, int jpFontSize = 0)
        {
            if (jpFontSize == 0)
                jpFontSize = fontSize;

            FontSettings.FontFamily = fontFamily;
            FontSettings.FontWeight = fontWeight;
            FontSettings.FontSize = fontSize;
            FontSettings.KanjiFontSize = jpFontSize;

            // Reset glyph height
            GlyphHeight = Defaults.GlyphHeight;
        }

        public void ResetTextureSize()
        {
            TextureWidth = Defaults.TextureWidth;
            TextureHeight = Defaults.TextureHeight;
        }

        public void ClearGlyphs()
            => Glyphs.Clear();

        public void AddGlyph(string glyph)
            => Glyphs.Add(glyph);

        /// <summary>
        /// Returns a string that contains the character and all the combining characters after it.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="index">Index to a character that is followed by a combining character.</param>
        private static string GetCombinedCharacter(string word, ref int index)
        {
            string combined = word.Substring(index, 1);
            UnicodeCategory nextCharCategory;

            do
            {
                index++;
                combined += word.Substring(index, 1);
                if (index + 1 == word.Length)
                    break;

                nextCharCategory = char.GetUnicodeCategory(word[index + 1]);
            }
            while (nextCharCategory.IsCombiningCategory());

            return combined;
        }

        public void AddGlyphsFromWord(string word)
        {
            for (int i = 0; i < word.Length; i++)
            {
                // Check if the next character is a combining character
                if (i + 1 < word.Length)
                {
                    UnicodeCategory nextCharCategory = char.GetUnicodeCategory(word[i + 1]);

                    if (nextCharCategory.IsCombiningCategory())
                    {
                        string combinedCharacter = GetCombinedCharacter(word, ref i);

                        // Not very likely but check anyway
                        if (Encoding.UTF8.GetByteCount(combinedCharacter) > 12)
                        {
                            MessageBox.Show(
                                string.Format(Properties.Resources.Error_AddGlyphs_TooLongCombination, combinedCharacter),
                                Properties.Resources.Error,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                            continue;
                        }

                        Glyphs.Add(combinedCharacter);
                        continue;
                    }
                }

                char currentCharacter = word[i];

                switch (char.GetUnicodeCategory(currentCharacter))
                {
                    // Skip format characters
                    case UnicodeCategory.Format:
                        Debug.Print("Format character skipped");
                        break;

                    // Skip modifier symbols
                    case UnicodeCategory.ModifierSymbol:
                        Debug.Print("Modifier symbol skipped");
                        break;

                    // Combine surrogate pairs
                    case UnicodeCategory.Surrogate:
                        string surrogatePair = currentCharacter.ToString();

                        if (++i == word.Length)
                            throw new InvalidOperationException(string.Format(Properties.Resources.Error_AddGlyphs_InvalidSurrogatePair, word));

                        surrogatePair += word.Substring(i, 1);

                        Glyphs.Add(surrogatePair);
                        break;

                    // All other categories
                    default:
                        Glyphs.Add(currentCharacter.ToString());
                        break;
                }
            }
        }

        public GenerationResult TryGenerateFont(bool firstTry = true)
        {
            if (firstTry)
                RectErrorMessageShown = false;

            var result = GenerateFont();

            if (result == GenerationResult.ResizeRequired)
            {
                if (TextureHeight == 512) // Resize to 512x1024
                {
                    TextureHeight = 1024;
                }
                else if (TextureWidth == 512) // Resize to 1024x1024
                {
                    TextureWidth = 1024;
                }
                else
                {
                    // At max size (1024x1024), not resizing anymore
                    return GenerationResult.DidNotFitIntoMaxSize;
                }

                // Retry after resizing
                return TryGenerateFont(firstTry: false);
            }

            return result;
        }

        private GenerationResult GenerateFont()
        {
            RectCanvas.Children.Clear();
            FontCanvas.Children.Clear();
            OuterRects.Clear();
            InnerRects.Clear();

            double x = 0.0, y = 0.0;

            if (!TestGlyphSize())
                return GenerationResult.UserCanceled;

            foreach (string glyph in Glyphs)
            {
                Grid glyphGrid = new Grid
                {
                    Height = GlyphHeight,
                    Background = Brushes.Transparent
                };

                int fontSize = FontSettings.FontSize;
                if (glyph[0].IsKanaOrKanji())
                {
                    fontSize = FontSettings.KanjiFontSize;
                }

                Run glyphRun = new Run
                {
                    Text = glyph,
                    FontFamily = FontSettings.FontFamily,
                    FontSize = fontSize,
                    FontWeight = FontSettings.FontWeight,
                    Foreground = ReverseColors ? Brushes.Blue : Brushes.Red,
                    Background = Brushes.Transparent
                };

                TextBlock txtBlock = new TextBlock(glyphRun)
                {
                    Margin = new Thickness(GlyphHorizontalMargin, 0, GlyphHorizontalMargin, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                glyphGrid.Children.Add(txtBlock);

                // Measure size of glyph grid and text
                glyphGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                glyphGrid.Arrange(new Rect(glyphGrid.DesiredSize));

                // Check if we need to move to the next row
                if (Math.Ceiling(x + glyphGrid.ActualWidth) > TextureWidth)
                {
                    x = 0.0;
                    y += GlyphHeight;
                }

                if (y + GlyphHeight >= TextureHeight)
                {
                    return GenerationResult.ResizeRequired;
                }

                glyphGrid.SetValue(Canvas.LeftProperty, x);
                glyphGrid.SetValue(Canvas.TopProperty, y);

                // Get bounding rectangles
                AddOuterRect(glyph, glyphGrid, x, y);
                AddInnerRect(glyph, glyphGrid, txtBlock, x, y);

                x += glyphGrid.ActualWidth;

                FontCanvas.Children.Add(glyphGrid);
            }

            // Official Japanese textures have same outer rect as inner rect for space
            if(OuterRects.ContainsKey(" "))
                OuterRects[" "] = InnerRects[" "];

            return GenerationResult.Success;
        }

        /// <summary>
        /// Tests whether the glyph height of the capital letter X using the selected font is larger than the default glyph height.
        /// </summary>
        /// <returns>Returns false if the user cancels the operation.</returns>
        private bool TestGlyphSize()
        {
            Grid testGrid = new Grid();
            int fontSize = Math.Max(FontSettings.FontSize, FontSettings.KanjiFontSize);
            Run testRun = new Run
            {
                Text = "X",
                FontFamily = FontSettings.FontFamily,
                FontSize = fontSize,
                FontWeight = FontSettings.FontWeight,
                Foreground = Brushes.Red
            };

            TextBlock testText = new TextBlock(testRun)
            {
                Margin = new Thickness(GlyphHorizontalMargin, 0, GlyphHorizontalMargin, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            testGrid.Children.Add(testText);
            testGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            testGrid.Arrange(new Rect(testGrid.DesiredSize));

            BitmapSource glyphBitmap = BitmapFunctions.CaptureScreen(testGrid, 96.0, 96.0);
            Rect innerRect = DetectInnerRectFromPixels(glyphBitmap, new Rect(0.0, 0.0, testGrid.ActualWidth, testGrid.ActualHeight));

            //if (testGrid.ActualHeight > GlyphHeight)
            if (innerRect.Height > GlyphHeight)
            {
                int newGlyphHeight = (int)Math.Ceiling(innerRect.Height);

                string message = Properties.Resources.Error_Generation_GlyphSizeTooLargeMessage +
                    Environment.NewLine + Environment.NewLine +
                    string.Format(Properties.Resources.Error_Generation_GlyphSizeConfirmation, newGlyphHeight);

                var answer = MessageBox.Show(
                    message,
                    Properties.Resources.Error_Generation_GlyphSizeTooLargeTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (answer == MessageBoxResult.No)
                    return false;
                else
                    GlyphHeight = newGlyphHeight;
            }

            return true;
        }

        private void AddOuterRect(string glyph, Grid glyphGrid, double x, double y)
        {
            Rect outer = new Rect(x, y, glyphGrid.ActualWidth, glyphGrid.ActualHeight);
            OuterRects.Add(glyph, outer);

            CreateOuterRectangleForCanvas(glyph, outer);
        }

        private void AddInnerRect(string glyph, Grid glyphGrid, TextBlock innerText, double x, double y)
        {
            Rect innerRect;
            BitmapSource glyphBitmap = BitmapFunctions.CaptureScreen(glyphGrid, 96.0, 96.0);

            if (string.IsNullOrWhiteSpace(glyph) || glyphBitmap == null || !UseAccurateInnerRects)
            {
                double innerWidth = innerText.ActualWidth + SpacingAdjustment;
                if (innerWidth < 0)
                    innerWidth = 0;

                innerRect = CreateInnerRect(
                    x: x,
                    y: y,
                    outerHeight: OuterRects[glyph].Height,
                    innerWidth: innerWidth,
                    innerHeight: innerText.ActualHeight);
            }
            else
            {
                innerRect = DetectInnerRectFromPixels(glyphBitmap, OuterRects[glyph]);

                // Create new outer rect to keep horizontal margin constant
                OuterRects[glyph] = new Rect(
                    innerRect.X - GlyphHorizontalMargin,
                    OuterRects[glyph].Y,
                    innerRect.Width + (GlyphHorizontalMargin * 2),
                    OuterRects[glyph].Height);

                RectCanvas.Children.RemoveAt(RectCanvas.Children.Count - 1);
                CreateOuterRectangleForCanvas(glyph, OuterRects[glyph]);
            }

            /*if(inner.Height > DefaultGlyphHeight)
            {
                var answer = MessageBox.Show(
                    "The glyph height for the selected font is larger than the default (52 pixels).\n\n",
                    "Glyph Size Too Large");
            }*/

            Brush rectBrush = Brushes.DarkViolet;
            Rect outer = OuterRects[glyph];

            // Check if the inner rect is larger than the outer
            if (innerRect.Top < outer.Top
                || innerRect.Left < outer.Left
                || innerRect.Bottom > outer.Bottom
                || innerRect.Right > outer.Right)
            {
                if (!RectErrorMessageShown)
                {
                    MessageBox.Show(Properties.Resources.Error_Generation_OverlappingBoundingRects, Properties.Resources.Error_Warning);
                    RectErrorMessageShown = true;
                }

                // Resize the width of the inner rect to fit inside the outer
                if (innerRect.Right > outer.Right)
                {
                    innerRect = new Rect(
                        innerRect.X,
                        innerRect.Y,
                        innerRect.Width - (outer.Right - innerRect.Right),
                        innerRect.Height);
                }

                rectBrush = Brushes.Red;
            }

            InnerRects.Add(glyph, innerRect);

            CreateInnerRectangleForCanvas(innerRect, rectBrush);
        }

        private Rect CreateInnerRect(double x, double y, double outerHeight, double innerWidth, double innerHeight)
        {
            return new Rect(x + GlyphHorizontalMargin,
                            y + ((outerHeight - innerHeight) / 2),
                            innerWidth,
                            innerHeight);
        }

        private Rect DetectInnerRectFromPixels(BitmapSource bitmap, Rect outerRect)
        {
            int stride = bitmap.PixelWidth * 4;
            int size = bitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            bitmap.CopyPixels(pixels, stride, 0);

            int lowX = -1;
            int lowY = int.MaxValue;
            int highX = -1;
            int highY = -1;

            for (int x = 0; x < bitmap.PixelWidth; x++)
            {
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    int index = (y * stride) + (4 * x);
                    byte blue = pixels[index];
                    byte green = pixels[index + 1];
                    byte red = pixels[index + 2];
                    //byte alpha = pixels[index + 3];

                    if (red != 0 || blue != 0)
                    {
                        if (lowX == -1)
                            lowX = x;

                        if (y < lowY)
                            lowY = y;

                        highX = x;

                        if (y > highY)
                            highY = y;
                    }
                    if (green != 0)
                    {
                        MessageBox.Show("Green!?");
                        return Rect.Empty;
                    }
                }
            }

            // Some glyphs may not be rendered at all in the selected font
            if (lowX == -1)
            {
                return outerRect;
            }

            Point upperLeft = new Point(outerRect.X + lowX, outerRect.Y + lowY);
            Point lowerRight = new Point(outerRect.X + highX + SpacingAdjustment + 1, outerRect.Y + highY + 2); // TODO: Why is +1/+2 needed?

            return new Rect(upperLeft, lowerRight);
        }

        private void CreateOuterRectangleForCanvas(string glyph, Rect outer)
        {
            Run tooltip = new Run
            {
                Text = glyph,
                FontSize = 40
            };
            Rectangle outerRectangle = new Rectangle
            {
                Stroke = Brushes.Black,
                Height = outer.Height,
                Width = outer.Width,
                ToolTip = tooltip,
                Fill = Brushes.Transparent
            };
            outerRectangle.SetValue(Canvas.LeftProperty, outer.Left);
            outerRectangle.SetValue(Canvas.TopProperty, outer.Top);

            RectCanvas.Children.Add(outerRectangle);
        }

        private void CreateInnerRectangleForCanvas(Rect inner, Brush brush)
        {
            Rectangle innerRectangle = new Rectangle
            {
                Stroke = brush,
                Height = inner.Height,
                Width = inner.Width,
                IsHitTestVisible = false
            };
            innerRectangle.SetValue(Canvas.LeftProperty, inner.Left);
            innerRectangle.SetValue(Canvas.TopProperty, inner.Top);

            RectCanvas.Children.Add(innerRectangle);
        }
    }
}
