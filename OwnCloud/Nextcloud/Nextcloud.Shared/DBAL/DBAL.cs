using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace Nextcloud.DBAL
{
    class DBAL
    {
        private StorageFile database;


        private async void CreateDatabaseFile(string filename) {
            this.database = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
        }
    }
}
