﻿using Microsoft.Win32;
using OnePaceCore.Utils;
using OnePaceMuxer.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OnePaceMuxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileInfo chapterFile;
        private FileInfo videoFile;
        private FileInfo audioFile;
        private FileInfo subtitlesFile;
        private string subtitlesAppendFile;

        public MainWindow()
        {
            InitializeComponent();
            TextBox_Languages.Text = Settings.Default.Languages;
        }

        private void TextBox_PreviewDragOverVideoFile(object sender, DragEventArgs e)
        {
            string dropFile = GetDropFile(e);
            string fileExtension = Path.GetExtension(dropFile);
            e.Handled = dropFile != null && (fileExtension == ".mp4" || fileExtension == ".mkv");
        }

        private void TextBox_PreviewDropVideoFile(object sender, DragEventArgs e)
        {
            videoFile = new FileInfo(GetDropFile(e));
            UpdateUIState();
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
            subtitlesFile = new FileInfo(GetDropFile(e));
            UpdateUIState();
        }

        private void TextBox_PreviewDropChapterFile(object sender, DragEventArgs e)
        {
            chapterFile = new FileInfo(GetDropFile(e));
            UpdateUIState();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] languages = TextBox_Languages.Text.Split(',').Select(i => i.Trim()).ToArray();
            Task.Run(() =>
            {
                try
                {
                    IList<FileInfo> attachments = null;
                    if (subtitlesFile.Extension == ".ssa" || subtitlesFile.Extension == ".ass")
                    {
                        attachments = GetSSAFontLocations(subtitlesFile);
                    }
                    string outputFile = videoFile.FullName + ".muxed.mkv";
                    string[] subtitleAppendices = new string[] { subtitlesAppendFile };
                    if (string.IsNullOrWhiteSpace(subtitlesAppendFile))
                    {
                        subtitleAppendices = null;
                    }
                    MKVToolNixUtils.Multiplex(videoFile, audioFile, subtitlesFile, languages, attachments, chapterFile, subtitleAppendices, outputFile);
                }
                catch (Exception exception)
                {
                    Dispatcher.Invoke(() => HandleException(exception));
                }
            });
        }

        private void HandleException(Exception exception)
        {
            string message = GetExceptionMessage(exception);
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private string GetExceptionMessage(Exception exception)
        {
            if (exception.InnerException != null)
            {
                return GetExceptionMessage(exception.InnerException);
            }
            else
            {
                return exception.Message;
            }
        }

        private class Font
        {
            public GlyphTypeface glyph;
            public string familyName;
            public string faceName;
            public string familyFaceName;
            public FileInfo path;

            public Font(GlyphTypeface glyph, string familyName, string faceName, FileInfo path)
            {
                this.glyph = glyph;
                this.familyName = familyName;
                this.faceName = faceName;
                familyFaceName = string.Join(" ", familyName, faceName);
                this.path = path;
            }
        }

        /// <summary>
        /// Read used fonts in SSA file, check that they exist on the system (otherwise throw exception), and return used fonts' system paths
        /// </summary>
        /// <param name="ssaFile"></param>
        private IList<FileInfo> GetSSAFontLocations(FileInfo ssaFile)
        {
            var fontDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var fontFiles = Directory.GetFiles(fontDirectory).Where(i => !i.EndsWith(".fon"));
            var fontUris = fontFiles.Select(i => new Uri(i)).ToList();
            var fontNames = new List<Font>();
            foreach (Uri uri in fontUris)
            {
                try
                {
                    GlyphTypeface glyph = new GlyphTypeface(uri);
                    string familyName = glyph.FamilyNames[CultureInfo.GetCultureInfo("en-US")];
                    string faceName = glyph.FaceNames[CultureInfo.GetCultureInfo("en-US")];
                    fontNames.Add(new Font(glyph, familyName, faceName, new FileInfo(glyph.FontUri.AbsolutePath)));
                }
                catch { }
            }
            IList<FileInfo> fontLocations = new List<FileInfo>();
            var v4StylesRegex = @"(?:Style: [\w +-]+,)([\w -]+)(?=,)";
            var fnRegex = @"(?<=\\fn)[\w -]+";
            string ssaText = File.ReadAllText(ssaFile.FullName);
            var v4fonts = Regex.Matches(ssaText, v4StylesRegex).Cast<Match>().Select(i => i.Groups[1].Value);
            var fnfonts = Regex.Matches(ssaText, fnRegex).Cast<Match>().Select(i => i.Value);
            
            var fonts = v4fonts.Concat(fnfonts).Distinct();
            foreach (var font in fonts)
            {
                Font systemFont = fontNames.FirstOrDefault(i => i.familyName.ToLower() == font.ToLower() || i.familyFaceName.ToLower() == font.ToLower());
                if (systemFont == null)
                {
                    throw new Exception(string.Format("Missing font '{0}'. Please install it to your system to continue.", font));
                }
                FileInfo systemFontPath = new FileInfo(systemFont.path.ToString().Replace("%20", " "));
                if (!fontLocations.Any(i => i.ToString() == systemFontPath.ToString()))
                {
                    fontLocations.Add(systemFontPath);
                }
            }
            return fontLocations;
        }

        private void TextBox_TextChangedLanguages(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!TextBox_Languages.IsLoaded)
            {
                return;
            }
            Settings.Default.Languages = TextBox_Languages.Text;
            Settings.Default.Save();
        }

        private void TextBox_PreviewMouseLeftButtonUpVideoFile(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Video files (*.mp4;*.mkv)|*.mp4;*.mkv" };
            if (openFileDialog.ShowDialog() == true)
            {
                videoFile = new FileInfo(openFileDialog.FileName);
                UpdateUIState();
            }
        }

        private void TextBox_PreviewMouseLeftButtonUpSubtitleFile(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Subtitle files (*.srt;*.ssa;*.ass)|*.srt;*.ssa;*.ass" };
            if (openFileDialog.ShowDialog() == true)
            {
                subtitlesFile = new FileInfo(openFileDialog.FileName);
                UpdateUIState();
            }
        }

        private void TextBox_PreviewMouseLeftButtonUpChapterFile(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Chapter files (*.xml)|*.xml" };
            if (openFileDialog.ShowDialog() == true)
            {
                chapterFile = new FileInfo(openFileDialog.FileName);
                UpdateUIState();
            }
        }

        private void TextBox_PreviewDragOverChapterFile(object sender, DragEventArgs e)
        {
            string dropFile = GetDropFile(e);
            e.Handled = dropFile != null && Path.GetExtension(dropFile) == ".xml";
        }

        private void TextBox_PreviewDragOverSubtitleFile(object sender, DragEventArgs e)
        {
            string dropFile = GetDropFile(e);
            e.Handled = dropFile != null && (Path.GetExtension(dropFile) == ".srt" || Path.GetExtension(dropFile) == ".ssa" || (Path.GetExtension(dropFile) == ".ass"));
        }

        private void UpdateUIState()
        {
            TextBox_ChapterFile.Text = chapterFile?.FullName ?? "Browse...";
            TextBox_SubtitlesFile.Text = subtitlesFile?.FullName ?? "Browse...";
            TextBox_SubtitlesAppendFile.Text = subtitlesAppendFile ?? "Browse...";
            TextBox_VideoFile.Text = videoFile?.FullName ?? "Browse...";
            TextBox_AudioFile.Text = audioFile?.FullName ?? "Browse...";
            Button_Mux.IsEnabled = chapterFile != null && subtitlesFile != null && videoFile != null;
        }

        #region Subtitle append
        private void TextBox_PreviewDropSubtitlesAppend(object sender, DragEventArgs e)
        {
            subtitlesAppendFile = GetDropFile(e);
            UpdateUIState();
        }

        private void TextBox_PreviewMouseLeftButtonUpSubtitleAppendFile(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Subtitle files (*.srt;*.ssa;*.ass)|*.srt;*.ssa;*.ass" };
            if (openFileDialog.ShowDialog() == true)
            {
                subtitlesAppendFile = openFileDialog.FileName;
                UpdateUIState();
            }
        }

        private void TextBox_PreviewDragOverSubtitleAppendFile(object sender, DragEventArgs e)
        {
            string dropFile = GetDropFile(e);
            e.Handled = dropFile != null && (Path.GetExtension(dropFile) == ".srt" || Path.GetExtension(dropFile) == ".ssa" || (Path.GetExtension(dropFile) == ".ass"));
        }
        #endregion

        private void TextBox_PreviewMouseLeftButtonUpAudioFile(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Audio files (*.mp3;*.flac;*.aac;*.ac3;*.m4a;*.wav;*.ogg)|*.mp3;*.flac;*.aac;*.ac3;*.m4a;*.wav;*.ogg" };
            if (openFileDialog.ShowDialog() == true)
            {
                audioFile = new FileInfo(openFileDialog.FileName);
                UpdateUIState();
            }
        }

        private void TextBox_PreviewDropAudioFile(object sender, DragEventArgs e)
        {
            audioFile = new FileInfo(GetDropFile(e));
            UpdateUIState();
        }

        string[] audioFormats = new string[]
        {
            ".mp3",
            ".flac",
            ".aac",
            ".ac3",
            ".m4a",
            ".wav",
            ".ogg"
        };
        private void TextBox_PreviewDragOverAudioFile(object sender, DragEventArgs e)
        {
            string dropFile = GetDropFile(e);
            string fileExtension = Path.GetExtension(dropFile);
            e.Handled = dropFile != null && audioFormats.Contains(fileExtension);
        }
    }
}
