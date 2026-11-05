using System.Net;

namespace Jardin.Configuracion.Pruebas;

// Solo para comprobar el frontend con JWT y bases SQLite descartables del ejecutable de pruebas.
public static class ServidorIntegracion
{
    public static async Task EjecutarAsync(HttpClient cliente)
    {
        using var escucha = new HttpListener();
        escucha.Prefixes.Add("http://127.0.0.1:5189/");
        escucha.Start();
        Console.WriteLine("API aislada de integración: http://127.0.0.1:5189. Ctrl+C para finalizar; no utiliza la base real.");
        while (true)
        {
            var llamada = await escucha.GetContextAsync();
            try
            {
                using var peticion = new HttpRequestMessage(new HttpMethod(llamada.Request.HttpMethod), llamada.Request.RawUrl);
                if (llamada.Request.HasEntityBody)
                {
                    using var memoria = new MemoryStream();
                    await llamada.Request.InputStream.CopyToAsync(memoria);
                    peticion.Content = new ByteArrayContent(memoria.ToArray());
                }
                foreach (var nombre in llamada.Request.Headers.AllKeys.Where(x => x != null))
                {
                    if (nombre is "Host" or "Content-Length" or "Connection") continue;
                    if (!peticion.Headers.TryAddWithoutValidation(nombre!, llamada.Request.Headers[nombre]))
                        peticion.Content?.Headers.TryAddWithoutValidation(nombre!, llamada.Request.Headers[nombre]);
                }
                using var resultado = await cliente.SendAsync(peticion);
                llamada.Response.StatusCode = (int)resultado.StatusCode;
                foreach (var cabecera in resultado.Headers.Concat(resultado.Content.Headers))
                {
                    if (cabecera.Key is "Transfer-Encoding" or "Content-Length" or "Connection") continue;
                    llamada.Response.Headers[cabecera.Key] = string.Join(", ", cabecera.Value);
                }
                var contenido = await resultado.Content.ReadAsByteArrayAsync();
                llamada.Response.ContentLength64 = contenido.Length;
                await llamada.Response.OutputStream.WriteAsync(contenido);
            }
            catch (Exception error)
            {
                llamada.Response.StatusCode = 500;
                Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
            }
            finally { llamada.Response.Close(); }
        }
    }
}
