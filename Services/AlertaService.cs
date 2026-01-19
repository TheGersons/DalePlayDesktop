using StreamManager.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamManager.Services
{
    public class AlertaService
    {
        private readonly SupabaseService _supabase;

        public AlertaService(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        /// <summary>
        /// Genera todas las alertas automáticamente
        /// </summary>
        public async Task GenerarAlertasAutomaticasAsync()
        {
            await GenerarAlertasCobrosAsync();
            await GenerarAlertasPagosPlataformaAsync();
        }

        /// <summary>
        /// Genera alertas de cobros pendientes a clientes
        /// ✅ SOLO VENCIDAS O VENCEN HOY
        /// </summary>
        public async Task GenerarAlertasCobrosAsync()
        {
            try
            {
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var activas = suscripciones.Where(s => s.Estado == "activa").ToList();

                foreach (var suscripcion in activas)
                {
                    var diasRestantes = (suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    // ✅ CAMBIO: Solo vencidas (< 0) o vencen hoy (= 0)
                    if (diasRestantes <= 0)
                    {
                        var alertas = await _supabase.ObtenerAlertasAsync();
                        var alertaExistente = alertas.FirstOrDefault(a =>
                            a.TipoAlerta == "cobro_cliente" &&
                            a.EntidadId == suscripcion.Id &&
                            a.Estado == "pendiente");

                        if (alertaExistente == null)
                        {
                            var clientes = await _supabase.ObtenerClientesAsync();
                            var plataformas = await _supabase.ObtenerPlataformasAsync();

                            var cliente = clientes.FirstOrDefault(c => c.Id == suscripcion.ClienteId);
                            var plataforma = plataformas.FirstOrDefault(p => p.Id == suscripcion.PlataformaId);

                            if (cliente == null || plataforma == null) continue;

                            string mensaje;
                            if (diasRestantes < 0)
                            {
                                var diasVencidos = Math.Abs(diasRestantes);
                                mensaje = diasVencidos == 1
                                    ? $"{cliente.NombreCompleto} - {plataforma.Nombre} vencido hace 1 día - L {suscripcion.Precio:N2}"
                                    : $"{cliente.NombreCompleto} - {plataforma.Nombre} vencido hace {diasVencidos} días - L {suscripcion.Precio:N2}";
                            }
                            else
                            {
                                mensaje = $"{cliente.NombreCompleto} - {plataforma.Nombre} vence HOY - L {suscripcion.Precio:N2}";
                            }

                            var alerta = new Alerta
                            {
                                TipoAlerta = "cobro_cliente",
                                TipoEntidad = "suscripcion",
                                EntidadId = suscripcion.Id,
                                ClienteId = suscripcion.ClienteId,
                                PlataformaId = suscripcion.PlataformaId,
                                Nivel = "critico",
                                DiasRestantes = diasRestantes,
                                Monto = suscripcion.Precio,
                                Mensaje = mensaje,
                                Estado = "pendiente",
                                FechaCreacion = DateTime.Now
                            };

                            await _supabase.CrearAlertaAsync(alerta);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al generar alertas de cobros: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Genera alertas de pagos pendientes a plataformas
        /// ✅ CORREGIDO: SOLO VENCIDAS O VENCEN HOY
        /// </summary>
        public async Task GenerarAlertasPagosPlataformaAsync()
        {
            try
            {
                var pagosPendientes = await _supabase.ObtenerPagosPlataformaAsync();

                // ✅ CORRECCIÓN: Estados correctos
                var pendientes = pagosPendientes
                    .Where(p => p.Estado == "por_pagar" || p.Estado == "vencido")
                    .ToList();

                foreach (var pago in pendientes)
                {
                    // ✅ CORRECCIÓN: Campo correcto
                    var diasRestantes = (pago.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    // ✅ CAMBIO: Solo vencidas (< 0) o vencen hoy (= 0)
                    if (diasRestantes <= 0)
                    {
                        var alertas = await _supabase.ObtenerAlertasAsync();
                        var alertaExistente = alertas.FirstOrDefault(a =>
                            a.TipoAlerta == "pago_plataforma" &&
                            a.EntidadId == pago.Id &&
                            a.Estado == "pendiente");

                        if (alertaExistente == null)
                        {
                            var plataformas = await _supabase.ObtenerPlataformasAsync();
                            var plataforma = plataformas.FirstOrDefault(p => p.Id == pago.PlataformaId);

                            string mensaje;
                            if (diasRestantes < 0)
                            {
                                var diasVencidos = Math.Abs(diasRestantes);
                                mensaje = diasVencidos == 1
                                    ? $"Pago a {plataforma?.Nombre ?? "Plataforma"} vencido hace 1 día - L {pago.MontoMensual:N2}"
                                    : $"Pago a {plataforma?.Nombre ?? "Plataforma"} vencido hace {diasVencidos} días - L {pago.MontoMensual:N2}";
                            }
                            else
                            {
                                mensaje = $"Pago a {plataforma?.Nombre ?? "Plataforma"} vence HOY - L {pago.MontoMensual:N2}";
                            }

                            var alerta = new Alerta
                            {
                                TipoAlerta = "pago_plataforma",
                                TipoEntidad = "pago_plataforma",
                                EntidadId = pago.Id,
                                PlataformaId = pago.PlataformaId,
                                Nivel = "critico",
                                DiasRestantes = diasRestantes,
                                Monto = pago.MontoMensual,
                                Mensaje = mensaje,
                                Estado = "pendiente",
                                FechaCreacion = DateTime.Now
                            };

                            await _supabase.CrearAlertaAsync(alerta);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al generar alertas de pagos a plataforma: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ✅ NUEVO: Resolver alerta cuando se registra un pago a plataforma
        /// </summary>
        public async Task ResolverAlertasPagoPlataformaAsync(Guid pagoPlataformaId)
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alertaPendiente = alertas.FirstOrDefault(a =>
                    a.TipoAlerta == "pago_plataforma" &&
                    a.EntidadId == pagoPlataformaId &&
                    a.Estado == "pendiente");

                if (alertaPendiente != null)
                {
                    await ResolverAlertaAsync(alertaPendiente.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al resolver alerta de pago plataforma: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NUEVO: Resolver alerta cuando se registra un pago de cliente
        /// </summary>
        public async Task ResolverAlertasCobroClienteAsync(Guid suscripcionId)
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alertaPendiente = alertas.FirstOrDefault(a =>
                    a.TipoAlerta == "cobro_cliente" &&
                    a.EntidadId == suscripcionId &&
                    a.Estado == "pendiente");

                if (alertaPendiente != null)
                {
                    await ResolverAlertaAsync(alertaPendiente.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al resolver alerta de cobro cliente: {ex.Message}");
            }
        }

        /// <summary>
        /// Marca todas las alertas como leídas
        /// </summary>
        public async Task MarcarTodasComoLeidasAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var pendientes = alertas.Where(a => a.Estado == "pendiente").ToList();

                foreach (var alerta in pendientes)
                {
                    await MarcarAlertaComoLeidaAsync(alerta.Id);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al marcar alertas como leídas: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Limpia alertas antiguas (más de 30 días)
        /// </summary>
        public async Task LimpiarAlertasAntiguasAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var antiguas = alertas
                    .Where(a => a.FechaCreacion < DateTime.Now.AddDays(-30))
                    .ToList();

                foreach (var alerta in antiguas)
                {
                    await _supabase.EliminarAlertaAsync(alerta.Id);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al limpiar alertas antiguas: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Limpia alertas resueltas
        /// </summary>
        public async Task LimpiarAlertasResueltasAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var resueltas = alertas.Where(a => a.Estado == "resuelta").ToList();

                foreach (var alerta in resueltas)
                {
                    await _supabase.EliminarAlertaAsync(alerta.Id);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al limpiar alertas resueltas: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resuelve una alerta específica
        /// </summary>
        public async Task ResolverAlertaAsync(Guid alertaId)
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alerta = alertas.FirstOrDefault(a => a.Id == alertaId);

                if (alerta != null)
                {
                    alerta.Estado = "resuelta";
                    await _supabase.ActualizarAlertaAsync(alerta);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al resolver alerta: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Marca una alerta como leída
        /// </summary>
        public async Task MarcarAlertaComoLeidaAsync(Guid alertaId)
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alerta = alertas.FirstOrDefault(a => a.Id == alertaId);

                if (alerta != null)
                {
                    alerta.Estado = "leida";
                    await _supabase.ActualizarAlertaAsync(alerta);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al marcar alerta como leída: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene el conteo de alertas pendientes
        /// </summary>
        public async Task<int> ObtenerConteoAlertasPendientesAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                return alertas.Count(a => a.Estado == "pendiente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener conteo de alertas: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Obtiene todas las alertas pendientes
        /// </summary>
        public async Task<List<Alerta>> ObtenerAlertasPendientesAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                return alertas
                    .Where(a => a.Estado == "pendiente")
                    .OrderByDescending(a => a.FechaCreacion)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener alertas pendientes: {ex.Message}");
                return new List<Alerta>();
            }
        }

        /// <summary>
        /// Obtiene alertas por tipo
        /// </summary>
        public async Task<List<Alerta>> ObtenerAlertasPorTipoAsync(string tipoAlerta)
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                return alertas
                    .Where(a => a.TipoAlerta == tipoAlerta && a.Estado == "pendiente")
                    .OrderByDescending(a => a.FechaCreacion)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener alertas por tipo: {ex.Message}");
                return new List<Alerta>();
            }
        }
    }
}