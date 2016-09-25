using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using Nextcloud.Extensions;

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

        [Ignore]
        public bool IsFullDayEvent
        {
            get { return StartDate.ToLocalTime().TimeOfDay == new TimeSpan(0, 0, 0) && EndDate.ToLocalTime().TimeOfDay == new TimeSpan(0, 0, 0); }
        }

        public int GetReccurenceDates(DateTime firstDay, DateTime lastDay, out List<DateTime> startDates, out List<DateTime> endDates) {
            startDates = new List<DateTime>();
            endDates = new List<DateTime>();
            foreach (RecurrenceRule rrule in RecurrenceRules) {
                string rFrequency = rrule.Frequency;
                int rInterval = rrule.Interval;
                DateTime rEnd = rrule.Until;
                int rCount = rrule.Count;

                if (rCount == 0 && rEnd < firstDay) { continue; };
                if (rCount > 0) {
                    for (var i = 0; i < rCount; i++) {
                        switch (rFrequency) {
                            case "DAILY":
                                startDates.Add(StartDate.Date.AddDays(i * rInterval));
                                endDates.Add(EndDate.Date.AddDays(i * rInterval));
                                
                                break;
                            case "WEEKLY":
                                startDates.Add(StartDate.Date.AddDays(i * rInterval * 7));
                                endDates.Add(EndDate.Date.AddDays(i * rInterval * 7));
                                
                                break;
                            case "MONTHLY":
                                startDates.Add(StartDate.Date.AddMonths(i * rInterval));
                                endDates.Add(EndDate.Date.AddMonths(i * rInterval));
                                
                                break;
                            case "YEARLY":
                                startDates.Add(StartDate.Date.AddYears(i * rInterval));
                                endDates.Add(EndDate.Date.AddYears(i * rInterval));
                                
                                break;
                        }
                    }
                } else if (rEnd > firstDay) {
                    var currentDate = StartDate.ToLocalTime().Date;
                    var endDate = EndDate.ToLocalTime().Date;
                    switch (rFrequency) {
                        case "DAILY":
                            currentDate = currentDate.AddDays((firstDay - currentDate).TotalDays + (firstDay - currentDate).TotalDays % rInterval);
                            endDate = endDate.AddDays((firstDay - endDate).TotalDays + (firstDay - endDate).TotalDays % rInterval);
                            while (currentDate < StartDate) {
                                currentDate = currentDate.AddDays(rInterval);
                                endDate = endDate.AddDays(rInterval);
                            }
                            while (currentDate <= rEnd && currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                                currentDate = currentDate.AddDays(rInterval);
                                endDate = endDate.AddDays(rInterval);
                            }
                            break;
                        case "WEEKLY":
                            while (currentDate <= firstDay) {
                                currentDate = currentDate.AddDays(7 * rInterval);
                                endDate = endDate.AddDays(7 * rInterval);
                            }
                            while (currentDate <= rEnd && currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                                currentDate = currentDate.AddDays(7 * rInterval);
                                endDate = endDate.AddDays(7 * rInterval);
                            }
                            break;
                        case "MONTHLY":
                            while (currentDate.Date < firstDay.LastDayOfWeek().FirstOfMonth().Date) {
                                currentDate = currentDate.AddMonths(rInterval);
                                endDate = endDate.AddMonths(rInterval);
                            }
                            if (currentDate <= rEnd && currentDate >= firstDay && currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                            }
                            break;
                        case "YEARLY":
                            while (currentDate.Year < firstDay.LastDayOfWeek().FirstOfMonth().Year) {
                                currentDate = currentDate.AddYears(rInterval);
                                endDate = endDate.AddYears(rInterval);
                            }
                            if (currentDate <= rEnd && currentDate >= firstDay && currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                            }
                            break;
                    }
                } else if (rCount == 0 && rEnd == DateTime.MaxValue) {
                    var currentDate = StartDate.ToLocalTime().Date;
                    var endDate = EndDate.ToLocalTime().Date;
                    switch (rFrequency) {
                        case "DAILY":
                            currentDate = currentDate.AddDays((firstDay - currentDate).TotalDays + (firstDay - currentDate).TotalDays % rInterval);
                            endDate = endDate.AddDays((firstDay - endDate).TotalDays + (firstDay - endDate).TotalDays % rInterval);
                            while (currentDate < StartDate) {
                                currentDate = currentDate.AddDays(rInterval);
                                endDate = endDate.AddDays(rInterval);
                            }
                            while (currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                                currentDate = currentDate.AddDays(rInterval);
                                endDate = endDate.AddDays(rInterval);
                            }
                            break;
                        case "WEEKLY":
                            while (currentDate < firstDay) {
                                currentDate = currentDate.AddDays(7 * rInterval);
                                endDate = endDate.AddDays(7 * rInterval);
                            }
                            while (currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                                currentDate = currentDate.AddDays(7 * rInterval);
                                endDate = endDate.AddDays(7 * rInterval);
                            }
                            break;
                        case "MONTHLY":
                            while (currentDate.Date < firstDay.LastDayOfWeek().FirstOfMonth().Date) {
                                currentDate = currentDate.AddMonths(rInterval);
                                endDate = endDate.AddMonths(rInterval);
                            }
                            if (currentDate >= firstDay && currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                            }
                            break;
                        case "YEARLY":
                            while (currentDate.Year < firstDay.LastDayOfWeek().FirstOfMonth().Year) {
                                currentDate = currentDate.AddYears(rInterval);
                                endDate = endDate.AddYears(rInterval);
                            }
                            if (currentDate >= firstDay && currentDate <= lastDay) {
                                startDates.Add(currentDate);
                                endDates.Add(endDate);
                            }
                            break;
                    }
                }
            }
            return startDates.Count;
        }
    }
}
