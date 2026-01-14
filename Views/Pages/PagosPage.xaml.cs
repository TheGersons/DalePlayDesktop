using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class PagosPage : Page
    {
        private readonly SupabaseService _supabase;
        private List<Pago> _todosPagos = new();

        public PagosPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            // Establecer fechas por defecto (mes actual)
            FechaInicioDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FechaFinDatePicker.SelectedDate = DateTime.Today;

            Loaded += PagosPage_Loaded;
        }

        private async void PagosPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPagosAsync();
        }

        private async Task CargarPagosAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Obtener todos los pagos
                _todosPagos = await _supabase.ObtenerPagosAsync();

                // Aplicar filtros
                AplicarFiltros();

                // Actualizar cards de resumen
                await ActualizarResumenAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar pagos: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ActualizarResumenAsync()
        {
            try
            {
                var fechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                var fechaFin = FechaFinDatePicker.SelectedDate ?? DateTime.Today;

                var pagosPeriodo = _todosPagos.Where(p => 
                    p.FechaPago >= fechaInicio && 
                    p.FechaPago <= fechaFin.AddDays(1).AddTicks(-1)).ToList();

                // Total recaudado
                var totalRecaudado = pagosPeriodo.Sum(p => p.Monto);
                TotalRecaudadoTextBlock.Text = $"L {totalRecaudado:N2}";
                PeriodoRecaudadoTextBlock.Text = fechaInicio.Month == DateTime.Now.Month ? 
                    "Este mes" : $"{fechaInicio:MMM yyyy}";

                // Total pagos
                TotalPagosTextBlock.Text = pagosPeriodo.Count.ToString();
                PeriodoPagosTextBlock.Text = fechaInicio.Month == DateTime.Now.Month ? 
                    "Este mes" : $"{fechaInicio:MMM yyyy}";

                // Promedio
                var promedio = pagosPeriodo.Any() ? pagosPeriodo.Average(p => p.Monto) : 0;
                PromedioTextBlock.Text = $"L {promedio:N2}";

                // Último pago
                var ultimoPago = _todosPagos.OrderByDescending(p => p.FechaPago).FirstOrDefault();
                if (ultimoPago != null)
                {
                    UltimoPagoMontoTextBlock.Text = $"L {ultimoPago.Monto:N2}";
                    UltimoPagoFechaTextBlock.Text = ObtenerTextoFechaRelativa(ultimoPago.FechaPago);
                }
                else
                {
                    UltimoPagoMontoTextBlock.Text = "L 0.00";
                    UltimoPagoFechaTextBlock.Text = "Sin pagos";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar resumen: {ex.Message}");
            }
        }

        private async void AplicarFiltros()
        {
            try
            {
                var metodoFiltro = (MetodoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";
                var fechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.MinValue;
                var fechaFin = FechaFinDatePicker.SelectedDate ?? DateTime.MaxValue;
                var textoBusqueda = BusquedaTextBox.Text.ToLower();

                var pagosFiltrados = _todosPagos.AsEnumerable();

                // Filtrar por método
                if (metodoFiltro != "todos")
                {
                    pagosFiltrados = pagosFiltrados.Where(p => p.MetodoPago == metodoFiltro);
                }

                // Filtrar por fechas
                pagosFiltrados = pagosFiltrados.Where(p => 
                    p.FechaPago >= fechaInicio && 
                    p.FechaPago <= fechaFin.AddDays(1).AddTicks(-1));

                // Obtener datos relacionados
                var clientes = await _supabase.ObtenerClientesAsync();
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                var pagosViewModel = new List<PagoViewModel>();

                foreach (var pago in pagosFiltrados.OrderByDescending(p => p.FechaPago))
                {
                    var cliente = clientes.FirstOrDefault(c => c.Id == pago.ClienteId);
                    var suscripcion = suscripciones.FirstOrDefault(s => s.Id == pago.SuscripcionId);
                    var plataforma = suscripcion != null ? 
                        plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId) : null;

                    var vm = new PagoViewModel
                    {
                        Id = pago.Id,
                        FechaPagoTexto = pago.FechaPago.ToString("dd/MM/yyyy HH:mm"),
                        ClienteNombre = cliente?.NombreCompleto ?? "Cliente desconocido",
                        PlataformaNombre = plataforma?.Nombre ?? "N/A",
                        PlataformaColor = plataforma != null ? 
                            new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color)) : 
                            Brushes.Gray,
                        MontoTexto = $"L {pago.Monto:N2}",
                        MetodoPago = CapitalizarMetodo(pago.MetodoPago),
                        MetodoIcono = ObtenerIconoMetodo(pago.MetodoPago),
                        Referencia = string.IsNullOrEmpty(pago.Referencia) ? "-" : pago.Referencia
                    };

                    // Filtrar por búsqueda
                    if (!string.IsNullOrWhiteSpace(textoBusqueda))
                    {
                        var textoCompleto = $"{vm.ClienteNombre} {vm.PlataformaNombre} {vm.MontoTexto} {vm.Referencia}".ToLower();
                        if (!textoCompleto.Contains(textoBusqueda))
                            continue;
                    }

                    pagosViewModel.Add(vm);
                }

                PagosDataGrid.ItemsSource = pagosViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al aplicar filtros: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string ObtenerTextoFechaRelativa(DateTime fecha)
        {
            var diferencia = DateTime.Now - fecha;

            if (diferencia.TotalMinutes < 1)
                return "Justo ahora";
            if (diferencia.TotalMinutes < 60)
                return $"Hace {(int)diferencia.TotalMinutes} min";
            if (diferencia.TotalHours < 24)
                return $"Hace {(int)diferencia.TotalHours}h";
            if (diferencia.TotalDays < 7)
                return $"Hace {(int)diferencia.TotalDays}d";
            
            return fecha.ToString("dd/MM/yyyy");
        }

        private string CapitalizarMetodo(string metodo)
        {
            return metodo switch
            {
                "efectivo" => "Efectivo",
                "transferencia" => "Transferencia",
                "deposito" => "Depósito",
                "otro" => "Otro",
                _ => metodo
            };
        }

        private string ObtenerIconoMetodo(string metodo)
        {
            return metodo switch
            {
                "efectivo" => "Cash",
                "transferencia" => "BankTransfer",
                "deposito" => "Bank",
                "otro" => "Wallet",
                _ => "CurrencyUsd"
            };
        }

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarPagosAsync();
        }

        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }

        private void BusquedaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }

        private async void VerDetallesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid pagoId)
            {
                var pago = _todosPagos.FirstOrDefault(p => p.Id == pagoId);
                if (pago != null)
                {
                    var clientes = await _supabase.ObtenerClientesAsync();
                    var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                    var plataformas = await _supabase.ObtenerPlataformasAsync();

                    var cliente = clientes.FirstOrDefault(c => c.Id == pago.ClienteId);
                    var suscripcion = suscripciones.FirstOrDefault(s => s.Id == pago.SuscripcionId);
                    var plataforma = suscripcion != null ? 
                        plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId) : null;

                    var detalles = $"DETALLES DEL PAGO\n\n" +
                                  $"Cliente: {cliente?.NombreCompleto ?? "N/A"}\n" +
                                  $"Teléfono: {cliente?.Telefono ?? "N/A"}\n" +
                                  $"Plataforma: {plataforma?.Nombre ?? "N/A"}\n" +
                                  $"Monto: L {pago.Monto:N2}\n" +
                                  $"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}\n" +
                                  $"Método: {CapitalizarMetodo(pago.MetodoPago)}\n" +
                                  $"Referencia: {(string.IsNullOrEmpty(pago.Referencia) ? "N/A" : pago.Referencia)}\n";

                    if (!string.IsNullOrEmpty(pago.Notas))
                    {
                        detalles += $"\nNotas:\n{pago.Notas}";
                    }

                    MessageBox.Show(
                        detalles,
                        "Detalles del Pago",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        // ViewModel para pagos
        private class PagoViewModel
        {
            public Guid Id { get; set; }
            public string FechaPagoTexto { get; set; } = string.Empty;
            public string ClienteNombre { get; set; } = string.Empty;
            public string PlataformaNombre { get; set; } = string.Empty;
            public Brush PlataformaColor { get; set; } = Brushes.Gray;
            public string MontoTexto { get; set; } = string.Empty;
            public string MetodoPago { get; set; } = string.Empty;
            public string MetodoIcono { get; set; } = string.Empty;
            public string Referencia { get; set; } = string.Empty;
        }
    }
}
