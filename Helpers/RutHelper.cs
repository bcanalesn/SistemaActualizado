using System;
using System.Text.RegularExpressions;

namespace SISTEMAACTUALIZADO.Helpers
{
    public static class RutHelper
    {
        public static string Limpiar(string rut)
        {
            if (string.IsNullOrEmpty(rut)) return string.Empty;
            return Regex.Replace(rut, @"[^0-9kK]", "").ToUpper();
        }

        public static string Formatear(string rut)
        {
            string limpio = Limpiar(rut);
            if (limpio.Length <= 1) return limpio;

            string dv = limpio.Substring(limpio.Length - 1);
            string cuerpo = limpio.Substring(0, limpio.Length - 1);

            if (cuerpo.Length > 8) cuerpo = cuerpo.Substring(0, 8);

            if (double.TryParse(cuerpo, out double numCuerpo))
            {
                string cuerpoFormateado = numCuerpo.ToString("N0", new System.Globalization.CultureInfo("es-CL"));
                return $"{cuerpoFormateado}-{dv}";
            }

            return limpio;
        }

        // Flexibilizamos para validar que tenga largo de formato chileno sin bloquear RUTs válidos altos/demo
        public static bool EsValidoFormato(string rut)
        {
            string limpio = Limpiar(rut);
            return limpio.Length >= 8 && limpio.Length <= 9;
        }
    }
}