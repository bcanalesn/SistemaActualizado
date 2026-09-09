using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class TicketPrintService
    {
        private ConfiguracionEmpresa ObtenerDatosEmpresa()
        {
            using var db = new AppDbContext();
            var emp = db.ConfiguracionEmpresa.FirstOrDefault(e => e.EmpresaID == 1);
            
            if (emp == null || string.IsNullOrWhiteSpace(emp.Rut) || string.IsNullOrWhiteSpace(emp.RazonSocial))
            {
                throw new InvalidOperationException("No se han configurado los datos fiscales de este local. Ingrese con rol Administrador al módulo 'Configuración' antes de emitir documentos.");
            }

            return emp;
        }

        public void DibujarDocumentoTributario(Graphics g, TVE2607 venta, List<DetalleCarrito> items, decimal pagoCon, decimal vuelto, int anchoContenedor)
        {
            ConfiguracionEmpresa emp;
            try
            {
                emp = ObtenerDatosEmpresa();
            }
            catch (Exception ex)
            {
                g.DrawString($"⚠️ ERROR DE CONFIGURACIÓN:\n{ex.Message}", new Font("Segoe UI", 9F, FontStyle.Bold), Brushes.Red, new RectangleF(10, 20, anchoContenedor - 20, 100));
                return;
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Font fontTitle = new Font("Segoe UI", 12F, FontStyle.Bold);
            Font fontSub = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            Font fontBold = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            Font fontSmall = new Font("Segoe UI", 8F, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penDash = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen penRedBox = new Pen(Color.Red, 2);

            int y = 15;
            int width = anchoContenedor;
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };

            // Encabezado del Emisor (Datos fiscales reales desde la BD)
            g.DrawString(emp.RazonSocial, fontTitle, brushText, new RectangleF(0, y, width, 25), centerFormat);
            y += 24;
            g.DrawString($"R.U.T.: {emp.Rut}", fontBold, brushText, new RectangleF(0, y, width, 20), centerFormat);
            y += 18;
            g.DrawString($"Giro: {emp.Giro}", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
            y += 16;
            
            // Ubicación con Comuna y Ciudad
            List<string> partesUbicacion = new List<string>();
            if (!string.IsNullOrWhiteSpace(emp.Direccion)) partesUbicacion.Add(emp.Direccion);

            string comunaCiudadEmpresa = !string.IsNullOrWhiteSpace(emp.Comuna)
                ? (!string.IsNullOrWhiteSpace(emp.Ciudad) && !emp.Ciudad.Equals(emp.Comuna, StringComparison.OrdinalIgnoreCase) 
                    ? $"{emp.Comuna} - {emp.Ciudad}" 
                    : emp.Comuna)
                : (emp.Ciudad ?? "");

            if (!string.IsNullOrWhiteSpace(comunaCiudadEmpresa)) partesUbicacion.Add(comunaCiudadEmpresa);

            string ubicacion = string.Join(", ", partesUbicacion);
            g.DrawString($"Casa Matriz: {ubicacion}", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
            y += 16;
            
            if (!string.IsNullOrWhiteSpace(emp.Telefono))
            {
                g.DrawString($"Teléfono: {emp.Telefono}", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
                y += 18;
            }
            y += 6;

            if (!string.IsNullOrWhiteSpace(emp.Email))
            {
                g.DrawString($"Email: {emp.Email}", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
                y += 18;
            }
            y += 6;

            // Recuadro DTE o Ticket de Atención
            if (venta.iddocDTE > 0)
            {
                Rectangle redBox = new Rectangle(30, y, width - 60, 55);
                g.DrawRectangle(penRedBox, redBox);

                string tituloDoc = (venta.Documento ?? "BOLETA ELECTRÓNICA").ToUpper();
                g.DrawString(tituloDoc, fontBold, Brushes.Red, new RectangleF(30, y + 8, width - 60, 20), centerFormat);
                g.DrawString($"FOLIO N° {venta.nroDTE:D6}", fontTitle, Brushes.Red, new RectangleF(30, y + 26, width - 60, 24), centerFormat);
                y += 68;

                g.DrawString(emp.UnidadSII, fontBold, Brushes.Red, new RectangleF(0, y, width, 18), centerFormat);
                y += 22;
            }
            else
            {
                Rectangle blackBox = new Rectangle(30, y, width - 60, 50);
                g.DrawRectangle(Pens.Black, blackBox);

                g.DrawString("TICKET DE ATENCIÓN", fontBold, brushText, new RectangleF(30, y + 8, width - 60, 20), centerFormat);
                g.DrawString($"N° TICKET: {venta.nroDTE:D6}", fontTitle, brushText, new RectangleF(30, y + 26, width - 60, 22), centerFormat);
                y += 62;
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 10;

            g.DrawString($"Fecha Emisión : {venta.FecDoc:dd/MM/yyyy HH:mm:ss}", fontSub, brushText, 15, y); 
            y += 18;

            string formaPagoStr = !string.IsNullOrWhiteSpace(venta.MedioPago) ? venta.MedioPago : "CONTADO";
            g.DrawString($"Forma de Pago : {formaPagoStr}", fontBold, brushText, 15, y);
            y += 18;
            
            string nombreVendedor = !string.IsNullOrEmpty(venta.Vendedor) ? venta.Vendedor : (!string.IsNullOrEmpty(venta.UserDTE) ? venta.UserDTE : "Cajero");
            g.DrawString($"Atendido por  : {nombreVendedor}", fontSub, brushText, 15, y); 
            y += 18;

            // Datos del Receptor / Cliente
            if (!string.IsNullOrWhiteSpace(venta.RazonSocial))
            {
                g.DrawString($"SEÑOR(ES)    : {venta.RazonSocial}", fontBold, brushText, 15, y); 
                y += 18;
            }

            if (!string.IsNullOrWhiteSpace(venta.RuT) && !venta.RuT.Contains("66.666.666"))
            {
                g.DrawString($"R.U.T.        : {venta.RuT}", fontBold, brushText, 15, y); 
                y += 18;
            }

            // Desglose fiscal exclusivo para Facturas Electrónicas (iddocDTE == 33)
            if (venta.iddocDTE == 33)
            {
                if (!string.IsNullOrWhiteSpace(venta.Giro))
                {
                    g.DrawString($"GIRO          : {venta.Giro}", fontSub, brushText, 15, y);
                    y += 18;
                }

                if (!string.IsNullOrWhiteSpace(venta.Direccion))
                {
                    g.DrawString($"DIRECCIÓN     : {venta.Direccion}", fontSub, brushText, 15, y);
                    y += 18;
                }

                string comunaCiudadCliente = !string.IsNullOrWhiteSpace(venta.nComuna) 
                    ? (!string.IsNullOrWhiteSpace(venta.nCiudad) && !venta.nCiudad.Equals(venta.nComuna, StringComparison.OrdinalIgnoreCase) 
                        ? $"{venta.nComuna} - {venta.nCiudad}" 
                        : venta.nComuna)
                    : (venta.nCiudad ?? "");

                if (!string.IsNullOrWhiteSpace(comunaCiudadCliente))
                {
                    g.DrawString($"COMUNA/CIUDAD : {comunaCiudadCliente}", fontSub, brushText, 15, y);
                    y += 18;
                }

                if (!string.IsNullOrWhiteSpace(venta.email))
                {
                    g.DrawString($"CONTACTO      : {venta.email}", fontSub, brushText, 15, y);
                    y += 18;
                }
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 10;

            g.DrawString("CANT   DESCRIPCIÓN", fontBold, brushText, 15, y);
            g.DrawString("TOTAL", fontBold, brushText, width - 65, y);
            y += 18;
            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 8;

            foreach (var item in items)
            {
                string desc = item.Nombre.Length > 22 ? item.Nombre.Substring(0, 22) : item.Nombre;
                g.DrawString($"{item.Cantidad,2} x   {desc}", fontSub, brushText, 15, y);
                g.DrawString(MonedaHelper.Formatear(item.Subtotal, conSigno: true), fontSub, brushText, width - 85, y);
                y += 18;
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 12;

            g.DrawString("MONTO NETO:", fontSub, brushText, 120, y);
            g.DrawString(MonedaHelper.Formatear(venta.Neto, conSigno: true), fontSub, brushText, width - 95, y); 
            y += 18;

            g.DrawString("I.V.A. (19%):", fontSub, brushText, 120, y);
            g.DrawString(MonedaHelper.Formatear(venta.IvA, conSigno: true), fontSub, brushText, width - 95, y); 
            y += 22;

            g.DrawString("TOTAL A PAGAR:", fontTitle, brushText, 90, y);
            g.DrawString(MonedaHelper.Formatear(venta.Total, conSigno: true), fontTitle, brushText, width - 110, y); 
            y += 28;

            if (pagoCon > 0)
            {
                g.DrawLine(penDash, 15, y, width - 15, y); 
                y += 10;
                g.DrawString($"Paga Con: {MonedaHelper.Formatear(pagoCon, conSigno: true)}", fontSub, brushText, 15, y);
                g.DrawString($"Vuelto: {MonedaHelper.Formatear(vuelto, conSigno: true)}", fontBold, Brushes.Green, 200, y); 
                y += 22;
            }

            g.DrawLine(penDash, 15, y, width - 15, y); 
            y += 15;

            g.DrawString("Timbre Electrónico S.I.I.", fontSmall, brushText, new RectangleF(0, y, width, 15), centerFormat); 
            y += 15;
            g.DrawString($"{emp.ResolucionSII} - Verifique documento en www.sii.cl", fontSmall, brushText, new RectangleF(0, y, width, 15), centerFormat); 
            y += 25;

            g.DrawString(emp.TextoPieTicket, fontBold, brushText, new RectangleF(0, y, width, 20), centerFormat);
        }

        public void ImprimirTicket(Panel pnlPapel)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                Bitmap bmp = new Bitmap(pnlPapel.Width, pnlPapel.Height);
                pnlPapel.DrawToBitmap(bmp, new Rectangle(0, 0, pnlPapel.Width, pnlPapel.Height));
                ev.Graphics?.DrawImage(bmp, 0, 0);
            };

            PrintPreviewDialog preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 600,
                Height = 700
            };
            preview.ShowDialog();
        }

        public void ImprimirTicketAtencion(int nroTicket, string vendedor, string cliente, decimal total, List<DetalleCarrito> items)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintController = new StandardPrintController();

                pd.PrintPage += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    int width = 280;
                    int y = 10;

                    using Font fontBoldBig = new Font("Segoe UI", 12F, FontStyle.Bold);
                    using Font fontBold = new Font("Segoe UI", 9F, FontStyle.Bold);
                    using Font fontRegular = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                    using Font fontSmall = new Font("Segoe UI", 7.5F, FontStyle.Regular);
                    using StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                    using StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };

                    g.DrawString("TICKET DE ATENCIÓN", fontBoldBig, Brushes.Black, new RectangleF(0, y, width, 25), sfCenter);
                    y += 25;
                    g.DrawString($"N° #{nroTicket:D6}", fontBoldBig, Brushes.Black, new RectangleF(0, y, width, 25), sfCenter);
                    y += 28;

                    g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
                    y += 15;

                    g.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontRegular, Brushes.Black, 5, y); 
                    y += 16;
                    g.DrawString($"Vendedor: {vendedor}", fontRegular, Brushes.Black, 5, y); 
                    y += 16;
                    g.DrawString($"Cliente: {cliente}", fontRegular, Brushes.Black, 5, y); 
                    y += 20;

                    g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
                    y += 15;

                    g.DrawString("CANT  PRODUCTO", fontBold, Brushes.Black, 5, y);
                    g.DrawString("TOTAL", fontBold, Brushes.Black, new RectangleF(0, y, width - 5, 20), sfRight);
                    y += 18;

                    foreach (var item in items)
                    {
                        string nom = item.Nombre.Length > 20 ? item.Nombre.Substring(0, 18) + ".." : item.Nombre;
                        g.DrawString($"{item.Cantidad}x  {nom}", fontRegular, Brushes.Black, 5, y);
                        g.DrawString(MonedaHelper.Formatear(item.Subtotal, conSigno: true), fontRegular, Brushes.Black, new RectangleF(0, y, width - 5, 20), sfRight);
                        y += 16;
                    }

                    g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
                    y += 15;

                    g.DrawString("TOTAL A PAGAR:", fontBold, Brushes.Black, 5, y);
                    g.DrawString(MonedaHelper.Formatear(total, conSigno: true), fontBoldBig, Brushes.Black, new RectangleF(0, y - 2, width - 5, 25), sfRight);
                    y += 28;

                    g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
                    y += 15;

                    g.DrawString("Pase a CAJA a cancelar su ticket", fontBold, Brushes.Black, new RectangleF(0, y, width, 20), sfCenter);
                    y += 18;
                    g.DrawString("¡Gracias por su visita!", fontSmall, Brushes.Black, new RectangleF(0, y, width, 20), sfCenter);
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar a la impresora de tickets: {ex.Message}", "Aviso Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}