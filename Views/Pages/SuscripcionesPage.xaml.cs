using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using StreamManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;    // Para CargarSuscripcionesAsync
using System.Linq;               // Para .Select(), .FirstOrDefault(), .Where()


namespace StreamManager.Views.Pages
{
    public partial class SuscripcionesPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<SuscripcionViewModel> _suscripciones;
        private List<SuscripcionViewModel> _todasSuscripciones;

        public SuscripcionesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _suscripciones = new ObservableCollection<SuscripcionViewModel>();
            _todasSuscripciones = new List<SuscripcionViewModel>();

            SuscripcionesDataGrid.ItemsSource = _suscripciones;

            Loaded += SuscripcionesPage_Loaded;
        }

        private async void SuscripcionesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarSuscripcionesAsync();
        }

        private async Task CargarSuscripcionesAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Cargar datos relacionados
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var clientes = await _supabase.ObtenerClientesAsync();
                var perfiles = await _supabase.ObtenerPerfilesAsync();
                var cuentas = await _supabase.ObtenerCuentasAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                // Crear ViewModels con información relacionada
                _todasSuscripciones = suscripciones.Select(s =>
                {
                    var cliente = clientes.FirstOrDefault(c => c.Id == s.ClienteId);
                    var perfil = perfiles.FirstOrDefault(p => p.Id == s.PerfilId);
                    var cuenta = perfil != null ? cuentas.FirstOrDefault(c => c.Id == perfil.CuentaId) : null;
                    var plataforma = cuenta != null ? plataformas.FirstOrDefault(p => p.Id == cuenta.PlataformaId) : null;

                    return new SuscripcionViewModel
                    {
                        Id = s.Id,
                        ClienteId = s.ClienteId,
                        ClienteNombre = cliente?.NombreCompleto ?? "Cliente desconocido",

                        // CORRECCIÓN 1: Casteo de Guid? a Guid (quitando la coma doble)
                        PerfilId = s.PerfilId ?? Guid.Empty,

                        PerfilNombre = perfil?.NombrePerfil ?? "Perfil desconocido",
                        PlataformaNombre = plataforma?.Nombre ?? "Plataforma desconocida",
                        CostoMensual = s.Precio,

                        // CORRECCIÓN 2: Conversión de DateOnly a DateTime
                        FechaInicio = s.FechaInicio.ToDateTime(TimeOnly.MinValue),
                        ProximoPago = s.FechaProximoPago.ToDateTime(TimeOnly.MinValue),

                        Estado = s.Estado,
                        Notas = s.Notas,
                        Suscripcion = s
                    };
                }).OrderByDescending(s => s.FechaInicio).ToList();
                AplicarFiltros();
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

        private void AplicarFiltros()
        {
            var busqueda = BuscarTextBox.Text.ToLower();
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            var filtradas = _todasSuscripciones.Where(s =>
            {
                // Filtro de búsqueda
                var coincideBusqueda = string.IsNullOrWhiteSpace(busqueda) ||
                    s.ClienteNombre.ToLower().Contains(busqueda) ||
                    s.PerfilNombre.ToLower().Contains(busqueda) ||
                    s.PlataformaNombre.ToLower().Contains(busqueda);

                // Filtro de estado
                var coincideEstado = string.IsNullOrWhiteSpace(estadoFiltro) ||
                    s.Estado.ToLower() == estadoFiltro;

                return coincideBusqueda && coincideEstado;
            }).ToList();

            _suscripciones.Clear();
            foreach (var suscripcion in filtradas)
            {
                _suscripciones.Add(suscripcion);
            }
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SuscripcionDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevaSuscripcion = dialog.Suscripcion;
                    await _supabase.CrearSuscripcionAsync(nuevaSuscripcion);

                    MessageBox.Show(
                        "Suscripción creada exitosamente!",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await CargarSuscripcionesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al crear suscripción: {ex.Message}",
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

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SuscripcionViewModel viewModel)
            {
                var dialog = new SuscripcionDialog(viewModel.Suscripcion)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.ActualizarSuscripcionAsync(dialog.Suscripcion);

                        MessageBox.Show(
                            "Suscripción actualizada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar suscripción: {ex.Message}",
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

        private void VerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SuscripcionViewModel viewModel)
            {
                var dialog = new SuscripcionDetalleDialog(viewModel)
                {
                    Owner = Window.GetWindow(this)
                };

                dialog.ShowDialog();
            }
        }

        private async void RenovarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SuscripcionViewModel viewModel)
            {
                var confirmDialog = new ConfirmDialog(
                    $"¿Renovar la suscripción de {viewModel.ClienteNombre}?\n\n" +
                    $"• Perfil: {viewModel.PerfilNombre}\n" +
                    $"• Costo: L {viewModel.CostoMensual:N2}\n\n" +
                    "Se extenderá el próximo pago por 30 días.",
                    "Renovar Suscripción",
                    "Info")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        var suscripcion = viewModel.Suscripcion;
                        suscripcion.FechaProximoPago = suscripcion.FechaProximoPago.AddMonths(1);
                        suscripcion.Estado = "activa";

                        await _supabase.ActualizarSuscripcionAsync(suscripcion);

                        MessageBox.Show(
                            "Suscripción renovada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al renovar suscripción: {ex.Message}",
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

        private async void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SuscripcionViewModel viewModel)
            {
                var confirmDialog = new ConfirmDialog(
                    $"¿Cancelar la suscripción de {viewModel.ClienteNombre}?\n\n" +
                    $"• Perfil: {viewModel.PerfilNombre}\n" +
                    $"• Plataforma: {viewModel.PlataformaNombre}\n\n" +
                    "El perfil quedará disponible para otro cliente.",
                    "Cancelar Suscripción",
                    "Warning")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        var suscripcion = viewModel.Suscripcion;
                        suscripcion.Estado = "cancelada";

                        await _supabase.ActualizarSuscripcionAsync(suscripcion);

                        MessageBox.Show(
                            "Suscripción cancelada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al cancelar suscripción: {ex.Message}",
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

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarSuscripcionesAsync();
        }

        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }
    }
}
