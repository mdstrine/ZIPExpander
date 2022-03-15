using System.Windows;
using System.Windows.Input;

namespace ZIPExpander
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
        }

        private void CurrentItemProgBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }


    }
}
