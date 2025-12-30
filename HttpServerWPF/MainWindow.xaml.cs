using System.Windows;
using System.Windows.Threading;

namespace HttpServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LogTextBox.TextChanged += (_, __) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogTextBox.ScrollToEnd();
                }), DispatcherPriority.Background);
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}