using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Pages
{
    public partial class ClientesPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<Cliente> _clientes;
        private List<Cliente> _todosClientes;

        public ClientesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _clientes = new ObservableCollection<Cliente>();
            _todosClientes = new List<Cliente>();

            ClientesDataGrid.ItemsSource = _clientes;

            Loaded += ClientesPage_Loaded;
        }

        private async void ClientesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarClientesAsync();
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                var clientes = await _supabase.ObtenerClientesAsync();
                _todosClientes = clientes.OrderBy(c => c.NombreCompleto).ToList();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar clientes: {ex.Message}",
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
            var busquedaNombre = BuscarTextBox.Text.ToLower().Trim();
            var busquedaTelefono = TelefonoFiltroTextBox.Text.Trim();
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var fechaDesde = FechaDesdeFilterPicker.SelectedDate;
            var fechaHasta = FechaHastaFilterPicker.SelectedDate;

            var filtrados = _todosClientes.Where(c =>
            {
                // Filtro de búsqueda por nombre
                var coincideNombre = string.IsNullOrWhiteSpace(busquedaNombre) ||
                    c.NombreCompleto.ToLower().Contains(busquedaNombre);

                // Filtro de búsqueda por teléfono
                var coincideTelefono = string.IsNullOrWhiteSpace(busquedaTelefono) ||
                    (c.Telefono?.Contains(busquedaTelefono) ?? false);

                // Filtro de estado
                var coincideEstado = string.IsNullOrWhiteSpace(estadoFiltro) ||
                    c.Estado.ToLower() == estadoFiltro;

                // Filtro de fecha DESDE
                var coincideFechaDesde = !fechaDesde.HasValue ||
                    c.FechaRegistro >= fechaDesde.Value;

                // Filtro de fecha HASTA
                var coincideFechaHasta = !fechaHasta.HasValue ||
                    c.FechaRegistro <= fechaHasta.Value;

                return coincideNombre &&
                       coincideTelefono &&
                       coincideEstado &&
                       coincideFechaDesde &&
                       coincideFechaHasta;
            }).ToList();

            _clientes.Clear();
            foreach (var cliente in filtrados)
            {
                _clientes.Add(cliente);
            }

            // Actualizar contador de resultados
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
            // Limpiar todos los filtros
            BuscarTextBox.Text = string.Empty;
            TelefonoFiltroTextBox.Text = string.Empty;
            EstadoFiltroComboBox.SelectedIndex = 0;
            FechaDesdeFilterPicker.SelectedDate = null;
            FechaHastaFilterPicker.SelectedDate = null;

            AplicarFiltros();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClienteDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevoCliente = dialog.Cliente;
                    nuevoCliente.FechaRegistro = DateTime.Now;

                    await _supabase.CrearClienteAsync(nuevoCliente);

                    MessageBox.Show(
                        "Cliente creado exitosamente",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await CargarClientesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al crear cliente: {ex.Message}",
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
            if (sender is Button button && button.Tag is Cliente cliente)
            {
                var dialog = new ClienteDialog(cliente)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        var clienteActualizado = dialog.Cliente;

                        // Validación 1: Teléfono duplicado
                        var todosClientes = await _supabase.ObtenerClientesAsync();
                        if (!string.IsNullOrWhiteSpace(clienteActualizado.Telefono))
                        {
                            var telefonoDuplicado = todosClientes.Any(c =>
                                c.Id != cliente.Id &&
                                c.Telefono == clienteActualizado.Telefono);

                            if (telefonoDuplicado)
                            {
                                LoadingOverlay.Visibility = Visibility.Collapsed;
                                MessageBox.Show(
                                    $"⚠️ El teléfono '{clienteActualizado.Telefono}' ya está registrado.\n\n" +
                                    "Por favor usa un número diferente.",
                                    "Teléfono duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        // Validación 2: Desactivar con suscripciones activas
                        if (cliente.Estado == "activo" && clienteActualizado.Estado == "inactivo")
                        {
                            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                            var suscripcionesActivas = suscripciones.Where(s =>
                                s.ClienteId == cliente.Id &&
                                s.Estado == "activa").ToList();

                            if (suscripcionesActivas.Any())
                            {
                                LoadingOverlay.Visibility = Visibility.Collapsed;

                                var resultado = MessageBox.Show(
                                    $"⚠️ El cliente tiene {suscripcionesActivas.Count} suscripción(es) activa(s)\n\n" +
                                    "Al desactivar el cliente, podrías afectar el servicio.\n\n" +
                                    "Recomendación:\n" +
                                    "1. Cancela las suscripciones activas primero\n" +
                                    "2. Luego desactiva el cliente\n\n" +
                                    "¿Deseas continuar de todos modos?",
                                    "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                                if (resultado == MessageBoxResult.No)
                                    return;

                                LoadingOverlay.Visibility = Visibility.Visible;
                            }
                        }

                        await _supabase.ActualizarClienteAsync(clienteActualizado);

                        MessageBox.Show("Cliente actualizado exitosamente", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await CargarClientesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al actualizar cliente:\n\n{ex.Message}",
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
            if (sender is Button button && button.Tag is Cliente cliente)
            {
                var dialog = new ClienteDetalleDialog(cliente)
                {
                    Owner = Window.GetWindow(this)
                };

                dialog.ShowDialog();
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Cliente cliente)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // Validar suscripciones activas
                    var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                    var suscripcionesActivas = suscripciones.Where(s =>
                        s.ClienteId == cliente.Id &&
                        s.Estado == "activa").ToList();

                    if (suscripcionesActivas.Any())
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;

                        var plataformas = await _supabase.ObtenerPlataformasAsync();
                        var listaPlataf = suscripcionesActivas
                            .Select(s => plataformas.FirstOrDefault(p => p.Id == s.PlataformaId)?.Nombre ?? "Desconocida")
                            .Distinct()
                            .Take(3);

                        var mensaje = $"⚠️ No se puede eliminar al cliente '{cliente.NombreCompleto}'\n\n" +
                                    $"Tiene {suscripcionesActivas.Count} suscripción(es) activa(s):\n" +
                                    $"• {string.Join("\n• ", listaPlataf)}\n\n" +
                                    "Acción requerida:\n" +
                                    "1. Cancela todas las suscripciones activas\n" +
                                    "2. Luego podrás eliminar el cliente";

                        MessageBox.Show(mensaje, "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Verificar historial de pagos
                    var pagos = await _supabase.ObtenerPagosAsync();
                    var pagosCliente = pagos.Where(p => p.ClienteId == cliente.Id).ToList();

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    string mensajeConfirm;
                    if (pagosCliente.Any())
                    {
                        var totalPagado = pagosCliente.Sum(p => p.Monto);
                        mensajeConfirm = $"⚠️ ADVERTENCIA: El cliente '{cliente.NombreCompleto}' tiene historial\n\n" +
                                       $"• {pagosCliente.Count} pago(s) registrado(s)\n" +
                                       $"• Total pagado: L {totalPagado:N2}\n\n" +
                                       "Si eliminas este cliente, se perderá el historial de pagos.\n\n" +
                                       "¿Deseas continuar?";
                    }
                    else
                    {
                        mensajeConfirm = $"¿Estás seguro de eliminar al cliente '{cliente.NombreCompleto}'?\n\n" +
                                       "Esta acción NO se puede deshacer.";
                    }

                    var confirmDialog = new ConfirmDialog(mensajeConfirm, "Eliminar Cliente", "Delete")
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (confirmDialog.ShowDialog() == true)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;
                        await _supabase.EliminarClienteAsync(cliente.Id);

                        MessageBox.Show("Cliente eliminado exitosamente", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await CargarClientesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar cliente:\n\n{ex.Message}",
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
            await CargarClientesAsync();
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