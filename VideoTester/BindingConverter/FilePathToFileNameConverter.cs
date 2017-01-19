using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace VideoTester.BindingConverter
{
    public class FilePathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var fileInfo = new FileInfo(value.ToString());
                return fileInfo.Name.Replace(fileInfo.Extension, "");
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
