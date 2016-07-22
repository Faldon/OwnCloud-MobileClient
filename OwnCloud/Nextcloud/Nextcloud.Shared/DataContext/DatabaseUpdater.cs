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
                    var sql = "UPDATE Files ADD(AccountId INTEGER NOT NULL)";
                    db.CreateCommand(sql).ExecuteNonQuery();
                    sql = "CREATE INDEX Files_AccountId ON Files(AccountId);";
                    db.CreateCommand(sql).ExecuteNonQuery();
                    break;
                default:
                    break;
            }
            return version+1;
        }
    }
}
