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
            try {
                switch (version) {
                    case 1:
                        db.BeginTransaction();
                        db.Execute("ALTER TABLE Files ADD AccountId INTEGER");
                        db.Execute("CREATE INDEX Files_AccountId ON Files(AccountId)");
                        db.Commit();
                        break;
                    case 2:
                        db.BeginTransaction();
                        db.Execute("ALTER TABLE Files ADD IsDownloaded NOT NULL DEFAULT FALSE");
                        db.Commit();
                        break;
                    case 3:
                        db.BeginTransaction();
                        db.Execute("ALTER TABLE Calendars ADD DisplayName VARCHAR");
                        db.Execute("ALTER TABLE Calendars ADD Path VARCHAR");
                        db.Execute("ALTER TABLE Calendars ADD Color VARCHAR");
                        db.Execute("ALTER TABLE Calendars ADD CTag VARCHAR");
                        db.Execute("ALTER TABLE Calendars ADD AccountId INTEGER");
                        db.Execute("CREATE INDEX Calendars_AccountId ON Calendars(AccountId)");
                        db.Commit();
                        break;
                    default:
                        break;
                }
                return version + 1;
            } catch(SQLiteException sqlE) {
                db.Rollback();
#if DEBUG
                System.Diagnostics.Debug.WriteLine(sqlE.Message);
#endif
                App.Current.Exit();
            }
            return version;
        }
    }
}
