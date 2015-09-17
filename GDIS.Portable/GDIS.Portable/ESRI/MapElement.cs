using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Collections.Specialized;

namespace AtlasOf.GIS
{
    public class MapElement
    {
        internal string _objectElement;
        internal List<MapElement> _innerElements = new List<MapElement>();
        internal Dictionary<string, string> _objectAttributes = new Dictionary<string, string>();

        public MapElement(string elementName)
        {
            _objectElement = elementName;
        }

        public MapElement() {}

        public string Name
        {
            get { return _objectElement; }
            set { _objectElement = value; }
        }

        public Dictionary<string, string> Attributes
        {
            get { return _objectAttributes; }
            set { _objectAttributes = value; }
        }

        public List<MapElement> InnerElements
        {
            get { return _innerElements; }
            set { _innerElements = value; }
        }

        public void AsXml(XmlWriter xtw)
        {
            xtw.WriteStartElement("", _objectElement, "");

            foreach (KeyValuePair<string, string> nameKey in _objectAttributes)
            {
                xtw.WriteAttributeString("", nameKey.Key, "", nameKey.Value);
            }

            if (_innerElements.Count > 0)
            {
                for (int i = 0; i < _innerElements.Count; i++)
                {
                    ((MapElement)_innerElements[i]).AsXml(xtw);
                }
            }

            xtw.WriteEndElement(); // _objectElement
        }
    }
}
