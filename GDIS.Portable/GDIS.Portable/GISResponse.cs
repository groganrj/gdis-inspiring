using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AtlasOf.GIS
{
    public class GISResponse
    {
        protected bool _hasError;
        private string _errorMessage;
        private string _lastRequest;
        private string _lastResponse;
        private double _zoomLevel;
        protected internal GISEnvelope _envelope;
        protected internal List<GISLayerInfo> _layers = new List<GISLayerInfo>();

        public string LastRequest
        {
            get { return _lastRequest; }
            set { _lastRequest = value; }
        }

        public string LastResponse
        {
            get { return _lastResponse; }
            set { _lastResponse = value; }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public bool HasError
        {
            get { return _hasError; }
            set { _hasError = value; }
        }

        public GISEnvelope Envelope
        {
            get { return _envelope; }
            set { _envelope = value; }
        }

        public List<GISLayerInfo> Layers
        {
            get { return _layers; }
            set { _layers = value; }
        }

        public double ZoomLevel
        {
            get { return _zoomLevel; }
            set { _zoomLevel = value; }
        }

        internal static GISResponse ProcessErrorResponse(XmlReader responseReader, string requestXml, string responseXml)
        {
            return ProcessErrorResponse(responseReader.ReadContentAsString(), requestXml, responseXml);
        }

        internal static GISResponse ProcessErrorResponse(string errorMessage, string requestXml, string responseXml)
        {
            GISResponse response = new GISResponse();
            response._lastRequest = requestXml;
            response._lastResponse = responseXml;
            response._hasError = true;
            response._errorMessage = errorMessage;

            return response;
        }
    }

    public abstract class GISImageResponse : GISResponse
    {
        //internal BitmapImage _mapImage;
        internal string _mapImageUrl;
        //internal string _legendUrl;

        public string MapImageUrl
        {
            get { return _mapImageUrl; }
            //set { _mapImageUrl = value; }
        }

        //public BitmapImage Image
        //{
        //    get { return _mapImage; }
        //    //set { _mapImage = value; }
        //}

        //public string LegendUrl
        //{
        //    get { return _legendUrl; }
        //    //set { _legendUrl = value; }
        //}

        public abstract List<GISLayerInfo> GetLayerInfo();
    }

    public abstract class GISTiledImageResponse : GISResponse
    {
        internal string _legendUrl;
        internal string _topLeftUrl;
        internal string _topRightUrl;
        internal string _bottomLeftUrl;
        internal string _bottomRightUrl;

        public string TopLeftUrl
        {
            get { return _topLeftUrl; }
        }

        public string TopRightUrl
        {
            get { return _topRightUrl; }
        }

        public string BottomLeftUrl
        {
            get { return _bottomLeftUrl; }
        }

        public string BottomRightUrl
        {
            get { return _bottomRightUrl; }
        }

        public string LegendUrl
        {
            get { return _legendUrl; }
        }

        public abstract List<GISLayerInfo> GetLayerInfo();
    }

    public class GISServiceResponse : GISFeatureResponse
    {
        internal GISServer _server;

        public GISServer Server
        {
            get { return _server; }
            set { _server = value; }
        }
    }

    public class GISFeatureResponse : GISResponse
    {
        internal List<GISFeature> _features = new List<GISFeature>();

        public string SearchTerm { get; set; }

        public List<GISFeature> Features
        {
            get { return _features; }
            set { _features = value; }
        }
    }
}
