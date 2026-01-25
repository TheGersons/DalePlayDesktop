using Microsoft.Extensions.Configuration;
using Supabase;
using StreamManager.Data.Models;
using SupabaseClient = Supabase.Client;
using System.Diagnostics;

namespace StreamManager.Services
{
    public class SupabaseService
    {
        private SupabaseClient? _client;
        private Usuario? _usuarioActual;

        public Usuario? UsuarioActual => _usuarioActual;
        public bool EstaAutenticado => _usuarioActual != null;

        private readonly string _url;
        private readonly string _key;

        public SupabaseService(IConfiguration configuration)
        {
            _url = configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase URL no configurada");
            _key = configuration["Supabase:Key"]
                ?? throw new InvalidOperationException("Supabase Key no configurada");

            Debug.WriteLine($"[SUPABASE] URL configurada: {_url}");
            Debug.WriteLine($"[SUPABASE] Key configurada: {_key.Substring(0, 20)}...");
        }

        public async Task InicializarAsync()
        {
            try
            {
                Debug.WriteLine("[SUPABASE] Inicializando cliente...");

                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true
                };

                _client = new SupabaseClient(_url, _key, options);
                await _client.InitializeAsync();

                Debug.WriteLine("[SUPABASE] Cliente inicializado exitosamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SUPABASE ERROR] Error al inicializar: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el cliente de Supabase para operaciones avanzadas.
        /// Consolidado en un único método público.
        /// </summary>
        public SupabaseClient GetClient()
        {
            if (_client == null)
                throw new InvalidOperationException("Cliente Supabase no inicializado. Llama a InicializarAsync() primero.");
            return _client;
        }

        // ================== AUTENTICACIÓN ==================

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                Debug.WriteLine($"[LOGIN] Iniciando login para: {email}");

                var client = GetClient();
                var table = client.From<Usuario>();

                Debug.WriteLine("[LOGIN] Consultando base de datos...");
                var response = await table.Where(x => x.Email == email).Get();

                var usuario = response.Models.FirstOrDefault();

                if (usuario == null)
                {
                    Debug.WriteLine("[LOGIN] Usuario no encontrado en la base de datos");
                    return false;
                }

                Debug.WriteLine("[LOGIN] Verificando contraseña...");
                bool passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

                if (!passwordValido)
                {
                    Debug.WriteLine("[LOGIN] Contraseña incorrecta");
                    return false;
                }

                if (usuario.Estado != "activo")
                {
                    Debug.WriteLine($"[LOGIN] Usuario inactivo. Estado: {usuario.Estado}");
                    return false;
                }

                _usuarioActual = usuario;

                usuario.FechaUltimoAcceso = DateTime.Now;
                await table.Update(usuario);

                Debug.WriteLine("[LOGIN] Login exitoso!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN ERROR] Excepción: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            _usuarioActual = null;
            Debug.WriteLine("[LOGOUT] Sesión cerrada");
        }

        // ================== PLATAFORMAS ==================

        public async Task<List<Plataforma>> ObtenerPlataformasAsync()
        {
            var client = GetClient();
            var response = await client.From<Plataforma>().Get();
            return response.Models;
        }

        public async Task<Plataforma> CrearPlataformaAsync(Plataforma plataforma)
        {
            var client = GetClient();
            var response = await client.From<Plataforma>().Insert(plataforma);
            return response.Models.First();
        }

        public async Task<Plataforma> ActualizarPlataformaAsync(Plataforma plataforma)
        {
            var client = GetClient();
            var response = await client.From<Plataforma>().Update(plataforma);
            return response.Models.First();
        }

        public async Task EliminarPlataformaAsync(Guid id)
        {
            var client = GetClient();
            await client.From<Plataforma>().Where(x => x.Id == id).Delete();
        }

        // ================== CUENTAS DE CORREO ==================

        public async Task<List<CuentaCorreo>> ObtenerCuentasAsync()
        {
            var client = GetClient();
            var response = await client
                .From<CuentaCorreo>()
                .Where(c => c.DeletedAt == null)
                .Get();
            return response.Models;
        }

        public async Task<List<CuentaCorreo>> ObtenerCuentasPorPlataformaAsync(Guid plataformaId)
        {
            var client = GetClient();
            var response = await client.From<CuentaCorreo>()
                .Where(x => x.PlataformaId == plataformaId).Get();
            return response.Models;
        }

        public async Task<CuentaCorreo> CrearCuentaAsync(CuentaCorreo cuenta)
        {
            var client = GetClient();
            var response = await client.From<CuentaCorreo>().Insert(cuenta);
            return response.Models.First();
        }

        public async Task<CuentaCorreo> ActualizarCuentaAsync(CuentaCorreo cuenta)
        {
            var client = GetClient();
            var response = await client.From<CuentaCorreo>().Update(cuenta);
            return response.Models.First();
        }

        public async Task EliminarCuentaAsync(Guid id)
        {
            var client = GetClient();
            await client.From<CuentaCorreo>().Where(x => x.Id == id).Delete();
        }

        // ================== PERFILES ==================

        public async Task<List<Perfil>> ObtenerPerfilesAsync()
        {
            var client = GetClient();
            var response = await client
                .From<Perfil>()
                .Where(p => p.DeletedAt == null)
                .Get();
            return response.Models;
        }

        public async Task<List<Perfil>> ObtenerPerfilesPorCuentaAsync(Guid cuentaId)
        {
            var client = GetClient();
            var response = await client.From<Perfil>()
                .Where(x => x.CuentaId == cuentaId).Get();
            return response.Models;
        }

        public async Task<List<Perfil>> ObtenerPerfilesDisponiblesAsync(Guid plataformaId)
        {
            var cuentas = await ObtenerCuentasPorPlataformaAsync(plataformaId);
            var cuentaIds = cuentas.Select(c => c.Id).ToList();

            var client = GetClient();
            var response = await client.From<Perfil>()
                .Where(x => x.Estado == "disponible").Get();

            return response.Models.Where(p => cuentaIds.Contains(p.CuentaId)).ToList();
        }

        public async Task<Perfil> CrearPerfilAsync(Perfil perfil)
        {
            var client = GetClient();
            var response = await client.From<Perfil>().Insert(perfil);
            return response.Models.First();
        }

        public async Task<Perfil> ActualizarPerfilAsync(Perfil perfil)
        {
            var client = GetClient();
            var response = await client.From<Perfil>().Update(perfil);
            return response.Models.First();
        }

        public async Task EliminarPerfilAsync(Guid id)
        {
            var client = GetClient();
            await client.From<Perfil>().Where(x => x.Id == id).Delete();
        }

        // ================== CLIENTES ==================

        public async Task<List<Cliente>> ObtenerClientesAsync()
        {
            var client = GetClient();
            var response = await client
                .From<Cliente>()
                .Where(c => c.DeletedAt == null)
                .Get();
            return response.Models;
        }

        public async Task<Cliente> CrearClienteAsync(Cliente cliente)
        {
            var client = GetClient();
            var response = await client.From<Cliente>().Insert(cliente);
            return response.Models.First();
        }

        public async Task<Cliente> ActualizarClienteAsync(Cliente cliente)
        {
            var client = GetClient();
            var response = await client.From<Cliente>().Update(cliente);
            return response.Models.First();
        }

        public async Task EliminarClienteAsync(Guid clienteId)
        {
            var client = GetClient();
            var cliente = await client
                .From<Cliente>()
                .Where(c => c.Id == clienteId)
                .Single();
            if (cliente is null) return;
            cliente.DeletedAt = DateTime.UtcNow;

            await client.From<Cliente>().Update(cliente);
        }

        public async Task RestaurarClienteAsync(Guid clienteId)
        {
            var client = GetClient();
            var cliente = await client
                .From<Cliente>()
                .Where(c => c.Id == clienteId)
                .Single();

            if (cliente is null) return;
            cliente.DeletedAt = null;

            await client.From<Cliente>().Update(cliente);
        }

        public async Task<List<Cliente>> ObtenerClientesEliminadosAsync()
        {
            var client = GetClient();
            var response = await client
                .From<Cliente>()
                .Where(c => c.DeletedAt != null)
                .Get();

            return response.Models;
        }

        // ================== SUSCRIPCIONES ==================

        public async Task<List<Suscripcion>> ObtenerSuscripcionesAsync()
        {
            var client = GetClient();
            var response = await client
                .From<Suscripcion>()
                .Where(s => s.DeletedAt == null)
                .Get();
            return response.Models;
        }

        public async Task<List<Suscripcion>> ObtenerSuscripcionesPorClienteAsync(Guid clienteId)
        {
            var client = GetClient();
            var response = await client.From<Suscripcion>()
                .Where(x => x.ClienteId == clienteId).Get();
            return response.Models;
        }

        public async Task<Suscripcion> CrearSuscripcionAsync(Suscripcion suscripcion)
        {
            var client = GetClient();
            var response = await client.From<Suscripcion>().Insert(suscripcion);
            return response.Models.First();
        }

        public async Task<Suscripcion> ActualizarSuscripcionAsync(Suscripcion suscripcion)
        {
            var client = GetClient();
            var response = await client.From<Suscripcion>().Update(suscripcion);
            return response.Models.First();
        }

        public async Task EliminarSuscripcionAsync(Guid id)
        {
            var client = GetClient();
            await client.From<Suscripcion>().Where(x => x.Id == id).Delete();
        }

        // ================== PAGOS ==================

        public async Task<List<Pago>> ObtenerPagosAsync()
        {
            var client = GetClient();
            var response = await client.From<Pago>().Get();
            return response.Models;
        }

        public async Task<List<Pago>> ObtenerPagosPorSuscripcionAsync(Guid suscripcionId)
        {
            var client = GetClient();
            var response = await client.From<Pago>()
                .Where(x => x.SuscripcionId == suscripcionId).Get();
            return response.Models;
        }

        public async Task<Pago> CrearPagoAsync(Pago pago)
        {
            var client = GetClient();
            var response = await client.From<Pago>().Insert(pago);
            return response.Models.First();
        }

        // ================== PAGOS PLATAFORMA ==================

        public async Task<List<PagoPlataforma>> ObtenerPagosPlataformaAsync()
        {
            var client = GetClient();
            var response = await client.From<PagoPlataforma>().Get();
            return response.Models;
        }

        public async Task<PagoPlataforma> CrearPagoPlataformaAsync(PagoPlataforma pago)
        {
            var client = GetClient();
            var response = await client.From<PagoPlataforma>().Insert(pago);
            return response.Models.First();
        }

        public async Task<PagoPlataforma> ActualizarPagoPlataformaAsync(PagoPlataforma pago)
        {
            var client = GetClient();
            var response = await client.From<PagoPlataforma>().Update(pago);
            return response.Models.First();
        }

        public async Task EliminarPagoPlataformaAsync(Guid id)
        {
            var client = GetClient();
            await client.From<PagoPlataforma>().Where(x => x.Id == id).Delete();
        }

        // ================== HISTORIAL PAGOS PLATAFORMA ==================

        public async Task<List<HistorialPagoPlataforma>> ObtenerHistorialPagosPlataformaAsync(Guid pagoPlataformaId)
        {
            var client = GetClient();
            var response = await client.From<HistorialPagoPlataforma>()
                .Where(x => x.PagoPlataformaId == pagoPlataformaId).Get();
            return response.Models;
        }

        public async Task<HistorialPagoPlataforma> CrearHistorialPagoPlataformaAsync(HistorialPagoPlataforma historial)
        {
            var client = GetClient();
            var response = await client.From<HistorialPagoPlataforma>().Insert(historial);
            return response.Models.First();
        }

        // ================== ALERTAS ==================

        public async Task<List<Alerta>> ObtenerAlertasAsync()
        {
            var client = GetClient();
            var response = await client.From<Alerta>().Get();
            return response.Models;
        }

        public async Task<List<Alerta>> ObtenerAlertasPendientesAsync()
        {
            var client = GetClient();
            var response = await client.From<Alerta>()
                .Where(x => x.Estado == "pendiente").Get();
            return response.Models;
        }

        public async Task<Alerta> CrearAlertaAsync(Alerta alerta)
        {
            var client = GetClient();
            var response = await client.From<Alerta>().Insert(alerta);
            return response.Models.First();
        }

        public async Task<Alerta> ActualizarAlertaAsync(Alerta alerta)
        {
            var client = GetClient();
            var response = await client.From<Alerta>().Update(alerta);
            return response.Models.First();
        }

        public async Task EliminarAlertaAsync(Guid id)
        {
            var client = GetClient();
            await client.From<Alerta>().Where(x => x.Id == id).Delete();
        }

        // ================== CONFIGURACIÓN ==================

        public async Task<List<Configuracion>> ObtenerConfiguracionAsync()
        {
            var client = GetClient();
            var response = await client.From<Configuracion>().Get();
            return response.Models;
        }

        public async Task<string?> ObtenerValorConfiguracionAsync(string clave)
        {
            var client = GetClient();
            var response = await client.From<Configuracion>()
                .Where(x => x.Clave == clave).Get();
            return response.Models.FirstOrDefault()?.Valor;
        }

        public async Task ActualizarConfiguracionAsync(string clave, string valor)
        {
            var client = GetClient();
            var response = await client.From<Configuracion>()
                .Where(x => x.Clave == clave).Get();

            var config = response.Models.FirstOrDefault();
            if (config != null)
            {
                config.Valor = valor;
                config.FechaModificacion = DateTime.Now;
                await client.From<Configuracion>().Update(config);
            }
        }

        // Usuarios
        public async Task<List<AuthUser>> ObtenerUsuariosAsync()
        {
            var response = await _client
                .From<AuthUser>()
                .Get();

            return response.Models;
        }

        public async Task ActualizarUsuarioAsync(AuthUser usuario)
        {
            await _client
                .From<AuthUser>()
                .Where(u => u.Id == usuario.Id)
                .Update(usuario);
        }

        public async Task CrearUsuarioAsync(AuthUser usuario)
        {
            await _client
                .From<AuthUser>()
                .Insert(usuario);
        }

        public async Task EliminarUsuarioAsync(Guid id)
        {
            await _client
                .From<AuthUser>()
                .Where(u => u.Id == id)
                .Delete();
        }
    }
}