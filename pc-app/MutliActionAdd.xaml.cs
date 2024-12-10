using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MultiActionAdd : Window
    {
        public string Output { get; set; }
        public MultiActionAdd()
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
            if (CommSelectBoc.SelectedItem != null)
            {
                string ToSave = "";
                bool gud = true;
                if (CommSelectBoc.SelectedItem.ToString().Contains("Keypress"))
                {
                    ToSave = "keypress";
                }
                if (CommSelectBoc.SelectedItem.ToString().Contains("Open"))
                {
                    ToSave = "open";
                }
                if (CommSelectBoc.SelectedItem.ToString().Contains("Button"))
                {
                    ToSave = "setbuttonpreset";
                }
                if (CommSelectBoc.SelectedItem.ToString().Contains("Fader"))
                {
                    ToSave = "setsliderpreset";
                }
                if (CommSelectBoc.SelectedItem.ToString().Contains("Wait"))
                {
                    ToSave = "wait";
                    //sub 5, CommandBox.Text.Length - 6
                    if (CommandBox.Text.All(char.IsDigit))
                    {
                    }
                    else
                    {
                        gud = false;
                        MessageBox.Show("Time can be only defined in seconds by numbers");
                    }
                }
                if (gud)
                {
                    Output = ToSave + "(" + CommandBox.Text + ")";
                    DialogResult = true;
                    this.Close();
                }

            }
            //if (CommandBox.Text == "" || CommandBox.Text == " " || CommandBox.Text.Contains("\n"))
            //{
            //    MessageBox.Show("Invalid input, make sure you have entered the command correctly without newlines.");
            //}
            //else
            //{
            //    if (CommandBox.Text.StartsWith("keypress") || CommandBox.Text.StartsWith("open") || CommandBox.Text.StartsWith("setbuttonpreset") || CommandBox.Text.StartsWith("setsliderpreset") || CommandBox.Text.StartsWith("wait"))
            //    {
            //        bool gud = true;
            //        if (CommandBox.Text.StartsWith("wait"))
            //        {
            //            if (CommandBox.Text.Substring(5, CommandBox.Text.Length - 6).All(char.IsDigit))
            //            {
            //                gud = true;
            //            }
            //            else
            //            {
            //                gud = false;
            //                MessageBox.Show("Time can be only defined in seconds by numbers");
            //            }
            //        }
            //        if (CommandBox.Text.EndsWith(")") == false)
            //        {
            //            gud = false;
            //            MessageBox.Show("End of command is missing a bracket");
            //        }
            //        if (gud == true)
            //        {
            //            string value;
            //            if (ReadSettingA(key) != "")
            //            {
            //                value = ReadSettingA(key) + CommandBox.Text + ",";
            //            }
            //            else
            //            {
            //                value = CommandBox.Text + ",";
            //            }
            //            AddUpdateAppSettingsA(key, value);
            //            this.Close();
            //        }

            //    }
            //    else
            //    {
            //        MessageBox.Show("Invalid input, make sure you have entered the command correctly");
            //    }

            //}

        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CommSelectBoc.SelectedItem.ToString().Contains("Keypress"))
            {
                InstructionLabel.Content = "Keys to press (specials same as in standalone function)";
            }
            if (CommSelectBoc.SelectedItem.ToString().Contains("Open"))
            {
                InstructionLabel.Content = "Path to file";
            }
            if (CommSelectBoc.SelectedItem.ToString().Contains("Button"))
            {
                InstructionLabel.Content = "Preset name";
            }
            if (CommSelectBoc.SelectedItem.ToString().Contains("Fader"))
            {
                InstructionLabel.Content = "Preset name";
            }
            if (CommSelectBoc.SelectedItem.ToString().Contains("Wait"))
            {
                InstructionLabel.Content = "Time to wait (seconds)";
            }
        }
    }
}
