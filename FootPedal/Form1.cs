#define SINGLE_THREADED_WAITHANDLE_APPROACH
#define SHOW_CHANGES_ONLY

using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using SharpDX.DirectInput;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;
using HidSharp;
using System.Linq;
using System.Diagnostics;

namespace FootPedal
{
    public partial class Form1 : Form
    {
        int lastPressed = 0;

        

        Timer timer1 = new Timer();
        // import the function in your class
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);



        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;

            // Associate the event-handling method with the
            // KeyDown event.
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);

            new System.Threading.Thread(new System.Threading.ThreadStart(isUsbPedal)).Start();
            new System.Threading.Thread(new System.Threading.ThreadStart(loop)).Start();
         //   isUsbPedal();
         
        //    loop();
        }


        
        private void isUsbPedal()
        {
            int pedalIndex = -1;
            var list = DeviceList.Local;
            list.Changed += (sender, e) => Console.WriteLine("Device list changed.");



            var stopwatch = Stopwatch.StartNew();
            var hidDeviceList = list.GetHidDevices().ToArray();

            // Console.WriteLine("Complete device list (took {0} ms to get {1} devices):", stopwatch.ElapsedMilliseconds, hidDeviceList.Length);
            // FIND FOOT PEDAL
            for (int lp = 0; lp < hidDeviceList.Length; lp++)
            {
                if (hidDeviceList[lp].GetProductName().Contains("Pedal") || hidDeviceList[lp].GetProductName().Contains("pedal"))
                {
                    pedalIndex = lp;
                    // Console.WriteLine("Found "+ hidDeviceList[lp].GetProductName()+" on index: "+lp);
                }
            }

            if (pedalIndex >= 0)
            {
                HidDevice dev = hidDeviceList[pedalIndex];
                CheckHIDInput(dev);
            }
        }


        static void WriteDeviceItemInputParserResult(HidSharp.Reports.Input.DeviceItemInputParser parser)
        {
#if SHOW_CHANGES_ONLY
            while (parser.HasChanged)
            {
                int changedIndex = parser.GetNextChangedIndex();
                var previousDataValue = parser.GetPreviousValue(changedIndex);
                var dataValue = parser.GetValue(changedIndex);

                if (dataValue.GetPhysicalValue() == 1) { 
                    Console.WriteLine("Button pressed: "+ (Usage)dataValue.Usages.FirstOrDefault()); 
                    SendKeys.SendWait("{+}");
                }

            }
#else
            if (parser.HasChanged)
            {
                int valueCount = parser.ValueCount;

                for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    var dataValue = parser.GetValue(valueIndex);
                    //  Console.Write(string.Format("  {0}: {1}", (Usage)dataValue.Usages.FirstOrDefault(), dataValue.GetPhysicalValue()));
                }

                Console.WriteLine();
            }
#endif
        }


        
        private void CheckHIDInput(HidDevice dev)
        {
           
            {
                try
                {
                    var rawReportDescriptor = dev.GetRawReportDescriptor();

                    int indent = 0;
                    foreach (var element in EncodedItem.DecodeItems(rawReportDescriptor, 0, rawReportDescriptor.Length))
                    {
                        if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.EndCollection) { indent -= 2; }
                        if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.Collection) { indent += 2; }
                    }

                    var reportDescriptor = dev.GetReportDescriptor();

                    // Lengths should match.
                    Debug.Assert(dev.GetMaxInputReportLength() == reportDescriptor.MaxInputReportLength);
                    Debug.Assert(dev.GetMaxOutputReportLength() == reportDescriptor.MaxOutputReportLength);
                    Debug.Assert(dev.GetMaxFeatureReportLength() == reportDescriptor.MaxFeatureReportLength);

                    foreach (var deviceItem in reportDescriptor.DeviceItems)
                    {
                        {

                            HidStream hidStream;
                            if (dev.TryOpen(out hidStream))
                            {
                                hidStream.ReadTimeout = System.Threading.Timeout.Infinite;

                                using (hidStream)
                                {
                                    var inputReportBuffer = new byte[dev.GetMaxInputReportLength()];
                                    var inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                                    var inputParser = deviceItem.CreateDeviceItemInputParser();

#if SINGLE_THREADED_WAITHANDLE_APPROACH
                                    inputReceiver.Start(hidStream);

                                    int startTime = Environment.TickCount;
                                    while (true)
                                    {
                                        if (inputReceiver.WaitHandle.WaitOne(1000))
                                        {
                                            if (!inputReceiver.IsRunning) { break; } // Disconnected?

                                            Report report;
                                            while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                                            {
                                                // Parse the report if possible.
                                                // This will return false if (for example) the report applies to a different DeviceItem.
                                                if (inputParser.TryParseReport(inputReportBuffer, 0, report))
                                                {
                                                    WriteDeviceItemInputParserResult(inputParser);
                                                }
                                            }
                                        }

                                        uint elapsedTime = (uint)(Environment.TickCount - startTime);
                                       // if (elapsedTime >= 20000) { break; } // Stay open for 20 seconds.
                                    }
#endif
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Footpedal Error: "+e.Message);
                }
            }
        }
        



        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //.KeyCode
            MessageBox.Show("Key Pressed: " + e.KeyValue  );
            e.Handled = false;
        }


        private void loop()
        {
            


            // Initialize DirectInput
            var directInput = new DirectInput();

        // Find a Joystick Guid
        var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Console.WriteLine("No joystick/Gamepad found.");
                //  Console.ReadKey();
                //         Environment.Exit(1);
                // THIS WILL HAVE TO LOOP ON A TIMER UNTIL JOYSTICK FOUND
                return;
            }

    // Instantiate the joystick
    var joystick = new Joystick(directInput, joystickGuid);

    Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);

            // Query all suported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                Console.WriteLine("Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;
            
            // Acquire the joystick
            joystick.Acquire();

           




            //polls 10 times a second for input. This allows UI to refresh
            timer1.Interval = 100;
            timer1.Tick += (s, e) =>
            {
                joystick.Poll();
                var datas = joystick.GetBufferedData(); //only show the last state

                foreach (var state in datas)
                {
                    //SET FOR ANY JOYSTICK BUTTON TO TRIGGER //state.Offset == JoystickOffset.Buttons2 && 
                    if (state.Value == 128)
                    {
                        //foot pedal has been clicked, force a wait of half second before another click can happen. This is to stop the possiblity of double input
                        if (state.Timestamp > (lastPressed + 500))
                        {
                            SendKeys.SendWait("{+}");
                            lastPressed = state.Timestamp;
                        }

                        Console.WriteLine(state);

                    }
                    else
                    {
                        datas = null;
                    }
                }
            };

            //start the timer
            timer1.Enabled = true;


        }
        private void Form1_Load(object sender, EventArgs e)
        { 
        }




        private void BtnClose_Click(object sender, EventArgs e)
        {
          //  System.Threading.Thread.CurrentThread.Abort();
            this.Close();
            
            Environment.Exit(0);
        }

        private void BtnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }

        private void NotifyIcon1_MouseDoubleClick(object sender, EventArgs e)
        {
            
              //  Activate();
           
            Show();
            this.WindowState = FormWindowState.Normal;

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}
