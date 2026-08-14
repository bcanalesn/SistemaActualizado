using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class TicketPrintService
    {
        public void DibujarDocumentoTributario(Graphics g, TVE2607 venta, List<DetalleCarrito> items, decimal pagoCon, decimal vuelto, int anchoContenedor)
        {
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
            g.DrawString("SISTEMA POS COMERCIAL", fontTitle, brushText, new RectangleF(0, y, width, 25), centerFormat);
            y += 24;
            g.DrawString("R.U.T.: 76.543.210-K", fontBold, brushText, new RectangleF(0, y, width, 20), centerFormat);
            y += 18;
            g.DrawString("Giro: Comercializadora General y Retail", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
            y += 16;
            g.DrawString("Casa Matriz: Av. Principal #1234, Santiago", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
            y += 16;
            g.DrawString("Teléfono: +56 2 2345 6789", fontSub, brushText, new RectangleF(0, y, width, 18), centerFormat);
            y += 22;

            if (venta.iddocDTE > 0)
            {
                Rectangle redBox = new Rectangle(30, y, width - 60, 55);
                g.DrawRectangle(penRedBox, redBox);

                string tituloDoc = (venta.Documento ?? "BOLETA ELECTRÓNICA").ToUpper();
                g.DrawString(tituloDoc, fontBold, Brushes.Red, new RectangleF(30, y + 8, width - 60, 20), centerFormat);
                g.DrawString($"FOLIO N° {venta.nroDTE:D6}", fontTitle, Brushes.Red, new RectangleF(30, y + 26, width - 60, 24), centerFormat);
                y += 68;

                g.DrawString("S.I.I. - SANTIAGO CENTRO", fontBold, Brushes.Red, new RectangleF(0, y, width, 18), centerFormat);
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

            g.DrawString($"Fecha Emisión: {venta.FecDoc:dd/MM/yyyy HH:mm:ss}", fontSub, brushText, 15, y); y += 18;
            string nombreVendedor = !string.IsNullOrEmpty(venta.Vendedor) ? venta.Vendedor : (!string.IsNullOrEmpty(venta.UserDTE) ? venta.UserDTE : "Bárbara");
            g.DrawString($"Atendido por : {nombreVendedor}", fontSub, brushText, 15, y); 
            y += 18;

            if (!string.IsNullOrEmpty(venta.RuT))
            {
                g.DrawString($"RUT Cliente   : {venta.RuT}", fontBold, brushText, 15, y); y += 18;
            }
            if (!string.IsNullOrEmpty(venta.RazonSocial))
            {
                g.DrawString($"Razón Social : {venta.RazonSocial}", fontSub, brushText, 15, y); y += 18;
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
                g.DrawString($"${item.Subtotal,8:N0}", fontSub, brushText, width - 85, y);
                y += 18;
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 12;

            g.DrawString("MONTO NETO:", fontSub, brushText, 120, y);
            g.DrawString($"${venta.Neto,10:N0}", fontSub, brushText, width - 95, y); y += 18;

            g.DrawString("I.V.A. (19%):", fontSub, brushText, 120, y);
            g.DrawString($"${venta.IvA,10:N0}", fontSub, brushText, width - 95, y); y += 22;

            g.DrawString("TOTAL A PAGAR:", fontTitle, brushText, 90, y);
            g.DrawString($"${venta.Total,10:N0}", fontTitle, brushText, width - 110, y); y += 28;

            if (pagoCon > 0)
            {
                g.DrawLine(penDash, 15, y, width - 15, y); y += 10;
                g.DrawString($"Paga Con: ${pagoCon:N0}", fontSub, brushText, 15, y);
                g.DrawString($"Vuelto: ${vuelto:N0}", fontBold, Brushes.Green, 200, y); y += 22;
            }

            g.DrawLine(penDash, 15, y, width - 15, y); y += 15;

            g.DrawString("Timbre Electrónico S.I.I.", fontSmall, brushText, new RectangleF(0, y, width, 15), centerFormat); y += 15;
            g.DrawString("Res. 99 de 2026 - Verifique documento en www.sii.cl", fontSmall, brushText, new RectangleF(0, y, width, 15), centerFormat); y += 25;

            g.DrawString("¡Gracias por su compra!", fontBold, brushText, new RectangleF(0, y, width, 20), centerFormat);
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
        pd.PrintController = new StandardPrintController(); // Oculta el cuadro de diálogo emergente de impresión

        pd.PrintPage += (s, e) =>
        {
            Graphics g = e.Graphics;
            int width = 280; // Ancho estándar térmico 80mm
            int y = 10;

            using Font fontBoldBig = new Font("Segoe UI", 12F, FontStyle.Bold);
            using Font fontBold = new Font("Segoe UI", 9F, FontStyle.Bold);
            using Font fontRegular = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using Font fontSmall = new Font("Segoe UI", 7.5F, FontStyle.Regular);
            using StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
            using StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };

            // Encabezado
            g.DrawString("TICKET DE ATENCIÓN", fontBoldBig, Brushes.Black, new RectangleF(0, y, width, 25), sfCenter);
            y += 25;
            g.DrawString($"N° #{nroTicket:D6}", fontBoldBig, Brushes.Black, new RectangleF(0, y, width, 25), sfCenter);
            y += 28;

            g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
            y += 15;

            // Datos Preventa
            g.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontRegular, Brushes.Black, 5, y); y += 16;
            g.DrawString($"Vendedor: {vendedor}", fontRegular, Brushes.Black, 5, y); y += 16;
            g.DrawString($"Cliente: {cliente}", fontRegular, Brushes.Black, 5, y); y += 20;

            g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
            y += 15;

            // Ítems
            g.DrawString("CANT  PRODUCTO", fontBold, Brushes.Black, 5, y);
            g.DrawString("TOTAL", fontBold, Brushes.Black, new RectangleF(0, y, width - 5, 20), sfRight);
            y += 18;

            foreach (var item in items)
            {
                string nom = item.Nombre.Length > 20 ? item.Nombre.Substring(0, 18) + ".." : item.Nombre;
                g.DrawString($"{item.Cantidad}x  {nom}", fontRegular, Brushes.Black, 5, y);
                g.DrawString($"${item.Subtotal:N0}", fontRegular, Brushes.Black, new RectangleF(0, y, width - 5, 20), sfRight);
                y += 16;
            }

            g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
            y += 15;

            // Total
            g.DrawString("TOTAL A PAGAR:", fontBold, Brushes.Black, 5, y);
            g.DrawString($"${total:N0}", fontBoldBig, Brushes.Black, new RectangleF(0, y - 2, width - 5, 25), sfRight);
            y += 28;

            g.DrawString("--------------------------------------------------", fontRegular, Brushes.Black, 0, y);
            y += 15;

            // Pie
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