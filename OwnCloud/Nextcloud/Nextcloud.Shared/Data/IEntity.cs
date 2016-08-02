using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nextcloud.Data
{
    public class IEntity : INotifyPropertyChanged
    {

        #region Interface implimentations

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }

        #endregion
    }
}
