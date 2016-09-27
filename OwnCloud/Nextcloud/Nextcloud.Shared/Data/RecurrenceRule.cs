using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("RecurrenceRules")]
    public class RecurrenceRule : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int RuleId { get; set; }

        [NotNull]
        public string Frequency { get; set; }

        [NotNull, Default(value:1)]
        public int Interval { get; set; }

        public int Count { get; set; }

        public string ByMonth { get; set; }

        public string ByDay { get; set; }

        public string ByMonthDay { get; set; }

        public DateTime Until { get; set; }

        [ForeignKey(typeof(CalendarEvent))]
        public int CalendarEventId { get; set; }

        [ManyToOne]
        public CalendarEvent CalendarEvent { get; set; }

    }
}
