using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ScrapSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ScrapSystem.Models;
using System.Diagnostics;

namespace ScrapSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            return View();
        }


        [HttpPost]
        public IActionResult Index(string rejectCode)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            rejecCode? result = null;
            string searchMessage = "";

            if (!string.IsNullOrEmpty(rejectCode))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var query = "SELECT rejectID,trwNumber,line,turno,defect FROM lineRejects WHERE rejectID = @rejectCode";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@rejectCode", rejectCode);

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    result = new rejecCode
                                    {
                                        RejectID = reader["rejectID"].ToString() ?? "",
                                        trwNumber = reader["trwNumber"].ToString() ?? "",
                                        Line = reader["line"].ToString() ?? "",
                                        Turno = reader["turno"].ToString() ?? "",
                                        Defect = reader["defect"].ToString() ?? ""
                                    };
                                    searchMessage = "Registro encontrado correctamente.";
                                }
                                else
                                {
                                    searchMessage = "No se encontro ningun registro con ese codigo";
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    searchMessage = $"Erro al buscar {ex.Message}";
                }


            }
            else
            {
                searchMessage = "Por favor ingrese un codigo de rechazo";
            }
            ViewBag.SearchMessage = searchMessage;
            ViewBag.SearchResult = result;
            return View();

        }

        public IActionResult Privacy()
        { 
            return View();
        }

        [ResponseCache(Duration = 0,Location =ResponseCacheLocation.None,NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }  
}
