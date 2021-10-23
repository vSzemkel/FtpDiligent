
// -----------------------------------------------------------------------
// <copyright file="FindParent.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, pażdziernik 2021</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Odnajduje wśród przodków kontrolkę typu T
    /// </summary>
    public static class FindParentHelper
    {
        /// <summary>
        /// Odnajduje wśród przodków kontrolkę typu T
        /// </summary>
        /// <returns>Referencja do przodka zadanego typu lub null</returns>
        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            var current = VisualTreeHelper.GetParent(child);
            while (current != null) {
                if (current is T)
                    return current as T;
                current = VisualTreeHelper.GetParent(child);
            }

            return null;
        }
    }
}
