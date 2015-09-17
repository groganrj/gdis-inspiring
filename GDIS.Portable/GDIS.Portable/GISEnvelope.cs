using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;

namespace AtlasOf.GIS
{
    public enum Direction
    {
        North,
        East,
        West,
        South,
        NorthEast,
        NorthWest,
        SouthEast,
        SouthWest,
        Reset,
        All,
        None
    }

    public class GISPoint
    {
        public GISPoint() { }

        public GISPoint(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double ZoomLevel { get; set; }
        public double Longitude {get;set;}
        public double Latitude{get;set;}
    }

    public class GISEnvelope
    {
        internal double minX = -180;
        internal double maxX = 180;
        internal double minY = -90;
        internal double maxY = 90;
        private string _coordinateSystem = "EPSG:4326";

        public string CoordinateSystem
        {
            get { return _coordinateSystem; }
            set { _coordinateSystem = value; }
        }

        public double MinX
        {
            get { return minX; }
            set { minX = value; }
        }

        public double MaxX
        {
            get { return maxX; }
            set { maxX = value; }
        }

        public double MinY
        {
            get { return minY; }
            set { minY = value; }
        }

        public double MaxY
        {
            get { return maxY; }
            set { maxY = value; }
        }

        /// <summary>
        /// Longitude of extent
        /// </summary>
        public double CenterX
        {
            get
            {
                if (maxX > minX)
                {
                    return (maxX - minX) / 2 + minX;
                }
                else
                {
                    double wid = ((180 - maxX) - (180 + minX))/2;

                    if (wid > 180)
                    {
                        return 180 - wid;
                    }
                    else if (wid < -180)
                    {
                        return 180 - wid;
                    }
                    else return wid;
                }
            }
        }

        /// <summary>
        /// Latitude of extent
        /// </summary>
        public double CenterY
        {
            get
            {
                if (maxY > minY)
                {
                    return (maxY - minY) / 2 + minY;
                }
                else
                {
                    double wid = ((90 - maxY) - (90 + minY)) / 2;

                    if (wid > 90)
                    {
                        return 90 - wid;
                    }
                    else if (wid < -90)
                    {
                        return 90 - wid;
                    }
                    else return wid;
                }
            }
        }

        public double Width
        {
            get
            {
                if (maxX > minX)
                {
                    return maxX - minX;
                }
                else
                {
                    double wid = 360 + (180 + maxX) + (180 - minX);
                    return wid;
                }
            }
        }

        public double Height
        {
            get { return maxY - minY; }
        }

        public static GISEnvelope TheWorld
        {
            get { return new GISEnvelope() { _coordinateSystem = "EPSG:4326", maxX = 180, maxY = 90, minX = -180, minY = -90 }; }
        }

        public GISEnvelope() { }

        public GISEnvelope(double newMinX, double newMaxX, double newMinY, double newMaxY, string coordsys = "EPSG:4326")
        {
            maxX = newMaxX;
            maxY = newMaxY;
            minX = newMinX;
            minY = newMinY;
            _coordinateSystem = coordsys;
        }

        public GISEnvelope Copy()
        {
            return new GISEnvelope(minX, maxX, minY, maxY) { _coordinateSystem = this._coordinateSystem };
        }

        public GISEnvelope Pan(Direction panDirection, double panAmount)
        {
            //if (_coordinateSystem == "EPSG:4326") return PanLatLon(panDirection, panAmount);

            switch (panDirection)
            {
                case Direction.North:
                    maxY += panAmount;
                    minY += panAmount;
                    break;
                case Direction.South:
                    maxY -= panAmount;
                    minY -= panAmount;
                    break;
                case Direction.East:
                    maxX += panAmount;
                    minX += panAmount;
                    break;
                case Direction.West:
                    maxX -= panAmount;
                    minX -= panAmount;
                    break;
                case Direction.NorthEast:
                    maxX += panAmount;
                    minX += panAmount;
                    maxY += panAmount;
                    minY += panAmount;
                    break;
                case Direction.NorthWest:
                    maxX -= panAmount;
                    minX -= panAmount;
                    maxY += panAmount;
                    minY += panAmount;
                    break;
                case Direction.SouthEast:
                    maxY -= panAmount;
                    minY -= panAmount;
                    maxX += panAmount;
                    minX += panAmount;
                    break;
                case Direction.SouthWest:
                    maxX -= panAmount;
                    minX -= panAmount;
                    maxY -= panAmount;
                    minY -= panAmount;
                    break;
            }

            return this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            GISEnvelope env = (GISEnvelope)obj;
            return this.maxX == env.maxX && this.minX == env.minX && this.minY == env.minY && this.maxY == env.maxY;
        }

        private GISEnvelope PanLatLon(Direction panDirection, double panAmount)
        {
            double centerX = CenterX;
            double centerY = CenterY;

            switch (panDirection)
            {
                case Direction.North:
                    centerY += panAmount;
                    break;
                case Direction.South:
                    centerY -= panAmount;
                    break;
                case Direction.East:
                    centerX += panAmount;
                    break;
                case Direction.West:
                    centerX -= panAmount;
                    break;
                case Direction.NorthEast:
                    centerX += panAmount;
                    centerY += panAmount;
                    break;
                case Direction.NorthWest:
                    centerX -= panAmount;
                    centerY += panAmount;
                    break;
                case Direction.SouthEast:
                    centerY -= panAmount;
                    centerX += panAmount;
                    break;
                case Direction.SouthWest:
                    centerX -= panAmount;
                    centerY -= panAmount;
                    break;
            }

            // wrap around the earth
            if (centerX > 180) centerX = centerX - 180;
            if (centerX < -180) minX = centerX + 180;
            if (centerY < -90) centerY = centerY + 90;
            if (centerY > 90) centerY = centerY - 90;

            double width = Width / 2;
            double height = Height / 2;

            minY = centerY - height;
            maxY = centerY + height;
            maxX = centerX + width;
            minX = centerX - width;

            return this;
        }

        //public LOCATIONEXTENT ToExtent()
        //{
        //    LOCATIONEXTENT l = new LOCATIONEXTENT();
        //    l.maxx = maxX.ToString();
        //    l.maxy = maxY.ToString();
        //    l.minx = minX.ToString();
        //    l.miny = minY.ToString();
        //    l.coordsys = _coordinateSystem;
        //    return l;
        //}

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", minX, minY, maxX, maxY);
        }

        //public static GISEnvelope Create(LOCATIONEXTENT eNVELOPE)
        //{
        //    GISEnvelope e = new GISEnvelope();

        //    e.maxX = double.Parse(eNVELOPE.maxx.Replace(',', '.'));
        //    e.minX = double.Parse(eNVELOPE.minx.Replace(',', '.'));
        //    e.maxY = double.Parse(eNVELOPE.maxy.Replace(',', '.'));
        //    e.minY = double.Parse(eNVELOPE.miny.Replace(',', '.'));
        //    e._coordinateSystem = eNVELOPE.coordsys;
        //    return e;
        //}

        public GISEnvelope Zoom(double zoomFactor)
        {
            return Zoom(zoomFactor, CenterX, CenterY);
        }

        public GISEnvelope Zoom(double zoomFactor, double centerX, double centerY)
        {
            double widthFactor = (this.Width / zoomFactor) / 2;
            double heightFactor = (this.Height / zoomFactor) / 2;

            return new GISEnvelope(centerX - widthFactor, centerX + widthFactor, centerY - heightFactor, centerY + heightFactor) { _coordinateSystem = this._coordinateSystem };
        }

        internal static GISEnvelope Parse(string coordstring, string coordsys)
        {
            GISEnvelope e = new GISEnvelope();

            string[] coords = coordstring.Split(',');
            e.maxX = double.Parse(coords[3].Replace(',', '.'));
            e.minX = double.Parse(coords[1].Replace(',', '.'));
            e.maxY = double.Parse(coords[2].Replace(',', '.'));
            e.minY = double.Parse(coords[0].Replace(',', '.'));

            if (coordstring.Contains(":"))
            {
                e._coordinateSystem = coordsys;
            }
            else e._coordinateSystem = string.Format("epsg:{0}", coordsys);
            return e;
        }

        public bool ContainsPoint(double latitude, double longitude)
        {
            return (maxY > latitude && minY < latitude) && (maxX > longitude && minX < longitude);
        }

        public static GISEnvelope Create(double latitude, double longitude)
        {
            GISEnvelope envelope = new GISEnvelope();
            envelope.minY = latitude - Math.Abs(latitude * .001);
            envelope.minX = longitude - Math.Abs(longitude * .001);
            envelope.maxY = latitude + Math.Abs(latitude * .001);
            envelope.maxX = longitude + Math.Abs(longitude * .001);
            return envelope;
        }

        public string ToBBoxString()
        {
            //-128.6875,31.425,-111.8125,42.675
            return string.Format("{0},{1},{2},{3}", minX, minY, maxX, maxY);
        }
    }
}
