using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StreamManager.Converters
{
    public class EstadoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string estado)
            {
                return estado.ToLower() switch
                {
                    "activa" or "activo" or "disponible" or "al_dia" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Verde
                    "inactiva" or "inactivo" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gris
                    "ocupado" or "vencida" or "vencido" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Rojo
                    "por_pagar" or "pendiente" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Amarillo
                    "cancelada" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),  // Gris azulado
                    _ => new SolidColorBrush(Color.FromRgb(33, 150, 243))  // Azul default
                };
            }

            return new SolidColorBrush(Color.FromRgb(33, 150, 243));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Sí" : "No";
            }
            return "No";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}