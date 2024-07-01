using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace t7_2021630460_vs
{
    public class borrar_carrito
    {
        public class ParamQuitarArticulo
        {
            public int? id_articulo;
            public int? opcion;
        }

        public bool borrar_articulo(MySqlConnection conexion, int id_articulo)
        {
            conexion.Open();
            var transaction = conexion.BeginTransaction();
            try
            {
                var cmd2 = new MySqlCommand();
                cmd2.Connection = conexion;
                cmd2.Transaction = transaction;
                cmd2.CommandText = "UPDATE articulos a JOIN carrito_compra c ON a.id_articulo = c.id_articulo SET a.cantidad = a.cantidad + c.cantidad WHERE a.id_articulo = @id_articulo";
                cmd2.Parameters.AddWithValue("@id_articulo", id_articulo);
                cmd2.ExecuteNonQuery();

                var cmd = new MySqlCommand();
                cmd.Connection = conexion;
                cmd.Transaction = transaction;
                cmd.CommandText = "DELETE FROM carrito_compra WHERE id_articulo=@id_articulo";
                cmd.Parameters.AddWithValue("@id_articulo", id_articulo);
                cmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
            finally
            {
                conexion.Close();
            }
        }

        public bool borrar_todo(MySqlConnection conexion)
        {
            conexion.Open();
            var transaction = conexion.BeginTransaction();
            try
            {
                var cmd2 = new MySqlCommand();
                cmd2.Connection = conexion;
                cmd2.Transaction = transaction;
                cmd2.CommandText = "UPDATE articulos a JOIN carrito_compra c ON a.id_articulo = c.id_articulo SET a.cantidad = a.cantidad + c.cantidad";
                cmd2.ExecuteNonQuery();

                var cmd = new MySqlCommand();
                cmd.Connection = conexion;
                cmd.Transaction = transaction;
                cmd.CommandText = "DELETE FROM carrito_compra";
                cmd.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
            finally
            {
                conexion.Close();
            }
        }

        [Function("borrar_carrito")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamQuitarArticulo? data = JsonConvert.DeserializeObject<ParamQuitarArticulo>(body);

                string? Server = Environment.GetEnvironmentVariable("Server");
                string? User = Environment.GetEnvironmentVariable("User");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";Database=" + Database + ";Uid=" + User + ";Pwd=" + Password + ";sslmode=Preferred";

                var conexion = new MySqlConnection(cs);
                try
                {
                    if (data.opcion == 0)
                    {
                        if (borrar_todo(conexion))
                        {
                            return new OkObjectResult("Carrito eliminado");
                        }
                        else
                        {
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Error al borrar el carrito")));
                        }

                    }else if (data.opcion == 1) {
                        if (data == null || data.id_articulo == null) throw new Exception("Se espera el id del articulo");
                        if (borrar_articulo(conexion, data.id_articulo ?? 0))
                        {
                            return new OkObjectResult("Articulo eliminado del carrito");
                        }
                        else
                        {
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Error al borrar el articulo")));
                        }
                    }
                    else
                    {
                        return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Opcion no valida")));
                    }
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
                }
                
            }
            catch (Exception ex) 
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(ex.Message)));
            }
        }
    }
}
