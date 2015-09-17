using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace AtlasOf.GIS
{
    public enum ServiceType
    {
        ImageServer,
        FeatureServer,
        ExtractServer,
        MetadataServer,
        MapServer,
        GlobeServer,
        GeometryServer,
        GeocodeServer,
        GeoDataServer,
        GPServer
    }

    public abstract class GISLayerInfo
    {
        internal string _id;
        internal string _type;
        internal double? _maxscale;
        internal double? _minscale;
        internal string _name;
        internal bool _isVisible = true;
        private int _featureCount = 0;
        internal GISEnvelope _baseExtent;
        internal List<GISField> _Fields = new List<GISField>();
        internal List<GISLayerInfo> _childLayers = new List<GISLayerInfo>();

        public bool IsQueryable { get; set; }

        public List<GISLayerInfo> ChildLayers
        {
            get { return _childLayers; }
            set { _childLayers = value; }
        }

        internal int FeatureCount
        {
            get { return _featureCount; }
            set { _featureCount = value; }
        }

        public List<GISField> Fields
        {
            get { return _Fields; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public double? Maxscale
        {
            get { return _maxscale; }
            set { _maxscale = value; }
        }

        public double? Minscale
        {
            get { return _minscale; }
            set { _minscale = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; }
        }

        public GISEnvelope BaseExtent
        {
            get { return _baseExtent; }
            set { _baseExtent = value; }
        }

        public string Description { get; set; }

        public bool IsActive { get; set; }
    }

    public abstract class GISService
    {
        #region Member Variables
        internal bool _isPublic = true;
        internal bool _isEnabled = true;
        internal string _serviceId;
        internal string _serviceName;
        internal string _keywords = string.Empty;
        internal string _subjects = string.Empty;
        private string _map = string.Empty;
        internal string _format = "image/png";
        internal string _description = string.Empty;
        internal ServiceType _type = ServiceType.ImageServer;
        internal GISEnvelope _baseExtent = GISEnvelope.TheWorld;
        internal List<GISLayerInfo> _baseLayers = new List<GISLayerInfo>();
        internal List<GISLayerInfo> _featureLayers = new List<GISLayerInfo>();
        internal List<GISLayerInfo> _activeLayers = new List<GISLayerInfo>();
        #endregion

        #region Properties
        public List<GISLayerInfo> ActiveLayers { get { return _activeLayers; } }

        public List<GISLayerInfo> FeatureLayers
        {
            get { return _featureLayers; }
        }

        public string Subjects
        {
            get { return _subjects; }
            set { _subjects = value; }
        }

        public string Keywords
        {
            get { return _keywords; }
            set { _keywords = value; }
        }

        public string Format
        {
            get { return _format; }
            set { _format = value; }
        }

        public GISEnvelope BaseExtent
        {
            get { return _baseExtent; }
            set { _baseExtent = value; }
        }

        public string Map
        {
            get { return _map; }
            set { _map = value; }
        }

        public bool IsPublic
        {
            get { return _isPublic; }
        }

        public bool HasLayers
        {
            get { return _baseLayers.Count > 0; }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        public string Id
        {
            get { return _serviceId; }
        }

        public string Name
        {
            get { return _serviceName; }
        }

        public ServiceType ServiceType
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public List<GISLayerInfo> BaseLayers
        {
            get { return _baseLayers; }
            set { _baseLayers = value; }
        }

        #endregion

        #region Constructor
        public GISService() { }

        public GISService(string serviceName)
        {
            _serviceName = serviceName;
        }
        #endregion

        //internal static Dictionary<string, GISService> GetServices(AtlasOf.Configuration.SERVERSERVERServices sERVERSERVERServices)
        //{
        //    Dictionary<string, GISService> services = new Dictionary<string, GISService>();

        //    foreach (AtlasOf.Configuration.SERVERSERVERServicesSERVICE service in sERVERSERVERServices.SERVICE)
        //    {
        //        if (!services.ContainsKey(service.NAME))
        //        {
        //            services.Add(service.NAME, GISService.Create(service));
        //        }
        //    }

        //    return services;
        //}

        //private static GISService Create(AtlasOf.Configuration.SERVERSERVERServicesSERVICE service)
        //{
        //    GISService newService = new GISService(service.NAME);
        //    newService._type = (ServiceType)System.Enum.Parse(typeof(ServiceType), service.TYPE, true);
        //    newService._isPublic = service.ACCESS == "PUBLIC";
        //    newService._description = service.DESC;
        //    return newService;
        //}

        //public static AtlasOf.Configuration.SERVERSERVERServicesSERVICE Persist(GISService service)
        //{
        //    AtlasOf.Configuration.SERVERSERVERServicesSERVICE newService = new AtlasOf.Configuration.SERVERSERVERServicesSERVICE();
        //    newService.NAME = service.Name;
        //    newService.TYPE = service.ServiceType.ToString();
        //    newService.ACCESS = service._isPublic ? "PUBLIC" : "HIDDEN";
        //    newService.DESC = service.Description;
        //    return newService;
        //}

        internal bool FindLayer(string layer, out GISLayerInfo returnLayer)
        {
            var lyr = from x in _baseLayers
                      where string.Compare(x._name, layer, StringComparison.CurrentCultureIgnoreCase) == 0
                          || string.Compare(x._id, layer, StringComparison.CurrentCultureIgnoreCase) == 0
                      select x;

            if (lyr.Count() > 0)
            {
                returnLayer = lyr.FirstOrDefault();
                return true;
            }
            else
            {
                var lyr2 = from x in _activeLayers
                           where string.Compare(x._name, layer, StringComparison.CurrentCultureIgnoreCase) == 0
                              || string.Compare(x._id, layer, StringComparison.CurrentCultureIgnoreCase) == 0
                           select x;

                if (lyr2.Count() > 0)
                {
                    returnLayer = lyr2.First();
                    return true;
                }
                else
                {
                    returnLayer = null;
                    return false;
                }
            }
        }

        public override bool Equals(object obj)
        {
            GISService svc = obj as GISService;

            if (svc == null) return false;
            return Name == svc.Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
