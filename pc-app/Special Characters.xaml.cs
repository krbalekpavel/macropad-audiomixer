using System.Windows;
using System.Windows.Input;

namespace macropad_audiomixer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SpecialCharacterWindow : Window
    {
        public SpecialCharacterWindow()
        {
            InitializeComponent();
            foreach (var item in System.Enum.GetNames(typeof(SpecialKeyCode)))
            {
                SCListBox.Items.Add(item);
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

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("[" + SCListBox.SelectedItem.ToString() + "]");
        }
    }
}
