using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ScrapSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ScrapSystem.Models;
using System.Diagnostics;
using System.Linq.Expressions;

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
        public IActionResult Index(string rejectCode,string action)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            rejectCode? result = null;
            string searchMessage = "";
            if (action == "SearchBOM")
            {
                return SearchBOM(rejectCode);
            }

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
                                    result = new rejectCode
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

        private IActionResult SearchBOM(string rejectCode)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            string trwNumber = "";
            List<BOM>? bomResults = null;
            string bomMessage = "";

            if (!string.IsNullOrEmpty(rejectCode))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // OBTENER TRW
                        var getTrwQuery = "SELECT trwNumber FROM lineRejects WHERE rejectID = @rejectCode";
                        using (var command = new SqlCommand(getTrwQuery, connection))
                        {
                            command.Parameters.AddWithValue("@rejectCode", rejectCode);
                            var trwResult = command.ExecuteScalar();
                                trwNumber = trwResult?.ToString() ?? "";
                        }

                        // SI SE ENCUENTRA TRW NUMBER, BUSCAMOS EN LA TABLA BOM
                        if (!string.IsNullOrEmpty(trwNumber))
                        {
                            // Consulta BOM corregida
                            var bomQuery = @"SELECT b.PiezaID, b.MaterialID, 
                                               p.TRWNumber, p.DescripcionPieza,
                                               m.materialNumber, m.materialDescription, 
                                               m.unidad, m.precio, m.per
                                        FROM BOM b
                                        INNER JOIN Piezas p ON b.PiezaID = p.PiezaID
                                        INNER JOIN Materiales m ON b.MaterialID = m.materialID
                                        WHERE p.TRWNumber = @trwNumber";
                            
                            using (var command = new SqlCommand(bomQuery, connection))
                            {
                                command.Parameters.AddWithValue("@trwNumber", trwNumber);

                                bomResults = new List<BOM>();
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read()) // Cambié de 'if' a 'while' para obtener todos los registros
                                    {
                                        bomResults.Add(new BOM
                                        {
                                            PiezaID = Convert.ToInt32(reader["PiezaID"]),
                                            MaterialID = Convert.ToInt32(reader["MaterialID"]),
                                            TRWNumber = reader["TRWNumber"].ToString() ?? "",
                                            DescripcionPieza = reader["DescripcionPieza"].ToString() ?? "",
                                            MaterialNumber = reader["materialNumber"].ToString() ?? "",
                                            MaterialDescription = reader["materialDescription"].ToString() ?? "",
                                            Unidad = reader["unidad"].ToString() ?? "",
                                            Precio = Convert.ToDecimal(reader["precio"]),
                                            Per = Convert.ToInt32(reader["per"])
                                        });
                                    }
                                }
                                
                                if (bomResults.Any())
                                {
                                    bomMessage = $"Se encontraron {bomResults.Count} materiales para la pieza {trwNumber}";
                                }
                                else
                                {
                                    bomMessage = $"No se encontró BOM para pieza {trwNumber}";
                                }
                            }
                        }
                        else
                        {
                            bomMessage = "No se pudo obtener el trwNumber del código de rechazo";
                        }
                    }
                }
                catch (Exception ex)
                {
                    bomMessage = $"Error al buscar BOM: {ex.Message}";
                }
            }
            else
            {
                bomMessage = "Por favor ingresa un código de rechazo válido";
            }

            ViewBag.BOMMessage = bomMessage;
            ViewBag.BOMResults = bomResults;
            ViewBag.TRWNumber = trwNumber;

            return View("Index");
        }

        [HttpPost]
        public IActionResult RegistrarRechazo(string rejectCode, string trwNumber, string Line, string turno, string defect, List<ComponenteSeleccionado> componenteSeleccionados)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            string mensaje = "";
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            //Insertar en piezas
                            var insertPiezaQuery = @"INSERT INTO PiezasRechazadas (RejectCode,TRWNumber,Line,Turno,Defect,FechaRegistro,Usuario)
                                                     OUTPUT INSERTED.RechazadoID VALUES (@RejectCode,@TRWNumber,@Line,@Turno,@Defect,GETDATE(),@Usuario)";
                            int rechazadoID;
                            using (var command = new SqlCommand(insertPiezaQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@RejectCode", rejectCode);
                                command.Parameters.AddWithValue("@TRWNumber", trwNumber);
                                command.Parameters.AddWithValue("@Line", Line ?? "");
                                command.Parameters.AddWithValue("@Turno", turno ?? "");
                                command.Parameters.AddWithValue("@Defect", defect ?? "");
                                command.Parameters.AddWithValue("@Usuario", "Usuario");

                                rechazadoID = (int)command.ExecuteScalar();
                            }
                            //insertar componentes seleccionados
                            var componentesRechazados = componenteSeleccionados
                                .Where(c => c.Seleccionado && c.Cantidad > 0)
                                .ToList();

                            if (componentesRechazados.Any())
                            {
                                var insertComponenteQuery = @"Insert INTO ComponentesRechazados (RechazoID,MaterialID,Cantidad) VALUES (@RechazadoID,@MaterialID,@Cantidad)";

                                foreach (var componente in componentesRechazados)
                                {
                                    using (var command = new SqlCommand(insertComponenteQuery, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@RechazadoID", rechazadoID);
                                        command.Parameters.AddWithValue("@MaterialID", componente.MaterialID);
                                        command.Parameters.AddWithValue("@Cantidad", componente.Cantidad);

                                        command.ExecuteNonQuery();
                                    }
                                }
                                mensaje = $"Rechazo registraod exitosamente. Pieza {trwNumber}, Componentes rechazados : {componentesRechazados.Count}";
                            }
                            else
                            {
                                mensaje = "Rechazo registrado como pierza terminada";
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al registrar el rechazo : {ex.Message}";
            }
            ViewBag.RegistroMesage = mensaje;
            return View("Index");
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
