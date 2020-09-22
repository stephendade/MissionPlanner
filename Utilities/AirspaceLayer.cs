using DirectShowLib.BDA;
using GMap.NET;
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using SixLabors.Fonts.Exceptions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// Displays a layer of Australian airspace zones
    /// Data from http://xcaustralia.org/aircheck/aircheck.php
    /// </summary>
    class AirspaceLayer
    {
        public List<GMapPolygon> zones = new List<GMapPolygon>();
        string inFile = "zones.txt";

        public AirspaceLayer()
        {
            //open the file aand process data
            try
            {
                using (StreamReader sr = File.OpenText(inFile))
                {
                    string line = "";
                    string zoneName = "";
                    // Read the text
                    while ((line = sr.ReadLine()) != null)
                    {
                        // ignore comments
                        if(!line.StartsWith("//"))
                        {
                            //new zone
                            if (line.StartsWith("+") && zoneName == "")
                            {
                                zoneName = line.Trim().Substring(1);
                            }
                            // latlon points for the above zone
                            else if(line.StartsWith("(") && zoneName != "")
                            {
                                // parse the points in the zone
                                string[] points = line.Substring(1, line.Length-2).Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
                                List <PointLatLng> zone = new List<PointLatLng>();
                                foreach (string point in points)
                                {   
                                    float lat = float.Parse(point.Split(',')[0]);
                                    float lon = float.Parse(point.Split(',')[1]);
                                    zone.Add(new PointLatLng(lat, lon));
                                }
                                GMapPolygon newZone = new GMapPolygon(zone, zoneName);
                                // and format zone
                                newZone.Fill = Brushes.Transparent;
                                newZone.Stroke = new Pen(Color.Tan, 2);
                                zones.Add(newZone);
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    "Error loading layer:" + ex.StackTrace,
                    "Airspace Layer", CustomMessageBox.MessageBoxButtons.OK);
            }


        }
    }
}
