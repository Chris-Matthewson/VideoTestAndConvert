using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoTester.BindingConverter
{
    public class PlayPauseMediaStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().Equals("Play") ? "❚❚" : "►";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
