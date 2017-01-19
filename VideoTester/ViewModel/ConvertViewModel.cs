using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoTester.BackgroundWorkers;

namespace VideoTester.ViewModel
{
    public class ConvertViewModel : ViewModelBase
    {
        private RelayCommand _addToQueueCommand;
        private double _currentConvertProgress;
        private string _currentConvertText = "";
        private ObservableCollection<VideoViewModel> _queue = new ObservableCollection<VideoViewModel>();
        private readonly VideoConverterBackgroundWorker _worker = new VideoConverterBackgroundWorker();
        
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public ConvertViewModel()
        {
            _worker.Complete += WorkerOnComplete;
            _worker.ProgressChanged += WorkerOnProgressChanged;
            _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Converted Videos\";
        }

        public ICommand AddToQueueCommand => _addToQueueCommand ?? (_addToQueueCommand = new RelayCommand(AddToQueue));

        private string _outputDirectory;
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set
            {
                if (Equals(_outputDirectory, value))
                {
                    return;
                }
                _outputDirectory = value;
                RaisePropertyChanged();
            }
        }

        public double CurrentConvertProgress
        {
            get { return _currentConvertProgress; }
            set
            {
                if (Equals(_currentConvertProgress, value))
                    return;
                _currentConvertProgress = value;
                RaisePropertyChanged();
            }
        }
        public string CurrentConvertText
        {
            get { return _currentConvertText; }
            set
            {
                if (Equals(_currentConvertText, value))
                    return;
                _currentConvertText = value;
                RaisePropertyChanged();
            }
        }
        public ObservableCollection<VideoViewModel> Queue
        {
            get { return _queue; }
            set
            {
                if (Equals(_queue, value))
                    return;
                _queue = value;
                RaisePropertyChanged();
            }
        }

        private void WorkerOnComplete(object sender, string file)
        {
            foreach (var videoViewModel in Queue)
            {
                if (videoViewModel.FilePath == file)
                {
                    videoViewModel.IsComplete = true;
                }
            }

            var jobsComplete = Queue.Count(item => item.IsComplete);
            CurrentConvertText = jobsComplete + " / " + Queue.Count + "   Current job complete.";
            CurrentConvertProgress = 100;

            StartWorker();
        }

        private void WorkerOnProgressChanged(object sender, int i)
        {
            var jobsComplete = Queue.Count(item => item.IsComplete);
            CurrentConvertText = (jobsComplete + 1) + "/" + Queue.Count + " - " + Path.GetFileName(_worker.CurrentPath) + " - " + i.ToString("00") + "%";
            CurrentConvertProgress = i; 
        }

        protected void AddToQueue()
        {
            var odf = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Videos (*.webm, *.mkv, *.flv, *.vob, *.ogv, *.ogg, *.gif, *.gifv, *.avi, *.mov, *.wmv, *.mp4, *.yuv, *.m4v, *.mpeg, *.mpg, *.3gp, *.f4v, *.f4p, *.f4a, *.f4b)|*.webm; *.mkv; *.flv; *.vob; *.ogv; *.ogg; *.gif; *.gifv; *.avi; *.mov; *.wmv; *.mp4; *.yuv; *.m4v; *.mpeg; *.mpg; *.3gp; *.f4v; *.f4p; *.f4a; *.f4b"
            };

            if (odf.ShowDialog() == true)
                foreach (var odfFileName in odf.FileNames)
                    Queue.Add(new VideoViewModel
                    {
                        FilePath = odfFileName,
                        FileName = Path.GetFileName(odfFileName),
                        IsComplete = false
                    });

            if (!_worker.IsBusy)
            {
                StartWorker();
            }
        }

        private void StartWorker()
        {
            foreach (var t in Queue)
            {
                if (!t.IsComplete)
                {
                    if (t.FilePath != null)
                    {
                        _worker.StartWorker(t.FilePath, OutputDirectory + t.FileName.Replace(Path.GetExtension(t.FilePath), ".mp4"));
                    }
                    return;
                }
            }
        }

        private RelayCommand _changeOutputDirectoryCommand;
        public ICommand ChangeOutputDirectoryCommand => _changeOutputDirectoryCommand ?? (_changeOutputDirectoryCommand = new RelayCommand(ChangeOutputDirectory));

        protected void ChangeOutputDirectory()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OutputDirectory = dialog.FileName + @"\";
            }
        }

        private RelayCommand<VideoViewModel> _removeFromQueueCommand;
        public ICommand RemoveFromQueueCommand => _removeFromQueueCommand ?? (_removeFromQueueCommand = new RelayCommand<VideoViewModel>(RemoveFromQueue));

        protected void RemoveFromQueue(VideoViewModel queueItem)
        {
            if (queueItem == null) return;

            foreach (var videoViewModel in Queue.ToList())
            {
                if (videoViewModel.FilePath == queueItem.FilePath)
                {
                    if (_worker.CurrentPath == videoViewModel.FilePath)
                    {
                        _worker.CancelCurrentJob();
                    }
                    Queue.Remove(videoViewModel);
                }
            }
        }
    }
}