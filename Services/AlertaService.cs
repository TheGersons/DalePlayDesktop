using StreamManager.Data.Models;

namespace StreamManager.Services
{
    public class AlertaService
    {
        private readonly SupabaseService _supabase;
        private readonly EmailService _email;

        public AlertaService(SupabaseService supabase, EmailService email)
        {
            _supabase = supabase;
            _email = email;
        }

        public async Task GenerarAlertasAutomaticasAsync()
        {
            await GenerarAlertasCobroClientesAsync();
            await GenerarAlertasPagoPlataformasAsync();
        }

        private async Task GenerarAlertasCobroClientesAsync()
        {
            var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
            var suscripcionesActivas = suscripciones.Where(s => s.Estado == "activa").ToList();

            var diasAlerta = new[] { 7, 3, 1, 0 };
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            foreach (var suscripcion in suscripcionesActivas)
            {
                var diasRestantes = suscripcion.FechaProximoPago.DayNumber - hoy.DayNumber;

                // Verificar si necesita alerta
                if (diasRestantes > 7 && diasRestantes >= 0)
                    continue; // Aún no necesita alerta

                // Determinar nivel de alerta
                string nivel;
                if (diasRestantes >= 7)
                    nivel = "normal";
                else if (diasRestantes >= 3)
                    nivel = "advertencia";
                else if (diasRestantes >= 1)
                    nivel = "urgente";
                else
                    nivel = "critico";

                // Verificar si ya existe alerta similar reciente
                var alertasExistentes = await _supabase.ObtenerAlertasAsync();
                var alertaExistente = alertasExistentes.FirstOrDefault(a =>
                    a.TipoAlerta == "cobro_cliente" &&
                    a.EntidadId == suscripcion.Id &&
                    a.Estado == "pendiente");

                if (alertaExistente == null || alertaExistente.Nivel != nivel)
                {
                    // Crear nueva alerta
                    var mensaje = diasRestantes >= 0
                        ? $"Cliente debe pagar en {diasRestantes} día(s)"
                        : $"Pago vencido hace {Math.Abs(diasRestantes)} día(s)";

                    var alerta = new Alerta
                    {
                        TipoAlerta = "cobro_cliente",
                        TipoEntidad = "suscripcion",
                        EntidadId = suscripcion.Id,
                        ClienteId = suscripcion.ClienteId,
                        PlataformaId = suscripcion.PlataformaId,
                        Nivel = nivel,
                        DiasRestantes = diasRestantes,
                        Monto = suscripcion.Precio,
                        Mensaje = mensaje,
                        Estado = "pendiente"
                    };

                    await _supabase.CrearAlertaAsync(alerta);

                    // Si ya pasó fecha límite, actualizar estado de suscripción
                    if (hoy > suscripcion.FechaLimitePago && suscripcion.Estado != "vencida")
                    {
                        suscripcion.Estado = "vencida";
                        await _supabase.ActualizarSuscripcionAsync(suscripcion);
                    }
                }
            }
        }

        private async Task GenerarAlertasPagoPlataformasAsync()
        {
            var pagosPlataf = await _supabase.ObtenerPagosPlataformaAsync();
            var pagosPendientes = pagosPlataf.Where(p => p.Estado != "al_dia").ToList();

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            foreach (var pago in pagosPendientes)
            {
                var diasRestantes = pago.FechaProximoPago.DayNumber - hoy.DayNumber;

                // Verificar si necesita alerta
                if (diasRestantes > 7 && diasRestantes >= 0)
                    continue;

                // Determinar nivel
                string nivel;
                if (diasRestantes >= 7)
                    nivel = "normal";
                else if (diasRestantes >= 3)
                    nivel = "advertencia";
                else if (diasRestantes >= 1)
                    nivel = "urgente";
                else
                    nivel = "critico";

                // Verificar alerta existente
                var alertasExistentes = await _supabase.ObtenerAlertasAsync();
                var alertaExistente = alertasExistentes.FirstOrDefault(a =>
                    a.TipoAlerta == "pago_plataforma" &&
                    a.EntidadId == pago.Id &&
                    a.Estado == "pendiente");

                if (alertaExistente == null || alertaExistente.Nivel != nivel)
                {
                    var mensaje = diasRestantes >= 0
                        ? $"Pagar plataforma en {diasRestantes} día(s)"
                        : $"Pago a plataforma vencido hace {Math.Abs(diasRestantes)} día(s)";

                    var alerta = new Alerta
                    {
                        TipoAlerta = "pago_plataforma",
                        TipoEntidad = "pago_plataforma",
                        EntidadId = pago.Id,
                        PlataformaId = pago.PlataformaId,
                        Nivel = nivel,
                        DiasRestantes = diasRestantes,
                        Monto = pago.MontoMensual,
                        Mensaje = mensaje,
                        Estado = "pendiente"
                    };

                    await _supabase.CrearAlertaAsync(alerta);

                    // Actualizar estado del pago
                    if (hoy > pago.FechaLimitePago)
                    {
                        pago.Estado = "vencido";
                        await _supabase.ActualizarPagoPlataformaAsync(pago);
                    }
                    else if (diasRestantes <= 7)
                    {
                        pago.Estado = "por_pagar";
                        await _supabase.ActualizarPagoPlataformaAsync(pago);
                    }
                }
            }
        }

        public async Task EnviarEmailsAlertasPendientesAsync()
        {
            var alertasPendientes = await _supabase.ObtenerAlertasPendientesAsync();

            foreach (var alerta in alertasPendientes)
            {
                bool enviado = false;

                if (alerta.TipoAlerta == "cobro_cliente")
                {
                    enviado = await EnviarEmailCobroClienteAsync(alerta);
                }
                else if (alerta.TipoAlerta == "pago_plataforma")
                {
                    enviado = await EnviarEmailPagoPlataformaAsync(alerta);
                }

                if (enviado)
                {
                    alerta.Estado = "enviada";
                    alerta.FechaEnvioEmail = DateTime.Now;
                    await _supabase.ActualizarAlertaAsync(alerta);
                }
            }
        }

        private async Task<bool> EnviarEmailCobroClienteAsync(Alerta alerta)
        {
            try
            {
                // Obtener datos completos
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var suscripcion = suscripciones.FirstOrDefault(s => s.Id == alerta.EntidadId);

                if (suscripcion == null)
                    return false;

                var clientes = await _supabase.ObtenerClientesAsync();
                var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);

                var plataformas = await _supabase.ObtenerPlataformasAsync();
                var plataforma = plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId);

                var perfilNombre = "Cuenta Completa";
                if (suscripcion.PerfilId.HasValue)
                {
                    var perfiles = await _supabase.ObtenerPerfilesAsync();
                    var perfil = perfiles.FirstOrDefault(p => p.Id == suscripcion.PerfilId);
                    perfilNombre = perfil?.NombrePerfil ?? perfilNombre;
                }

                if (cliente == null || plataforma == null)
                    return false;

                return await _email.EnviarAlertaCobroClienteAsync(
                    cliente.NombreCompleto,
                    cliente.Telefono,
                    plataforma.Nombre,
                    perfilNombre,
                    suscripcion.Precio,
                    suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue),
                    alerta.DiasRestantes ?? 0,
                    alerta.Nivel
                );
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> EnviarEmailPagoPlataformaAsync(Alerta alerta)
        {
            try
            {
                var pagosPlataf = await _supabase.ObtenerPagosPlataformaAsync();
                var pago = pagosPlataf.FirstOrDefault(p => p.Id == alerta.EntidadId);

                if (pago == null)
                    return false;

                var cuentas = await _supabase.ObtenerCuentasAsync();
                var cuenta = cuentas.FirstOrDefault(c => c.Id == pago.CuentaId);

                var plataformas = await _supabase.ObtenerPlataformasAsync();
                var plataforma = plataformas.FirstOrDefault(p => p.Id == pago.PlataformaId);

                if (cuenta == null || plataforma == null)
                    return false;

                return await _email.EnviarAlertaPagoPlataformaAsync(
                    plataforma.Nombre,
                    cuenta.Email,
                    pago.MontoMensual,
                    pago.FechaProximoPago.ToDateTime(TimeOnly.MinValue),
                    alerta.DiasRestantes ?? 0,
                    pago.MetodoPagoPreferido
                );
            }
            catch
            {
                return false;
            }
        }

        public async Task MarcarAlertaComoLeidaAsync(Guid alertaId)
        {
            var alertas = await _supabase.ObtenerAlertasAsync();
            var alerta = alertas.FirstOrDefault(a => a.Id == alertaId);

            if (alerta != null)
            {
                alerta.Estado = "leida";
                await _supabase.ActualizarAlertaAsync(alerta);
            }
        }

        public async Task ResolverAlertaAsync(Guid alertaId)
        {
            var alertas = await _supabase.ObtenerAlertasAsync();
            var alerta = alertas.FirstOrDefault(a => a.Id == alertaId);

            if (alerta != null)
            {
                alerta.Estado = "resuelta";
                await _supabase.ActualizarAlertaAsync(alerta);
            }
        }

        /// <summary>
        /// Obtener conteo de alertas pendientes
        /// </summary>
        public async Task<int> ObtenerConteoAlertasPendientesAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                return alertas.Count(a => a.Estado == "pendiente");
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Marcar todas las alertas como leídas
        /// </summary>
        public async Task MarcarTodasComoLeidasAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alertasPendientes = alertas.Where(a => a.Estado == "pendiente").ToList();

                foreach (var alerta in alertasPendientes)
                {
                    alerta.Estado = "leida";
                    await _supabase.ActualizarAlertaAsync(alerta);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al marcar todas como leídas: {ex.Message}");
            }
        }

        /// <summary>
        /// Eliminar alertas leídas/resueltas con más de 30 días
        /// </summary>
        public async Task LimpiarAlertasAntiguasAsync()
        {
            try
            {
                var fechaLimite = DateTime.Now.AddDays(-30);
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alertasAntiguas = alertas.Where(a =>
                    a.FechaCreacion < fechaLimite &&
                    (a.Estado == "leida" || a.Estado == "resuelta")).ToList();

                foreach (var alerta in alertasAntiguas)
                {
                    await _supabase.EliminarAlertaAsync(alerta.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar alertas antiguas: {ex.Message}");
            }
        }
    }
}