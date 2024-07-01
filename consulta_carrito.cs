using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace t7_2021630460_vs
{
    public class consulta_carrito
    {
       public class Carrito
        {
            public int? id_carrito;
            public int? id_articulo;
            public string? nombre;
            public string? descripcion;
            public float? precio;
            public int? cantidad;
            public string? foto;
        }

        public class ParamConsultaCarrito
        {
            public Carrito? carrito;
        }

        [Function("consulta_carrito")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamConsultaCarrito? data = JsonConvert.DeserializeObject<ParamConsultaCarrito>(body);

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
                            "c.nombre, " +
                            "c.descripcion, " +
                            "a.cantidad, " +
                            "c.precio, " +
                            "b.foto, " +
                            "length(b.foto) " +
                        "FROM " +
                            "carrito_compra a " +
                        "INNER JOIN " +
                            "articulos c " +
                        "ON " +
                            "a.id_articulo = c.id_articulo " +
                        "LEFT OUTER JOIN " +
                            "fotos_articulos b " +
                        "ON " +
                            "a.id_articulo = b.id_articulo ");

                    cmd.Connection = conexion;
                    MySqlDataReader r = cmd.ExecuteReader();

                    try
                    {
                        List<Carrito> articulos = new List<Carrito>();

                        while (r.Read())
                        {
                            var carrito = new Carrito();
                            carrito.id_articulo = r.GetInt32(0);
                            carrito.nombre = r.GetString(1);
                            carrito.descripcion = r.GetString(2);
                            carrito.cantidad = r.GetInt32(3);
                            carrito.precio = r.GetFloat(4);

                            if (!r.IsDBNull(5))
                            {
                                var longitud = r.GetInt32(6);
                                byte[] foto = new byte[longitud];
                                r.GetBytes(5, 0, foto, 0, longitud);
                                carrito.foto = Convert.ToBase64String(foto);
                            }
                            articulos.Add(carrito);
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
