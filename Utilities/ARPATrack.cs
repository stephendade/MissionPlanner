using GMap.NET;
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

namespace MissionPlanner.Utilities
{
    class ARPATrack
    {
        public static List<GMapMarker> CreateContacts()
        {
            List<GMapMarker> contacts = new List<GMapMarker>();

            double lat = -38.41;
            double lon = 145.16;
            int heading = 0;
            string MMSI = "Contact1";
            double velocity = 1.5;

            GMapMarkerAISBoat marker = new GMapMarkerAISBoat(new PointLatLngAlt(lat, lon, 0), heading);
            //marker.Position = new PointLatLngAlt(item.lat / 1e7, item.lon / 1e7, 0);
            //marker.heading = item.heading / 100.0f;
            marker.ToolTipText = "MMSI: " + MMSI + "\n" +
                                 "Speed: " + (velocity / 100).ToString("0 m/s") + "\n";
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
            //marker.Tag = item;

            contacts.Add(marker);

            //add a 10m circle around the object
            contacts.Add(CreateCircle(lat, lon, 10));

            return contacts;

        }

        /// <summary>
        /// Create a single range marker overlay
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static RangeCircle CreateCircle(Double lat, Double lon, int radius)
        {
            PointLatLng point = new PointLatLng(lat, lon);

            RangeCircle rr = new RangeCircle(point, radius);

            return rr;
        }
    }

    [Serializable]
    public class RangeCircle : GMapMarker
    {
        public Pen Pen = new Pen(Brushes.Black, 2);

        public System.Drawing.Color Color
        {
            get { return Pen.Color; }
            set
            {
                if (!initcolor.HasValue) initcolor = value;
                Pen.Color = value;
            }
        }

        System.Drawing.Color? initcolor = null;

        public GMapMarker InnerMarker;

        // m
        public int wprad = 9000;

        public void ResetColor()
        {
            if (initcolor.HasValue)
                Color = initcolor.Value;
            else
                Color = Color.Black;
        }

        public RangeCircle(PointLatLng p, int radius)
            : base(p)
        {
            //Pen.DashStyle = DashStyle.Dash;

            wprad = radius;

            // do not forget set Size of the marker
            // if so, you shall have no event on it ;}
            Size = new System.Drawing.Size(50, 50);
            Offset = new System.Drawing.Point(-Size.Width / 2, -Size.Height / 2);

            //ToolTipText = "" + wprad.ToString() + "m";
            //ToolTipMode = MarkerTooltipMode.Always;
            //ToolTip.Font = new Font(FontFamily.GenericSansSerif, 8);
            //ToolTip.Stroke = new Pen(Brushes.Transparent, 0);


        }

        public override void OnRender(IGraphics g)
        {
            base.OnRender(g);

            if (wprad == 0 || Overlay.Control == null)
                return;

            // if we have drawn it, then keep that color
            if (!initcolor.HasValue)
                Color = Color.Red;

            //wprad = 300;

            // undo autochange in mouse over
            //if (Pen.Color == Color.Blue)
            //  Pen.Color = Color.White;

            double width =
                (Overlay.Control.MapProvider.Projection.GetDistance(Overlay.Control.FromLocalToLatLng(0, 0),
                    Overlay.Control.FromLocalToLatLng(Overlay.Control.Width, 0)) * 1000.0);
            double height =
                (Overlay.Control.MapProvider.Projection.GetDistance(Overlay.Control.FromLocalToLatLng(0, 0),
                    Overlay.Control.FromLocalToLatLng(Overlay.Control.Height, 0)) * 1000.0);
            double m2pixelwidth = Overlay.Control.Width / width;
            double m2pixelheight = Overlay.Control.Height / height;

            GPoint loc = new GPoint((int)(LocalPosition.X - (m2pixelwidth * wprad * 2)), LocalPosition.Y);
            // MainMap.FromLatLngToLocal(wpradposition);


            int x = LocalPosition.X - Offset.X - (int)(Math.Abs(loc.X - LocalPosition.X) / 2);
            int y = LocalPosition.Y - Offset.Y - (int)Math.Abs(loc.X - LocalPosition.X) / 2;
            int widtharc = (int)Math.Abs(loc.X - LocalPosition.X);
            int heightarc = (int)Math.Abs(loc.X - LocalPosition.X);

            //ToolTip.Offset = new Point((widtharc/2)-10, 0);

            //ToolTipPosition = LocalPosition + new Point(radius, 0);

            if (widtharc > 0 && widtharc < 200000000 && Overlay.Control.Zoom > 3)
            {
                g.DrawArc(Pen, new System.Drawing.Rectangle(x, y, widtharc, heightarc), 0, 360);

                //g.FillPie(new SolidBrush(Color.FromArgb(25, Color.Red)), x, y, widtharc, heightarc, 0, 360);
            }
        }
    }
}
