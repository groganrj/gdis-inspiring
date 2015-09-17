using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;

namespace AtlasOf.GIS
{
    public enum LayerType
    {
        featureclass,
        image
    }

    public class GISLayer
    {
        internal string _id;
        internal bool _visible = true;
        internal LayerType _type;
        internal string _name;
        internal GISEnvelope _envelope;
        internal GISFeature _Features;

        public GISFeature Features
        {
            get { return _Features; }
            set { _Features = value; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int FeatureCount
        {
            get { return _Features.Fields.Count; }
        }

        public GISEnvelope Envelope
        {
            get { return _envelope; }
            set { _envelope = value; }
        }

        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        public LayerType LayerType
        {
            get { return _type; }
            set { _type = value; }
        }

        public GISLayer() { }

        public GISLayer(LayerType layerType, string layerName)
        {
            _type = layerType;
            _name = layerName;
        }

        public GISLayer(GISLayer currentLayer)
        {
            this.Visible = currentLayer.Visible;
            this.Id = currentLayer.Id;
            this.Name = currentLayer.Name;
            this.LayerType = currentLayer.LayerType;
            this._envelope = currentLayer._envelope;
            this._Features = currentLayer._Features;
        }
    }
}
