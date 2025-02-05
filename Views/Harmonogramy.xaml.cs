
// -----------------------------------------------------------------------
// <copyright file="Harmonogramy.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Views;

using System.Windows.Controls;

/// <summary>
/// Interaction logic for Harmonogramy.xaml
/// </summary>
public partial class Harmonogramy : UserControl
{
    #region constructors
    public Harmonogramy()
    {
        InitializeComponent();
    }
    #endregion

    #region UI handlers
    /// <summary>
    /// Switch to edit mode on double click
    /// </summary>
    private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lvHarmonogramy.SelectedIndex >= 0)
            (DataContext as ViewModels.HarmonogramyViewModel).OnChange();
    }
    #endregion
}
