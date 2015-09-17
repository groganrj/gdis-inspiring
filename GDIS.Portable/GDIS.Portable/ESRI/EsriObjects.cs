using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml;
using AtlasOf.GIS;
using System.Collections.Generic;
using System.Xml.Linq;
using Nii.JSON;

namespace AtlasOf.GIS.ESRI
{
    #region Layer
    public class EsriLayer : GISLayer
    {
        private MapElement _object;
        private MapElement _renderer;
        private MapElement _dataset;
        private MapElement _query;

        public MapElement Object
        {
            get { return _object; }
            set { _object = value; }
        }

        public MapElement Dataset
        {
            get { return _dataset; }
            set { _dataset = value; }
        }

        public MapElement Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public MapElement Renderer
        {
            get { return _renderer; }
            set { _renderer = value; }
        }

        //    public EsriLayer() { }

        //    public EsriLayer(LAYERINFO info)
        //    {
        //        _type = (LayerType)info.type;
        //        _visible = info.visible == LAYERINFOVisible.@true;
        //        _name = info.name;
        //        _id = info.id;
        //        PopulateItems(info.Items);
        //    }

        //    //[System.Xml.Serialization.XmlElementAttribute("ENVELOPE", typeof(ENVELOPE))]
        //    //[System.Xml.Serialization.XmlElementAttribute("EXTENSION", typeof(EXTENSION))]
        //    //[System.Xml.Serialization.XmlElementAttribute("FCLASS", typeof(FCLASS))]
        //    private void PopulateItems(object[] p)
        //    {
        //        for (int i = 0; i < p.Length; i++)
        //        {
        //            if (p[i] is EXTENSION)
        //            {
        //                string t2 = "";
        //            }
        //            else if (p[i] is FCLASS)
        //            {
        //                string t = "";
        //            }
        //            else if (p[i] is ENVELOPE)
        //            {
        //                ENVELOPE e = p[i] as ENVELOPE;
        //                _envelope = new EsriEnvelope(double.Parse(e.minx), double.Parse(e.maxx), double.Parse(e.miny), double.Parse(e.maxy));
        //            }
        //        }
        //    }

        //    public EsriLayer(LayerType layerType, string layerName)
        //    {
        //        _type = layerType;
        //        _name = layerName;
        //    }

        //    public EsriLayer(EsriLayer currentLayer)
        //    {
        //        this.Visible = currentLayer.Visible;
        //        this.Renderer = currentLayer.Renderer;
        //        this.Query = currentLayer.Query;
        //        this.Object = currentLayer.Object;
        //        this.Name = currentLayer.Name;
        //        this.Dataset = currentLayer.Dataset;
        //        this.LayerType = currentLayer.LayerType;
        //    }

        public void AsXml(XmlWriter xtw)
        {
            xtw.WriteStartElement("LAYERDEF");
            xtw.WriteAttributeString("", "name", "", _name);
            xtw.WriteAttributeString("", "id", "", _id);
            xtw.WriteAttributeString("", "visible", "", _visible.ToString().ToLower());
            //xtw.WriteAttributeString("", "type", "", _type);

            if (_dataset != null) _dataset.AsXml(xtw);
            if (_query != null) _query.AsXml(xtw);

            if (_object != null)
            {
                xtw.WriteStartElement("OBJECT");
                xtw.WriteAttributeString("", "units", "", "pixel");
                _object.AsXml(xtw);
                xtw.WriteEndElement(); // OBJECT
            }

            if (_renderer != null)
            {
                xtw.WriteStartElement("SIMPLERENDERER");
                _renderer.AsXml(xtw);
                xtw.WriteEndElement(); // SIMPLERENDERER
            }

            xtw.WriteEndElement(); // LAYER
        }

        // [{"id":0,"name":"Davidson_County","parentLayerId":-1,"defaultVisibility":true,"subLayerIds":null}],"spatialReference":{"wkid":102113},"singleFusedMapCache":true,"tileInfo":{"rows":256,"cols":256,"dpi":96,"format":"JPEG","compressionQuality":75,"origin":{"x":-20037508.342787,"y":20037508.342787},"spatialReference":{"wkid":102113},"lods":[{"level":0,"resolution":156543.033928,"scale":591657527.591555},{"level":1,"resolution":78271.5169639999,"scale":295828763.795777},{"level":2,"resolution":39135.7584820001,"scale":147914381.897889},{"level":3,"resolution":19567.8792409999,"scale":73957190.948944},{"level":4,"resolution":9783.93962049996,"scale":36978595.474472},{"level":5,"resolution":4891.96981024998,"scale":18489297.737236},{"level":6,"resolution":2445.98490512499,"scale":9244648.868618},{"level":7,"resolution":1222.99245256249,"scale":4622324.434309},{"level":8,"resolution":611.49622628138,"scale":2311162.217155},{"level":9,"resolution":305.748113140558,"scale":1155581.108577},{"level":10,"resolution":152.874056570411,"scale":577790.554289},{"level":11,"resolution":76.4370282850732,"scale":288895.277144},{"level":12,"resolution":38.2185141425366,"scale":144447.638572},{"level":13,"resolution":19.1092570712683,"scale":72223.819286},{"level":14,"resolution":9.55462853563415,"scale":36111.909643},{"level":15,"resolution":4.77731426794937,"scale":18055.954822},{"level":16,"resolution":2.38865713397468,"scale":9027.977411},{"level":17,"resolution":1.19432856685505,"scale":4513.988705},{"level":18,"resolution":0.597164283559817,"scale":2256.994353},{"level":19,"resolution":0.298582141647617,"scale":1128.497176}]},"initialExtent":{"xmin":-9718317.13891344,"ymin":4282403.91976706,"xmax":-9605030.75071575,"ymax":4369195.91072496,"spatialReference":{"wkid":102113}},"fullExtent":{"xmin":-20037507.2295943,"ymin":-19971868.8804086,"xmax":20037507.2295943,"ymax":19971868.8804086,"spatialReference":{"wkid":102113}},"units":"esriMeters","supportedImageFormatTypes":"PNG24,PNG,JPG,DIB,TIFF,EMF,PS,PDF,GIF,SVG,SVGZ,AI","documentInfo":{"Title":"DavidsonFlood","Author":"agsglobe","Comments":"","Subject":"","Category":"","Keywords":""}}
        internal static List<GISLayerInfo> Create(JSONArray s)
        {
            List<GISLayerInfo> layers = new List<GISLayerInfo>();

            GISLayerInfo layer;

            foreach (JSONObject obj in s.List)
            {
                layer = EsriLayerInfo.Create(obj);
                if (layer != null) layers.Add(layer);
            }

            return layers;
        }
    }
    #endregion

    #region LayerDef
    public class LayerDef
    {
        private bool visible;
        private int featureCount;
        private string name, id;

        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        public int FeatureCount
        {
            get { return featureCount; }
        }

        public string Name
        {
            get { return name; }
        }

        public string Id
        {
            get { return id; }
        }

        public LayerDef()
        {
        }

        public LayerDef(string layerName, string layerID, int layerFeatureCount)
        {
            name = layerName;
            id = layerID;
            featureCount = layerFeatureCount;
            visible = false;
        }

        public LayerDef(string layerName, string layerID, int layerFeatureCount, bool layerVisible)
        {
            name = layerName;
            id = layerID;
            featureCount = layerFeatureCount;
            visible = layerVisible;
        }

        public void AsXml(XmlWriter xtw)
        {
            xtw.WriteStartElement("LAYERDEF");
            xtw.WriteAttributeString("", "id", "", id);
            xtw.WriteAttributeString("", "visible", "", visible.ToString());
            xtw.WriteEndElement();
        }

        internal static LayerDef ProcessLayerNode(XmlReader responseReader)
        {
            LayerDef newLayer = new LayerDef();

            while (responseReader.MoveToNextAttribute())
            {
                switch (responseReader.Name)
                {
                    case "name":
                        newLayer.name = responseReader.Value;
                        break;
                    case "id":
                        newLayer.id = responseReader.Value;
                        break;
                    case "featurecount":
                        newLayer.featureCount = int.Parse(responseReader.Value);
                        break;
                    case "visible":
                        newLayer.visible = bool.Parse(responseReader.Value);
                        break;
                }
            }

            return newLayer;
        }
    }
    #endregion

    #region EsriEnvelope
    public class EsriEnvelope : GISEnvelope
    {
        public EsriEnvelope() { }

        public EsriEnvelope(double newMinX, double newMaxX, double newMinY, double newMaxY)
            : base(newMinX, newMaxX, newMinY, newMaxY)
        {
        }

        public EsriEnvelope(GISEnvelope env) : base(env.minX, env.maxX, env.minY, env.MaxY) { CoordinateSystem = env.CoordinateSystem; }

        public static EsriEnvelope ProcessEnvelopeNode(XmlReader envelopeNode)
        {
            string realVal;
            EsriEnvelope env = new EsriEnvelope();

            while (envelopeNode.MoveToNextAttribute())
            {
                // international numbers
                realVal = envelopeNode.Value.Replace(',', '.');

                switch (envelopeNode.Name)
                {
                    case "maxx":
                        env.maxX = double.Parse(realVal);
                        break;
                    case "maxy":
                        env.maxY = double.Parse(realVal);
                        break;
                    case "minx":
                        env.minX = double.Parse(realVal);
                        break;
                    case "miny":
                        env.minY = double.Parse(realVal);
                        break;
                }
            }

            return env;
        }

        public static void AsXml(XmlWriter xtw, GISEnvelope envelope)
        {
            xtw.WriteStartElement("ENVELOPE");
            xtw.WriteAttributeString("", "maxx", "", envelope.maxX.ToString());
            xtw.WriteAttributeString("", "minx", "", envelope.minX.ToString());
            xtw.WriteAttributeString("", "maxy", "", envelope.maxY.ToString());
            xtw.WriteAttributeString("", "miny", "", envelope.minY.ToString());
            xtw.WriteEndElement(); // ENVELOPER
        }

        public string ToJSON()
        {
            return string.Format("{0},{1},{2},{3}", minX, minY, maxX, maxY);
        }

        //internal static EsriEnvelope Create(ENVELOPE eNVELOPE)
        //{
        //    EsriEnvelope e = new EsriEnvelope();

        //    e.maxX = double.Parse(eNVELOPE.maxx.Replace(',', '.'));
        //    e.minX = double.Parse(eNVELOPE.minx.Replace(',', '.'));
        //    e.maxY = double.Parse(eNVELOPE.maxy.Replace(',', '.'));
        //    e.minY = double.Parse(eNVELOPE.miny.Replace(',', '.'));
        //    return e;
        //}

        internal static GISEnvelope Create(JSONObject s)
        {
            EsriEnvelope env = new EsriEnvelope(s.getDouble("xmin"), s.getDouble("xmax"), s.getDouble("ymin"), s.getDouble("ymax"));

            if (s["spatialReference"] != null)
            {
                JSONObject o = s.getJSONObject("spatialReference");
                int i = o.getInt("wkid");
                env.CoordinateSystem = string.Format("EPSG:{0}", i);
            }

            return env;
        }
    }
    #endregion

    #region EsriService
    public class EsriService : GISService
    {
        public EsriService()
        {
        }

        public EsriService(string serviceName)
        {
            _serviceName = serviceName;
            _serviceId = serviceName;
        }

        #region Static
        internal static void AddServiceInfo(GISService service, XmlReader infoReader)
        {
            while (infoReader.Read())
            {
                switch (infoReader.Name)
                {
                    case "ENVIRONMENT":
                        break;
                    case "PROPERTIES":
                        AddPropertyInfo(service, infoReader);
                        break;
                    case "LAYERINFO":
                        service._baseLayers.Add(AddLayerInfo(infoReader, true));
                        break;
                }
            }
        }

        internal static EsriLayerInfo AddLayerInfo(XmlReader responseReader, bool isServiceInfo)
        {
            EsriLayerInfo newLayer = new EsriLayerInfo();

            while (responseReader.MoveToNextAttribute())
            {
                switch (responseReader.Name)
                {
                    case "name":
                        newLayer._name = responseReader.Value;
                        break;
                    case "maxscale":
                        newLayer._maxscale = double.Parse(responseReader.Value);
                        break;
                    case "minscale":
                        newLayer._minscale = double.Parse(responseReader.Value);
                        break;
                    case "visible":
                        bool vis;

                        if (bool.TryParse(responseReader.Value, out vis))
                        {
                            newLayer._isVisible = vis;
                        }
                        else newLayer._isVisible = responseReader.Value == "@true";
                        break;
                    case "type":
                        newLayer._type = responseReader.Value;
                        break;
                    case "id":
                        newLayer._id = responseReader.Value;
                        break;
                    case "featurecount":
                        newLayer.FeatureCount = int.Parse(responseReader.Value);
                        break;
                }
            }

            if (isServiceInfo) AddLayerInfo(newLayer, responseReader);
            return newLayer;
        }

        internal static void AddLayerInfo(EsriLayerInfo info, XmlReader propReader)
        {
            while (propReader.Read())
            {
                if (propReader.NodeType == XmlNodeType.Element)
                {
                    switch (propReader.Name)
                    {
                        case "FCLASS":
                            info.IsQueryable = true;
                            EsriField.ProcessFields(info, propReader);
                            //                        AddFeatureInfo(info, propReader);
                            break;
                        case "ENVELOPE":
                            info._baseExtent = EsriEnvelope.ProcessEnvelopeNode(propReader);
                            break;
                        case "EXTENSION":
                            // type =  Geocode, StoredQuery, or Extract
                            if (propReader.MoveToAttribute("type"))
                            {
                                switch (propReader.Value)
                                {
                                    case "Geocode":
                                        if (propReader.MoveToElement())
                                        {
                                            if (propReader.MoveToAttribute("name"))
                                            {
                                                string x = propReader.Value;
                                            }
                                        }
                                        break;
                                    case "StoredQuery":
                                        break;
                                    case "Extract":
                                        break;
                                }
                            }
                            break;
                        default:
                            string t = propReader.Name;
                            break;
                    }
                }
                else if (propReader.NodeType == XmlNodeType.EndElement)
                {
                    if (propReader.Name == "LAYERINFO" || propReader.Name == "LAYER") break;
                }
            }
        }

        internal static void AddFeatureInfo(EsriLayerInfo info, XmlReader propReader)
        {
            while (propReader.Read())
            {
                if (propReader.NodeType == XmlNodeType.Element)
                {
                    switch (propReader.Name)
                    {
                        case "FIELD":
                            EsriField.ProcessFields(info, propReader);
                            break;
                        default:
                            string t = propReader.Name;
                            break;
                    }
                }
                else if (propReader.NodeType == XmlNodeType.EndElement)
                {
                    if (propReader.Name == "PROPERTIES") break;
                }
            }
        }

        //<PROPERTIES>
        //    <FEATURECOORDSYS string="GEOGCS[&quot;GCS_WGS_1984&quot;,DATUM[&quot;D_WGS_1984&quot;,SPHEROID[&quot;WGS_1984&quot;,6378137.0,298.257223563]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]]" id="4326" />
        //    <FILTERCOORDSYS string="GEOGCS[&quot;GCS_WGS_1984&quot;,DATUM[&quot;D_WGS_1984&quot;,SPHEROID[&quot;WGS_1984&quot;,6378137.0,298.257223563]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]]" id="4326" />
        //    <ENVELOPE minx="100" miny="-50" maxx="180" maxy="5" name="Initial_Extent" />
        //    <ENVELOPE minx="-180" miny="-80" maxx="180" maxy="80" name="Extent_Limit" />
        //    <MAPUNITS units="decimal_degrees" />
        //    <BACKGROUND color="255,255,255" transcolor="255,255,255" />
        //</PROPERTIES>
        internal static void AddPropertyInfo(GISService service, XmlReader propReader)
        {
            string coords = string.Empty;
            GISEnvelope coordsys = null;

            while (propReader.Read())
            {
                if (propReader.NodeType == XmlNodeType.Element)
                {
                    switch (propReader.Name)
                    {
                        case "FILTERCOORDSYS":
                            if (propReader.MoveToAttribute("id"))
                            {
                                coords = propReader.Value;
                            }
                            break;
                        case "ENVELOPE":
                            coordsys = EsriEnvelope.ProcessEnvelopeNode(propReader);
                            coordsys.CoordinateSystem = coords;
                            break;
                    }
                }
                else if (propReader.NodeType == XmlNodeType.EndElement)
                {
                    if (propReader.Name == "PROPERTIES") break;
                }
            }

            service._baseExtent = coordsys;
        }

        //		<SERVICE ACCESS="PUBLIC" DESC="" NAME="The_National_Map" SERVICEGROUP="USGS_WEB" STATUS="ENABLED" TYPE="ImageServer" VERSION="" group="*">
        //			<IMAGE TYPE="JPG"/>
        //			<ENVIRONMENT>
        //				 <LOCALE country="US" language="en" variant=""/>
        //					<UIFONT name="dialog"/>
        //			</ENVIRONMENT>
        //		</SERVICE>
        public static EsriService ProcessNode(XmlReader reader)
        {
            EsriService newService = new EsriService();

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "NAME":
                        newService._serviceId = newService._serviceName = reader.Value;
                        break;
                    case "TYPE":
                        try
                        {
                            newService._type = (ServiceType)System.Enum.Parse(typeof(ServiceType), reader.Value, true);
                        }
                        catch
                        {
                            newService._type = ServiceType.ImageServer;
                        }
                        break;
                    case "STATUS":
                        newService._isEnabled = reader.Value == "ENABLED";
                        break;
                    case "ACCESS":
                        newService._isPublic = reader.Value == "PUBLIC";
                        break;
                    case "DESC":
                        newService._description = reader.Value;
                        break;
                    default:
                        string t = reader.Value;
                        break;
                }
            }

            ProcessChildNode(newService, reader);
            return newService;
        }

        public static EsriService ProcessChildNode(EsriService newService, XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.Name == "SERVICE") break;

                switch (reader.Name)
                {
                    case "LOCALE":
                        break;
                    case "UIFONT":
                        break;
                    case "IMAGE":
                        break;
                    case "ENVIRONMENT":
                        break;
                    default:
                        string t = reader.Value;
                        break;
                }
            }

            return newService;
        }
        #endregion

        // {"name":"Davidson_County","type":"ImageServer"}
        internal static EsriService ProcessNode(JSONObject service)
        {
            try
            {
                return new EsriService() { _serviceId = service.getString("name"), _serviceName = service.getString("name"), _type = (ServiceType)Enum.Parse(typeof(ServiceType), service.getString("type"), true) };
            }
            catch
            {
                return new EsriService() { _serviceId = service.getString("name"), _serviceName = service.getString("name"), _type = ServiceType.ImageServer };
            }
        }

        // {"serviceDescription":"","mapName":"Layers","description":"","copyrightText":"","layers":[{"id":0,"name":"Davidson_County","parentLayerId":-1,"defaultVisibility":true,"subLayerIds":null}],"spatialReference":{"wkid":102113},"singleFusedMapCache":true,"tileInfo":{"rows":256,"cols":256,"dpi":96,"format":"JPEG","compressionQuality":75,"origin":{"x":-20037508.342787,"y":20037508.342787},"spatialReference":{"wkid":102113},"lods":[{"level":0,"resolution":156543.033928,"scale":591657527.591555},{"level":1,"resolution":78271.5169639999,"scale":295828763.795777},{"level":2,"resolution":39135.7584820001,"scale":147914381.897889},{"level":3,"resolution":19567.8792409999,"scale":73957190.948944},{"level":4,"resolution":9783.93962049996,"scale":36978595.474472},{"level":5,"resolution":4891.96981024998,"scale":18489297.737236},{"level":6,"resolution":2445.98490512499,"scale":9244648.868618},{"level":7,"resolution":1222.99245256249,"scale":4622324.434309},{"level":8,"resolution":611.49622628138,"scale":2311162.217155},{"level":9,"resolution":305.748113140558,"scale":1155581.108577},{"level":10,"resolution":152.874056570411,"scale":577790.554289},{"level":11,"resolution":76.4370282850732,"scale":288895.277144},{"level":12,"resolution":38.2185141425366,"scale":144447.638572},{"level":13,"resolution":19.1092570712683,"scale":72223.819286},{"level":14,"resolution":9.55462853563415,"scale":36111.909643},{"level":15,"resolution":4.77731426794937,"scale":18055.954822},{"level":16,"resolution":2.38865713397468,"scale":9027.977411},{"level":17,"resolution":1.19432856685505,"scale":4513.988705},{"level":18,"resolution":0.597164283559817,"scale":2256.994353},{"level":19,"resolution":0.298582141647617,"scale":1128.497176}]},"initialExtent":{"xmin":-9718317.13891344,"ymin":4282403.91976706,"xmax":-9605030.75071575,"ymax":4369195.91072496,"spatialReference":{"wkid":102113}},"fullExtent":{"xmin":-20037507.2295943,"ymin":-19971868.8804086,"xmax":20037507.2295943,"ymax":19971868.8804086,"spatialReference":{"wkid":102113}},"units":"esriMeters","supportedImageFormatTypes":"PNG24,PNG,JPG,DIB,TIFF,EMF,PS,PDF,GIF,SVG,SVGZ,AI","documentInfo":{"Title":"DavidsonFlood","Author":"agsglobe","Comments":"","Subject":"","Category":"","Keywords":""}}
        // "{\"serviceDescription\":\"\",\"name\":\"IP-0AD00EAF/Davidson_County\",\"description\":\"\",\"extent\":{\"xmin\":1646981.21805573,\"ymin\":587616.749700546,\"xmax\":1822910.26351929,\"ymax\":760697.139667511,\"spatialReference\":{\"wkid\":2274}},\"pixelSizeX\":0.98425,\"pixelSizeY\":0.98425,\"bandCount\":3,\"pixelType\":\"U8\",\"minPixelSize\":0,\"maxPixelSize\":0,\"copyrightText\":\"\",\"serviceDataType\":\"esriImageServiceDataTypeRGB\",\"minValues\":[0,0,0],\"maxValues\":[255,255,255],\"meanValues\":[98.79502,120.31811,101.45624],\"stdvValues\":[51.45737,46.51745,48.35864]}"
        internal static void AddServiceInfo(GISService activeService, JSONObject responseString)
        {
            GISEnvelope layerExtent = null;

            foreach (DictionaryEntry s in responseString.getDictionary())
            {
                switch (s.Key.ToString())
                {
                    case "documentInfo":
                        JSONObject obj = s.Value as JSONObject;
                        activeService._serviceName = obj.getString("Title");
                        activeService._keywords = obj.getString("Keywords");
                        activeService._subjects = obj.getString("Subject");
                        //documentInfo
                        //{
                        //    "Title":"U.S. Monthly Extremes",
                        //    "Author":"National Climatic Data Center, NESDIS, NOAA, U.S. Department of Commerce ",
                        //    "Comments":"",
                        //    "Subject":"",
                        //    "Category":"",
                        //    "AntialiasingMode":"None",
                        //    "TextAntialiasingMode":"Force",
                        //    "Keywords":"monthly"
                        //}
                        break;
                    case "serviceDescription":
                        activeService._description = s.Value.ToString();
                        break;
                    case "name":
                        //activeService._serviceName = s.Value.ToString();
                        break;
                    case "description":
                        activeService._description = string.IsNullOrEmpty(activeService._description) ? s.Value.ToString() : activeService._description;
                        break;
                    case "fullExtent":
                        activeService._baseExtent = EsriEnvelope.Create(s.Value as JSONObject);
                        if (layerExtent == null) layerExtent = activeService._baseExtent;
                        break;
                    case "initialExtent":
                        layerExtent = EsriEnvelope.Create(s.Value as JSONObject);
                        if (activeService._baseExtent == null) activeService._baseExtent = layerExtent;
                        break;
                    case "extent":
                        layerExtent = EsriEnvelope.Create(s.Value as JSONObject);
                        if (activeService._baseExtent == null || activeService._baseExtent.Equals(GISEnvelope.TheWorld)) activeService._baseExtent = EsriEnvelope.Create(s.Value as JSONObject);
                        break;
                    case "layers":
                        activeService._baseLayers = EsriLayer.Create(s.Value as JSONArray);
                        break;
                }
            }

            if (activeService._baseLayers.Count == 0)
            {
                activeService._baseLayers.Add(new EsriLayerInfo() { _baseExtent = layerExtent, _id = activeService._serviceName, _name = activeService._serviceName, _type = "Image" });
            }
            else
            {
                foreach (EsriLayerInfo info in activeService._baseLayers)
                {
                    if (info._baseExtent == null) info._baseExtent = layerExtent;
                }
            }
        }
    }
    #endregion

    #region EsriLayerInfo
    public class EsriLayerInfo : GISLayerInfo
    {
        public EsriLayerInfo() { }

        public EsriLayerInfo(string serviceName, string serviceId)
        {
            _id = serviceId;
            _name = serviceName;
        }

        public static void GetRequestXml(GISLayerInfo info, XmlWriter xtw)
        {
            xtw.WriteStartElement("LAYERDEF");
            xtw.WriteAttributeString("", "name", "", info.Name);
            xtw.WriteAttributeString("", "id", "", info.Id);
            xtw.WriteAttributeString("", "visible", "", info.IsVisible.ToString());

            xtw.WriteEndElement(); // LAYER
        }

        //public static EsriLayerInfo Create(LAYERINFO layerInfo)
        //{
        //    EsriLayerInfo info = new EsriLayerInfo();

        //    if (layerInfo.visibleSpecified) info._isVisible = layerInfo.visible == LAYERINFOVisible.@true;

        //    info._id = layerInfo.id;

        //    try
        //    {
        //        if (!string.IsNullOrEmpty(layerInfo.minscale))
        //        {
        //            info._minscale = double.Parse(layerInfo.minscale);
        //        }
        //    }
        //    catch { }

        //    try
        //    {
        //        if (!string.IsNullOrEmpty(layerInfo.maxscale))
        //        {
        //            info._maxscale = double.Parse(layerInfo.maxscale);
        //        }
        //    }
        //    catch { }

        //    info._name = layerInfo.name;
        //    info._type = layerInfo.type.ToString();

        //    if (layerInfo.Items != null)
        //    {
        //        for (int i = 0; i < layerInfo.Items.Length; i++)
        //        {
        //            if (layerInfo.Items[i] is ENVELOPE)
        //            {
        //                info._baseExtent = EsriEnvelope.Create(layerInfo.Items[i] as ENVELOPE);
        //            }
        //            else if (layerInfo.Items[i] is FCLASS)
        //            {
        //                info.IsQueryable = true;
        //                ProcessItems(layerInfo.Items[i] as FCLASS, info);
        //            }
        //        }
        //    }

        //    return info;
        //}

        //private static void ProcessItems(FCLASS fCLASS, EsriLayerInfo info)
        //{
        //    foreach (object obj in fCLASS.Items)
        //    {
        //        if (obj is ENVELOPE)
        //        {
        //            info._baseExtent = EsriEnvelope.Create(obj as ENVELOPE);
        //        }
        //        else if (obj is FIELD)
        //        {
        //            info._Fields.Add(EsriField.ProcessFieldNode(obj as FIELD));
        //        }
        //    }
        //}

        //{"id":0,"name":"Davidson_County","parentLayerId":-1,"defaultVisibility":true,"subLayerIds":null}
        internal static GISLayerInfo Create(JSONObject obj)
        {
            EsriLayerInfo info = new EsriLayerInfo();

            for (int idx = 0; idx < obj.Count; idx++)
            {
                switch (obj[idx])
                {
                    case "id":
                        info._id = obj.getString("id");
                        break;
                    case "name":
                        info._name = obj.getString("name");
                        break;
                    case "":
                        info._isVisible = obj.getBool("defaultVisibility");
                        break;
                }
            }

            return info;
        }

        internal static void AddLayerDetails(GISLayerInfo info, JSONObject obj)
        {
            for (int idx = 0; idx < obj.Count; idx++)
            {
                switch (obj[idx])
                {
                    case "id":
                        break;
                    case "name":
                        break;
                    case "type":
                        info._type = obj.getString("type");
                        break;
                    case "geometryType":
                        break;
                    case "copyrightText":
                    case "parentLayer":
                        //{"id" : <parentLayerId>, "name" : "<parentLayerName>"}
                        break;
                    case "subLayers":
                    //[    {"id" : <subLayerId1>, "name" : "<subLayerName1>"},    {"id" : <subLayerId2>, "name" : "<subLayerName2>"}],
                    case "minScale":
                        break;
                    case "maxScale":
                        break;
                    case "extent":
                        info._baseExtent = EsriEnvelope.Create(obj.getJSONObject("extent"));
                        break;
                    case "displayField":
                        break;
                    case "fields":
                        EsriField.ProcessFields(info._Fields, obj.getJSONArray("fields"));
                        info.IsQueryable = true;
                        break;
                }
            }
        }
    }
    #endregion

    #region EsriResponse
    public class EsriImageResponse : GISImageResponse
    {
        private List<LayerDef> _layerdefs = new List<LayerDef>();
        private string _legendUrl;

        protected List<LayerDef> Layerdefs
        {
            get { return _layerdefs; }
            set { _layerdefs = value; }
        }

        public override List<GISLayerInfo> GetLayerInfo()
        {
            List<GISLayerInfo> layerInfo = new List<GISLayerInfo>();

            layerInfo.AddRange(_layers);

            foreach (LayerDef layer in _layerdefs)
            {
                layerInfo.Add(new EsriLayerInfo() { Name = layer.Name, Id = layer.Id });
            }

            return layerInfo;
        }

        // starts at IMAGE begin element
        public static EsriImageResponse ProcessImageReturn(XmlReader responseReader, string requestString, string responseString)
        {
            string localName = "start";
            EsriImageResponse response = new EsriImageResponse();

            response.LastRequest = requestString;
            response.LastResponse = responseString;

            try
            {
                while (responseReader.Read())
                {
                    if (responseReader.NodeType == XmlNodeType.Element)
                    {
                        localName = responseReader.LocalName;

                        switch (responseReader.LocalName)
                        {
                            case "OUTPUT":
                                responseReader.MoveToAttribute("url");
                                response._mapImageUrl = responseReader.Value;
                                break;
                            case "LEGEND":
                                responseReader.MoveToAttribute("url");
                                response._legendUrl = responseReader.Value;
                                break;
                            case "ENVELOPE":
                                response._envelope = EsriEnvelope.ProcessEnvelopeNode(responseReader);
                                break;
                            case "LAYER":
                                EsriLayerInfo newLayer = EsriService.AddLayerInfo(responseReader, false);

                                try
                                {
                                    response._layers.Add(newLayer);
                                }
                                catch
                                {
                                    // exception for value in range
                                }
                                break;
                            case "LAYERDEF":
                                LayerDef newLayerDef = LayerDef.ProcessLayerNode(responseReader);
                                try
                                {
                                    response._layerdefs.Add(newLayerDef);
                                }
                                catch
                                {
                                    // exception for value in range
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.LastResponse = localName;
                response.ErrorMessage = ex.Message;
            }

            return response;
        }
    }

    public class EsriField : GISField
    {
        public EsriField(string name, string fvalue) : base(name, fvalue) { }

        public EsriField(string name)
            : base(name)
        {
        }

        internal static void ProcessFields(EsriFeature info, XmlReader responseReader)
        {
            while (responseReader.Read())
            {
                if (responseReader.NodeType == XmlNodeType.EndElement && responseReader.Name == "FCLASS") break;

                if (responseReader.NodeType == XmlNodeType.Element)
                {
                    if (responseReader.Name == "ENVELOPE")
                    {
                        info._Envelope = EsriEnvelope.ProcessEnvelopeNode(responseReader);
                    }
                    else
                    {
                        info.Fields.AddRange(EsriField.ProcessFields(responseReader));
                    }
                }
            }
        }

        internal static List<GISField> ProcessFields(XmlReader responseReader)
        {
            string fieldName;
            List<GISField> field = new List<GISField>();

            while (responseReader.MoveToNextAttribute())
            {
                fieldName = responseReader.Name;
                string[] arr = fieldName.Split('.');
                field.Add(new GISField() { _fieldName = arr[arr.Length - 1], _fullFieldName = responseReader.Name, _fieldValue = responseReader.Value });
            }

            return field;
        }

        internal static void ProcessFields(EsriLayerInfo info, XmlReader responseReader)
        {
            while (responseReader.Read())
            {
                if (responseReader.NodeType == XmlNodeType.EndElement && responseReader.Name == "FCLASS") break;

                if (responseReader.NodeType == XmlNodeType.Element)
                {
                    if (responseReader.Name == "ENVELOPE")
                    {
                        info._baseExtent = EsriEnvelope.ProcessEnvelopeNode(responseReader);
                    }
                    else
                    {
                        info.Fields.Add(EsriField.ProcessField(responseReader));
                    }
                }
            }
        }

        private static GISField ProcessField(XmlReader responseReader)
        {
            string name = responseReader.GetAttribute("name");
            string[] nameparts = name.Split('.');
            // add type, etc
            return new EsriField(nameparts[nameparts.Length - 1]);
        }

        //internal static GISField ProcessFieldNode(FIELD field)
        //{
        //    GISField efield = new GISField();

        //    efield._fieldName = field.name;
        //    if (field.typeSpecified) efield._fieldValue = field.type.ToString();
        //    efield._size = field.size;
        //    efield._precision = field.precision;
        //    return efield;
        //}

        internal static void ProcessFields(List<GISField> fieldList, JSONArray fields)
        {
            bool hasFields = fieldList.Count > 0;

            if (hasFields) return;

            // [    {"name" : "<fieldName1>", "type" : "<fieldType1>", "alias" : "<fieldAlias1>"},    {"name" : "<fieldName2>", "type" : "<fieldType2>", "alias" : "<fieldAlias2>"}]

            foreach (JSONObject obj in fields.List)
            {
                EsriField f = new EsriField("temp");

                foreach (DictionaryEntry field in obj.getDictionary())
                {
                    switch (field.Key.ToString())
                    {
                        case "name":
                            f._fieldName = field.Value.ToString();
                            break;
                        case "type":
                            f._type = field.Value.ToString();
                            break;
                    }
                }

                fieldList.Add(f);
            }
        }
    }

    public class EsriFeature : GISFeature
    {
        internal static EsriFeature ProcessFeature(XmlReader responseReader)
        {
            EsriFeature feature = new EsriFeature();

            while (responseReader.Read())
            {
                if (responseReader.NodeType == XmlNodeType.Element)
                {
                    switch (responseReader.LocalName)
                    {
                        case "ENVELOPE":
                            feature._Envelope = EsriEnvelope.ProcessEnvelopeNode(responseReader);
                            break;
                        case "FIELDS":
                            feature._Fields.AddRange(EsriField.ProcessFields(responseReader));
                            break;
                    }
                }
                else if (responseReader.NodeType == XmlNodeType.EndElement)
                {
                    if (responseReader.LocalName == "FEATURE") break;
                }
            }

            return feature;
        }

        internal static GISFeature Create(JSONObject obj, GISLayerInfo layer, bool hasFields)
        {
            string layerid = layer._id;
            GISFeature feature = new GISFeature();
            //{"layerId":1,"layerName":"Block Groups","value":"310899741.002","displayFieldName":"NAME","attributes":{"Shape":"Polygon","ID":"310899741002","State Abbreviation":"NE","2010 Total Population":"776","1990-2000 Population Change":"-0.5","2000-2010 Population: Annual Growth Rate":"-1.25","2010-2015 Population: Annual Growth Rate":"-1.19","Land Area in Square Miles":"811.3085"}}
            for (int i = 0; i < obj.Count; i++)
            {
                try
                {
                    switch (obj[i])
                    {
                        case "layerId":
                            layerid = obj[obj[i]].ToString();
                            break;
                        case "attributes":
                            if (string.Compare(layerid, layer._id) == 0)
                            {
                                AddAttributeValues((JSONObject)obj[obj[i]], feature, layer.Fields, hasFields);
                            }
                            else
                            {
                                AddAttributeValues((JSONObject)obj[obj[i]], feature);
                            }
                            break;
                        //case "bbox":
                        //    feature.Envelope = BingEnvelope.Parse((JSONArray)obj[obj[i]]);
                        //    break;
                        default:
                            feature.Fields.Add(new GISField(obj[i], obj[obj[i]].ToString()));
                            break;
                    }
                }
                catch { }
            }

            return feature;
        }

        internal static void AddAttributeValues(JSONObject obj, GISFeature feature, List<GISField> fields, bool hasFields)
        {            //{"layerId":1,"layerName":"Block Groups","value":"310899741.002","displayFieldName":"NAME","attributes":{"Shape":"Polygon","ID":"310899741002","State Abbreviation":"NE","2010 Total Population":"776","1990-2000 Population Change":"-0.5","2000-2010 Population: Annual Growth Rate":"-1.25","2010-2015 Population: Annual Growth Rate":"-1.19","Land Area in Square Miles":"811.3085"}}
            for (int i = 0; i < obj.Count; i++)
            {
                try
                {
                    if (hasFields)
                    {
                        var x = from c in fields where string.Compare(c.FieldName, obj[i], StringComparison.CurrentCultureIgnoreCase) == 0 select c;

                        if (hasFields && x.Count() == 0) continue;
                    }

                    feature.Fields.Add(new GISField(obj[i], obj[obj[i]].ToString()));
                }
                catch { }
            }
        }

        internal static void AddAttributeValues(JSONObject obj, GISFeature feature)
        {            //{"layerId":1,"layerName":"Block Groups","value":"310899741.002","displayFieldName":"NAME","attributes":{"Shape":"Polygon","ID":"310899741002","State Abbreviation":"NE","2010 Total Population":"776","1990-2000 Population Change":"-0.5","2000-2010 Population: Annual Growth Rate":"-1.25","2010-2015 Population: Annual Growth Rate":"-1.19","Land Area in Square Miles":"811.3085"}}
            for (int i = 0; i < obj.Count; i++)
            {
                try
                {
                    feature.Fields.Add(new GISField(obj[i], obj[obj[i]].ToString()));
                }
                catch { }
            }
        }
    }

    public class EsriServiceResponse : EsriFeatureResponse
    {
        private GISServer _server;

        public GISServer Server
        {
            get { return _server; }
            set { _server = value; }
        }

        internal static void ProcessServiceReturn(XmlReader responseReader, GISServer server, string responseString)
        {
            try
            {
                while (responseReader.Read())
                {
                    if (responseReader.NodeType == XmlNodeType.Element)
                    {
                        switch (responseReader.LocalName)
                        {
                            case "SERVICE":
                                EsriService service = EsriService.ProcessNode(responseReader);

                                if (service != null)
                                {
                                    server._services.Add(service);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string t = "";
            }

            server._lastUpdated = DateTime.Now;
        }

        // {"serviceDescription":"","mapName":"Layers","description":"","copyrightText":"","layers":[{"id":0,"name":"Davidson_County","parentLayerId":-1,"defaultVisibility":true,"subLayerIds":null}],"spatialReference":{"wkid":102113},"singleFusedMapCache":true,"tileInfo":{"rows":256,"cols":256,"dpi":96,"format":"JPEG","compressionQuality":75,"origin":{"x":-20037508.342787,"y":20037508.342787},"spatialReference":{"wkid":102113},"lods":[{"level":0,"resolution":156543.033928,"scale":591657527.591555},{"level":1,"resolution":78271.5169639999,"scale":295828763.795777},{"level":2,"resolution":39135.7584820001,"scale":147914381.897889},{"level":3,"resolution":19567.8792409999,"scale":73957190.948944},{"level":4,"resolution":9783.93962049996,"scale":36978595.474472},{"level":5,"resolution":4891.96981024998,"scale":18489297.737236},{"level":6,"resolution":2445.98490512499,"scale":9244648.868618},{"level":7,"resolution":1222.99245256249,"scale":4622324.434309},{"level":8,"resolution":611.49622628138,"scale":2311162.217155},{"level":9,"resolution":305.748113140558,"scale":1155581.108577},{"level":10,"resolution":152.874056570411,"scale":577790.554289},{"level":11,"resolution":76.4370282850732,"scale":288895.277144},{"level":12,"resolution":38.2185141425366,"scale":144447.638572},{"level":13,"resolution":19.1092570712683,"scale":72223.819286},{"level":14,"resolution":9.55462853563415,"scale":36111.909643},{"level":15,"resolution":4.77731426794937,"scale":18055.954822},{"level":16,"resolution":2.38865713397468,"scale":9027.977411},{"level":17,"resolution":1.19432856685505,"scale":4513.988705},{"level":18,"resolution":0.597164283559817,"scale":2256.994353},{"level":19,"resolution":0.298582141647617,"scale":1128.497176}]},"initialExtent":{"xmin":-9718317.13891344,"ymin":4282403.91976706,"xmax":-9605030.75071575,"ymax":4369195.91072496,"spatialReference":{"wkid":102113}},"fullExtent":{"xmin":-20037507.2295943,"ymin":-19971868.8804086,"xmax":20037507.2295943,"ymax":19971868.8804086,"spatialReference":{"wkid":102113}},"units":"esriMeters","supportedImageFormatTypes":"PNG24,PNG,JPG,DIB,TIFF,EMF,PS,PDF,GIF,SVG,SVGZ,AI","documentInfo":{"Title":"DavidsonFlood","Author":"agsglobe","Comments":"","Subject":"","Category":"","Keywords":""}}
        // {"currentVersion":"9.31","folders":[],"services":[{"name":"Davidson_County","type":"ImageServer"},{"name":"GeoEye_Chile_Feb28","type":"ImageServer"},{"name":"GeoEye_Chile_Feb28","type":"MapServer"},{"name":"GeoEye_Haiti_Jan13","type":"ImageServer"},{"name":"GeoEye_Haiti_Jan13","type":"MapServer"},{"name":"GeoEye_PortAuPrince_Jan13","type":"ImageServer"},{"name":"Gulf_Coast_ENC","type":"MapServer"},{"name":"Gulf_Coast_ENC_WM","type":"MapServer"},{"name":"Gulf_Coast_ESI","type":"MapServer"},{"name":"Gulf_Coast_ESI_Maps","type":"MapServer"},{"name":"Gulf_Coast_Fishery_Closure","type":"MapServer"},{"name":"Gulf_Coast_Offshore_Oil_Spill_Forecast","type":"MapServer"},{"name":"Gulf_Coast_Oil_Spill_Forecast","type":"MapServer"},{"name":"Gulf_Coast_Oil_Spill_Plume","type":"MapServer"},{"name":"Haiti_Grids","type":"MapServer"},{"name":"NCMI_HealthFacilities_Haiti","type":"MapServer"},{"name":"NOAA_Haiti_Jan17","type":"ImageServer"},{"name":"NOAA_Haiti_Jan18","type":"ImageServer"},{"name":"NOAA_Haiti_Jan18","type":"MapServer"},{"name":"NOAA_Haiti_Jan20","type":"ImageServer"},{"name":"NOAA_Haiti_Jan20","type":"MapServer"},{"name":"UN_Haiti_Base_Map","type":"MapServer"}]}
        internal static List<GISService> ProcessServiceReturn(IList responseReader)
        {
            List<GISService> services = new List<GISService>();

            try
            {
                foreach (JSONObject service in responseReader)
                {
                    EsriService eservice = EsriService.ProcessNode(service);

                    if (eservice != null)
                    {
                        if (services.Contains(eservice))
                        {
                            eservice._serviceId = eservice._serviceName = string.Format(eservice.Name + "__{0}", eservice.ServiceType);
                        }

                        if (eservice.ServiceType != ServiceType.GlobeServer) services.Add(eservice);
                    }
                }
            }
            catch (Exception ex)
            {
                string t = "";
            }

            return services;
        }
    }

    public class EsriFeatureResponse : GISFeatureResponse
    {
        internal static GISResponse ProcessFeatureReturn(XmlReader responseReader, GISFeatureResponse response, string responseString)
        {
            response.LastResponse = responseString;
            string searchTerm = response.SearchTerm;

            while (responseReader.Read())
            {
                if (responseReader.NodeType == XmlNodeType.Element)
                {
                    switch (responseReader.LocalName)
                    {
                        case "FEATURE":
                            EsriFeature feature = EsriFeature.ProcessFeature(responseReader);

                            if (string.IsNullOrEmpty(searchTerm))
                            {
                                response._features.Add(feature);
                            }
                            else
                            {
                                var r = from x in feature.Fields where x._fieldValue.ToLower().Contains(searchTerm.ToLower()) select x;

                                if (r.Count() > 0)
                                {
                                    feature.Fields.Add(new GISField("Title", r.First()._fieldValue));

                                    if (feature.Envelope != null)
                                    {
                                        feature.Fields.Add(new GISField("Latitude", feature.Envelope.CenterY.ToString()));
                                        feature.Fields.Add(new GISField("Longitude", feature.Envelope.CenterX.ToString()));
                                    }

                                    response._features.Add(feature);
                                }
                            }
                            break;
                    }
                }
            }

            return response;
        }

        internal static GISResponse ProcessFeatureReturn(Nii.JSON.JSONArray responseReader, GISFeatureResponse response, string Return)
        {
            string searchTerm = response.SearchTerm;

            foreach (JSONObject obj in responseReader.List)
            {
                GISFeature feature = new GISFeature();
                //"geometry" and "geometryType" // f._Envelope = 
                JSONObject attr = obj.getJSONObject("attributes");

                foreach (DictionaryEntry entry in attr.getDictionary())
                {
                    feature._Fields.Add(new GISField(entry.Key.ToString(), entry.Value.ToString()));
                }

                if (string.IsNullOrEmpty(searchTerm))
                {
                    response._features.Add(feature);
                }
                else
                {
                    var r = from x in feature.Fields where x._fieldValue.ToLower().Contains(searchTerm.ToLower()) select x;

                    feature.Fields.Add(new GISField("Title", r.First()._fieldValue));

                    if (feature.Envelope != null)
                    {
                        feature.Fields.Add(new GISField("Latitude", feature.Envelope.CenterY.ToString()));
                        feature.Fields.Add(new GISField("Longitude", feature.Envelope.CenterX.ToString()));
                    }

                    if (r.Count() > 0) response._features.Add(feature);
                }
            }
            //{"displayFieldName":"STATE_NAME","fieldAliases":{"STATE_NAME":"STATE_NAME","Shape_Length":"Shape_Length","Shape_Area":"Shape_Area"},"geometryType":"esriGeometryPolygon","spatialReference":{"wkid":4326},"features":[{"attributes":{"STATE_NAME":"Oregon","Shape_Length":24.7872489860459,"Shape_Area":28.1877891988605},"geometry":{"rings":[[[-121.441508911,41.9943345300001],[-122.284705082,42.0007645520001],[-123.222102653,42.002191774],[-123.513204633,41.997832917],[-123.819146439,41.992948772],[-124.206444444,41.997647912],[-124.352246781,42.098677261],[-124.415061997,42.2458945380001],[-124.437818793,42.4296087420001],[-124.391763164,42.5530276640001],[-124.401078666,42.622699244],[-124.559616906,42.832457397],[-124.485346585,42.95545394],[-124.386772401,43.2615889150001],[-124.40607626,43.3001978550001],[-124.273993989,43.459105396],[-124.226004804,43.605004857],[-124.158325577,43.857118234],[-124.118319319,44.269515002],[-124.054404869,44.6621389760001],[-124.075568095,44.8147385250001],[-124.007572643,45.036102923],[-123.956606853,45.292965715],[-123.980559909,45.4850845020001],[-123.936674662,45.5079659130001],[-123.892107877,45.474050161],[-123.859507215,45.4990826740001],[-123.953415554,45.5685287150001],[-123.936075917,45.7028352130001],[-123.97662908,45.7754823860001],[-123.956273973,45.8710413760001],[-123.996505696,45.9419219480001],[-123.921187205,46.0123233510001],[-123.977340685,46.2027060080001],[-123.794096469,46.1114485860001],[-123.777083421,46.1444302580001],[-123.820978313,46.193649603],[-123.761414137,46.2099393730001],[-123.71716098,46.1698937890001],[-123.670246414,46.1744984860001],[-123.517029327,46.236091581],[-123.363556869,46.1441540210001],[-123.304717085,46.1447375690001],[-123.248799416,46.1440203370001],[-123.212437027,46.1700060590001],[-123.176196406,46.1835864540001],[-123.118554159,46.1793104930001],[-123.050596212,46.155736227],[-122.974169261,46.1104834430001],[-122.899757286,46.0793296860001],[-122.875417724,46.0271833260001],[-122.807741767,45.94389012],[-122.806222916,45.9040724290001],[-122.78407365,45.8678864510001],[-122.784515918,45.8504495180001],[-122.788009566,45.8003435910001],[-122.764288519,45.760568027],[-122.772551033,45.7276855420001],[-122.760541289,45.6493974090001],[-122.696323094,45.6310455500001],[-122.6512092,45.6068304560001],[-122.565429806,45.594818768],[-122.437154198,45.5647789110001],[-122.356457484,45.5661712420001],[-122.303150329,45.543092834],[-122.244922294,45.5481128640001],[-122.082037518,45.5905040110001],[-122.000011554,45.61782429],[-121.972659452,45.6357760830001],[-121.926820689,45.6420283730001],[-121.888283499,45.6768563690001],[-121.811041035,45.7006830890001],[-121.758694097,45.6897160500001],[-121.706416861,45.6887931700001],[-121.529054612,45.719567678],[-121.442552169,45.6949670870001],[-121.422029029,45.6906031870001],[-121.367814251,45.6996865940001],[-121.319977744,45.6966428360001],[-121.276390902,45.6783399710001],[-121.214271714,45.665644963],[-121.203308118,45.6572869340001],[-121.192054666,45.6132419260001],[-121.174316012,45.6005161590001],[-121.125204666,45.607059098],[-121.073529929,45.6466107720001],[-121.033482584,45.6528444140001],[-120.968478512,45.6451545400001],[-120.948572831,45.6503159660001],[-120.907937251,45.6354771060001],[-120.861419668,45.665186254],[-120.696993904,45.710509819],[-120.658403368,45.732612487],[-120.623757205,45.743610571],[-120.570082462,45.7409179400001],[-120.499156502,45.6956306770001],[-120.443383763,45.689279722],[-120.283634875,45.7165828720001],[-120.20744539,45.7197840640001],[-120.155907861,45.761261667],[-120.068647865,45.7802024440001],[-119.99432016,45.8111403440001],[-119.869735635,45.831698524],[-119.833555881,45.8416093430001],[-119.678445664,45.8525390290001],[-119.622116728,45.8994103380001],[-119.589294283,45.9133149480001],[-119.512220001,45.8992005960001],[-119.43886109,45.914268522],[-119.379441421,45.9176100640001],[-119.30276351,45.932662726],[-119.178742643,45.922351608],[-119.1402506,45.9257086390001],[-119.032221681,45.9662745800001],[-118.982132819,45.9990583740001],[-117.992527778,46.0016389040001],[-117.982677429,45.9998805150001],[-117.602826164,46.000268158],[-117.481663001,45.999834713],[-116.919132428,45.995175487],[-116.898197048,45.9805159410001],[-116.856471819,45.903597344],[-116.791262488,45.8458671050001],[-116.773707129,45.8197636120001],[-116.703180308,45.819169243],[-116.654397937,45.7806301590001],[-116.560631908,45.747424568],[-116.52827493,45.710728009],[-116.514915091,45.6644908950001],[-116.470418798,45.6062572690001],[-116.47855126,45.5660584950001],[-116.554503319,45.4936471940001],[-116.565771964,45.4598636740001],[-116.672265496,45.335410289],[-116.688813374,45.262350842],[-116.736585295,45.13730709],[-116.761268253,45.1063007170001],[-116.778091925,45.0994801020001],[-116.787210029,45.0757525120001],[-116.807307342,45.0497552830001],[-116.854513191,45.016945294],[-116.848097045,45.0000421560001],[-116.855887625,44.9799657220001],[-116.848158998,44.9717412370001],[-116.831396506,44.9726336690001],[-116.847556799,44.9548499430001],[-116.835396183,44.9201440900001],[-116.867076347,44.868608447],[-116.89736686,44.848555101],[-116.909620757,44.8289402510001],[-116.951494105,44.7760351260001],[-117.039572246,44.749115712],[-117.066513052,44.697556965],[-117.079353924,44.6893364250001],[-117.13050391,44.5725238650001],[-117.143939864,44.5592869480001],[-117.145160662,44.534655623],[-117.187391126,44.511805601],[-117.203962567,44.485785504],[-117.224409907,44.472987054],[-117.217221803,44.42785524],[-117.236920894,44.3899826420001],[-117.201602017,44.3394380020001],[-117.217455714,44.3006651360001],[-117.213571879,44.284719684],[-117.170722953,44.2533327370001],[-117.14327894,44.250632243],[-117.112691921,44.2698052750001],[-117.100560675,44.2670779000001],[-117.081386868,44.2438466030001],[-117.052027613,44.231555959],[-117.030352023,44.2493365540001],[-116.992707111,44.247063396],[-116.976127235,44.2251823620001],[-116.981871729,44.1978422980001],[-116.913051097,44.1773044500001],[-116.902254133,44.146313831],[-116.946886687,44.0930258100001],[-116.9634432,44.0902981680001],[-116.976817766,44.0738948190001],[-116.933593484,44.0142025690001],[-116.967956848,43.9631956100001],[-116.959715844,43.9285769420001],[-116.978141229,43.904441041],[-116.97814812,43.8734692300001],[-116.985769946,43.8593508850001],[-117.016220427,43.8529724410001],[-117.010505344,43.8397697250001],[-117.027626548,43.8315678470001],[-117.03711733,43.800141963],[-117.023794478,43.7537015920001],[-117.02629522,43.679031222],[-117.018864364,41.99479418],[-118.185316829,41.9966370970001],[-119.31094213,41.989135386],[-119.351692186,41.9888529740001],[-119.99345937,41.9892049520001],[-120.871908519,41.987672177],[-121.441508911,41.9943345300001]]]}},{"attributes":{"STATE_NAME":"Nevada","Shape_Length":23.6087337093515,"Shape_Area":29.9699248474031},"geometry":{"rings":[[[-119.152450421,38.4118009580001],[-119.31882507,38.5271086230001],[-119.575687063,38.7029101290001],[-119.889341639,38.9222515590001],[-119.995254694,38.9941061530001],[-119.995150114,39.0634913590001],[-119.994541258,39.1061318050001],[-119.995527336,39.1587132860001],[-119.995304181,39.3115454320001],[-119.996011479,39.4435009760001],[-119.996165311,39.7206108070001],[-119.99632466,41.1775662650001],[-119.99345937,41.9892049520001],[-119.351692186,41.9888529740001],[-119.31094213,41.989135386],[-118.185316829,41.9966370970001],[-117.018864364,41.99479418],[-116.992313338,41.9947945090001],[-115.947544658,41.9945994620001],[-115.024862911,41.9965064550001],[-114.269471633,41.9959242340001],[-114.039072662,41.9953908970001],[-114.038151249,40.9976868400001],[-114.038108189,40.1110466520001],[-114.039844684,39.9087788590001],[-114.040105339,39.5386849260001],[-114.044267501,38.678995881],[-114.045090206,38.5710950530001],[-114.047272999,38.1376524390001],[-114.047260595,37.598478486],[-114.043939384,36.996537936],[-114.043716436,36.841848945],[-114.037392074,36.2160228960001],[-114.045105557,36.1939778830001],[-114.107775186,36.1210907060001],[-114.129023084,36.041730493],[-114.20676887,36.0172554160001],[-114.233472615,36.018331059],[-114.307587598,36.062233098],[-114.303857056,36.0871084030001],[-114.316095375,36.111438036],[-114.344233942,36.1374802510001],[-114.380803117,36.1509912710001],[-114.443945698,36.121053283],[-114.466613475,36.124711258],[-114.530573569,36.1550902040001],[-114.598935242,36.138335452],[-114.621610747,36.1419666830001],[-114.712761725,36.105181051],[-114.728150311,36.08596277],[-114.728966013,36.0587530350001],[-114.717673568,36.0367580430001],[-114.736212494,35.987648349],[-114.699275906,35.9116119530001],[-114.661600122,35.8804735850001],[-114.662462096,35.8709599060001],[-114.689867343,35.8474424940001],[-114.682739705,35.7647034170001],[-114.688820028,35.7325957390001],[-114.665091346,35.6930994100001],[-114.668486065,35.656398987],[-114.654065925,35.646584079],[-114.639866722,35.6113485690001],[-114.653134321,35.5848331050001],[-114.649792053,35.546637386],[-114.672215156,35.515754164],[-114.645396168,35.4507608250001],[-114.589584275,35.3583787300001],[-114.58788984,35.304768128],[-114.559583046,35.2201828710001],[-114.561039964,35.1743461610001],[-114.572255261,35.1400677440001],[-114.582616239,35.1325604690001],[-114.626440825,35.1339067520001],[-114.635909084,35.1186557760001],[-114.595631972,35.076057974],[-114.633779873,35.0418633500001],[-114.621068606,34.998914428],[-115.626197383,35.795698314],[-115.885769344,36.001225956],[-117.160423772,36.9595941430001],[-117.838686423,37.4572982390001],[-118.417419756,37.8866767480001],[-119.152450421,38.4118009580001]]]}},{"attributes":{"STATE_NAME":"WEATHER","Shape_Length":42.2601573911116,"Shape_Area":41.533618210309},"geometry":{"rings":[[[-121.665219928,38.169285281],[-121.782362663,38.0667758500001],[-121.902765892,38.0729095500001],[-121.984548899,38.1395004700001],[-122.232243019,38.071079791],[-122.273001701,38.1594183910001],[-122.315759127,38.2059335770001],[-122.338907366,38.1935818910001],[-122.285354126,38.1593115750001],[-122.272771919,38.0974845580001],[-122.398463945,38.161337066],[-122.429202751,38.113807195],[-122.488935225,38.113414279],[-122.528648755,38.1506715630001],[-122.474544987,38.0854571230001],[-122.506450381,38.018652176],[-122.441781117,37.982955233],[-122.49002234,37.9317675500001],[-122.458259449,37.834221191],[-122.51572541,37.8221063010001],[-122.666392804,37.9069197430001],[-122.691723723,37.894392666],[-122.822193338,38.0076725280001],[-122.921180932,38.0306229400001],[-122.956597666,37.990757477],[-123.0107302,37.9944662020001],[-122.939271912,38.1532649530001],[-122.994649085,38.2972271990001],[-123.048796645,38.2941413760001],[-123.121544682,38.4335999350001],[-123.297941066,38.5473335460001],[-123.523886601,38.757659318],[-123.721901389,38.924771224],[-123.683447583,39.0418059040001],[-123.81371804,39.3478064490001],[-123.754651576,39.5518792560001],[-123.783531917,39.6871084890001],[-123.838108417,39.8263968810001],[-124.007638557,39.9985808390001],[-124.094560485,40.1003777870001],[-124.345306273,40.2524300100001],[-124.336106834,40.3275549100001],[-124.392637845,40.4352369100001],[-124.109446186,40.9782109500001],[-124.149703258,41.1288322460001],[-124.07160247,41.3138325800001],[-124.057954769,41.4581641670001],[-124.144210356,41.7271932560001],[-124.243098914,41.7767571810001],[-124.207500962,41.848327414],[-124.206444444,41.997647912],[-123.819146439,41.992948772],[-123.513204633,41.997832917],[-123.222102653,42.002191774],[-122.284705082,42.0007645520001],[-121.441508911,41.9943345300001],[-120.871908519,41.987672177],[-119.99345937,41.9892049520001],[-119.99632466,41.1775662650001],[-119.996165311,39.7206108070001],[-119.996011479,39.4435009760001],[-119.995304181,39.3115454320001],[-119.995527336,39.1587132860001],[-119.994541258,39.1061318050001],[-119.995150114,39.0634913590001],[-119.995254694,38.9941061530001],[-119.889341639,38.9222515590001],[-119.575687063,38.7029101290001],[-119.31882507,38.5271086230001],[-119.152450421,38.4118009580001],[-118.417419756,37.8866767480001],[-117.838686423,37.4572982390001],[-117.160423772,36.9595941430001],[-115.885769344,36.001225956],[-115.626197383,35.795698314],[-114.621068606,34.998914428],[-114.63227653,34.9976517250001],[-114.621007389,34.9436098410001],[-114.630475659,34.9195012870001],[-114.62726344,34.8755338140001],[-114.570216833,34.831860438],[-114.542040693,34.7599586190001],[-114.525553174,34.7489115700001],[-114.497804378,34.7447576440001],[-114.465637689,34.7098730180001],[-114.422270356,34.6108950910001],[-114.434302241,34.5989628900001],[-114.409742349,34.5837235610001],[-114.376827823,34.5365634760001],[-114.383862032,34.4770856140001],[-114.376506948,34.4596793680001],[-114.332636412,34.454873079],[-114.302865367,34.4357541370001],[-114.283394305,34.4120690060001],[-114.257842522,34.4054888210001],[-114.182079822,34.3652063640001],[-114.153414998,34.3364477640001],[-114.134127058,34.3145478750001],[-114.125230508,34.272620965],[-114.149912369,34.2669789890001],[-114.235775822,34.186222747],[-114.285368523,34.1712309580001],[-114.322799431,34.141297266],[-114.410166357,34.1026543630001],[-114.424029196,34.0783320570001],[-114.428980324,34.0298439860001],[-114.518208553,33.9650630900001],[-114.525632127,33.9524137550001],[-114.498188092,33.9250362560001],[-114.520962184,33.862926379],[-114.511722549,33.8419650150001],[-114.521122163,33.8260312840001],[-114.504557872,33.7717148130001],[-114.51028751,33.7432004970001],[-114.495676447,33.708369427],[-114.536433559,33.6827352270001],[-114.525263595,33.6655047],[-114.527170511,33.622136513],[-114.540247206,33.580507771],[-114.529420547,33.560072974],[-114.587061706,33.509445557],[-114.598086339,33.4861269500001],[-114.621089579,33.4685989070001],[-114.630573116,33.439424945],[-114.645092242,33.41911608],[-114.724936285,33.4110596370001],[-114.703603782,33.3524180300001],[-114.735426989,33.3057084340001],[-114.677693392,33.268016517],[-114.687711075,33.2392582960001],[-114.680050859,33.2245949420001],[-114.6781204,33.1672499410001],[-114.70946302,33.1223749340001],[-114.711355134,33.0953827790001],[-114.663951696,33.0389226880001],[-114.64515976,33.0444118720001],[-114.633966946,33.033566916],[-114.60992572,33.0270019220001],[-114.559089058,33.0367824780001],[-114.520627662,33.0277074350001],[-114.468387198,32.9777894650001],[-114.476443984,32.9359088460001],[-114.461436322,32.84542251],[-114.526219492,32.80991235],[-114.535077445,32.7880470470001],[-114.530095237,32.7714115220001],[-114.543187696,32.7712322830001],[-114.543004547,32.7607497400001],[-114.561582708,32.7607536240001],[-114.560751027,32.748935976],[-114.572210734,32.748829211],[-114.57195891,32.7374388910001],[-114.603522692,32.7358864770001],[-114.603942285,32.7262851940001],[-114.694040668,32.7414255800001],[-114.712695098,32.7350133480001],[-114.722048985,32.7208574910001],[-116.106973546,32.6194706960001],[-117.128098105,32.5357813430001],[-117.199812068,32.7184424170001],[-117.120606604,32.6028724050001],[-117.124529297,32.6789314820001],[-117.19877466,32.7389343980001],[-117.248206873,32.6800939780001],[-117.285325399,32.8512204960001],[-117.254867898,32.888172845],[-117.328439406,33.1114819910001],[-117.410144185,33.234089468],[-117.597331326,33.3945339350001],[-118.106717457,33.747564599],[-118.246616192,33.7739249850001],[-118.286892272,33.703907438],[-118.405088973,33.738450722],[-118.428954479,33.7754482400001],[-118.388175257,33.8123248520001],[-118.412110378,33.8829675740001],[-118.541854437,34.037251778],[-118.788115085,34.0182570850001],[-118.93936008,34.040081317],[-119.21633453,34.146340648],[-119.266767436,34.2380982650001],[-119.483009812,34.374861889],[-119.60629359,34.4164349210001],[-119.869433246,34.404796284],[-120.011495395,34.4616616220001],[-120.140162829,34.4719023550001],[-120.456202684,34.442499453],[-120.509405995,34.5213738670001],[-120.641293029,34.572337803],[-120.601627028,34.704022551],[-120.631673131,34.759906792],[-120.60815885,34.8556158390001],[-120.665946557,34.9038095260001],[-120.644339565,34.9726369050001],[-120.616765262,35.074816577],[-120.63841022,35.1400283490001],[-120.86134197,35.209253737],[-120.883597598,35.259405384],[-120.849996141,35.364537182],[-120.875212389,35.4277651410001],[-120.991948024,35.4565810720001],[-121.146559304,35.6293227390001],[-121.270261538,35.6635357480001],[-121.329080662,35.8010340340001],[-121.445541578,35.87985052],[-121.689811557,36.1811341980001],[-121.882277425,36.3069435],[-121.955283155,36.582773671],[-121.911420523,36.640427844],[-121.867381728,36.6077136040001],[-121.808564716,36.6482211420001],[-121.761391229,36.818990204],[-121.791711903,36.8503269990001],[-121.883536569,36.9620979730001],[-122.061331831,36.9475067690001],[-122.173442726,37.0008694140001],[-122.27463371,37.106781889],[-122.414637835,37.2391263950001],[-122.389253147,37.3524127670001],[-122.441463098,37.4794824740001],[-122.50568218,37.5229048510001],[-122.498207824,37.7002541020001],[-122.498214531,37.7829421050001],[-122.400931118,37.8086250980001],[-122.346471627,37.725222881],[-122.366331359,37.7024501180001],[-122.359671258,37.609786763],[-122.089307825,37.4525414650001],[-121.975337116,37.460720204],[-122.093023678,37.4973135310001],[-122.199732511,37.7352008890001],[-122.312413627,37.7784626900001],[-122.307553741,37.891763558],[-122.371497009,37.9093451720001],[-122.379683868,37.9734454200001],[-122.295522197,38.0147955020001],[-122.000623251,38.0571514360001],[-121.698956098,38.023495913],[-121.657749868,38.086100866],[-121.576884969,38.094138484],[-121.569545177,38.0636676700001],[-121.547473074,38.0634732070001],[-121.572833853,38.1137988300001],[-121.554149384,38.1373615480001],[-121.659581043,38.0964650610001],[-121.665219928,38.169285281]],[[-119.867823257,34.0752286490001],[-119.667922334,34.0213434360001],[-119.572589325,34.055781165],[-119.523095554,34.034590613],[-119.539377455,34.0064960640001],[-119.712539718,33.965284361],[-119.847275401,33.9684159340001],[-119.889061794,34.0046697170001],[-119.873986856,34.0318755740001],[-119.927690399,34.0591802390001],[-119.867823257,34.0752286490001]],[[-120.167386086,33.9241621960001],[-120.238548701,34.010885241],[-120.046801069,34.041105223],[-119.963385936,33.947763158],[-120.109179246,33.894813979],[-120.167386086,33.9241621960001]],[[-118.594780502,33.480818356],[-118.362395805,33.4110113280001],[-118.294590834,33.3344480910001],[-118.304036434,33.3074940430001],[-118.455386786,33.3247859790001],[-118.481342626,33.419552438],[-118.55643388,33.434482545],[-118.594780502,33.480818356]],[[-118.350958201,32.8191952320001],[-118.420105889,32.8061145340001],[-118.511676947,32.892076219],[-118.599517215,33.02102197],[-118.571485988,33.0359715050001],[-118.541585051,32.9873842710001],[-118.350958201,32.8191952320001]]]}}]}

            return response;
        }
    }
    #endregion
}
