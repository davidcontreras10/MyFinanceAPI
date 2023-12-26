using System;

namespace MyFinanceModel
{
    public class DateTimeParam
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Minute { get; set; }
        public int Hour { get; set; }
        public int Seconds { get; set; }
        public int Milliseconds { get; set; }

        public DateTime GetDateTime()
        {
            return new DateTime(Year, Month, Day, Hour, Minute, Seconds);
        }

        public DateTime GetDate()
        {
            return new DateTime(Year, Month, Day);
        }

        public static DateTimeParam CreateDateTimeParam(DateTime dateTime)
        {
            return new DateTimeParam
            {
                Day = dateTime.Day,
                Hour = dateTime.Hour,
                Minute = dateTime.Minute,
                Month = dateTime.Month,
                Milliseconds = dateTime.Millisecond,
                Seconds = dateTime.Second,
                Year = dateTime.Year
            };
        }
    }
}