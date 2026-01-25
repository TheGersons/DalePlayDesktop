using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Pages
{
    public partial class UsuariosPage : Page
    {
        private readonly SupabaseService _supabase;
        private List<AuthUser> _todosUsuarios = new();

        public UsuariosPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            Loaded += UsuariosPage_Loaded;
        }

        private async void UsuariosPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarUsuariosAsync();
        }

        private async Task CargarUsuariosAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                _todosUsuarios = await _supabase.ObtenerUsuariosAsync();

                if (_todosUsuarios.Any())
                {
                    var usuariosVM = _todosUsuarios.Select(u => new UsuarioViewModel
                    {
                        Id = u.Id,
                        NombreCompleto = u.NombreCompleto,
                        Email = u.Email,
                        RolTexto = u.Rol == "admin" ? "Administrador" : "Vendedor",
                        RolIcono = u.Rol == "admin" ? "ShieldCrown" : "AccountTie",
                        RolColor = new SolidColorBrush(u.Rol == "admin" 
                            ? Color.FromRgb(156, 39, 176)  // Morado
                            : Color.FromRgb(33, 150, 243)), // Azul
                        EstadoTexto = u.Estado == "activo" ? "Activo" : "Inactivo",
                        EstadoColor = new SolidColorBrush(u.Estado == "activo"
                            ? Color.FromRgb(76, 175, 80)   // Verde
                            : Color.FromRgb(158, 158, 158)), // Gris
                        FechaCreacionTexto = u.FechaCreacion.ToString("dd/MM/yyyy"),
                        Usuario = u
                    }).OrderBy(u => u.NombreCompleto).ToList();

                    UsuariosItemsControl.ItemsSource = usuariosVM;
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    UsuariosItemsControl.ItemsSource = null;
                    EmptyStatePanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar usuarios: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void NuevoUsuarioButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UsuarioDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                _ = CargarUsuariosAsync();
            }
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var usuario = _todosUsuarios.FirstOrDefault(u => u.Id == userId);
                if (usuario == null) return;

                var dialog = new UsuarioDialog(usuario)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    _ = CargarUsuariosAsync();
                }
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var usuario = _todosUsuarios.FirstOrDefault(u => u.Id == userId);
                if (usuario == null) return;

                // No permitir eliminar el usuario actual
                if (UserSession.CurrentUser?.Id == userId)
                {
                    MessageBox.Show(
                        "No puedes eliminar tu propio usuario.",
                        "Acción no permitida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var resultado = MessageBox.Show(
                    $"¿Estás seguro de eliminar al usuario?\n\n" +
                    $"Nombre: {usuario.NombreCompleto}\n" +
                    $"Email: {usuario.Email}\n" +
                    $"Rol: {(usuario.Rol == "admin" ? "Administrador" : "Vendedor")}\n\n" +
                    "Esta acción no se puede deshacer.",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarUsuarioAsync(userId);

                        MessageBox.Show(
                            "Usuario eliminado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarUsuariosAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al eliminar usuario: {ex.Message}",
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
            await CargarUsuariosAsync();
        }

        // Clase auxiliar
        private class UsuarioViewModel
        {
            public Guid Id { get; set; }
            public string NombreCompleto { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string RolTexto { get; set; } = string.Empty;
            public string RolIcono { get; set; } = string.Empty;
            public Brush RolColor { get; set; } = Brushes.Blue;
            public string EstadoTexto { get; set; } = string.Empty;
            public Brush EstadoColor { get; set; } = Brushes.Green;
            public string FechaCreacionTexto { get; set; } = string.Empty;
            public AuthUser Usuario { get; set; } = null!;
        }
    }
}
