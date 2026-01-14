using System.Windows;
using MaterialDesignThemes.Wpf;

namespace StreamManager.Views.Dialogs
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string mensaje, string titulo = "Confirmar acción", string icono = "AlertCircle")
        {
            InitializeComponent();

            TituloTextBlock.Text = titulo;
            MensajeTextBlock.Text = mensaje;

            // Cambiar icono según el tipo
            IconoPrincipal.Kind = icono switch
            {
                "Warning" => PackIconKind.AlertCircle,
                "Delete" => PackIconKind.DeleteForever,
                "Info" => PackIconKind.Information,
                "Question" => PackIconKind.HelpCircle,
                _ => PackIconKind.AlertCircle
            };

            // Si es eliminar, botón rojo
            if (icono == "Delete")
            {
                ConfirmarButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54)); // Rojo
            }
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
