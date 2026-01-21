using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using StreamManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Linq;

namespace StreamManager.Views.Pages
{
    public partial class SuscripcionesPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<SuscripcionViewModel> _suscripciones;
        private List<SuscripcionViewModel> _todasSuscripciones;
        private List<Cliente> _todosClientes;
        private List<Plataforma> _todasPlataformas;

        public SuscripcionesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _suscripciones = new ObservableCollection<SuscripcionViewModel>();
            _todasSuscripciones = new List<SuscripcionViewModel>();
            _todosClientes = new List<Cliente>();
            _todasPlataformas = new List<Plataforma>();

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

                // Guardar referencias para los filtros
                _todosClientes = clientes.Where(c => c.Estado == "activo").ToList();
                _todasPlataformas = plataformas.Where(p => p.Estado == "activa").ToList();

                // Cargar ComboBoxes de filtros
                CargarFiltros();

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
                        ClienteTelefono = cliente?.Telefono ?? "",
                        PerfilId = s.PerfilId ?? Guid.Empty,
                        PerfilNombre = perfil?.NombrePerfil ?? "Perfil desconocido",
                        PlataformaNombre = plataforma?.Nombre ?? "Plataforma desconocida",
                        CostoMensual = s.Precio,
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

        private void CargarFiltros()
        {
            // Cargar ComboBox de Clientes
            var clientesParaFiltro = new List<Cliente> { new Cliente { Id = Guid.Empty, NombreCompleto = "Todos los clientes" } };
            clientesParaFiltro.AddRange(_todosClientes.OrderBy(c => c.NombreCompleto));
            ClienteFiltroComboBox.ItemsSource = clientesParaFiltro;
            ClienteFiltroComboBox.SelectedIndex = 0;

            // Cargar ComboBox de Plataformas
            var plataformasParaFiltro = new List<Plataforma> { new Plataforma { Id = Guid.Empty, Nombre = "Todas las plataformas" } };
            plataformasParaFiltro.AddRange(_todasPlataformas.OrderBy(p => p.Nombre));
            PlataformaFiltroComboBox.ItemsSource = plataformasParaFiltro;
            PlataformaFiltroComboBox.SelectedIndex = 0;
        }

        private void AplicarFiltros()
        {
            var busqueda = BuscarTextBox.Text.ToLower().Trim();
            var telefonoBusqueda = TelefonoFiltroTextBox.Text.Trim();
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            var clienteSeleccionado = ClienteFiltroComboBox.SelectedItem as Cliente;
            var plataformaSeleccionada = PlataformaFiltroComboBox.SelectedItem as Plataforma;

            var fechaDesde = FechaDesdeFilterPicker.SelectedDate;
            var fechaHasta = FechaHastaFilterPicker.SelectedDate;

            var filtradas = _todasSuscripciones.Where(s =>
            {
                // Filtro de búsqueda general
                var coincideBusqueda = string.IsNullOrWhiteSpace(busqueda) ||
                    s.ClienteNombre.ToLower().Contains(busqueda) ||
                    s.PerfilNombre.ToLower().Contains(busqueda) ||
                    s.PlataformaNombre.ToLower().Contains(busqueda);

                // Filtro de teléfono
                var coincideTelefono = string.IsNullOrWhiteSpace(telefonoBusqueda) ||
                    s.ClienteTelefono.Contains(telefonoBusqueda);

                // Filtro de estado
                var coincideEstado = string.IsNullOrWhiteSpace(estadoFiltro) ||
                    s.Estado.ToLower() == estadoFiltro;

                // Filtro de cliente específico
                var coincideCliente = clienteSeleccionado == null ||
                    clienteSeleccionado.Id == Guid.Empty ||
                    s.ClienteId == clienteSeleccionado.Id;

                // Filtro de plataforma específica
                var coincidePlataforma = plataformaSeleccionada == null ||
                    plataformaSeleccionada.Id == Guid.Empty ||
                    s.PlataformaNombre == plataformaSeleccionada.Nombre;

                // Filtro de fecha DESDE
                var coincideFechaDesde = !fechaDesde.HasValue ||
                    s.ProximoPago >= fechaDesde.Value;

                // Filtro de fecha HASTA
                var coincideFechaHasta = !fechaHasta.HasValue ||
                    s.ProximoPago <= fechaHasta.Value;

                return coincideBusqueda &&
                       coincideTelefono &&
                       coincideEstado &&
                       coincideCliente &&
                       coincidePlataforma &&
                       coincideFechaDesde &&
                       coincideFechaHasta;
            }).ToList();

            _suscripciones.Clear();
            foreach (var suscripcion in filtradas)
            {
                _suscripciones.Add(suscripcion);
            }

            // Actualizar contador de resultados
            ActualizarContadorResultados(filtradas.Count);
        }

        private void ActualizarContadorResultados(int cantidad)
        {
            ResultadosTextBlock.Text = cantidad == 1
                ? "1 resultado"
                : $"{cantidad} resultados";
        }

        private void LimpiarFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar todos los filtros
            BuscarTextBox.Text = string.Empty;
            TelefonoFiltroTextBox.Text = string.Empty;
            EstadoFiltroComboBox.SelectedIndex = 0;
            ClienteFiltroComboBox.SelectedIndex = 0;
            PlataformaFiltroComboBox.SelectedIndex = 0;
            FechaDesdeFilterPicker.SelectedDate = null;
            FechaHastaFilterPicker.SelectedDate = null;

            AplicarFiltros();
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

                        var suscripcionActualizada = dialog.Suscripcion;
                        var suscripcionOriginal = viewModel.Suscripcion;

                        // Validación 1: Cambio de perfil
                        if (suscripcionOriginal.PerfilId != suscripcionActualizada.PerfilId)
                        {
                            var perfiles = await _supabase.ObtenerPerfilesAsync();
                            var perfilOriginal = perfiles.FirstOrDefault(p => p.Id == suscripcionOriginal.PerfilId);
                            var perfilNuevo = perfiles.FirstOrDefault(p => p.Id == suscripcionActualizada.PerfilId);

                            LoadingOverlay.Visibility = Visibility.Collapsed;

                            var resultado = MessageBox.Show(
                                $"⚠️ ADVERTENCIA: Cambio de perfil detectado\n\n" +
                                $"De: {perfilOriginal?.NombrePerfil ?? "Desconocido"}\n" +
                                $"A: {perfilNuevo?.NombrePerfil ?? "Desconocido"}\n\n" +
                                "Las credenciales de acceso cambiarán para el cliente.\n" +
                                "El perfil anterior quedará disponible.\n\n" +
                                "¿Deseas continuar?",
                                "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                            if (resultado == MessageBoxResult.No)
                                return;

                            LoadingOverlay.Visibility = Visibility.Visible;
                        }

                        // Validación 2: Cambio de estado a cancelada
                        if (suscripcionOriginal.Estado != "cancelada" && suscripcionActualizada.Estado == "cancelada")
                        {
                            LoadingOverlay.Visibility = Visibility.Collapsed;

                            var resultado = MessageBox.Show(
                                $"⚠️ Vas a cancelar la suscripción de {viewModel.ClienteNombre}\n\n" +
                                "El perfil quedará disponible.\n" +
                                "El cliente perderá acceso al servicio.\n\n" +
                                "¿Confirmas la cancelación?",
                                "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                            if (resultado == MessageBoxResult.No)
                                return;

                            LoadingOverlay.Visibility = Visibility.Visible;
                        }

                        await _supabase.ActualizarSuscripcionAsync(suscripcionActualizada);

                        MessageBox.Show("Suscripción actualizada exitosamente", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al actualizar suscripción:\n\n{ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var suscripcion = viewModel.Suscripcion;
                    var estadoOriginal = suscripcion.Estado;

                    // Si está cancelada, reactivar perfil
                    if (estadoOriginal == "cancelada")
                    {
                        var perfiles = await _supabase.ObtenerPerfilesAsync();
                        var perfil = perfiles.FirstOrDefault(p => p.Id == suscripcion.PerfilId);

                        if (perfil != null && perfil.Estado == "ocupado")
                        {
                            LoadingOverlay.Visibility = Visibility.Collapsed;
                            MessageBox.Show(
                                $"⚠️ No se puede renovar la suscripción\n\n" +
                                $"El perfil '{viewModel.PerfilNombre}' está ocupado por otro cliente.\n\n" +
                                "Acción requerida:\n" +
                                "1. Libera el perfil\n" +
                                "2. Luego podrás renovar esta suscripción",
                                "Perfil ocupado", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    string mensaje;
                    if (estadoOriginal == "cancelada")
                    {
                        mensaje = $"¿Reactivar suscripción de {viewModel.ClienteNombre}?\n\n" +
                                $"• Perfil: {viewModel.PerfilNombre}\n" +
                                $"• Plataforma: {viewModel.PlataformaNombre}\n" +
                                $"• Costo: L {viewModel.CostoMensual:N2}\n\n" +
                                "La suscripción se reactivará y se agregará 1 mes.\n" +
                                "El perfil quedará ocupado nuevamente.";
                    }
                    else
                    {
                        mensaje = $"¿Renovar suscripción de {viewModel.ClienteNombre}?\n\n" +
                                $"• Perfil: {viewModel.PerfilNombre}\n" +
                                $"• Costo: L {viewModel.CostoMensual:N2}\n" +
                                $"• Próximo pago: {suscripcion.FechaProximoPago:dd/MM/yyyy}\n\n" +
                                "Se agregará 1 mes al próximo pago (pago adelantado).";
                    }

                    var confirmDialog = new ConfirmDialog(mensaje, "Renovar Suscripción", "Info")
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (confirmDialog.ShowDialog() == true)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        // Sumar 30 días al próximo pago
                        var fechaTemp = suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue);
                        suscripcion.FechaProximoPago = DateOnly.FromDateTime(fechaTemp.AddDays(30));
                        suscripcion.FechaLimitePago = DateOnly.FromDateTime(fechaTemp.AddDays(35)); // +30 días + 5 de gracia
                        suscripcion.Estado = "activa";

                        await _supabase.ActualizarSuscripcionAsync(suscripcion);

                        // Si estaba cancelada, ocupar el perfil
                        if (estadoOriginal == "cancelada")
                        {
                            var perfiles = await _supabase.ObtenerPerfilesAsync();
                            var perfil = perfiles.FirstOrDefault(p => p.Id == suscripcion.PerfilId);
                            if (perfil != null)
                            {
                                perfil.Estado = "ocupado";
                                await _supabase.ActualizarPerfilAsync(perfil);
                            }
                        }

                        MessageBox.Show(
                            estadoOriginal == "cancelada"
                                ? "Suscripción reactivada y renovada exitosamente"
                                : "Suscripción renovada exitosamente (pago adelantado)",
                            "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al renovar suscripción:\n\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }


        private async void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SuscripcionViewModel viewModel)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // Verificar pagos pendientes
                    var pagos = await _supabase.ObtenerPagosAsync();
                    var pagosPendientes = pagos.Where(p =>
                        p.SuscripcionId == viewModel.Id).ToList();

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    string mensaje;
                    if (pagosPendientes.Any())
                    {
                        var totalPendiente = pagosPendientes.Sum(p => p.Monto);
                        mensaje = $"⚠️ Cancelar suscripción de {viewModel.ClienteNombre}?\n\n" +
                                $"• Perfil: {viewModel.PerfilNombre}\n" +
                                $"• Plataforma: {viewModel.PlataformaNombre}\n" +
                                $"• Pagos pendientes: {pagosPendientes.Count} (L {totalPendiente:N2})\n\n" +
                                "El perfil quedará disponible.\n" +
                                "Los pagos pendientes seguirán registrados.\n\n" +
                                "¿Confirmas la cancelación?";
                    }
                    else
                    {
                        mensaje = $"¿Cancelar la suscripción de {viewModel.ClienteNombre}?\n\n" +
                                $"• Perfil: {viewModel.PerfilNombre}\n" +
                                $"• Plataforma: {viewModel.PlataformaNombre}\n\n" +
                                "El perfil quedará disponible para otro cliente.";
                    }

                    var confirmDialog = new ConfirmDialog(mensaje, "Cancelar Suscripción", "Warning")
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (confirmDialog.ShowDialog() == true)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        var suscripcion = viewModel.Suscripcion;
                        suscripcion.Estado = "cancelada";

                        await _supabase.ActualizarSuscripcionAsync(suscripcion);

                        // Liberar perfil
                        var perfiles = await _supabase.ObtenerPerfilesAsync();
                        var perfil = perfiles.FirstOrDefault(p => p.Id == suscripcion.PerfilId);
                        if (perfil != null)
                        {
                            perfil.Estado = "disponible";
                            await _supabase.ActualizarPerfilAsync(perfil);
                        }

                        MessageBox.Show("Suscripción cancelada exitosamente", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await CargarSuscripcionesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cancelar suscripción:\n\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
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

        private void FechaFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }
    }
}