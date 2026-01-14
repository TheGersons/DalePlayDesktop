using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class PagosPlataformaPage : Page
    {
        private readonly SupabaseService _supabase;
        private List<PagoPlataforma> _todosPagosPlataf = new();

        public PagosPlataformaPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            Loaded += PagosPlataformaPage_Loaded;
        }

        private async void PagosPlataformaPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPagosPlataformaAsync();
        }

        private async Task CargarPagosPlataformaAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Obtener todos los pagos a plataformas
                _todosPagosPlataf = await _supabase.ObtenerPagosPlataformaAsync();

                // Aplicar filtros
                AplicarFiltros();

                // Actualizar cards de resumen
                ActualizarResumen();
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

        private void ActualizarResumen()
        {
            try
            {
                var vencidos = _todosPagosPlataf.Where(p => p.Estado == "vencido").ToList();
                var porPagar = _todosPagosPlataf.Where(p => p.Estado == "por_pagar").ToList();
                var alDia = _todosPagosPlataf.Where(p => p.Estado == "al_dia").ToList();

                // Vencidos
                PagosVencidosTextBlock.Text = vencidos.Count.ToString();
                MontoVencidoTextBlock.Text = $"L {vencidos.Sum(p => p.MontoMensual):N2}";

                // Por pagar
                PagosPorPagarTextBlock.Text = porPagar.Count.ToString();
                MontoPorPagarTextBlock.Text = $"L {porPagar.Sum(p => p.MontoMensual):N2}";

                // Al día
                PagosAlDiaTextBlock.Text = alDia.Count.ToString();
                MontoAlDiaTextBlock.Text = $"L {alDia.Sum(p => p.MontoMensual):N2}";

                // Gasto mensual total
                var gastoTotal = _todosPagosPlataf.Sum(p => p.MontoMensual);
                GastoMensualTextBlock.Text = $"L {gastoTotal:N2}";
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
                var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";

                var pagosFiltrados = _todosPagosPlataf.AsEnumerable();

                // Filtrar por estado
                if (estadoFiltro != "todos")
                {
                    pagosFiltrados = pagosFiltrados.Where(p => p.Estado == estadoFiltro);
                }

                // Obtener datos relacionados
                var cuentas = await _supabase.ObtenerCuentasAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                var pagosViewModel = new List<PagoPlataformaViewModel>();

                foreach (var pago in pagosFiltrados.OrderBy(p => p.FechaProximoPago))
                {
                    var cuenta = cuentas.FirstOrDefault(c => c.Id == pago.CuentaId);
                    var plataforma = plataformas.FirstOrDefault(p => p.Id == pago.PlataformaId);

                    var diasRestantes = (pago.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    var vm = new PagoPlataformaViewModel
                    {
                        Id = pago.Id,
                        PlataformaNombre = plataforma?.Nombre ?? "Plataforma desconocida",
                        PlataformaIcono = plataforma?.Icono ?? "Television",
                        PlataformaColor = plataforma != null ? 
                            new SolidColorBrush((Color)ColorConverter.ConvertFromString(plataforma.Color)) : 
                            Brushes.Gray,
                        CuentaEmail = cuenta?.Email ?? "N/A",
                        MontoMensualTexto = $"L {pago.MontoMensual:N2}",
                        FechaProximoPagoTexto = $"Próximo pago: {pago.FechaProximoPago:dd/MM/yyyy}",
                        DiasRestantesTexto = ObtenerTextoDiasRestantes(diasRestantes),
                        DiasRestantesColor = ObtenerColorDiasRestantes(diasRestantes),
                        Estado = pago.Estado,
                        EstadoTexto = ObtenerTextoEstado(pago.Estado),
                        EstadoColor = ObtenerColorEstado(pago.Estado),
                        EstadoBackground = pago.Estado == "vencido" ? 
                            new SolidColorBrush(Color.FromRgb(255, 245, 245)) : 
                            Brushes.White,
                        MetodoPagoPreferido = CapitalizarMetodo(pago.MetodoPagoPreferido)
                    };

                    pagosViewModel.Add(vm);
                }

                if (pagosViewModel.Any())
                {
                    PagosPlataformaItemsControl.ItemsSource = pagosViewModel;
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PagosPlataformaItemsControl.ItemsSource = null;
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

        private string ObtenerTextoDiasRestantes(int dias)
        {
            if (dias < 0)
                return $"⚠️ Vencido hace {Math.Abs(dias)} día(s)";
            if (dias == 0)
                return "🔔 Vence HOY";
            if (dias == 1)
                return "⏰ Vence MAÑANA";
            if (dias <= 7)
                return $"📅 Vence en {dias} días";
            
            return $"✓ Vence en {dias} días";
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

        private string ObtenerTextoEstado(string estado)
        {
            return estado switch
            {
                "vencido" => "VENCIDO",
                "por_pagar" => "POR PAGAR",
                "al_dia" => "AL DÍA",
                _ => estado.ToUpper()
            };
        }

        private Brush ObtenerColorEstado(string estado)
        {
            return estado switch
            {
                "vencido" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rojo
                "por_pagar" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Naranja
                "al_dia" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Verde
                _ => Brushes.Gray
            };
        }

        private string CapitalizarMetodo(string metodo)
        {
            return metodo switch
            {
                "transferencia" => "Transferencia",
                "deposito" => "Depósito",
                "efectivo" => "Efectivo",
                "tarjeta" => "Tarjeta",
                _ => metodo
            };
        }

        private async void NuevoPagoButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementar dialog para configurar nuevo pago a plataforma
            MessageBox.Show(
                "Función en desarrollo.\n\nPronto podrás configurar pagos automáticos a plataformas.",
                "Nuevo Pago a Plataforma",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarPagosPlataformaAsync();
        }

        private void EstadoFiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }

        private async void RegistrarPagoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid pagoId)
            {
                var pago = _todosPagosPlataf.FirstOrDefault(p => p.Id == pagoId);
                if (pago == null) return;

                var plataformas = await _supabase.ObtenerPlataformasAsync();
                var plataforma = plataformas.FirstOrDefault(p => p.Id == pago.PlataformaId);

                var resultado = MessageBox.Show(
                    $"¿Confirmar pago de L {pago.MontoMensual:N2} a {plataforma?.Nombre ?? "plataforma"}?\n\n" +
                    $"Fecha próximo pago: {pago.FechaProximoPago:dd/MM/yyyy}\n" +
                    $"Método preferido: {CapitalizarMetodo(pago.MetodoPagoPreferido)}",
                    "Confirmar Pago",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        // Registrar en historial
                        var historial = new HistorialPagoPlataforma
                        {
                            Id = Guid.NewGuid(),
                            PagoPlataformaId = pago.Id,
                            MontoPagado = pago.MontoMensual,
                            FechaPago = DateTime.Now,
                            MetodoPago = pago.MetodoPagoPreferido,
                            Notas = $"Pago registrado desde el sistema"
                        };

                        await _supabase.CrearHistorialPagoPlataformaAsync(historial);

                        // Actualizar pago plataforma
                        pago.FechaUltimoPago = DateOnly.FromDateTime(DateTime.Today);
                        pago.FechaProximoPago = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
                        pago.FechaLimitePago = pago.FechaProximoPago.AddDays(pago.DiasGracia != 0 ? pago.DiasGracia : 5);
                        pago.Estado = "al_dia";

                        await _supabase.ActualizarPagoPlataformaAsync(pago);

                        MessageBox.Show(
                            "Pago registrado exitosamente.\n\n" +
                            $"Próximo pago: {pago.FechaProximoPago:dd/MM/yyyy}",
                            "Pago Registrado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPagosPlataformaAsync();
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

        private async void VerHistorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid pagoId)
            {
                var pago = _todosPagosPlataf.FirstOrDefault(p => p.Id == pagoId);
                if (pago == null) return;

                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var historial = await _supabase.ObtenerHistorialPagosPlataformaAsync(pagoId);
                    var plataformas = await _supabase.ObtenerPlataformasAsync();
                    var cuentas = await _supabase.ObtenerCuentasAsync();

                    var plataforma = plataformas.FirstOrDefault(p => p.Id == pago.PlataformaId);
                    var cuenta = cuentas.FirstOrDefault(c => c.Id == pago.CuentaId);

                    var mensaje = $"HISTORIAL DE PAGOS\n\n";
                    mensaje += $"Plataforma: {plataforma?.Nombre ?? "N/A"}\n";
                    mensaje += $"Cuenta: {cuenta?.Email ?? "N/A"}\n";
                    mensaje += $"Monto mensual: L {pago.MontoMensual:N2}\n";
                    mensaje += $"Estado: {ObtenerTextoEstado(pago.Estado)}\n";
                    mensaje += $"Próximo pago: {pago.FechaProximoPago:dd/MM/yyyy}\n\n";

                    if (historial.Any())
                    {
                        mensaje += $"═══════════════════════\n";
                        mensaje += $"ÚLTIMOS PAGOS:\n";
                        mensaje += $"═══════════════════════\n\n";

                        foreach (var h in historial.OrderByDescending(h => h.FechaPago).Take(10))
                        {
                            mensaje += $"📅 {h.FechaPago:dd/MM/yyyy HH:mm}\n";
                            mensaje += $"   Monto: L {h.MontoPagado:N2}\n";
                            mensaje += $"   Método: {CapitalizarMetodo(h.MetodoPago ?? "N/A")}\n";
                            if (!string.IsNullOrEmpty(h.Referencia))
                                mensaje += $"   Ref: {h.Referencia}\n";
                            mensaje += $"\n";
                        }
                    }
                    else
                    {
                        mensaje += "No hay historial de pagos registrados.";
                    }

                    MessageBox.Show(
                        mensaje,
                        "Historial de Pagos",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al cargar historial: {ex.Message}",
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

        // ViewModel para pagos a plataformas
        private class PagoPlataformaViewModel
        {
            public Guid Id { get; set; }
            public string PlataformaNombre { get; set; } = string.Empty;
            public string PlataformaIcono { get; set; } = string.Empty;
            public Brush PlataformaColor { get; set; } = Brushes.Gray;
            public string CuentaEmail { get; set; } = string.Empty;
            public string MontoMensualTexto { get; set; } = string.Empty;
            public string FechaProximoPagoTexto { get; set; } = string.Empty;
            public string DiasRestantesTexto { get; set; } = string.Empty;
            public Brush DiasRestantesColor { get; set; } = Brushes.Gray;
            public string Estado { get; set; } = string.Empty;
            public string EstadoTexto { get; set; } = string.Empty;
            public Brush EstadoColor { get; set; } = Brushes.Gray;
            public Brush EstadoBackground { get; set; } = Brushes.White;
            public string MetodoPagoPreferido { get; set; } = string.Empty;
        }
    }
}
