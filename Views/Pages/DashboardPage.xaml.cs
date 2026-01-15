using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StreamManager.Views.Dialogs;

namespace StreamManager.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private readonly SupabaseService _supabase;

        public DashboardPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Establecer fecha actual
                var cultura = new CultureInfo("es-ES");
                FechaTextBlock.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM yyyy", cultura);
                MesActualTextBlock.Text = DateTime.Now.ToString("MMMM yyyy", cultura);

                // Cargar datos
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var clientes = await _supabase.ObtenerClientesAsync();
                var perfiles = await _supabase.ObtenerPerfilesAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();
                var cuentas = await _supabase.ObtenerCuentasAsync();
                var alertas = await _supabase.ObtenerAlertasAsync();

                // Calcular estadísticas de suscripciones
                var suscripcionesActivas = suscripciones.Where(s => s.Estado == "activa").ToList();
                TotalSuscripcionesTextBlock.Text = suscripcionesActivas.Count.ToString();

                var suscripcionesEsteMes = suscripcionesActivas
                    .Count(s => s.FechaInicio.Month == DateTime.Now.Month && 
                               s.FechaInicio.Year == DateTime.Now.Year);
                SuscripcionesChangeTextBlock.Text = $"+{suscripcionesEsteMes} este mes";

                // Calcular ingresos mensuales
                var ingresosMensuales = suscripcionesActivas.Sum(s => s.Precio);
                IngresosMensualesTextBlock.Text = $"L {ingresosMensuales:N2}";

                // Calcular clientes
                var clientesActivos = clientes.Where(c => c.Estado == "activo").ToList();
                TotalClientesTextBlock.Text = clientesActivos.Count.ToString();
                ClientesActivosTextBlock.Text = $"{clientesActivos.Count} activos";

                // Calcular perfiles
                var perfilesDisponibles = perfiles.Count(p => p.Estado == "disponible");
                PerfilesDisponiblesTextBlock.Text = perfilesDisponibles.ToString();
                PerfilesTotalTextBlock.Text = $"De {perfiles.Count} totales";

                // Gráfico de ingresos por plataforma
                CargarIngresosPorPlataforma(suscripcionesActivas, plataformas, cuentas, perfiles);

                // Cargar alertas
                CargarAlertas(alertas.Where(a => a.Estado == "pendiente").OrderByDescending(a => a.FechaCreacion).Take(5).ToList());

                // Plataformas populares
                CargarPlataformasPopulares(suscripcionesActivas, plataformas, cuentas, perfiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar datos del dashboard: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void CargarIngresosPorPlataforma(List<Data.Models.Suscripcion> suscripciones, 
            List<Data.Models.Plataforma> plataformas, 
            List<Data.Models.CuentaCorreo> cuentas, 
            List<Data.Models.Perfil> perfiles)
        {
            var ingresosPorPlataforma = new List<IngresoPorPlataforma>();

            foreach (var plataforma in plataformas)
            {
                var cuentasPlataforma = cuentas.Where(c => c.PlataformaId == plataforma.Id).Select(c => c.Id).ToList();
                var perfilesPlataforma = perfiles.Where(p => cuentasPlataforma.Contains(p.CuentaId)).Select(p => p.Id).ToList();
                var suscripcionesPlataforma = suscripciones.Where(s => perfilesPlataforma.Contains((Guid)s.PerfilId)).ToList();

                if (suscripcionesPlataforma.Any())
                {
                    var monto = suscripcionesPlataforma.Sum(s => s.Precio);
                    ingresosPorPlataforma.Add(new IngresoPorPlataforma
                    {
                        Nombre = plataforma.Nombre,
                        Monto = monto,
                        Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color))
                    });
                }
            }

            var totalIngresos = ingresosPorPlataforma.Sum(i => i.Monto);
            foreach (var ingreso in ingresosPorPlataforma)
            {
                ingreso.Porcentaje = totalIngresos > 0 ? (double)(ingreso.Monto / totalIngresos * 100) : 0;
                ingreso.MontoTexto = $"L {ingreso.Monto:N2}";
            }

            IngresosPorPlataformaList.ItemsSource = ingresosPorPlataforma.OrderByDescending(i => i.Monto).Take(5);
        }

        private void CargarAlertas(List<Data.Models.Alerta> alertas)
        {
            var alertasVista = alertas.Select(a => new AlertaVista
            {
                Titulo = a.Mensaje,
                FechaTexto = ObtenerTextoFechaRelativa(a.FechaCreacion)
            }).ToList();

            AlertasList.ItemsSource = alertasVista;
        }

        private string ObtenerTextoFechaRelativa(DateTime fecha)
        {
            var diferencia = DateTime.Now - fecha;

            if (diferencia.TotalMinutes < 60)
                return $"Hace {(int)diferencia.TotalMinutes} minutos";
            if (diferencia.TotalHours < 24)
                return $"Hace {(int)diferencia.TotalHours} horas";
            if (diferencia.TotalDays < 7)
                return $"Hace {(int)diferencia.TotalDays} días";
            
            return fecha.ToString("dd/MM/yyyy");
        }

        private void CargarPlataformasPopulares(List<Data.Models.Suscripcion> suscripciones, 
            List<Data.Models.Plataforma> plataformas, 
            List<Data.Models.CuentaCorreo> cuentas, 
            List<Data.Models.Perfil> perfiles)
        {
            var plataformasPopulares = new List<PlataformaPopular>();

            foreach (var plataforma in plataformas)
            {
                var cuentasPlataforma = cuentas.Where(c => c.PlataformaId == plataforma.Id).Select(c => c.Id).ToList();
                var perfilesPlataforma = perfiles.Where(p => cuentasPlataforma.Contains(p.CuentaId)).Select(p => p.Id).ToList();
                var cantidadSuscripciones = suscripciones.Count(s => perfilesPlataforma.Contains((Guid)s.PerfilId));

                if (cantidadSuscripciones > 0)
                {
                    plataformasPopulares.Add(new PlataformaPopular
                    {
                        Nombre = plataforma.Nombre,
                        Icono = plataforma.Icono,
                        Suscripciones = cantidadSuscripciones,
                        SuscripcionesTexto = $"{cantidadSuscripciones} {(cantidadSuscripciones == 1 ? "suscripción" : "suscripciones")}",
                        Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color))
                    });
                }
            }

            PlataformasPopularesList.ItemsSource = plataformasPopulares.OrderByDescending(p => p.Suscripciones).Take(5);
        }

        private void VerAlertasButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página de alertas
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToPage("AlertasPage");
            }
        }

        // Clases auxiliares
        private class IngresoPorPlataforma
        {
            public string Nombre { get; set; } = string.Empty;
            public decimal Monto { get; set; }
            public double Porcentaje { get; set; }
            public string MontoTexto { get; set; } = string.Empty;
            public Brush Color { get; set; } = Brushes.Blue;
        }

        private class AlertaVista
        {
            public string Titulo { get; set; } = string.Empty;
            public string FechaTexto { get; set; } = string.Empty;
        }

        private class PlataformaPopular
        {
            public string Nombre { get; set; } = string.Empty;
            public string Icono { get; set; } = string.Empty;
            public int Suscripciones { get; set; }
            public string SuscripcionesTexto { get; set; } = string.Empty;
            public Brush Color { get; set; } = Brushes.Blue;
        }

        private void SuscripcionRapidaButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SuscripcionRapidaDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                // Recargar datos del dashboard
                _ = CargarDatosAsync();

                MessageBox.Show(
                    "✓ Dashboard actualizado con la nueva suscripción",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
