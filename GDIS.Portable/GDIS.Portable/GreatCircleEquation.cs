using System;
using System.Net;
using AtlasOf.GIS;

namespace AtlasOf.GIS
{
    public class GreatCircleEquation
    {
        class ellipsoid
        {
            public string name;
            public double a;
            public double invf;
        }

        public class crsd
        {
            public double d;
            public double crs21;
            public double crs12;
        }

        public class point
        {
            public double lat;
            public double lon;
            public double c;
        }

        public static GISEnvelope Calculate(double centerX, double centerY, double width, double height)
        {
            double widthfactor = 1.0;
            GISEnvelope env = new GISEnvelope();

            do
            {
                env.minY = GetCoordinate(centerX, centerY, 180, height / widthfactor).lat;
                env.maxY = GetCoordinate(centerX, centerY, 0, height / widthfactor).lat;
                env.maxX = GetCoordinate(centerX, centerY, 270, width / widthfactor).lon;
                env.minX = GetCoordinate(centerX, centerY, 90, width / widthfactor).lon;

                widthfactor += 1.0;
            }
            while (((int)env.CenterX != (int)centerX || (int)env.CenterY != (int)centerY || env.minY > env.maxY) && widthfactor < 6);

            if (env.minX > env.maxX)
            {
                env.maxX = -env.maxX;
                env.minX = -env.minX;
            }

            return env;
        }

        private static double mod(double x, double y)
        {
            return x - y * Math.Floor(x / y);
        }

        private static double modlon(double x)
        {
            return mod(x + Math.PI, 2 * Math.PI) - Math.PI;
        }

        private static double modcrs(double x)
        {
            return mod(x, 2 * Math.PI);
        }

        private static double modlat(double x)
        {
            return mod(x + Math.PI / 2, 2 * Math.PI) - Math.PI / 2;
        }

        private static string degtodm(double deg, int decplaces)
        {
            var deg1 = Math.Floor(deg);
            var min = 60.0 * (deg - Math.Floor(deg));
            var mins = Math.Round(min, decplaces);

            if (mins > 59.0)
            {
                deg1 += 1;
                mins = Math.Round(0.0, decplaces);
            }
            return deg1 + ":" + mins;
        }

        private static point direct(double lat1, double lon1, double crs12, double d12)
        {
            double EPS = 0.00000000005;
            double dlon, lat, lon;

            // 5/16 changed to "long-range" algorithm
            if ((Math.Abs(Math.Cos(lat1)) < EPS) && !(Math.Abs(Math.Sin(crs12)) < EPS))
            {
                //alert("Only N-S courses are meaningful, starting at a pole!")
            }

            lat = Math.Asin(Math.Sin(lat1) * Math.Cos(d12) +
                          Math.Cos(lat1) * Math.Sin(d12) * Math.Cos(crs12));
            if (Math.Abs(Math.Cos(lat)) < EPS)
            {
                lon = 0.0; //endpoint a pole
            }
            else
            {
                dlon = Math.Atan2(Math.Sin(crs12) * Math.Sin(d12) * Math.Cos(lat1),
                              Math.Cos(d12) - Math.Sin(lat1) * Math.Sin(lat));
                lon = mod(lon1 - dlon + Math.PI, 2 * Math.PI) - Math.PI;
            }

            point retval = new point() { lat = lat, lon = lon, c = 0.0 };

            return retval;
        }

        private static point direct_ell(double glat1, double glon1, double faz, double s, ellipsoid ellipse)
        {
            // glat1 initial geodetic latitude in radians N positive 
            // glon1 initial geodetic longitude in radians E positive 
            // faz forward azimuth in radians
            // s distance in units of a (=nm)

            double EPS = 0.00000000005;
            double r, tu, sf, cf, b, cu, su, sa, c2a, x, c, d, y, sy = 0.0, cy = 0.0, cz = 0.0, e = 0.0;
            double glat2, glon2, baz, f;

            if ((Math.Abs(Math.Cos(glat1)) < EPS) && !(Math.Abs(Math.Sin(faz)) < EPS))
            {
                //  alert("Only N-S courses are meaningful, starting at a pole!")
            }

            double a = ellipse.a;
            f = 1.0 / ellipse.invf;
            r = 1.0 - f;
            tu = r * Math.Tan(glat1);
            sf = Math.Sin(faz);
            cf = Math.Cos(faz);
            if (cf == 0.0)
            {
                b = 0.0;
            }
            else
            {
                b = 2.0 * Math.Atan2(tu, cf);
            }
            cu = 1.0 / Math.Sqrt(1 + tu * tu);
            su = tu * cu;
            sa = cu * sf;
            c2a = 1.0 - sa * sa;
            x = 1.0 + Math.Sqrt(1.0 + c2a * (1.0 / (r * r) - 1.0));
            x = (x - 2.0) / x;
            c = 1.0 - x;
            c = (x * x / 4.0 + 1.0) / c;
            d = (0.375 * x * x - 1.0) * x;
            tu = s / (r * a * c);
            y = tu;
            c = y + 1;

            while (Math.Abs(y - c) > EPS)
            {
                sy = Math.Sin(y);
                cy = Math.Cos(y);
                cz = Math.Cos(b + y);
                e = 2.0 * cz * cz - 1.0;
                c = y;
                x = e * cy;
                y = e + e - 1.0;
                y = (((sy * sy * 4.0 - 3.0) * y * cz * d / 6.0 + x) * d / 4.0 - cz) * sy * d + tu;
            }

            b = cu * cy * cf - su * sy;
            c = r * Math.Sqrt(sa * sa + b * b);
            d = su * cy + cu * sy * cf;
            glat2 = modlat(Math.Atan2(d, c));
            c = cu * cy - su * sy * cf;
            x = atan2(sy * sf, c);
            c = ((-3.0 * c2a + 4.0) * f + 4.0) * c2a * f / 16.0;
            d = ((e * cy * c + cz) * sy * c + y) * sa;
            glon2 = modlon(glon1 + x - (1.0 - c) * d * f);	// fix date line problems 
            baz = modcrs(Math.Atan2(sa, b) + Math.PI);

            point retval = new point() { lat = glat2, lon = glon2, c = baz };
            return retval;
        }

        static double atan2(double y, double x)
        {
            double retval = 0.0;
            if (x < 0) { retval = Math.Atan(y / x) + Math.PI; }
            if ((x > 0) && (y >= 0)) { retval = Math.Atan(y / x); }
            if ((x > 0) && (y < 0)) { retval = Math.Atan(y / x) + 2.0 * Math.PI; }
            if ((x == 0) && (y > 0)) { retval = Math.PI / 2.0; }
            if ((x == 0) && (y < 0)) { retval = 3.0 * Math.PI / 2.0; }
            if ((x == 0) && (y == 0))
            {
                return 0.0;
            }
            return retval;
        }

        private static ellipsoid getEllipsoid(int selection, double? majorradius, double? inverse_f)
        {
            int no_selections = 9;
            ellipsoid[] ells = new ellipsoid[10];
            ells[0] = new ellipsoid() { name = "Sphere", a = 180 * 60 / Math.PI, invf = double.PositiveInfinity };
            ells[1] = new ellipsoid() { name = "WGS84", a = 6378.137 / 1.852, invf = 298.257223563 };
            ells[2] = new ellipsoid() { name = "NAD27", a = 6378.2064 / 1.852, invf = 294.9786982138 };
            ells[3] = new ellipsoid() { name = "International", a = 6378.388 / 1.852, invf = 297.0 };
            ells[4] = new ellipsoid() { name = "Krasovsky", a = 6378.245 / 1.852, invf = 298.3 };
            ells[5] = new ellipsoid() { name = "Bessel", a = 6377.397155 / 1.852, invf = 299.1528 };
            ells[6] = new ellipsoid() { name = "WGS72", a = 6378.135 / 1.852, invf = 298.26 };
            ells[7] = new ellipsoid() { name = "WGS66", a = 6378.145 / 1.852, invf = 298.25 };
            ells[8] = new ellipsoid() { name = "FAI sphere", a = 6371.0 / 1.852, invf = 1000000000.0 };
            ells[9] = new ellipsoid() { name = "User", a = 0.0, invf = 0.0 };  // last one!

            if (selection == no_selections)
            { // user defined
                ells[no_selections].name = "User";
                ells[no_selections].a = majorradius.Value / 1.852;
                ells[no_selections].invf = inverse_f.Value;
                if (ells[no_selections].invf == double.PositiveInfinity)
                {
                    ells[no_selections].invf = 1000000000.0;
                }
            }

            return ells[selection];
        }

        static double dconv(int selection)
        {
            double[] dc = new double[4];
            dc[0] = 1.0;
            dc[1] = 1.852; //km
            dc[2] = 185200.0 / 160934.40; // 1.150779448 sm
            dc[3] = 185200.0 / 30.48; // 6076.11549  //ft
            return dc[selection];
        }

        public static point GetCoordinate(double centerX, double centerY, double heading, double distance)
        {
            double dc, lat1, lon1, lat2, lon2;
            double d12, crs12;

            lat1 = (Math.PI / 180.0) * centerY;
            lon1 = (Math.PI / 180.0) * centerX;

            d12 = distance;
            dc = dconv(1); /* get distance conversion factor */
            d12 /= dc;  // in nm

            crs12 = heading * Math.PI / 180.0;  // radians

            ellipsoid ellipse = getEllipsoid(1, null, null); //get ellipse
            //showProps(ellipse,"ellipse")

            if (ellipse.name == "Sphere")
            {
                // spherical code
                d12 /= (180.0 * 60.0 / Math.PI);  // in radians
                point cd = direct(lat1, lon1, crs12, d12);
                lat2 = cd.lat * (180.0 / Math.PI);
                lon2 = cd.lon * (180.0 / Math.PI);
            }
            else
            {
                // elliptic code
                point cde = direct_ell(lat1, -lon1, crs12, d12, ellipse);  // ellipse uses East negative
                lat2 = cde.lat * (180.0 / Math.PI);
                lon2 = cde.lon * (180.0 / Math.PI);                  // ellipse uses East negative
            }

            return new point() { lat = lat2, lon = lon2 };
        }


        public static double GetDistanceBetweenCoordinates(double lon1, double lat1, double lon2, double lat2)
        {
            double d, dc, crs12, crs21;

            lat1 = (Math.PI / 180.0) * lat1;
            lat2 = (Math.PI / 180.0) * lat2;
            lon1 = (Math.PI / 180.0) * lon1;
            lon2 = (Math.PI / 180.0) * lon2;

            dc = dconv(1);

            ellipsoid ellipse = getEllipsoid(1, null, null); //get ellipse

            if (ellipse.name == "Sphere")
            {
                // spherical code
                crsd cd = crsdist(lat1, lon1, lat2, lon2); // compute crs and distance 
                crs12 = cd.crs12 * (180.0 / Math.PI);
                crs21 = cd.crs21 * (180.0 / Math.PI);
                d = cd.d * (180 / Math.PI) * 60 * dc;  // go to physical units  
            }
            else
            {
                // elliptic code
                crsd cde = crsdist_ell(lat1, lon1, lat2, lon2, ellipse);  // ellipse uses East negative
                crs12 = cde.crs12 * (180 / Math.PI);
                crs21 = cde.crs21 * (180 / Math.PI);
                d = cde.d * dc;  // go to physical units
            }
            double emp12 = crs12;
            double emp21 = crs21;
            return d;
        }

        static crsd crsdist(double lat1, double lon1, double lat2, double lon2)
        { // radian args
            /* compute course and distance (spherical) */

            if ((lat1 + lat2 == 0.0) && (Math.Abs(lon1 - lon2) == Math.PI)
                                && (Math.Abs(lat1) != (Math.PI / 180) * 90.0))
            {
                //alert("Course between antipodal points is undefined")
            }

            double crs12, crs21, argacos;
            double d = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

            if ((d == 0.0) || (lat1 == -(Math.PI / 180.0) * 90.0))
            {
                crs12 = 2 * Math.PI;
            }
            else if (lat1 == (Math.PI / 180.0) * 90.0)
            {
                crs12 = Math.PI;
            }
            else
            {
                argacos = (Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(d)) / (Math.Sin(d) * Math.Cos(lat1));
                if (Math.Sin(lon2 - lon1) < 0)
                {
                    crs12 = Math.Acos(argacos);
                }
                else
                {
                    crs12 = 2 * Math.PI - Math.Acos(argacos);
                }
            }
            if ((d == 0.0) || (lat2 == -(Math.PI / 180.0) * 90.0))
            {
                crs21 = 0.0;
            }
            else if (lat2 == (Math.PI / 180.0) * 90.0)
            {
                crs21 = Math.PI;
            }
            else
            {
                argacos = (Math.Sin(lat1) - Math.Sin(lat2) * Math.Cos(d)) / (Math.Sin(d) * Math.Cos(lat2));

                if (Math.Sin(lon1 - lon2) < 0)
                {
                    crs21 = Math.Acos(argacos);
                }
                else
                {
                    crs21 = 2 * Math.PI - Math.Acos(argacos);
                }
            }

            return new crsd() { d = d, crs12 = crs12, crs21 = crs21 };
        }

        static crsd crsdist_ell(double glat1, double glon1, double glat2, double glon2, ellipsoid ellipse)
        {
            // glat1 initial geodetic latitude in radians N positive 
            // glon1 initial geodetic longitude in radians E positive 
            // glat2 final geodetic latitude in radians N positive 
            // glon2 final geodetic longitude in radians E positive 
            double a = ellipse.a;
            double f = 1 / ellipse.invf;

            double r, tu1, tu2, cu1, su1, cu2, s1, b1, f1;
            double x, sx = 0.0, cx = 0.0, sy = 0.0, cy = 0.0, y = 0.0, sa, c2a = 0.0, cz = 0.0, e = 0.0, c, d;
            double EPS = 0.00000000005;
            double faz, baz, s;
            double iter = 1.0;
            double MAXITER = 100.0;

            if ((glat1 + glat2 == 0.0) && (Math.Abs(glon1 - glon2) == Math.PI))
            {
                glat1 = glat1 + 0.00001; // allow algorithm to complete
            }
            if (glat1 == glat2 && (glon1 == glon2 || Math.Abs(Math.Abs(glon1 - glon2) - 2 * Math.PI) < EPS))
            {
                return new crsd();
            }

            r = 1 - f;
            tu1 = r * Math.Tan(glat1);
            tu2 = r * Math.Tan(glat2);
            cu1 = 1.0 / Math.Sqrt(1.0 + tu1 * tu1);
            su1 = cu1 * tu1;
            cu2 = 1.0 / Math.Sqrt(1.0 + tu2 * tu2);
            s1 = cu1 * cu2;
            b1 = s1 * tu2;
            f1 = b1 * tu1;
            x = glon2 - glon1;
            d = x + 1; // force one pass

            while ((Math.Abs(d - x) > EPS) && (iter < MAXITER))
            {
                iter = iter + 1;
                sx = Math.Sin(x);
                cx = Math.Cos(x);
                tu1 = cu2 * sx;
                tu2 = b1 - su1 * cu2 * cx;
                sy = Math.Sqrt(tu1 * tu1 + tu2 * tu2);
                cy = s1 * cx + f1;
                y = Math.Atan2(sy, cy);
                sa = s1 * sx / sy;
                c2a = 1 - sa * sa;
                cz = f1 + f1;
                if (c2a > 0.0)
                    cz = cy - cz / c2a;
                e = cz * cz * 2.0 - 1.0;
                c = ((-3.0 * c2a + 4.0) * f + 4.0) * c2a * f / 16.0;
                d = x;
                x = ((e * cy * c + cz) * sy * c + y) * sa;
                x = (1.0 - c) * x * f + glon2 - glon1;
            }

            faz = modcrs(Math.Atan2(tu1, tu2));
            baz = modcrs(Math.Atan2(cu1 * sx, b1 * cx - su1 * cu2) + Math.PI);
            x = Math.Sqrt((1.0 / (r * r) - 1.0) * c2a + 1.0);
            x += 1;
            x = (x - 2.0) / x;
            c = 1.0 - x;
            c = (x * x / 4.0 + 1.0) / c;
            d = (0.375 * x * x - 1.0) * x;
            x = e * cy;
            s = ((((sy * sy * 4.0 - 3.0) * (1.0 - e - e) * cz * d / 6.0 - x) * d / 4.0 + cz) * sy * d + y) * c * a * r;

            return new crsd() { d = s, crs12 = faz, crs21 = baz };
        }
    }
}
