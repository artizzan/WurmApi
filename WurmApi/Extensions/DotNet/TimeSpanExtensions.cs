﻿using System;

namespace AldursLab.WurmApi.Extensions.DotNet
{
    static class TimeSpanExtensions
    {
        public static TimeSpan StripMilliseconds(this TimeSpan timeSpan)
        {
            return new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        /// <summary>
        /// Multiplies by an integer value.
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan multiplicand, int multiplier)
        {
            return TimeSpan.FromTicks(multiplicand.Ticks * multiplier);
        }

        /// <summary>
        /// Multiplies by a double value.
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan multiplicand, double multiplier)
        {
            return TimeSpan.FromTicks((long)(multiplicand.Ticks * multiplier));
        }
    }
}
