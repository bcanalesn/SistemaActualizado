using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormResumenTurnoModal : Form
    {
        private readonly CajaService _cajaService = new CajaService();
        private readonly CajaTurno _turno;

        public FormResumenTurnoModal(CajaTurno turno)
        {
            _turno = turno ?? throw new ArgumentNullException(nameof(turno));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Resumen del Turno";
            this.Size = new Size(390, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.BackColor = Color.White;

            var datos = _cajaService.ObtenerMetricasResumenTurno(_turno.FechaApertura);

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            // Encabezado
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(335, 10),
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            Label lblTitle = new Label { Text = "📈 Resumen del Turno", Location = new Point(12, 10), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            Label lblSubTitle = new Label { Text = "Turno #" + _turno.FechaApertura.ToString("yyyy-MM-dd") + "-" + _turno.CajaTurnoID.ToString("D3"), Location = new Point(12, 34), Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };

            Panel badgeEstado = new Panel { Location = new Point(270, 34), Size = new Size(60, 20), BackColor = Color.FromArgb(220, 252, 231) };
            Label lblEstadoText = new Label { Text = "ABIERTO", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 101, 52), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            badgeEstado.Controls.Add(lblEstadoText);

            // 1. TARJETA BALANCE
            Panel pnlBalance = CrearPanelTarjeta(12, 60, 350, 185);

            decimal fondoInicial = _turno.MontoInicial;
            decimal ventasTotales = datos.VentasTotales;
            decimal anulaciones = datos.MontoAnulaciones;
            decimal totalIngresos = ventasTotales - anulaciones;
            decimal efectivoEsperado = fondoInicial + datos.VentasEfectivo;

            int yPos = 8;
            CrearFilaMetrica(pnlBalance, "Fondo Inicial", "$ " + fondoInicial.ToString("N0"), ref yPos, isBold: true);
            CrearFilaMetrica(pnlBalance, "Ventas Totales (" + datos.CantVentas + ")", "$ " + ventasTotales.ToString("N0"), ref yPos);
            CrearFilaMetrica(pnlBalance, "Anulaciones (" + datos.CantAnulaciones + ")", "$ " + anulaciones.ToString("N0"), ref yPos, colorValor: Color.FromArgb(239, 68, 68));
            CrearFilaMetrica(pnlBalance, "Total Ingresos", "$ " + totalIngresos.ToString("N0"), ref yPos, isBold: true, colorValor: Color.FromArgb(22, 163, 74));
            
            yPos += 4;
            CrearFilaMetrica(pnlBalance, "Efectivo Esperado", "$ " + efectivoEsperado.ToString("N0"), ref yPos, isBold: true, colorValor: Color.FromArgb(37, 99, 235));
            CrearFilaMetrica(pnlBalance, "Efectivo en Caja", "$ " + efectivoEsperado.ToString("N0"), ref yPos, isBold: true, colorValor: Color.FromArgb(22, 163, 74));

            // Diferencia
            Panel pnlDiferencia = new Panel { Location = new Point(12, 250), Size = new Size(350, 32), BackColor = Color.FromArgb(240, 253, 244) };
            pnlDiferencia.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlDiferencia.ClientRectangle, Color.FromArgb(187, 247, 208), ButtonBorderStyle.Solid);
            
            Label lblDifT = new Label { Text = "Diferencia", Location = new Point(10, 7), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 101, 52), AutoSize = true };
            Label lblDifV = new Label { Text = "$ 0", Location = new Point(275, 6), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), AutoSize = true };
            pnlDiferencia.Controls.AddRange(new Control[] { lblDifT, lblDifV });

            // 2. VENTAS POR MEDIO DE PAGO (DINÁMICO)
            int altoMedios = 35 + (datos.ListaDesglose.Count * 22) + 20;
            Panel pnlMediosPago = CrearPanelTarjeta(12, 288, 350, Math.Max(altoMedios, 110));
            Label lblTitMedios = new Label { Text = "VENTAS POR MEDIO DE PAGO", Location = new Point(10, 8), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };

            // Donut Chart Dintáimico
            Panel pnlChart = new Panel { Location = new Point(12, 28), Size = new Size(68, 68), BackColor = Color.White };
            pnlChart.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using Pen penDonut = new Pen(Color.FromArgb(16, 185, 129), 10);
                g.DrawEllipse(penDonut, 6, 6, 54, 54);

                using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using Font fontPercent = new Font("Segoe UI", 7.5F, FontStyle.Bold);
                g.DrawString("100%", fontPercent, Brushes.Black, new RectangleF(0, 0, 68, 68), sf);
            };

            pnlMediosPago.Controls.Add(lblTitMedios);
            pnlMediosPago.Controls.Add(pnlChart);

            int yMedio = 28;
           foreach (var item in datos.ListaDesglose)
        {
            Color colorTag = item.Medio == "Efectivo" ? Color.FromArgb(16, 185, 129) :
                            item.Medio == "Débito" ? Color.FromArgb(37, 99, 235) :
                            item.Medio == "Crédito" ? Color.FromArgb(124, 58, 237) : 
                            Color.FromArgb(13, 148, 136); // Transferencia

            Panel pnlB = new Panel { Location = new Point(95, yMedio + 4), Size = new Size(8, 8), BackColor = colorTag };
            Label lblT = new Label { Text = item.Medio, Location = new Point(108, yMedio), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            Label lblV = new Label { Text = "$ " + item.Monto.ToString("N0"), Location = new Point(200, yMedio), Size = new Size(80, 16), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), TextAlign = ContentAlignment.TopRight };
            Label lblP = new Label { Text = item.Porcentaje.ToString("N0") + "%", Location = new Point(290, yMedio), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };

            pnlMediosPago.Controls.AddRange(new Control[] { pnlB, lblT, lblV, lblP });
            yMedio += 20;
        }

            Label lblTotalTag = new Label { Text = "Total", Location = new Point(108, yMedio + 2), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            Label lblTotalVal = new Label { Text = "$ " + ventasTotales.ToString("N0"), Location = new Point(200, yMedio + 2), Size = new Size(80, 16), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), TextAlign = ContentAlignment.TopRight };
            pnlMediosPago.Controls.AddRange(new Control[] { lblTotalTag, lblTotalVal });

            // 3. DETALLE RÁPIDO
            Panel pnlDetalleRapido = CrearPanelTarjeta(12, 288 + Math.Max(altoMedios, 110) + 8, 350, 95);
            Label lblTitDet = new Label { Text = "DETALLE RÁPIDO", Location = new Point(10, 6), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };

            int yDet = 24;
            CrearFilaIconoDetalle(pnlDetalleRapido, "🏷️", "Tickets emitidos", datos.CantVentas.ToString(), ref yDet);
            CrearFilaIconoDetalle(pnlDetalleRapido, "🛒", "Productos vendidos", datos.CantProductos.ToString(), ref yDet);
            
            TimeSpan duracion = DateTime.Now - _turno.FechaApertura;
            CrearFilaIconoDetalle(pnlDetalleRapido, "🕒", "Duración del turno", string.Format("{0:D2}:{1:D2}:{2:D2}", duracion.Hours, duracion.Minutes, duracion.Seconds), ref yDet);

            pnlDetalleRapido.Controls.Add(lblTitDet);

            pnlMain.Controls.AddRange(new Control[] {
                btnClose, lblTitle, lblSubTitle, badgeEstado,
                pnlBalance, pnlDiferencia, pnlMediosPago, pnlDetalleRapido
            });

            this.Controls.Add(pnlMain);
        }

        private Panel CrearPanelTarjeta(int x, int y, int ancho, int alto)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = Color.White };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            return pnl;
        }

        private void CrearFilaMetrica(Panel pnl, string nombre, string valor, ref int y, bool isBold = false, Color? colorValor = null)
        {
            Label lblN = new Label
            {
                Text = nombre,
                Location = new Point(10, y),
                Font = new Font("Segoe UI", 8F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true
            };

            Label lblV = new Label
            {
                Text = valor,
                Location = new Point(230, y),
                Size = new Size(110, 18),
                Font = new Font("Segoe UI", 8F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = colorValor ?? Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.TopRight
            };

            pnl.Controls.Add(lblN);
            pnl.Controls.Add(lblV);
            y += 22;
        }

        private void CrearFilaIconoDetalle(Panel pnl, string icono, string titulo, string valor, ref int y)
        {
            Label lblI = new Label { Text = icono, Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            Label lblT = new Label { Text = titulo, Location = new Point(32, y), AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
            Label lblV = new Label { Text = valor, Location = new Point(230, y), Size = new Size(110, 16), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), TextAlign = ContentAlignment.TopRight };

            pnl.Controls.AddRange(new Control[] { lblI, lblT, lblV });
            y += 20;
        }
    }
}