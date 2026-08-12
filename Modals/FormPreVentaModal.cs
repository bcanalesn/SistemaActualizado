using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormPreVentaModal : Form
    {
        private readonly TicketPrintService _printService = new TicketPrintService();

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        public FormPreVentaModal(int nroTicket, string vendedor, string cliente, decimal total, List<DetalleCarrito> items, bool autoImprimir = true)
        {
            InitializeComponent(nroTicket, vendedor, cliente, total, items);

            // IMPRESIÓN AUTOMÁTICA AL ABRIR LA VENTANA
            if (autoImprimir)
            {
                this.Shown += (s, e) =>
                {
                    _printService.ImprimirTicketAtencion(nroTicket, vendedor, cliente, total, items);
                };
            }
        }

        private void InitializeComponent(int nroTicket, string vendedor, string cliente, decimal total, List<DetalleCarrito> items)
        {
            this.Text = "Pre-Venta Registrada";
            this.Size = new Size(460, 530);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            pnlMain.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            // Encabezado Check Verde
            Panel circleCheck = new Panel { Location = new Point(20, 15), Size = new Size(46, 46), BackColor = Color.FromArgb(220, 252, 231) };
            circleCheck.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, circleCheck.Width - 1, circleCheck.Height - 1);
                circleCheck.Region = new Region(path);

                using Font font = new Font("Segoe UI", 16F, FontStyle.Bold);
                using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString("✓", font, new SolidBrush(Color.FromArgb(16, 185, 129)), new RectangleF(0, 0, circleCheck.Width, circleCheck.Height), sf);
            };

            Label lblTitle = new Label { Text = "PRE-VENTA REGISTRADA", Location = new Point(78, 18), Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(6, 78, 59), AutoSize = true };
            Label lblSubTitle = new Label { Text = "Ticket impreso y generado correctamente", Location = new Point(78, 42), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };

            // 1. Tarjeta Número de Ticket
            Panel pnlTicketCard = CrearTarjetaRedondeada(20, 75, 400, 68, Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231));
            Panel pnlIconTicket = new Panel { Location = new Point(14, 14), Size = new Size(38, 38), BackColor = Color.Transparent };
            pnlIconTicket.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using Pen p = new Pen(Color.FromArgb(16, 185, 129), 2f);
                e.Graphics.DrawRectangle(p, 4, 8, 28, 20);
                e.Graphics.DrawLine(p, 14, 8, 14, 28);
            };

            Label lblT1 = new Label { Text = "Ticket de atención", Location = new Point(60, 10), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };
            Label lblTicketNum = new Label { Text = $"#{nroTicket:D6}", Location = new Point(60, 26), Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(6, 78, 59), AutoSize = true };
            pnlTicketCard.Controls.AddRange(new Control[] { pnlIconTicket, lblT1, lblTicketNum });

            // 2. Tarjeta Datos
            Panel pnlDetallesCard = CrearTarjetaRedondeada(20, 152, 400, 120, Color.White, Color.FromArgb(226, 232, 240));
            int yFila = 12;
            CrearFilaDetalle(pnlDetallesCard, "Vendedor:", vendedor, ref yFila);
            CrearFilaDetalle(pnlDetallesCard, "Cliente:", string.IsNullOrWhiteSpace(cliente) ? "Consumidor Final" : cliente, ref yFila);
            CrearFilaDetalle(pnlDetallesCard, "Total a pagar en Caja:", $"${total:N0}", ref yFila, esBold: true, colorVal: Color.FromArgb(6, 78, 59));

            // 3. Tarjeta Indicación Azul
            Panel pnlInstrucCard = CrearTarjetaRedondeada(20, 282, 400, 52, Color.FromArgb(239, 246, 255), Color.FromArgb(191, 219, 254));
            Panel pnlIconInfo = new Panel { Location = new Point(12, 10), Size = new Size(30, 30), BackColor = Color.Transparent };
            pnlIconInfo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using SolidBrush b = new SolidBrush(Color.FromArgb(37, 99, 235));
                e.Graphics.FillEllipse(b, 4, 4, 20, 20);
                using Font font = new Font("Segoe UI", 10F, FontStyle.Bold);
                using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString("i", font, Brushes.White, new RectangleF(4, 4, 20, 20), sf);
            };

            Label lblInstrucText = new Label { Text = $"Entregue el ticket impreso al cliente para pagar en CAJA\ncon el número #{nroTicket:D6}.", Location = new Point(48, 8), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 58, 138), AutoSize = true };
            pnlInstrucCard.Controls.AddRange(new Control[] { pnlIconInfo, lblInstrucText });

            // 4. BOTONES: REIMPRIMIR Y ENTENDIDO
            Button btnReimprimir = new Button
            {
                Text = "🖨️  REIMPRIMIR",
                Location = new Point(20, 348),
                Size = new Size(195, 44),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReimprimir.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnReimprimir.Click += (s, e) =>
            {
                _printService.ImprimirTicketAtencion(nroTicket, vendedor, cliente, total, items);
            };

            Button btnEntendido = new Button
            {
                Text = "✓  ENTENDIDO",
                Location = new Point(225, 348),
                Size = new Size(195, 44),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEntendido.FlatAppearance.BorderSize = 0;
            btnEntendido.Click += (s, e) => this.Close();

            pnlMain.Controls.AddRange(new Control[] {
                circleCheck, lblTitle, lblSubTitle,
                pnlTicketCard, pnlDetallesCard, pnlInstrucCard, btnReimprimir, btnEntendido
            });

            this.Controls.Add(pnlMain);
        }

        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(colorBorde, 1.5f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
        }

        private void CrearFilaDetalle(Panel pnl, string titulo, string valor, ref int y, bool esBold = false, Color? colorVal = null)
        {
            Label lblT = new Label { Text = titulo, Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139) };
            Label lblV = new Label { Text = valor, Location = new Point(180, y), Size = new Size(205, 20), Font = new Font("Segoe UI", 9F, esBold ? FontStyle.Bold : FontStyle.Regular), ForeColor = colorVal ?? Color.FromArgb(15, 23, 42), TextAlign = ContentAlignment.TopRight };

            pnl.Controls.AddRange(new Control[] { lblT, lblV });
            y += 32;
        }

        private GraphicsPath CrearRutaRedondeada(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}