using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nexcloud.Data
{
    public class IEntity : INotifyPropertyChanged
    {

        #region Interface implimentations

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }

        #endregion
    }
}
