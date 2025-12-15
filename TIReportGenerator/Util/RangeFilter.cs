using System;
using System.Linq;
using System.Collections.Generic;

namespace TIReportGenerator.Util
{
    public static class RangeFilters
    {
        public static bool IsNonzeroPercentage(float v)
        {
            return Math.Abs(v) > 0.00001f;
        }

        public static bool IsNonzeroValue(float v)
        {
            return Math.Abs(v) > 0.005f;
        }

        private static IEnumerable<T> Exclude<T>(IEnumerable<T> c, Func<T, float> fn, float mag)
        {
            return c.Where(v => Math.Abs(fn(v)) > mag);
        }

        public static IEnumerable<T> ExcludeZeroValues<T>(this IEnumerable<T> c, Func<T, float> fn)
        {
            return Exclude(c, fn, 0.005f);
        }

        public static IEnumerable<T> ExcludeZeroPercent<T>(this IEnumerable<T> c, Func<T, float> fn)
        {
            return Exclude(c, fn, 0.00001f);
        }
    };
}