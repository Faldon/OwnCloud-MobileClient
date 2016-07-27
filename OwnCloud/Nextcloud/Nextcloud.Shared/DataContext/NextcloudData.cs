using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net;
using SQLite.Net.Async;
using SQLiteNetExtensions.Extensions;
using Windows.Storage;
using Nextcloud.Data;
using System.Threading.Tasks;

namespace Nextcloud.DataContext
{
    public class NextcloudData
    {
        private SQLiteConnection db;
        private StorageFile database;
        private const int DATABASE_VERSION = 2;

        public NextcloudData() {
            CreateDatabaseFile("nextcloud.db");
            db = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), database.Path);
            InitializeDatabase(db);
        }

        public SQLiteConnection GetConnection() {
            return db;
        }

        public void StoreAccount(Account newOrUpdatedAccount) {
            db.InsertOrReplaceWithChildren(newOrUpdatedAccount.Server, true);
        }

        public async void StoreFile(File newOrUpdatedFile) {
            if (!await IsFileFetched(newOrUpdatedFile)) {
                db.InsertOrReplaceWithChildren(newOrUpdatedFile, true);
            }
        }

        public void RemoveAccount(Account account) {
            db.Delete(account);
        }

        public async Task<bool> IsFileFetched(File file) {
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(() => (SQLiteConnectionWithLock)db);
            var fetchedFile = conn.Table<File>().Where(f => (
            f.Filename == file.Filename &&
            f.Filepath == file.Filepath &&
            f.AccountId == file.AccountId
            ));
            return await fetchedFile.FirstOrDefaultAsync() != null;
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
