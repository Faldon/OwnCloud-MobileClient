using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nexcloud.Data
{
    [Table("CalendarEvents")]
    class CalendarEvent
    {
        [PrimaryKey, AutoIncrement]
        public int CalendarEventId { get; set; }
    }
}
