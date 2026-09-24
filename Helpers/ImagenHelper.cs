using System;
using System.Drawing;
using System.IO;

namespace SISTEMAACTUALIZADO.Helpers
{
    public static class ImagenHelper
    {
        private static string? _carpetaRaizCache;

        /// <summary>
        /// Detecta la carpeta 'Imagenes' en la raíz del proyecto fuente (junto al .csproj)
        /// </summary>
        public static string ObtenerCarpetaImagenes()
        {
            if (!string.IsNullOrEmpty(_carpetaRaizCache) && Directory.Exists(_carpetaRaizCache))
            {
                return _carpetaRaizCache;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo? dir = new DirectoryInfo(baseDir);

            // Subir niveles hasta encontrar el archivo del proyecto .csproj
            while (dir != null)
            {
                if (dir.GetFiles("*.csproj").Length > 0)
                {
                    string rutaImagenesRaiz = Path.Combine(dir.FullName, "Imagenes");
                    if (!Directory.Exists(rutaImagenesRaiz))
                    {
                        Directory.CreateDirectory(rutaImagenesRaiz);
                    }
                    _carpetaRaizCache = rutaImagenesRaiz;
                    return _carpetaRaizCache;
                }
                dir = dir.Parent;
            }

            // Fallback por si estuviera publicado sin .csproj
            string fallback = Path.Combine(baseDir, "Imagenes");
            if (!Directory.Exists(fallback)) Directory.CreateDirectory(fallback);
            _carpetaRaizCache = fallback;
            return _carpetaRaizCache;
        }

        /// <summary>
        /// Busca el archivo en la raíz del proyecto, en bin/Debug y en rutas absolutas
        /// </summary>
        public static string? ResolverRutaFisica(string? pathOValorBD)
        {
            if (string.IsNullOrWhiteSpace(pathOValorBD)) return null;

            if (Path.IsPathRooted(pathOValorBD) && File.Exists(pathOValorBD))
            {
                return pathOValorBD;
            }

            string soloArchivo = Path.GetFileName(pathOValorBD);

            // 1. Probar en la carpeta Imagenes de la raíz del proyecto
            string carpetaRaiz = ObtenerCarpetaImagenes();
            string rutaRaiz = Path.Combine(carpetaRaiz, soloArchivo);
            if (File.Exists(rutaRaiz)) return rutaRaiz;

            // 2. Probar en bin\Debug\net10.0-windows\Imagenes
            string rutaBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", soloArchivo);
            if (File.Exists(rutaBin)) return rutaBin;

            return null;
        }

        /// <summary>
        /// Carga la imagen desacoplada en memoria (Bitmap) para evitar bloqueos
        /// </summary>
        public static Image? CargarImagenSegura(string? pathOValorBD)
        {
            string? rutaFisica = ResolverRutaFisica(pathOValorBD);
            if (string.IsNullOrEmpty(rutaFisica) || !File.Exists(rutaFisica)) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(rutaFisica);
                using var ms = new MemoryStream(bytes);
                return Image.FromStream(ms);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImagenHelper] Error cargando {rutaFisica}: {ex.Message}");
                return null;
            }
        }

        public static string CopiarAImagenesYObtenerNombreRelativo(string rutaArchivoOrigen, string? codigoBarra = null)
        {
            if (!File.Exists(rutaArchivoOrigen)) return string.Empty;

            string carpetaRaiz = ObtenerCarpetaImagenes();
            string ext = Path.GetExtension(rutaArchivoOrigen);
            string nombreFinal = !string.IsNullOrWhiteSpace(codigoBarra)
                ? $"{codigoBarra.Trim()}{ext}"
                : Path.GetFileName(rutaArchivoOrigen);

            string destinoRaiz = Path.Combine(carpetaRaiz, nombreFinal);
            File.Copy(rutaArchivoOrigen, destinoRaiz, overwrite: true);

            string destinoBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", nombreFinal);
            if (!string.Equals(Path.GetFullPath(destinoRaiz), Path.GetFullPath(destinoBin), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(rutaArchivoOrigen, destinoBin, overwrite: true);
            }

            return nombreFinal;
        }
    }
}