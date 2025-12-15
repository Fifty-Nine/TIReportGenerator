using System;
using System.Linq;
using System.Collections.Generic;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Util
{
    public static class GameEnums
    {
        public static IEnumerable<FactionResource> AllFactionResources()
        {
            return Enum.GetValues(typeof(FactionResource))
                       .Cast<FactionResource>()
                       .Where(v => v != FactionResource.None);
        }

        public static IEnumerable<TechCategory> AllTechCategories()
        {
            return Enum.GetValues(typeof(TechCategory))
                       .Cast<TechCategory>();
        }

        public static IEnumerable<CouncilorAttribute> AllCouncilorAttributes()
        {
            return Enum.GetValues(typeof(CouncilorAttribute))
                       .Cast<CouncilorAttribute>()
                       .Where(c => c != CouncilorAttribute.None);
        }
    };
}