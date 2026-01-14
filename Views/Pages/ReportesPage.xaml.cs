using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Pages
{
    public partial class ReportesPage : Page
    {
        private readonly SupabaseService _supabase;

        public ReportesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            // Establecer fechas por defecto (último mes)
            FechaFinDatePicker.SelectedDate = DateTime.Today;
            FechaInicioDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);

            Loaded += ReportesPage_Loaded;
        }

        private async void ReportesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarResumenAsync();
        }

        private async Task CargarResumenAsync()
        {
            try
            {
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var clientes = await _supabase.ObtenerClientesAsync();

                var suscripcionesActivas = suscripciones.Where(s => s.Estado == "activa").ToList();
                var clientesActivos = clientes.Where(c => c.Estado == "activo").ToList();

                // Ingresos totales
                var ingresosTotales = suscripcionesActivas.Sum(s => s.Precio);
                IngresosTotalesTextBlock.Text = $"L {ingresosTotales:N2}";
                PeriodoIngresosTextBlock.Text = $"{DateTime.Now:MMMM yyyy}";

                // Suscripciones
                TotalSuscripcionesTextBlock.Text = suscripciones.Count.ToString();
                SuscripcionesActivasTextBlock.Text = $"{suscripcionesActivas.Count} activas";

                // Clientes
                TotalClientesTextBlock.Text = clientes.Count.ToString();
                ClientesActivosTextBlock.Text = $"{clientesActivos.Count} activos";

                // Promedio por cliente
                var promedio = clientesActivos.Count > 0 ? ingresosTotales / clientesActivos.Count : 0;
                PromedioClienteTextBlock.Text = $"L {promedio:N2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar resumen: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void GenerarReporteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;

                var tipoReporte = (TipoReporteComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "completo";
                var fechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                var fechaFin = FechaFinDatePicker.SelectedDate ?? DateTime.Today;

                switch (tipoReporte)
                {
                    case "ingresos":
                        await GenerarReporteIngresosAsync(fechaInicio, fechaFin);
                        break;
                    case "clientes":
                        await GenerarReporteClientesAsync();
                        break;
                    case "suscripciones":
                        await GenerarReporteSuscripcionesAsync(fechaInicio, fechaFin);
                        break;
                    case "perfiles":
                        await GenerarReportePerfilesAsync();
                        break;
                    case "alertas":
                        await GenerarReporteAlertasAsync();
                        break;
                    case "completo":
                        await GenerarReporteCompletoAsync(fechaInicio, fechaFin);
                        break;
                }

                await CargarResumenAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al generar reporte: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task GenerarReporteIngresosAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            TituloTablaTextBlock.Text = "Reporte de Ingresos por Plataforma";

            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
            var plataformas = await _supabase.ObtenerPlataformasAsync();
            var cuentas = await _supabase.ObtenerCuentasAsync();
            var perfiles = await _supabase.ObtenerPerfilesAsync();

            var reporte = new List<object>();

            foreach (var plataforma in plataformas)
            {
                var cuentasPlataforma = cuentas.Where(c => c.PlataformaId == plataforma.Id).Select(c => c.Id).ToList();
                var perfilesPlataforma = perfiles.Where(p => cuentasPlataforma.Contains(p.CuentaId)).Select(p => p.Id).ToList();


                var inicioDt = new DateOnly(fechaInicio.Year, fechaInicio.Month, fechaInicio.Day);
                var finDt = new DateOnly(fechaFin.Year, fechaFin.Month, fechaFin.Day);
                var suscripcionesPlataforma = suscripciones
    .Where(s => s.PerfilId != null && // Primero validamos que no sea nulo
                perfilesPlataforma.Contains(s.PerfilId.Value) && // Ahora es seguro usar .Value
                s.FechaInicio >= inicioDt &&
                s.FechaInicio <= finDt)
    .ToList();

                if (suscripcionesPlataforma.Any())
                {
                    reporte.Add(new
                    {
                        Plataforma = plataforma.Nombre,
                        Suscripciones = suscripcionesPlataforma.Count,
                        IngresoTotal = suscripcionesPlataforma.Sum(s => s.Precio),
                        Promedio = suscripcionesPlataforma.Average(s => s.Precio)
                    });
                }
            }

            ReportesDataGrid.ItemsSource = reporte.OrderByDescending(r => ((dynamic)r).IngresoTotal);
        }

        private async Task GenerarReporteClientesAsync()
        {
            TituloTablaTextBlock.Text = "Reporte de Clientes";

            var clientes = await _supabase.ObtenerClientesAsync();
            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();

            var reporte = clientes.Select(c => new
            {
                Nombre = c.NombreCompleto,
                Telefono = c.Telefono ?? "N/A",
                Estado = c.Estado,
                Suscripciones = suscripciones.Count(s => s.ClienteId == c.Id),
                SuscripcionesActivas = suscripciones.Count(s => s.ClienteId == c.Id && s.Estado == "activa"),
                FechaRegistro = c.FechaRegistro.ToString("dd/MM/yyyy")
            }).ToList();

            ReportesDataGrid.ItemsSource = reporte;
        }

        private async Task GenerarReporteSuscripcionesAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            TituloTablaTextBlock.Text = "Reporte de Suscripciones";

            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
            var clientes = await _supabase.ObtenerClientesAsync();
            var perfiles = await _supabase.ObtenerPerfilesAsync();
            var cuentas = await _supabase.ObtenerCuentasAsync();
            var plataformas = await _supabase.ObtenerPlataformasAsync();

            var reporte = suscripciones
    .Where(s =>
    {
        // Convertimos el valor de la lista a DateTime para comparar
        DateTime fechaSuscripcionDt = s.FechaInicio.ToDateTime(TimeOnly.MinValue);

        return fechaSuscripcionDt >= fechaInicio && fechaSuscripcionDt <= fechaFin;
    })
    .Select(s =>
    {
        var cliente = clientes.FirstOrDefault(c => c.Id == s.ClienteId);
        var perfil = perfiles.FirstOrDefault(p => p.Id == s.PerfilId);
        var cuenta = perfil != null ? cuentas.FirstOrDefault(c => c.Id == perfil.CuentaId) : null;
        var plataforma = cuenta != null ? plataformas.FirstOrDefault(p => p.Id == cuenta.PlataformaId) : null;

        return new
        {
            Cliente = cliente?.NombreCompleto ?? "N/A",
            Plataforma = plataforma?.Nombre ?? "N/A",
            Perfil = perfil?.NombrePerfil ?? "N/A",
            Costo = s.Precio,
            FechaInicio = s.FechaInicio.ToString("dd/MM/yyyy"),
            ProximoPago = s.FechaProximoPago.ToString("dd/MM/yyyy"),
            Estado = s.Estado
        };
    }).ToList();

            ReportesDataGrid.ItemsSource = reporte;
        }

        private async Task GenerarReportePerfilesAsync()
        {
            TituloTablaTextBlock.Text = "Reporte de Perfiles";

            var perfiles = await _supabase.ObtenerPerfilesAsync();
            var cuentas = await _supabase.ObtenerCuentasAsync();
            var plataformas = await _supabase.ObtenerPlataformasAsync();

            var reporte = perfiles.Select(p =>
            {
                var cuenta = cuentas.FirstOrDefault(c => c.Id == p.CuentaId);
                var plataforma = cuenta != null ? plataformas.FirstOrDefault(pl => pl.Id == cuenta.PlataformaId) : null;

                return new
                {
                    Perfil = p.NombrePerfil,
                    Plataforma = plataforma?.Nombre ?? "N/A",
                    Cuenta = cuenta?.Email ?? "N/A",
                    PIN = p.Pin ?? "Sin PIN",
                    Estado = p.Estado
                };
            }).ToList();

            ReportesDataGrid.ItemsSource = reporte;
        }

        private async Task GenerarReporteAlertasAsync()
        {
            TituloTablaTextBlock.Text = "Reporte de Alertas";

            var alertas = await _supabase.ObtenerAlertasAsync();

            var reporte = alertas.Select(a => new
            {
                Tipo = a.TipoAlerta,
                Mensaje = a.Mensaje,
                Fecha = a.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                Estado = a.Estado
            }).OrderByDescending(a => a.Fecha).ToList();

            ReportesDataGrid.ItemsSource = reporte;
        }

        private async Task GenerarReporteCompletoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            TituloTablaTextBlock.Text = "Reporte Completo - Resumen General";

            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
            var clientes = await _supabase.ObtenerClientesAsync();
            var plataformas = await _supabase.ObtenerPlataformasAsync();
            var perfiles = await _supabase.ObtenerPerfilesAsync();

            var reporte = new List<object>
            {
                new { Categoría = "Clientes Totales", Cantidad = clientes.Count, Valor = "-" },
                new { Categoría = "Clientes Activos", Cantidad = clientes.Count(c => c.Estado == "activo"), Valor = "-" },
                new { Categoría = "Suscripciones Totales", Cantidad = suscripciones.Count, Valor = "-" },
                new { Categoría = "Suscripciones Activas", Cantidad = suscripciones.Count(s => s.Estado == "activa"), Valor = "-" },
                new { Categoría = "Plataformas", Cantidad = plataformas.Count, Valor = "-" },
                new { Categoría = "Perfiles Totales", Cantidad = perfiles.Count, Valor = "-" },
                new { Categoría = "Perfiles Disponibles", Cantidad = perfiles.Count(p => p.Estado == "disponible"), Valor = "-" },
                new { Categoría = "Ingresos Mensuales", Cantidad = 0, Valor = $"L {suscripciones.Where(s => s.Estado == "activa").Sum(s => s.Precio):N2}" }
            };

            ReportesDataGrid.ItemsSource = reporte;
        }

        private void ExportarPDFButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "La exportación a PDF estará disponible próximamente.\n\nPor ahora puedes usar la función de Imprimir.",
                "Función en desarrollo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExportarExcelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "La exportación a Excel estará disponible próximamente.\n\nPor ahora puedes copiar los datos directamente de la tabla.",
                "Función en desarrollo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ImprimirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(ReportesDataGrid, "Reporte StreamManager");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al imprimir: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
