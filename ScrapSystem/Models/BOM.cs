namespace ScrapSystem.Models
{
    public class BOM
    {
        public int PiezaID { get; set; }
        public int MaterialID { get; set; }
        public string TRWNumber { get; set; } = string.Empty;
        public string DescripcionPieza { get; set; } = string.Empty;
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Per { get; set; }

        //Propiedad caluclada para precio unitario
        public decimal PrecioUnitario => Per > 0 ? Precio / Per : 0;
    }
}