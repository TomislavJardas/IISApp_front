using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IISApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OpenLoginWindowButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        private void OpenCountriesWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndSaveWindow countriesWindow = new ValidateAndSaveWindow();
            countriesWindow.Show();
        }

        private void OpenSOAPWindowButton_Click(object sender, RoutedEventArgs e)
        {
            SOAPWindow soapWindow = new SOAPWindow();
            soapWindow.Show();
        }
        private void OpenXRCPWindowButton_Click(object sender, RoutedEventArgs e)
        {
            CityWindow soapWindow = new CityWindow();
            soapWindow.Show();
        }
    }
}