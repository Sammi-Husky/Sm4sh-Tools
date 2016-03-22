using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public static class DateTimeExtension
    {
        public static DateTime FromUnixBytes(uint stamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(stamp).ToLocalTime();
            return dtDateTime;
        }

        public static uint ToUnixBytes(this DateTime time)
        {
            return (uint)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
