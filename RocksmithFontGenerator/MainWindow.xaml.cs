using MahApps.Metro.Controls;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace RocksmithFontGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private const string SavedStateFileName = "state.xml";

        private readonly MainWindowViewModel ViewModel;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = ViewModel = new MainWindowViewModel(fontCanvas, rectCanvas, this.FontFamily);

            // Adjust tile brush when texture size changes
            this.WhenAnyValue(
                x => x.ViewModel.TextureWidth,
                x => x.ViewModel.TextureHeight)
                .DistinctUntilChanged()
                .ObserveOnDispatcher()
                .Subscribe(_ => AdjustTileBrush());

            // Change the cursor when generating font
            ViewModel.GenerateFont
                .IsExecuting
                .ObserveOnDispatcher()
                .Subscribe(generating => Cursor = generating ? Cursors.Wait : Cursors.Arrow);
        }

        // Keeps background tile pattern the same size if texture is resized
        private void AdjustTileBrush()
        {
            bgTileBrush.Viewport = new Rect(
                0.0,
                0.0,
                (double)Defaults.TextureWidth / ViewModel.TextureWidth * 0.05,
                (double)Defaults.TextureHeight / ViewModel.TextureHeight * 0.05);
        }

        #region Drag & Drop

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string filename = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

                if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    ViewModel.ReadVocalsXML(filename);

                    await ViewModel.GenerateFontIfReady();
                }
                else if(filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    ViewModel.ReadTextFile(filename);

                    await ViewModel.GenerateFontIfReady();
                }
            }
        }

        #endregion

        // Check if a filename was given as a command line argument
        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 2)
            {
                string filename = args[1];
                if (File.Exists(filename))
                {
                    if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        ViewModel.ReadVocalsXML(filename);
                    else if (filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        ViewModel.ReadTextFile(filename);

                    await ViewModel.GenerateFontIfReady();
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.SaveProgramState();

            base.OnClosed(e);
        }

        private void Link_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(sender is TextBlock txt)
                Process.Start(txt.Tag as string);
        }

        /*private void ReverseColorsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox)?.IsChecked == true)
                dropShadow.Color = Colors.Red;
            else
                dropShadow.Color = Colors.Blue;

            if (FontGenerator?.CanGenerate == true)
                GenerateFontButton_Click(null, null);
        }*/
    }
}
