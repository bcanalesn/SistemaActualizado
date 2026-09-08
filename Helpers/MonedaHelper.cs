using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SISTEMAACTUALIZADO.Helpers
{
    public static class MonedaHelper
    {
        private static readonly CultureInfo _culturaCL = new CultureInfo("es-CL");

        /// <summary>
        /// Convierte un monto numérico al formato chileno con punto separador de miles.
        /// Ejemplo: 3990 -> "3.990" | conSigno: true -> "$ 3.990"
        /// </summary>
        public static string Formatear(decimal monto, bool conSigno = false)
        {
            // N0 formatea como entero con separador de miles sin decimales
            string formateado = monto.ToString("N0", _culturaCL);
            return conSigno ? $"$ {formateado}" : formateado;
        }

        /// <summary>
        /// Sobrecarga para enteros.
        /// </summary>
        public static string Formatear(int monto, bool conSigno = false)
        {
            return Formatear((decimal)monto, conSigno);
        }

        /// <summary>
        /// Limpia cualquier carácter que no sea numérico (puntos, signos, espacios) 
        /// y devuelve el valor decimal real para cálculos y persistencia en BD.
        /// Ejemplo: "3.990" -> 3990 | "$ 1.250.000" -> 1250000
        /// </summary>
        public static decimal Limpiar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0m;

            // Extraer solo los números
            string soloNumeros = Regex.Replace(texto, @"[^\d]", "");
            
            return decimal.TryParse(soloNumeros, out decimal resultado) ? resultado : 0m;
        }

        /// <summary>
        /// Formatea el texto de un TextBox en tiempo real mientras el usuario escribe,
        /// manteniendo el cursor en la posición correcta al final.
        /// </summary>
        public static void AplicarMascaraEnVivo(System.Windows.Forms.TextBox txt)
        {
            string textoLimpio = Regex.Replace(txt.Text, @"[^\d]", "");
            if (decimal.TryParse(textoLimpio, out decimal valor))
            {
                string textoFormateado = valor.ToString("N0", _culturaCL);
                if (txt.Text != textoFormateado)
                {
                    txt.Text = textoFormateado;
                    txt.SelectionStart = txt.Text.Length; // Mantiene el cursor al final
                }
            }
            else
            {
                txt.Text = "";
            }
        }
    }
}