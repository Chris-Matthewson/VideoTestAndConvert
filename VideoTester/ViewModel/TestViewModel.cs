using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;

namespace VideoTester.ViewModel
{
    public class TestViewModel : ViewModelBase
    {
        private static readonly string[] AllVideoExtensions = { ".webm", ".mkv", ".flv", ".vob", ".ogv", ".ogg", ".gif", ".gifv", ".avi", ".mov", ".wmv", ".mp4", ".yuv", ".m4v", ".mpeg", ".mpg", ".3gp", ".f4v", ".f4p", ".f4a", ".f4b" };
        private static readonly string[] ValidVideoExtensions = { ".wmv", ".mp4", ".mpeg", ".mpg", ".gif", ".avi" };
        private static readonly string[] InvalidVideoExtensions = { ".webm", ".mkv", ".flv", ".vob", ".ogv", ".ogg", ".gifv", ".mov", ".yuv", ".3gp", ".f4v", ".f4p", ".f4a", ".f4b" };
        private static string AllVideoFilter = "Videos (*.webm, *.mkv, *.flv, *.vob, *.ogv, *.ogg, *.gif, *.gifv, *.avi, *.mov, *.wmv, *.mp4, *.yuv, *.m4v, *.mpeg, *.mpg, *.3gp, *.f4v, *.f4p, *.f4a, *.f4b)|*.webm; *.mkv; *.flv; *.vob; *.ogv; *.ogg; *.gif; *.gifv; *.avi; *.mov; *.wmv; *.mp4; *.yuv; *.m4v; *.mpeg; *.mpg; *.3gp; *.f4v; *.f4p; *.f4a; *.f4b";
        private readonly DispatcherTimer VideoCheckTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(100)};

        private MediaElement _mediaElementReference;
        private MediaState _currentMediaState = MediaState.Close;
        private RelayCommand _openFileCommand;
        private RelayCommand _playPauseCommand;
        private double _position;
        private RelayCommand _skipBackwardsCommand;
        private RelayCommand _skipForwardsCommand;
        private RelayCommand _stopCommand;
        private bool _useFfmpeg;
        private string _videoFile = "";
        private double _volume = 1;
        


        public TestViewModel()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            if (File.Exists(Path.GetDirectoryName(assemblyPath) + @"\Wildlife In HD.mp4"))
                _videoFile = Path.GetDirectoryName(assemblyPath) + @"\Wildlife In HD.mp4";

            VideoCheckTimer.Tick += (sender, args) => CheckVideo();
            VideoCheckTimer.Start();
        }

        public string VideoFile
        {
            get { return _videoFile; }
            set
            {
                if (Equals(_videoFile, value))
                    return;
                _videoFile = value;
                RaisePropertyChanged();
            }
        }

        public bool UseFfmpeg
        {
            get { return _useFfmpeg; }
            set
            {
                if (Equals(_useFfmpeg, value))
                    return;
                _useFfmpeg = value;
                RaisePropertyChanged();
            }
        }

        public double Volume
        {
            get { return _volume; }
            set
            {
                if (Equals(_volume, value))
                    return;
                _volume = value;
                if (_mediaElementReference != null)
                {
                    _mediaElementReference.Volume = _volume;
                }
                RaisePropertyChanged();
            }
        }

        public double Position
        {
            get { return _position; }
            set
            {
                if (Equals(_position, value))
                    return;
                _position = value;
                SetMediaElementPosition();
                RaisePropertyChanged();
            }
        }

        public MediaState CurrentMediaState
        {
            get { return _currentMediaState; }
            set
            {
                if (Equals(_currentMediaState, value))
                    return;
                _currentMediaState = value;
                RaisePropertyChanged();
            }
        }

        public ICommand OpenFileCommand => _openFileCommand ?? (_openFileCommand = new RelayCommand(OpenFile));

        public ICommand SkipBackwardsCommand
            => _skipBackwardsCommand ?? (_skipBackwardsCommand = new RelayCommand(SkipBackwards, CanSkipBackwards));

        public ICommand SkipForwardsCommand
            => _skipForwardsCommand ?? (_skipForwardsCommand = new RelayCommand(SkipForwards, CanSkipForwards));

        public ICommand PlayPauseCommand => _playPauseCommand ?? (_playPauseCommand = new RelayCommand(PlayPause));
        public ICommand StopCommand => _stopCommand ?? (_stopCommand = new RelayCommand(Stop));

        protected void OpenFile()
        {
            var openFileDialog = new OpenFileDialog {Filter = AllVideoFilter};
            if (openFileDialog.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);
                if (InvalidVideoExtensions.Contains(fileInfo.Extension.ToLower()) && !UseFfmpeg)
                {
                    MessageBox.Show("You have selected an invalid file. Please use the convert tool and try again.");
                    return;
                }

                VideoFile = fileInfo.FullName;
                Position = 0;
                _mediaElementReference.Source = new Uri(VideoFile);
                _mediaElementReference.Play();
                CurrentMediaState = MediaState.Play;
            }

        }
        

        protected void SkipBackwards()
        {
            var targetPosition = _mediaElementReference.Position -= TimeSpan.FromSeconds(5);
            if (targetPosition < TimeSpan.Zero)
            {
                Stop();
            }
        }

        protected bool CanSkipBackwards()
        {
            return CurrentMediaState == MediaState.Play || CurrentMediaState == MediaState.Pause &&
                   _mediaElementReference.Source != null && _mediaElementReference.NaturalDuration.HasTimeSpan;
        }

        protected void SkipForwards()
        {
            var targetPosition = _mediaElementReference.Position += TimeSpan.FromSeconds(5);
            if (targetPosition > _mediaElementReference.NaturalDuration)
            {
                Stop();
            }
        }

        protected bool CanSkipForwards()
        {
            return CurrentMediaState == MediaState.Play || CurrentMediaState == MediaState.Pause &&
                   _mediaElementReference.Source != null && _mediaElementReference.NaturalDuration.HasTimeSpan;
        }

        protected void PlayPause()
        {
            if (_mediaElementReference == null) return;

            switch (CurrentMediaState)
            {
                case MediaState.Manual:
                    MessageBox.Show("Error: Manual Media State");
                    break;
                case MediaState.Play:
                    _mediaElementReference.Pause();
                    CurrentMediaState = MediaState.Pause;
                    break;
                case MediaState.Pause:
                    _mediaElementReference.Play();
                    CurrentMediaState = MediaState.Play;
                    break;
                case MediaState.Stop:
                case MediaState.Close:
                    Position = 0;
                    _mediaElementReference.Source = new Uri(VideoFile);
                    _mediaElementReference.Play();
                    CurrentMediaState = MediaState.Play;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void Stop()
        {
            _mediaElementReference.Stop();
            _mediaElementReference.Source = null;
            CurrentMediaState = MediaState.Stop;
            Position = 0;
        }

        public void SetMediaElementReference(MediaElement newMediaElement)
        {
            _mediaElementReference = newMediaElement;
            _mediaElementReference.Volume = _volume;
        }

        private static MediaState GetMediaState(MediaElement mediaElement)
        {
            try
            {
                if (mediaElement == null)
                    return new MediaState();

                var hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
                if (hlp == null) return new MediaState();
                var helperObject = hlp.GetValue(mediaElement);
                var stateField = helperObject.GetType()
                    .GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField == null) return new MediaState();
                var state = (MediaState) stateField.GetValue(helperObject);
                return state;
            }
            catch (Exception)
            {
                return new MediaState();
            }
        }

        private void CheckVideo()
        {
            try
            {
                if (_mediaElementReference.Source == null || !_mediaElementReference.NaturalDuration.HasTimeSpan)
                {
                    return;
                }

                CurrentMediaState = GetMediaState(_mediaElementReference);

                Volume = _mediaElementReference.Volume;

                Position = _mediaElementReference.Position.TotalSeconds/
                           _mediaElementReference.NaturalDuration.TimeSpan.TotalSeconds;

                _skipBackwardsCommand.RaiseCanExecuteChanged();
                _skipForwardsCommand.RaiseCanExecuteChanged();
            }
            catch (Exception)
            {
                //
            }
        }

        private void SetMediaElementPosition()
        {
            if (_mediaElementReference.Source == null || !_mediaElementReference.NaturalDuration.HasTimeSpan)
            {
                return;
            }
            
            _mediaElementReference.Position = TimeSpan.FromSeconds(Position * _mediaElementReference.NaturalDuration.TimeSpan.TotalSeconds);
        }
    }
}