using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GMap.NET;
using GMap.NET.WindowsForms;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace MapOverlay
{
    public class MapOverlayPlugin : Plugin
    {
        string _Name = "MapOverlay";
        string _Version = "0.1";
        string _Author = "Stephen Dade";
        List<string> _Zones = new List<string>();

        public override string Name
        {
            get { return _Name; }
        }

        public override string Version
        {
            get { return _Version; }
        }

        public override string Author
        {
            get { return _Author; }
        }

        public override bool Exit()
        {
            return true;
        }

        public override bool Init()
        {
            return true;
        }

        public override bool Loaded()
        {
            MainV2.instance.Invoke((Action)
                delegate
                {

                    System.Windows.Forms.ToolStripMenuItem men = new System.Windows.Forms.ToolStripMenuItem() { Text = "Map Overlays" };

                    //add submenu from existing zones
                    string[] allDir = { Path.Combine(Directory.GetCurrentDirectory().ToString(), "CustomZones"), Path.Combine(Settings.GetUserDataDirectory().ToString(), "CustomZones") };
                    foreach (string topDir in allDir)
                    {
                        if (!Directory.Exists(topDir))
                            Directory.CreateDirectory(topDir);
                        string[] files = Directory.GetFiles(topDir, "*.txt", SearchOption.TopDirectoryOnly);
                        foreach (string file in files)
                        {
                            System.Windows.Forms.ToolStripMenuItem submen = new System.Windows.Forms.ToolStripMenuItem() { Text = Path.GetFileName(file), CheckOnClick=true };
                            submen.Click += (sender, e) => Submen_Click(sender, e, file);
                            men.DropDownItems.Add(submen);
                            _Zones.Add(file);
                        }
                    }
                    Host.FDMenuMap.Items.Add(men);

                });
            return true;
        }

        private void Submen_Click(object sender, EventArgs e, string file)
        {
            GMapOverlay overlay;
            if (Host.FDGMapControl.Overlays.Any(a => a.Id == "mapoverlayplugin"))
            {
                overlay = Host.FDGMapControl.Overlays.First(a => a.Id == "mapoverlayplugin");
            }
            else
            {
                overlay = new GMap.NET.WindowsForms.GMapOverlay("mapoverlayplugin");
                Host.FDGMapControl.Overlays.Add(overlay);
            }
            //if the zone already exists, remove it
            foreach (GMapPolygon zone in overlay.Polygons)
            {
                if (zone.Name == file)
                {
                    overlay.Polygons.Remove(zone);
                    return;
                }
            }

            //adding a zone to map
            if (File.Exists(file))
            {
                try
                {
                    string csvlines = File.ReadAllText(file);
                    List<PointLatLng> pts = new List<PointLatLng>();
                    foreach (string line in csvlines.Split('\n'))
                    {
                        if (line.Trim() != "")
                        {
                            float lat = float.Parse(line.Split(',')[0].Trim());
                            float lon = float.Parse(line.Split(',')[1].Trim());
                            PointLatLng pt = new PointLatLng(lat, lon);
                            pts.Add(pt);
                        }
                    }
                    //open file and add points
                    GMapPolygon zone = new GMapPolygon(pts, file);
                    zone.Fill = Brushes.Transparent;
                    zone.Stroke = new Pen(Color.Tan, 2);
                    zone.Name = file;
                    overlay.Polygons.Add(zone);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Unable to parse " + file + "\n" + ex.ToString());
                }

            }
        }

        public override bool Loop()
        {
            return true;
        }

    }
}
