using System;

namespace ZapDICOMCleaner.Extensions
{
    public static class StringExtension
    {
        public static DateTime ToDateTime(this string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 8)
            { 
                return default(DateTime);
            }

            int year = int.Parse(value.Substring(0, 4));
            int month = int.Parse(value.Substring(4, 2));
            int day = int.Parse(value.Substring(6, 2));

            return new DateTime(year, month, day);
        }
    }
}
