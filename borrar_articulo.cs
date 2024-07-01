using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace t7_2021630460_vs
{
    public class borrar_articulo
    {
        class ParamBorrarArticulo{
            public string? nombre;
        }
        [Function("borrar_articulo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                ParamBorrarArticulo? data = JsonConvert.DeserializeObject<ParamBorrarArticulo>(body);

                if (data == null || data.nombre == null) throw new Exception("Se espera el nombre");

                string? nombre = data.nombre;
                string? Server = Environment.GetEnvironmentVariable("Server");
                string? User = Environment.GetEnvironmentVariable("User");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");
                string cs = "Server=" + Server + ";Uid=" + User + ";Pwd=" + Password + ";Database=" + Database + ";SsMode=Preferred";

                var conn = new MySqlConnection(cs);
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.Transaction = transaction;
                    cmd.CommandText = "DELETE FROM fotos_articulos WHERE id_articulo=(SELECT id_articulo FROM articulos WHERE nombre=@nombre";
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.ExecuteNonQuery();

                    var cmd2 = new MySqlCommand();
                    cmd2.Connection = conn;
                    cmd2.Transaction = transaction;
                    cmd2.CommandText = "DELETE FROM articulo WHERE nombre = @nombre";
                    cmd2.Parameters.AddWithValue("@nombre", nombre);
                    cmd2.ExecuteNonQuery();
                    transaction.Commit();
                    return new OkObjectResult("Articulo borrado");
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult(new Error(e.Message));
                }
                finally
                {
                    conn.Close();
                }

            }
            catch
            {
                return new BadRequestObjectResult(new Error("Error al borrar el articulo"));
            }
        }
    }
}
