using System.Windows;
using VMTest.Tests;

namespace ErrorTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TestVMMonitor.VMDataErrorInfo _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = App.VM;
            DataContext = _vm;
        }

        private void OnErrorInA(object sender, RoutedEventArgs e)
        {
            _vm.SetError("A", "Bad");
        }

        private void OnErrorInB(object sender, RoutedEventArgs e)
        {
            _vm.SetError("B", "Bad");
        }

        private void OnErrorInC(object sender, RoutedEventArgs e)
        {
            _vm.SetError("C", "Bad");
        }

        private void OnErrorInD(object sender, RoutedEventArgs e)
        {
            _vm.SetError("D", "Bad");
        }
    }
}
