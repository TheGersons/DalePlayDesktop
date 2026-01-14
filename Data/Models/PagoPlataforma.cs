using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("pagos_plataforma")]
    public class PagoPlataforma : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("cuenta_id")]
        public Guid CuentaId { get; set; }

        [Column("plataforma_id")]
        public Guid PlataformaId { get; set; }

        [Column("monto_mensual")]
        public decimal MontoMensual { get; set; }

        [Column("dia_pago_mes")]
        public int DiaPagoMes { get; set; }

        [Column("fecha_proximo_pago")]
        public DateOnly FechaProximoPago { get; set; }

        [Column("fecha_limite_pago")]
        public DateOnly FechaLimitePago { get; set; }

        [Column("dias_gracia")]
        public int DiasGracia { get; set; } = 5;

        [Column("estado")]
        public string Estado { get; set; } = "pendiente";

        [Column("metodo_pago_preferido")]
        public string MetodoPagoPreferido { get; set; } = "transferencia";

        [Column("notas")]
        public string? Notas { get; set; }

        [Column("fecha_ultimo_pago")]
        public DateOnly? FechaUltimoPago { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }
    }
}