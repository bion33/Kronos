using System;
using TimeZoneConverter;

namespace Console.Utilities
{
    public static class TimeUtil
    {
        private static readonly TimeZoneInfo Tz = TZConvert.GetTimeZoneInfo("America/New_York");
        
        public static DateTime Epoch() => new DateTime(1970, 1, 1, 0,0,0, DateTimeKind.Utc);
        
        public static DateTime Now()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);
        }
        
        public static double PosixNow() => EstToUtc(Now()).Subtract(Epoch()).TotalSeconds;

        public static DateTime Today() => Now().Date;

        public static double PosixToday() => EstToUtc(Today()).Subtract(Epoch()).TotalSeconds;

        public static double PosixYesterday() => EstToUtc(Today().AddDays(-1)).Subtract(Epoch()).TotalSeconds;
        
        public static double PosixLastMajorStart()
        {
            if (Today().AddHours(3) < Now())
            {
                return EstToUtc(Today()).Subtract(Epoch()).TotalSeconds;
            }
            
            return EstToUtc(Today().AddDays(-1)).Subtract(Epoch()).TotalSeconds;
        }

        public static double PosixLastMajorEnd()
        {
            return PosixLastMajorStart() + (3 * 3600);
        }

        public static double PosixLastMinorStart()
        {
            if (Today().AddHours(14) < Now())
            {
                return EstToUtc(Today().AddHours(12)).Subtract(Epoch()).TotalSeconds;
            }
            
            return EstToUtc(Today().AddDays(-1).AddHours(12)).Subtract(Epoch()).TotalSeconds;
        }

        public static double PosixLastMinorEnd()
        {
            return PosixLastMinorStart() + (2 * 3600);
        }

        public static DateTime EstToUtc(DateTime dateTime) => TimeZoneInfo.ConvertTimeToUtc(dateTime, Tz);

        public static DateTime UtcToEst(DateTime dateTime) => TimeZoneInfo.ConvertTimeFromUtc(dateTime, Tz);

        public static string ToUpdateOffset(double posix)
        {
            var dt = UtcToEst(Epoch().AddSeconds(posix));

            var hours = (dt.Hour > 3) ? dt.Hour - 12 : dt.Hour;
            var minutes = dt.Minute;
            var seconds = dt.Second;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }
    }
}