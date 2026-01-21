using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using StreamManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Pages
{
    public partial class CuentasPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<CuentaViewModel> _cuentas;
        private List<CuentaViewModel> _todasCuentas;
        private List<Plataforma> _plataformas;

        public CuentasPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _cuentas = new ObservableCollection<CuentaViewModel>();
            _todasCuentas = new List<CuentaViewModel>();
            _plataformas = new List<Plataforma>();

            CuentasDataGrid.ItemsSource = _cuentas;

            Loaded += CuentasPage_Loaded;
        }

        private async void CuentasPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarCuentasAsync();
        }

        private async Task CargarCuentasAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Cargar datos relacionados
                var cuentas = await _supabase.ObtenerCuentasAsync();
                _plataformas = await _supabase.ObtenerPlataformasAsync();
                var perfiles = await _supabase.ObtenerPerfilesAsync();

                // Crear ViewModels
                _todasCuentas = cuentas.Select(c =>
                {
                    var plataforma = _plataformas.FirstOrDefault(p => p.Id == c.PlataformaId);
                    var perfilesDisponibles = perfiles.Count(p => p.CuentaId == c.Id && p.Estado == "disponible");

                    return new CuentaViewModel
                    {
                        Id = c.Id,
                        PlataformaId = c.PlataformaId,
                        PlataformaNombre = plataforma?.Nombre ?? "Desconocida",
                        CorreoElectronico = c.Email,
                        Contraseña = c.Password,
                        PerfilesDisponibles = perfilesDisponibles,
                        FechaCreacion = c.FechaCreacion,
                        Estado = c.Estado,
                        Notas = c.Notas,
                        CuentaCorreo = c
                    };
                }).OrderByDescending(c => c.FechaCreacion).ToList();

                // Cargar filtros
                CargarFiltros();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar cuentas: {ex.Message}",
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
            // Cargar filtro de plataformas
            PlataformaFiltroComboBox.Items.Clear();
            PlataformaFiltroComboBox.Items.Add(new { Id = Guid.Empty, Nombre = "Todas las plataformas" });
            foreach (var plataforma in _plataformas.OrderBy(p => p.Nombre))
            {
                PlataformaFiltroComboBox.Items.Add(plataforma);
            }
            PlataformaFiltroComboBox.SelectedIndex = 0;
        }

        private void AplicarFiltros()
        {
            var busqueda = BuscarTextBox.Text?.ToLower() ?? "";
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var perfilesFiltro = (PerfilesFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "todos";
            var fechaDesde = FechaDesdeFilterPicker.SelectedDate;
            var fechaHasta = FechaHastaFilterPicker.SelectedDate;

            // Obtener plataforma seleccionada
            Guid plataformaId = Guid.Empty;
            if (PlataformaFiltroComboBox.SelectedItem != null)
            {
                var selectedItem = PlataformaFiltroComboBox.SelectedItem;
                var idProp = selectedItem.GetType().GetProperty("Id");
                if (idProp != null)
                {
                    plataformaId = (Guid)idProp.GetValue(selectedItem)!;
                }
            }

            var filtradas = _todasCuentas.Where(c =>
            {
                // Filtro de búsqueda
                var coincideBusqueda = string.IsNullOrWhiteSpace(busqueda) ||
                    c.CorreoElectronico.ToLower().Contains(busqueda) ||
                    c.PlataformaNombre.ToLower().Contains(busqueda);

                // Filtro de plataforma
                var coincidePlataforma = plataformaId == Guid.Empty || c.PlataformaId == plataformaId;

                // Filtro de estado
                var coincideEstado = string.IsNullOrWhiteSpace(estadoFiltro) ||
                    c.Estado.ToLower() == estadoFiltro;

                // Filtro de perfiles disponibles
                var coincidePerfiles = perfilesFiltro switch
                {
                    "con_disponibles" => c.PerfilesDisponibles > 0,
                    "sin_disponibles" => c.PerfilesDisponibles == 0,
                    _ => true
                };

                // Filtro por rango de fechas
                var coincideFechaDesde = !fechaDesde.HasValue || c.FechaCreacion >= fechaDesde.Value;
                var coincideFechaHasta = !fechaHasta.HasValue || c.FechaCreacion <= fechaHasta.Value.AddDays(1).AddTicks(-1);

                return coincideBusqueda && coincidePlataforma && coincideEstado && coincidePerfiles &&
                       coincideFechaDesde && coincideFechaHasta;
            }).ToList();

            _cuentas.Clear();
            foreach (var cuenta in filtradas)
            {
                _cuentas.Add(cuenta);
            }

            ActualizarContadorResultados(filtradas.Count);
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
            BuscarTextBox.Text = string.Empty;
            PlataformaFiltroComboBox.SelectedIndex = 0;
            EstadoFiltroComboBox.SelectedIndex = 0;
            PerfilesFiltroComboBox.SelectedIndex = 0;
            FechaDesdeFilterPicker.SelectedDate = null;
            FechaHastaFilterPicker.SelectedDate = null;

            AplicarFiltros();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CuentaDialog { Owner = Window.GetWindow(this) };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevaCuenta = dialog.CuentaCorreo;
                    nuevaCuenta.FechaCreacion = DateTime.UtcNow;
                    nuevaCuenta.Estado = "activo";

                    await _supabase.CrearCuentaAsync(nuevaCuenta);

                    MessageBox.Show("Cuenta creada exitosamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CargarCuentasAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al crear cuenta: {ex.Message}", "Error de Relación");
                }
                finally { LoadingOverlay.Visibility = Visibility.Collapsed; }
            }
        }

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                var dialog = new CuentaDialog(viewModel.CuentaCorreo)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        var cuentaActualizada = dialog.CuentaCorreo;

                        // Validación 1: Email duplicado
                        var todasLasCuentas = await _supabase.ObtenerCuentasAsync();
                        var emailDuplicado = todasLasCuentas.Any(c =>
                            c.Id != viewModel.Id &&
                            c.Email.ToLower() == cuentaActualizada.Email.ToLower());

                        if (emailDuplicado)
                        {
                            LoadingOverlay.Visibility = Visibility.Collapsed;
                            MessageBox.Show(
                                $"⚠️ El correo '{cuentaActualizada.Email}' ya está en uso por otra cuenta.\n\n" +
                                "Por favor usa un correo electrónico diferente.",
                                "Email duplicado",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Validación 2: Cambio de plataforma
                        var plataformaOriginal = viewModel.PlataformaId;
                        var plataformaNueva = cuentaActualizada.PlataformaId;

                        if (plataformaOriginal != plataformaNueva)
                        {
                            var perfiles = await _supabase.ObtenerPerfilesAsync();
                            var perfilesAsociados = perfiles.Where(p => p.CuentaId == viewModel.Id).ToList();

                            if (perfilesAsociados.Any())
                            {
                                LoadingOverlay.Visibility = Visibility.Collapsed;

                                var plataformaNombreOriginal = _plataformas.FirstOrDefault(p => p.Id == plataformaOriginal)?.Nombre ?? "Desconocida";
                                var plataformaNombreNueva = _plataformas.FirstOrDefault(p => p.Id == plataformaNueva)?.Nombre ?? "Desconocida";

                                var resultado = MessageBox.Show(
                                    $"⚠️ ADVERTENCIA: Cambio de plataforma detectado\n\n" +
                                    $"De: {plataformaNombreOriginal}\n" +
                                    $"A: {plataformaNombreNueva}\n\n" +
                                    $"Esta cuenta tiene {perfilesAsociados.Count} perfil(es) asociado(s).\n\n" +
                                    "Al cambiar la plataforma, estos perfiles quedarán asociados a la nueva plataforma, " +
                                    "lo que puede causar inconsistencias.\n\n" +
                                    "¿Deseas continuar?",
                                    "Confirmación de cambio de plataforma",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (resultado == MessageBoxResult.No)
                                {
                                    return;
                                }

                                LoadingOverlay.Visibility = Visibility.Visible;
                            }
                        }

                        // Validación 3: Desactivar cuenta con suscripciones activas
                        if (viewModel.Estado == "activo" && cuentaActualizada.Estado == "inactivo")
                        {
                            var perfiles = await _supabase.ObtenerPerfilesAsync();
                            var perfilesAsociados = perfiles.Where(p => p.CuentaId == viewModel.Id).ToList();

                            if (perfilesAsociados.Any())
                            {
                                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                                var perfilesIds = perfilesAsociados.Select(p => p.Id).ToList();
                                var suscripcionesActivas = suscripciones.Where(s =>
                                    s.PerfilId.HasValue && perfilesIds.Contains(s.PerfilId.Value) &&
                                    s.Estado == "activa").ToList();

                                if (suscripcionesActivas.Any())
                                {
                                    LoadingOverlay.Visibility = Visibility.Collapsed;

                                    var resultado = MessageBox.Show(
                                        $"⚠️ ADVERTENCIA: Esta cuenta tiene {suscripcionesActivas.Count} suscripción(es) activa(s)\n\n" +
                                        "Al desactivar la cuenta, podrías afectar el servicio de los clientes.\n\n" +
                                        "Recomendación:\n" +
                                        "1. Cancela las suscripciones activas primero\n" +
                                        "2. Luego desactiva la cuenta\n\n" +
                                        "¿Deseas continuar de todos modos?",
                                        "Confirmación de desactivación",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning);

                                    if (resultado == MessageBoxResult.No)
                                    {
                                        return;
                                    }

                                    LoadingOverlay.Visibility = Visibility.Visible;
                                }
                            }
                        }

                        // Si pasa todas las validaciones, actualizar
                        await _supabase.ActualizarCuentaAsync(cuentaActualizada);

                        MessageBox.Show(
                            "Cuenta actualizada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarCuentasAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar cuenta:\n\n{ex.Message}",
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
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                var dialog = new CuentaDetalleDialog(viewModel)
                {
                    Owner = Window.GetWindow(this)
                };

                dialog.ShowDialog();
            }
        }

        private void CopiarPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                try
                {
                    Clipboard.SetText(viewModel.Contraseña);
                    MessageBox.Show(
                        "Contraseña copiada al portapapeles",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al copiar: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CopiarEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                try
                {
                    Clipboard.SetText(viewModel.CorreoElectronico);
                    MessageBox.Show(
                        "Correo copiado al portapapeles",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al copiar: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // Validar si tiene perfiles asociados
                    var perfiles = await _supabase.ObtenerPerfilesAsync();
                    var perfilesAsociados = perfiles.Where(p => p.CuentaId == viewModel.Id).ToList();

                    if (perfilesAsociados.Any())
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;

                        var perfilesOcupados = perfilesAsociados.Count(p => p.Estado == "ocupado");
                        var perfilesDisponibles = perfilesAsociados.Count(p => p.Estado == "disponible");

                        var mensaje = $"⚠️ No se puede eliminar la cuenta '{viewModel.CorreoElectronico}'\n\n" +
                                    "Esta cuenta tiene perfiles asociados:\n\n" +
                                    $"• {perfilesOcupados} perfil(es) ocupado(s)\n" +
                                    $"• {perfilesDisponibles} perfil(es) disponible(s)\n\n" +
                                    "Acción requerida:\n" +
                                    "1. Elimina o reasigna todos los perfiles asociados\n" +
                                    "2. Cancela las suscripciones activas que usan estos perfiles\n" +
                                    "3. Luego podrás eliminar la cuenta";

                        MessageBox.Show(
                            mensaje,
                            "No se puede eliminar - Perfiles asociados",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Validar si tiene suscripciones activas
                    var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                    var suscripcionesActivas = suscripciones.Where(s =>
                        s.PlataformaId == viewModel.PlataformaId &&
                        s.Estado == "activa").ToList();

                    if (suscripcionesActivas.Any())
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;

                        var mensaje = $"⚠️ No se puede eliminar la cuenta '{viewModel.CorreoElectronico}'\n\n" +
                                    $"Existen {suscripcionesActivas.Count} suscripción(es) activa(s) de {viewModel.PlataformaNombre}\n\n" +
                                    "Acción requerida:\n" +
                                    "1. Cancela o finaliza todas las suscripciones activas\n" +
                                    "2. Luego podrás eliminar la cuenta";

                        MessageBox.Show(
                            mensaje,
                            "No se puede eliminar - Suscripciones activas",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    // Si pasa todas las validaciones, mostrar confirmación final
                    var confirmDialog = new ConfirmDialog(
                        $"¿Estás seguro de eliminar la cuenta '{viewModel.CorreoElectronico}'?\n\n" +
                        $"Plataforma: {viewModel.PlataformaNombre}\n" +
                        $"Estado: {viewModel.Estado}\n\n" +
                        "Esta acción NO se puede deshacer.",
                        "Eliminar Cuenta",
                        "Delete")
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (confirmDialog.ShowDialog() == true)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarCuentaAsync(viewModel.Id);

                        MessageBox.Show(
                            "Cuenta eliminada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarCuentasAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al eliminar cuenta:\n\n{ex.Message}\n\nSi el problema persiste, verifica que no existan dependencias en la base de datos.",
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
            await CargarCuentasAsync();
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

        private void FechaFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }
    }
}