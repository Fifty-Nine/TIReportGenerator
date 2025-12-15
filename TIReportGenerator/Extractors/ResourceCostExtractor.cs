using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class ResourceCostExtractor
    {
        public static Dictionary<FactionResource, float> ResourceCostToDictionary(TIResourcesCost c)
        {
            var values = GameEnums.AllFactionResources()
                            .Select(v => (Resource: v, Cost: c.GetSingleCostValue(v)))
                            .ExcludeZeroValues(p => p.Cost)
            ;
            return values.ToDictionary(
                p => p.Resource,
                p => p.Cost
            );
        }

        public static IDictionary<string, float> Extract(TIResourcesCost cost)
        {
            return ResourceCostToDictionary(cost).ToDictionary(kvp => TIUtilities.GetResourceString(kvp.Key), kvp => kvp.Value);
        }
    };
}