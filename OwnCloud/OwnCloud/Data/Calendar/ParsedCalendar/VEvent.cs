using System;

namespace OwnCloud.Data.Calendar.ParsedCalendar
{
    /// <summary>
    /// represents a ical event
    /// </summary>
    public class VEvent
    {
        private string _description = "";
        private string _location;

        public string Title { get; set; }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public bool IsFullDayEvent { get; set; }

        public string ETag { get; set; }

        public bool IsRecurringEvent { get; set; }
    }
}
