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
                    db.Execute("ALTER TABLE Files ADD AccountId INTEGER");
                    db.Execute("CREATE INDEX Files_AccountId ON Files(AccountId)");
                    break;
                default:
                    break;
            }
            return version+1;
        }
    }
}
