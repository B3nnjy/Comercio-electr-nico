// Carlos Pineda G. 2024
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
namespace FunctionApp1
{
    public static class Get
    {
        [Function("Get")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequest req)
        {
            try
            {
                // obtiene los par�metros que pasan en la URL
                string? path = req.Query["nombre"];
                bool descargar = req.Query["descargar"] == "si";
                // la variable de entorno HOME est� predefinida en el servidor (C:\home o D:\home)
                string? home = Environment.GetEnvironmentVariable("HOME");
                byte[] contenido;
                try
                {
                    // lee el contenido solicitado en la peticion GET
                    contenido = File.ReadAllBytes(home + "/data" + path);
                }
                catch (FileNotFoundException)
                {
                    return new NotFoundResult();
                }
                string? nombre = Path.GetFileName(path);
                string? tipo_mime = MimeMapping.GetMimeMapping(nombre);
                Console.WriteLine(tipo_mime);
                DateTime fecha_modificacion = File.GetLastWriteTime(home + "/data" + path);
                // verifica si viene el encabezado "If-Modified-Since"
                // si es as�, compara la fecha que env�a el cliente con la fecha del archivo
                // si son iguales regresa el c�digo 304
                string? fecha = req.Headers["If-Modified-Since"];
                if (!string.IsNullOrEmpty(fecha))
                    if (DateTime.Parse(fecha) == fecha_modificacion)
                        return new StatusCodeResult(304);
                if (descargar) // indica al navegador que descargue el archivo
                    return new FileContentResult(contenido, tipo_mime) { FileDownloadName = nombre };
                else
                    return new FileContentResult(contenido, tipo_mime) { LastModified = fecha_modificacion };
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}