using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
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
        public Uri LightsOnMedia;
        public Dictionary<string, string> ConfigValues;
        public Dictionary<string, string> SongStatus;
        public List<string> AnimatedVideos;
        public DateTime FileLastChecked;
        public DateTime FileNextCheck;
        public DispatcherTimer UiTimer;
        public MediaPlayer mediaPlayer;
        public bool isPlaying;
        public string debugInfo;
        public string currentSong;
       
        public MainPage()
        {
            this.InitializeComponent();

            LoadConfig();
            LightsOnMedia = new Uri("ms-appx:///Assets/" + ConfigValues["ActionVideo"]);

            mediaPlayer = new MediaPlayer
            {
                IsLoopingEnabled = true,
                Source = MediaSource.CreateFromUri(LightsOnMedia)
            };
            isPlaying = false;
            _scoreboard.Visibility = Visibility.Collapsed;
            _mediaPlayerElement.Visibility = Visibility.Collapsed;
            _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            EnableFileWatcher();
        }
        public void OpenConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var localFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Windows.System.Launcher.LaunchFolderAsync(localFolder).GetAwaiter().GetResult();
        }

        public void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            _ = view.TryEnterFullScreenMode();
        }

        public void NormalButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            _ = view.TryResizeView(new Size(1024, 768));
        }

        public void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            UiTimer.Stop();
            Application.Current.Exit();
        }

        public void LoadConfig()
        {
            AnimatedVideos = new List<string>();
            ConfigValues = new Dictionary<string, string>();
            var config = File.ReadAllText("Assets/Config.txt");
            var configLines = config.Split("\n");

            foreach (var item in from line in configLines
                                 let item = line.Split("|")
                                 select item)
                ConfigValues.Add(item[0], item[1].Replace("\r", ""));

            AnimatedVideos = ConfigValues["AnimatedVideo"].Split(",").ToList();
        }

        public void EnableFileWatcher()
        {
            FileLastChecked = DateTime.Now;
            SongStatus = new Dictionary<string, string>();

            UiTimer = new DispatcherTimer();
            UiTimer.Tick += DispatcherTimer_Tick;

            CheckFile();
        }

        public void DispatcherTimer_Tick(object sender, object e)
        {
            UiTimer.Stop();
            CheckFile();
        }

        public void CheckFile()
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

            int interval = 1000;
            //var intSuccess = int.TryParse(SongStatus["Interval"], out interval);
            if (!intSuccess)
                interval = 1000;

            FileNextCheck = DateTime.Now.AddMilliseconds(interval);

            UiTimer.Interval = new TimeSpan(0,0,0,0, interval);
            UiTimer.Start();

            if (SongStatus["Song"] != currentSong) 
            {
                currentSong = SongStatus["Song"];

                if (SongStatus["SequenceType"] == "2")
                {
                    PlayAnimatedVideo(SongStatus["Song"]);
                } else if (SongStatus["SequenceType"] == "1")
                {
                    PlayVideo();
                } else if (SongStatus["SequenceType"] == "0")
                {
                    StopVideo();
                }


                if (SongStatus["SequenceType"] == "2")
                {
                    _nextShowTime.Text = "";
                }
                else
                {
                    // Load current day showtimes
                    var currentDay = FileLastChecked.ToString("ddd");
                    var showtimeSetting = ConfigValues[currentDay];
                    var showtimes = showtimeSetting.Split(",");

                    // Iterate through showtimes to find the next showtime
                    var currentTimeTicks = DateTime.Now.Ticks;
                    var currentDate = DateTime.Now.ToShortDateString();
                    var nextShowtime = new DateTime();
                    var foundNextShow = false;
                    foreach (var showtime in showtimes)
                    {
                        var showtimeDate = DateTime.Parse(currentDate + " " + showtime);
                        if (showtimeDate.Ticks > currentTimeTicks)
                        {
                            nextShowtime = showtimeDate;
                            foundNextShow = true;
                        }

                        if (foundNextShow)
                            break;
                    }

                    // Set the Next ShowTime Text
                    var nextShowTimeText = "Tomorrow";

                    if (foundNextShow)
                    {
                        var spanUntilNextShow = nextShowtime - DateTime.Now;
                        var minutesLeft = (int)spanUntilNextShow.TotalMinutes;
                        nextShowTimeText = minutesLeft == 0 ? "Up Next" : minutesLeft + " Minutes";
                    }

                    _nextShowTime.Text = nextShowTimeText;
                }
            }
        }

        public void PlayVideo()
        {
            _scoreboard.Visibility = Visibility.Visible;
            _mediaPlayerElement.Visibility = Visibility.Visible;

            if (!isPlaying)
            {
                _mediaPlayerElement.Source = MediaSource.CreateFromUri(LightsOnMedia);
                mediaPlayer.IsLoopingEnabled = true;
                mediaPlayer.Play();
                isPlaying = true;
            }
        }

        public void PlayAnimatedVideo(string song)
        {
            _scoreboard.Visibility = Visibility.Collapsed;
            mediaPlayer.IsLoopingEnabled = false;

            // Loop through video to get what's playing and compare it to what's in the AnimatedVideo list 
            if (AnimatedVideos.Contains(song))
            {
                _mediaPlayerElement.Visibility = Visibility.Visible;
                _mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/" + song + ".mp4"));
                mediaPlayer.Play();
                isPlaying = true;
            } else
            {
                _mediaPlayerElement.Visibility = Visibility.Collapsed;
                mediaPlayer.Pause();
                isPlaying = false;
            }
        }

        public void StopVideo()
        {
            _scoreboard.Visibility = Visibility.Collapsed;
            _mediaPlayerElement.Visibility = Visibility.Collapsed;
            mediaPlayer.Pause();
            isPlaying = false;
        }
    }
}
