using CSCore.CoreAudioAPI;
using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AddSoundDevice : Window
    {
        public string Output { get; set; }

        public AddSoundDevice()
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
            var enumerator1 = new MMDeviceEnumerator();
            MMDeviceCollection collection11 = enumerator1.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            for (int ac1 = 0; ac1 < collection11.Count; ac1++)
            {
                var device11 = collection11[ac1];
                AppListBox.Items.Add(device11.FriendlyName);
            }
        }

    }
}
