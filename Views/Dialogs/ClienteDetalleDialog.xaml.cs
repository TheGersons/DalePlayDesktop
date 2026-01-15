using StreamManager.Data.Models;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Views.Dialogs
{
    public partial class ClienteDetalleDialog : Window
    {
        public ClienteDetalleDialog(Cliente cliente)
        {
            InitializeComponent();

            CargarDatos(cliente);
        }

        private void CargarDatos(Cliente cliente) 
        {
            NombreTextBlock.Text = cliente.NombreCompleto;
            TelefonoTextBlock.Text = cliente.Telefono ?? "No especificado.";

            FechaRegistroTextBlock.Text = cliente.FechaRegistro.ToString("dd/MM/yyyy HH:mm");
            
            EstadoTextBlock.Text = cliente.Estado;
            
            // Color del estado
            EstadoBorder.Background = cliente.Estado.ToLower() == "activo"
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Verde
                : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris

            NotasTextBlock.Text = string.IsNullOrWhiteSpace(cliente.Notas) 
                ? "Sin notas" 
                : cliente.Notas;
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
