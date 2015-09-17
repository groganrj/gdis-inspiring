using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AtlasOf.GIS
{
    public enum ZOOM_TYPE
    {
        PERCENT,
        TILED
    }

    public abstract class GISController
    {
        #region Member Variables
        internal int _imageWidth = 500;
        internal int _imageHeight = 400;
        internal ZOOM_TYPE _zoomType = ZOOM_TYPE.PERCENT;
        internal int _maxZoom = 100;
        internal int _minZoom = 0;
        internal GISServer _Server;
        #endregion

        #region Properties

        internal GISServer Server
        {
            get { return _Server; }
        }

        public int ImageHeight
        {
            get { return _imageHeight; }
        }

        public int ImageWidth
        {
            get { return _imageWidth; }
        }

        public virtual void SetMapDimensions(int imageHeight, int imageWidth)
        {
            _imageHeight = imageHeight;
            _imageWidth = imageWidth;
        }

        internal ZOOM_TYPE ZoomType
        {
            get { return _zoomType; }
        }

        internal int MaxZoom
        {
            get { return _maxZoom; }
        }

        internal int MinZoom
        {
            get { return _minZoom; }
        }
        #endregion

        #region Constructor
        public GISController(GISServer server)
        {
            _Server = server;
        }
        #endregion

        #region Methods
        public abstract GISService CreateService(string serviceName);

        public abstract GISService AddService(string serviceName, string serviceId, ServiceType serviceType);

        public abstract GISLayerInfo CreateLayer(string layerName, string layerId, bool isVisible);

        public abstract Task<bool> GetClientServices();

        public abstract Task<bool> GetServiceDetails(GISService activeService);

        public abstract bool GetMap(GISService activeService);

        public abstract bool GetMap(GISService activeService, GISEnvelope mapEnvelope);

        public abstract bool GetMap(GISService activeService, GISEnvelope mapEnvelope, List<GISLayerInfo> mapLayers);

        public abstract bool GetMap(GISService activeService, double centerX, double centerY, double zoomLevel);

        public abstract bool GetMap(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, string requestString);

        public abstract string GetMapImage(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, int width, int height);

        //public abstract string GetMapLegend(GISService activeService, List<GISLayerInfo> mapLayers, GISEnvelope mapEnvelope, double zoomLevel, int width, int height);

        //http://gis.ncdc.noaa.gov/arcgis/rest/services/basemaps/MapServer/legend

        public GISEnvelope SelectEnvelope(GISService activeService)
        {
            if (activeService.HasLayers) // || GetServiceDetails(activeService))
            {
                if (activeService.ActiveLayers.Count > 0)
                {
                    foreach (GISLayerInfo serviceInfo in activeService.ActiveLayers)
                    {
                        if (serviceInfo.IsVisible && serviceInfo.BaseExtent != null)
                        {
                            return serviceInfo.BaseExtent;
                        }
                    }
                }
                else if (activeService.BaseLayers.Count > 0)
                {
                    foreach (GISLayerInfo serviceInfo in activeService.BaseLayers)
                    {
                        if (serviceInfo.IsVisible && serviceInfo.BaseExtent != null)
                        {
                            return serviceInfo.BaseExtent;
                        }
                    }
                }

                if (activeService.BaseExtent != null)
                {
                    return activeService.BaseExtent;
                } 
                else return GISEnvelope.TheWorld;
            }
            else if (activeService.BaseExtent != null)
            {
                return activeService.BaseExtent;
            }
            else return GISEnvelope.TheWorld;
        }

        protected GISEnvelope BuildEnvelope(double centerX, double centerY, GISEnvelope baseEnvelope, double zoomLevel)
        {
            if (zoomLevel == 0) return baseEnvelope;

            double newHeight = (baseEnvelope.Height * (100.0 - zoomLevel) / 100.0) / 2.0;
            double newWidth = (baseEnvelope.Width * (100.0 - zoomLevel) / 100.0) / 2.0;
            GISEnvelope mapEnvelope = new GISEnvelope(centerX - newWidth, centerX + newWidth, centerY - newHeight, centerY + newHeight);
            mapEnvelope.CoordinateSystem = baseEnvelope.CoordinateSystem;

            if (mapEnvelope.minX < baseEnvelope.minX) mapEnvelope.minX = baseEnvelope.minX;
            if (mapEnvelope.minY < baseEnvelope.minY) mapEnvelope.minY = baseEnvelope.minY;
            if (mapEnvelope.maxX > baseEnvelope.maxX) mapEnvelope.maxX = baseEnvelope.maxX;
            if (mapEnvelope.maxY > baseEnvelope.maxY) mapEnvelope.maxY = baseEnvelope.maxY;

            //TransformEnvelope(ref mapEnvelope);
            return mapEnvelope;
        }

        public virtual void ExecuteSearch(string searchTerm, SEARCH_TYPE searchType, GISEnvelope searchArea, GISLayerInfo featureLayer)
        {
            throw new NotSupportedException(string.Format("Server type {0} does not expose a search interface.", this._Server.Type));
        }

        //public abstract bool ExecuteQuery(GISService activeService, GISLayerInfo queryLayer, GISEnvelope queryEnvelope, int maxFeaturesReturned);

        public abstract void GetImageLayers(GISService service, List<GISLayerInfo> layers);

        public abstract void GetQueryLayers(GISService service, ref List<GISLayerInfo> layers);

        public abstract string GetErrorMessage(string responseXml);

        public virtual bool Identify(int xpoint, int ypoint, double latitude, double longitude, GISEnvelope envelope, GISLayerInfo featureLayer)
        {
            throw new NotImplementedException("base class");
        }

        #endregion

        #region Functions
        internal void TransformEnvelope(ref GISEnvelope inputEnvelope)
        {
            if (inputEnvelope.CoordinateSystem != "EPSG:4326") return;

            double aspectImg = (double)_imageWidth / (double)_imageHeight;

            inputEnvelope.maxX = inputEnvelope.minX + (inputEnvelope.Height * aspectImg);
            double aspectEnv = inputEnvelope.Width / inputEnvelope.Height;
        }

        internal virtual GISEnvelope ZoomAbsolute(double zoomPercent, GISEnvelope lastEnvelope)
        {
            double zoomAmount = 1 - zoomPercent / 100.0;
            GISEnvelope envelope = SelectEnvelope(_Server.ActiveService);

            if (lastEnvelope != null)
            {
                return envelope.Zoom(zoomAmount, lastEnvelope.CenterX, lastEnvelope.CenterY);
            }
            else return envelope.Zoom(zoomAmount, envelope.CenterX, envelope.CenterY);
        }

        //internal GISEnvelope SelectEnvelope(GISEnvelope lastEnvelope)
        //{
        //    if (_Server.ActiveLayers.Count > 0)
        //    {
        //        foreach (GISLayerInfo serviceInfo in _Server.ActiveLayers)
        //        {
        //            if (serviceInfo.IsVisible && serviceInfo.BaseExtent != null)
        //            {
        //                return serviceInfo.BaseExtent;
        //            }
        //        }
        //    }

        //    if (_Server.ActiveService.BaseLayers.Count > 0)
        //    {
        //        foreach (GISLayerInfo serviceInfo in _Server.ActiveService.BaseLayers)
        //        {
        //            if (serviceInfo.IsVisible && serviceInfo.BaseExtent != null)
        //            {
        //                return serviceInfo.BaseExtent;
        //            }
        //        }
        //    }

        //    if (_Server.ActiveService.BaseExtent != null)
        //    {
        //        return _Server.ActiveService.BaseExtent;
        //    }

        //    return lastEnvelope;
        //}

        internal virtual double SetZoomLevel(GISEnvelope lastEnvelope, GISEnvelope maximumExtent)
        {
            if (lastEnvelope != null && maximumExtent != null)
            {
                //TransformEnvelope(ref lastEnvelope);
                //TransformEnvelope(ref maximumExtent);
                double zoompercent = 1 - Math.Min(lastEnvelope.Width / maximumExtent.Width, lastEnvelope.Height / maximumExtent.Height);
                //            double zoompercent = lastEnvelope.Height / maximumExtent.Height;
                return 100.0 * zoompercent;
            }
            else return 0;
        }
        #endregion

        //internal GISEnvelope GetTileEnvelope(double centerX, double centerY, int zoomLevel)
        //{
        //    if (zoomLevel == MinZoom) return GISEnvelope.TheWorld;

        //    double R = 6378137.0; // Earth's mean radius in meters
        //    double groundresolution = GetGroundResolution(zoomLevel);
        //    //2 * (Math.Cos(mapEnvelope.CenterY * Math.PI / 180) * 2 * Math.PI * R /* meters */) / (256 * 2 << zoomLevel);
        //    //            double groundresolution = (mapEnvelope.Height * Math.PI / 180.0) * R * 2 / _imageHeight; // _imageWidth

        //    double lat1 = centerY * 0.0174532925;
        //    double lon1 = centerX * 0.0174532925;

        //    double dist = (groundresolution * _imageHeight / 2) / R;

        //    double northlat = Math.Asin(Math.Sin(lat1) * Math.Cos(dist) +
        //                            Math.Cos(lat1) * Math.Sin(dist) * Math.Cos(0));

        //    double southlat = Math.Asin(Math.Sin(lat1) * Math.Cos(dist) +
        //                            Math.Cos(lat1) * Math.Sin(dist) * Math.Cos(180 * 0.0174532925));

        //    dist = (groundresolution * _imageWidth / 2) / R;

        //    double eastlon = lon1 + Math.Atan2(Math.Sin(90 * 0.0174532925) * Math.Sin(dist) * Math.Cos(lat1),
        //                                 Math.Cos(dist) - Math.Sin(lat1) * Math.Sin(lat1));

        //    double westlon = lon1 + Math.Atan2(Math.Sin(270 * 0.0174532925) * Math.Sin(dist) * Math.Cos(lat1),
        //                                 Math.Cos(dist) - Math.Sin(lat1) * Math.Sin(lat1));

        //    eastlon = (eastlon + 3 * Math.PI) % (2 * Math.PI) - Math.PI;  // normalise to -180...+180
        //    westlon = (westlon + 3 * Math.PI) % (2 * Math.PI) - Math.PI;  // normalise to -180...+180

        //    GISEnvelope env = new GISEnvelope(Math.Round(westlon / 0.0174532925, 4),
        //                                            Math.Round(eastlon / 0.0174532925, 4),
        //                                            Math.Round(southlat / 0.0174532925, 4),
        //                                            Math.Round(northlat / 0.0174532925, 4));

        //    return env;
        //}

        internal GISEnvelope GetTileEnvelope(double centerX, double centerY, int zoomLevel)
        {
            if (zoomLevel == MinZoom) return GISEnvelope.TheWorld;

            double xmper, ymper;
            GetGroundResolution(zoomLevel, out xmper, out ymper); // meters/pixel
            double height = (ymper * _imageHeight) / 1000.0; //kilometers
            double width = (xmper * _imageWidth) / 1000.0;

            GISEnvelope env = GreatCircleEquation.Calculate(centerX, centerY, width, height);
            return new GISEnvelope(env.MinX, env.MaxX, env.MinY, env.MaxY);
        }

        public virtual void GetGroundResolution(int zoomLevel, out double xmper, out double ymper)
        {
            if (_zoomType == ZOOM_TYPE.TILED)
            {
                switch (zoomLevel)
                {
                    case 1:
                        xmper = 67500;
                        ymper = 62000;
                        break;
                    case 2:
                        xmper = 39700;
                        ymper = 39700;
                        break;
                    case 3:
                        xmper = 18000;
                        ymper = 19700;
                        break;
                    case 4:
                        xmper = 8800;
                        ymper = 9400;
                        break;
                    case 5:
                        xmper = 4750;
                        ymper = 4650;
                        break;
                    case 6:
                        xmper = 1850;
                        ymper = 2100;
                        break;
                    case 7:
                        xmper = 910;
                        ymper = 900;
                        break;
                    case 8:
                        xmper = 400;
                        ymper = 500;
                        break;
                    case 9:
                        xmper = 240;
                        ymper = 250;
                        break;
                    case 10:
                        xmper = 120;
                        ymper = 125;
                        break;
                    case 11:
                        xmper = 50;
                        ymper = 50;
                        break;
                    case 12:
                        xmper = 25;
                        ymper = 25;
                        break;
                    case 13:
                        xmper = 12;
                        ymper = 12;
                        break;
                    case 14:
                        xmper = 6;
                        ymper = 6;
                        break;
                    case 15:
                        xmper = 3;
                        ymper = 3;
                        break;
                    case 16:
                        xmper = 1.5;
                        ymper = 1.5;
                        break;
                    case 17:
                        xmper = 0.7273;
                        ymper = 0.7273;
                        break;
                    case 18:
                        xmper = 0.3673;
                        ymper = 0.3673;
                        break;
                    default:
                        xmper = 0.1873;
                        ymper = 0.1873;
                        break;
                }
            }
            else
            {
                xmper = 67500 - (zoomLevel * 675.00);
                ymper = 67500 - (zoomLevel - 675.00);
            }
        }


        //public override double GetGroundResolution(int zoomLevel, out double xmper, out double ymper)
        //{
        //    switch (zoomLevel)
        //    {
        //        case 1: return 78271.52;
        //        case 2: return 39135.76;
        //        case 3: return 19567.88;
        //        case 4: return 9783.94;
        //        case 5: return 4891.97;
        //        case 6: return 2445.98;
        //        case 7: return 1222.99;
        //        case 8: return 611.4962;
        //        case 9: return 305.7481;
        //        case 10: return 152.8741;
        //        case 11: return 76.437;
        //        case 12: return 38.2185;
        //        case 13: return 19.1093;
        //        case 14: return 9.5546;
        //        case 15: return 4.7773;
        //        case 16: return 2.3887;
        //        case 17: return 1.1943;
        //        case 18: return 0.5972;
        //        case 19: return 0.2986;
        //        case 20: return 0.1493;
        //        case 21: return 0.0746;
        //        case 22: return 0.0373;
        //        default: return 0.0187;
        //    }
        //}

        public bool RequestProcessing { get; set; }

        public virtual IGISLegend GetLegend(GISLayerInfo selectedLayer)
        {
            return new GISLegend() {  };
        }
    }
}

