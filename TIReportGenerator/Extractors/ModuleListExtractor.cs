using System;
using System.Collections.Generic;
using System.Linq;


namespace TIReportGenerator.Extractors
{
    public static class ModuleListExtractor
    {
        public static Protos.ModuleList Extract(IEnumerable<ModuleDataEntry> modules)
        {
            var unique = modules.Select(m => m.moduleTemplate.displayName).Distinct();
            var result = new Protos.ModuleList {};
            result.Modules.Add(
                unique.ToDictionary(m => m, m => modules.Count(o => o.moduleTemplate.displayName == m))
            );
            return result;
        }
    };
}