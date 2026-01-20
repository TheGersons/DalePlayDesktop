// ========================================
// MainWindow.xaml.cs - CON TRIGGERS DE BD
// ========================================

using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using StreamManager.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;

namespace StreamManager
{
    public partial class MainWindow : Window
    {
        private readonly SupabaseService _supabase;
        private readonly AlertaService _alertaService;
        private DispatcherTimer? _timerAlertas;

        public MainWindow()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _alertaService = App.ServiceProvider?.GetRequiredService<AlertaService>()
                ?? throw new InvalidOperationException("AlertaService no disponible");

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ NUEVO: Ejecutar mantenimiento completo al abrir
            await EjecutarMantenimientoCompletoAsync();

            // ✅ NUEVO: Configurar timer para mantenimiento cada 30 minutos
            ConfigurarTimerAlertas();

            // Navegar a dashboard
            MainFrame.Navigate(new Uri("Views/Pages/DashboardPage.xaml", UriKind.Relative));
            PageTitleTextBlock.Text = "/ Dashboard";
        }

        /// <summary>
        /// Ejecuta mantenimiento completo: BD + App
        /// </summary>
        private async Task EjecutarMantenimientoCompletoAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] ⚡ Iniciando mantenimiento automático...");

                await Task.Run(async () =>
                {
                    // 1. Ejecutar función de BD para actualizar estados y limpiar
                    await EjecutarMantenimientoBDAsync();

                    // 2. Generar alertas desde la app (esto usa la lógica de C#)
                    await _alertaService.GenerarAlertasAutomaticasAsync();
                });

                System.Diagnostics.Debug.WriteLine("[MainWindow] ✓ Mantenimiento completado");

                // 3. Actualizar badge de alertas
                if (AlertasBadge != null)
                {
                    await AlertasBadge.ActualizarContadorAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ✗ Error en mantenimiento: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecuta la función de mantenimiento en la BD
        /// </summary>
        private async Task EjecutarMantenimientoBDAsync()
        {
            try
            {
                var client = _supabase.GetClient();

                // Ejecutar función de BD que actualiza estados y limpia alertas
                await client.Rpc("mantenimiento_automatico_alertas", null);

                System.Diagnostics.Debug.WriteLine("[MainWindow] ✓ Mantenimiento BD ejecutado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ⚠️ Mantenimiento BD omitido: {ex.Message}");
                // No es crítico si falla, la app puede generar alertas igual
            }
        }

        /// <summary>
        /// Configura timer para mantenimiento cada 30 minutos
        /// </summary>
        private void ConfigurarTimerAlertas()
        {
            _timerAlertas = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(30)
            };

            _timerAlertas.Tick += async (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] ⏰ Timer: Ejecutando mantenimiento programado...");
                await EjecutarMantenimientoCompletoAsync();
            };

            _timerAlertas.Start();

            System.Diagnostics.Debug.WriteLine("[MainWindow] ⏰ Timer configurado: cada 30 minutos");
        }

        // ========================================
        // RESTO DEL CÓDIGO EXISTENTE
        // ========================================

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string pageName)
                return;

            try
            {
                var uri = new Uri($"Views/Pages/{pageName}.xaml", UriKind.Relative);
                MainFrame.Navigate(uri);

                var displayName = pageName switch
                {
                    "DashboardPage" => "Dashboard",
                    "PlataformasPage" => "Plataformas",
                    "CuentasPage" => "Cuentas de Correo",
                    "PerfilesPage" => "Perfiles",
                    "ClientesPage" => "Clientes",
                    "SuscripcionesPage" => "Suscripciones",
                    "GestionPagosClientesPage" => "Pagos Clientes",
                    "PagosPlataformaPage" => "Pago a Proveedores",
                    "ReportesPage" => "Reportes y Métricas",
                    "PagosPage" => "Historial de Pagos",
                    "AlertasPage" => "Alertas",
                    "ConfiguracionPage" => "Configuraciones",
                    _ => pageName
                };

                PageTitleTextBlock.Text = $"/ {displayName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al navegar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void AlertasBadge_AlertasButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var uri = new Uri("Views/Pages/AlertasPage.xaml", UriKind.Relative);
                MainFrame.Navigate(uri);
                PageTitleTextBlock.Text = "/ Alertas";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al abrir alertas: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CerrarSesionButton_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                // ✅ NUEVO: Detener timer al cerrar sesión
                _timerAlertas?.Stop();

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }
        public async Task RefrescarAlertasAsync()
        {
            // Reutilizamos la lógica completa que ya tienes programada
            await EjecutarMantenimientoCompletoAsync();
        }

        public void NavigateToPage(string pageName)
        {
            string pagePath = "";
            string title = "";

            // 1. Definimos la ruta del archivo XAML según el nombre que recibimos
            switch (pageName)
            {
                case "GestionPagosClientesPage":
                    pagePath = "Views/Pages/GestionPagosClientesPage.xaml";
                    title = "/ Gestión de Pagos Clientes";
                    break;

                case "PagosPlataformaPage":
                    pagePath = "Views/Pages/PagosPlataformaPage.xaml";
                    title = "/ Pagos a Plataformas";
                    break;

                case "AlertasPage":
                    pagePath = "Views/Pages/AlertasPage.xaml";
                    title = "/ Alertas";
                    break;

                // Agrega aquí más páginas si las necesitas en el futuro
                case "DashboardPage":
                    pagePath = "Views/Pages/DashboardPage.xaml";
                    title = "/ Dashboard";
                    break;

                default:
                    MessageBox.Show($"Página no encontrada: {pageName}", "Error de Navegación");
                    return;
            }

            // 2. Ejecutamos la navegación
            try
            {
                var uri = new Uri(pagePath, UriKind.Relative);
                MainFrame.Navigate(uri);
                PageTitleTextBlock.Text = title;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo navegar a {pagePath}.\nError: {ex.Message}",
                    "Error Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

    } // Fin de la clase MainWindow
} // Fin del namespace
