﻿using System;
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

        public NextcloudData() {
            CreateDatabaseFile("nextcloud.db");
            db = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), database.Path);
            InitializeSchema(db);
        }

        public SQLiteConnection GetConnection() {
            return db;
        }

        public void StoreAccount(Account newOrUpdatedAccount) {
            db.InsertOrReplaceWithChildren(newOrUpdatedAccount.Server, true);
        }

        private void InitializeSchema(SQLiteConnection db) {
            object dbVersion;
            ApplicationDataContainer dbsettings = ApplicationData.Current.LocalSettings.CreateContainer("DATABASE", ApplicationDataCreateDisposition.Always);
#if DEBUG
            dbsettings.Values["DATABASE_VERSION"] = null;
#endif
            if (!dbsettings.Values.TryGetValue("DATABASE_VERSION", out dbVersion)) {
                try {
                    db.CreateTable<Server>();
                    db.CreateTable<Account>();
                    db.CreateTable<File>();
                    db.CreateTable<Calendar>();
                    db.CreateTable<CalendarEvent>();
                    dbsettings.Values["DATABASE_VERSION"] = 1;
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
    }
}
