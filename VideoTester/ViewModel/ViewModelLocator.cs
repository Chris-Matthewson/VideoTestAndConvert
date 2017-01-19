/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocatorTemplate xmlns:vm="clr-namespace:VideoTester.ViewModel" x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/
using System.Diagnostics;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace VideoTester.ViewModel
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<TestViewModel>();
            SimpleIoc.Default.Register<ConvertViewModel>();
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
        public TestViewModel Test => ServiceLocator.Current.GetInstance<TestViewModel>();
        public ConvertViewModel Convert => ServiceLocator.Current.GetInstance<ConvertViewModel>();

        public static void Cleanup()
        {
            var ffp = Process.GetProcessesByName("ffmpeg");
            foreach (var p in ffp)
            {
                p.Kill();
            }
        }
    }
}