using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AtlasOf.GIS
{
    public interface IGISLegend 
    {
        string LegendUrl { get; set; }
    }

    public class GISLegend : IGISLegend
    {
        public string LegendUrl { get; set; }
    }
}
