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

                _clientes.Clear();
                foreach (var cliente in _todosClientes)
                {
                    _clientes.Add(cliente);
                }
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

                        await _supabase.ActualizarClienteAsync(dialog.Cliente);

                        MessageBox.Show(
                            "Cliente actualizado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarClientesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar cliente: {ex.Message}",
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
                var confirmDialog = new ConfirmDialog(
                    $"¿Estás seguro de eliminar al cliente '{cliente.NombreCompleto}'?\n\n" +
                    "⚠️ ADVERTENCIA: Esta acción eliminará:\n" +
                    "• Todas las suscripciones del cliente\n" +
                    "• Todos los pagos asociados\n\n" +
                    "Esta acción NO se puede deshacer.",
                    "Eliminar Cliente",
                    "Delete")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarClienteAsync(cliente.Id);

                        MessageBox.Show(
                            "Cliente eliminado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarClientesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al eliminar cliente: {ex.Message}",
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
            await CargarClientesAsync();
        }

        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var busqueda = BuscarTextBox.Text.ToLower();

            var filtrados = string.IsNullOrWhiteSpace(busqueda)
                ? _todosClientes
                : _todosClientes.Where(c =>
                    c.NombreCompleto.ToLower().Contains(busqueda) ||
                    (c.Telefono?.Contains(busqueda) ?? false)).ToList();

            _clientes.Clear();
            foreach (var cliente in filtrados)
            {
                _clientes.Add(cliente);
            }
        }
    }
}
