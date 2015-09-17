using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AtlasOf.GIS.WMS
{
    internal enum OGC_OPERATION
    {
        GetFeature,
        GetFeatureInfo,
        GetMap,
        GetLegendGraphic,
        GetCapabilities
    }

    internal enum OGC_SERVICE_TYPE
    {
        WMS,
        WFS
    }

    public class OGCController : GISController
    {
        #region Constants
        //http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetFeatureInfo&LAYERS=us_states_gen&QUERY_LAYERS=us_states_gen&STYLES=&BBOX=-75.684814453125,42.49640294093705,-69.093017578125,45.65244828675087&FEATURE_COUNT=5&HEIGHT=400&WIDTH=600&FORMAT=image%2Fpng&INFO_FORMAT=text%2Fxml&SRS=EPSG%3A4326&X=569&Y=175
        private const string OGC_URL = "{0}request={1}&service={2}&version={3}";
        private const string FEATURE_INFO_URL = "http://{0}/{1}?SERVICE={2}&VERSION={3}&REQUEST=GetFeatureInfo&LAYERS={4}&QUERY_LAYERS={4}&STYLES=&BBOX={5}&FEATURE_COUNT=10&HEIGHT={6}&WIDTH={7}&FORMAT=image%2Fpng&INFO_FORMAT=text%2Fxml&SRS={8}&X={9}&Y={10}";
        private const string OGC_MAP = "&WIDTH={0}&HEIGHT={1}&LAYERS={2}&{3}&QUALITY=MEDIUM&FORMAT={5}&styles={4}"; // &EXCEPTIONS=application/vnd.ogc.se_xml
        private const string OGC_FEATURE_INFO = "&VERSION={0}&{1}&QUERY_LAYERS={2}&WIDTH={4}&HEIGHT={5}&X={6}&Y={7}&EXCEPTIONS=application/vnd.ogc.se_xml";
        //private const string OGC_FEATURE_INFO = "&VERSION={0}&{1}&QUERY_LAYERS={2}&SERVICENAME={3}&WIDTH={4}&HEIGHT={5}&X={6}&Y={7}&EXCEPTIONS=application/vnd.ogc.se_xml";
        private const string OGC_FEATURE = "&typename={0}&{1}&INFO_FORMAT=text/html&EXCEPTIONS=application/vnd.ogc.se_xml";
        private static string LEGEND_URL = "&version={0}&layer={1}&format={2}";
        #endregion

        bool _requestProcessing = false;

        public OGCController(GISServer server) : base(server) { }

        public override string GetMapImage(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, int width, int height)
        {
            List<OGCLayer> ogcmapLayers = new List<OGCLayer>();

            if (!mapLayers.Any())
            {
                List<OGCLayer> ogcLayers = new List<OGCLayer>();
                var layerList = activeService.BaseLayers.ToList();

                foreach (GISLayerInfo layer in layerList)
                {
                    ogcLayers.Add(layer as OGCLayer);
                }

                ogcmapLayers = ogcLayers;
            }
            else
            {
                foreach (var l in mapLayers)
                {
                    if (l is OGCLayer)
                    {
                        ogcmapLayers.Add(l as OGCLayer);
                    }
                    else
                    {
                        ogcmapLayers.Add(new OGCLayer(l.Name, l.Id));
                    }
                }
            }

            return GetUrl(OGC_OPERATION.GetMap, OGC_SERVICE_TYPE.WMS, activeService as OGCService, ogcmapLayers, new OGCEnvelope(mapEnvelope), width, height);
        }

        private string GetUrl(OGC_OPERATION operation, OGC_SERVICE_TYPE type, OGCService service, IEnumerable<OGCLayer> layers, OGCEnvelope envelope, int width, int height, int x = 0, int y = 0)
        {
            StringBuilder requestUrl = new StringBuilder();

            requestUrl.AppendFormat("http://{0}/{1}?", Server.Host, Server.ServletPath);

            string stylename = string.Empty;

            switch (operation)
            {
                case OGC_OPERATION.GetCapabilities:
                    requestUrl.AppendFormat("request=GetCapabilities&version={0}&service={1}", Server.Version, type);
                    break;
                case OGC_OPERATION.GetMap:
                    requestUrl.AppendFormat(OGC_URL, service._getMapUrl, operation, type, Server.Version);

                    if (layers.Any() && layers.First().Style != null && layers.First().Style.Count > 0)
                    {
                        var style = from xx in layers where xx.Style != null && xx.Style.Count > 0 select xx.Style.First();
                        stylename = style.First().Name;
                    }

                    var fixedLayer = from qq in layers where qq.FixedHeight > 0 select qq;

                    if (fixedLayer.Count() > 0)
                    {
                        if (fixedLayer.First().Style != null && fixedLayer.First().Style.Count > 0)
                        {
                            stylename = fixedLayer.First().Style.First().Name;
                        }

                        requestUrl.AppendFormat(OGC_MAP, fixedLayer.First().FixedWidth, fixedLayer.First().FixedHeight, fixedLayer.First().Id, envelope, stylename);
                    }
                    else
                    {
                        string currentlayerId = BuildLayerString(layers, ref stylename);

                        requestUrl.AppendFormat(OGC_MAP, _imageWidth, height, currentlayerId, envelope, stylename, service.Format);
                    }
                    break;
                case OGC_OPERATION.GetFeatureInfo:
                    string layerString = BuildLayerString(layers, ref stylename);
                    requestUrl.AppendFormat(OGC_URL, service._getMapUrl, operation, type, Server.Version);
                    requestUrl.AppendFormat(OGC_FEATURE_INFO, Server.Version, envelope, layerString, type, _imageWidth, _imageHeight, x, y);
                    break;
                case OGC_OPERATION.GetFeature:
                    string layerString2 = BuildLayerString(layers, ref stylename);
                    requestUrl.AppendFormat(OGC_URL, service._getFeatureUrl, operation, type, "1.0.0");
                    requestUrl.AppendFormat(OGC_FEATURE, layerString2, envelope);
                    break;
                case OGC_OPERATION.GetLegendGraphic:
                    if (layers.First().Style != null && layers.First().Style.Count > 0)
                    {
                        var url = from xx in layers.First().Style where !string.IsNullOrEmpty(xx.LegendURL) select xx;

                        if (url.Count() > 0) return url.First().LegendURL;
                    }

                    string legendlayerId = BuildLayerString(layers, ref stylename);
                    requestUrl.AppendFormat(OGC_URL, service._getMapUrl, operation, type, Server.Version);
                    requestUrl.AppendFormat(LEGEND_URL, Server.Version, legendlayerId, service.Format);
                    break;
            }

            if (!string.IsNullOrEmpty(Server.Map))
            {
                requestUrl.AppendFormat("&map={0}", Server.Map);
            }

            return requestUrl.ToString();
        }

        private static string BuildLayerString(IEnumerable<OGCLayer> layers, ref string stylename, int maxLayers = 10)
        {
            string currentlayerId = string.Empty;
            int layerCount = 0;

            foreach (OGCLayer info in layers)
            {
                if (layerCount++ > maxLayers) break;
 
                if (string.IsNullOrEmpty(currentlayerId))
                {
                    currentlayerId = info._id;
                }
                else currentlayerId = string.Format("{0},{1}", currentlayerId, info._id);

                if (string.IsNullOrEmpty(stylename) && info.Style != null && info.Style.Count > 0)
                {
                    stylename = info.Style[0].Name;
                }
            }
            return currentlayerId;
        }

        #region Methods

        public override async Task<bool> GetClientServices()
        {
            if (_requestProcessing) return false;

            string requestUrl = GetUrl(OGC_OPERATION.GetCapabilities, OGC_SERVICE_TYPE.WMS, null, null, new OGCEnvelope(GISEnvelope.TheWorld), _imageWidth, _imageHeight);

            try
            {
                _requestProcessing = true;
                var result = await webClient.GetRequestAsync(requestUrl);

                if (result.success)
                {
                    if (ProcessServiceReturn(result.output))
                        Server.RaiseServiceResponse();
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private GISEnvelope GetExtentFromElement(XElement element, string xnamespace)
        {
            var extent = element.Descendants(XName.Get("BoundingBox", xnamespace));

            if (extent.Count() > 0)
            {
                return OGCEnvelope.Parse(extent.First());
            }
            else
            {
                extent = element.Descendants(XName.Get("LatLonBoundingBox", xnamespace));
                if (extent.Count() > 0)
                {
                    GISEnvelope baseExtent = OGCEnvelope.Parse(extent.First());

                    extent = element.Descendants(XName.Get("SRS", xnamespace));

                    if (extent.Count() > 0)
                    {
                        extent = element.Descendants(XName.Get("CRS", xnamespace));
                    }

                    if (extent.Count() > 0)
                    {
                        baseExtent.CoordinateSystem = extent.First().Value;
                    }
                    else baseExtent.CoordinateSystem = "EPSG:4326";

                    return baseExtent;
                }

                return GISEnvelope.TheWorld;
            }
        }

        private bool ProcessServiceReturn(string responseString)
        {
            _requestProcessing = false;

            //      <BoundingBox SRS="EPSG:4269" minx="-180" miny="-90" maxx="180" maxy="90"/>
            //<Layer queryable="1">
            //  <Name>world_countries_label</Name>
            //  <Title>Map Background - World Countries Labels</Title>
            //  <SRS>EPSG:4326</SRS>
            //  <LatLonBoundingBox minx="-180" miny="-90" maxx="180" maxy="90"/>
            //</Layer>
            try
            {
                string baseTag = "WMT_MS_Capabilities";
                string xnamespace = string.Empty;

                int idx = responseString.IndexOf("<WMT_MS_Capabilities");

                if (idx < 0)
                {
                    idx = responseString.IndexOf("<WMS_Capabilities");
                    baseTag = "WMS_Capabilities";
                    xnamespace = "http://www.opengis.net/wms";
                }

                XElement document = XElement.Parse(responseString.Substring(idx), LoadOptions.SetBaseUri);

                IEnumerable<XElement> serviceElements = document.Elements(XName.Get("Capability", xnamespace)).First().Elements(XName.Get("Layer", xnamespace));

                if (serviceElements.Count() == 1 && serviceElements.First().Elements(XName.Get("Layer", xnamespace)).Count() == 0)
                {
                }
                else if (serviceElements.Count() == 1 && serviceElements.First().Elements(XName.Get("Layer", xnamespace)).First().Elements(XName.Get("Layer", xnamespace)).Count() > 0)
                {
                    serviceElements = serviceElements.First().Elements(XName.Get("Layer", xnamespace));
                }

                _Server._services.Clear();

                foreach (XElement selement in serviceElements)
                {
                    GISService newSvc = new OGCService(selement.Elements(XName.Get("Title", xnamespace)).First().Value) { _description = selement.Elements(XName.Get("Title", xnamespace)).First().Value };
                    newSvc.BaseExtent = GetExtentFromElement(selement, xnamespace);

                    var layers = selement.Elements(XName.Get("Layer", xnamespace));

                    if (layers.Count() > 0)
                    {
                        foreach (XElement element in layers)
                        {
                            //                        <Layer queryable="1">
                            //  <Name>world_countries_label</Name>
                            //  <Title>Map Background - World Countries Labels</Title>
                            //  <SRS>EPSG:4326</SRS>
                            //  <LatLonBoundingBox minx="-180" miny="-90" maxx="180" maxy="90" />
                            //</Layer>

                            if (element.Descendants(XName.Get("Name", xnamespace)).Count() > 0)
                            {
                                GISLayerInfo layer = new OGCLayer(element.Descendants(XName.Get("Title", xnamespace)).First().Value, element.Descendants(XName.Get("Name", xnamespace)).First().Value);

                                if (element.Attribute(XName.Get("queryable", xnamespace)) != null)
                                {
                                    layer.IsQueryable = element.Attribute(XName.Get("queryable", xnamespace)).Value == "1";
                                }

                                layer.BaseExtent = GetExtentFromElement(element, xnamespace);

                                newSvc.BaseLayers.Add(layer);
                            }
                            else if (element.Descendants(XName.Get("Layer", xnamespace)).Count() > 0)
                            {
                                IEnumerable<XElement> childElements = element.Descendants(XName.Get("Layer", xnamespace));

                                foreach (XElement childElement in childElements)
                                {
                                    GISLayerInfo layer = new OGCLayer(element.Descendants(XName.Get("Title", xnamespace)).First().Value, childElement.Descendants(XName.Get("Name", xnamespace)).First().Value);

                                    if (childElement.Attribute(XName.Get("queryable", xnamespace)) != null)
                                    {
                                        layer.IsQueryable = childElement.Attribute(XName.Get("queryable", xnamespace)).Value == "1";
                                    }

                                    layer.BaseExtent = GetExtentFromElement(childElement, xnamespace);

                                    newSvc.BaseLayers.Add(layer);
                                }
                            }
                        }
                    }
                    else if (selement.Descendants().Count() > 0)
                    {
                        GISLayerInfo layer = new OGCLayer(selement.Descendants(XName.Get("Title", xnamespace)).First().Value, selement.Descendants(XName.Get("Name", xnamespace)).First().Value);

                        if (selement.Attribute(XName.Get("queryable", xnamespace)) != null)
                        {
                            layer.IsQueryable = selement.Attribute(XName.Get("queryable", xnamespace)).Value == "1";
                        }

                        layer.BaseExtent = GetExtentFromElement(selement, xnamespace);

                        newSvc.BaseLayers.Add(layer);
                    }
                    else
                    {
                        GISLayerInfo layer = new OGCLayer(newSvc._serviceName, newSvc._serviceId == null ? newSvc._serviceName : newSvc._serviceId);

                        layer.BaseExtent = GetExtentFromElement(selement, xnamespace);

                        newSvc.BaseLayers.Add(layer);
                    }

                    if (newSvc.Id == null) newSvc._serviceId = newSvc.Name;
                    _Server._services.Add(newSvc);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
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

        public override void GetQueryLayers(GISService service, ref List<GISLayerInfo> layers)
        {
            foreach (GISLayerInfo info in (service as OGCService).ActiveLayers)
            {
                if (!layers.Contains(info) && info.IsQueryable) layers.Add(info);
            }

            foreach (GISLayerInfo info in (service as OGCService).BaseLayers)
            {
                if (!layers.Contains(info) && info.IsQueryable) layers.Add(info);
            }
        }

        public override async Task<bool> GetServiceDetails(GISService activeService)
        {
            return ((activeService == null && GetClientServices().Result) || activeService.HasLayers);
        }

        public override bool GetMap(GISService activeService)
        {
            GISEnvelope imageEnvelope = SelectEnvelope(activeService);

            TransformEnvelope(ref imageEnvelope);

            //if (activeService.ActiveLayers.Count == 0 && activeService.BaseLayers.Count > 0) activeService.ActiveLayers.Add(activeService.BaseLayers[0]);
            return GetMap(activeService, imageEnvelope);
        }

        public override bool GetMap(GISService activeService, GISEnvelope imageEnvelope)
        {
            OGCImageResponse response = new OGCImageResponse();

            if (activeService.HasLayers || GetServiceDetails(activeService).Result)
            {
                if (activeService._activeLayers.Count == 0)
                {
                    for (int i = 0; i < activeService._baseLayers.Count; i++)
                    {
                        if (activeService._baseLayers[i].IsVisible && activeService._baseLayers[i]._baseExtent != null)
                        {
                            activeService._activeLayers.Add(activeService._baseLayers[i]);
                        }
                    }
                }

                return GetMap(activeService, imageEnvelope, activeService._activeLayers.ToList());
            }
            else
            {
                Server.RaiseErrorResponse(GISResponse.ProcessErrorResponse("Services could not be located for this server at this time.", "", ""));
                return false;
            }
        }

        public override bool GetMap(GISService activeService, GISEnvelope mapEnvelope, List<GISLayerInfo> mapLayers)
        {
            if (mapEnvelope == null) SelectEnvelope(activeService);

            OGCEnvelope ogcEnvelope = new OGCEnvelope(mapEnvelope);

            if (activeService.HasLayers || GetServiceDetails(activeService).Result)
            {
                if (mapLayers.Count == 0)
                {
                    mapLayers.Add(activeService.BaseLayers[0]);
                }

                //string currentlayerId = mapLayers[0]._id;
                //string styleId = (mapLayers[0] as OGCLayer).Style;
                GISEnvelope maxEnvelope = activeService._baseExtent;
                if (mapLayers.Count > 0) maxEnvelope = mapLayers[0]._baseExtent;

                //for (int i = 1; i < mapLayers.Count; i++)
                //{
                //    currentlayerId = string.Format("{0},{1}", currentlayerId, mapLayers[i]._id);
                //    if (mapLayers[i]._baseExtent != null) maxEnvelope = mapLayers[i]._baseExtent;
                //}

                //for (int i = 1; i < mapLayers.Count; i++)
                //{
                //    styleId = string.Format("{0},{1}", styleId, (mapLayers[i] as OGCLayer).Style);
                //}

                string mapUrl = GetUrl(OGC_OPERATION.GetMap, OGC_SERVICE_TYPE.WMS, activeService as OGCService, mapLayers.Cast<OGCLayer>(), ogcEnvelope, _imageWidth, _imageHeight);
                return GetMap(activeService, mapLayers, mapEnvelope, SetZoomLevel(mapEnvelope, maxEnvelope), mapUrl);
            }
            else
            {
                Server.RaiseErrorResponse(new GISResponse() { Envelope = mapEnvelope, ErrorMessage = "Service details could not be found for this service.", Layers = mapLayers, HasError = true });
                return false;
            }
        }

        public override bool GetMap(GISService activeService, double centerX, double centerY, double zoomLevel)
        {
            //double logZoom = Math.Log10(zoomLevel);

            //if (zoomLevel > 0 && logZoom < .1)
            //{
            //    zoomLevel = Math.Pow(10, .5 * 1.1);
            //}
            //else if (zoomLevel > 0)
            //{
            //    //GISEnvelope mapEnvelope = GetTileEnvelope(centerX, centerY, (int)zoomLevel);
            //    zoomLevel = Math.Pow(10, logZoom * 1.1);
            //}

            GISEnvelope mapEnvelope = BuildEnvelope(centerX, centerY, SelectEnvelope(activeService), zoomLevel);
            TransformEnvelope(ref mapEnvelope);

            if (activeService._activeLayers.Count == 0) activeService.ActiveLayers.AddRange(activeService.BaseLayers);

            //string currentlayerId = activeService.ActiveLayers.Count > 0 ? activeService.ActiveLayers[0]._id : string.Empty;
            //string styleId = activeService.ActiveLayers.Count > 0 ? (activeService.ActiveLayers[0] as OGCLayer).Style : string.Empty;

            //for (int i = 1; i < activeService.ActiveLayers.Count; i++)
            //{
            //    currentlayerId = string.Format("{0},{1}", currentlayerId, activeService.ActiveLayers[i]._id);
            //}

            //for (int i = 1; i < activeService.ActiveLayers.Count; i++)
            //{
            //    if (!string.IsNullOrEmpty((activeService.ActiveLayers[i] as OGCLayer).Style))
            //    {
            //        styleId = string.Format("{0},{1}", styleId, (activeService.ActiveLayers[i] as OGCLayer).Style);
            //    }
            //}

            string mapUrl = GetUrl(OGC_OPERATION.GetMap, OGC_SERVICE_TYPE.WMS, activeService as OGCService, activeService.ActiveLayers.Cast<OGCLayer>(), new OGCEnvelope(mapEnvelope), _imageWidth, _imageHeight);
            return GetMap(activeService, activeService.ActiveLayers, mapEnvelope, zoomLevel, mapUrl);
        }

        public override bool GetMap(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, string requestString)
        {
            OGCImageResponse response = new OGCImageResponse();

            try
            {
                response.Envelope = mapEnvelope;
                response.Layers = mapLayers;
                OGCEnvelope ogcEnvelope = new OGCEnvelope(mapEnvelope);

                //string currentlayerId = mapLayers[0]._id;
                //string styleId = (mapLayers[0] as OGCLayer).Style;

                //for (int i = 1; i < mapLayers.Count; i++)
                //{
                //    currentlayerId = string.Format("{0},{1}", currentlayerId, mapLayers[i]._id);
                //}

                //for (int i = 1; i < mapLayers.Count; i++)
                //{
                //    if (!string.IsNullOrEmpty((mapLayers[i] as OGCLayer).Style))
                //    {
                //        styleId = string.Format("{0},{1}", styleId, (mapLayers[i] as OGCLayer).Style);
                //    }
                //}

                response.LastRequest = requestString;

                response.LastResponse = "Complete";
                response._mapImageUrl = requestString;
                response.ZoomLevel = zoomLevel;
                Server.RaiseMapResponse(response);
                return true;
            }
            catch (System.Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { LastResponse = ex.Message, ErrorMessage = ex.Message, HasError = true, LastRequest = requestString, _envelope = response.Envelope, _layers = mapLayers });
            }

            return false;
        }

        public override void ExecuteSearch(string searchTerm, SEARCH_TYPE searchType, GISEnvelope searchArea, GISLayerInfo featureLayer)
        {
            if (searchType == SEARCH_TYPE.Geocode)
            {
                // http://{0}/{1}?SERVICE={2}&VERSION={3}&REQUEST=GetFeatureInfo&LAYERS={4}&QUERY_LAYERS={4}&STYLES=&BBOX={5}&FEATURE_COUNT=10&HEIGHT={6}&WIDTH={7}&FORMAT=image%2Fpng&INFO_FORMAT=text%2Fxml&SRS={8}&X={9}&Y={10}
                OGCEnvelope envelope = new OGCEnvelope(searchArea);

                string requestUrl = string.Format(FEATURE_INFO_URL, Server.Host, Server.ServletPath, OGC_SERVICE_TYPE.WMS, Server.Version, (featureLayer as OGCLayer).Id, (envelope as OGCEnvelope).ToBBoxString(), _imageHeight, _imageWidth, searchArea.CoordinateSystem, _imageWidth / 2, _imageHeight / 2);
                GISFeatureResponse response = new GISFeatureResponse();
                response._envelope = searchArea;
                response._layers = new List<GISLayerInfo>() { featureLayer };
                response.HasError = false;
                response.LastRequest = requestUrl;

                webClient.GetRequest(requestUrl, ProcessQueryResponse, response);
            }
            else
            {
                List<GISLayerInfo> layers = new List<GISLayerInfo>();
                GetQueryLayers(Server.ActiveService, ref layers);

                string queryLayers = string.Empty;

                foreach (GISLayerInfo layer in layers)
                {
                    queryLayers += layer.Id;
                }

                string requestUrl = GetUrl(OGC_OPERATION.GetFeatureInfo, OGC_SERVICE_TYPE.WMS, Server.ActiveService as OGCService, queryLayers.Cast<OGCLayer>(), new OGCEnvelope(searchArea), _imageWidth, _imageHeight);
                //http://<hostname>/<deploy_name>/com.esri.wms.Esrimap?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetFeatureInfo&SRS=EPSG:4326&BBOX=-117,38,-90,49&WIDTH=600&HEIGHT=400&QUERY_LAYERS=States&X=200&Y=150&

                GISFeatureResponse response = new GISFeatureResponse() { SearchTerm = searchTerm };
                response._envelope = new GISEnvelope();
                response._layers = new List<GISLayerInfo>() { new OGCLayer("Search", "Search") };
                response.HasError = false;
                response.LastRequest = requestUrl;

                webClient.GetRequest(requestUrl, ProcessSearchResponse, response);
            }
        }

        SimpleWebClient webClient = new SimpleWebClient();

        public override string GetErrorMessage(string responseXml)
        {
            return responseXml;
        }

        public override GISService CreateService(string serviceName)
        {
            return new OGCService(serviceName);
        }

        public override GISService AddService(string serviceName, string serviceId, ServiceType serviceType)
        {
            OGCService svc = new OGCService(serviceName) { _type = serviceType, _serviceId = serviceId };
            Server._services.Add(svc);
            return svc;
        }

        public override GISLayerInfo CreateLayer(string layerName, string layerId, bool isVisible)
        {
            return new OGCLayer() { _name = layerName, _id = layerId, _isVisible = isVisible };
        }

        public override IGISLegend GetLegend(GISLayerInfo selectedLayer)
        {
            string legendUrl = GetUrl(OGC_OPERATION.GetLegendGraphic, OGC_SERVICE_TYPE.WMS, Server.ActiveService as OGCService, new List<OGCLayer>(){selectedLayer as OGCLayer}, new OGCEnvelope(), _imageWidth, _imageHeight);

            return new GISLegend() { LegendUrl = legendUrl };
        }

        #endregion

        #region Functions

        private string GetLayerList(List<GISLayerInfo> mapLayers)
        {
            bool first = true;
            StringBuilder sb = new StringBuilder();

            foreach (GISLayerInfo info in mapLayers)
            {
                if (first)
                {
                    if (info.Id != null)
                        sb.Append(info.Id);
                    else sb.Append(info.Name);
                    first = false;
                }
                else
                {

                    if (info.Id != null)
                        sb.AppendFormat(",{0}", info.Id);
                    else sb.AppendFormat(",{0}", info.Name);
                }
            }

            return sb.ToString();
        }

        private GISResponse ProcessErrorReturn(string p)
        {
            return GISResponse.ProcessErrorResponse(p, "req", "resp");
        }

        private GISResponse ProcessQueryReturn(XmlReader returnDocument, string layerName)
        {
            //<wfs:FeatureCollection xmlns="http://www.cubewerx.com/cw" xmlns:cw="http://www.cubewerx.com/cw" xmlns:wfs="http://www.opengis.net/wfs" xmlns:gml="http://www.opengis.net/gml" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.cubewerx.com/cw http://demo.cubewerx.com/demo/cubeserv/cubeserv.cgi?DATASTORE=GSC&amp;service=WFS&amp;version=1.1.0&amp;request=DescribeFeatureType&amp;outputFormat=GML3&amp;typeName=FAULTS_CA  http://www.opengis.net/wfs http://schemas.cubewerx.com/schemas/wfs/1.1.0/wfs.xsd http://www.opengis.net/gml http://schemas.cubewerx.com/schemas/gml/3.1.1/base/feature.xsd">
            //  <gml:boundedBy>
            //    <gml:Envelope srsName="urn:ogc:def:crs:EPSG::4326">
            //      <gml:lowerCorner>48.05543511 -141.747323</gml:lowerCorner>
            //      <gml:upperCorner>67.67964198 -88.73328852</gml:upperCorner>
            //    </gml:Envelope>
            //  </gml:boundedBy>
            //  <gml:featureMember>
            //    <FAULTS_CA gml:id="CWFID.FAULTS_CA.0.744">
            //      <GEOMETRY>
            //        <gml:LineString srsName="EPSG:4326">
            //          <gml:posList>-132.901294 55.33849098 -132.8861369 55.24352568</gml:posList>
            //        </gml:LineString>
            //      </GEOMETRY>
            //      <FNODEX>30093</FNODEX>
            //      <TNODEX>38844</TNODEX>
            //      <LPOLYX>0</LPOLYX>
            //      <RPOLYX>0</RPOLYX>
            //      <LENGTH>10407.923</LENGTH>
            //      <FLTAPPX>0</FLTAPPX>
            //      <FLTAPP_ID>0</FLTAPP_ID>
            //      <SYMBOL>30</SYMBOL>
            //      <COMP>J. O. Wheeler</COMP>
            //      <DXF_LAYER>unclass_flt</DXF_LAYER>
            //      <COUNTRY>USA*</COUNTRY>
            //      <ENGDESC>unclassified fault</ENGDESC>
            //      <PAYS>Etats-Unis</PAYS>
            //      <FREDESC>faille de nature non difinie</FREDESC>
            //    </FAULTS_CA>
            //  </gml:featureMember>

            OGCFeatureResponse response = new OGCFeatureResponse();

            bool inElement = false;
            OGCFeature feature = new OGCFeature();

            try
            {
                var xDocument = XDocument.Load(returnDocument);

                var q = from c in xDocument.Descendants()
                        select c.Element("featuremember");


                foreach (XNode node1 in xDocument.DescendantNodes())
                {
                    if (node1.NodeType == XmlNodeType.Element)
                    {
                        XElement item = node1 as XElement;

                        foreach (XNode node in item.DescendantNodes())
                        {
                            //if (xReader.MoveToNextAttribute())
                            //{
                            //    feature._Fields.Add(new GISField("ID", xReader.Value));
                            //}

                            if (node.NodeType == XmlNodeType.Element)
                            {
                                XElement element = node as XElement;

                                if (element.Name.LocalName == layerName)
                                {
                                    if (element.HasAttributes)
                                    {
                                        feature._Fields.Add(new GISField("id", element.Attribute(XName.Get("id", "gml")).Value));
                                    }

                                    if (inElement)
                                    {
                                        response._features.Add(feature);
                                        feature = new OGCFeature();
                                    }
                                    else inElement = true;
                                }
                                else if (element.Name == "Envelope")
                                {
                                    ReadEnvelope(element, feature);
                                }
                                else if (element.Name == "GEOMETRY")
                                {
                                    //ReadGeometry(element, feature);
                                }
                                else feature._Fields.Add(new GISField(element.Name.LocalName, element.Value));
                            }
                            else if (node.NodeType == XmlNodeType.EndElement)
                            {
                                XElement endelement = node as XElement;
                                if (endelement.Name.LocalName == layerName)
                                {
                                    response._features.Add(feature);
                                    feature = new OGCFeature();
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                string x = "read error";
            }

            return response;
        }

        private void ReadGeometry(XElement xReader, OGCFeature feature)
        {
            feature._Envelope = new OGCEnvelope();

            //      <GEOMETRY>
            //        <gml:LineString srsName="EPSG:4326">
            //          <gml:posList>-132.901294 55.33849098 -132.8861369 55.24352568</gml:posList>
            //        </gml:LineString>
            //      </GEOMETRY>

            //while (xReader.Read())
            //{
            //    if (xReader.NodeType == XmlNodeType.Element)
            //    {
            //    }
            //    else if (xReader.NodeType == XmlNodeType.EndElement)
            //    {
            //        if (xReader.Name == "GEOMETRY") break;
            //    }
            //}
        }

        private void ReadEnvelope(XElement xReader, OGCFeature feature)
        {
            feature._Envelope = new OGCEnvelope();

            //    <gml:Envelope srsName="urn:ogc:def:crs:EPSG::4326">
            //      <gml:lowerCorner>48.05543511 -141.747323</gml:lowerCorner>
            //      <gml:upperCorner>67.67964198 -88.73328852</gml:upperCorner>
            //    </gml:Envelope>

            string[] coords;

            foreach (XElement element in xReader.Descendants())
            {
                if (element.NodeType == XmlNodeType.Element)
                {
                    if (element.Name.LocalName == "lowerCorner")
                    {
                        coords = element.Value.Split(' ');
                        feature._Envelope.minX = double.Parse(coords[0]);
                        feature._Envelope.minY = double.Parse(coords[1]);
                    }
                    else if (element.Name.LocalName == "upperCorner")
                    {
                        coords = element.Value.Split(' ');
                        feature._Envelope.maxX = double.Parse(coords[0]);
                        feature._Envelope.maxY = double.Parse(coords[1]);
                    }
                }
            }
        }

        void ProcessSearchResponse(object sender, WebEventArgs e)
        {
            GISFeatureResponse response = e.UserState as GISFeatureResponse;

            try
            {
                XDocument xDoc = XDocument.Parse(e.ResponseString);

                var features = from x in xDoc.Descendants("FeatureInfoResponse") select x;

                response.LastResponse = e.ResponseString;

                //geocodeResponse.ResponseSummary.StatusCode
                foreach (XElement element in features.Descendants())
                {
                    response.Features.Add(OGCFeature.ProcessResult(element));
                }

                Server.RaiseSearchResponse(response);
            }
            catch (Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { _envelope = response.Envelope, _layers = new List<GISLayerInfo>() { new OGCLayer("Geocode", "Geocode") }, ErrorMessage = ex.Message });
            }
        }

        public override bool Identify(int xpoint, int ypoint, double latitude, double longitude, GISEnvelope envelope, GISLayerInfo featureLayer)
        {
            // http://maps.google.com/maps/api/geocode/xml?latlng=40.714224,-73.961452&sensor=true

            try
            {
                if (featureLayer == null) return false;

                envelope = new OGCEnvelope(envelope);
                //string layerString = featureLayer.Id;

                string requestUrl = GetUrl(OGC_OPERATION.GetFeatureInfo, OGC_SERVICE_TYPE.WMS, Server.ActiveService as OGCService, new List<OGCLayer>() { featureLayer as OGCLayer }, envelope as OGCEnvelope, _imageWidth, _imageHeight, xpoint, ypoint);
                GISFeatureResponse response = new GISFeatureResponse();
                response._envelope = BuildEnvelope(longitude, latitude, envelope, Server.MaxZoom * .9);
                response._layers = new List<GISLayerInfo>() { featureLayer };
                response.HasError = false;
                response.LastRequest = requestUrl;

                webClient.GetRequest(requestUrl, ProcessQueryResponse, response);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string ConvertLayersToString(List<GISLayerInfo> layers)
        {
            if (layers.Count > 0)
            {
                StringBuilder s = new StringBuilder(layers[0].Id);
                for (int i = 1; i < layers.Count; i++)
                {
                    s.AppendFormat(",{0}", layers[i].Id);
                }

                return s.ToString();
            }
            else return string.Empty;
        }

        void ProcessQueryResponse(object sender, WebEventArgs e)
        {
            GISFeatureResponse response = e.UserState as GISFeatureResponse;

            try
            {
                XDocument xDoc = XDocument.Parse(e.ResponseString);

                var features = from x in xDoc.Descendants("FeatureInfoResponse") select x;

                response.LastResponse = e.ResponseString;

                //geocodeResponse.ResponseSummary.StatusCode
                foreach (XElement element in features.Descendants())
                {
                    response.Features.Add(OGCFeature.ProcessResult(element));
                }

                Server.RaiseIdentifyResponse(response); //.RaiseSearchResponse(response);
            }
            catch (Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { _envelope = response.Envelope, _layers = new List<GISLayerInfo>() { new OGCLayer("Geocode", "Geocode") }, ErrorMessage = ex.Message });
            }
        }

        #endregion
    }
}
