using System;
using TimeZoneConverter;

namespace Kronos.Utilities
{
    /// <summary>
    ///     Common utilities for working with time, timezones and (calculated) timestamps.
    ///     Note:
    ///     It is preferable to work in this timezone, because updates happen at a fixed offset from the start of the
    ///     day (in this timezone). The server respects DST changes, making calculating offsets in UTC or local
    ///     timezones a hassle. Keep this in mind when working with Unix Time also, simply adding a fixed amount of
    ///     hours won't work on the day DST goes into or out of effect. To be safe, convert any time to EST, then
    ///     manipulate it, then convert it back to the required format / timezone.
    /// </summary>
    public static class TimeUtil
    {
        /// <summary> Timezone of the NationStates server. </summary>
        private static readonly TimeZoneInfo Tz = TZConvert.GetTimeZoneInfo("America/New_York");

        /// <summary> The Unix epoch </summary>
        public static DateTime Epoch()
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary> The current time, from the perspective of the NationStates server </summary>
        public static DateTime Now()
        {
            return UtcToNs(DateTime.UtcNow);
        }

        /// <summary> The current Unix time (seconds since Unix Epoch) </summary>
        public static double UnixNow()
        {
            return DateTime.UtcNow.Subtract(Epoch()).TotalSeconds;
        }

        /// <summary> The current date </summary>
        public static DateTime Today()
        {
            return Now().Date;
        }

        /// <summary> The current date as YYYY-MM-DD </summary>
        public static string DateForPath()
        {
            return $"{Today().Year:0000}-{Today().Month:00}-{Today().Day:00}";
        }

        /// <summary> The start of "today", from the perspective of the NS server, in Unix time </summary>
        public static double UnixToday()
        {
            return NsToUtc(Today()).Subtract(Epoch()).TotalSeconds;
        }

        /// <summary> The start of "yesterday", from the perspective of the NS server, in Unix time </summary>
        public static double UnixYesterday()
        {
            return NsToUtc(Today().AddDays(-1)).Subtract(Epoch()).TotalSeconds;
        }

        /// <summary>
        ///     The start of the last major update. If a major update is currently happening, it is not considered by
        ///     this method, instead yesterday's major update is used.
        /// </summary>
        public static double UnixLastMajorStart()
        {
            if (Today().AddHours(3) < Now()) return NsToUtc(Today()).Subtract(Epoch()).TotalSeconds;

            return NsToUtc(Today().AddDays(-1)).Subtract(Epoch()).TotalSeconds;
        }

        /// <summary>
        ///     The definite end of the last major update. If a major update is currently happening, it is not
        ///     considered by this method, instead yesterday's major update is used.
        ///     To be safe, the end of a major update is taken as 3 hours after its start.
        /// </summary>
        public static double UnixLastMajorEnd()
        {
            return UnixLastMajorStart() + 3 * 3600;
        }

        /// <summary> The start of the current or upcoming major update. This method takes DST changes into account. </summary>
        public static double UnixNextMajorStart()
        {
            if (Today().AddHours(3) < Now()) return NsToUtc(Today().AddDays(1)).Subtract(Epoch()).TotalSeconds;

            return NsToUtc(Today()).Subtract(Epoch()).TotalSeconds;
        }

        /// <summary>
        ///     The start of the last minor update. If a minor update is currently happening, it is not considered by
        ///     this method, instead yesterday's minor update is used.
        /// </summary>
        public static double UnixLastMinorStart()
        {
            if (Today().AddHours(14) < Now()) return NsToUtc(Today().AddHours(12)).Subtract(Epoch()).TotalSeconds;

            return NsToUtc(Today().AddDays(-1).AddHours(12)).Subtract(Epoch()).TotalSeconds;
        }

        /// <summary>
        ///     The definite end of the last minor update. If a minor update is currently happening, it is not
        ///     considered by this method, instead yesterday's minor update is used.
        ///     To be safe, the end of a minor update is taken as 2 hours after its start.
        /// </summary>
        public static double UnixLastMinorEnd()
        {
            return UnixLastMinorStart() + 2 * 3600;
        }

        /// <summary> The start of the current or upcoming minor update. This method takes DST changes into account. </summary>
        public static double UnixNextMinorStart()
        {
            if (Today().AddHours(14) < Now())
                return NsToUtc(Today().AddDays(1).AddHours(12)).Subtract(Epoch()).TotalSeconds;

            return NsToUtc(Today().AddHours(12)).Subtract(Epoch()).TotalSeconds;
        }

        /// <summary> Convert time in the NS server's timezone to UTC </summary>
        public static DateTime NsToUtc(DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, Tz);
        }

        /// <summary> Convert time from UTC to the NS server's timezone </summary>
        public static DateTime UtcToNs(DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, Tz);
        }

        /// <summary> Convert a Unix timestamp to a HH:MM:SS offset since its corresponding update </summary>
        public static string ToUpdateOffset(double unixTime)
        {
            var dt = UtcToNs(Epoch().AddSeconds(unixTime));

            var hours = dt.Hour > 3 ? dt.Hour - 12 : dt.Hour;
            var minutes = dt.Minute;
            var seconds = dt.Second;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        /// <summary> Convert an amount of seconds to HH:MM:SS format </summary>
        public static string ToHms(double seconds)
        {
            var sign = seconds < 0 ? "-" : " ";
            seconds = Math.Abs(seconds);

            var hours = (int) (seconds / 3600);
            seconds -= hours * 3600;
            var minutes = (int) (seconds / 60);
            seconds -= minutes * 60;
            seconds = (int) seconds;

            return $"{sign}{hours:00}:{minutes:00}:{seconds:00}";
        }
    }
}