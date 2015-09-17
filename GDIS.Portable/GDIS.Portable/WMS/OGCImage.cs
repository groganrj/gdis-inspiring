using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using AtlasOf.GIS;

namespace GDIS.Module.OGC
{
    public class OGCImage
    {
        // Fields
        public Uri address;
        public GISEnvelope BBOX = new GISEnvelope();
        public string filterProperty = "";
        public int HEIGHT = 400;
        public int WIDTH = 600;
        public string imageFormat = "";
        public string layerName = "";
        public string REQUEST = "GetMap";
        public string SERVICE = "WMS";
        public string VERSION = "1.3.1";
        public string CONFIG = "main";
        public string CRS = "EPSG:4326";
        public List<string> LAYERS = new List<string>();
        public List<string> STYLES = new List<string>();
        public string FORMAT = "image/jpg"; 
        public string BGCOLOR = "0xFFFFFF";
        public bool TRANSPARENT = false;
        public string EXCEPTIONS = "INIMAGE";
        public string QUALITY = "MEDIUM";

//        public Image Image;

        public override string ToString()
        {
            // http://demo.cubewerx.com/demo/cubeserv/cubeserv.cgi?CONFIG=main&SERVICE=WMS&VERSION=1.3.1&REQUEST=GetMap&CRS=EPSG%3A4326&BBOX=-100.6113118213863,-150.9169677320795,100.6113118213863,150.9169677320795&WIDTH=600&HEIGHT=400&LAYERS=GTOPO30%3AFoundation,POLBNDL_1M%3AFoundation,COASTL_1M%3AFoundation&STYLES=,,&FORMAT=image%2Fpng%3B+PhotometricInterpretation%3DRGB&BGCOLOR=0xFFFFFF&TRANSPARENT=FALSE&EXCEPTIONS=INIMAGE&QUALITY=MEDIUM
            StringBuilder request = new StringBuilder();


            return string.Format("CONFIG={0}&SERVICE={1}&VERSION={2}&REQUEST={3}&{4}&WIDTH={5}&HEIGHT={6}&LAYERS={7}&STYLES={8}&FORMAT={9}&BGCOLOR={10}&TRANSPARENT={11}&EXCEPTIONS={12}&QUALITY={13}", CONFIG, SERVICE, VERSION, REQUEST, BBOX, WIDTH, HEIGHT, string.Join(",", LAYERS.ToArray()), string.Join(",", STYLES.ToArray()), FORMAT, BGCOLOR, TRANSPARENT, EXCEPTIONS, QUALITY);
        }
    }
}
