namespace ScrapSystem.Models
{
    public class ComponenteSuelto
    {
        public int ComponenteSueltoID { get; set; }
        public int FolioID { get; set; }
        public int MaterialID { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Usuario { get; set; } = string.Empty;

        // Propiedades de navegación
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal Precio { get; set; }
    }
}
