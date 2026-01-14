using StreamManager.Views.Pages;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Navegar a Dashboard al iniciar
            NavigateToPage("DashboardPage");
        }

        public void NavigateToPage(string pageName)
        {
            Page? page = pageName switch
            {
                "DashboardPage" => new DashboardPage(),
                "PlataformasPage" => new PlataformasPage(),
                "ClientesPage" => new ClientesPage(),
                "CuentasPage" => new CuentasPage(),
                "PerfilesPage" => new PerfilesPage(),
                "SuscripcionesPage" => new SuscripcionesPage(),
                "AlertasPage" => new AlertasPage(),
                "ReportesPage" => new ReportesPage(),
                "ConfiguracionPage" => new ConfiguracionPage(),
                "PagosPlataformaPage" => new PagosPlataformaPage(),
                "PagosPage" => new PagosPage(),
                "GestionPagosClientesPage" => new GestionPagosClientesPage(),
                _ => null
            };

            if (page != null)
            {
                MainFrame.Navigate(page);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageName)
            {
                NavigateToPage(pageName);
            }
        }

        private void CerrarSesionButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Estás seguro de cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();
                Close();
            }
        }

        private void AlertasBadge_AlertasButtonClicked(object? sender, EventArgs e)
        {
            // Navegar a la página de alertas cuando se hace click en el badge
            NavigateToPage("AlertasPage");
        }

        public async Task RefrescarAlertasAsync()
        {
            // Método para refrescar el contador de alertas desde otras páginas
            await AlertasBadge.RefrescarAsync();
        }
    }
}