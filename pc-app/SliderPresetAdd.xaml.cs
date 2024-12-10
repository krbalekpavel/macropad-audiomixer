using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SliderPresetAdd : Window
    {
        public string PassedString { get; set; }
        public SliderPresetAdd()
        {
            InitializeComponent();
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

        public void SavePreset1(object sender, RoutedEventArgs e)
        {
            bool aight = true;
            if (PresetNameBox.Text == null || PresetNameBox.Text == " ")
            {
                MessageBox.Show("Enter a valid name");
                aight = false;
            }
            else
            {
                PassedString = PresetNameBox.Text;
                DialogResult = true;
                this.Close();
            }
            /* 
             else
             {
                 foreach (string preset in presets)
                 {
                     if (preset == PresetNameBox.Text)
                     {
                         MessageBox.Show("A preset with this name already exists");
                         aight = false;
                     }
                 }
             }
             if (aight == true)
             {
                 string value = ReadSettingA("SliderPresets") + "," + PresetNameBox.Text;
                 try
                 {
                     var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                     var settings = configFile.AppSettings.Settings;
                     if (settings[key] == null)
                     {
                         settings.Add(key, value);
                     }
                     else
                     {
                         settings[key].Value = value;
                     }
                     configFile.Save(ConfigurationSaveMode.Modified);
                     ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                 }
                 catch (ConfigurationErrorsException)
                 {
                 }
                 this.Close();

             }*/
        }


    }
}
