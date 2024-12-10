using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class RemoveApp : Window
    {
        string button;
        string ActiveEditingButtonPreset;
        public RemoveApp(string[] IndividualApps, string bttn, string ActiveEditingButtonP)
        {
            InitializeComponent();
            button = bttn;
            ActiveEditingButtonPreset = ActiveEditingButtonP;
            foreach (string a in IndividualApps)
            {
                if (a != "" && a != " ")
                {
                    ListBoxItem ItemToAdd = new ListBoxItem { Content = a };
                    AppListBox.Items.Add(ItemToAdd);
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
        private void Remove(object sender, RoutedEventArgs e)
        {
            if (AppListBox.SelectedItem != null)
            {
                AppListBox.Items.Remove(AppListBox.SelectedItem);
                string AllItemsToSave = "";
                foreach (var item in AppListBox.Items)
                {
                    AllItemsToSave = item.ToString() + "|";
                }
                AddUpdateAppSettingsB(button + "AppOpeningEnabled" + ActiveEditingButtonPreset, "true");
                AddUpdateAppSettingsB(button + "AppsToOpen" + ActiveEditingButtonPreset, AllItemsToSave.Replace("System.Windows.Controls.ListBoxItem: ", string.Empty));
                this.Close();
            }

        }
        public void AddUpdateAppSettingsB(string key, string value)
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
