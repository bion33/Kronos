using System;

namespace Console.Utilities
{
    public static class TimeUtil
    {
        public static double ToPosixTime(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static DateTime Now()
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/New_York");
        }

        public static int HourNow()
        {
            return Now().Hour;
        }

        public static double PosixToday()
        {
            return ToPosixTime(Now().Date);
        }
    }
}