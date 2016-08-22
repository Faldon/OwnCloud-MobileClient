using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("CalendarObjects")]
    public class CalendarObject : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int? CalendarObjectId { get; set; }

        [NotNull]
        public string Path { get; set; }

        [NotNull]
        public string ETag { get; set; }

        public string CalendarData { get; set; }

        [NotNull, Default(value: false)]
        public bool InSync { get; set; }

        [ForeignKey(typeof(Calendar))]
        public int CalendarId { get; set; }

        [ManyToOne]
        public Calendar Calendar { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<CalendarEvent> CalendarEvents { get; set; }

        public void ParseCalendarData() {
            List<string> vevents = new List<string>();
            int lastStart = -1;
            while ((lastStart = CalendarData.IndexOf("BEGIN:VEVENT", ++lastStart)) > -1) {
                int lastEnd = CalendarData.IndexOf("END:VEVENT", lastStart);
                if(lastEnd > -1) {
                    vevents.Add(CalendarData.Substring(lastStart, lastEnd - lastStart + 10));
                }
            }
            foreach (string vevent in vevents) {
                List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
                foreach (string line in vevent.Split('\n')) {
                    var entry = new Dictionary<string, string>();
                    entry.Add(line.Split(':')[0], line.Split(new char[] { ':' }, 2)[1]);
                    data.Add(entry);
                };
            }
            //int start = CalendarData.IndexOf("BEGIN:VEVENT");
            //int end = CalendarData.IndexOf("END:VEVENT");
            //if(start == end) {
            //    return;
            //}
            //CalendarData = calendarData.Substring(start, end-start);
            //var data = CalendarData.Split('\n');
            //foreach(string s in data) {
            //    var line = s.Split(':');
            //    //if(line[0].StartsWith("UID")) { EventUID = line[1]; }
            //    //if (line[0].StartsWith("CREATED")) { EventCreated = DateTime.Parse(line[1]); }
            //}
        }
    }
}
