using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("Calendars")]
    public class Calendar : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int CalendarId { get; set; }
    }
}
