using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    public static class ResourceCostExtractor
    { 
        public static IEnumerable<FactionResource> AllFactionResources()
        {
            return Enum.GetValues(typeof(FactionResource))
                    .Cast<FactionResource>()
                    .Where(v => v != FactionResource.None);
        }
        
        public static Dictionary<FactionResource, float> ResourceCostToDictionary(TIResourcesCost c)
        {
            var values = AllFactionResources()
                            .Select(v => (Resource: v, Cost: c.GetSingleCostValue(v)))
                            .Where(p => p.Cost >= 0.05f || p.Cost <= -0.05f)
            ;
            return values.ToDictionary(
                p => p.Resource,
                p => p.Cost
            );
        }
        
        public static Protos.ResourceCost Extract(TIResourcesCost cost)
        {
            var result = new Protos.ResourceCost { };
            result.Costs.Add(ResourceCostToDictionary(cost).ToDictionary(kvp => TIUtilities.GetResourceString(kvp.Key), kvp => kvp.Value));
            return result;
        }
    };
}