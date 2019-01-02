using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OnePaceMuxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void TextBox_PreviewDropVideoFile(object sender, DragEventArgs e)
        {
        }

        private string GetDropFile(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            }
            else
            {
                return null;
            }
        }

        private void TextBox_PreviewDropSubtitles(object sender, DragEventArgs e)
        {

        }

        private void TextBox_PreviewDropChapterFile(object sender, DragEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Read used fonts in SSA file, check that they exist on the system (otherwise throw exception), and put them in the same directory as the SSA file.
        /// </summary>
        /// <param name="ssaFile"></param>
        private void ExtractSSAFonts(string ssaFile)
        {
            using (var stream = new FileStream(ssaFile, FileMode.Open))
            using (var fileReader = new StreamReader(stream))
            {
                while (!fileReader.EndOfStream)
                {
                    string line = fileReader.ReadLine();
                    if (line == "[V4+ Styles]")
                    {
                        do
                        {
                            line = fileReader.ReadLine();
                            if (line.StartsWith("Style: "))
                            {
                                string font = line.Substring(7).Split(',')[1];
                                var systemFonts = new InstalledFontCollection().Families;
                                System.Drawing.FontFamily systemFont = systemFonts.FirstOrDefault(i => i.Name == font);
                                if (systemFont == null)
                                {
                                    throw new Exception(string.Format("Missing font '{0}'. Please install it to your system to continue.", font));
                                }
                                GlyphTypeface
                            }
                        } while (!fileReader.EndOfStream && line != "");
                        break;
                    }
                }
            }
        }
    }
}
