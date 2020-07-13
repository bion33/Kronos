using System;
using TimeZoneConverter;

namespace Console.Utilities
{
    public static class TimeUtil
    {
        private static readonly TimeZoneInfo Tz = TZConvert.GetTimeZoneInfo("America/New_York");

        public static DateTime Epoch()
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static DateTime Now()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);
        }

        public static double PosixNow()
        {
            return EstToUtc(Now()).Subtract(Epoch()).TotalSeconds;
        }

        public static DateTime Today()
        {
            return Now().Date;
        }

        public static string DateForPath()
        {
            return $"{Today().Year:0000}-{Today().Month:00}-{Today().Day:00}";
        }

        public static double PosixToday()
        {
            return EstToUtc(Today()).Subtract(Epoch()).TotalSeconds;
        }

        public static double PosixYesterday()
        {
            return EstToUtc(Today().AddDays(-1)).Subtract(Epoch()).TotalSeconds;
        }

        public static double PosixLastMajorStart()
        {
            if (Today().AddHours(3) < Now()) return EstToUtc(Today()).Subtract(Epoch()).TotalSeconds;

            return EstToUtc(Today().AddDays(-1)).Subtract(Epoch()).TotalSeconds;
        }
        
        public static double PosixThisMajorStart()
        {
            if (Today().AddHours(3) < Now()) return EstToUtc(Today().AddDays(1)).Subtract(Epoch()).TotalSeconds;

            return EstToUtc(Today()).Subtract(Epoch()).TotalSeconds;
        }

        public static double PosixLastMajorEnd()
        {
            return PosixLastMajorStart() + 3 * 3600;
        }

        public static double PosixLastMinorStart()
        {
            if (Today().AddHours(14) < Now()) return EstToUtc(Today().AddHours(12)).Subtract(Epoch()).TotalSeconds;

            return EstToUtc(Today().AddDays(-1).AddHours(12)).Subtract(Epoch()).TotalSeconds;
        }
        
        public static double PosixThisMinorStart()
        {
            if (Today().AddHours(14) < Now()) return EstToUtc(Today().AddDays(1).AddHours(12)).Subtract(Epoch()).TotalSeconds;

            return EstToUtc(Today().AddHours(12)).Subtract(Epoch()).TotalSeconds;
        }

        public static double PosixLastMinorEnd()
        {
            return PosixLastMinorStart() + 2 * 3600;
        }

        public static DateTime EstToUtc(DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, Tz);
        }

        public static DateTime UtcToEst(DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, Tz);
        }

        public static string ToUpdateOffset(double posix)
        {
            var dt = UtcToEst(Epoch().AddSeconds(posix));

            var hours = dt.Hour > 3 ? dt.Hour - 12 : dt.Hour;
            var minutes = dt.Minute;
            var seconds = dt.Second;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        public static string ToHms(double seconds)
        {
            var hours = (int) (seconds / 3600);
            seconds -= hours * 3600;
            var minutes = (int) (seconds / 60);
            seconds -= minutes * 60;
            seconds = (int) seconds;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }
    }
}