using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FontGeneratorCLI
{
    public static class BitmapFunctions
    {
        /// <summary>
        /// Saves visual as a PNG image.
        /// </summary>
        /// <param name="visual">Visual to save.</param>
        /// <param name="filePath">Path of the target PNG file.</param>
        /// <param name="pixelWidth">Width of target bitmap.</param>
        /// <param name="pixelHeight">Height of target bitmap.</param>
        public static void SaveImage(Visual visual, string filePath, int pixelWidth, int pixelHeight)
        {
            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            bitmap.Render(visual);
            //BitmapSource bitmap = CaptureScreen(visual, 96.0, 96.0);

            var image = new PngBitmapEncoder();
            image.Frames.Add(BitmapFrame.Create(bitmap));
            using (Stream fs = File.Create(filePath))
            {
                image.Save(fs);
            }
        }

        /// <summary>
        /// Creates a BitmapSource out of a visual.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dpiX"></param>
        /// <param name="dpiY"></param>
        /// <returns></returns>
        public static BitmapSource CaptureScreen(Visual target, double dpiX, double dpiY)
        {
            if (target == null)
                return null;

            //Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Rect bounds = new Rect(0, 0,
                Math.Round((target as FrameworkElement).ActualWidth, 0, MidpointRounding.AwayFromZero),
                Math.Round((target as FrameworkElement).ActualHeight, 0, MidpointRounding.AwayFromZero));

            if (bounds.Width == 0 || bounds.Height == 0)
                return null;

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)(bounds.Width * dpiX / 96.0),
                                                            (int)(bounds.Height * dpiY / 96.0),
                                                            dpiX,
                                                            dpiY,
                                                            PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(target);
                ctx.DrawRectangle(vb, null, new Rect(new Point(0, 0), bounds.Size));
            }
            rtb.Render(dv);
            return rtb;
        }
    }
}
