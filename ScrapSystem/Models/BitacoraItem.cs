namespace ScrapSystem.Models
{
    public class BitacoraItem
    {
        public int FolioID { get; set; }
        public string NumeroFolio { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public int TotalPiezasRechazadas { get; set; }
    }
}