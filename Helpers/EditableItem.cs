
// -----------------------------------------------------------------------
// <copyright file="EditableItem.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// <based-on>WPF-Samples\Data Binding\EditingCollections\PurchaseItem.cs</based-on>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Klasa bazowa używana do instancjonowania elementów ObservableCollection
    /// </summary>
    /// <typeparam name="T">Model danych dla ObservableCollection</typeparam>
    public abstract class EditableItem<T> : INotifyPropertyChanged, IEditableObject where T: struct
    {
        protected T _copyData;
        protected T _currentData;

        public T GetModel() { return _currentData; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IEditableObject Members

        public void BeginEdit()
        {
            _copyData = _currentData;
        }

        public void CancelEdit()
        {
            _currentData = _copyData;
            NotifyPropertyChanged("");
        }

        public void EndEdit()
        {
            _copyData = new T();
        }

        #endregion
    }
}