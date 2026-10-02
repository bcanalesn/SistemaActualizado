using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SISTEMAACTUALIZADO.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SISTEMAACTUALIZADO.Services
{
    public class GoogleScraperService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static GoogleScraperService()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "es-CL,es;q=0.9,en;q=0.8");
        }

        public async Task<List<ProductoScrapDTO>> BuscarImagenesGoogleAsync(string termino)
        {
            var resultados = new List<ProductoScrapDTO>();
            if (string.IsNullOrWhiteSpace(termino)) return resultados;

            string query = termino.Trim();
            // Búsqueda en Bing con filtro de fotos cuadradas tipo catálogo
            string urlBing = $"https://www.bing.com/images/async?q={Uri.EscapeDataString(query + " chile")}&qft=+filterui:photo+filterui:aspect-square&async=content&first=1&count=24";

            try
            {
                var response = await _httpClient.GetAsync(urlBing);
                if (!response.IsSuccessStatusCode) return resultados;

                string html = await response.Content.ReadAsStringAsync();

                // Bing entrega en cada resultado un bloque JSON codificado en el atributo m:
                // murl = imagen de alta calidad, turl = miniatura cacheada en CDN de Microsoft
                var matches = Regex.Matches(html, @"m=""({[^}]+})""");

                var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int contador = 1;

                foreach (Match m in matches)
                {
                    string jsonItem = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);

                    // 1. Extraer URL de la miniatura rápida (CDN Bing)
                    var matchTurl = Regex.Match(jsonItem, @"""turl"":""(https?://[^""]+)""");
                    // 2. Extraer URL de alta resolución
                    var matchMurl = Regex.Match(jsonItem, @"""murl"":""(https?://[^""]+)""");
                    // 3. Extraer título si existe
                    var matchTitle = Regex.Match(jsonItem, @"""t"":""([^""]+)""");

                    string miniaturaUrl = matchTurl.Success ? matchTurl.Groups[1].Value : (matchMurl.Success ? matchMurl.Groups[1].Value : "");
                    string altaResolucionUrl = matchMurl.Success ? matchMurl.Groups[1].Value : miniaturaUrl;
                    string titulo = matchTitle.Success ? matchTitle.Groups[1].Value : $"{query} - Opción {contador}";

                    if (!string.IsNullOrWhiteSpace(miniaturaUrl) && seenUrls.Add(miniaturaUrl))
                    {
                        resultados.Add(new ProductoScrapDTO
                        {
                            Titulo = titulo,
                            // Usamos la miniatura del CDN para mostrarla al instante en pantalla
                            UrlImagen = miniaturaUrl
                        });

                        contador++;
                        if (resultados.Count >= 24) break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al consultar Bing CDN: {ex.Message}");
            }

            return resultados;
        }

        public async Task<string?> DescargarYGuardarImagenAsync(string urlImagen, string nombreProducto, string? codigoBarra = null)
        {
            try
            {
                byte[] data = await _httpClient.GetByteArrayAsync(urlImagen);
                if (data == null || data.Length < 400) return null;

                string cleanCodigo = Regex.Replace(codigoBarra ?? "", @"[\\/:*?""<>|]", "").Trim();
                string fileName = !string.IsNullOrWhiteSpace(cleanCodigo)
                    ? $"{cleanCodigo}.jpg"
                    : $"{Regex.Replace(nombreProducto, @"[\\/:*?""<>|]", "").Trim().Replace(" ", "_")}_{DateTime.Now.Ticks % 10000}.jpg";

                string carpetaRaiz = ImagenHelper.ObtenerCarpetaImagenes();
                string rutaRaiz = Path.Combine(carpetaRaiz, fileName);

                string carpetaBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes");
                if (!Directory.Exists(carpetaBin)) Directory.CreateDirectory(carpetaBin);
                string rutaBin = Path.Combine(carpetaBin, fileName);

                using (var inStream = new MemoryStream(data))
                using (var image = await SixLabors.ImageSharp.Image.LoadAsync(inStream))
                {
                    var encoder = new JpegEncoder { Quality = 90 };

                    using (var fsRaiz = new FileStream(rutaRaiz, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        await image.SaveAsJpegAsync(fsRaiz, encoder);
                    }

                    if (!string.Equals(Path.GetFullPath(rutaRaiz), Path.GetFullPath(rutaBin), StringComparison.OrdinalIgnoreCase))
                    {
                        using (var fsBin = new FileStream(rutaBin, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            await image.SaveAsJpegAsync(fsBin, encoder);
                        }
                    }
                }

                return fileName;
            }
            catch
            {
                return null;
            }
        }
    }
}