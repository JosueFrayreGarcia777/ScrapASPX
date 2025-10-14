using Microsoft.Identity.Client;

namespace ScrapSystem.Models
{
    public class PiezaRechazada
    {
        public int RechazoID { get; set; }
        public int? FolioID { get; set; }
        public string RejectCode { get; set; } = string.Empty;
        public string TRWNumber { get; set; } = string.Empty;
        public string Line { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public string Defect { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string UsuarioID { get; set; } = string.Empty;

    }
}
