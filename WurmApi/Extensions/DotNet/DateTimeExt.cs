﻿using System;

namespace AldursLab.WurmApi.Extensions.DotNet
{
    static class DateTimeExt
    {
        /// <summary>
        /// Checks if DateTime represents a moment in time, that is today, based on local time.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static bool IsToday(this DateTime dateTime)
        {
            return dateTime.Date == Time.Get.LocalNow.Date;
        }
    }
}