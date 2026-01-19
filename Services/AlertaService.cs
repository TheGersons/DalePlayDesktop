using StreamManager.Data.Models;
using Supabase;
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
        /// Genera TODAS las alertas automáticas del sistema
        /// </summary>
        public async Task GenerarAlertasAutomaticasAsync()
        {
            await GenerarAlertasCobrosAsync();
            await GenerarAlertasPagosPlataformaAsync();
            // Agregar otras generaciones de alertas aquí si es necesario
        }

        /// <summary>
        /// Genera alertas de cobros a clientes
        /// ✅ SOLO para suscripciones vencidas o que vencen HOY
        /// </summary>
        public async Task GenerarAlertasCobrosAsync()
        {
            try
            {
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var suscripcionesActivas = suscripciones.Where(s => s.Estado == "activa").ToList();

                var fechaHoy = DateOnly.FromDateTime(DateTime.Today);

                foreach (var suscripcion in suscripcionesActivas)
                {
                    var diasRestantes = (suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    // ✅ SOLO generar alertas si diasRestantes <= 0
                    if (diasRestantes <= 0)
                    {
                        var alertas = await _supabase.ObtenerAlertasAsync();
                        var alertaExistente = alertas.FirstOrDefault(a =>
                            a.TipoAlerta == "cobro_cliente" &&
                            a.EntidadId == suscripcion.Id &&
                            a.Estado == "pendiente");

                        if (alertaExistente == null)
                        {
                            string nivel;
                            string mensaje;

                            if (diasRestantes < 0)
                            {
                                // VENCIDA
                                nivel = "critico";
                                var diasVencidos = Math.Abs(diasRestantes);
                                mensaje = diasVencidos == 1
                                    ? $"Pago vencido hace 1 día - L {suscripcion.Precio:N2}"
                                    : $"Pago vencido hace {diasVencidos} días - L {suscripcion.Precio:N2}";
                            }
                            else
                            {
                                // VENCE HOY
                                nivel = "critico";
                                mensaje = $"Cliente debe pagar HOY - L {suscripcion.Precio:N2}";
                            }

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
        /// ✅ CORREGIDO: Usa fecha_proximo_pago y estados correctos
        /// </summary>
        public async Task GenerarAlertasPagosPlataformaAsync()
        {
            try
            {
                var pagosPendientes = await _supabase.ObtenerPagosPlataformaAsync();
                // ✅ CORRECCIÓN: Los estados son "por_pagar" o "vencido", no "pendiente"
                var pendientes = pagosPendientes.Where(p =>
                    p.Estado == "por_pagar" || p.Estado == "vencido").ToList();

                foreach (var pago in pendientes)
                {
                    // ✅ CORRECCIÓN: El campo es FechaProximoPago, no FechaPago
                    var diasRestantes = (pago.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

                    if (diasRestantes <= 7)
                    {
                        var alertas = await _supabase.ObtenerAlertasAsync();
                        var alertaExistente = alertas.FirstOrDefault(a =>
                            a.TipoAlerta == "pago_plataforma" &&
                            a.EntidadId == pago.Id &&
                            a.Estado == "pendiente");

                        if (alertaExistente == null)
                        {
                            string nivel;
                            string mensaje;

                            if (diasRestantes < 0)
                            {
                                nivel = "critico";
                                var diasVencidos = Math.Abs(diasRestantes);
                                mensaje = diasVencidos == 1
                                    ? $"Pago a plataforma vencido hace 1 día - L {pago.MontoMensual:N2}"
                                    : $"Pago a plataforma vencido hace {diasVencidos} días - L {pago.MontoMensual:N2}";
                            }
                            else if (diasRestantes == 0)
                            {
                                nivel = "critico";
                                mensaje = $"Pago a plataforma vence HOY - L {pago.MontoMensual:N2}";
                            }
                            else if (diasRestantes <= 3)
                            {
                                nivel = "urgente";
                                mensaje = diasRestantes == 1
                                    ? $"Pago a plataforma vence mañana - L {pago.MontoMensual:N2}"
                                    : $"Pago a plataforma en {diasRestantes} días - L {pago.MontoMensual:N2}";
                            }
                            else
                            {
                                nivel = "advertencia";
                                mensaje = $"Pago a plataforma en {diasRestantes} días - L {pago.MontoMensual:N2}";
                            }

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
        /// Marca todas las alertas como leídas (para limpiar badge)
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
                throw new Exception($"Error al marcar todas como leídas: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Limpia alertas antiguas (resueltas o leídas hace más de 30 días)
        /// </summary>
        public async Task LimpiarAlertasAntiguasAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alertasAntiguas = alertas.Where(a =>
                    (a.Estado == "resuelta" || a.Estado == "leida") &&
                    a.FechaCreacion < DateTime.Now.AddDays(-30)).ToList();

                foreach (var alerta in alertasAntiguas)
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
        /// Limpia alertas resueltas (cuando las suscripciones fueron pagadas)
        /// </summary>
        public async Task LimpiarAlertasResueltasAsync()
        {
            try
            {
                var alertas = await _supabase.ObtenerAlertasAsync();
                var alertasPendientes = alertas.Where(a => a.Estado == "pendiente").ToList();

                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();

                foreach (var alerta in alertasPendientes)
                {
                    if (alerta.TipoAlerta == "cobro_cliente")
                    {
                        var suscripcion = suscripciones.FirstOrDefault(s => s.Id == alerta.EntidadId);
                        if (suscripcion != null)
                        {
                            var diasRestantes = (suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
                            if (diasRestantes > 0)
                            {
                                alerta.Estado = "resuelta";
                                await _supabase.ActualizarAlertaAsync(alerta);
                            }
                        }
                    }
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
            var alertas = await _supabase.ObtenerAlertasAsync();
            var alerta = alertas.FirstOrDefault(a => a.Id == alertaId);

            if (alerta != null)
            {
                alerta.Estado = "leida";
                await _supabase.ActualizarAlertaAsync(alerta);
            }
        }

        /// <summary>
        /// Obtiene el conteo de alertas pendientes (para el badge)
        /// </summary>
        public async Task<int> ObtenerConteoAlertasPendientesAsync()
        {
            var alertas = await _supabase.ObtenerAlertasAsync();
            return alertas.Count(a => a.Estado == "pendiente");
        }

        /// <summary>
        /// Obtiene alertas pendientes ordenadas por prioridad
        /// </summary>
        public async Task<List<Alerta>> ObtenerAlertasPendientesAsync()
        {
            var alertas = await _supabase.ObtenerAlertasAsync();
            return alertas
                .Where(a => a.Estado == "pendiente")
                .OrderBy(a => GetPrioridadNivel(a.Nivel))
                .ThenBy(a => a.DiasRestantes)
                .ThenByDescending(a => a.FechaCreacion)
                .ToList();
        }

        /// <summary>
        /// Obtiene alertas por tipo específico
        /// </summary>
        public async Task<List<Alerta>> ObtenerAlertasPorTipoAsync(string tipoAlerta)
        {
            var alertas = await _supabase.ObtenerAlertasAsync();
            return alertas
                .Where(a => a.TipoAlerta == tipoAlerta && a.Estado == "pendiente")
                .OrderBy(a => GetPrioridadNivel(a.Nivel))
                .ThenBy(a => a.DiasRestantes)
                .ToList();
        }

        private int GetPrioridadNivel(string nivel)
        {
            return nivel switch
            {
                "critico" => 1,
                "urgente" => 2,
                "advertencia" => 3,
                "normal" => 4,
                _ => 5
            };
        }
    }
}