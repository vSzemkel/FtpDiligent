
// -----------------------------------------------------------------------
// <copyright file="Serwery.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Views;

using System.Windows.Controls;

using Prism.Unity;

public partial class Serwery : UserControl
{
    #region constructors
    public Serwery()
    {
        InitializeComponent();

        var shell = PrismApplication.Current.MainWindow as MainWindow;
        if (shell != null)
            shell.m_tbSerwery = this;
    }
    #endregion

    #region UI handlers
    /// <summary>
    /// Switch to edit mode on double click
    /// </summary>
    private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lvSerwery.SelectedIndex >= 0)
            (DataContext as ViewModels.SerweryViewModel).OnChange();
    }
    #endregion
}
