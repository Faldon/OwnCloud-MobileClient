using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nextcloud.ViewModel
{
    class IViewModel
    {
        #region Interface implimentations

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            OnPropertyChanged(propertyName);
        }

        #endregion
    }
}
