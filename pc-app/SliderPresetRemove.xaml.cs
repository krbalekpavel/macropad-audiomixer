using System.Configuration;
using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SliderPresetRemove : Window
    {
        public SliderPresetRemove()
        {
            InitializeComponent();
            PresetListBox.Items.Clear();
            string presets = ReadSettingA("SliderPresets");
            string[] PresetsSplit = presets.Split(',');
            foreach (string preset in PresetsSplit)
            {
                if (preset != "")
                {
                    PresetListBox.Items.Add(preset);
                }
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }
        public string ReadSettingA(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "null";
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                return "null";
            }
        }

        private void RemovePresetClick(object sender, RoutedEventArgs e)
        {
            if (PresetListBox.SelectedItem != null)
            {
                if (PresetListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty) != "Default")
                {
                    string PresetToRemove = PresetListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty);
                    AddUpdateAppSettingsA("SliderPresets", ReadSettingA("SliderPresets").Replace("," + PresetToRemove, string.Empty));
                    for (int i = 1; i < 13; i++)
                    {
                        foreach (string buttonpreset in ReadSettingA("ButtonPresets").Split(','))
                        {
                            try
                            {
                                if (ReadSettingA("Button" + i + "ChangeSliderPresetTo" + buttonpreset) == PresetToRemove)
                                {
                                    RemoveAppSettings(ReadSettingA("Button" + i + "ChangeSliderPresetTo" + buttonpreset));
                                    AddUpdateAppSettingsA(ReadSettingA("Button" + i + "ChangeSliderEnabled" + buttonpreset), "false");
                                }
                            }
                            catch { }
                        }
                    }
                    for (int i = 1; i < 5; i++)
                    {
                        // RemoveAppSettings();
                    }
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Default preset can not be deleted");
                }

            }
            else
            {
                MessageBox.Show("Please select a preset first");
            }
        }

        public void RemoveAppSettings(string key)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                settings.Remove(key);
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch
            {
            }
        }
        public void AddUpdateAppSettingsA(string key, string value)
        {
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
        }
    }
}
