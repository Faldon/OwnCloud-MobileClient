using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("CalendarEvents")]
    public class CalendarEvent
    {
        [PrimaryKey, AutoIncrement]
        public int CalendarEventId { get; set; }
    }
}
