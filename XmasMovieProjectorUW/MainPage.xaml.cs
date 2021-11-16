using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XmasMovieProjectorUW
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public Dictionary<string, string> ConfigValues;
        public Dictionary<string, string> SongStatus;
        public DateTime FileLastChecked;
        public DateTime FileNextCheck;

        public DispatcherTimer UiTimer;
        //public System.Timers.Timer Timer1;

        public MediaPlayer mediaPlayer;
        public bool isPlaying;
       
        public MainPage()
        {
            this.InitializeComponent();

            LoadConfig();

            mediaPlayer = new MediaPlayer
            {
                IsLoopingEnabled = true,
                Volume = 0,
                Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/" + ConfigValues["ActionVideo"]))
            };
            isPlaying = false;
            _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            EnableFileWatcher();
            PlayVideo();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            _ = view.TryEnterFullScreenMode();
        }

        private void NormalButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            _ = view.TryResizeView(new Size(1024, 768));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            //Timer1.Stop();
            //Timer1.Dispose();

            UiTimer.Stop();
            Application.Current.Exit();
        }

        private void LoadConfig()
        {
            ConfigValues = new Dictionary<string, string>();
            var config = File.ReadAllText("Assets/Config.txt");
            var configLines = config.Split("\n");

            foreach (var item in from line in configLines
                                 let item = line.Split("|")
                                 select item)
                ConfigValues.Add(item[0], item[1].Replace("\r", ""));
        }

        private void EnableFileWatcher()
        {
            FileLastChecked = DateTime.Now;
            SongStatus = new Dictionary<string, string>();

            UiTimer = new DispatcherTimer();
            UiTimer.Tick += DispatcherTimer_Tick;

            //Timer1 = new System.Timers.Timer();
            //Timer1.Elapsed += Timer1_Elapsed;

            CheckFile();
        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            UiTimer.Stop();
            CheckFile();
        }

        //private void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    Timer1.Stop();
        //    CheckFile();
        //}

        private void CheckFile()
        {
            var folder = StorageFolder.GetFolderFromPathAsync(ConfigValues["StatusFolder"]).GetAwaiter().GetResult();
            var file = folder.GetFileAsync(ConfigValues["StatusFile"]).GetAwaiter().GetResult();
            var statusFile = FileIO.ReadTextAsync(file).GetAwaiter().GetResult();

            // Load the Status File
            var statusFileContents = statusFile.Split("\n");

            SongStatus.Clear();
            foreach (var item in from line in statusFileContents
                                 let item = line.Split("|")
                                 select item)
                SongStatus.Add(item[0], item[1].Replace("\r", ""));

            int interval = 10000;
            var intSuccess = int.TryParse(SongStatus["Interval"], out interval);
            if (!intSuccess)
                interval = 10000;

            FileNextCheck = DateTime.Now.AddMilliseconds(interval);

            //Timer1.Interval = interval;
            //Timer1.Start();
            UiTimer.Interval = new TimeSpan(0,0,0,0, interval);
            UiTimer.Start();

            if (SongStatus["SequenceType"] == "2" && isPlaying)
            {
                StopVideo();
            } else if (SongStatus["SequenceType"] == "1" && !isPlaying)
            {
                PlayVideo();
            } else if (SongStatus["SequenceType"] == "0" && isPlaying)
            {
                StopVideo();
            }

            //// Load current day showtimes
            //var currentDay = FileLastChecked.ToString("ddd");
            //var showtimeSetting = ConfigValues[currentDay];
            //var showtimes = showtimeSetting.Split(",");
        }

        private void PlayVideo()
        {
            _mediaPlayerElement.Visibility = Visibility.Visible;
            mediaPlayer.Play();
            isPlaying = true;
        }

        private void StopVideo()
        {
            _mediaPlayerElement.Visibility = Visibility.Collapsed;
            mediaPlayer.Pause();
            isPlaying = false;
        }
    }
}
