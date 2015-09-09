﻿using System;
using System.Data.Linq.Mapping;
using System.Reflection;
using Microsoft.Phone.Data.Linq.Mapping;
using System.IO;
using OwnCloud.Data.Calendar;

namespace OwnCloud.Data
{
    /// <summary>
    /// Stellt eine Kalendar Event zum speichern in der Datenbank da.
    /// </summary>
    [Table(Name = "TableEvent")]
    [Index(Columns = "Url", IsUnique = true, Name = "Event_name")]
    public class TableEvent : Entity
    {

        private int _eventId;
        
        private string _calendarData;
        private string _title;
        private string _url;
        private string _getETag;
        private DateTime _from;
        private DateTime _to;
        private bool _isFullDayEvent;
        private bool _isRecurringEvent;

        /// <summary>
        /// Primärschlüssel des Events
        /// </summary>
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int EventId
        {
            get { return _eventId; }
            set
            {
                if (_eventId == value) return;

                OnPropertyChanging("EventId");
                _eventId = value;
                OnPropertyChanged("EventId");
            }
        }
        
        
        
        #region parent calendar

        [Column]
        public int CalendarId
        {
            get { return _calendarId; }
            set
            {
                OnPropertyChanging("CalendarId");
                _calendarId = value;
                OnPropertyChanged("CalendarId");
            }
        }
        private int _calendarId = 0;
        //private TableCalendar _calendar;
        //[Association(Storage = "CalendarId", ThisKey = "CalendarId", OtherKey = "Id", IsForeignKey = true)]  
        //public TableCalendar Calendar
        //{
        //    get { return _calendar; }
        //    set { _calendar = value; }
        //}

        #endregion



        /// <summary>
        /// Die rohen Kalendardaten
        /// </summary>
        [Column]
        public string CalendarData
        {
            get { return _calendarData; }
            set
            {
                OnPropertyChanging("CalendarData");
                _calendarData = value;
                OnPropertyChanged("CalendarData");
            }
        }

        /// <summary>
        /// Die Url zu dem Events relativ zum Hostname.
        /// </summary>
        [Column]
        public string Url
        {
            get { return _url; }
            set
            {
                OnPropertyChanging("Url");
                _url = value;
                OnPropertyChanged("Url");
            }
        }

        /// <summary>
        /// Der Titel des Events
        /// </summary>
        [Column]
        public string Title
        {
            get { return _title; }
            set
            {
                OnPropertyChanging("Title");
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        /// <summary>
        /// Der ETag des Events
        /// </summary>
        [Column]
        public string GetETag
        {
            get { return _getETag; }
            set { OnPropertyChanging("GetETag"); _getETag = value; OnPropertyChanged("GetETag"); }
        }

        /// <summary>
        /// Startzeit des Events
        /// </summary>
        [Column]
        public DateTime From
        {
            get { return _from; }
            set { OnPropertyChanging("From"); _from = value; OnPropertyChanged("From"); }
        }

        /// <summary>
        /// Endzeit des Events
        /// </summary>
        [Column]
        public DateTime To
        {
            get { return _to; }
            set { OnPropertyChanging("To"); _to = value; OnPropertyChanged("To"); }
        }

        /// <summary>
        /// True, if the events needs to sync with the server. That occurs eg. when the event changed when there was no internet connection.
        /// </summary>
        [Column]
        public bool RequirePushUpdate
        {
            get; set;
        }

        /// <summary>
        /// True, if the event lasts the whole day.
        /// </summary>
        [Column]
        public bool IsFullDayEvent
        {
            get { return _isFullDayEvent; }
            set { OnPropertyChanging("IsFullDayEvent"); _isFullDayEvent = value; OnPropertyChanged("IsFullDayEvent"); }
        }

        /// <summary>
        /// True, if the event is a recurring event.
        /// </summary>
        [Column]
        public bool IsRecurringEvent
        {
            get { return _isRecurringEvent; }
            set { OnPropertyChanging("IsRecurringEvent"); _isRecurringEvent = value; OnPropertyChanged("IsRecurringEvent"); }
        }

        /// <summary>
        /// Erstellt ein Event für die Datenbank von einem Ungeparsten Kalender
        /// </summary>
        /// <param name="unparsedEvent"></param>
        /// <returns></returns>
        public static TableEvent FromUnparsedEvent(UnparsedEvent unparsedEvent)
        {
            return new TableEvent
                {
                    CalendarData = unparsedEvent.RawEventData
                    , Url = unparsedEvent.EventInfo.Url
                    , GetETag = unparsedEvent.EventInfo.GetETag
                };
        }

        /// <summary>
        /// Creates a new Event with Calendar data. The meta info properties (Like from and to) 
        /// are not set correctly. They dont match with the calendar data
        /// </summary>
        /// <returns></returns>
        public static TableEvent CreateNew()
        {
            var newEvent = new TableEvent
                {
                    IsFullDayEvent = true,
                    From = DateTime.Now.Date,
                    To = DateTime.Now.Date.AddDays(1)
                };

            using (var icalStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("Ocwp.Assets.NewCalendar.ics"))
            {
                if (icalStream != null)
                {
                    var reader = new StreamReader(icalStream);

                    newEvent.CalendarData = reader.ReadToEnd();
                }
                else
                    throw new Exception("New calendar not found");
            }

            return newEvent;
        }

    }
}
