using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class GestionPagosClientesPage : Page
    {
        private readonly SupabaseService _supabase;
        private List<Suscripcion> _todasSuscripciones = new();
        private List<Plataforma> _todasPlataformas = new();

        public GestionPagosClientesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            Loaded += GestionPagosClientesPage_Loaded;
        }

        private async void GestionPagosClientesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarSuscripcionesAsync();
        }

        private async Task CargarSuscripcionesAsync()
        {
            try
            {
                if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Visible;

                // Obtener todas las suscripciones activas
                var todasSuscripciones = await _supabase.ObtenerSuscripcionesAsync();
                _todasSuscripciones = todasSuscripciones.Where(s => s.Estado == "activa").ToList();

                // Cargar plataformas para filtro
                var plataformas = await _supabase.ObtenerPlataformasAsync();
                _todasPlataformas = plataformas.Where(p => p.Estado == "activa").ToList();
                CargarFiltros();

                // Aplicar filtros
                await AplicarFiltrosAsync();

                // Actualizar cards de resumen
                ActualizarResumen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar suscripciones: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void CargarFiltros()
        {
            var plataformasParaFiltro = new List<Plataforma> { new Plataforma { Id = Guid.Empty, Nombre = "Todas las plataformas" } };
            plataformasParaFiltro.AddRange(_todasPlataformas.OrderBy(p => p.Nombre));
            PlataformaFiltroComboBox.ItemsSource = plataformasParaFiltro;
            PlataformaFiltroComboBox.SelectedIndex = 0;
        }

        private void ActualizarResumen()
        {
            try
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);

                // Pendientes de cobro (vencidas)
                var vencidas = _todasSuscripciones.Where(s => s.FechaProximoPago < hoy).ToList();
                if (PendientesCobrarTextBlock != null) PendientesCobrarTextBlock.Text = vencidas.Count.ToString();
                if (MontoPendienteTextBlock != null) MontoPendienteTextBlock.Text = $"L {vencidas.Sum(s => s.Precio):N2}";

                // Vencen hoy
                var vencenHoy = _todasSuscripciones.Where(s => s.FechaProximoPago == hoy).ToList();
                if (VencenHoyTextBlock != null) VencenHoyTextBlock.Text = vencenHoy.Count.ToString();
                if (MontoHoyTextBlock != null) MontoHoyTextBlock.Text = $"L {vencenHoy.Sum(s => s.Precio):N2}";

                // Próximos 7 días
                var proximos = _todasSuscripciones.Where(s =>
                    s.FechaProximoPago > hoy &&
                    s.FechaProximoPago <= hoy.AddDays(7)).ToList();

                if (ProximosTextBlock != null) ProximosTextBlock.Text = proximos.Count.ToString();
                if (MontoProximosTextBlock != null) MontoProximosTextBlock.Text = $"L {proximos.Sum(s => s.Precio):N2}";

                // Cobrado hoy
                Task.Run(async () =>
                {
                    try
                    {
                        var pagos = await _supabase.ObtenerPagosAsync();
                        var pagosHoy = pagos.Where(p => p.FechaPago.Date == DateTime.Today).ToList();

                        Dispatcher.Invoke(() =>
                        {
                            if (CobradoHoyTextBlock != null) CobradoHoyTextBlock.Text = $"L {pagosHoy.Sum(p => p.Monto):N2}";
                            if (PagosHoyTextBlock != null) PagosHoyTextBlock.Text = $"{pagosHoy.Count} pago{(pagosHoy.Count == 1 ? "" : "s")}";
                        });
                    }
                    catch { /* Silenciar error de resumen si falla */ }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar resumen: {ex.Message}");
            }
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

        private async void FechaFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await AplicarFiltrosAsync();
            }
        }

        private void LimpiarFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BusquedaTextBox.Text = string.Empty;
            PlataformaFiltroComboBox.SelectedIndex = 0;
            EstadoFiltroComboBox.SelectedIndex = 0;
            FechaDesdeFilterPicker.SelectedDate = null;
            FechaHastaFilterPicker.SelectedDate = null;

            Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await AplicarFiltrosAsync();
                });
            });
        }

        private async Task AplicarFiltrosAsync()
        {
            try
            {
                if (EstadoFiltroComboBox == null || BusquedaTextBox == null) return;

                var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";
                var textoBusqueda = BusquedaTextBox.Text?.ToLower() ?? "";
                var plataformaSeleccionada = PlataformaFiltroComboBox.SelectedItem as Plataforma;
                var fechaDesde = FechaDesdeFilterPicker.SelectedDate;
                var fechaHasta = FechaHastaFilterPicker.SelectedDate;

                var suscripcionesFiltradas = _todasSuscripciones.AsEnumerable();
                var hoy = DateOnly.FromDateTime(DateTime.Today);

                // Filtrar por estado
                suscripcionesFiltradas = estadoFiltro switch
                {
                    "vencidos" => suscripcionesFiltradas.Where(s => s.FechaProximoPago < hoy),
                    "hoy" => suscripcionesFiltradas.Where(s => s.FechaProximoPago == hoy),
                    "proximos" => suscripcionesFiltradas.Where(s =>
                        s.FechaProximoPago > hoy &&
                        s.FechaProximoPago <= hoy.AddDays(7)),
                    "activa" => suscripcionesFiltradas.Where(s => s.Estado == "activa"),
                    _ => suscripcionesFiltradas
                };

                // Filtrar por rango de fechas
                if (fechaDesde.HasValue)
                {
                    var desde = DateOnly.FromDateTime(fechaDesde.Value);
                    suscripcionesFiltradas = suscripcionesFiltradas.Where(s => s.FechaProximoPago >= desde);
                }

                if (fechaHasta.HasValue)
                {
                    var hasta = DateOnly.FromDateTime(fechaHasta.Value);
                    suscripcionesFiltradas = suscripcionesFiltradas.Where(s => s.FechaProximoPago <= hasta);
                }

                // Obtener datos relacionados
                var clientes = await _supabase.ObtenerClientesAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                var suscripcionesViewModel = new List<SuscripcionCobroViewModel>();

                foreach (var suscripcion in suscripcionesFiltradas.OrderBy(s => s.FechaProximoPago))
                {
                    var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);
                    var plataforma = plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId);

                    if (cliente == null || plataforma == null) continue;

                    // Filtro de búsqueda por cliente
                    if (!string.IsNullOrWhiteSpace(textoBusqueda))
                    {
                        if (!cliente.NombreCompleto.ToLower().Contains(textoBusqueda) &&
                            !(cliente.Telefono?.Contains(textoBusqueda) ?? false))
                            continue;
                    }

                    // Filtro por plataforma
                    if (plataformaSeleccionada != null && plataformaSeleccionada.Id != Guid.Empty)
                    {
                        if (plataforma.Nombre != plataformaSeleccionada.Nombre)
                            continue;
                    }

                    var diasRestantes = (suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    Brush colorPlataforma;
                    try
                    {
                        if (!string.IsNullOrEmpty(plataforma.Color))
                            colorPlataforma = new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color));
                        else
                            colorPlataforma = Brushes.Gray;
                    }
                    catch
                    {
                        colorPlataforma = Brushes.Gray;
                    }

                    var iniciales = string.Join("", cliente.NombreCompleto.Split(' ')
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Take(2)
                        .Select(p => p[0].ToString().ToUpper()));

                    string estadoPagoTexto;
                    Brush estadoColor;
                    Brush estadoBackground;

                    if (diasRestantes < 0)
                    {
                        estadoPagoTexto = "VENCIDO";
                        estadoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                        estadoBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    }
                    else if (diasRestantes == 0)
                    {
                        estadoPagoTexto = "VENCE HOY";
                        estadoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                        estadoBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                    }
                    else if (diasRestantes <= 3)
                    {
                        estadoPagoTexto = "PRÓXIMO";
                        estadoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                        estadoBackground = Brushes.White;
                    }
                    else
                    {
                        estadoPagoTexto = "ACTIVA";
                        estadoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        estadoBackground = Brushes.White;
                    }

                    var diasRestantesColor = diasRestantes < 0 ? estadoColor :
                                            diasRestantes == 0 ? estadoColor :
                                            diasRestantes <= 3 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")) :
                                            Brushes.Gray;

                    var diasTexto = diasRestantes < 0 ? $"{Math.Abs(diasRestantes)} días vencido" :
                                   diasRestantes == 0 ? "Vence hoy" :
                                   diasRestantes == 1 ? "1 día" :
                                   $"{diasRestantes} días";

                    suscripcionesViewModel.Add(new SuscripcionCobroViewModel
                    {
                        Id = suscripcion.Id,
                        ClienteNombre = cliente.NombreCompleto,
                        ClienteTelefono = cliente.Telefono ?? "",
                        ClienteIniciales = iniciales,
                        PlataformaNombre = plataforma.Nombre,
                        PlataformaColor = colorPlataforma,
                        PrecioTexto = $"L {suscripcion.Precio:N2}",
                        FechaProximoPagoTexto = $"📅 {suscripcion.FechaProximoPago:dd/MM/yyyy}",
                        DiasRestantesTexto = diasTexto,
                        DiasRestantesColor = diasRestantesColor,
                        EstadoPagoTexto = estadoPagoTexto,
                        EstadoColor = estadoColor,
                        EstadoBackground = estadoBackground
                    });
                }

                if (SuscripcionesItemsControl != null)
                    SuscripcionesItemsControl.ItemsSource = suscripcionesViewModel;

                if (EmptyStatePanel != null)
                    EmptyStatePanel.Visibility = suscripcionesViewModel.Any() ? Visibility.Collapsed : Visibility.Visible;

                ActualizarContadorResultados(suscripcionesViewModel.Count);
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

        private async void RegistrarPagoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid suscripcionId)
            {
                var suscripcion = _todasSuscripciones.FirstOrDefault(s => s.Id == suscripcionId);
                if (suscripcion == null) return;

                var clientes = await _supabase.ObtenerClientesAsync();
                var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);

                var dialog = new Window
                {
                    Title = "Registrar Pago",
                    Width = 450,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.NoResize
                };

                var stack = new StackPanel { Margin = new Thickness(20) };

                var tituloText = new TextBlock
                {
                    Text = "REGISTRAR PAGO",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                stack.Children.Add(tituloText);

                var clienteText = new TextBlock
                {
                    Text = $"Cliente: {cliente?.NombreCompleto ?? "N/A"}",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stack.Children.Add(clienteText);

                var montoText = new TextBlock
                {
                    Text = $"Monto: L {suscripcion.Precio:N2}",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                stack.Children.Add(montoText);

                var metodoLabel = new TextBlock
                {
                    Text = "Método de Pago:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stack.Children.Add(metodoLabel);

                var metodoCombo = new ComboBox
                {
                    Margin = new Thickness(0, 0, 0, 16),
                    SelectedIndex = 0
                };
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Efectivo", Tag = "efectivo", IsSelected = true });
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Transferencia", Tag = "transferencia" });
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Depósito", Tag = "deposito" });
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Otro", Tag = "otro" });
                stack.Children.Add(metodoCombo);

                var referenciaLabel = new TextBlock
                {
                    Text = "Referencia (opcional):",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stack.Children.Add(referenciaLabel);

                var referenciaText = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 20)
                };
                stack.Children.Add(referenciaText);

                var buttonStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 20, 0, 0)
                };

                var confirmarBtn = new Button
                {
                    Content = "CONFIRMAR PAGO",
                    Width = 160,
                    Height = 36,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                var cancelarBtn = new Button
                {
                    Content = "CANCELAR",
                    Width = 110,
                    Height = 36
                };

                buttonStack.Children.Add(confirmarBtn);
                buttonStack.Children.Add(cancelarBtn);
                stack.Children.Add(buttonStack);

                dialog.Content = stack;

                bool? resultado = null;
                confirmarBtn.Click += (s, args) => { resultado = true; dialog.Close(); };
                cancelarBtn.Click += (s, args) => { resultado = false; dialog.Close(); };

                dialog.ShowDialog();

                if (resultado == true)
                {
                    try
                    {
                        if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Visible;

                        var metodo = ((ComboBoxItem)metodoCombo.SelectedItem).Tag.ToString();
                        var referencia = referenciaText.Text;

                        // Registrar pago
                        var pago = new Pago
                        {
                            Id = Guid.NewGuid(),
                            SuscripcionId = suscripcion.Id,
                            ClienteId = suscripcion.ClienteId,
                            Monto = suscripcion.Precio,
                            FechaPago = DateTime.Now,
                            MetodoPago = metodo,
                            Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia,
                            Notas = $"Pago registrado por sistema - Renovación mensual"
                        };

                        await _supabase.CrearPagoAsync(pago);

                        var alertaService = App.ServiceProvider?.GetRequiredService<AlertaService>();
                        if (alertaService != null)
                        {
                            await alertaService.ResolverAlertasCobroClienteAsync(suscripcion.Id);
                        }

                        // Renovar suscripción
                        suscripcion.FechaProximoPago = suscripcion.FechaProximoPago.AddMonths(1);
                        suscripcion.FechaLimitePago = suscripcion.FechaProximoPago.AddDays(5);

                        await _supabase.ActualizarSuscripcionAsync(suscripcion);

                        MessageBox.Show(
                            $"✓ Pago registrado exitosamente\n\n" +
                            $"Monto: L {pago.Monto:N2}\n" +
                            $"Método: {metodo}\n\n" +
                            $"Próximo pago: {suscripcion.FechaProximoPago:dd/MM/yyyy}",
                            "Pago Registrado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al registrar pago: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async void VerDetallesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid suscripcionId)
            {
                var suscripcion = _todasSuscripciones.FirstOrDefault(s => s.Id == suscripcionId);
                if (suscripcion == null) return;

                var clientes = await _supabase.ObtenerClientesAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();
                var perfiles = await _supabase.ObtenerPerfilesAsync();
                var pagos = await _supabase.ObtenerPagosAsync();

                var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);
                var plataforma = plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId);
                var perfil = suscripcion.PerfilId.HasValue ?
                    perfiles.FirstOrDefault(p => p.Id == suscripcion.PerfilId) : null;

                var pagosSuscripcion = pagos.Where(p => p.SuscripcionId == suscripcion.Id)
                    .OrderByDescending(p => p.FechaPago)
                    .Take(5)
                    .ToList();

                var mensaje = $"DETALLES DE SUSCRIPCIÓN\n\n";
                mensaje += $"Cliente: {cliente?.NombreCompleto ?? "N/A"}\n";
                mensaje += $"Teléfono: {cliente?.Telefono ?? "N/A"}\n";
                mensaje += $"Plataforma: {plataforma?.Nombre ?? "N/A"}\n";
                mensaje += $"Perfil: {perfil?.NombrePerfil ?? "Cuenta completa"}\n";
                mensaje += $"Precio: L {suscripcion.Precio:N2}\n";
                mensaje += $"Fecha inicio: {suscripcion.FechaInicio:dd/MM/yyyy}\n";
                mensaje += $"Próximo pago: {suscripcion.FechaProximoPago:dd/MM/yyyy}\n";
                mensaje += $"Fecha límite: {suscripcion.FechaLimitePago:dd/MM/yyyy}\n";
                mensaje += $"Estado: {suscripcion.Estado}\n\n";

                if (pagosSuscripcion.Any())
                {
                    mensaje += $"═══════════════════════\n";
                    mensaje += $"ÚLTIMOS PAGOS:\n";
                    mensaje += $"═══════════════════════\n\n";

                    foreach (var pago in pagosSuscripcion)
                    {
                        mensaje += $"📅 {pago.FechaPago:dd/MM/yyyy HH:mm}\n";
                        mensaje += $"   L {pago.Monto:N2} - {pago.MetodoPago}\n";
                        if (!string.IsNullOrEmpty(pago.Referencia))
                            mensaje += $"   Ref: {pago.Referencia}\n";
                        mensaje += $"\n";
                    }
                }

                MessageBox.Show(
                    mensaje,
                    "Detalles de Suscripción",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarSuscripcionesAsync();
        }

        // ViewModel para suscripciones de cobro
        private class SuscripcionCobroViewModel
        {
            public Guid Id { get; set; }
            public string ClienteNombre { get; set; } = string.Empty;
            public string ClienteTelefono { get; set; } = string.Empty;
            public string ClienteIniciales { get; set; } = string.Empty;
            public string PlataformaNombre { get; set; } = string.Empty;
            public Brush PlataformaColor { get; set; } = Brushes.Gray;
            public string PrecioTexto { get; set; } = string.Empty;
            public string FechaProximoPagoTexto { get; set; } = string.Empty;
            public string DiasRestantesTexto { get; set; } = string.Empty;
            public Brush DiasRestantesColor { get; set; } = Brushes.Gray;
            public string EstadoPagoTexto { get; set; } = string.Empty;
            public Brush EstadoColor { get; set; } = Brushes.Gray;
            public Brush EstadoBackground { get; set; } = Brushes.White;
        }
    }
}