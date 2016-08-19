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
                        db.Execute("ALTER TABLE Files ADD IsDownloaded INTEGER NOT NULL DEFAULT FALSE");
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
                    case 4:
                        db.BeginTransaction();
                        db.Execute("ALTER TABLE Calendars ADD IsSynced INTEGER NOT NULL DEFAULT FALSE");
                        db.Execute("ALTER TABLE CalendarEvents ADD Path VARCHAR NOT NULL DEFAULT ''");
                        db.Execute("ALTER TABLE CalendarEvents ADD ETag VARCHAR NOT NULL DEFAULT ''");
                        db.Execute("ALTER TABLE CalendarEvents ADD EventUID INTEGER NOT NULL DEFAULT 0");
                        db.Execute("ALTER TABLE CalendarEvents ADD EventCreated DATETIME DEFAULT NOW");
                        db.Execute("ALTER TABLE CalendarEvents ADD EventLastModified DATETIME");
                        db.Execute("ALTER TABLE CalendarEvents ADD StartDate DATETIME NOT NULL DEFAULT NOW");
                        db.Execute("ALTER TABLE CalendarEvents ADD EndDate DATETIME");
                        db.Execute("ALTER TABLE CalendarEvents ADD Summary VARCHAR");
                        db.Execute("ALTER TABLE CalendarEvents ADD Description VARCHAR");
                        db.Execute("ALTER TABLE CalendarEvents ADD Location VARCHAR");
                        db.Execute("ALTER TABLE CalendarEvents ADD Duration VARCHAR");
                        db.Execute("ALTER TABLE CalendarEvents ADD CalendarId INTEGER");
                        db.Execute("CREATE INDEX CalendarEvents_CalendarId ON CalendarEvents(CalendarId)");
                        db.Execute("CREATE TABLE RecurrenceRules(RuleId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, Frequency VARCHAR NOT NULL DEFAULT '', Interval INTEGER NOT NULL DEFAULT 1, Count INTEGER, ByMonth VARCHAR, ByDay VARCHAR, Until DATETIME, CalendarEventId INTEGER)");
                        db.Execute("CREATE INDEX RecurrenceRules_CalendarEventId ON RecurrenceRules(CalendarEventId)");
                        db.Commit();
                        break;
                    case 5:
                        db.BeginTransaction();
                        db.Execute("ALTER TABLE CalendarEvents ADD InSync INTEGER NOT NULL DEFAULT FALSE");
                        db.Commit();
                        break;
                    case 6:
                        db.BeginTransaction();                        
                        db.Execute("ALTER TABLE CalendarEvents DROP EventUID");
                        db.Execute("ALTER TABLE CalendarEvents ADD EventUID VARCHAR NOT NULL DEFAULT ''");
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
