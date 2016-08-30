﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
                try {
                    CalendarEvent calEvent = App.GetDataContext().GetCalendarEvent((int)CalendarObjectId, vevent.Split('\n').ToList().Where(e => e.Split(':')[0] == "UID").First().Split(new char[] { ':' }, 2)[1]);
                    calEvent.CalendarObject = this;
                    calEvent.CalendarObjectId = (int)CalendarObjectId;

                    foreach(string data in vevent.Split('\n').ToList()) {
                        var kv = data.Split(new char[] { ':' }, 2);
                        if(kv[0].StartsWith("DTSTART")) {
                            DateTime startDate;
                            DateTime.TryParseExact(kv[1], DateTimeFormats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out startDate);
                            List<string> dtStartParams = kv[0].Split(';').ToList();
                            foreach (string dtStartParam in dtStartParams) {
                                if (dtStartParam.IndexOf("TZID=") > -1) {
                                    var tz = dtStartParam.Split('=')[1];
#if DEBUG
                                    tz = "Europe/Moscow";
#endif
                                }
                            };
                            calEvent.StartDate = startDate;
                        }
                        if (kv[0].StartsWith("DTEND")) {
                            DateTime endDate;
                            DateTime.TryParseExact(kv[1], DateTimeFormats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out endDate);
                            calEvent.EndDate = endDate;
                        }
                        if (kv[0].StartsWith("CREATED")) {
                            DateTime created;
                            DateTime.TryParseExact(kv[1], DateTimeFormats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out created);
                            calEvent.EventCreated = created;
                        }
                        if (kv[0].StartsWith("LAST-MODIFIED")) {
                            DateTime modified;
                            DateTime.TryParseExact(kv[1], DateTimeFormats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out modified);
                            calEvent.EventLastModified = modified;
                        }
                        if (kv[0].StartsWith("DURATION")) {
                            calEvent.Duration = kv[1];
                        }
                        if (kv[0].StartsWith("SUMMARY")) {
                            calEvent.Summary = kv[1];
                        }
                        if (kv[0].StartsWith("DESCRIPTION")) {
                            calEvent.Description = kv[1];
                        }
                        if (kv[0].StartsWith("LOCATION")) {
                            calEvent.Location = kv[1];
                        }
                        if (kv[0].StartsWith("RRULE")) {
                            RecurrenceRule rule = new RecurrenceRule();
                            foreach (string ruleString in kv[1].Split(';')) {
                                var rrulparam = ruleString.Split('=');
                                if(rrulparam[0] == "FREQ") {
                                    rule.Frequency = rrulparam[1];
                                }
                                if (rrulparam[0] == "BYDAY") {
                                    rule.Frequency = rrulparam[1];
                                }
                                if (rrulparam[0] == "BYMONTH") {
                                    rule.Frequency = rrulparam[1];
                                }
                                if (rrulparam[0] == "INTERVAL") {
                                    int parsed;
                                    if (Int32.TryParse(rrulparam[1], out parsed)) {
                                        rule.Interval = parsed;
                                    }
                                }
                                if (rrulparam[0] == "COUNT") {
                                    int parsed;
                                    if (Int32.TryParse(rrulparam[1], out parsed)) {
                                        rule.Count = parsed;
                                    }
                                }
                                if (rrulparam[0] == "UNTIL") {
                                    DateTime until;
                                    if (DateTime.TryParseExact(rrulparam[1], DateTimeFormats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out until)) {
                                        rule.Until = until;
                                    }
                                }
                            };

                            var inDatabase = calEvent.RecurrenceRules.Find(r => r.ByDay == rule.ByDay && r.ByMonth == rule.ByMonth && r.Interval == rule.Interval && r.Count == rule.Count && r.Frequency == rule.Frequency && r.Until == rule.Until);
                            if (inDatabase == null) {
                                rule.CalendarEvent = calEvent;
                                calEvent.RecurrenceRules.Add(rule);
                            }
                        }
                    }
                    App.GetDataContext().StoreCalendarEventAsync(calEvent);
                } catch (ArgumentNullException ex) {
                    continue;
                }
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

        private static String[] DateTimeFormats = {
            "yyyyMMddTHHmmssZ",
            "yyyyMMddTHHmmss",
            "yyyyMMdd",
            "yyyyMMddTHHmmssZzzz"
        };
    }
}
