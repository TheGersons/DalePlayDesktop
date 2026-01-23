using StreamManager.Data.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Views.Dialogs
{
    public partial class HistorialPagosPlataformaDialog : Window
    {
        public HistorialPagosPlataformaDialog(
            PagoPlataforma pago,
            Plataforma plataforma,
            CuentaCorreo cuenta,
            List<HistorialPagoPlataforma> historial)
        {
            InitializeComponent();
            CargarDatos(pago, plataforma, cuenta, historial);
        }

        private void CargarDatos(
            PagoPlataforma pago,
            Plataforma plataforma,
            CuentaCorreo cuenta,
            List<HistorialPagoPlataforma> historial)
        {
            // Header
            PlataformaNombreTextBlock.Text = plataforma?.Nombre ?? "N/A";
            CuentaEmailTextBlock.Text = cuenta?.Email ?? "N/A";

            // Información del Pago
            MontoMensualTextBlock.Text = $"L {pago.MontoMensual:N2}";
            ProximoPagoTextBlock.Text = pago.FechaProximoPago.ToString("dd/MM/yyyy");
            DiaPagoTextBlock.Text = $"Día {pago.DiaPagoMes}";

            // Estado
            EstadoTextBlock.Text = pago.Estado switch
            {
                "al_dia" => "Al Día",
                "por_pagar" => "Por Pagar",
                "vencido" => "Vencido",
                _ => "Desconocido"
            };

            EstadoBorder.Background = pago.Estado switch
            {
                "al_dia" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),      // Verde
                "por_pagar" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),   // Naranja
                "vencido" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),     // Rojo
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))            // Gris
            };

            // Historial
            if (historial.Any())
            {
                var historialVM = historial
                    .OrderByDescending(h => h.FechaPago)
                    .Take(15)
                    .Select(h => new HistorialPagoViewModel
                    {
                        FechaPagoTexto = h.FechaPago.ToString("dd/MM/yyyy HH:mm"),
                        MetodoPagoTexto = CapitalizarMetodo(h.MetodoPago ?? "N/A"),
                        Referencia = h.Referencia ?? "",
                        ReferenciaVisibility = string.IsNullOrWhiteSpace(h.Referencia) 
                            ? Visibility.Collapsed 
                            : Visibility.Visible,
                        MontoTexto = $"L {h.MontoPagado:N2}"
                    })
                    .ToList();

                HistorialItemsControl.ItemsSource = historialVM;
                EmptyHistorialPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                HistorialItemsControl.Visibility = Visibility.Collapsed;
                EmptyHistorialPanel.Visibility = Visibility.Visible;
            }
        }

        private string CapitalizarMetodo(string metodo)
        {
            if (string.IsNullOrWhiteSpace(metodo)) return "N/A";

            return metodo.ToLower() switch
            {
                "efectivo" => "Efectivo",
                "transferencia" => "Transferencia",
                "deposito" => "Depósito",
                "tarjeta" => "Tarjeta",
                "otro" => "Otro",
                _ => metodo
            };
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class HistorialPagoViewModel
    {
        public string FechaPagoTexto { get; set; } = string.Empty;
        public string MetodoPagoTexto { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public Visibility ReferenciaVisibility { get; set; }
        public string MontoTexto { get; set; } = string.Empty;
    }
}
