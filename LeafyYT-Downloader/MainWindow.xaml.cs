// -----------------------------------------------------------
// This program is private software, based on C# source code.
// To sell or change credits of this software is forbidden,
// except if someone approves it from the LeafyCoding INC. team.
// -----------------------------------------------------------
// Copyrights (c) 2016 LeafyYT-Downloader INC. All rights reserved.
// -----------------------------------------------------------

#region

#region

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Google.Apis.YouTube.v3;
using LeafyYT_Downloader.Classes;
using LeafyYT_Downloader.Properties;
using MaterialDesignThemes.Wpf;
using NAudio.Lame;
using NAudio.Wave;
using Ookii.Dialogs.Wpf;
using YoutubeExtractor;

#endregion

// ReSharper disable ImplicitlyCapturedClosure

#endregion

namespace LeafyYT_Downloader
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal sealed partial class MainWindow : INotifyPropertyChanged
    {
        private Visibility _loading; // Progressbar shown when processing URL.

        private DelegateCommand _urlCommand; // Ctrl-V shortcut handler.

        private int errNum; // Number of errors in a playlist handling.

        private int successNum; // Number of successfully processed videos.

        public YouTubeService youtubeService = new YouTubeService(); // Duh...

        public MainWindow()
        {
            InitializeComponent();
        }

        public ICommand UrlCommand => _urlCommand ?? (_urlCommand = new DelegateCommand(HandleClipboard));

        public ObservableCollection<SelectableViewModel> Items { get; set; }

        public Visibility Loading
        {
            get { return _loading; }
            set
            {
                _loading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Loading"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void HandleClipboard() => HandleURL(Clipboard.GetText());

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loading = Visibility.Collapsed;
            Items = new ObservableCollection<SelectableViewModel>();

            // Ugly binding hack
            Dispatcher.Invoke(() =>
            {
                DataGrid.ItemsSource = null;
                DataGrid.ItemsSource = Items;
                DataGrid.Items.Refresh();
            });

            if (string.IsNullOrEmpty(Settings.Default.dlDir)) // First run download directory setting. TODO allow changes.
            {
                var dialog = new VistaFolderBrowserDialog
                {
                    Description = @"Choose a folder to save files to:",
                    UseDescriptionForTitle = true
                };
                dialog.ShowDialog();
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    if (
                        MessageBox.Show(
                            "You cancelled the operation, bye bye :(",
                            "LeafyYT", MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        Environment.Exit(0);
                    }
                }
                Settings.Default.dlDir = dialog.SelectedPath;
                Settings.Default.Save();
            }

            Config.Init(); // Self explanatory.
        }

        private void AddVideos(string name, string id)
        {
            Dispatcher.Invoke(() =>
            {
                if (Items.Any(selectableViewModel => selectableViewModel.Name == name)) // Check if URL is already in the list.
                {
                    errNum++;
                    return;
                }
                Items.Add(new SelectableViewModel
                {
                    Name = name,
                    URL = id,
                    Progress = "Idle",
                    IsSelected = true
                });
                successNum++;
            });
        }

        private void RemoveVideo(object sender, RoutedEventArgs e)
        {
            foreach (var selectableViewModel in Items.Where(selectedItem => selectedItem.IsSelected).ToList()) // ToList to prevent errors.
            {
                Items.Remove(selectableViewModel);
            }
        }

        private void NewVideoDialog_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Convert.ToBoolean(eventArgs.Parameter)) // Check if dialog response was true (OK button) or false (CANCEL button)
                return;

            if (!string.IsNullOrWhiteSpace(URLTextBox.Text))
            {
                HandleURL(URLTextBox.Text);
                URLTextBox.Text = string.Empty;
            }
        }

        private void HandleURL(string text)
        {
            if (text.Contains("&")) // This may be supported later on.
            {
                Dialog.ShowMessageDialog("Invalid URL",
                    "Your link contains a '&', please provide either a playlist or a video URL.");
            }
            else
            {
                if (text.StartsWith("https://youtu.be/"))
                {
                    DownloadUrlResolver.TryNormalizeYoutubeUrl(text, out text); // Convert youtu.be links to their full form.
                }
                if (text.Contains("youtube.com/watch?v=")) // URL is a video.
                {
                    new Thread(() => HandleVideo(text)).Start();
                    return;
                }
                if (text.Contains("youtube.com/playlist?list=")) // URL is a playlist.
                {
                    HandlePlaylist(text);
                    return;
                }
                Dialog.ShowMessageDialog("Invalid URL",
                    "You've provided an invalid URL, please check it.");
            }
        }

        private async void HandleVideo(string text)
        {
            Loading = Visibility.Visible;

            // Ugly hack.
            string[] title = {string.Empty};
            foreach (
                var downloadUrl in
                    DownloadUrlResolver.GetDownloadUrls(text).Where(downloadUrl => string.IsNullOrEmpty(title[0])))
            {
                title[0] = downloadUrl.Title;
            }
            await Task.Run(() => AddVideos(title[0], text));

            Loading = Visibility.Collapsed;
        }

        private async void HandlePlaylist(string text)
        {
            Loading = Visibility.Visible;
            var nextPageToken = string.Empty;
            successNum = 0;
            errNum = 0;
            while (nextPageToken != null)
            {
                var playlistListRequest = youtubeService.PlaylistItems.List("snippet"); // Fetch all info.
                playlistListRequest.PlaylistId = text.Split('=')[1]; // For this ID.
                playlistListRequest.MaxResults = 50; // YTv3 API only allows 50 max, but we'll loop with tokens until we're done.
                playlistListRequest.PageToken = nextPageToken;
                var playlistItemsListResponse = await playlistListRequest.ExecuteAsync();

                Loading = Visibility.Visible;
                foreach (var playlistItem in playlistItemsListResponse.Items)
                {
                    await Task.Run(
                        () =>
                            AddVideos(playlistItem.Snippet.Title,
                                "https://youtube.com/watch?v=" + playlistItem.Snippet.ResourceId.VideoId));
                }
                Loading = Visibility.Collapsed;

                nextPageToken = playlistItemsListResponse.NextPageToken;
            }

            Loading = Visibility.Collapsed;
            Dialog.ShowMessageDialog("Success",
                errNum == 0
                    ? $"Added {successNum} videos successfully."
                    : $"Added {successNum} videos successfully. {errNum} videos were duplicate entries.");
        }

        private async void DownloadVideos(object sender, RoutedEventArgs e)
        {
            foreach (var item in Items.Where(selectedItem => selectedItem.IsSelected)
                .Where(selectedItem => selectedItem.Progress == "Idle"))
            {
                await Task.Run(() => ProcessVideo(item));
            }
        }

        private void ProcessVideo(SelectableViewModel selectedItem)
        {
            var videoInfo = DownloadUrlResolver.GetDownloadUrls(selectedItem.URL); // The long part.

            VideoInfo video = null;
            int[] bitrate = {0}; // Another ugly hack.

            foreach (
                var _video in
                    videoInfo.Where(_video => _video.AudioExtension == ".aac")
                        .Where(_video => _video.AudioBitrate > bitrate[0]))
            {
                bitrate[0] = _video.AudioBitrate;
                video = _video;
            }

            if (video != null && video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video); // Some fancy shit I don't know what it does but it's in the example app so I added it here.
            }

            var localFile = Path.Combine($"{Settings.Default.dlDir}/{video?.Title + video?.AudioExtension}");

            var videoDownloader = new VideoDownloader(video, localFile); // Prepare for video downloading.

            Dispatcher.Invoke(() => selectedItem.Progress = ProgressBar.Update(0)); // Initialise our lovely progress bar.

            videoDownloader.DownloadProgressChanged +=
                (s, a) => Dispatcher.Invoke(() => { selectedItem.Progress = ProgressBar.Update(a.ProgressPercentage); });
            try
            {
                videoDownloader.Execute();
            }
            catch (Exception ex)
            {
                if (ex.Message == "The given path's format is not supported.") // Title most probably contains some NTFS-incompatible chars.
                {
                    MessageBox.Show("Invalid filename detected, please choose a new filename in the next window.",
                        "LeafyYT");
                    var dialog = new VistaSaveFileDialog
                    {
                        Title = @"LeafyYT",
                        CheckFileExists = true,
                        ValidateNames = true,
                        InitialDirectory = Settings.Default.dlDir,
                        AddExtension = true,
                        DefaultExt = ".aac"
                    };
                    dialog.ShowDialog();
                    // Should probably send this back into a method instead of creating new ones...
                    if (!string.IsNullOrEmpty(dialog.FileName))
                    {
                        localFile = Path.Combine($"{Settings.Default.dlDir}/{dialog.FileName + video?.AudioExtension}");
                        var videoDownloader2 = new VideoDownloader(video, localFile);

                        Dispatcher.Invoke(() => selectedItem.Progress = ProgressBar.Update(0));

                        videoDownloader2.DownloadProgressChanged +=
                            (s, a) =>
                                Dispatcher.Invoke(
                                    () => { selectedItem.Progress = ProgressBar.Update(a.ProgressPercentage); });

                        try
                        {
                            videoDownloader2.Execute();
                        }
                        catch (Exception ex2)
                        {
                            MessageBox.Show($"EXCEPTION: {ex2.GetType()}: {ex2.Message}");
                            return;
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"EXCEPTION: {ex.GetType()}: {ex.Message}");
                    return;
                }
            }

            Dispatcher.Invoke(() => { selectedItem.Progress = "Converting..."; });

            var oldFile = localFile.Replace("/", "\\"); // LameWriter doesn't seem to like / in filePath.
            var newFile = oldFile.Replace(".aac", "") + ".mp3"; // lol.

            using (var reader = new AudioFileReader(oldFile))
            {
                if (video != null)
                {
                    using (var writer = new LameMP3FileWriter(newFile, reader.WaveFormat, video.AudioBitrate))
                    {
                        reader.CopyTo(writer); // Convert aac to mp3 using some black magic shit.
                    }
                }
            }

            File.Delete(localFile);

            Dispatcher.Invoke(() => { selectedItem.Progress = "Finished"; }); // Tadaaaaaaaaaaaa.
        }
    }
}