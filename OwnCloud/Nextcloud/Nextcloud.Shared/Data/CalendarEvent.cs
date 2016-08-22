using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("CalendarEvents")]
    public class CalendarEvent : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int? CalendarEventId { get; set; }

        [NotNull]
        public string Path { get; set; }

        [NotNull]
        public string ETag { get; set; }

        [NotNull]
        public string EventUID { get; set; }

        [NotNull]
        public DateTime EventCreated { get; set; }

        public DateTime EventLastModified { get; set; }

        [NotNull]
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Summary { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public string Duration { get; set; }

        [NotNull, Default(value: false)]
        public bool InSync { get; set; }

        [ForeignKey(typeof(Calendar))]
        public int CalendarObjectId { get; set; }

        [ManyToOne]
        public CalendarObject CalendarObject { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<RecurrenceRule> RecurrenceRules { get; set; }

        public void ParseCalendarData(string calendarData) {
            int start = calendarData.IndexOf("BEGIN:VEVENT");
            int end = calendarData.IndexOf("END:VEVENT");
            if(start == end) {
                return;
            }
            calendarData = calendarData.Substring(start, end-start);
            var data = calendarData.Split('\n');
            foreach(string s in data) {
                var line = s.Split(':');
                if(line[0].StartsWith("UID")) { EventUID = line[1]; }
                if (line[0].StartsWith("CREATED")) { EventCreated = DateTime.Parse(line[1]); }
            }
        }
    }
}
