using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using StreamManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Pages
{
    public partial class PerfilesPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<PerfilViewModel> _perfiles;
        private List<PerfilViewModel> _todosPerfiles;
        private List<Plataforma> _todasPlataformas;

        public PerfilesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _perfiles = new ObservableCollection<PerfilViewModel>();
            _todosPerfiles = new List<PerfilViewModel>();
            _todasPlataformas = new List<Plataforma>();

            PerfilesDataGrid.ItemsSource = _perfiles;

            Loaded += PerfilesPage_Loaded;
        }

        private async void PerfilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPerfilesAsync();
        }

        private async Task CargarPerfilesAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Cargar datos relacionados
                var perfiles = await _supabase.ObtenerPerfilesAsync();
                var cuentas = await _supabase.ObtenerCuentasAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                _todasPlataformas = plataformas.Where(p => p.Estado == "activa").ToList();
                CargarFiltros();

                // Crear ViewModels
                _todosPerfiles = perfiles.Select(p =>
                {
                    var cuenta = cuentas.FirstOrDefault(c => c.Id == p.CuentaId);
                    var plataforma = cuenta != null ? plataformas.FirstOrDefault(pl => pl.Id == cuenta.PlataformaId) : null;

                    return new PerfilViewModel
                    {
                        Id = p.Id,
                        CuentaCorreoId = p.CuentaId,
                        CuentaNombre = cuenta?.Email ?? "Cuenta desconocida",
                        PlataformaNombre = plataforma?.Nombre ?? "Plataforma desconocida",
                        Nombre = p.NombrePerfil,
                        Pin = p.Pin ?? "Sin PIN",
                        Estado = p.Estado,
                        Perfil = p
                    };
                }).OrderBy(p => p.PlataformaNombre).ThenBy(p => p.Nombre).ToList();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar perfiles: {ex.Message}",
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
            var plataformasParaFiltro = new List<Plataforma> { new Plataforma { Id = Guid.Empty, Nombre = "Todas las plataformas" } };
            plataformasParaFiltro.AddRange(_todasPlataformas.OrderBy(p => p.Nombre));
            PlataformaFiltroComboBox.ItemsSource = plataformasParaFiltro;
            PlataformaFiltroComboBox.SelectedIndex = 0;
        }

        private void AplicarFiltros()
        {
            var busquedaNombre = BuscarTextBox.Text.ToLower().Trim();
            var busquedaCuenta = CuentaFiltroTextBox.Text.ToLower().Trim();
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var plataformaSeleccionada = PlataformaFiltroComboBox.SelectedItem as Plataforma;

            var filtrados = _todosPerfiles.Where(p =>
            {
                // Filtro de búsqueda por nombre
                var coincideNombre = string.IsNullOrWhiteSpace(busquedaNombre) ||
                    p.Nombre.ToLower().Contains(busquedaNombre);

                // Filtro de búsqueda por cuenta
                var coincideCuenta = string.IsNullOrWhiteSpace(busquedaCuenta) ||
                    p.CuentaNombre.ToLower().Contains(busquedaCuenta);

                // Filtro de estado
                var coincideEstado = string.IsNullOrWhiteSpace(estadoFiltro) ||
                    p.Estado.ToLower() == estadoFiltro;

                // Filtro de plataforma
                var coincidePlataforma = plataformaSeleccionada == null ||
                    plataformaSeleccionada.Id == Guid.Empty ||
                    p.PlataformaNombre == plataformaSeleccionada.Nombre;

                return coincideNombre && coincideCuenta && coincideEstado && coincidePlataforma;
            }).ToList();

            _perfiles.Clear();
            foreach (var perfil in filtrados)
            {
                _perfiles.Add(perfil);
            }

            ActualizarContadorResultados(filtrados.Count);
        }

        private void ActualizarContadorResultados(int cantidad)
        {
            ResultadosTextBlock.Text = cantidad == 1
                ? "1 resultado"
                : $"{cantidad} resultados";
        }

        private void LimpiarFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BuscarTextBox.Text = string.Empty;
            CuentaFiltroTextBox.Text = string.Empty;
            EstadoFiltroComboBox.SelectedIndex = 0;
            PlataformaFiltroComboBox.SelectedIndex = 0;

            AplicarFiltros();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PerfilDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevoPerfil = dialog.Perfil;
                    await _supabase.CrearPerfilAsync(nuevoPerfil);

                    MessageBox.Show(
                        "Perfil creado exitosamente",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await CargarPerfilesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al crear perfil: {ex.Message}",
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
            if (sender is Button button && button.Tag is PerfilViewModel viewModel)
            {
                var dialog = new PerfilDialog(viewModel.Perfil)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        var perfilActualizado = dialog.Perfil;

                        // Validación 1: Cambio de cuenta
                        var cuentaOriginal = viewModel.CuentaCorreoId;
                        var cuentaNueva = perfilActualizado.CuentaId;

                        if (cuentaOriginal != cuentaNueva)
                        {
                            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                            var suscripcionesActivas = suscripciones.Where(s =>
                                s.PerfilId.HasValue &&
                                s.PerfilId.Value == viewModel.Id &&
                                s.Estado == "activa").ToList();

                            if (suscripcionesActivas.Any())
                            {
                                LoadingOverlay.Visibility = Visibility.Collapsed;

                                var cuentas = await _supabase.ObtenerCuentasAsync();
                                var cuentaOriginalObj = cuentas.FirstOrDefault(c => c.Id == cuentaOriginal);
                                var cuentaNuevaObj = cuentas.FirstOrDefault(c => c.Id == cuentaNueva);

                                var resultado = MessageBox.Show(
                                    $"⚠️ ADVERTENCIA: Cambio de cuenta detectado\n\n" +
                                    $"De: {cuentaOriginalObj?.Email ?? "Desconocida"}\n" +
                                    $"A: {cuentaNuevaObj?.Email ?? "Desconocida"}\n\n" +
                                    $"Este perfil tiene {suscripcionesActivas.Count} suscripción(es) activa(s).\n\n" +
                                    "Al cambiar la cuenta, las credenciales de acceso cambiarán para los clientes.\n\n" +
                                    "¿Deseas continuar?",
                                    "Confirmación de cambio de cuenta",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (resultado == MessageBoxResult.No)
                                {
                                    return;
                                }

                                LoadingOverlay.Visibility = Visibility.Visible;
                            }
                        }

                        // Validación 2: Cambio de estado a disponible cuando tiene suscripciones
                        if (viewModel.Estado == "ocupado" && perfilActualizado.Estado == "disponible")
                        {
                            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                            var suscripcionesActivas = suscripciones.Where(s =>
                                s.PerfilId.HasValue &&
                                s.PerfilId.Value == viewModel.Id &&
                                s.Estado == "activa").ToList();

                            if (suscripcionesActivas.Any())
                            {
                                LoadingOverlay.Visibility = Visibility.Collapsed;

                                MessageBox.Show(
                                    $"⚠️ No se puede cambiar el estado a 'disponible'\n\n" +
                                    $"Este perfil tiene {suscripcionesActivas.Count} suscripción(es) activa(s).\n\n" +
                                    "Acción requerida:\n" +
                                    "1. Cancela las suscripciones activas primero\n" +
                                    "2. Luego podrás cambiar el estado a disponible",
                                    "Estado no permitido",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                return;
                            }
                        }

                        // Si pasa validaciones, actualizar
                        await _supabase.ActualizarPerfilAsync(perfilActualizado);

                        MessageBox.Show(
                            "Perfil actualizado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPerfilesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar perfil:\n\n{ex.Message}",
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
            if (sender is Button button && button.Tag is PerfilViewModel viewModel)
            {
                var dialog = new PerfilDetalleDialog(viewModel)
                {
                    Owner = Window.GetWindow(this)
                };

                dialog.ShowDialog();
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PerfilViewModel viewModel)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // Validar si tiene suscripciones activas
                    var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                    var suscripcionesActivas = suscripciones.Where(s =>
                        s.PerfilId.HasValue &&
                        s.PerfilId.Value == viewModel.Id &&
                        s.Estado == "activa").ToList();

                    if (suscripcionesActivas.Any())
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;

                        var clientes = await _supabase.ObtenerClientesAsync();
                        var nombresClientes = suscripcionesActivas
                            .Select(s => clientes.FirstOrDefault(c => c.Id == s.ClienteId)?.NombreCompleto ?? "Desconocido")
                            .Take(3)
                            .ToList();

                        var listaClientes = string.Join("\n• ", nombresClientes);
                        var masClientes = suscripcionesActivas.Count > 3 ? $"\n• ... y {suscripcionesActivas.Count - 3} más" : "";

                        var mensaje = $"⚠️ No se puede eliminar el perfil '{viewModel.Nombre}'\n\n" +
                                    $"Este perfil tiene {suscripcionesActivas.Count} suscripción(es) activa(s):\n\n" +
                                    $"• {listaClientes}{masClientes}\n\n" +
                                    "Acción requerida:\n" +
                                    "1. Cancela o finaliza todas las suscripciones activas\n" +
                                    "2. Luego podrás eliminar el perfil";

                        MessageBox.Show(
                            mensaje,
                            "No se puede eliminar - Suscripciones activas",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    // Si pasa validaciones, mostrar confirmación
                    var confirmDialog = new ConfirmDialog(
                        $"¿Estás seguro de eliminar el perfil '{viewModel.Nombre}'?\n\n" +
                        $"• Plataforma: {viewModel.PlataformaNombre}\n" +
                        $"• Estado: {viewModel.Estado}\n" +
                        $"• PIN: {viewModel.Pin}\n\n" +
                        "Esta acción NO se puede deshacer.",
                        "Eliminar Perfil",
                        "Delete")
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (confirmDialog.ShowDialog() == true)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarPerfilAsync(viewModel.Id);

                        MessageBox.Show(
                            "Perfil eliminado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPerfilesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al eliminar perfil:\n\n{ex.Message}",
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

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarPerfilesAsync();
        }

        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
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