namespace ScrapSystem.Models
{
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public string CodigoBarras { get; set;  } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set;  }
    }
}
