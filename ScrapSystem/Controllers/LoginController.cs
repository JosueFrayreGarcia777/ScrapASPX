using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ScrapSystem.Models;
using Microsoft.Data.SqlClient;


namespace ScrapSystem.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Si ya está logueado, redirigir al Home
            if (HttpContext.Session.GetString("UsuarioNombre") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
        [HttpPost]
        public IActionResult Login(string codigoBarras)
        {
            if (string.IsNullOrEmpty(codigoBarras))
            {
                ViewBag.ErrorMessage = "Por favor escanee su gafete";
                return View("Index");
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"SELECT UsuarioID,CodigoBarras,Nombre,Rol
                                  FROM Usuarios
                                  WHERE CodigoBarras = @CodigoBarras AND Activo = 1";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CodigoBarras", codigoBarras);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                HttpContext.Session.SetInt32("UsuarioID", (int)reader["UsuarioID"]);
                                HttpContext.Session.SetString("UsuarioNombre", reader["Nombre"].ToString() ?? "");
                                HttpContext.Session.SetString("UsuarioRol", reader["Rol"].ToString() ?? "");
                                HttpContext.Session.SetString("CodigoBarras", reader["CodigoBarras"].ToString() ?? "");

                                UpdateUltimoAcceso((int)reader["UsuarioID"]);

                                return RedirectToAction("Index", "Home");

                            }
                            else
                            {
                                ViewBag.ErrorMessage = "Codigo de barras no valio o usuario inactivo";
                                return View("Index");
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                ViewBag.ErrorMessage = $"Error al iniciar sesion : {ex.Message}";
                return View("Index");
            }
        }

        private void UpdateUltimoAcceso(int usuarioID)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var updateQuery = "UPDATE Usuarios SET UltimoAcceso = GETDATE() WHERE UsuarioID = @UsuarioID";
                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UsuarioID", usuarioID);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            { 
            
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

    }
}
