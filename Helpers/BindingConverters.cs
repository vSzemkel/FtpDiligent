
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentOracleClient.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Windows.Data;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Collections.Generic;

    public class SelectedItemEnabler : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
    }

    public class ImageForOperation : IValueConverter
    {
        static Dictionary<int, string> mapping = new Dictionary<int, string>() {
            {1, "/FtpDiligent;component/Images/get.png" },
            {2, "/FtpDiligent;component/Images/put.png" },
            {4, "/FtpDiligent;component/Images/hot.png" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return mapping[(int)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class DayNameLocalisator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CultureInfo.CurrentUICulture.DateTimeFormat.DayNames[(int)(DayOfWeek)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string dayName = value as string;
            int index = Array.FindIndex(CultureInfo.CurrentUICulture.DateTimeFormat.DayNames, (dn) => {return dn == dayName; });
            return (DayOfWeek)index;
        }
    }
}
