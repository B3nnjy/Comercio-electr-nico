using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;


namespace t7_2021630460_vs
{
    public class agregar_carrito
    {
        public class Carrito
        {
            public int? id_articulo;
            public int? cantidad;
        }

        public class ParamCompraArticulo
        {
            public Carrito? carrito;
        }

        public int Cantidad_articulo(MySqlConnection conexion, int id_articulo)
        {
            try
            {
                var cmd = new MySqlCommand("SELECT cantidad FROM articulos WHERE id_articulo=@id_articulo");
                cmd.Connection = conexion;
                cmd.Parameters.AddWithValue("@id_articulo", id_articulo);
                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
                else
                {
                    Console.WriteLine("No se encontro el articulo");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener la cantidad de articulos: " + ex.Message);
                return -1;
            }
            finally
            {
                conexion.Close();
            }
        }

        [Function("agregar_carrito")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamCompraArticulo? data = JsonConvert.DeserializeObject<ParamCompraArticulo>(body);

                if (data == null || data.carrito == null) throw new Exception("Se esperan los datos del carrito");

                Carrito? carrito = data.carrito;

                string? Server = Environment.GetEnvironmentVariable("Server");
                string? User = Environment.GetEnvironmentVariable("User");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";Database=" + Database + ";Uid=" + User + ";Pwd=" + Password + ";sslmode=Preferred";

                var conexion = new MySqlConnection(cs);
                conexion.Open();
                MySqlTransaction tran = conexion.BeginTransaction();
                
                int cantidad = Cantidad_articulo(conexion, carrito.id_articulo ?? 0);

                if (cantidad == -1) return new BadRequestObjectResult("Error al obtener la cantidad de articulos");
                conexion.Open();
                try
                {

                    if (cantidad < carrito.cantidad) throw new Exception("No hay suficientes articulos en existencia");

                    var cmd_1 = new MySqlCommand();
                    cmd_1.Connection = conexion;
                    cmd_1.Transaction = tran;
                    cmd_1.CommandText = "INSERT INTO carrito_compra(id_articulo, cantidad) VALUES (@id_articulo, @cantidad) ON DUPLICATE KEY UPDATE cantidad = VALUES(cantidad) + cantidad";
                    cmd_1.Parameters.AddWithValue("@id_articulo", carrito.id_articulo);
                    cmd_1.Parameters.AddWithValue("@cantidad", carrito.cantidad);
                    cmd_1.ExecuteNonQuery();

                    var cmd_2 = new MySqlCommand();
                    cmd_2.Connection = conexion;
                    cmd_2.Transaction = tran;
                    cmd_2.CommandText = "UPDATE articulos SET cantidad = cantidad - @cantidad WHERE id_articulo = @id_articulo";
                    cmd_2.Parameters.AddWithValue("@cantidad", carrito.cantidad);
                    cmd_2.Parameters.AddWithValue("@id_articulo", carrito.id_articulo);
                    cmd_2.ExecuteNonQuery();
                    tran.Commit();


                    return new OkObjectResult("Articulo en el carrito!!");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Error al comprar el articulo: " + ex.Message)));
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
