using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class AlertasPage : Page
    {
        private readonly SupabaseService _supabase;
        private readonly AlertaService _alertaService;
        private List<Alerta> _todasLasAlertas = new();

        public AlertasPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _alertaService = App.ServiceProvider?.GetRequiredService<AlertaService>()
                ?? throw new InvalidOperationException("AlertaService no disponible");

            Loaded += AlertasPage_Loaded;
        }

        private async void AlertasPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarAlertasAsync();
        }

        private async Task CargarAlertasAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                AlertasScrollViewer.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Collapsed;

                // Generar alertas automáticas primero
                await _alertaService.GenerarAlertasAutomaticasAsync();

                // Obtener todas las alertas
                _todasLasAlertas = await _supabase.ObtenerAlertasAsync();

                // Aplicar filtros
                AplicarFiltros();

                // Refrescar el badge en MainWindow
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    await mainWindow.RefrescarAlertasAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar alertas: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void AplicarFiltros()
        {
            var tipoFiltro = (TipoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todas";
            var nivelFiltro = (NivelFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "pendiente";

            var alertasFiltradas = _todasLasAlertas.AsEnumerable();

            // Filtrar por tipo
            if (tipoFiltro != "todas")
            {
                alertasFiltradas = alertasFiltradas.Where(a => a.TipoAlerta == tipoFiltro);
            }

            // Filtrar por nivel
            if (nivelFiltro != "todos")
            {
                alertasFiltradas = alertasFiltradas.Where(a => a.Nivel == nivelFiltro);
            }

            // Filtrar por estado
            if (estadoFiltro != "todas")
            {
                alertasFiltradas = alertasFiltradas.Where(a => a.Estado == estadoFiltro);
            }

            var alertasOrdenadas = alertasFiltradas
                .OrderByDescending(a => ObtenerPrioridadNivel(a.Nivel))
                .ThenByDescending(a => a.FechaCreacion)
                .ToList();

            if (alertasOrdenadas.Any())
            {
                var alertasViewModel = alertasOrdenadas.Select(a => CrearAlertaViewModel(a)).ToList();
                AlertasItemsControl.ItemsSource = alertasViewModel;

                // ✅ CORRECCIÓN: Mostrar lista y ocultar estado vacío
                AlertasScrollViewer.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // ✅ CORRECCIÓN: Ocultar lista y mostrar estado vacío
                AlertasScrollViewer.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
        }

        private AlertaViewModel CrearAlertaViewModel(Alerta alerta)
        {
            return new AlertaViewModel
            {
                Id = alerta.Id,
                Mensaje = alerta.Mensaje,
                FechaTexto = ObtenerTextoFechaRelativa(alerta.FechaCreacion),
                Estado = alerta.Estado,
                Nivel = alerta.Nivel,
                TipoAlerta = alerta.TipoAlerta,

                // Iconos y colores
                TipoIcono = ObtenerIconoPorTipo(alerta.TipoAlerta),
                TipoColor = ObtenerColorPorTipo(alerta.TipoAlerta),
                TipoTexto = ObtenerTextoTipo(alerta.TipoAlerta),
                NivelColor = ObtenerColorPorNivel(alerta.Nivel),
                NivelTextoCorto = ObtenerTextoCortoNivel(alerta.Nivel),
                EstadoColor = ObtenerColorPorEstado(alerta.Estado),
                EstadoTexto = ObtenerTextoEstado(alerta.Estado),
                EstadoBackground = alerta.Estado != "pendiente" ? new SolidColorBrush(Color.FromRgb(250, 250, 250)) : Brushes.White,

                // Visibilidad de elementos
                EsPendiente = alerta.Estado == "pendiente" ? Visibility.Visible : Visibility.Collapsed,
                TieneDiasRestantes = alerta.DiasRestantes.HasValue ? Visibility.Visible : Visibility.Collapsed,
                TieneMonto = alerta.Monto.HasValue && alerta.Monto > 0 ? Visibility.Visible : Visibility.Collapsed,

                // Datos adicionales
                DiasRestantesTexto = alerta.DiasRestantes.HasValue ?
                    (alerta.DiasRestantes >= 0 ? $"{alerta.DiasRestantes} días restantes" : $"Vencido hace {Math.Abs(alerta.DiasRestantes.Value)} días") : "",
                MontoTexto = alerta.Monto.HasValue ? $"Monto: L {alerta.Monto:N2}" : ""
            };
        }

        private int ObtenerPrioridadNivel(string nivel)
        {
            return nivel switch
            {
                "critico" => 4,
                "urgente" => 3,
                "advertencia" => 2,
                "normal" => 1,
                _ => 0
            };
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

        private string ObtenerIconoPorTipo(string tipo)
        {
            return tipo switch
            {
                "cobro_cliente" => "CashMultiple",
                "pago_plataforma" => "CreditCardSettings",
                _ => "Bell"
            };
        }

        private Brush ObtenerColorPorTipo(string tipo)
        {
            return tipo switch
            {
                "cobro_cliente" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Verde
                "pago_plataforma" => new SolidColorBrush(Color.FromRgb(255, 82, 82)), // Rojo
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Gris
            };
        }

        private string ObtenerTextoTipo(string tipo)
        {
            return tipo switch
            {
                "cobro_cliente" => "Cobro Cliente",
                "pago_plataforma" => "Pago Plataforma",
                _ => "Sistema"
            };
        }

        private Brush ObtenerColorPorNivel(string nivel)
        {
            return nivel switch
            {
                "critico" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rojo
                "urgente" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Naranja
                "advertencia" => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Amarillo
                "normal" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Azul
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Gris
            };
        }

        private string ObtenerTextoCortoNivel(string nivel)
        {
            return nivel switch
            {
                "critico" => "!!!",
                "urgente" => "!!",
                "advertencia" => "!",
                "normal" => "i",
                _ => "?"
            };
        }

        private Brush ObtenerColorPorEstado(string estado)
        {
            return estado switch
            {
                "pendiente" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rojo
                "enviada" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Naranja
                "leida" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Azul
                "resuelta" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Verde
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Gris
            };
        }

        private string ObtenerTextoEstado(string estado)
        {
            return estado switch
            {
                "pendiente" => "PENDIENTE",
                "enviada" => "ENVIADA",
                "leida" => "LEÍDA",
                "resuelta" => "RESUELTA",
                _ => "DESCONOCIDO"
            };
        }

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarAlertasAsync();
        }

        private async void MarcarLeidasButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                await _alertaService.MarcarTodasComoLeidasAsync();

                MessageBox.Show(
                    "Todas las alertas pendientes han sido marcadas como leídas.",
                    "Alertas Actualizadas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await CargarAlertasAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al marcar alertas: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void LimpiarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "¿Deseas eliminar todas las alertas leídas/resueltas con más de 30 días de antigüedad?",
                    "Confirmar Limpieza",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    await _alertaService.LimpiarAlertasAntiguasAsync();

                    MessageBox.Show(
                        "Alertas antiguas eliminadas exitosamente.",
                        "Limpieza Completada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await CargarAlertasAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al limpiar alertas: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void MarcarLeidaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Guid alertaId)
                {
                    await _alertaService.MarcarAlertaComoLeidaAsync(alertaId);
                    await CargarAlertasAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al marcar alerta: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ResolverButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Guid alertaId)
                {
                    var result = MessageBox.Show(
                        "¿Marcar esta alerta como resuelta?",
                        "Confirmar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await _alertaService.ResolverAlertaAsync(alertaId);
                        await CargarAlertasAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al resolver alerta: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void VerDetallesButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la entidad correspondiente
            if (sender is Button button && button.Tag is Guid alertaId)
            {
                var alerta = _todasLasAlertas.FirstOrDefault(a => a.Id == alertaId);
                if (alerta != null)
                {
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        if (alerta.TipoAlerta == "cobro_cliente")
                        {
                            mainWindow.NavigateToPage("GestionPagosClientesPage");
                        }
                        else if (alerta.TipoAlerta == "pago_plataforma")
                        {
                            mainWindow.NavigateToPage("PagosPlataformaPage");
                        }
                    }
                }
            }
        }

        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }

        // ViewModel para las alertas
        private class AlertaViewModel
        {
            public Guid Id { get; set; }
            public string Mensaje { get; set; } = string.Empty;
            public string FechaTexto { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public string Nivel { get; set; } = string.Empty;
            public string TipoAlerta { get; set; } = string.Empty;
            public string TipoIcono { get; set; } = string.Empty;
            public Brush TipoColor { get; set; } = Brushes.Gray;
            public string TipoTexto { get; set; } = string.Empty;
            public Brush NivelColor { get; set; } = Brushes.Gray;
            public string NivelTextoCorto { get; set; } = string.Empty;
            public Brush EstadoColor { get; set; } = Brushes.Gray;
            public string EstadoTexto { get; set; } = string.Empty;
            public Brush EstadoBackground { get; set; } = Brushes.White;
            public Visibility EsPendiente { get; set; } = Visibility.Collapsed;
            public Visibility TieneDiasRestantes { get; set; } = Visibility.Collapsed;
            public Visibility TieneMonto { get; set; } = Visibility.Collapsed;
            public string DiasRestantesTexto { get; set; } = string.Empty;
            public string MontoTexto { get; set; } = string.Empty;
        }
    }
}