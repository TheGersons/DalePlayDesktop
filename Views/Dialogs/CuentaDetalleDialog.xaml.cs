using StreamManager.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Views.Dialogs
{
    public partial class CuentaDetalleDialog : Window
    {
        private readonly CuentaViewModel _cuenta;
        private bool _contraseñaVisible = false;

        public CuentaDetalleDialog(CuentaViewModel cuenta)
        {
            InitializeComponent();

            _cuenta = cuenta;
            CargarDatos();
        }

        private void CargarDatos()
        {
            // Header
            PlataformaTextBlock.Text = _cuenta.PlataformaNombre;
            CorreoTextBlock.Text = _cuenta.CorreoElectronico;

            // Información de Cuenta
            CorreoDetalleTextBlock.Text = _cuenta.CorreoElectronico;
            FechaCreacionTextBlock.Text = _cuenta.FechaCreacion.ToString("dd/MM/yyyy HH:mm");

            // Estado
            EstadoTextBlock.Text = _cuenta.Estado;
            EstadoBorder.Background = _cuenta.Estado.ToLower() == "activa"
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Verde
                : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris

            // Perfiles
            PerfilesDisponiblesTextBlock.Text = $"{_cuenta.PerfilesDisponibles} {(_cuenta.PerfilesDisponibles == 1 ? "perfil disponible" : "perfiles disponibles")}";

            // Notas
            NotasTextBlock.Text = string.IsNullOrWhiteSpace(_cuenta.Notas)
                ? "Sin notas"
                : _cuenta.Notas;
        }

        private void MostrarContraseñaButton_Click(object sender, RoutedEventArgs e)
        {
            _contraseñaVisible = !_contraseñaVisible;

            if (_contraseñaVisible)
            {
                ContraseñaTextBlock.Text = _cuenta.Contraseña;
                MostrarContraseñaButton.Content = "OCULTAR";
            }
            else
            {
                ContraseñaTextBlock.Text = "••••••••";
                MostrarContraseñaButton.Content = "MOSTRAR";
            }
        }

        private void CopiarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_cuenta.Contraseña);
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

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
