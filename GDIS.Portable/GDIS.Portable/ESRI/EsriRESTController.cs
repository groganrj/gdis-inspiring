using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Xml.Linq;
using Nii.JSON;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace AtlasOf.GIS.ESRI
{
    public class EsriRESTController : GISController
    {
        #region Constants
        // http://events.arcgisonline.com/ArcGIS/rest/services?f=json
        private const string CATALOG_URL = "http://{0}/{1}?f=json";
        private const string CATALOG_FOLDER_URL = "http://{0}/{2}/{1}?f=json";
        // http://events.arcgisonline.com/ArcGIS/rest/services/Davidson_County/MapServer?f=json
        private const string SERVICE_URL = "http://{0}/{3}/{1}/{2}?f=json";
        private const string SERVICE_LAYER_URL = "http://{0}/{3}/{1}/{2}?f=json";
        private const string IMAGE_URL = "http://{0}/{6}/{1}/{2}/export?bbox={3}&size={4},{5}&layers={7}&format=jpg&transparent=false&f={8}";
        private const string LEGEND_URL = "http://{0}/{1}/{2}/{3}/legend?f=json";
        private const string EXPORT_IMAGE_URL = "http://{0}/{6}/{1}/{2}/exportImage?bbox={3}&size={4},{5}&layers={7}&format=jpg&transparent=false&f={8}";
        private const string IDENTIFY_URL = "http://{0}/{10}/{1}/{2}/identify?geometryType=esriGeometryPoint&geometry={3},{4}&wkid={5}&layers=all:{6}&tolerance=5&mapExtent={7}&imageDisplay={8},{9},96&returnGeometry=true&f=json";
        //   /ArcGIS/rest/services/ AL/Map/identify?geometryType=esriGeometryPoint&geometry=540,412&     sr=&layers=all:6&time=&layerTimeOptions=&layerdefs=&tolerance=5&mapExtent=540684.666300471,167906.874423412,678219.388522694,262751.155344241&imageDisplay=451,654,96&returnGeometry=true&maxAllowableOffset=&f=HTML
        //http://tasks.arcgis.com/ArcGIS/rest/services/WorldLocator/LocationServer/findAddressCandidates?address=grand+canyon&outSR=&country=&max=&localeCode=&f=pjson
        private const string QUERY_URL = "http://{0}/{5}/{1}/{2}/{3}/query?geometry={4}&geometryType=esriGeometryEnvelope&outFields=*&returnGeometry=false&f=json";
        //http://MapOfTheDayserver1.arcgisonline.com/arcgis/rest/services/locators/esri_geocode_usa/geocodeserver/reversegeocode?location=-117.195681386%2c34.057517097&distance=0
        private const string GEOCODE_URL = "http://{0}/{1}/locators/{2}/geocodeserver/reversegeocode?location={3}%2c{4}&distance=100&f=json";
        #endregion

        #region Constructor

        public EsriRESTController(GISServer esriServer)
            : base(esriServer)
        {
            _zoomType = ZOOM_TYPE.PERCENT;
            if (string.IsNullOrEmpty(Server.ServletPath)) Server.ServletPath = "arcgis/rest/services";
        }

        #endregion

        #region Methods

        public override string GetMapImage(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, int height, int width)
        {
            //http://basemap.nationalmap.gov/ArcGIS/rest/services/USGSTopo/MapServer/export?bbox=-15809463.2958818%2C2409214.11633487%2C-7845336.4447948%2C7007665.73796985&bboxSR=&layers=&layerdefs=&size=250%2C250&imageSR=&format=png&transparent=false&dpi=&time=&layerTimeOptions=&f=image

            EsriEnvelope imageEnvelope = new EsriEnvelope(mapEnvelope);
            string activeServiceName = activeService.Id.IndexOf("__") > 0 ? activeService.Id.Substring(0, activeService.Id.IndexOf("__")) : activeService.Id;
            string requestUrl = activeService.ServiceType == ServiceType.MapServer ? String.Format(IMAGE_URL, Server.Host, activeServiceName, activeService.ServiceType, imageEnvelope.ToJSON(), height, width, Server.ServletPath, BuildLayers(mapLayers), "image")
                : String.Format(EXPORT_IMAGE_URL, Server.Host, activeServiceName, activeService.ServiceType, imageEnvelope.ToJSON(), height, width, Server.ServletPath, BuildLayers(mapLayers), "image");

            return requestUrl;
        }

        public override void GetQueryLayers(GISService service, ref List<GISLayerInfo> layers)
        {
            if (service.HasLayers || GetServiceDetails(service).Result)
            {
                layers.AddRange(service.BaseLayers);
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

        public override async Task<bool> GetClientServices()
        {
            string requestUrl = String.Format(CATALOG_URL, Server.Host, Server.ServletPath);

            try
            {
                var result = await webClient.GetRequestAsync(requestUrl);

                if (result.success)
                {
                    return await ProcessServiceReturn(result.output);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        SimpleWebClient webClient = new SimpleWebClient();

        public override void ExecuteSearch(string searchTerm, SEARCH_TYPE searchType, GISEnvelope searchArea, GISLayerInfo featureLayer)
        {
            string activeServiceName = Server.ActiveService.Name.IndexOf("__") > 0 ? Server.ActiveService.Name.Substring(0, Server.ActiveService.Name.IndexOf("__")) : Server.ActiveService.Name;
            EsriEnvelope queryEnvelope = new EsriEnvelope(searchArea);
            string requestUrl = string.Empty;

            if (searchType == SEARCH_TYPE.Geocode)
            {
                //http://planthardiness.ars.usda.gov/ArcGIS/rest/services/uspz/MapServer/identify?geometryType=esriGeometryPoint&geometry=-120%2C40&tolerance=10&mapExtent=-119%2C38%2C-121%2C41&imageDisplay=400%2C300%2C96
                //http://services.arcgisonline.com/ArcGIS/rest/services/Specialty/Soil_Survey_Map/MapServer/identify?geometryType=esriGeometryPoint&geometry=19.23865%2C48.23152&sr=102100&layers=0&time=&layerTimeOptions=&layerdefs=&tolerance=100&mapExtent=19.23865%2C48.23152%2C19.23865%2C48.23152&imageDisplay=600%2C550%2C96&returnGeometry=true&maxAllowableOffset=&f=json
                requestUrl = String.Format(GEOCODE_URL, Server.Host, Server.ServletPath, activeServiceName, searchArea.CenterX, searchArea.CenterY);
            }
            else if (searchType == SEARCH_TYPE.Identify)
            {
                //http://planthardiness.ars.usda.gov/ArcGIS/rest/services/uspz/MapServer/identify?geometryType=esriGeometryPoint&geometry=-120%2C40&tolerance=10&mapExtent=-119%2C38%2C-121%2C41&imageDisplay=400%2C300%2C96
                //http://services.arcgisonline.com/ArcGIS/rest/services/Specialty/Soil_Survey_Map/MapServer/identify?geometryType=esriGeometryPoint&geometry=19.23865%2C48.23152&sr=102100&layers=0&time=&layerTimeOptions=&layerdefs=&tolerance=100&mapExtent=19.23865%2C48.23152%2C19.23865%2C48.23152&imageDisplay=600%2C550%2C96&returnGeometry=true&maxAllowableOffset=&f=json
                requestUrl = String.Format(GEOCODE_URL, Server.Host, Server.ServletPath, activeServiceName, searchArea.CenterX, searchArea.CenterY);
            }
            else
            {
                requestUrl = String.Format(QUERY_URL, Server.Host, activeServiceName, Server.ActiveService.ServiceType, Server.ActiveService.BaseLayers[0].Id, queryEnvelope.ToJSON(), Server.ServletPath);
            }

            EsriFeatureResponse response = new EsriFeatureResponse();
            response._envelope = searchArea;
            response._layers = new List<GISLayerInfo>() { featureLayer };
            response.HasError = false;
            response.LastRequest = requestUrl;

            try
            {
                webClient.GetRequest(requestUrl, new EventHandler<WebEventArgs>(ProcessQueryReturn), response);
                //wc.DownloadStringAsync(new Uri(requestUrl), new AsyncState() { Response = response, CurrentState = Server.ActiveService });
            }
            catch (Exception ex)
            {
                Server.RaiseSearchResponse(new GISResponse() { HasError = true, ErrorMessage = string.Format("{0}: {1}", ex.Message), LastRequest = requestUrl, LastResponse = string.Empty, _layers = new List<GISLayerInfo>() { Server.ActiveService.BaseLayers[0] }, _envelope = queryEnvelope });
            }
        }

        private JSONObject BuildQuery(GISLayerInfo queryLayer, int maxFeaturesReturned, GISEnvelope queryEnvelope)
        {
            return new JSONObject();
        }

        public override bool GetMap(GISService activeService)
        {
            GISEnvelope imageEnvelope = SelectEnvelope(activeService);

            return GetMap(activeService, imageEnvelope);
        }

        public override bool GetMap(GISService activeService, GISEnvelope imageEnvelope)
        {
            EsriEnvelope mapEnvelope = new EsriEnvelope(imageEnvelope);
            string activeServiceName = activeService.Id.IndexOf("__") > 0 ? activeService.Id.Substring(0, activeService.Id.IndexOf("__")) : activeService.Id;
            string requestUrl = activeService.ServiceType == ServiceType.MapServer ? String.Format(IMAGE_URL, Server.Host, activeServiceName, activeService.ServiceType, mapEnvelope.ToJSON(), _imageWidth, _imageHeight, Server.ServletPath, BuildLayers(activeService.ActiveLayers), "json")
                : String.Format(EXPORT_IMAGE_URL, Server.Host, activeServiceName, activeService.ServiceType, mapEnvelope.ToJSON(), _imageWidth, _imageHeight, Server.ServletPath, BuildLayers(activeService.ActiveLayers), "json");

            return GetMap(activeService, activeService.ActiveLayers, mapEnvelope, SetZoomLevel(mapEnvelope, SelectEnvelope(activeService)), requestUrl);
        }

        private string BuildLayers(List<GISLayerInfo> layerList)
        {
            if (layerList.Count == 0) return string.Empty;

            StringBuilder layers = new StringBuilder();

            foreach (GISLayerInfo info in layerList)
            {
                layers.AppendFormat("{0},", info.Id);
            }

            return layers.ToString(0, layers.Length - 2);
        }

        public override bool GetMap(GISService activeService, double centerX, double centerY, double zoomLevel)
        {
            zoomLevel = Math.Min(MaxZoom, Math.Max(MinZoom, zoomLevel));
            if (activeService.ActiveLayers.Count == 0 && activeService._baseLayers.Count > 0) activeService.ActiveLayers.Add(activeService._baseLayers[0]);

            GISEnvelope mapEnvelope = BuildEnvelope(centerX, centerY, SelectEnvelope(activeService), zoomLevel);

            EsriEnvelope env = new EsriEnvelope(mapEnvelope.minX, mapEnvelope.maxX, mapEnvelope.minY, mapEnvelope.maxY);
            string activeServiceName = activeService.Id.IndexOf("__") > 0 ? activeService.Id.Substring(0, activeService.Id.IndexOf("__")) : activeService.Id;
            string requestUrl = activeService.ServiceType == ServiceType.MapServer ? String.Format(IMAGE_URL, Server.Host, activeServiceName, activeService.ServiceType, env.ToJSON(), _imageWidth, _imageHeight, Server.ServletPath, BuildLayers(activeService.ActiveLayers), "json")
                : String.Format(EXPORT_IMAGE_URL, Server.Host, activeServiceName, activeService.ServiceType, env.ToJSON(), _imageWidth, _imageHeight, Server.ServletPath, BuildLayers(activeService.ActiveLayers), "json");

            return GetMap(activeService, activeService.ActiveLayers, mapEnvelope, zoomLevel, requestUrl);
        }

        public override bool Identify(int xpoint, int ypoint, double latitude, double longitude, GISEnvelope envelope, GISLayerInfo featureLayer)
        {
            try
            {
                // http://{0}/ArcGIS/rest/services/{1}/{2}/identify?geometryType=esriGeometryPoint&geometry={{x:{3}, y:{4} }}&wkid={5}&layers=all:{6}&tolerance=10&mapExtent={7}&imageDisplay={8},{9},96&returnGeometry=true
                string layerid = featureLayer.Id;
                string requestUrl = string.Format(IDENTIFY_URL, _Server.Host, _Server.ActiveService.Id, _Server.ActiveService.ServiceType, longitude, latitude, envelope.CoordinateSystem, layerid, envelope.ToString(), _imageHeight, _imageWidth, Server.ServletPath);

                GISFeatureResponse response = new GISFeatureResponse();
                response._envelope = BuildEnvelope(longitude, latitude, SelectEnvelope(Server.ActiveService), MaxZoom - 2);
                response._layers = new List<GISLayerInfo>() { featureLayer };
                response.HasError = false;
                response.LastRequest = requestUrl;

                webClient.GetRequest(requestUrl, ProcessIdentifyResponse, response);
                return true;
            }
            catch (Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { _envelope = null, _layers = new List<GISLayerInfo>() { new EsriLayerInfo() { _name = "Search", _type = "Search" } }, ErrorMessage = ex.Message });
                return false;
            }
        }

        void ProcessIdentifyResponse(object sender, WebEventArgs e)
        {
            try
            {
                GISFeatureResponse response = e.UserState as GISFeatureResponse;
                response.LastResponse = e.ResponseString;
                Nii.JSON.JSONObject j = new Nii.JSON.JSONObject(e.ResponseString);

                //{"authenticationResultCode":"ValidCredentials","brandLogoUri":"http:\/\/dev.virtualearth.net\/Branding\/logo_powered_by.png","copyright":"Copyright Â© 2010 Microsoft and its suppliers. All rights reserved. This API cannot be accessed and the content and any results may not be used, reproduced or transmitted in any manner without express written permission from Microsoft Corporation.","resourceSets":[{"estimatedTotal":1,"resources":[{"__type":"ImageryMetadata:http:\/\/schemas.microsoft.com\/search\/local\/ws\/rest\/v1","imageHeight":256,"imageUrl":"http:\/\/t3.tiles.virtualearth.net\/tiles\/a032010110123333.jpeg?g=580&mkt={culture}&token={token}","imageUrlSubdomains":null,"imageWidth":256,"imageryProviders":null,"vintageEnd":"28 Feb 2007 GMT","vintageStart":"01 Jun 2006 GMT","zoomMax":15,"zoomMin":15}]}],"statusCode":200,"statusDescription":"OK","traceId":"986114694e894832aeb8e3c4e53ca7e3|CH1M001465|02.00.147.700|"}            }
                bool hasFields = response.Layers.Count > 0 && response.Layers[0]._Fields.Count > 0;

                Nii.JSON.JSONArray resultObj = j.getJSONArray("results");

                for (int i = 0; i < resultObj.Count; i++)
                {
                    Nii.JSON.JSONObject obj = resultObj[i] as Nii.JSON.JSONObject;

                    GISFeature feature = EsriFeature.Create(obj, response.Layers[0], hasFields);

                    if (feature.Envelope == null) feature.Envelope = response._envelope;

                    response.Features.Add(feature);
                }
                Server.RaiseIdentifyResponse(response);
            }
            catch (Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { _envelope = null, _layers = new List<GISLayerInfo>() { new EsriLayerInfo() { _name = "Search", _type = "Search" } }, ErrorMessage = ex.Message });
            }
        }

        public override bool GetMap(GISService activeService, GISEnvelope imageEnvelope, List<GISLayerInfo> mapLayers)
        {
            string requestUrl = string.Empty;

            try
            {
                List<GISLayerInfo> totalLayers = GetTotalLayers(activeService);

                EsriEnvelope mapEnvelope = new EsriEnvelope(imageEnvelope);
                requestUrl = activeService.ServiceType == ServiceType.MapServer ? String.Format(IMAGE_URL, Server.Host, activeService.Name, activeService.ServiceType, mapEnvelope.ToJSON(), _imageWidth, _imageHeight, Server.ServletPath, BuildLayers(mapLayers), "json")
                    : String.Format(EXPORT_IMAGE_URL, Server.Host, activeService.Name, activeService.ServiceType, mapEnvelope.ToJSON(), _imageWidth, _imageHeight, Server.ServletPath, BuildLayers(mapLayers), "json");

                return GetMap(activeService, totalLayers, mapEnvelope, SetZoomLevel(mapEnvelope, activeService._baseExtent), requestUrl);
            }
            catch (System.Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { LastResponse = ex.Message, ErrorMessage = ex.Message, HasError = true, LastRequest = requestUrl, _envelope = imageEnvelope, _layers = mapLayers });
                return false;
            }
        }

        private List<GISLayerInfo> GetTotalLayers(GISService activeService)
        {
            List<GISLayerInfo> totalLayers = new List<GISLayerInfo>();
            totalLayers.AddRange(activeService.ActiveLayers);

            foreach (GISLayerInfo info in activeService._baseLayers)
            {
                if (!totalLayers.Contains(info) && info._isVisible == true)
                {
                    totalLayers.Add(CreateLayer(info._name, info._id, false));
                }
            }

            return totalLayers;
        }

        public override bool GetMap(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, string requestString)
        {
            EsriImageResponse response = new EsriImageResponse();

            try
            {
                response.Envelope = mapEnvelope;
                response._layers.AddRange(mapLayers);
                response.LastRequest = requestString;
                response._mapImageUrl = requestString;
                response.ZoomLevel = zoomLevel;

                webClient.GetRequest(requestString, ProcessImageReturn, response);
                return true;
            }
            catch (System.Exception ex)
            {
                Server.RaiseErrorResponse(new GISResponse() { LastResponse = ex.Message, ErrorMessage = ex.Message, HasError = true, LastRequest = requestString, _envelope = response.Envelope, _layers = mapLayers });
                return false;
            }
        }

        public override IGISLegend GetLegend(GISLayerInfo selectedLayer)
        {
            EsriLegend response = new EsriLegend();

            try
            {
                string legendUrl = String.Format(LEGEND_URL, Server.Host, Server.ServletPath, Server.ActiveService.Id, Server.ActiveService.ServiceType);
                Task<WebResponseResult> resp = webClient.GetRequestAsync(legendUrl);
                System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(EsriLegend));
                MemoryStream mem = new MemoryStream(Encoding.UTF8.GetBytes(resp.Result.output));
                response = (EsriLegend)ser.ReadObject(mem);
            }
            catch (System.Exception ex)
            {
                //throw ex;
            }

            return response;
        }

        public override string GetErrorMessage(string responseXml)
        {
            //                    // {"error":{"code":500,"message":"Server Error - Object reference not set to an instance of an object.","details":[]}}

            return responseXml;
        }

        public override GISService CreateService(string serviceName)
        {
            EsriService svc = new EsriService(serviceName);
            svc.ServiceType = ServiceType.MapServer;
            Server._services.Add(svc);
            return svc;
        }

        public override GISService AddService(string serviceName, string serviceId, ServiceType serviceType)
        {
            EsriService svc = new EsriService(serviceName) { _type = serviceType, _serviceId = serviceId };
            Server._services.Add(svc);
            return svc;
        }

        public override GISLayerInfo CreateLayer(string layerName, string layerId, bool isVisible)
        {
            return new EsriLayerInfo() { _id = layerId, _isVisible = isVisible, _name = layerName, IsQueryable = true };
        }

        #endregion

        #region Functions

        //{"layers":[{"layerId":0,"layerName":"2010 Land Use","layerType":"Feature Layer","minScale":0,"maxScale":0,"legend":[{"label":"Farmstead","url":"E737F8B6","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEW9nnj+///C8uRDAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Seasonal/Vacation","url":"43F9BFAF","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEX+/////8wh8EffAAAAAnRSTlMA/1uRIrUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAVSURBVBiVY2AgBgiiglGRQSVCGAAAwkwW6skEM3gAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Single Family Detached","url":"976164C5","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEX6/3P+//+j/AdrAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Manufactured Housing Park","url":"D127B1EC","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEWZYyb+///FuU8eAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Single Family Attached","url":"EE9DB6E5","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEXmujn+//+zUqrrAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Multifamily","url":"27AF6F15","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEXyogD+///2poEOAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Retail and Other Commercial","url":"B5B91BE9","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEX+////gID2PS9dAAAAAnRSTlMA/1uRIrUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAVSURBVBiVY2AgBgiiglGRQSVCGAAAwkwW6skEM3gAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Office","url":"F505ED93","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEX+////1taY1CFOAAAAAnRSTlMA/1uRIrUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAVSURBVBiVY2AgBgiiglGRQSVCGAAAwkwW6skEM3gAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Mixed Use Residential","url":"2C9524F4","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEWzdwD+//9zt6juAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Mixed Use Industrial","url":"1E5071E8","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEWWesz+///g3alkAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Mixed Use Commercial and Other","url":"434B2F39","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEXmRUX+//9Y33JDAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Industrial and Utility","url":"A391F981","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEXMs//+//9LU18QAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Extractive","url":"A7A22B13","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEXKevX+//8WstbDAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Institutional","url":"B37157A1","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEVwrcz+//8bFfKyAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Park, Recreational or Preserve","url":"A16A2714","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEVUxGH+//+sC2F2AAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Golf Course","url":"5B5C7673","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEVciUT+//931yDvAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Major Highway","url":"F09D19F7","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEVOTk7+//8NQVfQAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Railway","url":"845A649A","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEWcnJz+//8t37ffAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Airport","url":"1D4AE85F","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEVjiqb+//+5LWb0AAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Agricultural","url":"7C2A8735","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEWw2YL+//99ZIUHAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Undeveloped","url":"97A85C8D","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEXm8tr+//8fG1e1AAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"},{"label":"Water","url":"DF4B4A33","imageData":"iVBORw0KGgoAAAANSUhEUgAAACIAAAAbBAMAAADrHECUAAAABlBMVEWz9f/+//9mlE3tAAAAAnRSTlP/AOW3MEoAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAYSURBVBiVYxBEBwxYRBhQwajIoBIhHIMAMeUHk4DfYdsAAAAASUVORK5CYII=","contentType":"image/png"}]}]}

        internal void ProcessImageReturn(object sender, WebEventArgs e)
        {
            GISImageResponse response = e.UserState as GISImageResponse;

            try
            {
                response.LastResponse = e.ResponseString;
                Nii.JSON.JSONObject responseReader = new Nii.JSON.JSONObject(e.ResponseString);

                if (responseReader.getJSONObject("error") != null)
                {
                    Server.RaiseErrorResponse(new GISResponse() { LastResponse = e.ResponseString, ErrorMessage = e.ResponseString, HasError = true, LastRequest = response._mapImageUrl, _envelope = response._envelope, _layers = response._layers });
                }
                else
                {
                    response._mapImageUrl = responseReader.getString("href");

                    if (!string.IsNullOrEmpty(response.MapImageUrl))
                    {
                        Uri u;

                        if (!Uri.TryCreate(response._mapImageUrl, UriKind.Absolute, out u))
                        {
                            response._mapImageUrl = response._mapImageUrl.Replace(',', '.'); // BS artifact
                            Uri.TryCreate(response._mapImageUrl, UriKind.Absolute, out u);
                        }

                        if (u.Host.IndexOf('.') < 0)
                        {
                            Uri u2 = new Uri(response.LastRequest.Replace("f=json", "f=image"));
                            response._mapImageUrl = u2.AbsoluteUri;
                            //                            response._mapImageUrl = GetMapImage(Server.ActiveService, response.Layers, response.Envelope, response.ZoomLevel, ImageHeight, ImageWidth);
                            //                            response._mapImageUrl = response._mapImageUrl.Replace(u.Host, Server.Host);
                        }
                    }
                    else
                    {
                        Uri u = new Uri(response.LastRequest.Replace("f=json", "f=image"));
                        response._mapImageUrl = u.AbsoluteUri;
                    }

                    response._envelope = EsriEnvelope.Create(responseReader.getJSONObject("extent"));
                    Server.RaiseMapResponse(response);
                }
            }
            catch (Exception ex)
            {
                response.HasError = true;
                response.ErrorMessage = "Map error: " + ex.Message;
                Server.RaiseErrorResponse(response);
            }
        }

        internal void ProcessQueryReturn(object sender, WebEventArgs e)
        {
            GISResponse response = e.UserState as GISResponse;

            try
            {
                Nii.JSON.JSONObject responseReader = new Nii.JSON.JSONObject(e.ResponseString);
                response = EsriFeatureResponse.ProcessFeatureReturn(responseReader.getJSONArray("features"), response as GISFeatureResponse, e.ResponseString);
            }
            catch (Exception ex)
            {
                response.HasError = true;
                response.ErrorMessage = ex.Message;
                response = GISResponse.ProcessErrorResponse(ex.Message, response.LastRequest, e.ResponseString);
            }

            Server.RaiseSearchResponse(response);
        }

        private async Task<bool> ProcessServiceReturn(string responseString)
        {
            GISResponse response = new GISResponse();

            Server._lastUpdated = DateTime.Now;
            _Server._services.Clear();
            // sdfds        http://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/6/24/17.jpg

            try
            {
                response.LastResponse = responseString;
                Nii.JSON.JSONObject responseReader = new Nii.JSON.JSONObject(responseString);

                for (int idx = 0; idx < responseReader.Count; idx++)
                {
                    switch (responseReader[idx])
                    {
                        case "error":
                            response = GISResponse.ProcessErrorResponse(responseReader.getString("message"), response.LastRequest, responseString);
                            Server.RaiseErrorResponse(response);
                            break;
                        case "services":
                            List<GISService> nservices = EsriServiceResponse.ProcessServiceReturn(responseReader.getJSONArray("services").List);
                            AddServiceValues(_Server._services, nservices);
                            Server.RaiseServiceResponse();
                            break;
                        case "folders":
                            await AddServiceFolders(responseReader.getJSONArray("folders").List, _Server._services);
                            break;
                    }
                }

                return true;
                //if (UNRESOLVED_SERVICES == 0) Server.RaiseServiceResponse();
            }
            catch (Exception ex)
            {
                response.HasError = true;
                response.ErrorMessage = ex.Message;
                response = GISResponse.ProcessErrorResponse(ex.Message, response.LastRequest, responseString);
                Server.RaiseErrorResponse(response);
                return false;
            }
        }

        private void AddServiceValues(List<GISService> services, List<GISService> nservices)
        {
            foreach (GISService eservice in nservices)
            {
                if (services.Contains(eservice))
                {
                    eservice._serviceName = eservice.Name + "_1";
                }

                services.Add(eservice);
            }
        }

        private async Task AddServiceFolders(System.Collections.IList responseReader, List<GISService> services)
        {
            for (int idx = 0; idx < responseReader.Count; idx++)
            {
                string requestUrl = String.Format(CATALOG_FOLDER_URL, Server.Host, responseReader[idx], Server.ServletPath);

                try
                {
                    var result = await webClient.GetRequestAsync(requestUrl);

                    if (result.success)
                    {
                        Nii.JSON.JSONObject responseReaderService = new Nii.JSON.JSONObject(result.output);

                        for (int idx2 = 0; idx2 < responseReaderService.Count; idx2++)
                        {
                            switch (responseReaderService[idx2])
                            {
                                case "services":
                                    List<GISService> nservices = EsriServiceResponse.ProcessServiceReturn(responseReaderService.getJSONArray("services").List);
                                    AddServiceValues(_Server._services, nservices);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Server.RaiseErrorResponse(GISResponse.ProcessErrorResponse(ex.Message, requestUrl, "service folders"));
                }
            }
        }

        private void ProcessServiceDetailReturn(GISService svc, string responseString)
        {
            GISResponse response = new GISResponse();
            response.LastResponse = responseString;

            try
            {
                Nii.JSON.JSONObject responseReader = new Nii.JSON.JSONObject(responseString);

                Server._lastUpdated = DateTime.Now;

                EsriService.AddServiceInfo(svc, responseReader);
                Server.RaiseServiceDetailResponse(svc);
            }
            catch (Exception ex)
            {
                response.HasError = true;
                response.ErrorMessage = ex.Message;
                response = GISResponse.ProcessErrorResponse(ex.Message, response.LastRequest, responseString);
                Server.RaiseErrorResponse(response);
            }
        }
        #endregion

        public override async Task<bool> GetServiceDetails(GISService activeService)
        {
            if (activeService.HasLayers) return true;

            // http://services.arcgisonline.com/ArcGIS/rest/services/Demographics/USA_1990-2000_Population_Change/MapServer?f=json
            string requestUrl = String.Format(SERVICE_LAYER_URL, Server.Host, activeService._serviceName, activeService._type, Server.ServletPath);

            try
            {
                var result = await webClient.GetRequestAsync(requestUrl);
                ProcessServiceDetailReturn(activeService, result.output);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
