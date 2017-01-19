using GalaSoft.MvvmLight.Ioc;
using VideoTester.ViewModel;

namespace VideoTester
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();

            SimpleIoc.Default.GetInstance<TestViewModel>().SetMediaElementReference(PreviewMediaElement);
        }

        private void PreviewMediaElement_MediaFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {

        }
    }
}