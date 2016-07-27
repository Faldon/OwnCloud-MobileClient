using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net;
using SQLite.Net.Async;
using SQLiteNetExtensions.Extensions;
using Windows.Storage;
using Nextcloud.Data;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Nextcloud.DataContext
{
    public class NextcloudData
    {
        private SQLiteConnection connection;
        private SQLiteAsyncConnection asyncConnection;
        private StorageFile database;
        private const int DATABASE_VERSION = 2;

        public NextcloudData() {
            CreateDatabaseFile("nextcloud.db");
            connection = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), database.Path);
            asyncConnection = null;
            InitializeDatabase(connection);
        }

        public SQLiteConnection GetConnection() {
            return connection;
        }

        public SQLiteAsyncConnection GetConnectionAsync() {
            if (asyncConnection == null) {
                var connectionString = new SQLiteConnectionString(database.Path, false);
                var connectionWithLock = new SQLiteConnectionWithLock(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), connectionString);
                asyncConnection = new SQLiteAsyncConnection(() => connectionWithLock);
            }
            return asyncConnection;
        }

        public void StoreAccount(Account newOrUpdatedAccount) {
            connection.InsertOrReplaceWithChildren(newOrUpdatedAccount.Server, true);
        }

        public async void StoreFile(File newOrUpdatedFile) {
            int result = await GetConnectionAsync().InsertOrReplaceAsync(newOrUpdatedFile);
            //var fetchedFile = asyncConnection.Table<File>().Where(f => (
            //    f.Filename == newOrUpdatedFile.Filename &&
            //    f.Filepath == newOrUpdatedFile.Filepath &&
            //    f.AccountId == newOrUpdatedFile.AccountId
            //));
            //File stored = await fetchedFile.FirstOrDefaultAsync();
            //bool needsUpdate = await NeedsUpdate(newOrUpdatedFile);
            //if (stored == null) {
                
            //    //db.InsertOrReplaceWithChildren(newOrUpdatedFile, true);
            //}
        }

        public void RemoveAccount(Account account) {
            connection.Delete(account);
        }

        public async Task<bool> NeedsUpdate(File file) {
            var connectionFactory = new Func<SQLiteConnectionWithLock>(() => new SQLiteConnectionWithLock(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), new SQLiteConnectionString(database.Path, storeDateTimeAsTicks: false)));
            var asyncConnection = new SQLiteAsyncConnection(connectionFactory);
            var fetchedFile = asyncConnection.Table<File>().Where(f => (
            f.Filename == file.Filename &&
            f.Filepath == file.Filepath &&
            f.AccountId == file.AccountId
            ));
            File stored = await fetchedFile.FirstOrDefaultAsync();
            return (stored == null);
        }

        private void InitializeDatabase(SQLiteConnection db) {
            object dbVersion;
            ApplicationDataContainer dbSettings = ApplicationData.Current.LocalSettings.CreateContainer("DATABASE", ApplicationDataCreateDisposition.Always);
            if (!dbSettings.Values.TryGetValue("DATABASE_VERSION", out dbVersion)) {
                try {
                    db.CreateTable<Server>();
                    db.CreateTable<Account>();
                    db.CreateTable<File>();
                    db.CreateTable<Calendar>();
                    db.CreateTable<CalendarEvent>();
                    dbSettings.Values["DATABASE_VERSION"] = DATABASE_VERSION;
                } catch (SQLiteException ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    ReplaceDatabaseFile("nextcloud.db");
                }
            } else {
                while ((int)dbVersion < DATABASE_VERSION) {
                    dbVersion = DatabaseUpdater.FromVersion((int)dbVersion, db);
                }
                dbSettings.Values["DATABASE_VERSION"] = dbVersion;
            }
        }

        private async void CreateDatabaseFile(string filename) {
            database = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
        }

        private async void ReplaceDatabaseFile(string filename) {
            database = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
        }

        public async void UpdateFileTable(ObservableCollection<File> fileCollection) {
            var connectionFactory = new Func<SQLiteConnectionWithLock>(() => new SQLiteConnectionWithLock(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), new SQLiteConnectionString(database.Path, storeDateTimeAsTicks: false)));
            var asyncConnection = new SQLiteAsyncConnection(connectionFactory);
            int result = await asyncConnection.InsertOrReplaceAllAsync(fileCollection);
        }
    }
}
