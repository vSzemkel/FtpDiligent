
// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Views;

using System.Windows;

using FtpDiligent.ViewModels;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region fields
    /// <summary>
    /// Zakładka Sterowanie
    /// </summary>
    public SterowanieViewModel m_tbSterowanie;

    /// <summary>
    /// Zakładka Serwery
    /// </summary>
    public SerweryViewModel m_tbSerwery;

    /// <summary>
    /// Zakładka do edycji danych serwera, leniwie inicjalizowana
    /// </summary>
    private SerweryDetails _m_tbSerweryDetails;

    /// <summary>
    /// Zakładka Harmonogramy
    /// </summary>
    public HarmonogramyViewModel m_tbHarmonogramy;

    /// <summary>
    /// Zakładka do edycji danych harmonogramu, leniwie inicjalizowana
    /// </summary>
    private HarmonogramyDetails _m_tbHarmonogramyDetails;

    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IFtpRepository m_repository;
    #endregion

    #region properties
    public SerweryDetails m_tbSerweryDetails {
        get {
            if (_m_tbSerweryDetails == null) {
                _m_tbSerweryDetails = new SerweryDetails(this, m_repository);
                _m_tbSerweryDetails.m_mainWnd = this;
                tabSerweryDetails.Content = _m_tbSerweryDetails;
            }

            return _m_tbSerweryDetails;
        }
    }

    public HarmonogramyDetails m_tbHarmonogramyDetails {
        get {
            if (_m_tbHarmonogramyDetails == null) {
                _m_tbHarmonogramyDetails = new HarmonogramyDetails(this, m_repository);
                _m_tbHarmonogramyDetails.m_mainWnd = this;
                tabHarmonogramyDetails.Content = _m_tbHarmonogramyDetails;
            }

            return _m_tbHarmonogramyDetails;
        }
    }
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor okna głównego
    /// </summary>
    /// <param name="repository">Repozytorium danych</param>
    /// <param name="config">Konfiguracja aplikacji</param>
    public MainWindow(IFtpRepository repository)
    {
        m_repository = repository;
        InitializeComponent();

        this.Title = $"FtpDiligent [instance {FtpDiligentGlobals.Instance}]";
    }
    #endregion
}
