using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using SISTEMAACTUALIZADO.Helpers;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;


namespace SISTEMAACTUALIZADO.Services
{
    public class ProductoScrapDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public string UrlImagen { get; set; } = string.Empty;
    }

    public class JumboScraperService
    {
        private static readonly HttpClientHandler _handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        private static readonly HttpClient _httpClient = new HttpClient(_handler);

        static JumboScraperService()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "es-CL,es;q=0.9,en;q=0.8");
        }

        public async Task<List<ProductoScrapDTO>> BuscarProductosAsync(string termino)
        {
            var resultados = new List<ProductoScrapDTO>();
            if (string.IsNullOrWhiteSpace(termino)) return resultados;

            string searchUrl = $"https://www.jumbo.cl/busqueda?ft={Uri.EscapeDataString(termino.Trim())}";

            try
            {
                var response = await _httpClient.GetAsync(searchUrl);
                if (!response.IsSuccessStatusCode) return resultados;

                string html = await response.Content.ReadAsStringAsync();
                var config = AngleSharp.Configuration.Default;
                var context = BrowsingContext.New(config);
                var doc = await context.OpenAsync(req => req.Content(html));

                var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Extraer desde los bloques JSON estructurados (ld+json)
                var scriptsLdJson = doc.QuerySelectorAll("script[type='application/ld+json']");
                foreach (var script in scriptsLdJson)
                {
                    string json = script.TextContent?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(json)) continue;

                    try
                    {
                        using var jdoc = JsonDocument.Parse(json);
                        ExtraerProductosDeJson(jdoc.RootElement, resultados, seenUrls);
                    }
                    catch { }
                }

                // 2. Si no encontró en ld+json, usar expresiones regulares en el HTML crudo
                if (resultados.Count == 0)
                {
                    var matches = Regex.Matches(html, @"""name""\s*:\s*""([^""]+)""\s*,\s*""url""[^}]+""image""\s*:\s*""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        string nombre = Regex.Unescape(m.Groups[1].Value).Trim();
                        string urlImg = Regex.Unescape(m.Groups[2].Value).Trim();

                        if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(urlImg) && !seenUrls.Contains(urlImg))
                        {
                            if (urlImg.StartsWith("//")) urlImg = "https:" + urlImg;
                            seenUrls.Add(urlImg);
                            resultados.Add(new ProductoScrapDTO { Titulo = nombre, UrlImagen = urlImg });
                        }
                    }
                }
            }
            catch { }

            return resultados;
        }

        private void ExtraerProductosDeJson(JsonElement elem, List<ProductoScrapDTO> lista, HashSet<string> seenUrls)
        {
            if (elem.ValueKind == JsonValueKind.Object)
            {
                // Manejo de ItemList
                if (elem.TryGetProperty("itemListElement", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in itemsElem.EnumerateArray())
                    {
                        ExtraerProductosDeJson(it, lista, seenUrls);
                    }
                    return;
                }

                // Manejo de Product individual o anidado en item
                JsonElement prod = elem;
                if (elem.TryGetProperty("item", out var innerItem) && innerItem.ValueKind == JsonValueKind.Object)
                {
                    prod = innerItem;
                }

                string nombre = string.Empty;
                if (prod.TryGetProperty("name", out var propNombre)) nombre = propNombre.GetString() ?? string.Empty;

                string imgUrl = string.Empty;
                if (prod.TryGetProperty("image", out var propImg))
                {
                    if (propImg.ValueKind == JsonValueKind.String)
                    {
                        imgUrl = propImg.GetString() ?? string.Empty;
                    }
                    else if (propImg.ValueKind == JsonValueKind.Array)
                    {
                        var first = propImg.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == JsonValueKind.String) imgUrl = first.GetString() ?? string.Empty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(nombre) && !string.IsNullOrWhiteSpace(imgUrl) && !seenUrls.Contains(imgUrl))
                {
                    if (imgUrl.StartsWith("//")) imgUrl = "https:" + imgUrl;
                    seenUrls.Add(imgUrl);
                    lista.Add(new ProductoScrapDTO
                    {
                        Titulo = nombre,
                        UrlImagen = imgUrl
                    });
                }
            }
            else if (elem.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in elem.EnumerateArray())
                {
                    ExtraerProductosDeJson(child, lista, seenUrls);
                }
            }
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

                // DECODIFICAR WEBP/CUALQUIER FORMATO Y CONVERTIR A JPEG GENUINO
                using (var inStream = new MemoryStream(data))
                using (var image = await SixLabors.ImageSharp.Image.LoadAsync(inStream))
                {
                    var encoder = new JpegEncoder { Quality = 90 };

                    // Guardar JPEG real en la raíz del proyecto
                    using (var fsRaiz = new FileStream(rutaRaiz, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        await image.SaveAsJpegAsync(fsRaiz, encoder);
                    }

                    // Guardar copia idéntica en bin/Debug
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al procesar imagen: {ex.Message}");
                return null;
            }
        }
    }
}