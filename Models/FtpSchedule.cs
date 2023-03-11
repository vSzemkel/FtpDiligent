
// -----------------------------------------------------------------------
// <copyright file="FtpSchedule.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Globalization;

/// <summary>
/// Harmonogram transferów FTP
/// </summary>
public class FtpSchedule : EditableItem<FtpScheduleModel>
{
    #region constructors
    /// <summary>
    /// Inicjalizacja dodawanego obiektu
    /// </summary>
    /// <param name="endpoint">Klucz rodzica</param>
    public FtpSchedule()
    {
        Stride = 10;
        Enabled = true;
    }

    /// <summary>
    /// Konstruktor używany do inicjowania obiektu na podstawie bazy danych
    /// </summary>
    /// <param name="fsm">MOdel bazodanowy</param>
    public FtpSchedule(FtpScheduleModel fsm)
    {
        _currentData = fsm;
    }
    #endregion

    #region properties
    public int XX {
        get { return _currentData.xx; }
        set {
            if (_currentData.xx != value) {
                _currentData.xx = value;
                NotifyPropertyChanged();
            }
        }
    }

    public int Endpoint {
        get { return _currentData.endXX; }
        set {
            if (_currentData.endXX != value) {
                _currentData.endXX = value;
                NotifyPropertyChanged();
            }
        }
    }

    public string Name {
        get { return _currentData.name; }
        set {
            if (_currentData.name != value) {
                _currentData.name = value;
                NotifyPropertyChanged();
            }
        }
    }

    public short Stride {
        get { return _currentData.stride; }
        set {
            if (_currentData.stride != value) {
                _currentData.stride = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("Repeats");
            }
        }
    }

    public int Repeats {
        get { return 1 + (int)Math.Floor(((StopTime - StartTime).TotalMinutes) / Stride); }
    }

    public DayOfWeek StartDow {
        get { return (DayOfWeek)_currentData.startSpan.Days; }
        set {
            if (_currentData.startSpan.Days != (int)value) {
                _currentData.startSpan += new TimeSpan((int)value - _currentData.startSpan.Days, 0, 0, 0);
                NotityScheduleChange();
            }
        }
    }

    public TimeSpan StartTime {
        get { return _currentData.startSpan; }
        set {
            if (_currentData.startSpan != value) {
                _currentData.startSpan = new TimeSpan(_currentData.startSpan.Days, value.Hours, value.Minutes, value.Seconds);
                NotityScheduleChange();
            }
        }
    }

    public string StartTimeString {
        get { 
            var ss = FormatTimeSpan(_currentData.startSpan);
            return ss.Substring(ss.Length - 5);
        }
        set {
            TimeSpan newSpan;
            if (TimeSpan.TryParse(value, out newSpan)) {
                var oldSpan = StartTime;
                StartTime = new TimeSpan(oldSpan.Days, newSpan.Hours, newSpan.Minutes, 0);
                NotifyPropertyChanged("StartString");
            }
        }

    }

    public string StartString {
        get { return FormatTimeSpan(_currentData.startSpan); }
    }

    public DayOfWeek StopDow {
        get { return (DayOfWeek)_currentData.stopSpan.Days; }
        set {
            if (_currentData.stopSpan.Days != (int)value) {
                _currentData.stopSpan += new TimeSpan((int)value - _currentData.stopSpan.Days, 0, 0, 0);
                NotityScheduleChange();
            }
        }
    }

    public TimeSpan StopTime {
        get { return _currentData.stopSpan; }
        set {
            if (_currentData.stopSpan != value) {
                _currentData.stopSpan = new TimeSpan(_currentData.stopSpan.Days, value.Hours, value.Minutes, value.Seconds);
                NotityScheduleChange();
            }
        }
    }

    public string StopTimeString {
        get {
            var ss = FormatTimeSpan(_currentData.stopSpan);
            return ss.Substring(ss.Length - 5);
        }
        set {
            TimeSpan newSpan;
            if (TimeSpan.TryParse(value, out newSpan)) {
                var oldSpan = StopTime;
                StopTime = new TimeSpan(oldSpan.Days, newSpan.Hours, newSpan.Minutes, 0);
                NotifyPropertyChanged("StopString");
            }
        }

    }

    public string StopString {
        get { return FormatTimeSpan(_currentData.stopSpan); }
    }

    public string NextSyncString {
        get { 
            TimeSpan ret;
            if (!Enabled) return "nieplanowany";
            TimeSpan weeklyNowSpan = DateTime.Now - DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            if (StopTime < weeklyNowSpan) return "kolejny tydzień";
            if (weeklyNowSpan <= StartTime)
                ret = StartTime;
            else {
                int tour = (int)Math.Ceiling((weeklyNowSpan - StartTime).TotalMinutes / Stride);
                ret = StartTime + new TimeSpan(0, tour * Stride, 0);
            }

            return FormatTimeSpan(ret);
        }
    }

    public bool Enabled {
        get { return _currentData.enabled; }
        set {
            if (_currentData.enabled != value) {
                _currentData.enabled = value;
                NotifyPropertyChanged("NextSyncString");
            }
        }
    }
    #endregion

    #region private
    private string FormatTimeSpan(TimeSpan ts) => $"{CultureInfo.CurrentUICulture.DateTimeFormat.DayNames[ts.Days]} {DateTime.Today + ts:HH:mm}";

    /// <summary>
    /// Aktualizuje wartości kolumn wyliczanych na podstawie czasu
    /// </summary>
    private void NotityScheduleChange()
    {
        NotifyPropertyChanged("Repeats");
        NotifyPropertyChanged("StopString");
        NotifyPropertyChanged("NextSyncString");
    }
    #endregion
}
