using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace AtlasOf.GIS
{
    public enum SEARCH_TYPE
    {
        Geocode,
        Query,
        Local,
        Image,
        Web,
        Identify
    }

    public class GISServer
    {
        private bool _supportSearch = false;
        private bool _useForm = false;

        public bool UseForm
        {
            get { return _useForm; }
            set { _useForm = value; }
        }
        internal string _display;
        internal string _host;
        private string _credentials;
        private string _version = "";
        internal string _servlet = "";
        internal DateTime _lastUpdated = DateTime.MinValue;

        private GISService _activeService;
        private GISLayerInfo _featureLayer;

        public GISController Controller
        {
            get
            {
                if (_Controller == null) _Controller = GISControllerFactory.Create(this);
                return _Controller;
            }
        }

        public GISLayerInfo FeatureLayer
        {
            get { return _featureLayer; }
            set { _featureLayer = value; }
        }
        internal List<GISLayerInfo> _queries = new List<GISLayerInfo>();
        internal List<GISService> _services = new List<GISService>();
        internal GISController _Controller;

        public bool SupportSearch
        {
            get { return _supportSearch; }
        }

        public string Map { get; set; }

        public ZOOM_TYPE ZoomType
        {
            get { return Controller.ZoomType; }
        }

        public int MaxZoom
        {
            get { return Controller.MaxZoom; }
        }

        public int MinZoom
        {
            get { return Controller.MinZoom; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public string Credentials
        {
            get { return _credentials; }
            set { _credentials = value; }
        }

        public GISService ActiveService
        {
            get
            {
                if (_activeService == null && _services.Count > 0)
                {
                    _activeService = _services[0];
                }

                return _activeService;
            }
        }

        public List<GISLayerInfo> QueryLayers
        {
            get { return _queries; }
            set { _queries = value; }
        }

        public string Display
        {
            get { return _display; }
            set { _display = value; }
        }

        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public string ServletPath
        {
            get { return _servlet; }
            set { _servlet = value; }
        }

        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { _lastUpdated = value; }
        }

        public bool HasServices
        {
            get { return _services.Count > 0; }
        }

        public List<GISService> Services
        {
            get
            {
                return _services;
            }
            set { _services = value; }
        }

        public string Type { get; set; }

        public double GetZoomLevel(GISEnvelope currentExtent)
        {
            return Controller.SetZoomLevel(currentExtent, Controller.SelectEnvelope(ActiveService));
        }

        public bool ZoomAbsolute(double zoomLevel, GISEnvelope lastEnvelope)
        {
            return Controller.GetMap(ActiveService, lastEnvelope.CenterX, lastEnvelope.CenterY, zoomLevel);
        }

        public static GISServer Create(System.Xml.Linq.XElement sERVERSERVER)
        {
            GISServer srv = new GISServer();

            foreach (XAttribute attr in sERVERSERVER.Attributes())
            {
                switch (attr.Name.LocalName)
                {
                    case "display":
                        srv._display = attr.Value;
                        break;
                    case "host":
                        srv._host = attr.Value;
                        break;
                    case "servlet":
                        srv._servlet = attr.Value;
                        break;
                    case "location":
                        srv._servlet = attr.Value;
                        break;
                    case "version":
                        srv._version = attr.Value;
                        break;
                    case "type":
                        srv.Type = attr.Value;
                        break;
                    case "credentials":
                        srv.Credentials = attr.Value;
                        break;
                }
            }

            srv._Controller = GISControllerFactory.Create(srv);
            return srv;
        }

        public static GISServer Create(string type, string display, string host, bool supportSearch, string servlet, string version)
        {
            GISServer srv = new GISServer();
            srv._display = display;
            srv._host = host;
            srv._servlet = servlet;
            srv._version = version;
            srv.Type = type;
            srv._supportSearch = supportSearch;
            srv._Controller = GISControllerFactory.Create(srv);
            return srv;
        }

        //public static GISServer Create(AtlasOf.Configuration.SERVERSERVER sERVERSERVER)
        //{
        //    GISServer srv = new GISServer();
        //    srv._display = sERVERSERVER.display;
        //    srv._host = sERVERSERVER.host;
        //    srv._location = sERVERSERVER.location == null ? "servlet" : sERVERSERVER.location;
        //    srv._servlet = sERVERSERVER.servlet == null ? "com.esri.esrimap.Esrimap" : sERVERSERVER.servlet;
        //    srv._version = sERVERSERVER.version == null ? "1.1.1" : sERVERSERVER.version;
        //    srv.Type = sERVERSERVER.type == null ? "ESRI" : sERVERSERVER.type;
        //    srv.Controller = GISControllerFactory.Create(srv);
        //    return srv;
        //}

        //public static SERVERSERVER Persist(GISServer srv)
        //{
        //    SERVERSERVER sERVERSERVER = new SERVERSERVER();
        //    sERVERSERVER.display = srv._display;
        //    sERVERSERVER.host = srv._host;
        //    sERVERSERVER.location = srv._location;
        //    sERVERSERVER.servlet = srv._servlet;
        //    sERVERSERVER.type = srv.Type;
        //    return sERVERSERVER;
        //}

        public async Task GetServices(bool isRefresh)
        {
            if (isRefresh || _services.Count == 0)
            {
                Controller.RequestProcessing = !await Controller.GetClientServices();
            }
            else Controller.RequestProcessing = false;
        }

        public IGISLegend GetLegend(GISLayerInfo selectedLayer)
        {
            if (selectedLayer == null)
            {
            return ActiveService.ActiveLayers.Any() ? Controller.GetLegend(this.ActiveService.ActiveLayers.First()) : Controller.GetLegend(ActiveService.BaseLayers.First());
            }
            else return Controller.GetLegend(selectedLayer);
        }

        public bool GetMap(GISEnvelope envelope)
        {
            if (Controller.RequestProcessing)
                return false;
            else Controller.RequestProcessing = true;

            if (ActiveService != null)
            {
                return (ZoomType == ZOOM_TYPE.TILED) ? Controller.GetMap(ActiveService, envelope.CenterX, envelope.CenterY, Controller.SetZoomLevel(envelope, ActiveService._baseExtent))
                    : Controller.GetMap(ActiveService, envelope);
            }

            return false;
        }

        public bool GetMap(GISEnvelope envelope, double zoomLevel)
        {
            if (Controller.RequestProcessing)
                return false;
            else Controller.RequestProcessing = true;

            if (ActiveService != null)
            {
                if (zoomLevel > MaxZoom) zoomLevel = Math.Floor(MaxZoom - MaxZoom * .1);
                return (ZoomType == ZOOM_TYPE.TILED) ? Controller.GetMap(ActiveService, envelope.CenterX, envelope.CenterY, zoomLevel)
                    : Controller.GetMap(ActiveService, envelope);
            }

            return false;
        }

        public bool GetMap(GISService service, GISEnvelope mapEnvelope, List<GISLayerInfo> mapLayers)
        {
            if (Controller.RequestProcessing)
                return false;
            else Controller.RequestProcessing = true;

            return Controller.GetMap(service, mapEnvelope, mapLayers);
        }

        public bool GetMap(double centerX, double centerY, double zoomLevel)
        {
            if (Controller.RequestProcessing)
                return false;
            else Controller.RequestProcessing = true;

            if (ActiveService != null)
            {
                if (zoomLevel > MaxZoom) zoomLevel = Math.Floor(MaxZoom - MaxZoom * .1);
                if (zoomLevel < MinZoom) zoomLevel = Math.Floor(MinZoom + MaxZoom * .1);
                return Controller.GetMap(ActiveService, centerX, centerY, zoomLevel);
            }

            return false;
        }

        public bool GetBaseMap()
        {
            if (Controller.RequestProcessing)
                return false;
            else Controller.RequestProcessing = true;

            if (ActiveService != null)
            {
                return Controller.GetMap(ActiveService);
            }
            else return false;
        }

        public string GetMapImage(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, int width, int height)
        {
            return Controller.GetMapImage(activeService, mapLayers, mapEnvelope, zoomLevel, width, height);
        }

        public bool Identify(int xpoint, int ypoint, double latitude, double longitude, GISEnvelope envelope, GISLayerInfo featureLayer)
        {
            if (Controller.RequestProcessing)
                return false;
            else Controller.RequestProcessing = true;

            if (featureLayer == null)
            {
                if (FeatureLayer == null)
                {
                    List<GISLayerInfo> layers = GetQueryLayers();

                    if (layers.Count() > 0)
                    {
                        featureLayer = layers.First();
                    }
                }
                else featureLayer = FeatureLayer;
            }

            return Controller.Identify(xpoint, ypoint, latitude, longitude, envelope, featureLayer);
        }

        public async Task<bool> GetServiceDetails(GISService service)
        {
            //if (service.HasLayers) return true;
            //if (Controller.RequestProcessing)
            //    return false;
            //else Controller.RequestProcessing = true;

            return await Controller.GetServiceDetails(service);
        }

        //public void SetActiveLayer(GISService activeService, GISLayerInfo activeLayer)
        //{
        //    _activeService = activeService;
        //    _activeService.ActiveLayers.Clear();
        //    if (activeLayer != null)
        //    {
        //        activeLayer.IsVisible = true;
        //        _activeService.ActiveLayers.Add(activeLayer);
        //    }
        //}

        public void SetMapDimensions(int imageWidth, int imageHeight)
        {
            Controller.SetMapDimensions(imageHeight, imageWidth);
        }

        //public bool ExecuteRequest(string requestXml, List<GISLayerInfo> layers, GISEnvelope envelope)
        //{
        //    return Controller.GetMap(ActiveService, layers, envelope, Controller.SetZoomLevel(envelope, ActiveService._baseExtent), requestXml);
        //}

        public bool SetActiveService(string service)
        {
            if (_services != null)
            {
                var svc = from x in _services where x.Name == service select x;

                if (svc.Count() > 0)
                {
                    _activeService = svc.First();
                    return true;
                }
                else
                {
                    var svc2 = from x in _services where x.Id == service select x;

                    if (svc2.Count() > 0)
                    {
                        _activeService = svc2.First();
                        return true;
                    }
                    else
                    {
                        _activeService = Controller.CreateService(service);
                        _services.Add(_activeService);
                        return true;
                    }
                }
            }
            else
            {
                _services = new List<GISService>();
                _activeService = Controller.CreateService(service);
                _services.Add(_activeService);
                return true;
            }

            //return false;
        }

        public List<GISLayerInfo> GetImageLayers()
        {
            List<GISLayerInfo> layers = new List<GISLayerInfo>();

            Controller.GetImageLayers(ActiveService, layers);

            return layers;
        }

        public List<GISLayerInfo> GetQueryLayers()
        {
            List<GISLayerInfo> layers = new List<GISLayerInfo>();

            layers.AddRange(_queries);
            Controller.GetQueryLayers(ActiveService, ref layers);

            return layers;
        }

        //internal static GISServer Create(GDIS.GIS.NationalMap.Service service)
        //{
        //    string serviceName = null;
        //    GISServer newServer = new GISServer();

        //    if (InspectEsriUrl(service.GetCapabilitiesURL, ref newServer, ref serviceName))
        //    {
        //        if (!string.IsNullOrEmpty(service.Title))
        //            newServer.Display = service.Title;
        //        else if (!string.IsNullOrEmpty(service.Abstract))
        //            newServer.Display = service.Abstract;

        //        if (serviceName != null)
        //        {
        //            newServer.AddService(serviceName, service.Layer);
        //        }

        //        return newServer;
        //    }
        //    else return null;
        //}

        //private static bool InspectEsriUrl(string url, ref GISServer server, ref string serviceName)
        //{
        //    try
        //    {
        //        Uri resultUri;
        //        SERVERSERVER svr = new SERVERSERVER();

        //        if (Uri.TryCreate(url, UriKind.Absolute, out resultUri))
        //        {
        //            if (!string.IsNullOrEmpty(resultUri.Query))
        //            {
        //                string[] queryParams = resultUri.Query.Substring(1).Split('=', '&');

        //                for (int i = 0; i < queryParams.Length; i++)
        //                {
        //                    switch (queryParams[i].ToLower())
        //                    {
        //                        case "map":
        //                            //svr.map = queryParams[++i];
        //                            break;
        //                        case "dataset":
        //                            //svr.dataset = queryParams[++i];
        //                            break;
        //                        case "servicename":
        //                            serviceName = queryParams[++i];
        //                            break;
        //                    }
        //                }
        //            }

        //            if (string.IsNullOrEmpty(serviceName))
        //            {
        //                serviceName = resultUri.DnsSafeHost;
        //            }

        //            if (!string.IsNullOrEmpty(System.IO.Path.GetFileName(url)) && !resultUri.LocalPath.Contains("Esrimap"))
        //            {
        //                svr.display = resultUri.Host;
        //                svr.host = resultUri.Host;
        //                svr.location = resultUri.LocalPath.Substring(1);

        //                svr.type = "OGC";
        //                server = GISServer.Create(svr);
        //            }
        //            else
        //            {
        //                svr.display = resultUri.Host;
        //                svr.host = resultUri.Host;
        //                svr.type = "ESRI";
        //                server = GISServer.Create(svr);
        //            }
        //        }
        //        else // assume just ESRI host entered
        //        {
        //            svr.display = url;
        //            svr.host = url;
        //            svr.type = "ESRI";
        //            server = GISServer.Create(svr);
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        public GISService AddService(string serviceName, string serviceId, ServiceType serviceType = ServiceType.ImageServer)
        {
            return Controller.AddService(serviceName, serviceId, serviceType);
        }

        //internal void AddService(string serviceName, GDIS.GIS.NationalMap.Layer[] lyrs)
        //{
        //    GISService newService = Controller.CreateService(serviceName);

        //    foreach (GDIS.GIS.NationalMap.Layer lyr in lyrs)
        //    {
        //        newService._baseExtent = GISEnvelope.Parse(lyr.LatLon, lyr.SRS[0]);
        //        newService._description = lyr.Description;
        //        newService._baseLayers.Add(GetLayerInfo(lyr));
        //    }

        //    _services.Add(newService);
        //    _activeService = newService;
        //}

        //internal GISLayerInfo GetLayerInfo(GDIS.GIS.NationalMap.Layer lyr)
        //{
        //    GISLayerInfo info = Controller.CreateLayer(lyr.Name, lyr.Name, true);

        //    if (!string.IsNullOrEmpty(lyr.LatLon))
        //    {
        //        info._baseExtent = GISEnvelope.Parse(lyr.LatLon, "4326");
        //    }
        //    else if (!string.IsNullOrEmpty(lyr.BoundingBox))
        //    {
        //        info._baseExtent = GISEnvelope.Parse(lyr.LatLon, lyr.SRS[0]);
        //    }

        //    info._isVisible = lyr.Status == "PUBLIC";
        //    info._maxscale = lyr.MaxScale;
        //    info._minscale = lyr.MinScale;
        //    info._type = lyr.Type;

        //    if (lyr.Layer1 != null)
        //    {
        //        foreach (GDIS.GIS.NationalMap.Layer sublyr in lyr.Layer1)
        //        {
        //            info._childLayers.Add(GetLayerInfo(sublyr));
        //        }
        //    }

        //    return info;
        //}

        public void SetActiveLayer(string serviceName, string layerName)
        {
            if (_services.Count > 0)
            {
                GISLayerInfo layer;

                if (SetActiveService(serviceName))
                {
                    if (ActiveService._baseLayers.Count > 0) // || GetServiceDetails(ActiveService))
                    {
                        if (ActiveService.FindLayer(layerName, out layer))
                        {
                            layer._isVisible = true;
                            _activeService.ActiveLayers.Clear();
                            _activeService.ActiveLayers.Add(layer);
                        }
                    }
                }
            }
        }

        public GISService FindService(string serviceName)
        {
            foreach (GISService svc in Services)
            {
                if (svc.Name == serviceName) return svc;
            }

            return null;
        }

        public void ExecuteSearch(string searchTerm, SEARCH_TYPE searchType, GISEnvelope searchArea)
        {
            if (Controller.RequestProcessing)
                return;
            else Controller.RequestProcessing = true;

            if (FeatureLayer == null)
            {
                List<GISLayerInfo> layers = GetQueryLayers();

                if (layers.Count() > 0)
                {
                    _featureLayer = layers.First();
                }
            }

            Controller.ExecuteSearch(searchTerm, searchType, searchArea, FeatureLayer);
        }

        public void SetActiveLayers(string layerString)
        {
            if (!string.IsNullOrEmpty(layerString))
            {
                string[] layers = layerString.Split();
                List<GISLayerInfo> retlayers = new List<GISLayerInfo>();

                foreach (string lyrid in layers)
                {
                    var layerTemp = from x in ActiveService.BaseLayers where x.Id == lyrid select x;

                    if (layerTemp.Any())
                    {
                        retlayers.Add(layerTemp.First());
                    }
                }

                ActiveService._activeLayers = retlayers;
            }
        }

        public void SetActiveLayers(List<GISLayerInfo> layers)
        {
            if (_activeService != null)
            {
                _activeService.ActiveLayers.Clear();
                _activeService.ActiveLayers.AddRange(layers);
            }
        }

        public delegate void RequestComplete(GISResponse response);
        public delegate void ServiceRequestComplete(GISServer response);
        public delegate void ServiceDetailRequestComplete(GISServer server, GISService service);

        public event RequestComplete OnRequestError;
        public event RequestComplete OnMapRequestComplete;
        public event RequestComplete OnSearchRequestComplete;
        public event RequestComplete OnIdentifyRequestComplete;
        public event ServiceRequestComplete OnServiceRequestComplete;
        public event ServiceDetailRequestComplete OnServiceDetailRequestComplete;

        public void SetSearchCallback(RequestComplete onRequestComplete, RequestComplete onRequestError)
        {
            OnSearchRequestComplete = null;
            OnSearchRequestComplete += onRequestComplete;
            OnRequestError += onRequestError;
        }

        internal void RaiseErrorResponse(GISResponse errorResponse)
        {
            Controller.RequestProcessing = false;

            if (OnRequestError != null)
            {
                OnRequestError(errorResponse);
            }
        }

        internal void RaiseServiceResponse()
        {
            Controller.RequestProcessing = false;

            if (OnServiceRequestComplete != null)
            {
                OnServiceRequestComplete(this);
            }
        }

        internal void RaiseMapResponse(GISResponse response)
        {
            Controller.RequestProcessing = false;

            try
            {
                if (!response.HasError && response is GISImageResponse)
                {
                    GISLayerInfo tempLayer;
                    GISImageResponse iresponse = response as GISImageResponse;

                    foreach (GISLayerInfo info in iresponse.Layers)
                    {
                        if (!ActiveService.FindLayer(info._name, out tempLayer))
                        {
                            ActiveService._baseLayers.Add(info);
                            ActiveService._activeLayers.Add(info);
                        }
                    }
                }

                if (OnMapRequestComplete != null)
                {
                    OnMapRequestComplete(response);
                }
            }
            catch { }
        }

        internal void RaiseSearchResponse(GISResponse response)
        {
            Controller.RequestProcessing = false;

            try
            {
                if (OnSearchRequestComplete != null)
                {
                    OnSearchRequestComplete(response);
                }
            }
            catch { }
        }

        internal void RaiseIdentifyResponse(GISResponse response)
        {
            Controller.RequestProcessing = false;

            try
            {
                if (OnIdentifyRequestComplete != null)
                {
                    OnIdentifyRequestComplete(response);
                }
            }
            catch { }
        }

        internal void RaiseServiceDetailResponse(GISService service)
        {
            Controller.RequestProcessing = false;

            try
            {
                if (OnServiceDetailRequestComplete != null)
                {
                    OnServiceDetailRequestComplete(this, service);
                }
            }
            catch { }
        }

        public void GetGroundResolution(int zoom, out double xmper, out double ymper)
        {
            Controller.GetGroundResolution(zoom, out xmper, out ymper);
        }

        public GISLayerInfo AddLayer(string name, string id)
        {
            return Controller.CreateLayer(name, id, true);
        }

        public void SetMapEvents(RequestComplete requestComplete, RequestComplete requestError, RequestComplete requestIdentifyComplete)
        {
            OnMapRequestComplete = OnRequestError = OnIdentifyRequestComplete = null;

            OnMapRequestComplete += requestComplete;
            OnRequestError += requestError;
            OnIdentifyRequestComplete += requestIdentifyComplete;
        }

        public void SetServiceCallbacks(ServiceRequestComplete serviceRequestComplete, RequestComplete requestComplete)
        {
            OnServiceDetailRequestComplete = null;
            OnServiceRequestComplete = null;
            OnServiceRequestComplete = serviceRequestComplete;
            OnRequestError = requestComplete;
        }
    }
}
