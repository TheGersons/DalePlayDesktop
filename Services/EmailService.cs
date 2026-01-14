using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace StreamManager.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> EnviarEmailAsync(string asunto, string cuerpoHtml, List<string>? destinatariosCustom = null)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"];
                var senderPassword = _configuration["Email:SenderPassword"];
                var senderName = _configuration["Email:SenderName"] ?? "StreamManager";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new InvalidOperationException("Configuración de email incompleta");
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));

                // Obtener destinatarios
                var destinatarios = destinatariosCustom ?? 
                    _configuration.GetSection("Email:DestinationEmails").Get<List<string>>() ?? 
                    new List<string>();

                if (!destinatarios.Any())
                {
                    return false; // No hay destinatarios
                }

                foreach (var dest in destinatarios)
                {
                    message.To.Add(MailboxAddress.Parse(dest));
                }

                message.Subject = asunto;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = cuerpoHtml
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error al enviar email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnviarAlertaCobroClienteAsync(
            string nombreCliente,
            string telefono,
            string plataforma,
            string perfil,
            decimal precio,
            DateTime fechaVencimiento,
            int diasRestantes,
            string estado)
        {
            var asunto = $"[COBRO] {nombreCliente} - {plataforma} {(diasRestantes >= 0 ? $"vence en {diasRestantes} días" : $"vencido hace {Math.Abs(diasRestantes)} días")}";

            var estadoEmoji = estado switch
            {
                "normal" => "🟢",
                "advertencia" => "🟡",
                "urgente" => "🟠",
                "critico" => "🔴",
                _ => "⚪"
            };

            var cuerpoHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f5f5f5; padding: 20px; margin: 20px 0; }}
        .info-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #000; }}
        .footer {{ text-align: center; color: #777; font-size: 12px; margin-top: 20px; }}
        .estado {{ font-size: 24px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{estadoEmoji} ALERTA DE COBRO</h2>
        </div>
        <div class='content'>
            <div class='info-row'>
                <span class='label'>Cliente:</span> 
                <span class='value'>{nombreCliente}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Teléfono:</span> 
                <span class='value'>{telefono}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Plataforma:</span> 
                <span class='value'>{plataforma}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Perfil:</span> 
                <span class='value'>{perfil}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Monto a cobrar:</span> 
                <span class='value'>L {precio:N2}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Fecha vencimiento:</span> 
                <span class='value'>{fechaVencimiento:dd/MM/yyyy}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Días restantes:</span> 
                <span class='value'>{(diasRestantes >= 0 ? diasRestantes.ToString() : $"Vencido hace {Math.Abs(diasRestantes)} días")}</span>
            </div>
        </div>
        <div class='footer'>
            <p>StreamManager - Sistema de Gestión de Streaming</p>
            <p>Este es un mensaje automático, no responder.</p>
        </div>
    </div>
</body>
</html>";

            return await EnviarEmailAsync(asunto, cuerpoHtml);
        }

        public async Task<bool> EnviarAlertaPagoPlataformaAsync(
            string plataforma,
            string cuentaEmail,
            decimal monto,
            DateTime fechaVencimiento,
            int diasRestantes,
            string metodoPago)
        {
            var asunto = $"[PAGO PLATAFORMA] {plataforma} {(diasRestantes >= 0 ? $"vence en {diasRestantes} días" : $"vencido")} - L {monto:N2}";

            var estadoEmoji = diasRestantes switch
            {
                >= 7 => "🔵",
                >= 3 => "🟣",
                >= 1 => "🟤",
                0 => "🔴",
                _ => "⚫"
            };

            var cuerpoHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF5722; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #fff3cd; padding: 20px; margin: 20px 0; border: 2px solid #ffc107; }}
        .info-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #000; }}
        .footer {{ text-align: center; color: #777; font-size: 12px; margin-top: 20px; }}
        .warning {{ background-color: #dc3545; color: white; padding: 10px; text-align: center; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{estadoEmoji} RECORDATORIO DE PAGO A PLATAFORMA</h2>
        </div>
        <div class='content'>
            <div class='info-row'>
                <span class='label'>Plataforma:</span> 
                <span class='value'>{plataforma}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Cuenta:</span> 
                <span class='value'>{cuentaEmail}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Monto a pagar:</span> 
                <span class='value'>L {monto:N2}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Fecha vencimiento:</span> 
                <span class='value'>{fechaVencimiento:dd/MM/yyyy}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Días restantes:</span> 
                <span class='value'>{(diasRestantes >= 0 ? diasRestantes.ToString() : "¡VENCIDO!")}</span>
            </div>
            <div class='info-row'>
                <span class='label'>Método preferido:</span> 
                <span class='value'>{metodoPago}</span>
            </div>
            {(diasRestantes < 0 ? "<div class='warning'>⚠️ ¡PAGO VENCIDO! - Realizar pago urgente</div>" : "")}
        </div>
        <div class='footer'>
            <p>¡No olvides pagar para mantener el servicio activo!</p>
            <p>StreamManager - Sistema de Gestión de Streaming</p>
        </div>
    </div>
</body>
</html>";

            return await EnviarEmailAsync(asunto, cuerpoHtml);
        }
    }
}
