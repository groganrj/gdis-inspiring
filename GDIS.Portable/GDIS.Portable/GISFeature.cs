using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;

namespace AtlasOf.GIS
{
    public class GISField
    {
        internal string _fieldName;
        internal string _size;
        internal string _precision;
        internal string _type = string.Empty;
        internal string _fieldValue = string.Empty;
        internal string _fullFieldName = string.Empty;
        internal SPECIAL_FIELD _specialField = SPECIAL_FIELD.NONE;

        public SPECIAL_FIELD SpecialField
        {
            get { return _specialField; }
            set { _specialField = value; }
        }

        public string Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public string Precision
        {
            get { return _precision; }
            set { _precision = value; }
        }

        public string Type
        {
            get { return _type; }
        }

        public string FieldName
        {
            get { return _fieldName; }
        }

        public string DisplayName
        {
            get { return _fullFieldName; }
            set { _fullFieldName = value; }
        }

        public string FieldValue
        {
            get { return _fieldValue; }
            set { _fieldValue = value; }
        }

        public GISField() { }

        public GISField(string name)
        {
            _fullFieldName = _fieldName = name;

            if (_fieldName.IndexOf('.') != -1)
            {
                string[] s = _fieldName.Split('.');
                _fieldName = s[s.Length - 1];
            }
        }

        public GISField(string name, string fValue)
            : this(name)
        {
            _fieldValue = fValue;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _fullFieldName, _fieldValue);
        }

        public enum SPECIAL_FIELD
        {
            minx,
            maxx,
            miny,
            maxy,
            latitude,
            longitude,
            NONE
        }
    }

    public class GISFeature
    {
        internal GISEnvelope _Envelope;
        internal List<GISField> _Fields = new List<GISField>();

        public GISEnvelope Envelope
        {
            get { return _Envelope; }
            set { _Envelope = value; }
        }

        public List<GISField> Fields
        {
            get { return _Fields; }
            set { _Fields = value; }
        }

        public GISField GetField(string fieldName)
        {
            foreach (GISField field in _Fields)
            {
                if (string.Compare(field._fieldName, fieldName) == 0) return field;
            }

            return null;
        }
    }
}
