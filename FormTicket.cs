using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public partial class FormTicket : Form
    {
        private TVE2607 _venta;
        private List<DetalleCarrito> _items;
        private decimal _pagoCon;
        private decimal _vuelto;

        private Button btnImprimir = null!;
        private Button btnCerrar = null!;
        private Panel pnlBoletaPapel = null!;

        public FormTicket(TVE2607 venta, List<DetalleCarrito> items, decimal pagaCon, decimal vuelto)
        {
            _venta = venta;
            _items = items ?? new List<DetalleCarrito>();
            _pagoCon = pagaCon;
            _vuelto = vuelto;

            InitializeComponent();
        }

        public FormTicket(int nroTicket, DateTime fecha, List<DetalleCarrito> items, decimal total, decimal pagaCon, decimal vuelto, string medioPago)
        {
            _venta = new TVE2607
            {
                nroDTE = nroTicket,
                FecDoc = fecha,
                Total = total,
                Neto = Math.Round(total / 1.19m, 0),
                IvA = total - Math.Round(total / 1.19m, 0),
                UserDTE = medioPago,
                Documento = medioPago.Contains("Factura") ? "Factura Electrónica" : "Boleta Electrónica"
            };
            _items = items ?? new List<DetalleCarrito>();
            _pagoCon = pagaCon;
            _vuelto = vuelto;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = $"{_venta.Documento} - N° {_venta.nroDTE:D6}";
            this.Size = new Size(420, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(241, 245, 249);

            // Panel Superior con Botones
            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };

            btnImprimir = new Button
            {
                Text = "🖨️ Imprimir Ticket",
                Size = new Size(150, 35),
                Location = new Point(12, 10),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            btnImprimir.Click += BtnImprimir_Click;

            btnCerrar = new Button
            {
                Text = "❌ Cerrar",
                Size = new Size(100, 35),
                Location = new Point(290, 10),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlTopBar.Controls.Add(btnImprimir);
            pnlTopBar.Controls.Add(btnCerrar);

            // Panel simulador de boleta en papel térmico
            Panel pnlScrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            pnlBoletaPapel = new Panel
            {
                Size = new Size(360, 850),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 20)
            };
            pnlBoletaPapel.Paint += PnlBoletaPapel_Paint;

            pnlScrollContainer.Controls.Add(pnlBoletaPapel);

            this.Controls.Add(pnlScrollContainer);
            this.Controls.Add(pnlTopBar);

            this.ResumeLayout(false);
        }

        private void PnlBoletaPapel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Font fontTitle = new Font("Segoe UI", 12F, FontStyle.Bold);
            Font fontSub = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            Font fontBold = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            Font fontSmall = new Font("Segoe UI", 8F, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penDash = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen penRedBox = new Pen(Color.Red, 2);

            int y = 15;
            int width = pnlBoletaPapel.Width;

            // 1. Encabezado de la Empresa
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

            // 2. Recuadro Rojo Tributario (Estilo SII)
            if (_venta.iddocDTE > 0)
            {
                Rectangle redBox = new Rectangle(30, y, width - 60, 55);
                g.DrawRectangle(penRedBox, redBox);

                string tituloDoc = _venta.Documento.ToUpper();
                g.DrawString(tituloDoc, fontBold, Brushes.Red, new RectangleF(30, y + 8, width - 60, 20), centerFormat);
                g.DrawString($"FOLIO N° {_venta.nroDTE:D6}", fontTitle, Brushes.Red, new RectangleF(30, y + 26, width - 60, 24), centerFormat);
                y += 68;

                g.DrawString("S.I.I. - SANTIAGO CENTRO", fontBold, Brushes.Red, new RectangleF(0, y, width, 18), centerFormat);
                y += 22;
            }
            else
            {
                // Ticket de Atención
                Rectangle blackBox = new Rectangle(30, y, width - 60, 50);
                g.DrawRectangle(Pens.Black, blackBox);

                g.DrawString("TICKET DE ATENCIÓN", fontBold, brushText, new RectangleF(30, y + 8, width - 60, 20), centerFormat);
                g.DrawString($"N° TICKET: {_venta.nroDTE:D6}", fontTitle, brushText, new RectangleF(30, y + 26, width - 60, 22), centerFormat);
                y += 62;
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 10;

            // 3. Datos de la Venta
            g.DrawString($"Fecha Emisión: {_venta.FecDoc:dd/MM/yyyy HH:mm:ss}", fontSub, brushText, 15, y); y += 18;
            g.DrawString($"Atendido por : {(_venta.UserDTE ?? "barbara")}", fontSub, brushText, 15, y); y += 18;

            if (!string.IsNullOrEmpty(_venta.RuT))
            {
                g.DrawString($"RUT Cliente  : {_venta.RuT}", fontBold, brushText, 15, y); y += 18;
            }
            if (!string.IsNullOrEmpty(_venta.RazonSocial))
            {
                g.DrawString($"Razón Social : {_venta.RazonSocial}", fontSub, brushText, 15, y); y += 18;
            }
            if (_venta.iddocDTE == 61 && _venta.nroREF.HasValue)
            {
                g.DrawString($"Doc. Modifica : Referencia DTE N° {_venta.nroREF}", fontBold, Brushes.Red, 15, y); y += 18;
                if (!string.IsNullOrEmpty(_venta.codigoREF))
                {
                    g.DrawString($"Causa / Glosa : {_venta.codigoREF}", fontSub, brushText, 15, y); y += 18;
                }
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 10;

            // 4. Encabezados de Tabla de Ítems
            g.DrawString("CANT  DESCRIPCIÓN", fontBold, brushText, 15, y);
            g.DrawString("TOTAL", fontBold, brushText, width - 65, y);
            y += 18;
            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 8;

            // 5. Listado de Ítems del Carrito
            foreach (var item in _items)
            {
                string desc = item.Nombre.Length > 22 ? item.Nombre.Substring(0, 22) : item.Nombre;
                g.DrawString($"{item.Cantidad,2} x  {desc}", fontSub, brushText, 15, y);
                g.DrawString($"${item.Subtotal,8:N0}", fontSub, brushText, width - 85, y);
                y += 18;
            }

            g.DrawLine(penDash, 15, y, width - 15, y);
            y += 12;

            // 6. Totales y Desglose de Impuestos
            g.DrawString("MONTO NETO:", fontSub, brushText, 120, y);
            g.DrawString($"${_venta.Neto,10:N0}", fontSub, brushText, width - 95, y); y += 18;

            g.DrawString("I.V.A. (19%):", fontSub, brushText, 120, y);
            g.DrawString($"${_venta.IvA,10:N0}", fontSub, brushText, width - 95, y); y += 22;

            g.DrawString("TOTAL A PAGAR:", fontTitle, brushText, 90, y);
            g.DrawString($"${_venta.Total,10:N0}", fontTitle, brushText, width - 110, y); y += 28;

            if (_pagoCon > 0)
            {
                g.DrawLine(penDash, 15, y, width - 15, y); y += 10;
                g.DrawString($"Paga Con: ${_pagoCon:N0}", fontSub, brushText, 15, y);
                g.DrawString($"Vuelto: ${_vuelto:N0}", fontBold, Brushes.Green, 200, y); y += 22;
            }

            g.DrawLine(penDash, 15, y, width - 15, y); y += 15;

            // 7. Pie de Página / Timbre Electrónico Simulado
            g.DrawString("Timbre Electrónico S.I.I.", fontSmall, brushText, new RectangleF(0, y, width, 15), centerFormat); y += 15;
            g.DrawString("Res. 99 de 2026 - Verifique documento en www.sii.cl", fontSmall, brushText, new RectangleF(0, y, width, 15), centerFormat); y += 25;

            g.DrawString("¡Gracias por su compra!", fontBold, brushText, new RectangleF(0, y, width, 20), centerFormat);
        }

        private void BtnImprimir_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    // Reutilizar el dibujado de la boleta para la impresora
                    Bitmap bmp = new Bitmap(pnlBoletaPapel.Width, pnlBoletaPapel.Height);
                    pnlBoletaPapel.DrawToBitmap(bmp, new Rectangle(0, 0, pnlBoletaPapel.Width, pnlBoletaPapel.Height));
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar a impresión: {ex.Message}", "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}