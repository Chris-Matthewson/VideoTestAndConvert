using GalaSoft.MvvmLight;

namespace VideoTester.ViewModel
{
    public class VideoViewModel : ViewModelBase
    {
        private string _filePath = "";
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (Equals(_filePath, value))
                {
                    return;
                }
                _filePath = value;
                RaisePropertyChanged();
            }
        }

        private string _fileName = "";
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (Equals(_fileName, value))
                {
                    return;
                }
                _fileName = value;
                RaisePropertyChanged();
            }
        }

        private bool _isComplete;
        public bool IsComplete
        {
            get { return _isComplete; }
            set
            {
                if (Equals(_isComplete, value))
                {
                    return;
                }
                _isComplete = value;
                RaisePropertyChanged();
            }
        }
    }
}