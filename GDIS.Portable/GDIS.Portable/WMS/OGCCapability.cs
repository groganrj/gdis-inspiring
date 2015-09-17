using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using AtlasOf.GIS;
using System.Linq;
using System.Xml.Linq;

namespace AtlasOf.GIS.WMS
{
    public partial class OGCStyle
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public string Abstract { get; set; }

        public string LegendURL { get; set; }

        public string StyleSheetURL { get; set; }

        public string StyleURL { get; set; }
    }

    public class OGCService : GISService
    {
        //internal string _getCapabilitiesUrl;
        internal string _getMapUrl;
        internal string _getFeatureUrl;

        public OGCService(string serviceName)
        {
            _serviceName = serviceName;
        }

        internal static OGCService Create(XElement layer)
        {
            GISEnvelope env = new GISEnvelope();

            OGCService service = new OGCService("");

            //            <CRS>CRS:84</CRS>
            //<EX_GeographicBoundingBox>
            //    <westBoundLongitude>-180.0</westBoundLongitude>
            //    <eastBoundLongitude>180.0</eastBoundLongitude>
            //    <southBoundLatitude>-90.0</southBoundLatitude>
            //    <northBoundLatitude>90.0</northBoundLatitude>
            //</EX_GeographicBoundingBox>

            foreach (XElement el in layer.Elements())
            {
                if (el.Name == "Name")
                {
                    service._serviceName = layer.Element("Name").Value;
                }
                else if (el.Name == "Title")
                {
                    service._description = el.Value;
                }
                else if (el.Name == "SRS")
                {
                    env.CoordinateSystem = el.Value;
                }
                else if (el.Name == "CRS")
                {
                    env.CoordinateSystem = el.Value;
                }
                else if (el.Name == "LatLonBoundingBox")
                {
                    env.minX = double.Parse(el.Attribute("minx").Value);
                    env.maxX = double.Parse(el.Attribute("maxx").Value);
                    env.minY = double.Parse(el.Attribute("miny").Value);
                    env.maxY = double.Parse(el.Attribute("maxy").Value);
                }
                else if (el.Name == "BoundingBox")
                {
                    env.minX = double.Parse(el.Attribute("minx").Value);
                    env.maxX = double.Parse(el.Attribute("maxx").Value);
                    env.minY = double.Parse(el.Attribute("miny").Value);
                    env.maxY = double.Parse(el.Attribute("maxy").Value);
                    env.CoordinateSystem = el.Attribute("SRS") != null ? el.Attribute("SRS").Value : el.Attribute("CRS").Value;
                }
                else if (el.Name == "Layer")
                {
                    service._baseLayers.Add(OGCLayer.Create(el));
                }
            }

            service._baseExtent = env;
            return service;
        }
    }

    public class OGCFeatureType : GISLayerInfo
    {
        public string Abstract { get; set; }
    }

    public class OGCLayer : GISLayerInfo
    {
        public OGCLayer() { }

        public OGCLayer(string serviceName, string serviceId)
        {
            _id = serviceId;
            _name = serviceName;
            Style = new List<OGCStyle>();
        }

        public int FixedHeight { get; set; }
        public int FixedWidth { get; set; }
        public string LegendUrl { get; set; }

        public List<OGCStyle> Style { get; set; }

        internal static GISLayerInfo Create(GISService activeService, GISEnvelope gISEnvelopes)
        {
            return new OGCLayer() { _id = activeService._serviceName, _isVisible = activeService.IsEnabled, _name = activeService.Name, _baseExtent = gISEnvelopes };
        }

        #region ICloneable Members

        public object Clone()
        {
            OGCLayer newLayer = new OGCLayer();
            newLayer._baseExtent = this._baseExtent;
            newLayer._childLayers = this._childLayers;
            newLayer._Fields = this._Fields;
            newLayer._id = this._id;
            newLayer._isVisible = this._isVisible;
            newLayer._maxscale = this._maxscale;
            newLayer._minscale = this._minscale;
            newLayer._name = this._name;
            newLayer._type = this._type;
            newLayer.LegendUrl = this.LegendUrl;
            newLayer.Style = this.Style;

            return newLayer;
        }

        #endregion

        internal static GISLayerInfo Create(XElement layer)
        {
            GISEnvelope env = new GISEnvelope();
            OGCLayer service = new OGCLayer();

            foreach (XElement el in layer.Elements())
            {
                if (el.Name == "Name")
                {
                    service._id = el.Value;
                }
                else if (el.Name == "Title")
                {
                    service._name = el.Value;
                }
                else if (el.Name == "SRS")
                {
                    env.CoordinateSystem = el.Value;
                }
                else if (el.Name == "LatLonBoundingBox")
                {
                    env.minX = double.Parse(el.Attribute("minx").Value);
                    env.maxX = double.Parse(el.Attribute("maxx").Value);
                    env.minY = double.Parse(el.Attribute("miny").Value);
                    env.maxY = double.Parse(el.Attribute("maxy").Value);
                }
                else if (el.Name == "BoundingBox")
                {
                    env.minX = double.Parse(el.Attribute("minx").Value);
                    env.maxX = double.Parse(el.Attribute("maxx").Value);
                    env.minY = double.Parse(el.Attribute("miny").Value);
                    env.maxY = double.Parse(el.Attribute("maxy").Value);
                    env.CoordinateSystem = el.Attribute("SRS").Value;
                }
                else if (el.Name == "Style")
                {
                    OGCStyle style = new OGCStyle() { Name = el.Element("Name").Value };
                    style.Abstract = el.Elements(XName.Get("Abstract")).First().Value;
                    style.Title = el.Elements(XName.Get("Title")).First().Value;

                    if (el.Elements(XName.Get("LegendURL")).Count() > 0)
                    {
                        style.LegendURL = GetHrefFromResource(el.Elements(XName.Get("LegendURL")).First());
                    }

                    service.Style.Add(style);
                }
            }

            service._baseExtent = env;
            return service;
        }

        private static string GetHrefFromResource(XElement element)
        {
            if (element.Elements(XName.Get("OnlineResource")).Count() > 0)
            {
                foreach (XAttribute attr in element.Elements(XName.Get("OnlineResource")).First().Attributes())
                {
                    if (attr.Name.LocalName == "href") return attr.Value;
                }
            }
            return string.Empty;
        }
    }

    public class OGCEnvelope : GISEnvelope
    {
        public OGCEnvelope() { }

        public OGCEnvelope(GISEnvelope env) : base(env.minX, env.maxX, env.minY, env.MaxY) { CoordinateSystem = env.CoordinateSystem; }

        public override string ToString()
        {
            return string.Format("SRS={0}&CRS={0}&BBOX={1},{2},{3},{4}", CoordinateSystem, minX, minY, maxX, maxY);
        }

        public static OGCEnvelope Parse(XElement element)
        {
            //<BoundingBox CRS="CRS:84" minx="-111.4" miny="43.868" maxx="-109.4" maxy="45.4"/>
            OGCEnvelope env = new OGCEnvelope()
            {
                maxX = double.Parse(element.Attribute(XName.Get("maxx")).Value),
                minX = double.Parse(element.Attribute(XName.Get("minx")).Value),
                maxY = double.Parse(element.Attribute(XName.Get("maxy")).Value),
                minY = double.Parse(element.Attribute(XName.Get("miny")).Value)
            };

            if (element.Attributes(XName.Get("CRS")).Count() > 0)
            {
                env.CoordinateSystem = element.Attribute(XName.Get("CRS")).Value;
            }
            else if (element.Attributes(XName.Get("SRS")).Count() > 0)
            {
                env.CoordinateSystem = element.Attribute(XName.Get("SRS")).Value;
            }

            return env;
        }
    }

    #region OGCResponse
    public class OGCImageResponse : GISImageResponse
    {
        public override List<GISLayerInfo> GetLayerInfo()
        {
            List<GISLayerInfo> layerInfo = new List<GISLayerInfo>();

            foreach (GISLayerInfo layer in _layers)
            {
                layerInfo.Add(layer);
                //                layerInfo.Add(new GISLayerInfo() { Name = layer.Name, Id = layer.Id, Type = layer.LayerType.ToString() });
            }

            return layerInfo;
        }

        // starts at IMAGE begin element
        public static OGCImageResponse ProcessImageReturn(string requestString, GISEnvelope responseEnvelope)
        {
            OGCImageResponse response = new OGCImageResponse();
            response.LastRequest = requestString;
            return response;
        }
    }

    public class OGCField : GISField
    {
        public OGCField(string name, string fvalue) : base(name, fvalue) { }

        public OGCField(string name)
        {
            _fieldName = name;
        }
    }

    public class OGCFeature : GISFeature
    {
        //internal static OGCFeature ProcessFeature(XmlTextReader responseReader)
        //{
        //    OGCFeature feature = new OGCFeature();

        //    while (responseReader.Read())
        //    {
        //        if (responseReader.NodeType == XmlNodeType.Element)
        //        {
        //            switch (responseReader.LocalName)
        //            {
        //            }
        //        }
        //        else if (responseReader.NodeType == XmlNodeType.EndElement)
        //        {
        //            if (responseReader.LocalName == "FEATURE") break;
        //        }
        //    }

        //    return feature;
        //}
        internal static GISFeature CreateSearchResult(Nii.JSON.JSONObject obj)
        {
            throw new NotImplementedException();
        }

        internal static GISFeature ProcessResult(XElement element)
        {
            GISFeature feature = new GISFeature();

            //<FIELDS CNTRY_NAME="United States" COLOR_MAP="5" CURR_CODE="USD" CURR_TYPE="US Dollar" FIPS_CNTRY="US" GMI_CNTRY="USA" ISO_2DIGIT="US" ISO_3DIGIT="USA" LANDLOCKED="N" LONG_NAME="United States" POP_CNTRY="258833000" SOVEREIGN="United States" SQKM="9449365" SQMI="3648399.75" _ID_="135" _LAYERID_="world_countries" _SHAPE_="[Geometry]"/>
            foreach (var xa in element.Attributes())
            {
                if (feature.Fields.Count == 0)
                {
                    feature.Fields.Add(new GISField("Title", xa.Value));
                }
                feature.Fields.Add(new GISField(xa.Name.LocalName, xa.Value));
            }

            return feature;
        }
    }

    public class OGCServiceResponse : OGCFeatureResponse
    {
        private GISServer _server;

        public GISServer Server
        {
            get { return _server; }
            set { _server = value; }
        }

        //internal static GISResponse ProcessServiceReturn(XmlTextReader responseReader, GISServer server, string requestString, string responseString)
        //{
        //    OGCServiceResponse response = new OGCServiceResponse();
        //    response.LastRequest = requestString;
        //    response.LastResponse = responseString;

        //    while (responseReader.Read())
        //    {
        //        if (responseReader.NodeType == XmlNodeType.Element)
        //        {
        //            switch (responseReader.LocalName)
        //            {
        //                case "SERVICE":
        //                    OGCService service = new OGCService(service.Name);

        //                    if (service != null)
        //                    {
        //                        server.Services.Add(service.Name, service);
        //                    }
        //                    break;
        //            }
        //        }
        //    }

        //    server._lastUpdated = DateTime.Now;
        //    server.HasServices = true;
        //    response._server = server;
        //    return response;
        //}
    }

    public class OGCFeatureResponse : GISFeatureResponse
    {
        //internal static GISResponse ProcessFeatureReturn(XmlTextReader responseReader, string requestString, string responseString)
        //{
        //    OGCFeatureResponse response = new OGCFeatureResponse();
        //    response.LastRequest = requestString;
        //    response.LastResponse = responseString;

        //    while (responseReader.Read())
        //    {
        //        if (responseReader.NodeType == XmlNodeType.Element)
        //        {
        //            switch (responseReader.LocalName)
        //            {
        //                case "FEATURE":
        //                    response._features.Add(OGCFeature.ProcessFeature(responseReader));
        //                    break;
        //            }
        //        }
        //    }

        //    return response;
        //}
        internal static void ProcessFeatureReturn(XElement responseReader, GISFeatureResponse response)
        {
            //<FeatureInfoResponse>
            //<FIELDS CNTRY_NAME="Turkey" COLOR_MAP="2" CURR_CODE="TRL" CURR_TYPE="Lira" FIPS_CNTRY="TU" GMI_CNTRY="TUR" ISO_2DIGIT="TR" ISO_3DIGIT="TUR" LANDLOCKED="N" LONG_NAME="Turkey" POP_CNTRY="61300930" SOVEREIGN="Turkey" SQKM="779331.5" SQMI="300899.91" _ID_="44" _LAYERID_="world_countries" _SHAPE_="[Geometry]"/>
            //</FeatureInfoResponse>

            foreach (XElement fields in responseReader.Descendants(XName.Get("FIELDS")))
            {
                GISFeature feature = new GISFeature();

                foreach (XAttribute attr in fields.Attributes())
                {
                    feature._Fields.Add(new GISField(attr.Name.LocalName, attr.Value));
                }

                response.Features.Add(feature);
            }
        }
    }
    #endregion
}
