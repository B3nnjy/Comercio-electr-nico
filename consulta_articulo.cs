using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace t7_2021630460_vs
{
    public class consulta_articulo
    {
        class Articulo
        {
            public int? id_articulo;
            public string? nombre;
            public string? descripcion;
            public float? precio;
            public string? foto;
        }

        class ParamConsultaArticulo
        {
            public string? palabra;
        }

        [Function("consulta_articulo")]
        public async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "post")]
           HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamConsultaArticulo? data = JsonConvert.DeserializeObject<ParamConsultaArticulo>(body);

                string? palabra = data.palabra;
                //Escribir la peticion en consola
                string? Server = Environment.GetEnvironmentVariable("Server");
                string? User = Environment.GetEnvironmentVariable("User");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";Database=" + Database + ";Uid=" + User + ";Pwd=" + Password + ";sslmode=Preferred";

                var conexion = new MySqlConnection(cs);
                conexion.Open();
                try
                {
                    var cmd = new MySqlCommand("SELECT " +
                            "a.id_articulo, " +
                            "a.nombre, " +
                            "a.descripcion, " +
                            "a.precio, " +
                            "b.foto, " +
                            "length(b.foto) " +
                        "FROM " +
                            "articulos a " +
                        "LEFT OUTER JOIN " +
                            "fotos_articulos b " +
                        "ON " +
                            "a.id_articulo = b.id_articulo " +
                        "WHERE " +
                            "(a.nombre LIKE @palabra OR a.descripcion LIKE @palabra)");
                    
                    cmd.Connection = conexion;
                    cmd.Parameters.AddWithValue("@palabra", "%" + palabra + "%");
                    MySqlDataReader r = cmd.ExecuteReader();

                    try
                    {
                        List<Articulo> articulos = new List<Articulo>();

                        while (r.Read())
                        {
                            var articulo = new Articulo();
                            articulo.id_articulo = r.GetInt32(0);
                            articulo.nombre = r.GetString(1);
                            articulo.descripcion = r.GetString(2);
                            articulo.precio = r.GetFloat(3);

                            if (!r.IsDBNull(4))
                            {
                                var longitud = r.GetInt32(5);
                                byte[] foto = new byte[longitud];
                                r.GetBytes(4, 0, foto, 0, longitud);
                                articulo.foto = Convert.ToBase64String(foto);
                            }
                            articulos.Add(articulo);
                        }

                        return new OkObjectResult(JsonConvert.SerializeObject(articulos));
                    }
                    finally
                    {
                        r.Close();
                    }
                }
                finally
                {
                    conexion.Close();
                }
                
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
