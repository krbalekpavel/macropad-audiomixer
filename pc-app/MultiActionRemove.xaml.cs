using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MultiActionRemove : Window
    {
        public string Output { get; set; }
        public int OutputId { get; set; }
        public MultiActionRemove(List<string> lbitems)
        {
            InitializeComponent();

            MultiactionListBox.Items.Clear();
            foreach (string command in lbitems)
            {
                MultiactionListBox.Items.Add(command);
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

        public void RemovePresetClick(object sender, RoutedEventArgs e)
        {
            if (MultiactionListBox.SelectedItem != null)
            {
                Output = MultiactionListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", string.Empty);
                OutputId = MultiactionListBox.SelectedIndex;
                DialogResult = true;
                this.Close();
            }

        }

    }
}
