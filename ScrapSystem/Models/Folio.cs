namespace ScrapSystem.Models
{
    public class Folio
    {
        public int FolioID { get; set; }
        public string NumeroFolio { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } 
        public DateTime? FechaFinaliacion { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public string Usuario { get; set; } = string.Empty;
        public int TotalPiezas { get; set; } 
        public int TotalComponentes { get; set; }
    }
}
