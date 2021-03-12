using GMap.NET;
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;
//using Settings = Properties.Settings;

/*
 * $PKHUN,2, 12,??, 1234,000.00,000.0, 16.0, 128.6,00.0,000.0,05029.78N,00224.96W,08:30:31,S,T,W,*00<CR><LF>
 * Meaning: OPS NTD ARPA local track 12, labelled as block track "1234" is positioned at 50 degrees 29.78
 * minutes North, 2 degrees 24.96 minutes West at time 8:30:31 with tme speed 16.0 knots, tme course 128.6.
 * Position is not from a GPS, target is being tracked and the full track table data is being output.
 * 
 *0 $PKHUN - start of message
 *1 X - radar source, 3-4
 *2 XXXX - local track number, 1-350
 *3 XX - target identity, first char F/H/N/U second char B/S/A/Y
 *4 XXXXXXXXXX - target label
 *5 XXX.XX - target range (nm)
 *6 XXX.X - target bearing (deg)
 *7 XXXX.X - target true speed (kts)
 *8 XXX.X - target true course (deg)
 *9 XXX.X - target closest point of approach (min)
 *10 DDDMM.SSX - latitude, degrees, minutes, 100ths of minutes, N or S
 *11 DDDMM.SSX - longitude, degrees, minutes, 100ths of minutes, E or W
 *12 HH:MM:SS - time of extraction
 *13 X - GPS position source N/S
 *14 X - target status L/T
 *15 X - content of target data W/U/S
 *16 *00<CR><LF> - end of message
 */

namespace MissionPlanner.Utilities
{
    struct contact
    {
        public GMapMarker vehicle;
        public GMapMarker rangeOverlay;
        public string label;
    }

    class ARPATrack
    {
        ConcurrentDictionary<string, contact> contacts;
        private static System.Timers.Timer aTimer;
        Random random = new Random();
        private string bufferstr;
        private const string logfile = "ARPALog.txt";
        private const string datafile = "WinningARPAExample.txt";
        private List<string> datafileRead = new List<string>();
        private SerialPort _serialPort;
        private Thread readThread;
        bool _continue = true;

        public ARPATrack()
        {
            contacts = new ConcurrentDictionary<string, contact>();

            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.AutoReset = true;
            //aTimer.Enabled = true;

            try
            {
                _serialPort = new SerialPort(MissionPlanner.Properties.Settings.Default.port, MissionPlanner.Properties.Settings.Default.baudrate);
                _serialPort.Open();
                readThread = new Thread(Read);
                readThread.Start();
            }
            catch (Exception e)
            {
                //try reading a text file instead
                if (File.Exists(datafile))
                {
                    //datafileRead = File.ReadAllText(datafile);
                    using (var reader = new StreamReader(datafile))
                        while (reader.Peek() >= 0)
                            datafileRead.Add(reader.ReadLine() + "\r\n"); //need to re-add newlines, as readline removes them
                    CustomMessageBox.Show("Using Datafile for ARPA Radar: " + datafileRead.Count, "ARPA Info");
                }
                else
                {
                    //shut down the read thread
                    CustomMessageBox.Show(e.ToString(), "ARPA Error");
                    CustomMessageBox.Show("Using Simulated Data for ARPA Radar", "ARPA Error");
                }

                _continue = false;
                if (readThread != null && readThread.IsAlive)
                {
                    readThread.Join();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                
                aTimer.Enabled = true;
            }


        }

        ~ARPATrack()
        {
            //shut down the read thread
            _continue = false;
            if (readThread != null && readThread.IsAlive)
            {
                readThread.Abort();
            }
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        private void ATimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (datafileRead.Count > 0)
            {
                //read next line. Remember to add in newlines!
                try
                {
                    processNewText(datafileRead[0]);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(ex.ToString());
                }

                // and send to back
                datafileRead.Add(datafileRead[0]);
                datafileRead.RemoveAt(0);
                //CustomMessageBox.Show("Timer: " + datafileRead[0]);

            }
            else
            {
                //create a test track
                double latDDDMM = 3800 + random.Next(0, 60).ConvertToDouble() + (random.Next(0, 100).ConvertToDouble() / 10000.0);
                double lonDDDMM = 14500 + random.Next(0, 60).ConvertToDouble() + (random.Next(0, 100).ConvertToDouble() / 10000.0);
                int heading = random.Next(0, 360);
                string MMSI = "Contact" + random.Next(0, 10).ToString();
                double velocity = random.Next(0, 20);

                //toSend = "$PKHUN,2,12,??,1234,000.00,000.0,16.0,128.6,00.0,000.0,05029.78N,00224.96W,08:30:31,S,T,W,*00\r\n"; DDDMM.SSX
                string toSend = "$PKHUN,2,12,??," + MMSI + ",000.00,000.0," + velocity.ToString("F1") + "," + heading.ToString("F1") + ",00.0,000.0,0" + latDDDMM.ToString("F2") + "S," + lonDDDMM.ToString("F2") + "E,08:30:31,S,T,W,*00\r\n";

                processNewText(toSend);
            }

        }

        public void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadExisting();
                    processNewText(message);
                }
                catch (TimeoutException) { }
            }
        }

        private void processNewText(string toSend)
        {
            //first log it all
            if (!File.Exists(logfile))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(logfile))
                {
                    sw.WriteLine("");
                }
            }
            using (StreamWriter sw = File.AppendText(logfile))
            {
                sw.Write(toSend);
            }

            bufferstr += toSend;

            parseARPAStringbuffer();
            /*if (newContact.rangeOverlay != null && newContact.vehicle != null)
            {
                contacts[newContact.label] = newContact;
            } */
        }

        public void parseARPAStringbuffer()
        {
            contact retContact = new contact();
            //search bufferstr for start and end
            int startindex = bufferstr.IndexOf("$PKHUN");
            if (startindex == -1)
            {
                //retContact.rangeOverlay = null;
                //retContact.vehicle = null;
                //return retContact;
                return;
            }
            int endindex = bufferstr.IndexOf("*00\r\n", startindex);
            //CustomMessageBox.Show("Buffer: " + bufferstr);
            

            //check if we actually have a string
            if (endindex == -1)
            {
                //retContact.rangeOverlay = null;
                //retContact.vehicle = null;
                //return retContact;
                return;
            }

            //CustomMessageBox.Show("ARPA HERE2a");

            //get substring
            string arpastring = bufferstr.Substring(startindex, endindex - startindex);
            //CustomMessageBox.Show("ARPA HERE2b: " + bufferstr.Length);
            //discard everything before AND the substring itself
            bufferstr = bufferstr.Remove(0, endindex + 1);
            //CustomMessageBox.Show("ARPA HERE2c");
            //and parse
            string[] fields = arpastring.Split(',');
            //CustomMessageBox.Show(fields.Length.ToString());
            // check we have the correct number of fields
            if (fields.Length != 18)
            {
                //retContact.rangeOverlay = null;
                //retContact.vehicle = null;
                //return retContact;
                return;
            }

            //CustomMessageBox.Show("ARPA HERE3");

            //interpret fields
            //DDDMM.SSX - latitude, degrees, minutes, 100ths of minutes, N or S
            //CustomMessageBox.Show(fields[11]);
            double lat = double.Parse(fields[11].Substring(0, 3)) + (double.Parse(fields[11].Substring(3, 2))/60.0) + (double.Parse(fields[11].Substring(6, 2)) / 6000.0);
            //CustomMessageBox.Show(fields[11].ToString() + " = " + lat.ToString());
            double lon = double.Parse(fields[12].Substring(0, 3)) + (double.Parse(fields[12].Substring(3, 2)) / 60.0) + (double.Parse(fields[12].Substring(6, 2)) / 6000.0);
            //CustomMessageBox.Show(fields[12].ToString() + " = " + lon.ToString());
            //CustomMessageBox.Show(fields[9]);
            float bearing = float.Parse(fields[8]);
            //CustomMessageBox.Show(fields[8]);
            float velocity = float.Parse(fields[7]);
            //CustomMessageBox.Show(fields[8]);
            string label = fields[4];
            if (fields[11][8] == 'S')
            {
                lat = -lat;
            }
            if (fields[12][8] == 'W')
            {
                lon = -lon;
            }

            //CustomMessageBox.Show("ARPA HERE4");

            GMapMarkerAISBoat marker = new GMapMarkerAISBoat(new PointLatLngAlt(lat, lon, 0), bearing);
            marker.ToolTipText = "Label: " + label + "\n" +
                     "Speed: " + velocity.ToString("0 kts") + "\n" +
                     "Bearing: " + bearing.ToString("0 deg") + "\n";
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;

            retContact.vehicle = marker;
            retContact.rangeOverlay = CreateCircle(lat, lon, 10);
            retContact.label = label;
            //CustomMessageBox.Show("ARPA " + label + " with pos " + fields[11] + ", " + fields[12] + " -> " + lat.ToString() + ", " + lon.ToString());
            //return retContact;
            //add contact in
            contacts[retContact.label] = retContact;
        }

        public List<GMapMarker> UpdateContacts()
        {
            List<GMapMarker> retlist = new List<GMapMarker>();
            foreach (var item in contacts)
            {
                retlist.Add(item.Value.vehicle);
                retlist.Add(item.Value.rangeOverlay);
            }

            return retlist;
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
                Color = System.Drawing.Color.Black;
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
                Color = System.Drawing.Color.Red;

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
