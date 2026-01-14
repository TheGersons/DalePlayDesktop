using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class GestionPagosClientesPage : Page
    {
        private readonly SupabaseService _supabase;
        private List<Suscripcion> _todasSuscripciones = new();

        public GestionPagosClientesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
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
                LoadingOverlay.Visibility = Visibility.Visible;

                // Obtener todas las suscripciones activas
                var todasSuscripciones = await _supabase.ObtenerSuscripcionesAsync();
                _todasSuscripciones = todasSuscripciones.Where(s => s.Estado == "activa").ToList();

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
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ActualizarResumen()
        {
            try
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);

                // Pendientes de cobro (vencidas)
                var vencidas = _todasSuscripciones.Where(s => s.FechaProximoPago < hoy).ToList();
                PendientesCobrarTextBlock.Text = vencidas.Count.ToString();
                MontoPendienteTextBlock.Text = $"L {vencidas.Sum(s => s.Precio):N2}";

                // Vencen hoy
                var vencenHoy = _todasSuscripciones.Where(s => s.FechaProximoPago == hoy).ToList();
                VencenHoyTextBlock.Text = vencenHoy.Count.ToString();
                MontoHoyTextBlock.Text = $"L {vencenHoy.Sum(s => s.Precio):N2}";

                // Próximos 7 días
                var proximos = _todasSuscripciones.Where(s => 
                    s.FechaProximoPago > hoy && 
                    s.FechaProximoPago <= hoy.AddDays(7)).ToList();
                ProximosTextBlock.Text = proximos.Count.ToString();
                MontoProximosTextBlock.Text = $"L {proximos.Sum(s => s.Precio):N2}";

                // Cobrado hoy
                Task.Run(async () =>
                {
                    var pagos = await _supabase.ObtenerPagosAsync();
                    var pagosHoy = pagos.Where(p => p.FechaPago.Date == DateTime.Today).ToList();

                    Dispatcher.Invoke(() =>
                    {
                        CobradoHoyTextBlock.Text = $"L {pagosHoy.Sum(p => p.Monto):N2}";
                        PagosHoyTextBlock.Text = $"{pagosHoy.Count} pago{(pagosHoy.Count == 1 ? "" : "s")}";
                    });
                });
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
                var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";
                var textoBusqueda = BusquedaTextBox.Text.ToLower();

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

                // Obtener datos relacionados
                var clientes = await _supabase.ObtenerClientesAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                var suscripcionesViewModel = new List<SuscripcionCobroViewModel>();

                foreach (var suscripcion in suscripcionesFiltradas.OrderBy(s => s.FechaProximoPago))
                {
                    var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);
                    var plataforma = plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId);

                    if (cliente == null || plataforma == null) continue;

                    var diasRestantes = (suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    var vm = new SuscripcionCobroViewModel
                    {
                        Id = suscripcion.Id,
                        ClienteNombre = cliente.NombreCompleto,
                        ClienteTelefono = cliente.Telefono ?? "Sin teléfono",
                        ClienteIniciales = ObtenerIniciales(cliente.NombreCompleto),
                        PlataformaNombre = plataforma.Nombre,
                        PlataformaColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color)),
                        PrecioTexto = $"L {suscripcion.Precio:N2}",
                        FechaProximoPagoTexto = $"Próximo pago: {suscripcion.FechaProximoPago:dd/MM/yyyy}",
                        DiasRestantesTexto = ObtenerTextoDiasRestantes(diasRestantes),
                        DiasRestantesColor = ObtenerColorDiasRestantes(diasRestantes),
                        EstadoPagoTexto = ObtenerEstadoPago(diasRestantes),
                        EstadoColor = ObtenerColorEstadoPago(diasRestantes),
                        EstadoBackground = diasRestantes < 0 ? 
                            new SolidColorBrush(Color.FromRgb(255, 245, 245)) : 
                            Brushes.White
                    };

                    // Filtrar por búsqueda
                    if (!string.IsNullOrWhiteSpace(textoBusqueda))
                    {
                        var textoCompleto = $"{vm.ClienteNombre} {vm.ClienteTelefono} {vm.PlataformaNombre}".ToLower();
                        if (!textoCompleto.Contains(textoBusqueda))
                            continue;
                    }

                    suscripcionesViewModel.Add(vm);
                }

                if (suscripcionesViewModel.Any())
                {
                    SuscripcionesItemsControl.ItemsSource = suscripcionesViewModel;
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SuscripcionesItemsControl.ItemsSource = null;
                    EmptyStatePanel.Visibility = Visibility.Visible;
                }
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

        private string ObtenerIniciales(string nombreCompleto)
        {
            var palabras = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (palabras.Length >= 2)
                return $"{palabras[0][0]}{palabras[1][0]}".ToUpper();
            if (palabras.Length == 1 && palabras[0].Length >= 2)
                return palabras[0].Substring(0, 2).ToUpper();
            return "CL";
        }

        private string ObtenerTextoDiasRestantes(int dias)
        {
            if (dias < 0)
                return $"⚠️ Vencido hace {Math.Abs(dias)} día(s)";
            if (dias == 0)
                return "🔔 Vence HOY";
            if (dias == 1)
                return "⏰ Vence MAÑANA";
            
            return $"📅 Vence en {dias} días";
        }

        private Brush ObtenerColorDiasRestantes(int dias)
        {
            if (dias < 0)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rojo
            if (dias == 0)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rojo
            if (dias == 1)
                return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Naranja
            if (dias <= 7)
                return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amarillo
            
            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Verde
        }

        private string ObtenerEstadoPago(int dias)
        {
            if (dias < 0)
                return "VENCIDO";
            if (dias == 0)
                return "VENCE HOY";
            if (dias <= 3)
                return "URGENTE";
            if (dias <= 7)
                return "PRÓXIMO";
            
            return "AL DÍA";
        }

        private Brush ObtenerColorEstadoPago(int dias)
        {
            if (dias < 0)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rojo
            if (dias == 0)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rojo
            if (dias <= 3)
                return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Naranja
            if (dias <= 7)
                return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amarillo
            
            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Verde
        }

        private async void RegistrarPagoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid suscripcionId)
            {
                var suscripcion = _todasSuscripciones.FirstOrDefault(s => s.Id == suscripcionId);
                if (suscripcion == null) return;

                var clientes = await _supabase.ObtenerClientesAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);
                var plataforma = plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId);

                // Dialog simple para capturar método de pago
                var dialog = new Window
                {
                    Title = "Registrar Pago",
                    Width = 400,
                    Height = 350,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                var stack = new StackPanel { Margin = new Thickness(20) };

                stack.Children.Add(new TextBlock 
                { 
                    Text = "REGISTRAR PAGO DE CLIENTE",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                });

                stack.Children.Add(new TextBlock 
                { 
                    Text = $"Cliente: {cliente?.NombreCompleto ?? "N/A"}",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                stack.Children.Add(new TextBlock 
                { 
                    Text = $"Plataforma: {plataforma?.Nombre ?? "N/A"}",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                stack.Children.Add(new TextBlock 
                { 
                    Text = $"Monto: L {suscripcion.Precio:N2}",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    Margin = new Thickness(0, 0, 0, 20)
                });

                var metodoCombo = new ComboBox 
                { 
                    Margin = new Thickness(0, 0, 0, 10),
                    SelectedIndex = 0
                };
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Efectivo", Tag = "efectivo" });
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Transferencia", Tag = "transferencia" });
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Depósito", Tag = "deposito" });
                metodoCombo.Items.Add(new ComboBoxItem { Content = "Otro", Tag = "otro" });

                stack.Children.Add(new TextBlock { Text = "Método de Pago:", FontWeight = FontWeights.SemiBold });
                stack.Children.Add(metodoCombo);

                var referenciaText = new TextBox 
                { 
                    Margin = new Thickness(0, 10, 0, 10)
                };
                stack.Children.Add(new TextBlock { Text = "Referencia (opcional):", FontWeight = FontWeights.SemiBold });
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
                    Width = 140,
                    Height = 36,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var cancelarBtn = new Button 
                { 
                    Content = "CANCELAR",
                    Width = 100,
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
                        LoadingOverlay.Visibility = Visibility.Visible;

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
                        LoadingOverlay.Visibility = Visibility.Collapsed;
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

        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                Task.Run(async () => await AplicarFiltrosAsync());
            }
        }

        private void BusquedaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                Task.Run(async () => await AplicarFiltrosAsync());
            }
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
