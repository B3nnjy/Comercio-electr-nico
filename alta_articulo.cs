using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace t7_2021630460_vs
{
    public class alta_articulo
    {
        class Articulo
        {
            public string? nombre;
            public string? descripcion;
            public float? precio;
            public int? cantidad;
            public string? foto;
        }
        class ParamAltaArticulo
        {
            public Articulo? articulo;
        }
       
        private readonly ILogger<alta_articulo> _logger;

        public alta_articulo(ILogger<alta_articulo> logger)
        {
            _logger = logger;
        }

        [Function("alta_articulo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                ParamAltaArticulo? data = JsonConvert.DeserializeObject<ParamAltaArticulo>(body);

                if (data == null || data.articulo == null) throw new Exception("Se esperan los datos del articulo");

                Articulo? articulo = data.articulo;

                if (articulo.precio == null) throw new Exception("Se debe ingresar el precio");
                if (articulo.cantidad == null) throw new Exception("Se debe ingresar la cantidad");

                string? Server = Environment.GetEnvironmentVariable("Server");
                string? User = Environment.GetEnvironmentVariable("User");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";Database=" + Database + ";Uid=" + User + ";Pwd=" + Password + ";sslmode=Preferred";

                var conexion = new MySqlConnection(cs);
                conexion.Open();
                MySqlTransaction tran = conexion.BeginTransaction();

                try
                {
                    var cmd_1 = new MySqlCommand();
                    cmd_1.Connection = conexion;
                    cmd_1.Transaction = tran;
                    cmd_1.CommandText = "INSERT INTO articulos(id_articulo, nombre, descripcion, precio, cantidad) VALUES (0, @nombre, @descripcion, @precio, @cantidad)";
                    cmd_1.Parameters.AddWithValue("@nombre", articulo.nombre);
                    cmd_1.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                    cmd_1.Parameters.AddWithValue("@precio", articulo.precio);
                    cmd_1.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                    cmd_1.ExecuteNonQuery();

                    if (articulo.foto != null)
                    {
                        var cmd_2 = new MySqlCommand();
                        cmd_2.Connection = conexion;
                        cmd_2.Transaction = tran;
                        cmd_2.CommandText = "INSERT INTO fotos_articulos (foto, id_articulo) VALUES (@foto, (SELECT id_articulo FROM articulos WHERE nombre=@nombre))";
                        cmd_2.Parameters.AddWithValue("@foto", Convert.FromBase64String(articulo.foto));
                        cmd_2.Parameters.AddWithValue("@nombre", articulo.nombre);
                        cmd_2.ExecuteNonQuery();
                    }

                    tran.Commit();
                    return new OkObjectResult("Articulo registrado!!");
                }
                catch (Exception e)
                {
                    tran.Rollback();
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
            }
            catch (Exception ex)
            { 
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(ex.Message)));
            }
        }
    }
}
