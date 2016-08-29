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

        [Ignore]
        public bool IsRecurringEvent
        {
            get { return RecurrenceRules != null && RecurrenceRules.Count > 0; }
        }
    }
}
