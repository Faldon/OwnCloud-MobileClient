using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using System.Threading.Tasks;

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

        public async Task ParseCalendarData() {
            List<string> vevents = new List<string>();
            int lastStart = -1;
            while ((lastStart = CalendarData.IndexOf("BEGIN:VEVENT", ++lastStart)) > -1) {
                int lastEnd = CalendarData.IndexOf("END:VEVENT", lastStart);
                if (lastEnd > -1) {
                    vevents.Add(CalendarData.Substring(lastStart, lastEnd - lastStart + 10));
                }
            }
            foreach (string vevent in vevents) {
                try {
                    CalendarEvent calEvent = App.GetDataContext().GetCalendarEvent((int)CalendarObjectId, vevent.Split('\n').ToList().Where(e => e.Split(':')[0] == "UID").First().Split(new char[] { ':' }, 2)[1]);
                    calEvent.CalendarObject = this;
                    calEvent.CalendarObjectId = (int)CalendarObjectId;

                    foreach (string data in vevent.Split('\n').ToList()) {
                        var kv = data.Split(new char[] { ':' }, 2);
                        if (kv[0].StartsWith("DTSTART")) {
                            DateTime startDate;
                            DateTime.TryParseExact(kv[1], DateTimeFormats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out startDate);
                            List<string> dtStartParams = kv[0].Split(';').ToList();
                            foreach (string dtStartParam in dtStartParams) {
                                if (dtStartParam.IndexOf("TZID=") > -1) {
                                    var tz = dtStartParam.Split('=')[1];
                                    var cal = new Windows.Globalization.Calendar(new[] { "en-US" }, "GregorianCalendar", "24HourClock", tz);
                                    cal.SetDateTime(new DateTimeOffset(startDate));

                                    // Can't use cal.GetDateTime() because it always uses the local time zone.
                                    // Instead, get the local time from the calendar properties and use it to calculate the offset manually.
                                    var dt = new DateTime(cal.Year, cal.Month, cal.Day, cal.Hour, cal.Minute, cal.Second).AddTicks(cal.Nanosecond / 100);
                                    TimeSpan offset = dt - startDate.ToUniversalTime();
                                    DateTimeOffset d = new DateTimeOffset(startDate, offset);

                                    startDate = new DateTime(d.UtcDateTime.Ticks, DateTimeKind.Utc);
                                };
                            }
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
                            RecurrenceRule rule = new RecurrenceRule() {
                                Until = DateTime.MaxValue,
                            };
                            foreach (string ruleString in kv[1].Split(';')) {
                                var rrulparam = ruleString.Split('=');
                                if (rrulparam[0] == "FREQ") {
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
                            rule.CalendarEvent = calEvent;

                            //var inDatabase = calEvent.RecurrenceRules.Find(r => r.ByDay == rule.ByDay && r.ByMonth == rule.ByMonth && r.Interval == rule.Interval && r.Count == rule.Count && r.Frequency == rule.Frequency && r.Until == rule.Until);
                            if (calEvent.RecurrenceRules.Find(r => r.CalendarEvent == calEvent) == null) {
                                calEvent.RecurrenceRules.Add(rule);
                            }
                        }
                    }
                    calEvent.InSync = true;
                    await App.GetDataContext().StoreCalendarEventAsync(calEvent);
                } catch (ArgumentNullException ex) {
                    continue;
                }
            }
        }

        private static String[] DateTimeFormats = {
            "yyyyMMddTHHmmssZ",
            "yyyyMMddTHHmmss",
            "yyyyMMdd",
            "yyyyMMddTHHmmssZzzz"
        };
    }
}
