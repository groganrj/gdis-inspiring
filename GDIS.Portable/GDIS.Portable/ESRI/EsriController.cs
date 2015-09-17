using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace AtlasOf.GIS.ESRI
{
    public class EsriController : GISController
    {
        bool _requestProcessing = false;

        #region Constants
        // http://toposervices.cr.usgs.gov/wmsconnector/com.esri.wms.Esrimap/USGS_EDNA_geo?TRANSPARENT=true&LAYERS=NED_2003_SHADEDRELIEF&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&STYLES=&FORMAT=image%2Fpng&SRS=EPSG%3A4326&BBOX=-128.6875,31.425,-111.8125,42.675&WIDTH=768&HEIGHT=512
        public static string BASE_REQUEST = "<?xml version='1.0' encoding='UTF-8'?><ARCXML version='1.1'><REQUEST><GET_IMAGE show='layers' auto_resize='true'><PROPERTIES><IMAGESIZE height='{0}' width='{1}'/><LEGEND title='' font='Century Gothic' height='{1}' width='{2}' autoextend='true' backgroundcolor='225,225,255' titlefontsize='14' layerfontsize='12' valuefontsize='12'></LEGEND></PROPERTIES></GET_IMAGE></REQUEST></ARCXML>";
        public static string GET_CLIENT_SERVICES = "<?xml version='1.0' encoding='UTF-8'?><GETCLIENTSERVICES />";
        public static string GET_METADATA = "<ARCXML version='1.1'><REQUEST><GET_METADATA><SEARCH_METADATA foldermask='4' fulloutput='true' startresult='0' maxresults='10' sort='name' operator='and'><FULLTEXT word='bound*'></FULLTEXT></SEARCH_METADATA></GET_METADATA></REQUEST></ARCXML>";
        public static string GET_SERVICE_INFO = "<?xml version='1.0' encoding='UTF-8'?><ARCXML version='1.1'><REQUEST><GET_SERVICE_INFO envelope='true' fields='true' renderer='false' extensions='true'/></REQUEST></ARCXML>";
        // metadata server http://gos2.geodata.gov/servlet/com.esri.esrimap.Esrimap?ServiceName=catalog
        // http://www.geocommunicator.gov/parcelservlet/com.esri.esrimap.Esrimap?ServiceName=BLM_TWP_INDEX_wms
        private static string CATALOG_URL = "http://{0}/{1}?ServiceName=catalog";
        private static string SERVICE_URL = "http://{0}/{1}?ServiceName={2}&ClientVersion={4}&Form={3}&Encode=False";
        private static string QUERY_URL = "http://{0}/{1}?ServiceName={2}&ClientVersion={4}&Form={3}&Encode=False&CustomService=Query";
        private static string GEOCODE_URL = "http://{0}/{1}?ServiceName={2}&ClientVersion={4}&Form={3}&Encode=False&CustomService=Geocode";
        #endregion

        #region Constructor

        public EsriController(GISServer esriServer)
            : base(esriServer)
        {
            if (string.IsNullOrEmpty(esriServer.ServletPath))
            {
                esriServer.ServletPath = "servlet/com.esri.esrimap.Esrimap";
            }
        }

        #endregion

        #region Methods
        public override void GetQueryLayers(GISService service, ref List<GISLayerInfo> layers)
        {
            var activeQuery = from x in service.ActiveLayers where x.IsQueryable select x;

            if (activeQuery.Count() > 0)
            {
                layers = activeQuery.ToList();
                return;
            }

            activeQuery = from x in service.ActiveLayers where ((EsriLayerInfo)x).FeatureCount > 0 select x;

            if (activeQuery.Count() > 0)
            {
                layers = activeQuery.ToList();
                return;
            }

            if (service.HasLayers || GetServiceDetails(service).Result)
            {
                foreach (EsriLayerInfo info in service.BaseLayers)
                {
                    if (!layers.Contains(info) && info.IsQueryable) layers.Add(info);
                }
            }
        }

        public override void GetImageLayers(GISService service, List<GISLayerInfo> layers)
        {
            if (service.HasLayers || GetServiceDetails(service).Result)
            {
                foreach (GISLayerInfo info in service.BaseLayers)
                {
                    if (!layers.Contains(info)) layers.Add(info);
                }
            }
        }

        //void ClientServicesCompleted(object sender, WebEventArgs e)
        //{
        //    _requestProcessing = false;

        //    if (!e.Success)
        //    {
        //        GISResponse response = GISResponse.ProcessErrorResponse("Controller error: ", "", e.ResponseString);
        //        Server.RaiseErrorResponse(response);
        //    }
        //    else
        //    {
        //        e.Callback(e.ResponseString);
        //    }
        //}

        public override async Task<bool> GetClientServices()
        {
            if (_requestProcessing) return false;

            string requestUrl = String.Format(CATALOG_URL, Server.Host, Server.ServletPath);

            try
            {
                _requestProcessing = true;
                webClient.PostRequest(requestUrl, Server.UseForm ? ApplyFormEncoding(GET_CLIENT_SERVICES) : GET_CLIENT_SERVICES, ProcessServiceReturn, null);
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        SimpleWebClient webClient = new SimpleWebClient();
        //        private static XmlSerializer _info = new XmlSerializer(typeof(SERVICEINFO));

        public override async Task<bool> GetServiceDetails(GISService activeService)
        {
            string requestUrl = String.Format(SERVICE_URL, Server.Host, Server.ServletPath, activeService.Name, false, Server.Version);

            try
            {
                webClient.PostRequest(requestUrl, Server.UseForm ? ApplyFormEncoding(GET_SERVICE_INFO) : GET_SERVICE_INFO, ProcessServiceDetailReturn, activeService);
                return true;
            }
            catch (Exception ex)
            {
                //_returnString = string.Format("{0}: {1}", ex.Message, _returnString);
                return false;
            }
            finally
            {
                //if (responseString != null) responseString.Close();
            }
        }


        public override bool GetMap(GISService activeService)
        {
            GISEnvelope imageEnvelope = SelectEnvelope(activeService);

            return GetMap(activeService, imageEnvelope);
        }

        public override bool GetMap(GISService activeService, GISEnvelope imageEnvelope)
        {
            List<GISLayerInfo> totalLayers = GetTotalLayers(activeService);
            return GetMap(activeService, totalLayers, imageEnvelope, SetZoomLevel(imageEnvelope, activeService._baseExtent), BuildRequest(ImageHeight, ImageWidth, imageEnvelope, totalLayers));
        }

        public override bool GetMap(GISService activeService, double centerX, double centerY, double zoomLevel)
        {
            GISEnvelope mapEnvelope = BuildEnvelope(centerX, centerY, SelectEnvelope(activeService), zoomLevel);
            List<GISLayerInfo> totalLayers = new List<GISLayerInfo>();// GetTotalLayers(activeService);

            return GetMap(activeService, totalLayers, mapEnvelope, zoomLevel, BuildRequest(ImageHeight, ImageWidth, mapEnvelope, totalLayers));
        }

        public override bool GetMap(GISService activeService, GISEnvelope mapEnvelope, List<GISLayerInfo> mapLayers)
        {
            List<GISLayerInfo> totalLayers = GetTotalLayers(activeService);

            return GetMap(activeService, totalLayers, mapEnvelope, SetZoomLevel(mapEnvelope, activeService._baseExtent), BuildRequest(ImageHeight, ImageWidth, mapEnvelope, totalLayers));
        }

        private List<GISLayerInfo> GetTotalLayers(GISService activeService)
        {
            List<GISLayerInfo> totalLayers = new List<GISLayerInfo>();

            var visibleLayers = from x in activeService._baseLayers where x._isVisible select x;
            int count = visibleLayers.Count();

            if (activeService.ActiveLayers.Count > 0)
            {
                totalLayers.AddRange(activeService.ActiveLayers);

                if (count > 0)
                {
                    foreach (GISLayerInfo info in visibleLayers)
                    {
                        if (!totalLayers.Contains(info))
                        {
                            totalLayers.Add(CreateLayer(info._name, info._id, false));
                        }
                    }
                }
            }
            else
            {
                foreach (GISLayerInfo info in activeService._baseLayers)
                {
                    if (!totalLayers.Contains(info))
                    {
                        totalLayers.Add(info);
                    }
                }
            }

            return totalLayers;
        }

        public override bool GetMap(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, string requestXml)
        {
            string requestUrl = String.Format(SERVICE_URL, Server.Host, Server.ServletPath, activeService.Id, Server.UseForm, Server.Version);
            XmlReader responseString = null;
            EsriImageResponse response = new EsriImageResponse();

            try
            {
                response.Envelope = mapEnvelope;
                response._layers.AddRange(mapLayers);
                response.LastRequest = requestXml;
                response._mapImageUrl = requestUrl;
                response.ZoomLevel = zoomLevel;

                // http://websig.hidrografico.pt/servlet/com.esri.esrimap.Esrimap?ServiceName=ICENCUK&ClientVersion=4.0&Form=True&Encode=False
                webClient.PostRequest(requestUrl, Server.UseForm ? ApplyFormEncoding(requestXml) : requestXml, ProcessImageReturn, response);
                return true;
            }
            catch (Exception ex)
            {
                return false; // new GISResponse() { _envelope = mapEnvelope, _layers = mapLayers, ErrorMessage = ex.Message, HasError = true, LastRequest = requestXml, LastResponse = Return };
                //return GISResponse.ProcessErrorResponse("The last map request failed: " + ex.Message, Request, Return);
            }
            finally
            {
                //if (responseString != null) responseString.Close();
            }
        }

        public override string GetErrorMessage(string responseXml)
        {
            return responseXml;
        }

        public override GISService CreateService(string serviceName)
        {
            return new EsriService(serviceName);
        }

        public override GISService AddService(string serviceName, string serviceId, ServiceType serviceType)
        {
            EsriService svc = new EsriService(serviceName) { _type = serviceType, _serviceId = serviceId };
            Server._services.Add(svc);
            return svc;
        }

        public override GISLayerInfo CreateLayer(string layerName, string layerId, bool isVisible)
        {
            return new EsriLayerInfo() { _id = layerId, _isVisible = isVisible, _name = layerName };
        }

        public override void ExecuteSearch(string searchTerm, SEARCH_TYPE searchType, GISEnvelope searchArea, GISLayerInfo featureLayer)
        {
            int maxFeaturesReturned = 20;
            string requestUrl = String.Format(QUERY_URL, Server.Host, Server.ServletPath, Server.ActiveService.Id, false, Server.Version);

            string requestXml = BuildQuery(featureLayer, maxFeaturesReturned, searchArea);

            EsriFeatureResponse response = new EsriFeatureResponse() { SearchTerm = searchTerm };
            response._envelope = searchArea;
            response._layers = new List<GISLayerInfo>() { featureLayer };
            response.HasError = false;
            response.LastRequest = requestXml;

            webClient.PostRequest(requestUrl, Server.UseForm ? ApplyFormEncoding(requestXml) : requestXml, ProcessSearchReturn, response);
        }

        #endregion

        #region Functions
        public static string HtmlEncode(string text)
        {
            if (text == null)
                return null;

            StringBuilder sb = new StringBuilder(text.Length);

            int len = text.Length;
            for (int i = 0; i < len; i++)
            {
                switch (text[i])
                {

                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    default:
                        if (text[i] > 159)
                        {
                            // decimal numeric entity
                            sb.Append("&#");
                            sb.Append(((int)text[i]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            sb.Append(";");
                        }
                        else
                            sb.Append(text[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        private string ApplyFormEncoding(string requestString)
        {
#if SILVERLIGHT
            return "";
            //return string.Format("ArcXMLRequest={0}&JavaScriptFunction=parent.MapFrame.processXML&BgColor=%23330000&FormCharset=ISO-8859-1&RedirectURL=&HeaderFile=&FooterFile=", System.Windows.Browser.HttpUtility.HtmlEncode(requestString));
#else
            return string.Format("ArcXMLRequest={0}&JavaScriptFunction=parent.MapFrame.processXML&BgColor=%23330000&FormCharset=ISO-8859-1&RedirectURL=&HeaderFile=&FooterFile=", HtmlEncode(requestString));
            //return string.Empty;
#endif
        }

        private string RemoveFormEncoding(string returnString)
        {
            return returnString.Substring(returnString.IndexOf("<ARCXML"), returnString.IndexOf("</ARCXML>") + 9 - returnString.IndexOf("<ARCXML"));
            //            return string.Format("ARCXML={0}", HttpUtility.HtmlEncode(requestString));
        }


        private string BuildRequest(int imageHeight, int imageWidth, GISEnvelope imageEnvelope, List<GISLayerInfo> layers)
        {
            StringBuilder requestString = new StringBuilder();
            System.Xml.XmlWriter requestWriter = XmlWriter.Create(requestString);
            requestWriter.WriteStartElement("ARCXML");
            requestWriter.WriteAttributeString("", "VERSION", "", "1.1");
            requestWriter.WriteStartElement("REQUEST");
            requestWriter.WriteStartElement("GET_IMAGE");
            requestWriter.WriteAttributeString("", "auto_resize", "", "true");
            requestWriter.WriteAttributeString("", "show", "", "layers");

            requestWriter.WriteStartElement("PROPERTIES");

            requestWriter.WriteStartElement("IMAGESIZE");
            requestWriter.WriteAttributeString("", "width", "", imageWidth.ToString());
            requestWriter.WriteAttributeString("", "height", "", imageHeight.ToString());
            requestWriter.WriteEndElement(); // IMAGESIZE

            EsriEnvelope.AsXml(requestWriter, imageEnvelope);
            new MapElement("LEGEND").AsXml(requestWriter);

            //if (layers != null) AddLayers(requestWriter, layers);

            if (layers != null && layers.Count > 0) AddLayerInfo(requestWriter, layers);

            requestWriter.WriteEndElement(); // PROPERTIES

            requestWriter.WriteEndElement(); // GET_IMAGE
            requestWriter.WriteEndElement(); // REQUEST
            requestWriter.WriteEndElement(); // ARCXML
            requestWriter.Flush();

            return requestString.ToString();
        }

        private void AddLayerInfo(XmlWriter messageWriter, List<GISLayerInfo> layers)
        {
            messageWriter.WriteStartElement("LAYERLIST");
            foreach (GISLayerInfo layer in layers)
            {
                EsriLayerInfo.GetRequestXml(layer, messageWriter);
            }
            messageWriter.WriteEndElement();
        }

        private string BuildQuery(string queryLayer, int maxFeaturesReturned, MapElement spatialQuery)
        {
            StringBuilder queryString = new StringBuilder();
            StringWriter messageString = new StringWriter(queryString);
            XmlWriter queryWriter = XmlWriter.Create(messageString);

            queryWriter.WriteStartElement("ARCXML");
            queryWriter.WriteAttributeString("", "VERSION", "", "1.1");
            queryWriter.WriteStartElement("REQUEST");
            queryWriter.WriteStartElement("GET_FEATURES");
            queryWriter.WriteAttributeString("", "outputmode", "", "xml");
            queryWriter.WriteAttributeString("", "geometry", "", "false");
            queryWriter.WriteAttributeString("", "envelope", "", "true");
            queryWriter.WriteAttributeString("", "beginrecord", "", "0");
            queryWriter.WriteAttributeString("", "globalenvelope", "", "true");

            queryWriter.WriteAttributeString("", "featurelimit", "", maxFeaturesReturned.ToString());

            queryWriter.WriteStartElement("PROPERTIES");
            spatialQuery.AsXml(queryWriter);

            queryWriter.WriteEndElement(); // PROPERTIES
            queryWriter.WriteEndElement(); // GET_FEATURES
            queryWriter.WriteEndElement(); // REQUEST
            queryWriter.WriteEndElement(); // ARCXML

            return queryString.ToString();
        }

        private string BuildGeocode(List<GISLayerInfo> queryLayers, int maxFeaturesReturned, GISEnvelope mapEnvelope)
        {
            //<ARCXML version="1.1">
            //  <REQUEST> 
            //    <GET_GEOCODE maxcandidates="25"  minscore="60">
            //      <LAYER id="streets" />
            //      <ADDRESS>
            //        <GCTAG id="STREET" value="380 New York St" />
            //        <GCTAG id="Zone" value="92373" />
            //        <GCTAG id="CrossStreet" value="" />
            //      </ADDRESS>
            //    </GET_GEOCODE>
            //  </REQUEST>       
            //</ARCXML>        
            StringBuilder queryString = new StringBuilder();
            StringWriter messageString = new StringWriter(queryString);
            XmlWriter queryWriter = XmlWriter.Create(messageString);

            queryWriter.WriteStartElement("ARCXML");
            queryWriter.WriteAttributeString("", "VERSION", "", "1.1");
            queryWriter.WriteStartElement("REQUEST");
            queryWriter.WriteStartElement("GET_GEOCODE");
            queryWriter.WriteAttributeString("", "maxcandidates", "", "25");

            foreach (GISLayerInfo queryLayer in queryLayers)
            {
                queryWriter.WriteStartElement("LAYER");
                queryWriter.WriteAttributeString("", "id", "", queryLayer.Id);
                queryWriter.WriteAttributeString("", "name", "", queryLayer.Name);
                queryWriter.WriteEndElement();
            }

            queryWriter.WriteEndElement(); // GET_GEOCODE
            queryWriter.WriteEndElement(); // REQUEST
            queryWriter.WriteEndElement(); // ARCXML

            queryWriter.Flush();
            return queryString.ToString();
        }

        private string BuildQuery(GISLayerInfo queryLayer, int maxFeaturesReturned, GISEnvelope spatialQuery)
        {
            StringBuilder queryString = new StringBuilder();
            StringWriter messageString = new StringWriter(queryString);
            XmlWriter queryWriter = XmlWriter.Create(messageString);

            queryWriter.WriteStartElement("ARCXML");
            queryWriter.WriteAttributeString("", "VERSION", "", "1.1");
            queryWriter.WriteStartElement("REQUEST");
            queryWriter.WriteStartElement("GET_FEATURES");
            queryWriter.WriteAttributeString("", "outputmode", "", "xml");
            queryWriter.WriteAttributeString("", "geometry", "", "false");
            queryWriter.WriteAttributeString("", "envelope", "", "true");
            queryWriter.WriteAttributeString("", "beginrecord", "", "0");
            queryWriter.WriteAttributeString("", "checkesc", "", "true");
            queryWriter.WriteAttributeString("", "compact", "", "true");

            queryWriter.WriteStartElement("LAYER");
            queryWriter.WriteAttributeString("", "id", "", queryLayer.Id);
            queryWriter.WriteAttributeString("", "name", "", queryLayer.Name);
            queryWriter.WriteEndElement();

            queryWriter.WriteStartElement("SPATIALQUERY");
            queryWriter.WriteAttributeString("", "subfields", "", "#ALL#");
            queryWriter.WriteStartElement("SPATIALFILTER");
            queryWriter.WriteAttributeString("", "relation", "", "area_intersection");
            EsriEnvelope.AsXml(queryWriter, spatialQuery);
            queryWriter.WriteEndElement(); // SPATIALFILTER
            queryWriter.WriteEndElement(); // SPATIALQUERY
            //queryWriter.WriteEndElement(); // PROPERTIES
            queryWriter.WriteEndElement(); // GET_FEATURES
            queryWriter.WriteEndElement(); // REQUEST
            queryWriter.WriteEndElement(); // ARCXML

            queryWriter.Flush();
            return queryString.ToString();
        }

        private string BuildRequest(GISEnvelope envelope)
        {
            return BuildRequest(ImageHeight, ImageWidth, envelope, new MapElement("LEGEND"), null, null);
        }

        private string BuildRequest(EsriLayer imageLayer, GISEnvelope envelope)
        {
            return BuildRequest(ImageHeight, ImageWidth, envelope, new MapElement("LEGEND"), null, new Dictionary<string, EsriLayer>() { { imageLayer.Name, imageLayer } });
        }

        private string BuildRequest(int imageHeight, int imageWidth, GISEnvelope requestEnvelope, MapElement legendElement, List<GISLayerInfo> layerInfo, Dictionary<string, EsriLayer> layers)
        {
            StringBuilder requestString = new StringBuilder();
            XmlWriter requestWriter = XmlWriter.Create(requestString);

            requestWriter.WriteStartElement("ARCXML");
            requestWriter.WriteAttributeString("", "VERSION", "", "1.1");
            requestWriter.WriteStartElement("REQUEST");
            requestWriter.WriteStartElement("GET_IMAGE");
            requestWriter.WriteAttributeString("", "auto_resize", "", "true");
            requestWriter.WriteAttributeString("", "show", "", "layers");

            requestWriter.WriteStartElement("PROPERTIES");

            requestWriter.WriteStartElement("IMAGESIZE");
            requestWriter.WriteAttributeString("", "width", "", imageWidth.ToString());
            requestWriter.WriteAttributeString("", "height", "", imageHeight.ToString());
            requestWriter.WriteEndElement(); // IMAGESIZE

            EsriEnvelope.AsXml(requestWriter, requestEnvelope);

            //            if (layerDefs != null) AddLayerlist(requestWriter, layerDefs);

            if (legendElement != null)
            {
                legendElement.AsXml(requestWriter);
            }

            if (layers != null) AddLayers(requestWriter, layers);

            if (layerInfo != null) AddLayerInfo(requestWriter, layerInfo);

            requestWriter.WriteEndElement(); // PROPERTIES
            requestWriter.WriteEndElement(); // GET_IMAGE
            requestWriter.WriteEndElement(); // REQUEST
            requestWriter.WriteEndElement(); // ARCXML
            requestWriter.Flush();

            return requestString.ToString();
        }

        private void AddLayerlist(XmlWriter messageWriter, Dictionary<string, LayerDef> layerDefs)
        {
            messageWriter.WriteStartElement("LAYERLIST");

            foreach (KeyValuePair<string, LayerDef> layerDef in layerDefs)
            {
                layerDef.Value.AsXml(messageWriter);
            }

            messageWriter.WriteEndElement(); //  LAYERLIST
        }

        private void AddLayers(XmlWriter messageWriter, List<EsriLayer> layers)
        {
            messageWriter.WriteStartElement("LAYERLIST");
            foreach (EsriLayer layer in layers)
            {
                layer.AsXml(messageWriter);
            }
            messageWriter.WriteEndElement();
        }

        private void AddLayers(XmlWriter messageWriter, Dictionary<string, EsriLayer> layers)
        {
            foreach (KeyValuePair<string, EsriLayer> layer in layers)
            {
                layer.Value.AsXml(messageWriter);
            }
        }

        private void ProcessImageReturn(object objReader, WebEventArgs e)
        {
            GISResponse response = null;

            try
            {
                if (Server.UseForm) e.ResponseString = RemoveFormEncoding(e.ResponseString);

                StringReader reader = new StringReader(e.ResponseString.Substring(e.ResponseString.IndexOf("?>") + 2));

                XmlReader responseReader = XmlReader.Create(reader, new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment });

                while (responseReader.Read())
                {
                    switch (responseReader.LocalName)
                    {
                        case "ERROR":
                            response = GISResponse.ProcessErrorResponse("Controller error: ", e.LastRequest, e.ResponseString);
                            Server.RaiseErrorResponse(response);
                            break;
                        case "IMAGE":
                            response = EsriImageResponse.ProcessImageReturn(responseReader, e.LastRequest, e.ResponseString);

                            if (response is GISImageResponse)
                            {
                                Uri responseUri;

                                if (Uri.TryCreate((response as GISImageResponse)._mapImageUrl, UriKind.Absolute, out responseUri))
                                {
                                    if (responseUri.Host != Server.Host && (responseUri.Host.StartsWith("192") || responseUri.Host.StartsWith("10.")))
                                    {
                                        UriBuilder builder = new UriBuilder("http", Server.Host, 80, "wmsconnector" + responseUri.LocalPath);
                                        (response as GISImageResponse)._mapImageUrl = builder.Uri.AbsoluteUri;
                                    }
                                }
                            }

                            response.ZoomLevel = (e.UserState as GISResponse).ZoomLevel;
                            Server.RaiseMapResponse(response);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                response = GISResponse.ProcessErrorResponse("Controller error: " + ex.Message, e.LastRequest, e.ResponseString);
                Server.RaiseErrorResponse(response);
            }
        }

        private void ProcessQueryReturn(object objReader, WebEventArgs e)
        {
            GISResponse response = null;

            try
            {
                XmlReader responseReader = GetCleanReturnXml(e.ResponseString);

                while (responseReader.Read())
                {
                    switch (responseReader.LocalName)
                    {
                        case "ERROR":
                            response = GISResponse.ProcessErrorResponse("Controller error: ", e.LastRequest, e.ResponseString);
                            Server.RaiseErrorResponse(response);
                            break;
                        case "FEATURES":
                            response = e.UserState as GISResponse;
                            EsriFeatureResponse.ProcessFeatureReturn(responseReader, response as EsriFeatureResponse, e.ResponseString);
                            Server.RaiseIdentifyResponse(response);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                response = GISResponse.ProcessErrorResponse("Controller error: " + ex.Message, e.LastRequest, e.ResponseString);
                Server.RaiseErrorResponse(response);
            }
        }

        private void ProcessSearchReturn(object objReader, WebEventArgs e)
        {
            GISResponse response = null;

            try
            {
                XmlReader responseReader = GetCleanReturnXml(e.ResponseString);

                while (responseReader.Read())
                {
                    switch (responseReader.LocalName)
                    {
                        case "ERROR":
                            response = GISResponse.ProcessErrorResponse("Controller error: ", e.LastRequest, e.ResponseString);
                            Server.RaiseErrorResponse(response);
                            break;
                        case "FEATURES":
                            response = e.UserState as GISResponse;
                            EsriFeatureResponse.ProcessFeatureReturn(responseReader, response as EsriFeatureResponse, e.ResponseString);
                            Server.RaiseSearchResponse(response);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                response = GISResponse.ProcessErrorResponse("Controller error: " + ex.Message, e.LastRequest, e.ResponseString);
                Server.RaiseErrorResponse(response);
            }
        }
        private XmlReader GetCleanReturnXml(string returnXml)
        {
            returnXml = returnXml.Replace("&", "&amp;");
            returnXml = returnXml.Replace("#", "_");
            returnXml = returnXml.Replace("2003_", "A2003_");

            StringReader responseString = new StringReader(returnXml);
            XmlReader responseReader = XmlReader.Create(responseString);
            return responseReader;
        }

        private void ProcessServiceDetailReturn(object objReader, WebEventArgs e)
        {
            try
            {
                XmlReader responseReader = GetCleanReturnXml(e.ResponseString);
                GISService service = e.UserState as GISService;
                Server.SetActiveService(service.Name);
                EsriService.AddServiceInfo(Server.ActiveService, responseReader);
                Server.RaiseServiceDetailResponse(Server.ActiveService);
            }
            catch (Exception ex)
            {
            }
        }

        private void ProcessServiceReturn(object objReader, WebEventArgs e)
        {
            GISResponse response = null;
            _Server._services.Clear();

            try
            {
                XmlReader responseReader = GetCleanReturnXml(e.ResponseString);

                while (responseReader.Read())
                {
                    switch (responseReader.LocalName)
                    {
                        case "ERROR":
                            response = GISResponse.ProcessErrorResponse("Controller error: ", e.LastRequest, e.ResponseString);
                            Server.RaiseErrorResponse(response);
                            break;
                        case "SERVICES":
                            EsriServiceResponse.ProcessServiceReturn(responseReader, Server, e.ResponseString);

                            var svc = from x in Server._services where x.ServiceType == ServiceType.ImageServer || x.ServiceType == ServiceType.MapServer select x;
                            Server.SetActiveService(svc.First().Name);
                            Server.RaiseServiceResponse();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                response = GISResponse.ProcessErrorResponse("Controller error: " + ex.Message, "", e.ResponseString);
                Server.RaiseErrorResponse(response);
            }
        }
        #endregion

        //        <?xml version="1.0" encoding="UTF-8"?>

        public override bool Identify(int xpoint, int ypoint, double centerY, double centerX, GISEnvelope envelope, GISLayerInfo featureLayer)
        {
            int maxFeaturesReturned = 20;
            string requestUrl = String.Format(QUERY_URL, Server.Host, Server.ServletPath, Server.ActiveService.Id, false, Server.Version);

            GISEnvelope mapEnvelope = BuildEnvelope(centerX, centerY, SelectEnvelope(Server.ActiveService), 99);


            string requestXml = BuildQuery(featureLayer, maxFeaturesReturned, mapEnvelope);

            EsriFeatureResponse response = new EsriFeatureResponse();
            response._envelope = mapEnvelope;
            response._layers = new List<GISLayerInfo>() { featureLayer };
            response.HasError = false;
            response.LastRequest = requestXml;

            webClient.PostRequest(requestUrl, Server.UseForm ? ApplyFormEncoding(requestXml) : requestXml, ProcessServiceReturn, response);
            return true;
        }

        public override IGISLegend GetLegend(GISLayerInfo selectedLayer)
        {
            try
            {
                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> response = httpClient.GetAsync(string.Empty);
                System.Threading.Tasks.Task<string> responseBody = response.Result.Content.ReadAsStringAsync();

                MemoryStream stream1 = new MemoryStream();
                StreamWriter sw = new StreamWriter(stream1);
                sw.Write(responseBody);
                System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(EsriLegend));
                StreamReader sr = new StreamReader(stream1);
                EsriLegend p2 = (EsriLegend)ser.ReadObject(stream1);
                return p2;
            }
            catch (System.Net.WebException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override string GetMapImage(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, int height, int width)
        {
            // http://toposervices.cr.usgs.gov/wmsconnector/com.esri.wms.Esrimap/USGS_EDNA_geo?TRANSPARENT=true&LAYERS=NED_2003_SHADEDRELIEF&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&STYLES=&FORMAT=image%2Fpng&SRS=EPSG%3A4326&BBOX=-128.6875,31.425,-111.8125,42.675&WIDTH=768&HEIGHT=512

            StringBuilder layerstring = new StringBuilder();

            if (!mapLayers.Any())
            {
                var layerList = activeService.BaseLayers.Take(Math.Min(activeService.BaseLayers.Count, 5)).ToList();

                foreach (GISLayerInfo layer in layerList)
                {
                    layerstring.AppendFormat("{0},", layer.Id);
                }
            }
            else
            {
                foreach (var l in mapLayers)
                {
                    layerstring.AppendFormat("{0},", l.Id);
                }
            }

            return string.Format("http://{0}/wmsconnector/com.esri.wms.Esrimap/{1}?TRANSPARENT=true&LAYERS={2}&SERVICE=WMS&VERSION={3}&REQUEST=GetMap&STYLES=&FORMAT=image%2Fpng&SRS={4}&BBOX={5}&WIDTH={6}&HEIGHT={7}", Server.Host, activeService.Name, layerstring, Server.Version, mapEnvelope.CoordinateSystem, mapEnvelope.ToBBoxString(), width, height);
        }
    }
}
