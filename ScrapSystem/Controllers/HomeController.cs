using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ScrapSystem.Models;
using Microsoft.Data.SqlClient;
using System.Linq.Expressions;
using System.Xml.Schema;
using System.Data;

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
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
            { 
                return RedirectToAction("Index", "Login");
            }
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            var folioActivo = HttpContext.Session.GetString("NumeroFolio");
            ViewBag.FolioActivo = folioActivo;

            ViewBag.UsuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.UsuarioRol = HttpContext.Session.GetString("UsuarioRol");

            return View();
        }

        
        [HttpPost]
        public IActionResult Index(string rejectCode,string action)
        {
            //validar que hay un folio activo antes de buscar
            var folioActivo = HttpContext.Session.GetInt32("FolioActivo");
            if (!folioActivo.HasValue)
            { 
                ViewBag.SearchMessage = "Debe iniciar un registro antes de buscar rechazos.";
                ViewBag.FolioActivo = null;
                return View();
            }

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
            var folioActivoString = HttpContext.Session.GetString("NumeroFolio");
            ViewBag.FolioActivo = folioActivoString; // Fixed: Use the string folio number instead of integer
            ViewBag.SearchMessage = searchMessage;
            ViewBag.SearchResult = result;
            return View();

        }

        private IActionResult SearchBOM(string rejectCode)
        {

            //validar que hay folio activo
            var folioActivo = HttpContext.Session.GetInt32("FolioActivo");
            if (!folioActivo.HasValue)
            { 
                ViewBag.BOMMessage = "Debe iniciar un registro antes de buscar rechazos.";
                ViewBag.FolioActivo = null;
                return View("Index");
            }
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            string trwNumber = "";
            List<BOM>? bomResults = null;
            string bomMessage = "";
            rejectCode? searchResult = null;

            if (!string.IsNullOrEmpty(rejectCode))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // OBTENER TRW
                        var getTrwQuery = "SELECT rejectID,trwNumber,line,turno,defect FROM lineRejects WHERE rejectID = @rejectCode";
                        using (var command = new SqlCommand(getTrwQuery, connection))
                        {
                            command.Parameters.AddWithValue("@rejectCode", rejectCode);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    searchResult = new rejectCode
                                    {
                                        RejectID = reader["rejectID"].ToString() ?? "",
                                        trwNumber = reader["trwNumber"].ToString() ?? "",
                                        Line = reader["line"].ToString()??"",
                                        Turno = reader["turno"].ToString()??"",
                                        Defect = reader["defect"].ToString()??""
                                    };
                                    trwNumber = searchResult.trwNumber;
                                }
                            }
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

                                // FILTRAR COMPONENTES ELIMINADOS VISUALMENTE
                                var componentesEliminados = HttpContext.Session.GetString("ComponentesEliminados");
                                if (!string.IsNullOrEmpty(componentesEliminados))
                                {
                                    var listaEliminados = componentesEliminados.Split(',').ToList();
                                    bomResults = bomResults.Where(b => !listaEliminados.Contains($"{b.PiezaID}_{b.MaterialID}")).ToList();
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

            var folioActivoString = HttpContext.Session.GetString("NumeroFolio");
            ViewBag.FolioActivo = folioActivoString; // Fixed: Use the string folio number instead of integer
            ViewBag.BOMMessage = bomMessage;
            ViewBag.BOMResults = bomResults;
            ViewBag.TRWNumber = trwNumber;
            ViewBag.SearchResult = searchResult;
            return View("Index");
        }

        [HttpPost]
        public IActionResult RegistrarRechazo(string rejectCode, string trwNumber, string Line, string turno, string defect)
        {
            var folioID = HttpContext.Session.GetInt32("FolioActivo");
            if (!folioID.HasValue)
            {
                ViewBag.RegistroMessage = "Debe iniciar un registro antes de registrar rechazos.";
                return View("Index");
            }

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
                            // 1. Insertar la pieza rechazada
                            var insertPiezaQuery = @"INSERT INTO PiezasRechazadas (FolioID, RejectCode, TRWNumber, Line, Turno, Defect, FechaRegistro, Usuario)
                                             OUTPUT INSERTED.RechazadoID
                                             VALUES (@FolioID, @RejectCode, @TRWNumber, @Line, @Turno, @Defect, GETDATE(), @Usuario)";
                            int rechazadoID;
                            using (var command = new SqlCommand(insertPiezaQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FolioID", folioID.Value);
                                command.Parameters.AddWithValue("@RejectCode", rejectCode);
                                command.Parameters.AddWithValue("@TRWNumber", trwNumber);
                                command.Parameters.AddWithValue("@Line", Line ?? "");
                                command.Parameters.AddWithValue("@Turno", turno ?? "");
                                command.Parameters.AddWithValue("@Defect", defect ?? "");
                                command.Parameters.AddWithValue("@Usuario", HttpContext.Session.GetString("UsuarioNombre") ?? "Usuario");

                                rechazadoID = (int)command.ExecuteScalar();
                            }

                            // 2. Obtener los componentes DISPONIBLES del BOM (después de eliminar visualmente)
                            var componentesDisponibles = ObtenerComponentesDisponibles(trwNumber, connection, transaction);
                            
                            if (componentesDisponibles.Any())
                            {
                                // 3. Guardar los componentes DISPONIBLES
                                var insertComponenteQuery = @"INSERT INTO ComponentesRechazados (RechazadoID, MaterialID, Cantidad) 
                                                    VALUES (@RechazadoID, @MaterialID, @Cantidad)";

                                foreach (var componente in componentesDisponibles)
                                {
                                    using (var command = new SqlCommand(insertComponenteQuery, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@RechazadoID", rechazadoID);
                                        command.Parameters.AddWithValue("@MaterialID", componente.MaterialID);
                                        command.Parameters.AddWithValue("@Cantidad", 1); // Por defecto 1

                                        command.ExecuteNonQuery();
                                    }
                                }
                                
                                mensaje = $"Rechazo registrado exitosamente. Pieza: {trwNumber}, Componentes disponibles: {componentesDisponibles.Count}";
                            }
                            else
                            {
                                mensaje = "Rechazo registrado - todos los componentes fueron eliminados (pieza sin componentes disponibles)";
                            }

                            transaction.Commit();
                            
                            // 4. Limpiar componentes eliminados de la sesión después de guardar
                            HttpContext.Session.Remove("ComponentesEliminados");
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
                mensaje = $"Error al registrar el rechazo: {ex.Message}";
            }
            
            var folioActivoString = HttpContext.Session.GetString("NumeroFolio");
            ViewBag.FolioActivo = folioActivoString; // Fixed: Use the string folio number instead of integer
            ViewBag.RegistroMessage = mensaje;
            return View("Index");
        }

        // Método auxiliar para obtener componentes disponibles
        private List<BOM> ObtenerComponentesDisponibles(string trwNumber, SqlConnection connection, SqlTransaction transaction)
        {
            var componentesDisponibles = new List<BOM>();
            
            var bomQuery = @"SELECT b.PiezaID, b.MaterialID, 
                           p.TRWNumber, p.DescripcionPieza,
                           m.materialNumber, m.materialDescription, 
                           m.unidad, m.precio, m.per
                    FROM BOM b
                    INNER JOIN Piezas p ON b.PiezaID = p.PiezaID
                    INNER JOIN Materiales m ON b.MaterialID = m.materialID
                    WHERE p.TRWNumber = @trwNumber";
    
            using (var command = new SqlCommand(bomQuery, connection, transaction))
            {
                command.Parameters.AddWithValue("@trwNumber", trwNumber);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        componentesDisponibles.Add(new BOM
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
            }
    
            // Filtrar componentes eliminados visualmente
            var componentesEliminados = HttpContext.Session.GetString("ComponentesEliminados");
            if (!string.IsNullOrEmpty(componentesEliminados))
            {
                var listaEliminados = componentesEliminados.Split(',').ToList();
                componentesDisponibles = componentesDisponibles
                    .Where(c => !listaEliminados.Contains($"{c.PiezaID}_{c.MaterialID}"))
                    .ToList();
            }
    
            return componentesDisponibles;
        }

        [HttpPost]
        public IActionResult IniciarRegistro()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //generar folio
                    string numeroFolio = $"FOL{DateTime.Now:yyyyMMdd}{DateTime.Now:HHmmss}";
                    var query = @"Insert INTO Folios (NumeroFolio, FechaInicio, Estado,Usuario)
                                  OUTPUT INSERTED.FolioID
                                  VALUES(@NumeroFolio,GETDATE(),'ACTIVO',@Usuario)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NumeroFolio", numeroFolio);
                        command.Parameters.AddWithValue("@Usuario", "Usuario");

                        int folioID = (int)command.ExecuteScalar();

                        HttpContext.Session.SetInt32("FolioActivo", folioID);
                        HttpContext.Session.SetString("NumeroFolio", folioID.ToString());

                        ViewBag.RegistroMessage = $"Registro iniciado con folio :  {numeroFolio}";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.RegistroMessage = $"Error al iniciar el registro : {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult FinalizarRegistro()
        {
            var folioID = HttpContext.Session.GetInt32("FolioActivo");

            if (folioID.HasValue)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        var query = @"UPDATE Folios
                                      SET FechaFinalizacion = GETDATE(), Estado = 'FINALIZADO'
                                      WHERE FolioID = @FolioID";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FolioID", folioID.Value);
                            command.ExecuteNonQuery();
                        }

                        var numeroFolio = HttpContext.Session.GetString("NumeroFolio");
                        ViewBag.RegistroMessage = $"Registro finalizado para folio: {numeroFolio}";

                        // LIMPIAR TODA LA SESIÓN
                        HttpContext.Session.Remove("FolioActivo");
                        HttpContext.Session.Remove("NumeroFolio");
                        HttpContext.Session.Remove("ComponentesEliminados"); // Limpiar componentes eliminados
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.RegistroMessage = $"Error al finalizar registro: {ex.Message}";
                }
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EliminarComponenteBOM(int piezaID, int materialID, string rejectCode)
        {
            var componentesEliminados = HttpContext.Session.GetString("ComponentesEliminados");
            var listaEliminados = new List<string>();
            if (!string.IsNullOrEmpty(componentesEliminados))
            {
                listaEliminados = componentesEliminados.Split(',').ToList();
            }

            string componenteID = $"{piezaID}_{materialID}";
            if (!listaEliminados.Contains(componenteID))
            {
                listaEliminados.Add(componenteID);
            }

            HttpContext.Session.SetString("ComponentesEliminados", string.Join(",", listaEliminados));
            TempData["Mensaje Eliminacion"] = "Componente eliminado del BOM";
            return SearchBOM(rejectCode);
        }

        [HttpPost]
        public IActionResult LimpiarComponentesEliminados(string rejectCode)
        {
            HttpContext.Session.Remove("ComponentesEliminados");
            TempData["MensajeEliminacion"] = "Se han restaurado todos los componentes del BOM";
            return SearchBOM(rejectCode);
        }

        [HttpPost]
        public IActionResult BuscarMaterial([FromBody] MaterialSearchRequest request)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = "SELECT materialID, materialNumber, materialDescription, precio FROM Materiales WHERE materialNumber = @materialNumber";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@materialNumber", request.MaterialNumber);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var material = new
                                {
                                    materialID = Convert.ToInt32(reader["materialID"]),
                                    materialNumber = reader["materialNumber"].ToString(),
                                    materialDescription = reader["materialDescription"].ToString(),
                                    precio = Convert.ToDecimal(reader["precio"])
                                };
                                
                                return Json(new { success = true, material = material });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Material no encontrado" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al buscar material: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult AgregarComponenteSuelto([FromBody] ComponenteSueltoRequest request)
        {
            var folioID = HttpContext.Session.GetInt32("FolioActivo");
            if (!folioID.HasValue)
            {
                return Json(new { success = false, message = "No hay folio activo" });
            }
            
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insertar en la nueva tabla ComponentesSueltos
                            var insertQuery = @"INSERT INTO ComponentesSueltos (FolioID, MaterialID, Cantidad, Usuario) 
                                      VALUES (@FolioID, @MaterialID, @Cantidad, @Usuario)";
            
                            using (var command = new SqlCommand(insertQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FolioID", folioID.Value);
                                command.Parameters.AddWithValue("@MaterialID", request.MaterialID);
                                command.Parameters.AddWithValue("@Cantidad", request.Cantidad);
                                command.Parameters.AddWithValue("@Usuario", HttpContext.Session.GetString("UsuarioNombre") ?? "Usuario");
                
                                command.ExecuteNonQuery();
                            }
            
                            transaction.Commit();
                            return Json(new { success = true, message = "Componente suelto agregado exitosamente" });
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
                return Json(new { success = false, message = $"Error al agregar componente: {ex.Message}" });
            }
        }

        public IActionResult Boleta()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Boleta(string folioID)
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            BoletaDetalle? boletaDetalle = null;
            string mensaje = "";

            if (!string.IsNullOrEmpty(folioID))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        
                        // Buscar información del folio
                        var folioQuery = @"SELECT FolioID, NumeroFolio, FechaInicio, FechaFinalizacion, Estado, Usuario 
                                 FROM Folios WHERE FolioID = @FolioID";
                
                        using (var command = new SqlCommand(folioQuery, connection))
                        {
                            command.Parameters.AddWithValue("@FolioID", folioID);
                    
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    boletaDetalle = new BoletaDetalle
                                    {
                                        FolioID = Convert.ToInt32(reader["FolioID"]),
                                        NumeroFolio = reader["NumeroFolio"].ToString() ?? "",
                                        FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                                        FechaFinalizacion = reader["FechaFinalizacion"] as DateTime?,
                                        Estado = reader["Estado"].ToString() ?? "",
                                        Usuario = reader["Usuario"].ToString() ?? "",
                                        PiezasRechazadas = new List<PiezaRechazadaDetalle>(),
                                        ComponentesSueltos = new List<ComponenteSueltoDetalle>()
                                    };
                                    mensaje = "Folio encontrado correctamente";
                                }
                                else
                                {
                                    mensaje = "No se encontró el folio especificado";
                                }
                            }
                        }

                        // Si se encontró el folio, buscar las piezas rechazadas
                        if (boletaDetalle != null)
                        {
                            // Buscar piezas rechazadas con sus componentes
                            var piezasQuery = @"
                        SELECT pr.RechazadoID, pr.RejectCode, pr.TRWNumber, pr.Line, pr.Turno, pr.Defect, pr.FechaRegistro,
                               cr.MaterialID, m.materialNumber, m.materialDescription, m.precio, cr.Cantidad
                        FROM PiezasRechazadas pr
                        LEFT JOIN ComponentesRechazados cr ON pr.RechazadoID = cr.RechazadoID
                        LEFT JOIN Materiales m ON cr.MaterialID = m.materialID
                        WHERE pr.FolioID = @FolioID
                        ORDER BY pr.RechazadoID";

                            using (var command = new SqlCommand(piezasQuery, connection))
                            {
                                command.Parameters.AddWithValue("@FolioID", folioID);
                        
                                using (var reader = command.ExecuteReader())
                                {
                                    var piezasDict = new Dictionary<int, PiezaRechazadaDetalle>();
                            
                                    while (reader.Read())
                                    {
                                        int rechazadoID = Convert.ToInt32(reader["RechazadoID"]);
                                
                                        if (!piezasDict.ContainsKey(rechazadoID))
                                        {
                                            piezasDict[rechazadoID] = new PiezaRechazadaDetalle
                                            {
                                                RechazadoID = rechazadoID,
                                                RejectCode = reader["RejectCode"].ToString() ?? "",
                                                TRWNumber = reader["TRWNumber"].ToString() ?? "",
                                                Line = reader["Line"].ToString() ?? "",
                                                Turno = reader["Turno"].ToString() ?? "",
                                                Defect = reader["Defect"].ToString() ?? "",
                                                FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                                                Componentes = new List<ComponenteRechazadoDetalle>()
                                            };
                                        }

                                        // Agregar componente si existe
                                        if (!reader.IsDBNull("MaterialID"))
                                        {
                                            piezasDict[rechazadoID].Componentes.Add(new ComponenteRechazadoDetalle
                                            {
                                                MaterialID = Convert.ToInt32(reader["MaterialID"]),
                                                MaterialNumber = reader["materialNumber"].ToString() ?? "",
                                                MaterialDescription = reader["materialDescription"].ToString() ?? "",
                                                Precio = Convert.ToDecimal(reader["precio"]),
                                                Cantidad = Convert.ToInt32(reader["Cantidad"])
                                            });
                                        }
                                    }
                            
                                    boletaDetalle.PiezasRechazadas = piezasDict.Values.ToList();
                                }
                            }

                            // Verificar qué piezas están completas
                            foreach (var pieza in boletaDetalle.PiezasRechazadas)
                            {
                                pieza.EsCompleta = EsPiezaCompleta(pieza.TRWNumber, pieza.Componentes, connection);
                            }

                            // Buscar componentes sueltos
                            var componentesSueltosQuery = @"
                        SELECT cs.MaterialID, m.materialNumber, m.materialDescription, m.precio, cs.Cantidad, cs.FechaRegistro
                        FROM ComponentesSueltos cs
                        INNER JOIN Materiales m ON cs.MaterialID = m.materialID
                        WHERE cs.FolioID = @FolioID";

                            using (var command = new SqlCommand(componentesSueltosQuery, connection))
                            {
                                command.Parameters.AddWithValue("@FolioID", folioID);
                        
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        boletaDetalle.ComponentesSueltos.Add(new ComponenteSueltoDetalle
                                        {
                                            MaterialID = Convert.ToInt32(reader["MaterialID"]),
                                            MaterialNumber = reader["materialNumber"].ToString() ?? "",
                                            MaterialDescription = reader["materialDescription"].ToString() ?? "",
                                            Precio = Convert.ToDecimal(reader["precio"]),
                                            Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                            FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"])
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    mensaje = $"Error al buscar el folio: {ex.Message}";
                }
            }
            else
            {
                mensaje = "Por favor ingrese un número de folio";
            }

            ViewBag.Mensaje = mensaje;
            ViewBag.FolioID = folioID;
            return View(boletaDetalle);
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

        // Add this method to the HomeController class
        private bool EsPiezaCompleta(string trwNumber, List<ComponenteRechazadoDetalle> componentesRegistrados, SqlConnection connection)
        {
            // Obtener todos los componentes del BOM original para esta pieza
            var bomQuery = @"SELECT b.MaterialID
                            FROM BOM b
                            INNER JOIN Piezas p ON b.PiezaID = p.PiezaID
                            WHERE p.TRWNumber = @trwNumber";

            var componentesOriginales = new List<int>();
            
            using (var command = new SqlCommand(bomQuery, connection))
            {
                command.Parameters.AddWithValue("@trwNumber", trwNumber);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        componentesOriginales.Add(Convert.ToInt32(reader["MaterialID"]));
                    }
                }
            }

            // Si no hay BOM original, no se puede considerar completa
            if (!componentesOriginales.Any())
                return false;

            // Obtener los MaterialIDs de los componentes registrados
            var componentesRegistradosIds = componentesRegistrados.Select(c => c.MaterialID).ToList();
            
            // Verificar si todos los componentes originales están presentes
            return componentesOriginales.All(materialId => componentesRegistradosIds.Contains(materialId));
        }
    }
}
