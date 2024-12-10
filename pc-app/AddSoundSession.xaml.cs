using CSCore.CoreAudioAPI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AddSoundSession : Window
    {
        public string Output { get; set; }
        private readonly BackgroundWorker worker = new BackgroundWorker();
        List<AudioSessionManager2> oogabooga;
        public AddSoundSession()
        {
            InitializeComponent();
            UpdateListBox();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            UpdateListBox();
        }

        private void SavePreset1(object sender, RoutedEventArgs e)
        {
            if (AppListBox.SelectedItem != null)
            {
                Output = AppListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty);
                DialogResult = true;
                this.Close();
            }
        }

        private void UpdateListBox()
        {
            AppListBox.Items.Clear();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += BackgroundWorkerOnProgressChanged;
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.RunWorkerAsync();
            Thread.Sleep(100);
            List<AudioSessionManager2> list1 = oogabooga;
            for (int i = 0; i < list1.Count; i++)
            {
                AudioSessionManager2 sessionManager = list1[i];
                using (var sessionEnumerator1 = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session1 in sessionEnumerator1)
                    {
                        using (var sessionControl1 = session1.QueryInterface<AudioSessionControl2>())
                        {
                            if (sessionControl1.Process.ProcessName != "Idle")
                            {
                                AppListBox.Items.Add(sessionControl1.Process.ProcessName);
                            }
                        }
                    }
                }
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            oogabooga = GetDefaultAudioSessionManager2(DataFlow.Render);
            worker.Dispose();
        }
        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }
        private static List<CSCore.CoreAudioAPI.AudioSessionManager2> GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                MMDeviceCollection collection = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
                var managers = new List<CSCore.CoreAudioAPI.AudioSessionManager2>();
                for (int i3 = 0; i3 < collection.Count; i3++)
                {
                    MMDevice device = collection[i3];
                    var manager = AudioSessionManager2.FromMMDevice(device);
                    managers.Add(manager);
                }
                return managers;
            }
        }
    }
}
