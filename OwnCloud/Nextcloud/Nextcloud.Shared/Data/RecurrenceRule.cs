using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Nextcloud.Extensions;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using System.Text.RegularExpressions;

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

        public bool IsApplyingOn(DateTime date) {
            if((ByDay ?? "").ToString().Length > 0) {
                List<string> daysApplying = ByDay.Split(',').ToList();
                string abbrWkDay = date.FormatDate("{dayofweek.abbreviated(2)}", new[] { "en" }).ToUpper();
                Regex regex = new Regex(@"((?:[0-9\+\-,])*)(" + abbrWkDay + ")$");
                string match = daysApplying.Where(s => regex.IsMatch(s)).FirstOrDefault();
                if (match != null) {
                    Match captured = regex.Match("1MO");
                    System.Diagnostics.Debug.WriteLine(regex.IsMatch("1MO"));
                    if (captured.Groups.Count == 1) {
                        return true;
                    }

                }
                Regex.Match(abbrWkDay, @"(\d*)abc");
                System.Diagnostics.Debug.WriteLine(date.FormatDate("{dayofweek.abbreviated(2)}", new[] { "en" }).ToUpper());
            }
            return true;
        }

    }
}
