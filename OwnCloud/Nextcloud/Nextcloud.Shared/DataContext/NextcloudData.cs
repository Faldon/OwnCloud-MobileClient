using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net;
using SQLiteNetExtensions.Extensions;
using Windows.Storage;
using Nextcloud.Data;

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
            InitializeSchema(db);
            UpdateSchema(db);
        }

        public SQLiteConnection GetConnection() {
            return db;
        }

        public void StoreAccount(Account newOrUpdatedAccount) {
            db.InsertOrReplaceWithChildren(newOrUpdatedAccount.Server, true);
        }

        public void RemoveAccount(Account account) {
            db.Delete(account);
        }

        private void InitializeSchema(SQLiteConnection db) {
            object dbVersion;
            ApplicationDataContainer dbSettings = ApplicationData.Current.LocalSettings.CreateContainer("DATABASE", ApplicationDataCreateDisposition.Always);
            if (!dbSettings.Values.TryGetValue("DATABASE_VERSION", out dbVersion)) {
                try {
                    db.CreateTable<Server>();
                    db.CreateTable<Account>();
                    db.CreateTable<File>();
                    db.CreateTable<Calendar>();
                    db.CreateTable<CalendarEvent>();
                    dbSettings.Values["DATABASE_VERSION"] = 1;
                } catch (SQLiteException ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    ReplaceDatabaseFile("nextcloud.db");
                }
            };
        }

        private async void CreateDatabaseFile(string filename) {
            database = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
        }

        private async void ReplaceDatabaseFile(string filename) {
            database = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
        }

        private void UpdateSchema(SQLiteConnection db) {
            ApplicationDataContainer dbSettings = ApplicationData.Current.LocalSettings.CreateContainer("DATABASE", ApplicationDataCreateDisposition.Always);
            int dbVersion = (int)dbSettings.Values["DATABASE_VERSION"];
            while(dbVersion < DATABASE_VERSION) {
                dbVersion = DatabaseUpdater.FromVersion(dbVersion, db);
            }
        }
    }
}
