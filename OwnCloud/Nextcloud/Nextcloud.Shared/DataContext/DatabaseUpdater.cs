using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace Nextcloud.DataContext
{
    public static class DatabaseUpdater
    {
        public static int FromVersion(int version, SQLiteConnection db) {
            switch (version) {
                case 1:
                    var sql = "ALTER TABLE Files ADD AccountId INTEGER";
                    db.Execute(sql);
                    sql = "CREATE INDEX Files_AccountId ON Files(AccountId);";
                    db.Execute(sql);
                    break;
                default:
                    break;
            }
            return version+1;
        }
    }
}
