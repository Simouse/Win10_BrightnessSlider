using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Management; //add dll to reference
using Microsoft.Win32;

namespace Win10_BrightnessSlider
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //override form as toolwindow
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;

            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;

            var area = System.Windows.SystemParameters.WorkArea;
            this.Location = new Point((int)area.Right - this.Width, (int)area.Bottom - this.Height);
            eSetVis(false);


            // Colors
            // Initial bg color (Will be changed by reading system accent color)
            BackColor = Color.FromArgb(31, 31, 31);
            label1.ForeColor = Color.White;

            // form show hide event
            notifyIcon1.MouseUp += NotifyIcon1_MouseClick;
            //notifyIcon1.MouseClick += NotifyIcon1_MouseClick;

            // clicked outside of form
            Deactivate += Form1_Deactivate;


            CreateNotifyIConContexMenu();
            UpdateStatesOnGuiControls();
        }

        private void CreateNotifyIConContexMenu()
        {

            var cm = new ContextMenu();
            var mi1 = new MenuItem("Exit", (snd, ev) => {
                Application.Exit();
            });

            // For debugging
            var mi2 = new MenuItem("State Of Window", (snd, ev) => {
                var msg =
                "visible:" + this.Visible + "\r\n" +
                "Focused:" + this.Focused + "\r\n" +
                "canFocus:" + this.CanFocus + "\r\n";
                MessageBox.Show(msg);
            });

            var mi3 = new MenuItem("Run At Startup", (snd, ev) => {
                var _mi3 = snd as MenuItem;

                _mi3.Checked = !_mi3.Checked; // toggle

                SetStartup(_mi3.Checked);
            });

            var mi4 = new MenuItem("About - (based on v1.7.14)", (snd, ev) => {
                var msg = "Based on official v1.7.14 source code. " +
                          "(It seems that the released source code is not up to date while the binary file is. " +
                          "And I have no idea which actual version my fork is based on.)\r\n" +
                          "Original information is shown below:\r\n" +
                          "\r\n\r\n" +
                          "Developer: blackholeearth \r\n" +
                          "Official Site: \r\n" +
                          "https://github.com/blackholeearth/Win10_BrightnessSlider \r\n";
                MessageBox.Show(msg);
            });

            cm.MenuItems.Add(mi1);
            cm.MenuItems.Add(mi3);
            cm.MenuItems.Add(mi4);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                cm.MenuItems.Add(mi2);
            }

            notifyIcon1.ContextMenu = cm;
        }
        private void UpdateStatesOnGuiControls()
        {
            //get current states
            var isRunSttup = isRunAtStartup();
            notifyIcon1.ContextMenu.MenuItems
                .Cast<MenuItem>().Where( x=> x.Text == "Run At Startup").FirstOrDefault()
                .Checked = isRunSttup;

            var initBrig = GetBrightness();
            label1.Text = initBrig + "";
            trackBar1.Value = initBrig;
        }


        private bool vis = false;
        private DateTime visChangeTime = DateTime.UtcNow;

        public void eSetVis(bool visible)
        {
            // Prevent multiple entering events
            var now = DateTime.UtcNow;
            if (visChangeTime.AddMilliseconds(125.0) > now)
            {
                return;
            }
            visChangeTime = now;

            Console.WriteLine("[eSetVis] vis: " + vis + " -> " + !vis);

            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.Manual;
            
            var scrWA = Screen.PrimaryScreen.WorkingArea;
            var p = new Point(scrWA.Width , scrWA.Height);

            if (visible)
            {
                // Set background color to windows accent color when panel shows
                this.SetBgColorToWindowsTheme();

                p.Offset(-this.Width, -this.Height);
                this.Location = p;

                this.Activate();
                this.Show();
                this.BringToFront();

                vis = true;
            }
            else
            {
                p.Offset(this.Width, this.Height);
                this.Location = p;

                vis = false;
            }
        }
       
        private void Form1_Deactivate(object sender, EventArgs e)
        {
            eSetVis(false);
        }
        
        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (vis)
            {
                this.OnDeactivate(e);
            }
            else
            {
                this.eSetVis(true);
            }

            //notifyIcon1.MouseClick -= NotifyIcon1_MouseClick;
            //this.Deactivate -= Form1_Deactivate;

            

            //this.Deactivate += Form1_Deactivate;
            //notifyIcon1.MouseClick += NotifyIcon1_MouseClick;
        }

        private void SetBgColorToWindowsTheme()
        {
            BackColor = ThemeInfo.GetVariantThemeColor();
        }

        private double Clamp(double v, double lo, double hi)
        {
            if (v > hi) return hi;
            if (v < lo) return lo;
            return v;
        }

        private void SetBrightnessByMouseX(int x)
        {
            double v = ((double)x / (double)trackBar1.Width) * (trackBar1.Maximum - trackBar1.Minimum);
            int i = Convert.ToInt32(Clamp(v, trackBar1.Minimum, trackBar1.Maximum));
            byte b = Convert.ToByte(Clamp(v, 0.0, 100.0));
            trackBar1.Value = i;
            SetBrightness(b);
            label1.Text = b + "";
        }

        private void MouseDownHandler(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Left))
                SetBrightnessByMouseX(e.X);
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Left))
                SetBrightnessByMouseX(e.X);
        }

        static void SetBrightness(byte targetBrightness)
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightnessMethods");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection objectCollection = searcher.Get())
                {
                    foreach (ManagementObject mObj in objectCollection)
                    {
                        mObj.InvokeMethod("WmiSetBrightness",
                            new Object[] { UInt32.MaxValue, targetBrightness });
                        break;
                    }
                }
            }
        }
        static int GetBrightness()
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightness");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection objectCollection = searcher.Get())
                {
                    foreach (ManagementObject mObj in objectCollection)
                    {
                        var br_obj = mObj.Properties["CurrentBrightness"].Value;

                        int br = 0;
                        int.TryParse(br_obj+"", out br);
                        return br;
                    }
                }
            }
            return 0;

        }

        private void SetStartup( bool RunAtStartup )
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (RunAtStartup)
                rk.SetValue(  Application.ProductName, Application.ExecutablePath );
            else
                rk.DeleteValue(Application.ProductName , false);

        }
        private bool isRunAtStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            var  val  = rk.GetValue(Application.ProductName );

            if (val+"" == Application.ExecutablePath  )
                return true;
            else
                return false;

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

    }


    
}





 