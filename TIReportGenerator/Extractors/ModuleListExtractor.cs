using System;
using System.Collections.Generic;
using System.Linq;


namespace TIReportGenerator.Extractors
{
    public static class ModuleListExtractor
    {
        public static Protos.ModuleList Extract<T>(IEnumerable<T> modules, Func<T, string> getName)
        {
            var unique = modules.Select(getName).Distinct();
            var result = new Protos.ModuleList {};
            result.Modules.Add(
                unique.ToDictionary(m => m, m => modules.Count(o => getName(o) == m))
            );
            return result;
        }
    };
}