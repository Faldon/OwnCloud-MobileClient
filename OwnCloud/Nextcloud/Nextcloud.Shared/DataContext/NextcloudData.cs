using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net;
using SQLite.Net.Async;
using System.Linq;
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
        private const int DATABASE_VERSION = 3;

        public NextcloudData() {
            CreateDatabaseFile("nextcloud.db");
            connection = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), database.Path, storeDateTimeAsTicks: false);
            asyncConnection = null;
            InitializeDatabase(connection);
        }

        public SQLiteConnection GetConnection() {
            return connection;
        }

        public SQLiteAsyncConnection GetConnectionAsync() {
            if (asyncConnection == null) {
                var connectionString = new SQLiteConnectionString(database.Path, storeDateTimeAsTicks: false);
                var connectionWithLock = new SQLiteConnectionWithLock(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), connectionString);
                asyncConnection = new SQLiteAsyncConnection(() => connectionWithLock);
            }
            return asyncConnection;
        }

        public void StoreAccount(Account newOrUpdatedAccount) {
            connection.InsertOrReplaceWithChildren(newOrUpdatedAccount.Server, true);
        }

        public Account LoadAccount(int id) {
            return connection.GetWithChildren<Account>(id, recursive: true);
        }

        public List<File> GetUserfilesInPath(Account user, string path) {
            List<File> currentFilesInDatabase = connection.GetAllWithChildren<File>(f => f.AccountId == user.AccountId && (f.Filepath.StartsWith(path) || path.StartsWith(f.Filepath+f.Filename)), recursive:true).ToList();
            return currentFilesInDatabase;
        }

        public int StoreFile(File newOrUpdatedFile) {
            File inDatabase = connection.Table<File>().Where(f => (f.Filename == newOrUpdatedFile.Filename && f.Filepath == newOrUpdatedFile.Filepath && f.AccountId == newOrUpdatedFile.AccountId)).FirstOrDefault();
            if(inDatabase == null) {
                connection.InsertWithChildren(newOrUpdatedFile, recursive: true);
            } else {
                connection.UpdateWithChildren(newOrUpdatedFile);
            }
            return newOrUpdatedFile.FileId?? 0;
        }

        public File LoadFile(int id) {
            File f = connection.GetWithChildren<File>(id, recursive: true);
            f.Account = connection.GetWithChildren<Account>(f.AccountId, recursive: true);
            return f;
        }

        public async void UpdateFile(File updatedFile) {
            await GetConnectionAsync().FindAsync<File>(f => (f.Filename == updatedFile.Filename && f.Filepath == updatedFile.Filepath && f.AccountId == updatedFile.AccountId)).ContinueWith(async f => {
                updatedFile.FileId = f.Result.FileId;
                await GetConnectionAsync().UpdateAsync(updatedFile);
            });
        }

        public async void RemoveFile(File file) {
            File fileInDatabase = connection.Get<File>(file.FileId);
            await GetConnectionAsync().DeleteAsync(fileInDatabase);
        }

        public void RemoveAccount(Account account) {
            connection.Delete(account);
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

    }
}
