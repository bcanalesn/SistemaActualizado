using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace SISTEMAACTUALIZADO.Helpers
{
    public class EstiloCategoriaDTO
    {
        public string FondoHex { get; set; } = "#F0FDF4";
        public string TextoHex { get; set; } = "#16A34A";
        public string BordeHex { get; set; } = "#15803D";
    }

    public static class CategoriaColorHelper
    {
        private static readonly string ArchivoJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "categorias_colores.json");

        // Paleta base predefinida para elegir
        public static readonly List<(string Nombre, Color Fondo, Color Texto, Color Borde)> OpcionesPaleta = new()
        {
            ("Azul", Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216)),
            ("Verde", Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61)),
            ("Naranja", Color.FromArgb(255, 247, 237), Color.FromArgb(234, 88, 12), Color.FromArgb(194, 65, 12)),
            ("Morado", Color.FromArgb(245, 243, 255), Color.FromArgb(124, 58, 237), Color.FromArgb(109, 40, 217)),
            ("Rojo", Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38), Color.FromArgb(185, 28, 28)),
            ("Celeste", Color.FromArgb(236, 254, 255), Color.FromArgb(8, 145, 178), Color.FromArgb(14, 116, 144)),
            ("Amarillo", Color.FromArgb(254, 252, 232), Color.FromArgb(202, 138, 4), Color.FromArgb(161, 98, 7)),
            ("Rosa", Color.FromArgb(253, 242, 248), Color.FromArgb(219, 39, 119), Color.FromArgb(190, 24, 93)),
            ("Gris", Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105), Color.FromArgb(51, 65, 85)),
            ("Lima", Color.FromArgb(247, 254, 231), Color.FromArgb(101, 163, 13), Color.FromArgb(77, 124, 15))
        };

        private static Dictionary<string, EstiloCategoriaDTO> _cache = new(StringComparer.OrdinalIgnoreCase);

        static CategoriaColorHelper()
        {
            CargarDesdeDisco();
        }

        private static void CargarDesdeDisco()
        {
            try
            {
                if (File.Exists(ArchivoJson))
                {
                    string json = File.ReadAllText(ArchivoJson);
                    var datos = JsonSerializer.Deserialize<Dictionary<string, EstiloCategoriaDTO>>(json);
                    if (datos != null)
                    {
                        _cache = new Dictionary<string, EstiloCategoriaDTO>(datos, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch { }
        }

        private static void GuardarEnDisco()
        {
            try
            {
                string json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ArchivoJson, json);
            }
            catch { }
        }

        public static (Color Fondo, Color Texto, Color Borde) ObtenerColor(string categoria)
        {
            string catLimpia = (categoria ?? "General").Trim();

            if (_cache.TryGetValue(catLimpia, out var dto))
            {
                return (
                    ColorTranslator.FromHtml(dto.FondoHex),
                    ColorTranslator.FromHtml(dto.TextoHex),
                    ColorTranslator.FromHtml(dto.BordeHex)
                );
            }

            // Fallback determinista por hash si nunca se ha personalizado
            int hash = Math.Abs(catLimpia.ToLowerInvariant().GetHashCode());
            var def = OpcionesPaleta[hash % OpcionesPaleta.Count];
            return (def.Fondo, def.Texto, def.Borde);
        }

        public static void GuardarColor(string categoria, Color fondo, Color texto, Color borde)
        {
            string catLimpia = (categoria ?? "").Trim();
            if (string.IsNullOrWhiteSpace(catLimpia)) return;

            _cache[catLimpia] = new EstiloCategoriaDTO
            {
                FondoHex = ColorTranslator.ToHtml(fondo),
                TextoHex = ColorTranslator.ToHtml(texto),
                BordeHex = ColorTranslator.ToHtml(borde)
            };

            GuardarEnDisco();
        }
    }
}