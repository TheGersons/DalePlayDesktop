using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;

namespace StreamManager.Utilities
{
    /// <summary>
    /// Utilidades para validaciones comunes en la aplicación
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Valida si un email tiene formato correcto
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email.Trim(), emailRegex);
        }

        /// <summary>
        /// Valida si un número de teléfono tiene formato válido
        /// </summary>
        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Acepta formatos: 99999999, 9999-9999, +504 9999-9999
            var phoneRegex = @"^(\+\d{1,3}\s?)?\d{4}-?\d{4}$";
            return Regex.IsMatch(phone.Trim(), phoneRegex);
        }

        /// <summary>
        /// Valida si un texto contiene solo números y punto decimal
        /// </summary>
        public static bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, @"^[0-9.]+$");
        }

        /// <summary>
        /// Valida si una cuenta puede ser eliminada
        /// </summary>
        public static async Task<(bool canDelete, string message)> ValidateAccountDeletion(
            Guid accountId, 
            string accountEmail,
            string platformName,
            SupabaseService supabase)
        {
            // Verificar perfiles asociados
            var perfiles = await supabase.ObtenerPerfilesAsync();
            var perfilesAsociados = perfiles.Where(p => p.CuentaId == accountId).ToList();

            if (perfilesAsociados.Any())
            {
                var perfilesOcupados = perfilesAsociados.Count(p => p.Estado == "ocupado");
                var perfilesDisponibles = perfilesAsociados.Count(p => p.Estado == "disponible");

                var mensaje = $"⚠️ No se puede eliminar la cuenta '{accountEmail}'\n\n" +
                            "Esta cuenta tiene perfiles asociados:\n\n" +
                            $"• {perfilesOcupados} perfil(es) ocupado(s)\n" +
                            $"• {perfilesDisponibles} perfil(es) disponible(s)\n\n" +
                            "Acción requerida:\n" +
                            "1. Elimina o reasigna todos los perfiles asociados\n" +
                            "2. Cancela las suscripciones activas que usan estos perfiles\n" +
                            "3. Luego podrás eliminar la cuenta";

                return (false, mensaje);
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida si un perfil puede ser eliminado
        /// </summary>
        public static async Task<(bool canDelete, string message)> ValidateProfileDeletion(
            Guid profileId,
            string profileName,
            SupabaseService supabase)
        {
            var suscripciones = await supabase.ObtenerSuscripcionesAsync();
            var suscripcionesActivas = suscripciones.Where(s => 
                s.PerfilId == profileId && 
                s.Estado == "activa").ToList();

            if (suscripcionesActivas.Any())
            {
                var mensaje = $"⚠️ No se puede eliminar el perfil '{profileName}'\n\n" +
                            $"Este perfil tiene {suscripcionesActivas.Count} suscripción(es) activa(s)\n\n" +
                            "Acción requerida:\n" +
                            "1. Cancela o finaliza todas las suscripciones activas\n" +
                            "2. Luego podrás eliminar el perfil";

                return (false, mensaje);
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida si una plataforma puede ser eliminada
        /// </summary>
        public static async Task<(bool canDelete, string message)> ValidatePlatformDeletion(
            Guid platformId,
            string platformName,
            SupabaseService supabase)
        {
            var cuentas = await supabase.ObtenerCuentasAsync();
            var cuentasAsociadas = cuentas.Where(c => c.PlataformaId == platformId).ToList();

            if (cuentasAsociadas.Any())
            {
                var mensaje = $"⚠️ No se puede eliminar la plataforma '{platformName}'\n\n" +
                            $"Esta plataforma tiene {cuentasAsociadas.Count} cuenta(s) asociada(s)\n\n" +
                            "Acción requerida:\n" +
                            "1. Elimina o reasigna todas las cuentas asociadas\n" +
                            "2. Luego podrás eliminar la plataforma";

                return (false, mensaje);
            }

            var suscripciones = await supabase.ObtenerSuscripcionesAsync();
            var suscripcionesActivas = suscripciones.Where(s => 
                s.PlataformaId == platformId && 
                s.Estado == "activa").ToList();

            if (suscripcionesActivas.Any())
            {
                var mensaje = $"⚠️ No se puede eliminar la plataforma '{platformName}'\n\n" +
                            $"Existen {suscripcionesActivas.Count} suscripción(es) activa(s)\n\n" +
                            "Acción requerida:\n" +
                            "1. Cancela o finaliza todas las suscripciones activas\n" +
                            "2. Luego podrás eliminar la plataforma";

                return (false, mensaje);
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida si un cliente puede ser eliminado
        /// </summary>
        public static async Task<(bool canDelete, string message)> ValidateClientDeletion(
            Guid clientId,
            string clientName,
            SupabaseService supabase)
        {
            var suscripciones = await supabase.ObtenerSuscripcionesAsync();
            var suscripcionesActivas = suscripciones.Where(s => 
                s.ClienteId == clientId && 
                s.Estado == "activa").ToList();

            if (suscripcionesActivas.Any())
            {
                var mensaje = $"⚠️ No se puede eliminar el cliente '{clientName}'\n\n" +
                            $"Este cliente tiene {suscripcionesActivas.Count} suscripción(es) activa(s)\n\n" +
                            "Acción requerida:\n" +
                            "1. Cancela o finaliza todas las suscripciones activas\n" +
                            "2. Luego podrás eliminar el cliente";

                return (false, mensaje);
            }

            var pagos = await supabase.ObtenerPagosAsync();
            var pagosAsociados = pagos.Where(p => p.ClienteId == clientId).ToList();

            if (pagosAsociados.Any())
            {
                var resultado = MessageBox.Show(
                    $"⚠️ El cliente '{clientName}' tiene {pagosAsociados.Count} pago(s) registrado(s)\n\n" +
                    "Si eliminas este cliente, se perderá el historial de pagos.\n\n" +
                    "¿Deseas continuar de todos modos?",
                    "Advertencia - Historial de pagos",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.No)
                {
                    return (false, "Eliminación cancelada por el usuario");
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida si un email ya existe en el sistema (excepto el ID especificado)
        /// </summary>
        public static async Task<bool> IsEmailDuplicate(
            string email, 
            Guid? excludeAccountId,
            SupabaseService supabase)
        {
            var cuentas = await supabase.ObtenerCuentasAsync();
            
            if (excludeAccountId.HasValue)
            {
                return cuentas.Any(c => 
                    c.Id != excludeAccountId.Value && 
                    c.Email.ToLower() == email.ToLower());
            }

            return cuentas.Any(c => c.Email.ToLower() == email.ToLower());
        }

        /// <summary>
        /// Muestra un mensaje de error con formato consistente
        /// </summary>
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Muestra un mensaje de advertencia con formato consistente
        /// </summary>
        public static void ShowWarning(string message, string title = "Advertencia")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// Muestra un mensaje de éxito con formato consistente
        /// </summary>
        public static void ShowSuccess(string message, string title = "Éxito")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Muestra un diálogo de confirmación
        /// </summary>
        public static bool ShowConfirmation(string message, string title = "Confirmación")
        {
            var resultado = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return resultado == MessageBoxResult.Yes;
        }
    }
}
