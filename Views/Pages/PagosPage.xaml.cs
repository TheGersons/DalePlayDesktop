using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class PagosPage : Page
    {
        private readonly SupabaseService _supabase;
        private List<Pago> _todosPagos = new();
        private List<Cliente> _todosClientes = new();
        private List<Plataforma> _todasPlataformas = new();

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

                // Cargar clientes y plataformas para filtros
                _todosClientes = await _supabase.ObtenerClientesAsync();
                _todasPlataformas = await _supabase.ObtenerPlataformasAsync();
                CargarFiltros();

                // Aplicar filtros
                await AplicarFiltrosAsync();

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

        private void CargarFiltros()
        {
            // Cargar clientes
            var clientesParaFiltro = new List<Cliente> { new Cliente { Id = Guid.Empty, NombreCompleto = "Todos los clientes" } };
            clientesParaFiltro.AddRange(_todosClientes.OrderBy(c => c.NombreCompleto));
            ClienteFiltroComboBox.ItemsSource = clientesParaFiltro;
            ClienteFiltroComboBox.SelectedIndex = 0;

            // Cargar plataformas
            var plataformasParaFiltro = new List<Plataforma> { new Plataforma { Id = Guid.Empty, Nombre = "Todas las plataformas" } };
            plataformasParaFiltro.AddRange(_todasPlataformas.Where(p => p.Estado == "activa").OrderBy(p => p.Nombre));
            PlataformaFiltroComboBox.ItemsSource = plataformasParaFiltro;
            PlataformaFiltroComboBox.SelectedIndex = 0;
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

        private async Task AplicarFiltrosAsync()
        {
            try
            {
                var textoBusqueda = BusquedaTextBox.Text?.ToLower() ?? "";
                var clienteSeleccionado = ClienteFiltroComboBox.SelectedItem as Cliente;
                var plataformaSeleccionada = PlataformaFiltroComboBox.SelectedItem as Plataforma;
                var metodoFiltro = (MetodoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";
                var fechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.MinValue;
                var fechaFin = FechaFinDatePicker.SelectedDate ?? DateTime.MaxValue;

                // Parsear montos
                decimal? montoDesde = null;
                decimal? montoHasta = null;
                if (decimal.TryParse(MontoDesdeTextBox.Text, out decimal mDesde))
                    montoDesde = mDesde;
                if (decimal.TryParse(MontoHastaTextBox.Text, out decimal mHasta))
                    montoHasta = mHasta;

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

                // Filtrar por rango de montos
                if (montoDesde.HasValue)
                    pagosFiltrados = pagosFiltrados.Where(p => p.Monto >= montoDesde.Value);
                if (montoHasta.HasValue)
                    pagosFiltrados = pagosFiltrados.Where(p => p.Monto <= montoHasta.Value);

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

                    if (cliente == null) continue;

                    // Filtro por cliente específico
                    if (clienteSeleccionado != null && clienteSeleccionado.Id != Guid.Empty)
                    {
                        if (cliente.NombreCompleto != clienteSeleccionado.NombreCompleto)
                            continue;
                    }

                    // Filtro por plataforma específica
                    if (plataformaSeleccionada != null && plataformaSeleccionada.Id != Guid.Empty)
                    {
                        if (plataforma == null || plataforma.Nombre != plataformaSeleccionada.Nombre)
                            continue;
                    }

                    Brush plataformaColor = Brushes.Gray;
                    if (plataforma != null)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(plataforma.Color))
                                plataformaColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color));
                        }
                        catch { }
                    }

                    var vm = new PagoViewModel
                    {
                        Id = pago.Id,
                        FechaPagoTexto = pago.FechaPago.ToString("dd/MM/yyyy HH:mm"),
                        ClienteNombre = cliente.NombreCompleto,
                        ClienteTelefono = cliente.Telefono ?? "-",
                        PlataformaNombre = plataforma?.Nombre ?? "N/A",
                        PlataformaColor = plataformaColor,
                        MontoTexto = $"L {pago.Monto:N2}",
                        MetodoPago = CapitalizarMetodo(pago.MetodoPago),
                        MetodoIcono = ObtenerIconoMetodo(pago.MetodoPago),
                        Referencia = string.IsNullOrEmpty(pago.Referencia) ? "-" : pago.Referencia
                    };

                    // Filtrar por búsqueda general
                    if (!string.IsNullOrWhiteSpace(textoBusqueda))
                    {
                        var textoCompleto = $"{vm.ClienteNombre} {vm.ClienteTelefono} {vm.PlataformaNombre} {vm.MontoTexto} {vm.Referencia}".ToLower();
                        if (!textoCompleto.Contains(textoBusqueda))
                            continue;
                    }

                    pagosViewModel.Add(vm);
                }

                PagosDataGrid.ItemsSource = pagosViewModel;
                ActualizarContadorResultados(pagosViewModel.Count);
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

        private void ActualizarContadorResultados(int cantidad)
        {
            if (ResultadosTextBlock != null)
            {
                ResultadosTextBlock.Text = cantidad == 1
                    ? "1 resultado"
                    : $"{cantidad} resultados";
            }
        }

        private void LimpiarFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BusquedaTextBox.Text = string.Empty;
            ClienteFiltroComboBox.SelectedIndex = 0;
            PlataformaFiltroComboBox.SelectedIndex = 0;
            MetodoFiltroComboBox.SelectedIndex = 0;
            FechaInicioDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FechaFinDatePicker.SelectedDate = DateTime.Today;
            MontoDesdeTextBox.Text = string.Empty;
            MontoHastaTextBox.Text = string.Empty;

            Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await AplicarFiltrosAsync();
                });
            });
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

        private void MontoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permite números y punto decimal
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarPagosAsync();
        }

        private async void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await AplicarFiltrosAsync();
            }
        }

        private async void BusquedaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await AplicarFiltrosAsync();
            }
        }

        private async void MontoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await AplicarFiltrosAsync();
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
            public string ClienteTelefono { get; set; } = string.Empty;
            public string PlataformaNombre { get; set; } = string.Empty;
            public Brush PlataformaColor { get; set; } = Brushes.Gray;
            public string MontoTexto { get; set; } = string.Empty;
            public string MetodoPago { get; set; } = string.Empty;
            public string MetodoIcono { get; set; } = string.Empty;
            public string Referencia { get; set; } = string.Empty;
        }
    }
}