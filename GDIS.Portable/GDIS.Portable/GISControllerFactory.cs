using System;
using System.Collections.Generic;
using System.Text;
using AtlasOf.GIS.ESRI;
using AtlasOf.GIS.WMS;
//using AtlasOf.GIS.Bing;
//using AtlasOf.GIS.Google;
//using AtlasOf.GIS.MapQuest;
//using AtlasOf.GIS.Yahoo;

namespace AtlasOf.GIS
{
    public class GISControllerFactory
    {
        public static GISController Create(GISServer server)
        {
            if (server.Type == null) return new EsriRESTController(server);

            switch (server.Type.ToUpper())
            {
                case "ESRI":
                    return new EsriController(server);
                case "ESRI_REST":
                case "ESRI REST":
                    return new EsriRESTController(server);
                case "OGC":
                case "WMS":
                case "WFS":
                    return new OGCController(server);
                //case "Yahoo":
                //    return new Yahoo.YahooController(server);
                //case "BING":
                //    return new BingController(server);
                //case "GOOGLE":
                //    return new GoogleController(server);
                //case "BingTiled":
                //    return new BingTiled.BingTiledController(server);
                //case "OSM":
                //case "OPEN STREET":
                //    return new OSM.OpenStreetController(server);
                //case "MAPQUEST":
                //    return new MapQuest.MapQuestController(server);
                //case "OpenStreetTiled":
                //    return new OSM_Tiled.OpenStreetController(server);
                default:
                    throw new ArgumentException(server.Type + " is unknown", "server");
            }
        }
    }
}
