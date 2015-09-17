using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AtlasOf.GIS.ESRI
{
    public class EsriLegend : GISLegend
    {
        public EsriLegendLayer[] layers { get; set; }
    }

    public class EsriLegendLayer
    {
        public int layerId { get; set; }
        public string layerName { get; set; }
        public string layerType { get; set; }
        public int minScale { get; set; }
        public int maxScale { get; set; }
        public EsriLegendLegend[] legend { get; set; }
    }

    public class EsriLegendLegend
    {
        public string label { get; set; }
        public string url { get; set; }
        public string imageData { get; set; }
        public string contentType { get; set; }

        public string Name { get { return label; } }

        public byte[] ImageData
        {
            get
            {
                return Convert.FromBase64String(imageData);
            }
        }

        //public string ImageUrl(string baseUrl)
        //{
        //    //http://gis2.metc.state.mn.us/ArcGIS/rest/services/cd/landuse_2010_tile/MapServer/0/images/E737F8B6
        // return string.Format("http://gis2.metc.state.mn.us/ArcGIS/rest/services/cd/landuse_2010_tile/MapServer/0/images/E737F8B6;
        //}
    }
}