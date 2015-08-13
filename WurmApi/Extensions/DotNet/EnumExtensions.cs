﻿using System;

namespace AldursLab.WurmApi.Extensions.DotNet
{
    public static class EnumExtensions
    {
        public static bool IsDefined<TEnum>(TEnum enumValue)
        {
            return Enum.IsDefined(typeof(TEnum), enumValue);
        }

        public static void ValidateIsDefined<TEnum>(TEnum enumValue)
        {
            if (!IsDefined<TEnum>(enumValue))
                throw new ArgumentOutOfRangeException("enumValue", "Enumeration value " + enumValue + " is not defined.");
        }
    }
}