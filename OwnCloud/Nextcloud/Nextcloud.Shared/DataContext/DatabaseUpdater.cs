using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace Nextcloud.DataContext
{
    public static class DatabaseUpdater
    {
        public static int FromVersion(int version, SQLiteConnection db) {
            try {
                switch (version) {
                    //case 1:
                    //    db.BeginTransaction();
                    //    db.Execute("ALTER TASLE Files ADD drool");
                    //    db.Execute("CREATE INDEX Files_AccountId ON Files(AccountId)");
                    //    db.Commit();
                    //    break;
                    //case 2:
                    //    db.BeginTransaction();
                    //    db.Execute("ALTER TABLE Files ADD IsDownloaded INTEGER NOT NULL DEFAULT FALSE");
                    //    db.Commit();
                    //    break;
                    //case 3:
                    //    db.BeginTransaction();
                    //    db.Execute("ALTER TABLE Calendars ADD DisplayName VARCHAR");
                    //    db.Execute("ALTER TABLE Calendars ADD Path VARCHAR");
                    //    db.Execute("ALTER TABLE Calendars ADD Color VARCHAR");
                    //    db.Execute("ALTER TABLE Calendars ADD CTag VARCHAR");
                    //    db.Execute("ALTER TABLE Calendars ADD AccountId INTEGER");
                    //    db.Execute("CREATE INDEX Calendars_AccountId ON Calendars(AccountId)");
                    //    db.Commit();
                    //    break;
                    //case 4:
                    //    db.BeginTransaction();
                    //    db.Execute("ALTER TABLE Calendars ADD IsSynced INTEGER NOT NULL DEFAULT FALSE");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD Path VARCHAR NOT NULL DEFAULT ''");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD ETag VARCHAR NOT NULL DEFAULT ''");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD EventUID INTEGER NOT NULL DEFAULT 0");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD EventCreated DATETIME DEFAULT NOW");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD EventLastModified DATETIME");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD StartDate DATETIME NOT NULL DEFAULT NOW");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD EndDate DATETIME");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD Summary VARCHAR");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD Description VARCHAR");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD Location VARCHAR");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD Duration VARCHAR");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD CalendarId INTEGER");
                    //    db.Execute("CREATE INDEX CalendarEvents_CalendarId ON CalendarEvents(CalendarId)");
                    //    db.Execute("CREATE TABLE RecurrenceRules(RuleId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, Frequency VARCHAR NOT NULL DEFAULT '', Interval INTEGER NOT NULL DEFAULT 1, Count INTEGER, ByMonth VARCHAR, ByDay VARCHAR, Until DATETIME, CalendarEventId INTEGER)");
                    //    db.Execute("CREATE INDEX RecurrenceRules_CalendarEventId ON RecurrenceRules(CalendarEventId)");
                    //    db.Commit();
                    //    break;
                    //case 5:
                    //    db.BeginTransaction();
                    //    db.Execute("ALTER TABLE CalendarEvents ADD InSync INTEGER NOT NULL DEFAULT FALSE");
                    //    db.Commit();
                    //    break;
                    //case 6:
                    //    db.BeginTransaction();                        
                    //    db.Execute("ALTER TABLE CalendarEvents DROP EventUID");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD EventUID VARCHAR NOT NULL DEFAULT ''");
                    //    db.Commit();
                    //    break;
                    //case 7:
                    //    db.BeginTransaction();
                    //    db.Execute("CREATE TABLE CalendarObjects(CalendarObjectId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, Path VARCHAR NOT NULL DEFAULT '', ETag VARCHAR NOT NULL DEFAULT '', CalendarData VARCHAR NOT NULL DEFAULT '', InSync INTEGER NOT NULL DEFAULT FALSE, CalendarId INTEGER)");
                    //    db.Execute("CREATE INDEX CalendarObjects_CalendarId ON CalendarObjects(CalendarId)");
                    //    db.Execute("TRUNCATE TABLE CalendarEvents");
                    //    db.Execute("DROP INDEX CalendarEvents_CalendarId");
                    //    db.Execute("ALTER TABLE CalendarEvents DROP CalendarID");
                    //    db.Execute("ALTER TABLE CalendarEvents ADD CalendarObjectId INTEGER");
                    //    db.Execute("CREATE INDEX CalendarEvents_CalendarObjectId ON CalendarEvents(CalendarObjectId)");
                    //    db.Commit();
                    //    break;
                    //case 8:
                    //    db.BeginTransaction();
                    //    db.Execute("ALTER TABLE CalendarObjects DROP CalendarData");
                    //    db.Execute("ALTER TABLE CalendarObjects ADD CalendarData VARCHAR");
                    //    db.Commit();
                    //    break;
                    default:
                        break;
                }
                return version + 1;
            } catch(SQLiteException sqlE) {
                db.Rollback();
                string logTime = DateTime.Now.ToUniversalTime().ToString();
                var logMessage = "[" + logTime + "]: Error updating database from version " + version.ToString() + Environment.NewLine;
                logMessage += "[" + logTime + "]: " + sqlE.Message + Environment.NewLine;
                WriteUpdateLog(logMessage);
                CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => App.Current.Exit());
            }
            return version;
        }

        private static async void WriteUpdateLog(string message) {
            StorageFile log = await ApplicationData.Current.LocalFolder.CreateFileAsync("cloudStore.log", CreationCollisionOption.OpenIfExists);
            await FileIO.AppendTextAsync(log, message);
        }

        public static async void DeleteUpdateLog() {
            StorageFile log = await ApplicationData.Current.LocalFolder.CreateFileAsync("cloudStore.log", CreationCollisionOption.OpenIfExists);
            await log.DeleteAsync();
        }
    }
}
