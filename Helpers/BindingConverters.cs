
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentOracleClient.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

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
    static Dictionary<int, string> mapping = new() {
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

public class ImageForError : IValueConverter
{
    static Dictionary<eSeverityCode, string> mapping = new() {
        {eSeverityCode.Message, "/FtpDiligent;component/Images/detail.png" },
        {eSeverityCode.Warning, "/FtpDiligent;component/Images/warn.png" },
        {eSeverityCode.Error, "/FtpDiligent;component/Images/err.png" },
        {eSeverityCode.TransferError, "/FtpDiligent;component/Images/trans.png" }
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return mapping[(eSeverityCode)value];
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
