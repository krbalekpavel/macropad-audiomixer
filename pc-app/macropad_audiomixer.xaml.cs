using CSCore.CoreAudioAPI;
using Microsoft.VisualBasic.Logging;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Websocket.Client;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace macropad_audiomixer
{

    public class PressButtonStruct
    {
        public string command { get; set; }
        public int button { get; set; }
        public int page { get; set; }
    }
    public class SetButtonStruct
    {
        public string command { get; set; }
        public int button { get; set; }
        public int page { get; set; }
        public string text { get; set; }
        public int bgColor { get; set; }
        public int textColor { get; set; }
        public int outlineColor { get; set; }
        public string imageURL { get; set; }
        public string imageData = "";
    }
    public class SetOLEDTextStruct
    {
        public string command { get; set; }
        public int oled { get; set; }
        public string text { get; set; }
    }
    public class SetLEDColorStruct
    {
        public string command { get; set; }
        public int strip { get; set; }
        public int color { get; set; }
    }
    public class SetBrightnessStruct
    {
        public string command { get; set; }
        public int brightness { get; set; }
    }

    public class FaderStruct
    {
        public string command { get; set; }
        public int fader { get; set; }
        public int value { get; set; }
    }

    public class ButtonFunction
    {
        public bool ChangeBPreset { get; set; }
        public bool ChangeFPreset { get; set; }
        public string ChangeBPresetTo { get; set; }
        public string ChangeFPresetTo { get; set; }
        public bool AppsEnabled { get; set; }
        public List<string> OpenApps { get; set; }
        public bool KeystrokesEnabled { get; set; }
        public string SendKeystrokes { get; set; }
        public List<string> MultiActions { get; set; }
        public bool MultiActionEnable { get; set; }
    }
    public class ButtonPreset
    {
        public string PresetName { get; set; }
        public List<string> Labels { get; set; }
        public List<string> ImgUrls { get; set; }
        public List<int> BGColors { get; set; }
        public List<int> TextColors { get; set; }
        public List<int> OutlineColors { get; set; }
        public List<ButtonFunction> ButtonFunctions { get; set; }
    }
    public class OLEDdisplay
    {
        public int LineCount { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string Line4 { get; set; }

    }
    public class Submix
    {
        public bool Enabled { get; set; }
        public int Offset { get; set; }
        public List<string> Apps { get; set; }
        public List<string> Devices { get; set; }
    }
    public class FaderPreset
    {
        public string PresetName { get; set; }
        public bool AutoDisplayName { get; set; }
        public List<string> Labels { get; set; }
        public List<int> Colors { get; set; }
        public List<bool> AutoDisplay { get; set; }
        public List<OLEDdisplay> OLEDdisplays { get; set; }
        public List<int> StoredVolumes { get; set; }
        public List<List<string>> Apps { get; set; }
        public List<List<string>> Devices { get; set; }
        public List<List<string>> OBS { get; set; }
        public List<Submix> Submixes { get; set; }

    }
    public class SettingsJson
    {
        public int COMport { get; set; }
        public int Brightness { get; set; }
        public bool DarkMode { get; set; }
        public string OBSip { get; set; }
        public string OBSpw { get; set; }
        public List<ButtonPreset> ButtonPresets { get; set; }
        public List<FaderPreset> FaderPresets { get; set; }
    }

    class SimpleHttpServer
    {
        private HttpListener _listener;
        private string _baseDirectory;

        public SimpleHttpServer(string baseDirectory, string urlPrefix)
        {
            _baseDirectory = baseDirectory;
            _listener = new HttpListener();
            _listener.Prefixes.Add(urlPrefix);
        }

        public void Start()
        {
            _listener.Start();
            //Console.WriteLine("Server started, listening for requests...");
            while (true)
            {
                var context = _listener.GetContext();
                ProcessRequest(context);
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string filePath = Path.Combine(_baseDirectory, context.Request.Url.LocalPath.TrimStart('/'));

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    context.Response.ContentType = GetContentType(filePath);
                    context.Response.ContentLength64 = fileBytes.Length;
                    context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    //Console.WriteLine("Error processing request: " + ex.Message);
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                byte[] notFoundMessage = Encoding.UTF8.GetBytes("404 - File Not Found");
                context.Response.OutputStream.Write(notFoundMessage, 0, notFoundMessage.Length);
            }
            try { 
            context.Response.OutputStream.Close();
                 } catch { }
        }

        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".html" => "text/html",
                ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                _ => "application/octet-stream",
            };
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
    public partial class MainWindow : System.Windows.Window
    {
        SettingsJson settings;
        int TimesPressed1;
        int TimesPressed2;
        int TimesPressed3;
        int TimesPressed4;
        bool maximized;
        string CurrentPortRX;
        System.Windows.Controls.Button SelectedButton;
        System.Windows.Controls.Button PreviousButton;
        System.Windows.Controls.Button SelectedFader;
        System.Windows.Controls.Button PreviousFader;
        int ActiveButtonPreset = 0;
        int ActiveEditingButtonPreset;
        int ActiveSliderPreset = 0;
        int ActiveEditingSliderPreset;
        int Slider1 = 0;
        int Slider1sub;
        int Slider2 = 0;
        int Slider2sub;
        int Slider3 = 0;
        int Slider3sub;
        int Slider4 = 0;
        int Slider4sub;
        int TimerInterval;

        List<int> Sliders = new List<int>();
        public List<List<AudioSessionControl>> SliderActiveApps = new List<List<AudioSessionControl>>();
        public List<List<AudioSessionControl>> SliderActiveSubApps = new List<List<AudioSessionControl>>();
        public List<List<CSCore.CoreAudioAPI.MMDevice>> SliderActiveDevices = new List<List<CSCore.CoreAudioAPI.MMDevice>>();
        public List<List<CSCore.CoreAudioAPI.MMDevice>> SliderActiveSubDevices = new List<List<CSCore.CoreAudioAPI.MMDevice>>();
        private readonly BackgroundWorker worker1 = new BackgroundWorker();
        List<AudioSessionManager2> oogabooga;
        bool disconnected;
        protected OBSWebsocket obs;
        DateTime lastCheckD;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        System.Windows.Threading.DispatcherTimer getTimer = new System.Windows.Threading.DispatcherTimer();
        List<bool> ledLock = new List<bool>();
        List<bool> ledSent = new List<bool>();
        

        List<List<int>> rgbb = new List<List<int>>();
        List<int> vol = new List<int>();


        static string wshost = File.ReadAllText("address.cfg");
        static Uri url = new Uri(wshost);
        WebsocketClient client = new WebsocketClient(url);

        string directoryToServe = "./icons";
        string urlPrefix = "http://+:8080/";


        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            ApplySettings();

            client.ReconnectTimeout = TimeSpan.FromSeconds(300);
            client.DisconnectionHappened.Subscribe(info => WSDisconnected(info));
            client.ReconnectionHappened.Subscribe(info => WSReconnected(info));

            client.MessageReceived.Subscribe(msg => ParseMessage(msg));
            client.Start();

            SimpleHttpServer Fserver = new SimpleHttpServer(directoryToServe, urlPrefix);

            Thread serverThread = new Thread(new ThreadStart(Fserver.Start));
            serverThread.IsBackground = true;  // Makes the thread a background thread
            serverThread.Start();

            lastCheckD = DateTime.Now;
            for (int i = 0; i < 4; i++)
            {
                SliderActiveApps.Add(new List<AudioSessionControl>());
                SliderActiveSubApps.Add(new List<AudioSessionControl>());
                SliderActiveDevices.Add(new List<CSCore.CoreAudioAPI.MMDevice>());
                SliderActiveSubDevices.Add(new List<CSCore.CoreAudioAPI.MMDevice>());
            }

            TimesPressed1 = 0;
            TimesPressed2 = 0;
            maximized = false;
            LinePrompt1.Visibility = Visibility.Hidden;
            Display1LineSelector.Visibility = Visibility.Hidden;
            Display1LineSelector.IsEnabled = false;
            TextBox1.IsEnabled = false;
            LinePrompt2.Visibility = Visibility.Hidden;
            Display2LineSelector.Visibility = Visibility.Hidden;
            Display2LineSelector.IsEnabled = false;
            TextBox2.IsEnabled = false;
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("tmpicon.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };


            DisconnectIndicator.Visibility = Visibility.Hidden;
            ReconnectButton.Visibility = Visibility.Hidden;
            //StartSerial();

            obs = new OBSWebsocket();
            obs.Connected += onConnect;
            obs.Disconnected += onDisconnect;
            OBSConnect();
            ActiveSliderPresetComboBox.SelectedIndex = 0;
            ActiveButtonPresetComboBox.SelectedIndex = 0;
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 1, 20);
            dispatcherTimer.Start();
            //getTimer.Tick += new EventHandler(getTimer_Tick);
            //getTimer.Interval = new TimeSpan(0, 0, 0, 0, 35);
            //getTimer.Start();
            Sliders.Add(settings.FaderPresets[0].StoredVolumes[0]);
            Sliders.Add(settings.FaderPresets[0].StoredVolumes[1]);
            Sliders.Add(settings.FaderPresets[0].StoredVolumes[2]);
            Sliders.Add(settings.FaderPresets[0].StoredVolumes[3]);
            ledLock.Add(false);
            ledLock.Add(false);
            ledLock.Add(false);
            ledLock.Add(false);
            ledSent.Add(false);
            ledSent.Add(false);
            ledSent.Add(false);
            ledSent.Add(false);
            //req3();

            rgbb.Add(new List<int>());
            rgbb.Add(new List<int>());
            rgbb.Add(new List<int>());
            rgbb.Add(new List<int>());

            vol.Add(0);
            vol.Add(0);
            vol.Add(0);
            vol.Add(0);

            ApplyDisplaySettings();
            ApplyButtonSettings();

            
            //Task.Run(() => client.Send("{ message }"));

            for (int i = 1; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    SetButtonStruct setStruct = new SetButtonStruct();
                    setStruct.button = j;
                    setStruct.page = i;
                    setStruct.command = "setButton";
                    setStruct.text = settings.ButtonPresets[i].Labels[j];
                    setStruct.imageURL = settings.ButtonPresets[i].ImgUrls[j];
                    setStruct.bgColor = settings.ButtonPresets[i].BGColors[j];
                    setStruct.outlineColor = settings.ButtonPresets[i].OutlineColors[j];
                    setStruct.textColor = settings.ButtonPresets[i].TextColors[j];
                    string jsonString2 = JsonSerializer.Serialize(setStruct);
                    post(jsonString2);
                    
                }
            }

            for (int i = 0; i < 4;i++)
            {
                SetLEDColorStruct setLEDColorStruct= new SetLEDColorStruct();
                setLEDColorStruct.command = "setLEDColor";
                setLEDColorStruct.strip = i+1;
                setLEDColorStruct.color = settings.FaderPresets[ActiveSliderPreset].Colors[i];
                string jsonString2 = JsonSerializer.Serialize(setLEDColorStruct);
                post(jsonString2);
            }

        }

        void WSDisconnected(DisconnectionInfo info)
        {

            this.Dispatcher.Invoke(
                      new Action(() =>
                      {
                          DisconnectIndicator.Visibility = Visibility.Visible;
                          ReconnectButton.Visibility = Visibility.Visible;

                      }), DispatcherPriority.Normal);

        }

        void WSReconnected(ReconnectionInfo info)
        {
            this.Dispatcher.Invoke(
                      new Action(() =>
                      {
                          DisconnectIndicator.Visibility = Visibility.Hidden;
                          ReconnectButton.Visibility = Visibility.Hidden;
                      }), DispatcherPriority.Normal);
        }

        void ParseMessage(ResponseMessage msg)
        {
            string text = msg.Text;
            if (text.Contains("fader")) 
            {
                FaderStruct faderstruct;
                faderstruct = JsonSerializer.Deserialize<FaderStruct>(text)!;
                this.Dispatcher.Invoke(
                      new Action(() =>
                      {
                          setFader(faderstruct.fader-1, faderstruct.value);
                      }), DispatcherPriority.Normal);
            }
            else {
                PressButtonStruct pressstruct;
                pressstruct = JsonSerializer.Deserialize<PressButtonStruct>(text)!;
                this.Dispatcher.Invoke(
                      new Action(() =>
                      {
                          ButtonPress(pressstruct.button+1, pressstruct.page);
                      }), DispatcherPriority.Normal);
            }
        }



        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            UpdateActiveApps();
            if ((DateTime.Now - lastCheckD).TotalMinutes > 10)
            {
                UpdateActiveDevices();
                lastCheckD = DateTime.Now;
            }
        }




        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }
        void post(string data)
        {
            client.Send(data);

           
        }
        void LoadSettings()
        {
            string fileName = "settings.json";
            string jsonString = File.ReadAllText(fileName);
            settings = JsonSerializer.Deserialize<SettingsJson>(jsonString)!;
        }
        void SaveSettings()
        {
            string fileName = "settings.json";
            string jsonString = JsonSerializer.Serialize(settings);
            File.WriteAllText(fileName, jsonString);
        }
        private void OBSConnect()
        {
            try
            {
                string obsip = settings.OBSip;
                string obspass = settings.OBSpw;
                if (obspass == "null")
                {
                    obspass = "";
                }
                obs.Connect("ws://" + obsip, obspass);
            }
            catch { }
        }

        private void onConnect(object sender, EventArgs e)
        {
            OBSIndicator.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        }
        private void onDisconnect(object sender, EventArgs e)
        {
            OBSIndicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        }




        async void OnData(object sender, SerialDataReceivedEventArgs e)
        {
            

        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            //StartSerial();
            //req3();
            client.Reconnect();
        }
        
        async void setFader(int index, int volume)
        {
            List<System.Windows.Controls.Slider> sldrs = new List<Slider>();
            sldrs.Add(SliderA1);
            sldrs.Add(SliderB1);
            sldrs.Add(SliderC1);
            sldrs.Add(SliderD1);
            List<System.Windows.Controls.Slider> sldrssub = new List<Slider>();
            sldrs.Add(SliderA2);
            sldrs.Add(SliderB2);
            sldrs.Add(SliderC2);
            sldrs.Add(SliderD2);
            
            Sliders[index] = volume;
            sldrs[index].Value = volume;
            

            

            try
            {
                if (SliderActiveApps[index].Count > 0)
                {
                    SetAppsVolume(SliderActiveApps[index], volume);
                }
            }
            catch { }
            try
            {
                if (SliderActiveDevices[index].Count > 0)
                {
                    SetDevicesVolume(SliderActiveDevices[index], volume);
                }
            }
            catch { }
            if (settings.FaderPresets[ActiveSliderPreset].Submixes[index].Enabled)
            {
                int offset = settings.FaderPresets[ActiveSliderPreset].Submixes[index].Offset;
                if (volume + offset > 100 || volume + offset < 0)
                {
                    offset = 0;
                }
                sldrssub[index].Value = volume + offset;
                try
                {
                    if (SliderActiveSubApps[index].Count > 0)
                    {
                        SetAppsVolume(SliderActiveSubApps[index], volume + offset);
                    }
                }
                catch { }
                try
                {
                    if (SliderActiveSubDevices[index].Count > 0)
                    {
                        SetDevicesVolume(SliderActiveSubDevices[index], volume + offset);
                    }
                }
                catch { }
            }
            if (ledLock[index] && !ledSent[index])
            {
                ledSent[index] = true;
            }
        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }
        public void ApplySettings()
        {


            //darkmode
            if (settings.DarkMode)
            {
                for (int a = 0; a < ComboBox.Items.Count; a++)
                {
                    string item = ComboBox.Items[a].ToString();
                    string itemName = item.Substring(item.IndexOf(":") + 2);
                    if (itemName == "Dark")
                    {
                        ComboBox.SelectedItem = ComboBox.Items[a];
                    }
                }
                SetDarkMode();
            }
            else
            {
                for (int a = 0; a < ComboBox.Items.Count; a++)
                {
                    string item = ComboBox.Items[a].ToString();
                    string itemName = item.Substring(item.IndexOf(":") + 2);
                    if (itemName == "Light")
                    {
                        ComboBox.SelectedItem = ComboBox.Items[a];
                    }
                }
                SetLightMode();
            }

            //obs

            OBSIP_TextBox.Text = settings.OBSip;

            OBSPassword_TextBox.Text = settings.OBSpw;

            //brightness
            LedBrightnessTB.Text = settings.Brightness.ToString();

        }

        void RefreshDisplayUi(System.Windows.Controls.TextBox LineSelector, System.Windows.Controls.TextBox TB, System.Windows.Controls.CheckBox Auto, int i)
        {

            LineSelector.IsEnabled = true;
            int lc = settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].LineCount;
            if (lc != 3)
            {
                TB.FontSize = (22 - lc * 5 + lc);
            }
            else
            {
                TB.FontSize = 9;
            }
            LineSelector.Text = lc.ToString();
            LineSelector.IsEnabled = false;

            TB.IsEnabled = true;
            TB.Text = settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line1 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line2 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line3 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line4;
            TB.IsEnabled = false;
            Auto.IsChecked = settings.FaderPresets[ActiveSliderPreset].AutoDisplay[i];
        }
        public void ApplyDisplaySettings()
        {
            UpdateSliderDisplayPresets();
            RefreshDisplayUi(Display1LineSelector, TextBox1, Display1AutoCheckBox, 0);
            RefreshDisplayUi(Display2LineSelector, TextBox2, Display2AutoCheckBox, 1);
            RefreshDisplayUi(Display3LineSelector, TextBox3, Display3AutoCheckBox, 2);
            RefreshDisplayUi(Display4LineSelector, TextBox4, Display4AutoCheckBox, 3);

            System.Windows.Controls.Label[] LabelAlist = { Fader1LabelA, Fader2LabelA, Fader3LabelA, Fader4LabelA };
            for (int i = 0; i < 4; i++)
            {
                int TextColor = 0;
                try
                {
                    TextColor = settings.FaderPresets[ActiveSliderPreset].Colors[i];
                }
                catch
                {
                    TextColor = 0;
                    settings.FaderPresets[ActiveSliderPreset].Colors.Add(0);
                    SaveSettings();
                }
                Color Tcolor = Color.FromRgb(
                    Convert.ToByte((TextColor >> 16) & 0xFF),
                    Convert.ToByte((TextColor >> 8) & 0xFF),
                    Convert.ToByte(TextColor & 0xFF)
                );
                LabelAlist[i].Foreground = new SolidColorBrush(Tcolor);

            }
            for (int i = 0; i < 4; i++)
            {
                SetLEDColorStruct setLEDColorStruct = new SetLEDColorStruct();
                setLEDColorStruct.command = "setLEDColor";
                setLEDColorStruct.strip = i+1;
                setLEDColorStruct.color = settings.FaderPresets[ActiveSliderPreset].Colors[i];
                string jsonString2 = JsonSerializer.Serialize(setLEDColorStruct);
                post(jsonString2);
            }



        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedItem.ToString().Contains("Dark"))
            {
                settings.DarkMode = true;
            }
            else
            {
                settings.DarkMode = false;
            }
            SaveSettings();
            ApplySettings();
        }
        public void SetDarkMode()
        {
            GradientStopCollection collection = new GradientStopCollection
            {
                new GradientStop() {Color = Color.FromArgb(255, 60, 60, 60), Offset = 0 },
                new GradientStop() {Color = Color.FromArgb(255, 80, 80, 80), Offset = 0.5 }
            };
            System.Windows.Application.Current.Resources["BackgroundColor"] = new SolidColorBrush(Color.FromArgb(235, 20, 20, 20));
            System.Windows.Application.Current.Resources["ButtonFaceColor"] = new LinearGradientBrush(collection);
            System.Windows.Application.Current.Resources["TextColor"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            System.Windows.Application.Current.Resources["BorderColor"] = new SolidColorBrush(Color.FromArgb(255, 62, 62, 62));
            System.Windows.Application.Current.Resources["HoverColor"] = new SolidColorBrush(Color.FromArgb(255, 132, 132, 132));
            System.Windows.Application.Current.Resources["SelectedColor"] = new SolidColorBrush(Color.FromArgb(255, 162, 162, 162));
        }
        public void SetLightMode()
        {
            GradientStopCollection collection = new GradientStopCollection
            {
                new GradientStop() {Color = Color.FromArgb(255, 240, 240, 240), Offset = 0 },
                new GradientStop() {Color = Color.FromArgb(255, 229, 229, 229), Offset = 0.5 }
            };
            System.Windows.Application.Current.Resources["BackgroundColor"] = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255));
            System.Windows.Application.Current.Resources["ButtonFaceColor"] = new LinearGradientBrush(collection);
            System.Windows.Application.Current.Resources["TextColor"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            System.Windows.Application.Current.Resources["BorderColor"] = new SolidColorBrush(Color.FromArgb(255, 172, 172, 172));
            System.Windows.Application.Current.Resources["HoverColor"] = new SolidColorBrush(Color.FromArgb(255, 132, 132, 132));
            System.Windows.Application.Current.Resources["SelectedColor"] = new SolidColorBrush(Color.FromArgb(255, 162, 162, 162));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].StoredVolumes = Sliders;
            SaveSettings();
            System.Windows.Application.Current.Shutdown();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (maximized == false)
            {
                base.OnMouseLeftButtonDown(e);

                // Begin dragging the window
                this.DragMove();
            }
        }
        public StringCollection DisplayText(System.Windows.Controls.TextBox TB)
        {
            TB.IsEnabled = true;
            StringCollection Value = new StringCollection();
            for (int i = 0; i < TB.LineCount; i++)
            {
                string CurrentTBLine;
                //try
                // {
                CurrentTBLine = TB.GetLineText(i).Replace("\r\n", string.Empty); ;
                // }
                // catch (ArgumentOutOfRangeException)
                // {
                //     CurrentTBLine = " ";
                // }
                if (CurrentTBLine == null)
                {
                    Value.Add(" ".PadRight(20));
                }
                else
                {
                    Value.Add(CurrentTBLine.PadRight(20));
                }
            }
            if (Value.Count == 0)
            {
                Value.Add(" ".PadRight(20));
                Value.Add(" ".PadRight(20));
                Value.Add(" ".PadRight(20));
                Value.Add(" ".PadRight(20));
            }
            if (Value.Count == 1)
            {
                Value.Add(" ".PadRight(20));
                Value.Add(" ".PadRight(20));
                Value.Add(" ".PadRight(20));
            }
            if (Value.Count == 2)
            {
                Value.Add(" ".PadRight(20));
                Value.Add(" ".PadRight(20));
            }
            if (Value.Count == 3)
            {
                Value.Add(" ".PadRight(20));
            }
            TB.IsEnabled = false;
            return Value;
        }

        private void TBButton1_Click(object sender, RoutedEventArgs e)
        {
            TimesPressed1 = TimesPressed1 + 1;
            if (TimesPressed1 == 1)
            {
                LinePrompt1.Visibility = Visibility.Visible;
                Display1LineSelector.Visibility = Visibility.Visible;
                Display1LineSelector.IsEnabled = true;
                TextBox1.IsEnabled = true;
            }
            else
            {
                if (Display1LineSelector.Text == "1" || Display1LineSelector.Text == "2" || Display1LineSelector.Text == "3" || Display1LineSelector.Text == "4")
                {
                    if (Int32.Parse(Display1LineSelector.Text) != 3)
                    {
                        TextBox1.FontSize = (22 - Int32.Parse(Display1LineSelector.Text) * 5 + Int32.Parse(Display1LineSelector.Text));
                    }
                    else
                    {
                        TextBox1.FontSize = 9;
                    }

                    StringCollection text4 = DisplayText(TextBox1);
                    if (text4 != null)
                    {
                        LinePrompt1.Visibility = Visibility.Hidden;
                        Display1LineSelector.Visibility = Visibility.Hidden;
                        Display1LineSelector.IsEnabled = false;
                        TextBox1.IsEnabled = false;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[0].LineCount = Int32.Parse(Display1LineSelector.Text);
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[0].Line1 = text4[0];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[0].Line2 = text4[1];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[0].Line3 = text4[2];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[0].Line4 = text4[3];
                        SaveSettings();
                        ApplyDisplaySettings();
                        //ReadySendung();
                        TimesPressed1 = 0;
                    }
                    else { }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a valid number of lines (1-4)");
                }
            }

        }
        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            WindowAspectRatio.Register((System.Windows.Window)sender);
        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Windows.Application.Current.Resources["WindowScaleX"] = e.NewSize.Width / 800;
            System.Windows.Application.Current.Resources["WindowScaleY"] = e.NewSize.Height / 450;
        }

        double normaltop;
        double normalleft;
        double normalwidth;
        double normalheight;
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            if (maximized == false)
            {
                normaltop = this.Top;
                normalheight = this.Height;
                normalwidth = this.Width;
                normalleft = this.Left;
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Height * 1.77777777778;
                this.Top = SystemParameters.WorkArea.Top;
                this.Left = SystemParameters.WorkArea.Left + ((SystemParameters.WorkArea.Width - this.Width) / 2);
                this.ResizeMode = ResizeMode.NoResize;
                maximized = true;
            }
            else
            {
                this.Top = normaltop;
                this.Height = normalheight;
                this.Width = normalwidth;
                this.Left = normalleft;
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                maximized = false;
            }

        }

        private void TBButton2_Click(object sender, RoutedEventArgs e)
        {
            TimesPressed2 = TimesPressed2 + 1;
            if (TimesPressed2 == 1)
            {
                LinePrompt2.Visibility = Visibility.Visible;
                Display2LineSelector.Visibility = Visibility.Visible;
                Display2LineSelector.IsEnabled = true;
                TextBox2.IsEnabled = true;
            }
            else
            {
                if (Display2LineSelector.Text == "1" || Display2LineSelector.Text == "2" || Display2LineSelector.Text == "3" || Display2LineSelector.Text == "4")
                {
                    if (Int32.Parse(Display2LineSelector.Text) != 3)
                    {
                        TextBox2.FontSize = (22 - Int32.Parse(Display2LineSelector.Text) * 5 + Int32.Parse(Display2LineSelector.Text));
                    }
                    else
                    {
                        TextBox2.FontSize = 9;
                    }
                    StringCollection text4 = DisplayText(TextBox2);
                    if (text4 != null)
                    {
                        LinePrompt2.Visibility = Visibility.Hidden;
                        Display2LineSelector.Visibility = Visibility.Hidden;
                        Display2LineSelector.IsEnabled = false;
                        TextBox2.IsEnabled = false;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[1].LineCount = Int32.Parse(Display2LineSelector.Text);
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[1].Line1 = text4[0];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[1].Line2 = text4[1];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[1].Line3 = text4[2];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[1].Line4 = text4[3];
                        SaveSettings();
                        ApplyDisplaySettings();
                        //ReadySendung();
                        TimesPressed2 = 0;
                    }
                    else { }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a valid number of lines (1-4)");
                }
            }
        }



        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public const int KEYEVENTF_KEYDOWN = 0x0000; // New definition
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_LCONTROL = 0xA2; //Left Control key code
        public const int A = 0x41; //A key code
        public const int C = 0x43; //C key code
        public T GetEnumValue<T>(string str) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new Exception("T must be an Enumeration type.");
            }
            T val = ((T[])Enum.GetValues(typeof(T)))[0];
            if (!string.IsNullOrEmpty(str))
            {
                foreach (T enumValue in (T[])Enum.GetValues(typeof(T)))
                {
                    if (enumValue.ToString().ToUpper().Equals(str.ToUpper()))
                    {
                        val = enumValue;
                        break;
                    }
                }
            }

            return val;
        }
        public List<byte> KeyCodeFinder(string DefaultString)
        {
            List<byte> ArrayToReturn = new List<byte>();
            ArrayToReturn.Clear();
            string[] SplitString = DefaultString.Split(' ');
            foreach (string SoloString in SplitString)
            {
                if (SoloString.Contains("[") || SoloString.Contains("]"))
                {
                    ArrayToReturn.Add((byte)GetEnumValue<SpecialKeyCode>(SoloString.Substring(1, SoloString.Length - 2)));
                }
                else
                {
                    foreach (char Character in SoloString)
                    {
                        ArrayToReturn.Add((byte)GetEnumValue<KeyCode>("VK_" + Character));
                    }
                }
            }
            return (ArrayToReturn);
        }
        public void ButtonPress(int button, int page)
        {
           
            button = button - 1;
            
            if (settings.ButtonPresets[page].ButtonFunctions[button].KeystrokesEnabled)
            {
                SendKeypresses(button);
            }
            if (settings.ButtonPresets[page].ButtonFunctions[button].AppsEnabled)
            {
                OpenApp(button);
            }
            if (settings.ButtonPresets[page].ButtonFunctions[button].ChangeBPreset)
            {
                //Thread.Sleep(200);
                ChangeButtonPreset(button);
            }
            if (settings.ButtonPresets[page].ButtonFunctions[button].ChangeFPreset)
            {
                ChangeSliderPresetBTTN(button);
            }
            if (settings.ButtonPresets[page].ButtonFunctions[button].MultiActionEnable)
            {
                ExecuteMultiAction(button);
            }
            //}
            //catch { }
        }

        public void SendKeypresses(int button)
        {
            //try
            //{
            foreach (byte code in KeyCodeFinder(settings.ButtonPresets[ActiveButtonPreset].ButtonFunctions[button].SendKeystrokes))
            {
                keybd_event(code, 0, KEYEVENTF_KEYDOWN, 0);
                Thread.Sleep(10);
            }
            Thread.Sleep(40);
            foreach (byte code in KeyCodeFinder(settings.ButtonPresets[ActiveButtonPreset].ButtonFunctions[button].SendKeystrokes))
            {
                keybd_event(code, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(10);
            }

            //}
            //catch
            //{

            //}
        }


        private void OnWindowLoad(object sender, RoutedEventArgs e)
        {
            //ReadySendung();

            UpdateButtonPresetStuff();
            UpdateSliderPresetStuff();
            ApplyFaderSettings();
            ApplyDisplaySettings();
            UpdateSliderDisplayPresets();
        }

        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveEditingButtonPresetComboBox.SelectedItem == null)
            {
                ActiveEditingButtonPresetComboBox.SelectedItem = "Default";
                UpdateAppOpenButtonSettings();
                PreviousButton = SelectedButton;
                SelectedButton = (System.Windows.Controls.Button)sender;
                SelectedButtonChanged();
            }
            else
            {
                UpdateAppOpenButtonSettings();
                PreviousButton = SelectedButton;
                SelectedButton = (System.Windows.Controls.Button)sender;
                SelectedButtonChanged();
            }
        }

        private void ResetSelection_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppOpenButtonSettings();
            PreviousButton = SelectedButton;
            SelectedButton = null;
            SelectedButtonChanged();
            EnableDisableMacroSetting(false);
        }

        public void SelectedButtonChanged()
        {
            if (PreviousButton != null)
            {
                PreviousButton.Background = (LinearGradientBrush)System.Windows.Application.Current.Resources["ButtonFaceColor"];
            }
            if (SelectedButton != null)
            {
                SelectedButton.Background = new SolidColorBrush(Color.FromRgb(88, 157, 198));
                SK_CheckBox.IsEnabled = true;
                KeyStrokeBox.Clear();
                VisualiseButtonSettings();
                ReadAppOpenButtonSettings();
                EnableDisableMacroSetting(true);
                UpdateMultiActionUI();
                UpdateMultiActionUI();
            }
        }
        public void VisualiseButtonSettings()
        {
            //try
            //{
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            try
            {
                ButtonNameBox.Text = settings.ButtonPresets[ActiveEditingButtonPreset].Labels[index - 1];
            }
            catch
            {
            }
            try
            {
                BGColorTB.Text = settings.ButtonPresets[ActiveEditingButtonPreset].BGColors[index - 1].ToString();
            }
            catch
            {
            }
            try
            {
                TextColorTB.Text = settings.ButtonPresets[ActiveEditingButtonPreset].TextColors[index - 1].ToString();
            }
            catch
            {
            }
            try
            {
                OutlineColorTB.Text = settings.ButtonPresets[ActiveEditingButtonPreset].OutlineColors[index - 1].ToString();
            }
            catch
            {
            }
            try
            {
                IMGTB.Text = settings.ButtonPresets[ActiveEditingButtonPreset].ImgUrls[index - 1].ToString();
            }
            catch
            {
            }
            try
            {
                if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].KeystrokesEnabled)
                {
                    SK_CheckBox.IsChecked = true;
                    SKSaveButton.IsEnabled = true;
                    SCButton.IsEnabled = true;
                    SK_Label.IsEnabled = true;
                    KeyStrokeBox.IsEnabled = true;
                    KeyStrokeBox.Text = settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].SendKeystrokes;
                }
                else
                {
                    SK_CheckBox.IsChecked = false;
                    SKSaveButton.IsEnabled = false;
                    SCButton.IsEnabled = false;
                    SK_Label.IsEnabled = false;
                    KeyStrokeBox.IsEnabled = false;
                }
            }
            catch
            {
                SK_CheckBox.IsChecked = false;
                SKSaveButton.IsEnabled = false;
                SCButton.IsEnabled = false;
                SK_Label.IsEnabled = false;
                KeyStrokeBox.IsEnabled = false;
            }
            // }
            //catch {
            //SK_CheckBox.IsChecked = false;
            //SKSaveButton.IsEnabled = false;
            //SCButton.IsEnabled = false;
            //SK_Label.IsEnabled = false;
            //KeyStrokeBox.IsEnabled = false;
            //} try
            //{
            try
            {
                if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeBPreset)
                {
                    ChangeButtonPresetCheckBox.IsChecked = true;
                    SelectButtonPresetComboBox.IsEnabled = true;
                    AddNewButtonPresetButton.IsEnabled = true;
                    RemoveButtonPresetButton.IsEnabled = true;
                    SelectButtonPresetComboBox.SelectedItem = settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeBPresetTo;
                }
                else
                {
                    SelectButtonPresetComboBox.SelectedItem = null;
                    ChangeButtonPresetCheckBox.IsChecked = false;
                    SelectButtonPresetComboBox.IsEnabled = false;
                    AddNewButtonPresetButton.IsEnabled = false;
                    RemoveButtonPresetButton.IsEnabled = false;
                }
            }
            catch
            {
                SelectButtonPresetComboBox.SelectedItem = null;
                ChangeButtonPresetCheckBox.IsChecked = false;
                SelectButtonPresetComboBox.IsEnabled = false;
                AddNewButtonPresetButton.IsEnabled = false;
                RemoveButtonPresetButton.IsEnabled = false;
            }
            //}
            //catch {
            //SelectButtonPresetComboBox.SelectedItem = null;
            //ChangeButtonPresetCheckBox.IsChecked = false;
            //SelectButtonPresetComboBox.IsEnabled = false;
            //AddNewButtonPresetButton.IsEnabled = false;
            //RemoveButtonPresetButton.IsEnabled = false;
            //}
            try
            {
                if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeFPreset)
                {
                    ChangeSliderPresetCheckBox.IsChecked = true;
                    SelectSliderPresetComboBox.IsEnabled = true;
                    AddSliderPresetButton.IsEnabled = true;
                    RemoveSliderPresetButton.IsEnabled = true;
                    SelectSliderPresetComboBox.SelectedItem = settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeFPresetTo;
                }
                else
                {
                    SelectSliderPresetComboBox.SelectedItem = null;
                    ChangeSliderPresetCheckBox.IsChecked = false;
                    SelectSliderPresetComboBox.IsEnabled = false;
                    AddSliderPresetButton.IsEnabled = false;
                    RemoveSliderPresetButton.IsEnabled = false;
                }
            }
            catch
            {
                SelectSliderPresetComboBox.SelectedItem = null;
                ChangeSliderPresetCheckBox.IsChecked = false;
                SelectSliderPresetComboBox.IsEnabled = false;
                AddSliderPresetButton.IsEnabled = false;
                RemoveSliderPresetButton.IsEnabled = false;
            }
            //}
            //catch
            //{
            //SelectSliderPresetComboBox.SelectedItem = null;
            //ChangeSliderPresetCheckBox.IsChecked = false;
            //SelectSliderPresetComboBox.IsEnabled = false;
            //AddSliderPresetButton.IsEnabled = false;
            //RemoveSliderPresetButton.IsEnabled = false;
            // }

        }
        public ImageSource GetImageSource(string filename)
        {
            string _fileName = filename;

            BitmapImage glowIcon = new BitmapImage();

            glowIcon.BeginInit();
            glowIcon.UriSource = new Uri(_fileName);
            glowIcon.EndInit();

            return glowIcon;
        }
        public void ApplyButtonSettings()
        {
            
            SaveSettings();

            for (int i = 1; i < 13; i++)
            {


                System.Windows.Controls.Button Button = ((System.Windows.Controls.Button)this.FindName("Button" + i));
                Button.Content = settings.ButtonPresets[ActiveButtonPreset].Labels[i - 1];
                int BgColor = settings.ButtonPresets[ActiveButtonPreset].BGColors[i - 1];
                Color Bcolor = Color.FromRgb(
                  Convert.ToByte(((BgColor >> 11) & 0x1F) << 3), // red component
                  Convert.ToByte(((BgColor >> 5) & 0x3F) << 2),  // green component
                  Convert.ToByte((BgColor & 0x1F) << 3)          // blue component
                );
                int OutColor = settings.ButtonPresets[ActiveButtonPreset].OutlineColors[i - 1];
                Color Ocolor = Color.FromRgb(
                    Convert.ToByte((OutColor >> 8) & 0xFF),
                    Convert.ToByte((OutColor >> 4) & 0xFF),
                    Convert.ToByte(OutColor & 0xFF)
                );
                int TextColor = settings.ButtonPresets[ActiveButtonPreset].TextColors[i - 1];
                Color Tcolor = Color.FromRgb(
                    Convert.ToByte((TextColor >> 8) & 0xFF),
                    Convert.ToByte((TextColor >> 4) & 0xFF),
                    Convert.ToByte(TextColor & 0xFF)
                );
                Button.Background = new SolidColorBrush(Bcolor);
                Button.Foreground = new SolidColorBrush(Tcolor);
                Button.BorderBrush = new SolidColorBrush(Ocolor);

                string img = settings.ButtonPresets[ActiveButtonPreset].ImgUrls[i - 1];
                bool gut = false;
                if (img != "" && img != null)
                {
                    try
                    {
                        Button.Background = new System.Windows.Media.ImageBrush(GetImageSource(img));
                        gut = true;
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Failed to load image");
                    }
                }
                if (true)
                {
                    if (gut)
                    {

                        SetButtonStruct setStruct = new SetButtonStruct();
                        setStruct.button = i - 1;
                        setStruct.page = ActiveButtonPreset;
                        setStruct.command = "setButton";
                        setStruct.text = settings.ButtonPresets[ActiveButtonPreset].Labels[i - 1];
                        setStruct.imageURL = img;
                        setStruct.bgColor = settings.ButtonPresets[ActiveButtonPreset].BGColors[i - 1];
                        setStruct.outlineColor = settings.ButtonPresets[ActiveButtonPreset].OutlineColors[i - 1];
                        setStruct.textColor = settings.ButtonPresets[ActiveButtonPreset].TextColors[i - 1];
                        string jsonString2 = JsonSerializer.Serialize(setStruct);
                        post(jsonString2);
                       
                    }
                    else
                    {
                        SetButtonStruct setStruct = new SetButtonStruct();
                        setStruct.button = i - 1;
                        setStruct.page = ActiveButtonPreset;
                        setStruct.command = "setButton";
                        setStruct.text = settings.ButtonPresets[ActiveButtonPreset].Labels[i - 1];
                        setStruct.imageURL = "";
                        setStruct.bgColor = settings.ButtonPresets[ActiveButtonPreset].BGColors[i - 1];
                        setStruct.outlineColor = settings.ButtonPresets[ActiveButtonPreset].OutlineColors[i - 1];
                        setStruct.textColor = settings.ButtonPresets[ActiveButtonPreset].TextColors[i - 1];
                        string jsonString4 = JsonSerializer.Serialize(setStruct);
                        post(jsonString4);
                       
                    }

                }
                

                System.Windows.Controls.Button ButtonC = ((System.Windows.Controls.Button)this.FindName("B" + i));
                ButtonC.Content = settings.ButtonPresets[ActiveEditingButtonPreset].Labels[i - 1];

              
            }



        }
        private void SaveName_Click(object sender, RoutedEventArgs e)
        {
            SelectedButton.Content = ButtonNameBox.Text;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].Labels[index - 1] = ButtonNameBox.Text;
            SaveSettings();
            ApplyButtonSettings();
        }

        private void SpecialCharactersView(object sender, RoutedEventArgs e)
        {
            SpecialCharacterWindow SCWindow = new SpecialCharacterWindow();
            SCWindow.ShowDialog();
        }

        private void KeyStrokesSave(object sender, RoutedEventArgs e)
        {
            if (SelectedButton != null)
            {
                if (KeyStrokeBox.Text.Contains("\r\n") != true)
                {
                    int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                    settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].KeystrokesEnabled = true;
                    settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].SendKeystrokes = KeyStrokeBox.Text;
                    SaveSettings();
                }
                else
                {
                    System.Windows.MessageBox.Show("Text is invalid, has unnecessary newline.");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a button to edit");
            }
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            SKSaveButton.IsEnabled = true;
            SCButton.IsEnabled = true;
            SK_Label.IsEnabled = true;
            KeyStrokeBox.IsEnabled = true;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].KeystrokesEnabled = true;
            SaveSettings();
        }

        private void CheckBox_UnChecked_1(object sender, RoutedEventArgs e)
        {
            SKSaveButton.IsEnabled = false;
            SCButton.IsEnabled = false;
            SK_Label.IsEnabled = false;
            KeyStrokeBox.IsEnabled = false;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].KeystrokesEnabled = false;
            SaveSettings();
        }

        private void AppAddButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ListBoxItem ItemToAdd = new ListBoxItem { Content = openFileDialog.FileName };
                AppListBox.Items.Add(ItemToAdd);
                UpdateAppOpenButtonSettings();
            }

        }

        private void AppDele_Click(object sender, RoutedEventArgs e)
        {

            List<string> muitems = new List<string>();
            foreach (var item in AppListBox.Items)
            {
                muitems.Add(item.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty));
            }
            MultiActionRemove MUREMWindow = new MultiActionRemove(muitems);
            MUREMWindow.Closed += new EventHandler(AppRem_Closed);
            MUREMWindow.ShowDialog();


        }

        void AppRem_Closed(object sender, EventArgs e)
        {
            if (((MultiActionRemove)sender).DialogResult == true)
            {
                string outs = ((MultiActionRemove)sender).Output;
                int id = ((MultiActionRemove)sender).OutputId;
                AppListBox.Items.RemoveAt(id);
                
                UpdateAppOpenButtonSettings();
            }
        }

        public void UpdateAppOpenButtonSettings()
        {
            if (SelectedButton != null)
            {
                try
                {
                    if (AppCheckBox.IsChecked == true)
                    {
                        int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                        if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].OpenApps != null)
                        {
                            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].OpenApps.Clear();
                        }
                        else
                        {
                            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].OpenApps = new List<string>();
                        }

                        foreach (var item in AppListBox.Items)
                        {
                            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].OpenApps.Add(item.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty));
                        }

                        settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].AppsEnabled = true;
                        SaveSettings();

                    }
                    else
                    {
                        int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                        settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].AppsEnabled = false;
                        SaveSettings();

                    }
                }
                catch
                {

                }

            }
        }

        public void ReadAppOpenButtonSettings()
        {
            bool IsEnabled;
            AppListBox.Items.Clear();
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));

            try
            {
                IsEnabled = settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].AppsEnabled;
            }
            catch
            {
                IsEnabled = false;
            }

            if (IsEnabled == true && settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].OpenApps != null)
            {
                AppCheckBox.IsChecked = true;
                AppAddButton.IsEnabled = true;
                AppRemove.IsEnabled = true;
                List<string> IndividualApps = settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].OpenApps;
                foreach (string a in IndividualApps)
                {
                    if (a != "" && a != " ")
                    {
                        ListBoxItem ItemToAdd = new ListBoxItem { Content = a };
                        AppListBox.Items.Add(ItemToAdd);
                    }
                }
            }
            else
            {
                AppCheckBox.IsChecked = false;
                AppAddButton.IsEnabled = false;
                AppRemove.IsEnabled = false;
            }
        }

        private void CheckBox_Checked_2(object sender, RoutedEventArgs e)
        {
            AppAddButton.IsEnabled = true;
            AppRemove.IsEnabled = true;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].AppsEnabled = true;
            SaveSettings();
        }

        private void CheckBox_UnChecked_2(object sender, RoutedEventArgs e)
        {
            AppAddButton.IsEnabled = false;
            AppRemove.IsEnabled = false;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].AppsEnabled = false;
            SaveSettings();
        }

        private void OpenApp(int button)
        {

            List<string> IndividualApps = settings.ButtonPresets[ActiveButtonPreset].ButtonFunctions[button].OpenApps;
            foreach (string a in IndividualApps)
            {
                if (a != "" && a != " ")
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = a,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
        }
        private void MacrosOpening(object sender, MouseButtonEventArgs e)
        {
            if (SelectedButton == null)
            {
                EnableDisableMacroSetting(false);
            }
        }
        public void EnableDisableMacroSetting(bool isenabled)
        {
            AppAddButton.IsEnabled = isenabled;
            AppRemove.IsEnabled = isenabled;
            AppCheckBox.IsEnabled = isenabled;
            SKSaveButton.IsEnabled = isenabled;
            SCButton.IsEnabled = isenabled;
            SK_Label.IsEnabled = isenabled;
            KeyStrokeBox.IsEnabled = isenabled;
            SK_CheckBox.IsEnabled = isenabled;
            SelectButtonPresetComboBox.IsEnabled = isenabled;
            AddNewButtonPresetButton.IsEnabled = isenabled;
            RemoveButtonPresetButton.IsEnabled = isenabled;
            SelectSliderPresetComboBox.IsEnabled = isenabled;
            AddSliderPresetButton.IsEnabled = isenabled;
            RemoveSliderPresetButton.IsEnabled = isenabled;
            ChangeButtonPresetCheckBox.IsEnabled = isenabled;
            ChangeSliderPresetCheckBox.IsEnabled = isenabled;
        }

        private void TabSelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedItem == TabControl.Items[2])
            {
                EnableDisableMacroSetting(false);
            }
        }
        #region BUTTON PRESET STUFF
        public void ActiveButtonPresetChange(string NewActivePreset)
        {
            int i = 0;
            bool gut = false;
            foreach (var item in settings.ButtonPresets)
            {
                if (item.PresetName == NewActivePreset)
                {
                    ActiveButtonPreset = i;
                    gut = true;
                }
                i++;
            }
            if (gut)
            {

                ActiveButtonPresetComboBox.SelectedIndex = ActiveButtonPreset;
                ApplyButtonSettings();
                
                ApplyDisplaySettings();

            }

        }

        private void ActiveButtonPresetCBChanged(object sender, SelectionChangedEventArgs e)
        {
            // try
            //{
            ActiveButtonPresetChange(ActiveButtonPresetComboBox.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty));
            // } catch
            // {

            // }

        }

        public void UpdateButtonPresetStuff()
        {
            //try
            //{
            var PresetsSplit = settings.ButtonPresets;
            bool rem = false;
            ComboBoxItem ix = null;
            foreach (var item in ActiveButtonPresetComboBox.Items)
            {
                bool WasTrue = false;
                foreach (var preset in PresetsSplit)
                {
                    if (item.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty) == preset.PresetName)
                    {
                        WasTrue = true;
                    }
                }
                if (WasTrue == true)
                {

                }
                else
                {
                    if (item.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty) == settings.ButtonPresets[ActiveButtonPreset].PresetName || item.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty) == settings.ButtonPresets[ActiveButtonPreset].PresetName)
                    {
                        ActiveButtonPreset = 0;
                        ActiveEditingButtonPreset = 0;
                        rem = true;
                        ix = (ComboBoxItem)item;
                        //ActiveButtonPresetComboBox.Items.Remove(item);
                        //ActiveEditingButtonPresetComboBox.Items.Remove(item);
                        //SelectButtonPresetComboBox.Items.Remove(item);
                    }
                    else
                    {
                        rem = true;
                        ix = (ComboBoxItem)item;
                        //ActiveButtonPresetComboBox.Items.Remove(item);
                        //ActiveEditingButtonPresetComboBox.Items.Remove(item);
                        //SelectButtonPresetComboBox.Items.Remove(item);
                    }
                }
            }
            if (rem && ix != null)
            {
                ActiveButtonPresetComboBox.Items.Remove(ix);
                ActiveEditingButtonPresetComboBox.Items.Remove(ix);
                SelectButtonPresetComboBox.Items.Remove(ix);
            }
            foreach (var preset in PresetsSplit)
            {
                if (preset.PresetName != "" || preset.PresetName != " ")
                {
                    if (!ActiveButtonPresetComboBox.Items.Contains(preset.PresetName))
                    {
                        ActiveButtonPresetComboBox.Items.Add(preset.PresetName);
                    }
                    if (!ActiveEditingButtonPresetComboBox.Items.Contains(preset.PresetName))
                    {
                        ActiveEditingButtonPresetComboBox.Items.Add(preset.PresetName);
                    }
                    if (!SelectButtonPresetComboBox.Items.Contains(preset.PresetName))
                    {
                        SelectButtonPresetComboBox.Items.Add(preset.PresetName);
                    }
                }
            }
            
        }

        private void AddButtonPresetClick(object sender, RoutedEventArgs e)
        {
            //try
            //{
            ButtonPresetAdd BTADDWindow = new ButtonPresetAdd();
            BTADDWindow.Closed += new EventHandler(Window_Closed);
            BTADDWindow.ShowDialog();
            //}
            //catch { }
        }
        public void Window_Closed(object sender, EventArgs e)
        {
            if (((ButtonPresetAdd)sender).DialogResult == true)
            {
                string passedString = ((ButtonPresetAdd)sender).PassedString;
                // Do something with the passed string
                bool gut = true;
                foreach (var preset in settings.ButtonPresets)
                {
                    if (preset.PresetName == passedString)
                    {
                        System.Windows.MessageBox.Show("A preset with this name already exists");
                        gut = false;
                    }
                }
                if (gut)
                {
                    ButtonPreset newPreset = new ButtonPreset();
                    newPreset.PresetName = passedString;
                   
                    settings.ButtonPresets.Add(newPreset);
                    int id = settings.ButtonPresets.Count - 1;
                    for (int i = 0; i < settings.ButtonPresets.Count; i++)
                    {
                        if (settings.ButtonPresets[i].PresetName == passedString)
                        {
                            id = i;
                        }
                    }

                    resetButtonPreset(id, passedString);
                    SaveSettings();
                    UpdateButtonPresetStuff();
                    ApplyButtonSettings();
                }

            }

        }

        void resetButtonPreset(int index, string presetName)
        {
            settings.ButtonPresets[index].PresetName = presetName;
            settings.ButtonPresets[index].ButtonFunctions = new List<ButtonFunction> { new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction(), new ButtonFunction() }; ;
            settings.ButtonPresets[index].ImgUrls = new List<string>();
            settings.ButtonPresets[index].Labels = new List<string>();
            settings.ButtonPresets[index].OutlineColors = new List<int> { 69, 69, 69, 69, 69, 69, 69, 69, 69, 69, 69, 69 };
            settings.ButtonPresets[index].TextColors = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            settings.ButtonPresets[index].BGColors = new List<int> { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
            for (int i = 0; i < 12; i++)
            {
                settings.ButtonPresets[index].Labels.Add((i + 1).ToString());
                //settings.ButtonPresets[index].Labels[i] = (i + 1).ToString();
                settings.ButtonPresets[index].ImgUrls.Add("");
                settings.ButtonPresets[index].ButtonFunctions[i].MultiActionEnable = false;
                settings.ButtonPresets[index].ButtonFunctions[i].ChangeBPreset = false;
                settings.ButtonPresets[index].ButtonFunctions[i].ChangeFPreset = false;
                settings.ButtonPresets[index].ButtonFunctions[i].AppsEnabled = false;
                settings.ButtonPresets[index].ButtonFunctions[i].KeystrokesEnabled = false;

            }
            SaveSettings();
        }
        private void ActiveEditingButtonPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAppOpenButtonSettings();
            PreviousButton = SelectedButton;
            SelectedButton = null;
            SelectedButtonChanged();
            EnableDisableMacroSetting(false);
            ActiveEditingButtonPreset = ActiveEditingButtonPresetComboBox.SelectedIndex;
            ApplyButtonSettings();
        }

        private void BPCheckBox_UnChecked_2(object sender, RoutedEventArgs e)
        {
            SelectButtonPresetComboBox.IsEnabled = false;
            AddNewButtonPresetButton.IsEnabled = false;
            RemoveButtonPresetButton.IsEnabled = false;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeBPreset = false;
            SaveSettings();
            ApplyButtonSettings();
        }

        private void BPCheckBox_Checked_2(object sender, RoutedEventArgs e)
        {
            SelectButtonPresetComboBox.IsEnabled = true;
            AddNewButtonPresetButton.IsEnabled = true;
            RemoveButtonPresetButton.IsEnabled = true;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeBPreset = true;
            SaveSettings();
            ApplyButtonSettings();
        }

        private void RemoveButtonPresetButton_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //ButtonPresetRemove BTREMWindow = new ButtonPresetRemove();
            //BTREMWindow.Closed += new EventHandler(Window_Closed);
            //BTREMWindow.ShowDialog();
            // }
            // catch { }

            if (SelectButtonPresetComboBox.SelectedItem != null && !SelectButtonPresetComboBox.SelectedItem.ToString().Contains("Default"))
            {
                if (ActiveButtonPreset != SelectButtonPresetComboBox.SelectedIndex)
                {
                    if (ActiveEditingButtonPreset == SelectButtonPresetComboBox.SelectedIndex)
                    {
                        ActiveEditingButtonPreset = 0;
                    }

                    settings.ButtonPresets.RemoveAt(SelectButtonPresetComboBox.SelectedIndex);
                    SaveSettings();
                    UpdateButtonPresetStuff();
                    ApplyButtonSettings();


                }
            }
        }

        private void SelectButtonPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectButtonPresetComboBox.SelectedItem != null && SelectedButton != null)
            {
                //try
                //{
                int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeBPreset)
                {
                    settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeBPresetTo = SelectButtonPresetComboBox.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty);
                    SaveSettings();
                }
                // }
                //catch
                // {
                //     ChangeSliderPresetCheckBox.IsChecked = false;
                // }
            }
            else
            {
                ChangeButtonPresetCheckBox.IsChecked = false;
            }

        }

        public void ChangeButtonPreset(int ButtonIndex)
        {
            //try
            //{
            string NewPreset = settings.ButtonPresets[ActiveButtonPreset].ButtonFunctions[ButtonIndex].ChangeBPresetTo;
            ActiveButtonPresetChange(NewPreset);

            // } catch
            // { }
        }
        #endregion
        List<int> getrgb(int volume, int color)
        {
            List<int> rgb = new List<int>();
            if (volume > 0 && volume <= 10)
            {
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 10 && volume <= 20)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 20 && volume <= 30)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 30 && volume <= 40)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 40 && volume <= 50)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 50 && volume <= 60)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 60 && volume <= 70)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 70 && volume <= 80)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
                rgb.Add(0);
            }
            if (volume > 80 && volume <= 90)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(0);
            }
            if (volume > 90 && volume <= 100)
            {
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
                rgb.Add(color);
            }
            return rgb;
        }
        #region SLIDER PRESET STUFF
        public void ChangeSliderPreset(string NewPreset)
        {
            int i = 0;
            bool gut = false;
            foreach (var item in settings.FaderPresets)
            {
                if (item.PresetName == NewPreset)
                {
                    settings.FaderPresets[ActiveSliderPreset].StoredVolumes = Sliders;
                    SaveSettings();
                    ActiveSliderPreset = i;
                    gut = true;
                }
                i++;
            }
            if (gut)
            {
                ActiveSliderPresetComboBox.SelectedIndex = ActiveSliderPreset;
                ApplyButtonSettings();
                UpdateActiveApps();
                UpdateActiveDevices();
                UpdateSliderDisplayPresets();
                ApplyDisplaySettings();
                Sliders = settings.FaderPresets[ActiveSliderPreset].StoredVolumes;
                for (int id = 0; id < 4; id++)
                {
                    ledLock[id] = true;
                    setFader(id, settings.FaderPresets[ActiveSliderPreset].StoredVolumes[id]);
                }
                for (int j = 0; j < 4; j++)
                {
                    SetLEDColorStruct setLEDColorStruct = new SetLEDColorStruct();
                    setLEDColorStruct.command = "setLEDColor";
                    setLEDColorStruct.strip = j+1;
                    setLEDColorStruct.color = settings.FaderPresets[ActiveSliderPreset].Colors[j];
                    string jsonString2 = JsonSerializer.Serialize(setLEDColorStruct);
                    post(jsonString2);
                }
                //req3();
            }
        }

        public void UpdateSliderPresetStuff()
        {
            //try
            //{
            List<FaderPreset> PresetsSplit = settings.FaderPresets;
            bool rem = false;
            ComboBoxItem ix = null;
            foreach (var item in ActiveSliderPresetComboBox.Items)
            {
                bool WasTrue = false;
                foreach (var preset in PresetsSplit)
                {
                    if (item.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty) == preset.PresetName)
                    {
                        WasTrue = true;
                    }
                }
                if (WasTrue == true)
                {

                }
                else
                {
                    if (item.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty) == settings.FaderPresets[ActiveSliderPreset].PresetName || item.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty) == settings.FaderPresets[ActiveSliderPreset].PresetName)
                    {
                        ActiveSliderPreset = 0;
                        ActiveEditingSliderPreset = 0;
                        rem = true;
                        ix = ((ComboBoxItem)item);
                        
                    }
                    else
                    {
                        rem = true;
                        ix = ((ComboBoxItem)item);
                        
                    }
                }
            }
            if (rem && ix != null)
            {
                ActiveSliderPresetComboBox.Items.Remove(ix);
                ActiveEditingFaderPresetComboBox.Items.Remove(ix);
                SelectSliderPresetComboBox.Items.Remove(ix);
            }
            foreach (var preset in PresetsSplit)
            {
                if (preset.PresetName != "")
                {
                    if (!ActiveSliderPresetComboBox.Items.Contains(preset.PresetName))
                    {
                        ActiveSliderPresetComboBox.Items.Add(preset.PresetName);
                    }

                    if (!ActiveEditingFaderPresetComboBox.Items.Contains(preset.PresetName))
                    {
                        ActiveEditingFaderPresetComboBox.Items.Add(preset.PresetName);
                    }
                    if (!SelectSliderPresetComboBox.Items.Contains(preset.PresetName))
                    {
                        SelectSliderPresetComboBox.Items.Add(preset.PresetName);
                    }
                }
            }
            
        }
        private void SelectSliderPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectSliderPresetComboBox.SelectedItem != null && SelectedButton != null)
            {
                //try
                //{
                int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeFPreset)
                {
                    settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeFPresetTo = SelectSliderPresetComboBox.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty);
                    SaveSettings();
                }
                // }
                //catch
                // {
                //     ChangeSliderPresetCheckBox.IsChecked = false;
                // }
            }
            else
            {
                ChangeSliderPresetCheckBox.IsChecked = false;
            }
        }

        private void ChangeSliderPresetCheckBox_UnChecked_2(object sender, RoutedEventArgs e)
        {
            SelectSliderPresetComboBox.IsEnabled = false;
            AddSliderPresetButton.IsEnabled = false;
            RemoveSliderPresetButton.IsEnabled = false;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeFPreset = false;
            SaveSettings();
        }

        private void ChangeSliderPresetCheckBox_Checked_2(object sender, RoutedEventArgs e)
        {
            SelectSliderPresetComboBox.IsEnabled = true;
            AddSliderPresetButton.IsEnabled = true;
            RemoveSliderPresetButton.IsEnabled = true;
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].ChangeFPreset = true;
            SaveSettings();

        }

        private void AddSliderPresetButton_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            SliderPresetAdd SLADDWindow = new SliderPresetAdd();
            SLADDWindow.Closed += new EventHandler(WindowSL_Closed);
            SLADDWindow.ShowDialog();
            // }
            // catch { }
        }

        void WindowSL_Closed(object sender, EventArgs e)
        {
            if (((SliderPresetAdd)sender).DialogResult == true)
            {
                string passedString = ((SliderPresetAdd)sender).PassedString;
                // Do something with the passed string
                bool gut = true;
                foreach (var preset in settings.FaderPresets)
                {
                    if (preset.PresetName == passedString)
                    {
                        System.Windows.MessageBox.Show("A preset with this name already exists");
                        gut = false;
                    }
                }
                if (gut)
                {
                    FaderPreset newPreset = new FaderPreset();
                    newPreset.Apps = new List<List<string>> { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
                    newPreset.Devices = new List<List<string>> { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
                    newPreset.Submixes = new List<Submix> { new Submix(), new Submix(), new Submix(), new Submix() }; ;
                    newPreset.PresetName = passedString;
                    newPreset.AutoDisplayName = false;
                    newPreset.AutoDisplay = new List<bool> { true, true, true, true };
                    newPreset.Colors = new List<int> { 255, 255, 255, 255 };
                    newPreset.Labels = new List<string>();
                    newPreset.OBS = new List<List<string>> { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
                    newPreset.OLEDdisplays = new List<OLEDdisplay> { new OLEDdisplay(), new OLEDdisplay(), new OLEDdisplay(), new OLEDdisplay() };
                    newPreset.StoredVolumes = new List<int>();
                    settings.FaderPresets.Add(newPreset);
                    SaveSettings();
                    UpdateSliderPresetStuff();
                    ApplyButtonSettings();
                }

            }

        }

        void resetFaderPreset(int index, string presetName)
        {

        }
        private void RemoveSliderPresetButton_Click(object sender, RoutedEventArgs e)
        {
           

            if (SelectSliderPresetComboBox.SelectedItem != null && !SelectSliderPresetComboBox.SelectedItem.ToString().Contains("Default"))
            {
                if (ActiveSliderPreset != SelectSliderPresetComboBox.SelectedIndex)
                {
                    if (ActiveEditingSliderPreset == SelectSliderPresetComboBox.SelectedIndex)
                    {
                        ActiveEditingSliderPreset = 0;
                    }

                    settings.FaderPresets.RemoveAt(SelectSliderPresetComboBox.SelectedIndex);
                    SaveSettings();
                    UpdateSliderPresetStuff();
                    ApplyButtonSettings();


                }
            }

        }

        private void ActiveSliderPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //try
            //{
            ChangeSliderPreset(ActiveSliderPresetComboBox.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", string.Empty));
            //}
            //catch (NullReferenceException)
            //{

            // }
        }
        public void ChangeSliderPresetBTTN(int ButtonIndex)
        {
            //try
            //{
            string NewPreset = settings.ButtonPresets[ActiveButtonPreset].ButtonFunctions[ButtonIndex].ChangeFPresetTo;
            ChangeSliderPreset(NewPreset);

            // }
            // catch
            // { }
        }
        #endregion

        private void Button1_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button Pressed = (System.Windows.Controls.Button)sender;
            int index = Int32.Parse(Pressed.Name.Replace("Button", string.Empty));
            ButtonPress(index,ActiveButtonPreset);
        }

        #region MULTIACTION

        public void UpdateMultiActionUI()
        {
            MultiactionListBox.Items.Clear();
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            try
            {
                if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActionEnable)
                {
                    MultiactionAddButton.IsEnabled = true;
                    MultiactionRemoveButton.IsEnabled = true;
                    MultiactionCheckBox.IsEnabled = true;
                    MultiactionCheckBox.IsChecked = true;
                    if (settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActions == null)
                    {
                        settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActions = new List<string>();
                        SaveSettings();
                    }
                    else
                    {
                        foreach (string command in settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActions)
                        {
                            if (command != "")
                            {
                                MultiactionListBox.Items.Add(command);
                            }
                        }
                    }
                }
                else
                {
                    if (SelectedButton == null)
                    {
                        MultiactionCheckBox.IsEnabled = false;
                    }
                    MultiactionAddButton.IsEnabled = false;
                    MultiactionRemoveButton.IsEnabled = false;
                    MultiactionCheckBox.IsChecked = false;
                }
            }
            catch
            {
                if (SelectedButton == null)
                {
                    MultiactionCheckBox.IsEnabled = false;
                }
                MultiactionAddButton.IsEnabled = false;
                MultiactionRemoveButton.IsEnabled = false;
                MultiactionCheckBox.IsChecked = false;
            }
        }
        private void MultiactionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActionEnable = true;
            SaveSettings();
            UpdateMultiActionUI();
        }

        private void MultiactionCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
            settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActionEnable = false;
            SaveSettings();
            UpdateMultiActionUI();
        }

        private void MultiactionAddButtonClick(object sender, RoutedEventArgs e)
        {
            //try
            // {
            MultiActionAdd MUADDWindow = new MultiActionAdd();
            MUADDWindow.Closed += new EventHandler(WindowMU_Closed);
            MUADDWindow.ShowDialog();
            //  }
            // catch { }
        }

        void WindowMU_Closed(object sender, EventArgs e)
        {
            if (((MultiActionAdd)sender).DialogResult == true)
            {
                string passedString = ((MultiActionAdd)sender).Output;
                int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActions.Add(passedString);
                SaveSettings();
                UpdateMultiActionUI();
            }

        }

        private void MultiactionRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            // try
            //{
            List<string> muitems = new List<string>();
            foreach (var item in MultiactionListBox.Items)
            {
                muitems.Add(item.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty));
            }
            MultiActionRemove MUREMWindow = new MultiActionRemove(muitems);
            MUREMWindow.Closed += new EventHandler(WindowMUREM_Closed);
            MUREMWindow.ShowDialog();
            
        }

        void WindowMUREM_Closed(object sender, EventArgs e)
        {
            if (((MultiActionRemove)sender).DialogResult == true)
            {
                string outs = ((MultiActionRemove)sender).Output;
                int id = ((MultiActionRemove)sender).OutputId;
                MultiactionListBox.Items.RemoveAt(id);
                int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                settings.ButtonPresets[ActiveEditingButtonPreset].ButtonFunctions[index - 1].MultiActions.RemoveAt(id);

                SaveSettings();
                UpdateMultiActionUI();
            }
        }
        int ButtonIndex;
        public void ExecuteMultiAction(int BtnIndex)
        {
            ButtonIndex = BtnIndex;
            Thread MUThread = new Thread(MultiAction);
            MUThread.Start();

        }

        public void MultiAction()
        {
            foreach (string command in settings.ButtonPresets[ActiveButtonPreset].ButtonFunctions[ButtonIndex].MultiActions)
            {
                if (command.StartsWith("keypress"))
                {
                    foreach (byte code in KeyCodeFinder((command.Substring(9, command.Length - 10)).ToString()))
                    {
                        keybd_event(code, 0, KEYEVENTF_KEYDOWN, 0);
                        Thread.Sleep(10);
                    }
                    Thread.Sleep(40);
                    foreach (byte code in KeyCodeFinder((command.Substring(9, command.Length - 10)).ToString()))
                    {
                        keybd_event(code, 0, KEYEVENTF_KEYUP, 0);
                        Thread.Sleep(10);
                    }
                }
                if (command.StartsWith("open"))
                {
                    //try
                    //{
                    System.Diagnostics.Process.Start(command.Substring(5, command.Length - 6));
                    //} catch 
                    // {
                    //    MessageBox.Show("The file does not exist");
                    //}
                }
                if (command.StartsWith("setbuttonpreset"))
                {
                    foreach (var preset in settings.ButtonPresets)
                    {
                        if (preset.PresetName == command.Substring(16, command.Length - 17))
                        {
                            ActiveButtonPresetChange(preset.PresetName);
                        }
                    }
                }
                if (command.StartsWith("setsliderpreset"))
                {
                    foreach (var preset in settings.FaderPresets)
                    {
                        if (preset.PresetName == command.Substring(16, command.Length - 17))
                        {
                            ChangeSliderPreset(preset.PresetName);
                        }
                    }
                }
                if (command.StartsWith("wait"))
                {
                    Thread.Sleep(Int32.Parse(command.Substring(5, command.Length - 6)) * 1000);
                }
            }
        }


        #endregion

        #region DisplayAutoSetters
        private void DisplayAutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[0] = true;
            SaveSettings();
            TextBox1.IsEnabled = false;
            TBButton1.IsEnabled = false;
            Display1LineSelector.Visibility = Visibility.Hidden;
            LinePrompt1.Visibility = Visibility.Hidden;
            UpdateSliderDisplayPresets();
        }
        private void DisplayAutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[0] = false;
            SaveSettings();
            TextBox1.IsEnabled = true;
            TBButton1.IsEnabled = true;
            Display1LineSelector.Visibility = Visibility.Visible;
            LinePrompt1.Visibility = Visibility.Visible;
            UpdateSliderDisplayPresets();
        }
        private void Display2AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[1] = true;
            SaveSettings();
            TextBox2.IsEnabled = false;
            TBButton2.IsEnabled = false;
            Display2LineSelector.Visibility = Visibility.Hidden;
            LinePrompt2.Visibility = Visibility.Hidden;
            UpdateSliderDisplayPresets();
        }
        private void Display2AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[1] = false;
            SaveSettings();
            TextBox2.IsEnabled = true;
            TBButton2.IsEnabled = true;
            Display2LineSelector.Visibility = Visibility.Visible;
            LinePrompt2.Visibility = Visibility.Visible;
            UpdateSliderDisplayPresets();
        }
        #endregion

        public void UpdateSliderDisplayPresets()
        {
            if (settings.FaderPresets[ActiveSliderPreset].AutoDisplay[0])
            {
                Display1AutoCheckBox.IsChecked = true;
                Display1LineSelector.Visibility = Visibility.Hidden;
                LinePrompt1.Visibility = Visibility.Hidden;
                if (settings.FaderPresets[ActiveSliderPreset].AutoDisplayName)
                {
                    Sliderdisplayautomode.SelectedIndex = 0;
                    System.Windows.Controls.Label[] LabelAlist = { Fader1LabelA, Fader2LabelA, Fader3LabelA, Fader4LabelA };
                    for (int i = 0; i < 4; i++)
                    {
                        string SliderName1 = LabelAlist[i].Content.ToString();
                        string Line1 = SliderName1;
                        string Line2 = "";
                        string Line3 = "";
                        string Line4 = "";
                        int lineCount = 1;
                        if (SliderName1.Length > 10)
                        {
                            lineCount = 2;
                        }
                        if (SliderName1.Length > 15)
                        {
                            lineCount = 3;
                            Line1 = SliderName1.Substring(0, SliderName1.Length / 2);
                            Line2 = SliderName1.Substring(SliderName1.Length / 2);
                        }
                        if (SliderName1.Length > 25)
                        {
                            lineCount = 4;
                            Line1 = SliderName1.Substring(0, SliderName1.Length / 2);
                            Line2 = SliderName1.Substring(SliderName1.Length / 2);
                        }
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line1 = Line1;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line2 = Line2;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line3 = Line3;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line4 = Line4;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].LineCount = lineCount;
                    }
                }
                else
                {
                    Sliderdisplayautomode.SelectedIndex = 1;
                    for (int i = 0; i < 4; i++)
                    {
                        string Line1 = "";
                        string Line2 = "";
                        string Line3 = "";
                        string Line4 = "";
                        int lineCount = 1;
                        List<string> functions = new List<string>();
                        functions.AddRange(settings.FaderPresets[ActiveSliderPreset].Apps[i]);
                        //functions.AddRange(settings.FaderPresets[ActiveSliderPreset].Devices[i]);
                        if (settings.FaderPresets[ActiveSliderPreset].Devices[i].Count > 0)
                        {
                            foreach (var dev in settings.FaderPresets[ActiveSliderPreset].Devices[i])
                            {
                                functions.Add(dev.Substring(dev.IndexOf("(") + 1));
                            }
                        }
                        if (settings.FaderPresets[ActiveSliderPreset].Submixes[i].Enabled)
                        {
                            functions.AddRange(settings.FaderPresets[ActiveSliderPreset].Submixes[i].Apps);
                            functions.AddRange(settings.FaderPresets[ActiveSliderPreset].Submixes[i].Devices);
                        }

                        if (functions.Count == 1)
                        {
                            lineCount = 1;
                            Line1 = functions[0];
                        }
                        if (functions.Count == 2)
                        {
                            lineCount = 2;
                            Line1 = functions[0];
                            Line2 = functions[1];
                        }
                        if (functions.Count == 3)
                        {
                            lineCount = 3;
                            Line1 = functions[0];
                            Line2 = functions[1];
                            Line3 = functions[2];
                        }
                        if (functions.Count == 4)
                        {
                            lineCount = 4;
                            Line1 = functions[0];
                            Line2 = functions[1];
                            Line3 = functions[2];
                            Line4 = functions[3];
                        }
                        if (functions.Count == 5)
                        {
                            lineCount = 4;
                            Line1 = functions[0] + " " + functions[4];
                            Line2 = functions[1];
                            Line3 = functions[2];
                            Line4 = functions[3];
                        }
                        if (functions.Count == 6)
                        {
                            lineCount = 4;
                            Line1 = functions[0] + " " + functions[4];
                            Line2 = functions[1] + " " + functions[5];
                            Line3 = functions[2];
                            Line4 = functions[3];
                        }
                        if (functions.Count == 7)
                        {
                            lineCount = 4;
                            Line1 = functions[0] + " " + functions[4];
                            Line2 = functions[1] + " " + functions[5];
                            Line3 = functions[2] + " " + functions[6];
                            Line4 = functions[3];
                        }
                        if (functions.Count == 8)
                        {
                            lineCount = 4;
                            Line1 = functions[0] + " " + functions[4];
                            Line2 = functions[1] + " " + functions[5];
                            Line3 = functions[2] + " " + functions[6];
                            Line4 = functions[3] + " " + functions[7];
                        }
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line1 = Line1;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line2 = Line2;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line3 = Line3;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].Line4 = Line4;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[i].LineCount = lineCount;


                    }


                }
            }
            else
            {
                

            }
            if (true)
            {
                for (int id = 0; id < 4; id++)
                {
                    SetOLEDTextStruct setoled = new SetOLEDTextStruct();
                    setoled.command = "setOLEDText";
                    string text = "";
                    switch(settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].LineCount)
                    {
                        case 1: { text = settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line1; break; }
                        case 2: { text = settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line1 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line2; break; }
                        case 3: { text = settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line1 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line2 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line3; break; }
                        case 4: { text = settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line1 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line2 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line3 + "\n" + settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[id].Line4; break; }
                    }
                    setoled.text = text;
                    setoled.oled = id + 1;

                    
                    string jsonString = JsonSerializer.Serialize(setoled);
                    post(jsonString);
                   
                }

            }
            SaveSettings();

            

        }
        private string ListboxLine(System.Windows.Controls.ListBox ListBox, int index)
        {
            string stuff;
            if (ListBox.Items.Count < index + 1)
            {
                stuff = " ".PadRight(20);
            }
            else
            {
                stuff = ListBox.Items[index].ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty).PadRight(20);
                if (stuff.Contains("("))
                {
                    stuff = stuff.Substring(stuff.IndexOf("(") + 1);
                }
            }
            return stuff;
        }
        #region SLIDERS
        private static List<CSCore.CoreAudioAPI.AudioSessionManager2> GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                MMDeviceCollection collection = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
                var managers = new List<CSCore.CoreAudioAPI.AudioSessionManager2>();
                for (int i3 = 0; i3 < collection.Count; i3++)
                {
                    MMDevice device = collection[i3];
                    Console.WriteLine(device.FriendlyName);
                    var manager = AudioSessionManager2.FromMMDevice(device);
                    managers.Add(manager);
                }
                return managers;
            }
        }

        System.Collections.Generic.List<string> SliderApps(int SliderID)
        {
            try
            {
                return settings.FaderPresets[ActiveSliderPreset].Apps[SliderID];
            }
            catch
            {
                List<string> nuller = new List<string>();
                return nuller;
            }
        }
        System.Collections.Generic.List<string> SliderSubApps(int SliderID)
        {
            try
            {
                return settings.FaderPresets[ActiveSliderPreset].Submixes[SliderID].Apps;
            }
            catch
            {
                List<string> nuller = new List<string>();
                return nuller;
            }
        }
        System.Collections.Generic.List<string> SliderDevices(int SliderID)
        {
            try
            {
                return settings.FaderPresets[ActiveSliderPreset].Devices[SliderID];
            }
            catch
            {
                List<string> nuller = new List<string>();
                return nuller;
            }
        }
        System.Collections.Generic.List<string> SliderSubDevices(int SliderID)
        {
            try
            {
                return settings.FaderPresets[ActiveSliderPreset].Submixes[SliderID].Devices;
            }
            catch
            {
                List<string> nuller = new List<string>();
                return nuller;
            }
        }
        void worker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            oogabooga = GetDefaultAudioSessionManager2(DataFlow.Render);
            worker.Dispose();
        }
        private void BackgroundWorkerOnProgressChanged1(object sender, ProgressChangedEventArgs e)
        {
        }

        void worker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void synclistbox()
        {
            ListBox1.Items.Clear();
            ListBox2.Items.Clear();
            ListBox3.Items.Clear();
            ListBox4.Items.Clear();
            if (SliderActiveApps.Count == 0)
            {
                return;
            }
            foreach (var session in SliderActiveApps[0])
            {
                using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                {
                    ListBox1.Items.Add(sessionControl.Process.ProcessName);
                }
            }
            foreach (var session in SliderActiveApps[1])
            {
                using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                {
                    ListBox2.Items.Add(sessionControl.Process.ProcessName);
                }
            }
            foreach (var session in SliderActiveApps[2])
            {
                using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                {
                    ListBox3.Items.Add(sessionControl.Process.ProcessName);
                }
            }
            foreach (var session in SliderActiveApps[3])
            {
                using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                {
                    ListBox4.Items.Add(sessionControl.Process.ProcessName);
                }
            }
            foreach (MMDevice device in SliderActiveDevices[0])
            {
                ListBox1.Items.Add(device.FriendlyName);
            }
            foreach (MMDevice device in SliderActiveDevices[1])
            {
                ListBox2.Items.Add(device.FriendlyName);
            }
            foreach (MMDevice device in SliderActiveDevices[2])
            {
                ListBox3.Items.Add(device.FriendlyName);
            }
            foreach (MMDevice device in SliderActiveDevices[3])
            {
                ListBox4.Items.Add(device.FriendlyName);
            }
        }
        void UpdateActiveApps()
        {
            try
            {
                SliderActiveApps.Clear();
                SliderActiveApps.Add(new List<AudioSessionControl>());
                SliderActiveApps.Add(new List<AudioSessionControl>());
                SliderActiveApps.Add(new List<AudioSessionControl>());
                SliderActiveApps.Add(new List<AudioSessionControl>());
                SliderActiveSubApps.Clear();
                SliderActiveSubApps.Add(new List<AudioSessionControl>());
                SliderActiveSubApps.Add(new List<AudioSessionControl>());
                SliderActiveSubApps.Add(new List<AudioSessionControl>());
                SliderActiveSubApps.Add(new List<AudioSessionControl>());
                worker1.DoWork += worker1_DoWork;
                worker1.RunWorkerCompleted += worker1_RunWorkerCompleted;
                worker1.ProgressChanged += BackgroundWorkerOnProgressChanged1;
                worker1.WorkerSupportsCancellation = true;
                worker1.WorkerReportsProgress = true;
                if (!worker1.IsBusy)
                {
                    worker1.RunWorkerAsync();
                }
                while (oogabooga == null) { }
                //Thread.Sleep(110);
                List<AudioSessionManager2> list = oogabooga;
                for (int i1 = 0; i1 < list.Count; i1++)
                {
                    AudioSessionManager2 sessionManager = list[i1];
                    using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                    {
                        for (int i2 = 0; i2 < sessionEnumerator.Count; i2++)
                        {
                            AudioSessionControl session = sessionEnumerator[i2];
                            using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                            {
                                for (int g = 0; g < 4; g++)
                                {
                                    if (SliderApps(g).Contains(sessionControl.Process.ProcessName))
                                    {
                                        SliderActiveApps[g].Add(session);
                                    }
                                    if (settings.FaderPresets[ActiveSliderPreset].Submixes[g].Enabled)
                                    {
                                        if (SliderSubApps(g).Contains(sessionControl.Process.ProcessName))
                                        {
                                            SliderActiveSubApps[g].Add(session);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                synclistbox();
            }
            catch { }
        }

        void UpdateActiveDevices()
        {
            SliderActiveDevices.Clear();
            SliderActiveDevices.Add(new List<MMDevice>());
            SliderActiveDevices.Add(new List<MMDevice>());
            SliderActiveDevices.Add(new List<MMDevice>());
            SliderActiveDevices.Add(new List<MMDevice>());
            SliderActiveSubDevices.Clear();
            SliderActiveSubDevices.Add(new List<MMDevice>());
            SliderActiveSubDevices.Add(new List<MMDevice>());
            SliderActiveSubDevices.Add(new List<MMDevice>());
            SliderActiveSubDevices.Add(new List<MMDevice>());
            var enumerator = new MMDeviceEnumerator();
            MMDeviceCollection collectiond = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            for (int a2 = 0; a2 < collectiond.Count; a2++)
            {
                var deviced = collectiond[a2];
                for (int y = 0; y < 4; y++)
                {
                    if (SliderDevices(y).Contains(deviced.FriendlyName))
                    {
                        if (SliderActiveDevices[y] == null)
                        {
                            SliderActiveDevices[y] = new List<MMDevice>();
                        }
                        SliderActiveDevices[y].Add(deviced);
                    }

                    if (settings.FaderPresets[ActiveSliderPreset].Submixes[y].Enabled)
                    {

                        if (SliderSubDevices(y).Contains(deviced.FriendlyName))
                        {
                            if (SliderActiveSubDevices[y] == null)
                            {
                                SliderActiveSubDevices[y] = new List<MMDevice>();
                            }
                            SliderActiveSubDevices[y].Add(deviced);
                        }
                    }
                }



            }
            synclistbox();
        }

        private void SetAppsVolume(List<AudioSessionControl> AppList, float Volume)
        {
            foreach (AudioSessionControl session in AppList)
            {
                using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                {
                    if (Volume > 96) { Volume = 100; }
                    if (Volume < 2) { Volume = 0; }
                    simpleVolume.MasterVolume = Volume / 100;
                }
            }
        }

        private void SetDevicesVolume(List<CSCore.CoreAudioAPI.MMDevice> DeviceList, float Volume)
        {
            foreach (CSCore.CoreAudioAPI.MMDevice deviced in DeviceList)
            {
                if (Volume > 96) { Volume = 100; }
                if (Volume < 2) { Volume = 0; }
                AudioEndpointVolume.FromDevice(deviced).MasterVolumeLevelScalar = Volume / 100;
            }
        }

        private void SetObsVolume(List<string> list, float Volume)
        {
            if (obs.IsConnected)
            {
                foreach (string a in list)
                {
                    if (Volume > 96) { Volume = 100; }
                    if (Volume < 2) { Volume = 0; }
                    obs.SetVolume(a, Volume / 100, false);
                }
            }
        }
        private void FaderSelector_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveEditingFaderPresetComboBox.SelectedItem == null)
            {
                ActiveEditingFaderPresetComboBox.SelectedItem = "Default";
                PreviousFader = SelectedFader;
                SelectedFader = (System.Windows.Controls.Button)sender;
                SelectedFaderChanged();
            }
            else
            {
                PreviousFader = SelectedFader;
                SelectedFader = (System.Windows.Controls.Button)sender;
                SelectedFaderChanged();
            }
        }

        private void EnableDisableFaderSetting(bool IsEnabled)
        {
            SubmixOffsetTextBox.IsEnabled = IsEnabled;
            AddSubmixApp.IsEnabled = IsEnabled;
            AddSubmixDevice.IsEnabled = IsEnabled;
            RemoveSubmixApp.IsEnabled = IsEnabled;
            RemoveSubmixDevice.IsEnabled = IsEnabled;
            SaveSubmixSettings.IsEnabled = IsEnabled;
            EnableSubmixCheckBox.IsEnabled = IsEnabled;
            AddVolumeApp.IsEnabled = IsEnabled;
            AddVolumeDevice.IsEnabled = IsEnabled;
            RemoveVolumeApp.IsEnabled = IsEnabled;
            RemoveVolumeDevice.IsEnabled = IsEnabled;
        }
        private void SelectedFaderChanged()
        {
            if (PreviousFader != null)
            {
                PreviousFader.Background = (ImageBrush)System.Windows.Application.Current.Resources["FaderDefault"];
            }
            if (SelectedFader != null)
            {
                SelectedFader.Background = (ImageBrush)System.Windows.Application.Current.Resources["FaderSelected"];
                EnableDisableFaderSetting(true);
            }
            ApplyFaderSettings();
        }

        private void ResetFaderSelection_Click(object sender, RoutedEventArgs e)
        {
            PreviousFader = SelectedFader;
            SelectedFader = null;
            SelectedFaderChanged();
            EnableDisableFaderSetting(false);
        }

        private void SaveFaderName_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Labels[faderid] = FaderNameBox.Text;
                SaveSettings();
                ApplyFaderSettings();

            }
        }

        private void ApplyFaderSettings()
        {
            ApplyDisplaySettings();
            SubmixAppsListBox.Items.Clear();
            VolumeDevicesListBox.Items.Clear();
            VolumeAppsListBox.Items.Clear();
            SubmixDevicesListBox.Items.Clear();
            SubmixOffsetTextBox.Text = "0";

            EnableSubmixCheckBox.IsChecked = false;
            SubmixOffsetTextBox.IsEnabled = false;
            AddSubmixApp.IsEnabled = false;
            AddSubmixDevice.IsEnabled = false;
            RemoveSubmixApp.IsEnabled = false;
            RemoveSubmixDevice.IsEnabled = false;
            SaveSubmixSettings.IsEnabled = false;

            for (int i = 1; i < 5; i++)
            {
                string a = i.ToString();
                string b = i.ToString();
                try
                {
                    a = settings.FaderPresets[ActiveSliderPreset].Labels[i - 1];
                }
                catch { }
                try
                {
                    b = settings.FaderPresets[ActiveEditingSliderPreset].Labels[i - 1];
                }
                catch { }
                ((System.Windows.Controls.Label)this.FindName("Fader" + i + "LabelA")).Content = a;
                ((System.Windows.Controls.Label)this.FindName("Slider" + i + "Label")).Content = b;
            }
            if (SelectedFader != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                FaderColorTB.Text = settings.FaderPresets[ActiveEditingSliderPreset].Colors[faderid].ToString();
                try
                {
                    FaderNameBox.Text = settings.FaderPresets[ActiveEditingSliderPreset].Labels[faderid];
                }
                catch
                {
                    settings.FaderPresets[ActiveEditingSliderPreset].Labels.Add("1");
                    settings.FaderPresets[ActiveEditingSliderPreset].Labels.Add("2");
                    settings.FaderPresets[ActiveEditingSliderPreset].Labels.Add("3");
                    settings.FaderPresets[ActiveEditingSliderPreset].Labels.Add("4");
                    FaderNameBox.Text = settings.FaderPresets[ActiveEditingSliderPreset].Labels[faderid];
                }

                EnableSubmixCheckBox.IsChecked = settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Enabled;
                SubmixOffsetTextBox.Text = settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Offset.ToString();


                List<string> split = settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Apps;
                if (split != null)
                {
                    foreach (string item in split)
                    {
                        if (item != "" && item != " " && item != null)
                        {
                            SubmixAppsListBox.Items.Add(item);
                        }
                    }
                }


                split = settings.FaderPresets[ActiveEditingSliderPreset].Apps[faderid];
                if (split != null)
                {
                    foreach (string item in split)
                    {
                        if (item != "" && item != " " && item != null)
                        {
                            VolumeAppsListBox.Items.Add(item);
                        }
                    }
                }
                split = settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Devices;
                if (split != null)
                {
                    foreach (string item in split)
                    {
                        if (item != "" && item != " " && item != null)
                        {

                            SubmixDevicesListBox.Items.Add(item);

                        }
                    }
                }
                split = settings.FaderPresets[ActiveEditingSliderPreset].Devices[faderid];
                if (split != null)
                {
                    foreach (string item in split)
                    {
                        if (item != "" && item != " " && item != null)
                        {
                            VolumeDevicesListBox.Items.Add(item);
                        }
                    }
                }
            }


            UpdateActiveApps();
            UpdateActiveDevices();
            UpdateSliderObsUi();
        }
        private void ActiveEditingSliderPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PreviousFader = SelectedFader;
            SelectedFader = null;
            SelectedFaderChanged();
            ActiveEditingSliderPreset = ActiveEditingFaderPresetComboBox.SelectedIndex;
            ApplyFaderSettings();
        }

        private void EnableSubmixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Enabled = true;
                SaveSettings();
                SubmixOffsetTextBox.IsEnabled = true;
                AddSubmixApp.IsEnabled = true;
                AddSubmixDevice.IsEnabled = true;
                RemoveSubmixApp.IsEnabled = true;
                RemoveSubmixDevice.IsEnabled = true;
                SaveSubmixSettings.IsEnabled = true;
            }
        }

        private void EnableSubmixCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Enabled = false;
                SaveSettings();
            }
            SubmixOffsetTextBox.IsEnabled = false;
            AddSubmixApp.IsEnabled = false;
            AddSubmixDevice.IsEnabled = false;
            RemoveSubmixApp.IsEnabled = false;
            RemoveSubmixDevice.IsEnabled = false;
            SaveSubmixSettings.IsEnabled = false;
        }

        private void SaveSubmixSettings_Click(object sender, RoutedEventArgs e)
        {
            Int32 offset;
            string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
            int faderid = Int32.Parse(c) - 1;
            if (SubmixOffsetTextBox.Text != null)
            {
                string ToCheck = SubmixOffsetTextBox.Text;
                if (ToCheck.StartsWith("-"))
                {
                    ToCheck = ToCheck.Replace("-", string.Empty);
                }
                if (ToCheck.All(char.IsDigit))
                {
                    if (Int32.Parse(SubmixOffsetTextBox.Text) < 98 && Int32.Parse(SubmixOffsetTextBox.Text) > -98)
                    {
                        offset = Int32.Parse(SubmixOffsetTextBox.Text);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Submix offset can not be higher than 100%, will have its default value zero");
                        offset = 0;
                    }

                }
                else
                {
                    System.Windows.MessageBox.Show("Submix offset can be made only out of numbers, will have its default value zero");
                    offset = 0;
                }

            }
            else
            {
                System.Windows.MessageBox.Show("Offset not entered, will have its default value zero");
                offset = 0;

            }
            settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Enabled = (bool)EnableSubmixCheckBox.IsChecked;
            settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Offset = offset;
            SaveSettings();
            UpdateActiveApps();
            UpdateActiveDevices();
        }

        private void AddSubmixApp_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                AddSoundSession SubmixAppAdd = new AddSoundSession();
                SubmixAppAdd.Closed += new EventHandler(SubmixAppAdd_WindowClosed);
                SubmixAppAdd.ShowDialog();
            }
        }

        private void SubmixAppAdd_WindowClosed(object sender, EventArgs e)
        {
            AddSoundSession window = (AddSoundSession)sender;
            if (window.DialogResult == true)
            {
                string passedString = window.Output;
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Apps.RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Apps.Add(passedString);
                SaveSettings();
                ApplyFaderSettings();
            }
        }

        private void RemoveSubmixApp_Click(object sender, RoutedEventArgs e)
        {
            if (SubmixAppsListBox.SelectedItem != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Apps.RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Apps.RemoveAt(SubmixAppsListBox.SelectedIndex);
                SaveSettings();
                ApplyFaderSettings();
            }
        }
        void AddSubmixDevice_onClosed(object a, EventArgs eventArgs)
        {
            AddSoundDevice window = (AddSoundDevice)a;
            if (window.DialogResult == true)
            {
                string passedString = window.Output;
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Devices.RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Devices.Add(passedString);
                SaveSettings();
                ApplyFaderSettings();
            }
        }
        private void AddSubmixDevice_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                AddSoundDevice SubmixAppAdd = new AddSoundDevice();
                SubmixAppAdd.Closed += new EventHandler(AddSubmixDevice_onClosed);
                SubmixAppAdd.ShowDialog();
            }
        }

        private void RemoveSubmixDevice_Click(object sender, RoutedEventArgs e)
        {
            if (SubmixDevicesListBox.SelectedItem != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Devices.RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Submixes[faderid].Devices.RemoveAt(SubmixDevicesListBox.SelectedIndex);
                SaveSettings();
                ApplyFaderSettings();
            }
        }

        private void AddVolumeApp_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                AddSoundSession AppAdd = new AddSoundSession();
                AppAdd.Closed += new EventHandler(AddApp_onClosed);
                AppAdd.ShowDialog();
            }
        }
        void AddApp_onClosed(object a, EventArgs eventArgs)
        {
            AddSoundSession window = (AddSoundSession)a;
            if (window.DialogResult == true)
            {
                string passedString = window.Output;
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Apps[faderid].RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Apps[faderid].Add(passedString);
                SaveSettings();
                ApplyFaderSettings();
            }
        }
        private void RemoveVolumeApp_Click(object sender, RoutedEventArgs e)
        {
            if (VolumeAppsListBox.SelectedItem != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Apps[faderid].RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Apps[faderid].RemoveAt(VolumeAppsListBox.SelectedIndex);
                SaveSettings();
                ApplyFaderSettings();
            }
        }

        private void AddVolumeDevice_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                AddSoundDevice AppAdd = new AddSoundDevice();
                AppAdd.Closed += new EventHandler(AddDevice_onClosed);
                AppAdd.ShowDialog();
            }
        }

        void AddDevice_onClosed(object a, EventArgs eventArgs)
        {
            AddSoundDevice window = (AddSoundDevice)a;
            if (window.DialogResult == true)
            {
                string passedString = window.Output;
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Devices[faderid].RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Devices[faderid].Add(passedString);

                SaveSettings();
                ApplyFaderSettings();
            }
        }
        private void RemoveVolumeDevice_Click(object sender, RoutedEventArgs e)
        {
            if (VolumeDevicesListBox.SelectedItem != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].Devices[faderid].RemoveAll(string.IsNullOrEmpty);
                settings.FaderPresets[ActiveEditingSliderPreset].Devices[faderid].RemoveAt(VolumeDevicesListBox.SelectedIndex);
                SaveSettings();
                ApplyFaderSettings();
            }
        }

        #endregion

        private void Sliderdisplayautomode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Sliderdisplayautomode.SelectedItem.ToString().Contains("names"))
            {
                settings.FaderPresets[ActiveSliderPreset].AutoDisplayName = true;
            }
            else
            {
                settings.FaderPresets[ActiveSliderPreset].AutoDisplayName = false;
            }
            SaveSettings();
            ApplyDisplaySettings();
        }

        private void Display3AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[2] = true;
            SaveSettings();
            TextBox3.IsEnabled = false;
            TBButton3.IsEnabled = false;
            Display3LineSelector.Visibility = Visibility.Hidden;
            LinePrompt3.Visibility = Visibility.Hidden;
            UpdateSliderDisplayPresets();
        }

        private void Display3AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[2] = false;
            SaveSettings();
            TextBox3.IsEnabled = true;
            TBButton3.IsEnabled = true;
            Display3LineSelector.Visibility = Visibility.Visible;
            LinePrompt3.Visibility = Visibility.Visible;
            UpdateSliderDisplayPresets();
        }

        private void TBButton3_Click(object sender, RoutedEventArgs e)
        {
            TimesPressed3 = TimesPressed3 + 1;
            if (TimesPressed3 == 1)
            {
                LinePrompt3.Visibility = Visibility.Visible;
                Display3LineSelector.Visibility = Visibility.Visible;
                Display3LineSelector.IsEnabled = true;
                TextBox3.IsEnabled = true;
            }
            else
            {
                if (Display3LineSelector.Text == "1" || Display3LineSelector.Text == "2" || Display3LineSelector.Text == "3" || Display3LineSelector.Text == "4")
                {
                    if (Int32.Parse(Display3LineSelector.Text) != 3)
                    {
                        TextBox3.FontSize = (22 - Int32.Parse(Display3LineSelector.Text) * 5 + Int32.Parse(Display3LineSelector.Text));
                    }
                    else
                    {
                        TextBox3.FontSize = 9;
                    }

                    StringCollection text3 = DisplayText(TextBox3);
                    if (text3 != null)
                    {
                        LinePrompt3.Visibility = Visibility.Hidden;
                        Display3LineSelector.Visibility = Visibility.Hidden;
                        Display3LineSelector.IsEnabled = false;
                        TextBox3.IsEnabled = false;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[2].LineCount = Int32.Parse(Display4LineSelector.Text);
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[2].Line1 = text3[0];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[2].Line2 = text3[1];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[2].Line3 = text3[2];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[2].Line4 = text3[3];
                        SaveSettings();
                        ApplyDisplaySettings();
                        //ReadySendung();
                        TimesPressed3 = 0;
                    }
                    else { }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a valid number of lines (1-4)");
                }
            }
        }

        private void TBButton4_Click(object sender, RoutedEventArgs e)
        {
            TimesPressed4 = TimesPressed4 + 1;
            if (TimesPressed4 == 1)
            {
                LinePrompt4.Visibility = Visibility.Visible;
                Display4LineSelector.Visibility = Visibility.Visible;
                Display4LineSelector.IsEnabled = true;
                TextBox4.IsEnabled = true;
            }
            else
            {
                if (Display4LineSelector.Text == "1" || Display4LineSelector.Text == "2" || Display4LineSelector.Text == "3" || Display4LineSelector.Text == "4")
                {
                    if (Int32.Parse(Display4LineSelector.Text) != 3)
                    {
                        TextBox4.FontSize = (22 - Int32.Parse(Display4LineSelector.Text) * 5 + Int32.Parse(Display4LineSelector.Text));
                    }
                    else
                    {
                        TextBox4.FontSize = 9;
                    }

                    StringCollection text4 = DisplayText(TextBox4);
                    if (text4 != null)
                    {
                        LinePrompt4.Visibility = Visibility.Hidden;
                        Display4LineSelector.Visibility = Visibility.Hidden;
                        Display4LineSelector.IsEnabled = false;
                        TextBox4.IsEnabled = false;
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[3].LineCount = Int32.Parse(Display4LineSelector.Text);
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[3].Line1 = text4[0];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[3].Line2 = text4[1];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[3].Line3 = text4[2];
                        settings.FaderPresets[ActiveSliderPreset].OLEDdisplays[3].Line4 = text4[3];
                        SaveSettings();
                        ApplyDisplaySettings();
                        //ReadySendung();
                        TimesPressed4 = 0;
                    }
                    else { }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a valid number of lines (1-4)");
                }
            }
        }

        private void Display4AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[3] = true;
            SaveSettings();
            TextBox4.IsEnabled = false;
            TBButton4.IsEnabled = false;
            Display4LineSelector.Visibility = Visibility.Hidden;
            LinePrompt4.Visibility = Visibility.Hidden;
            UpdateSliderDisplayPresets();
        }

        private void Display4AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            settings.FaderPresets[ActiveSliderPreset].AutoDisplay[3] = false;
            SaveSettings();
            TextBox4.IsEnabled = true;
            TBButton4.IsEnabled = true;
            Display4LineSelector.Visibility = Visibility.Visible;
            LinePrompt4.Visibility = Visibility.Visible;
            UpdateSliderDisplayPresets();
        }

        private void OBS_SaveConnectionSettings(object sender, RoutedEventArgs e)
        {
            settings.OBSip = OBSIP_TextBox.Text;
            settings.OBSpw = OBSPassword_TextBox.Text;
            SaveSettings();
            OBSConnect();
        }

        private void AddObsSource(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                if (obs.IsConnected)
                {
                    List<string> list = new List<string>();
                    var a = obs.GetSpecialSources();
                    foreach (var b in a)
                    {
                        list.Add(b.Value);
                    }
                    ObsSourceAdd ObsAdd = new ObsSourceAdd(list);
                    ObsAdd.Closed += new EventHandler(OBSAddClosed);
                    ObsAdd.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show("OBS is not connected");
                }
            }
        }
        private void UpdateSliderObsUi()
        {
            if (SelectedFader != null && obs != null && obs.IsConnected)
            {
                List<string> list = new List<string>();
                var a = obs.GetSpecialSources();
                foreach (var b in a)
                {
                    list.Add(b.Value);
                }
                OBSListBox.Items.Clear();
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                List<string> doodoo = settings.FaderPresets[ActiveEditingSliderPreset].OBS[faderid];
                foreach (var ding in doodoo)
                {
                    if (list.Contains(ding))
                    {
                        OBSListBox.Items.Add(ding);
                    }
                }
            }
        }


        private void OBSAddClosed(object a, EventArgs eventArgs)
        {
            ObsSourceAdd window = (ObsSourceAdd)a;
            if (window.DialogResult == true)
            {
                string output = window.Output;
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].OBS[faderid].Add(output);
                SaveSettings();
            }

            UpdateSliderObsUi();
        }

        private void RemoveObsSource_Click(object sender, RoutedEventArgs e)
        {
            if (OBSListBox.SelectedItem != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingSliderPreset].OBS[faderid].RemoveAt(OBSListBox.SelectedIndex);
                SaveSettings();
                UpdateSliderObsUi();
            }
        }

        private void S1valchanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            setFader(0, (int)SliderA1.Value);
        }


        private void SaveColor_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedButton != null)
            {
                int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                settings.ButtonPresets[ActiveEditingButtonPreset].BGColors[index - 1] = Int32.Parse(BGColorTB.Text);
                settings.ButtonPresets[ActiveEditingButtonPreset].OutlineColors[index - 1] = Int32.Parse(OutlineColorTB.Text);
                settings.ButtonPresets[ActiveEditingButtonPreset].TextColors[index - 1] = Int32.Parse(TextColorTB.Text);
                SaveSettings();
                ApplyButtonSettings();
            }
        }

        private void SaveImg_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedButton != null)
            {
                int index = Int32.Parse(SelectedButton.Name.Replace("B", string.Empty));
                settings.ButtonPresets[ActiveEditingButtonPreset].ImgUrls[index - 1] = IMGTB.Text;
                SaveSettings();
                ApplyButtonSettings();
            }
        }

        private void SaveFaderColor_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFader != null)
            {
                string c = SelectedFader.Name[SelectedFader.Name.Length - 1].ToString();
                int faderid = Int32.Parse(c) - 1;
                settings.FaderPresets[ActiveEditingButtonPreset].Colors[faderid] = Int32.Parse(FaderColorTB.Text);
                SaveSettings();
                ApplyFaderSettings();
            }
        }
        void setBrighness()
        {
            
            if (settings.Brightness == -1)
            {
                settings.Brightness = 10;
                SaveSettings();
                ApplySettings();
            }
            SetBrightnessStruct bstruct = new SetBrightnessStruct();
            bstruct.command = "setBrightness";
            bstruct.brightness = settings.Brightness;
            string jsonString = JsonSerializer.Serialize(bstruct);
            post(jsonString);
            //mySerialPort.WriteLine("");
        }
        private void ChangeBrightness_FromTB(object sender, TextChangedEventArgs e)
        {
            if (LedBrightnessTB.Text == "")
            {
                return;
            }
            settings.Brightness = Int32.Parse(LedBrightnessTB.Text);
            SaveSettings();
            setBrighness();
        }
        
    }
}
internal class WindowAspectRatio
{
    private double _ratio;

    private WindowAspectRatio(System.Windows.Window window)
    {
        _ratio = window.Width / window.Height;
        ((HwndSource)HwndSource.FromVisual(window)).AddHook(DragHook);
    }

    public static void Register(System.Windows.Window window)
    {
        new WindowAspectRatio(window);
    }

    internal enum WM
    {
        WINDOWPOSCHANGING = 0x0046,
    }

    [Flags()]
    public enum SWP
    {
        NoMove = 0x2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handeled)
    {
        if ((WM)msg == WM.WINDOWPOSCHANGING)
        {
            WINDOWPOS position = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

            if ((position.flags & (int)SWP.NoMove) != 0 ||
                HwndSource.FromHwnd(hwnd).RootVisual == null) return IntPtr.Zero;

            position.cx = (int)(position.cy * _ratio);

            Marshal.StructureToPtr(position, lParam, true);
            handeled = true;
        }

        return IntPtr.Zero;
    }
}


