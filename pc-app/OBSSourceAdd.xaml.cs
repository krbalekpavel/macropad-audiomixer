using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    ///


    public partial class ObsSourceAdd : Window
    {
        List<string> all = new List<string>();

        public string Output { get; set; }
        public ObsSourceAdd(List<string> allsources)
        {
            InitializeComponent();
            all = allsources;
            foreach (var a in all)
            {
                OBSSelListBox.Items.Add(a);
            }
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
            if (OBSSelListBox.SelectedItem != null)
            {
                Output = OBSSelListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty);
                DialogResult = true;
                this.Close();
            }
        }


    }
}
