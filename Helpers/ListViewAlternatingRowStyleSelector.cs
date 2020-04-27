
// -----------------------------------------------------------------------
// <copyright file="Serwery.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System.Windows;
    using System.Windows.Controls;
    
    public class ListViewAlternatingRowStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            ListView listView = ItemsControl.ItemsControlFromItemContainer(container) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(container);
            if (index % 2 == 0)
                return (Style)listView.FindResource("ListViewRow");
            else
                return (Style)listView.FindResource("ListViewAlternatingRow");
        }
    }
}
