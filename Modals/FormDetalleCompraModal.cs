using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormDetalleCompraModal : Form
    {
        private readonly CompraService _compraService = new CompraService();
        private readonly int _compraId;

        public FormDetalleCompraModal(int compraId)
        {
            _compraId = compraId;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarDetalles();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Detalle de Factura de Compra";
            this.Size = new Size(860, 570);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMainModal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            this.Controls.Add(pnlMainModal);
            this.ResumeLayout(false);
        }

        private void CargarDetalles()
        {
            try
            {
                var compra = _compraService.ObtenerCompraPorId(_compraId);
                if (compra == null) return;

                var detalles = _compraService.ObtenerDetallesCompra(_compraId);

                this.Text = $"Detalle de Compra — Folio N° {compra.NroFacturaProveedor} ({compra.TipoDocumento})";

                Panel pnlMainModal = (Panel)this.Controls[0];
                pnlMainModal.Controls.Clear();

                // =========================================================================
                // 1. INFORMACIÓN DE PROVEEDOR Y DOCUMENTO EN TARJETA REDONDEADA
                // =========================================================================
                Panel pnlInfoWrapper = new Panel 
                { 
                    Dock = DockStyle.Top, 
                    Height = 74, 
                    Padding = new Padding(0, 0, 0, 10), 
                    BackColor = Color.Transparent 
                };

                Panel pnlInfoCard = CrearTarjetaRedondeada(0, 0, 0, 64, Color.White, Color.FromArgb(226, 232, 240));
                pnlInfoCard.Dock = DockStyle.Fill;
                pnlInfoCard.Padding = new Padding(14, 10, 14, 10);

                Label lblProv = new Label 
                { 
                    Text = $"PROVEEDOR: {compra.RazonSocialProveedor} (RUT: {compra.RutProveedor})", 
                    Dock = DockStyle.Top, 
                    AutoSize = true, 
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(15, 23, 42) 
                };

                Label lblFch = new Label 
                { 
                    Text = $"Fecha Emisión: {compra.FechaEmision:dd-MM-yyyy}   •   Tipo Origen: {(compra.TipoCompra == "MERCADERIA" ? "Mercadería para Venta (Afectó Stock)" : "Gasto / Insumo Interno")}", 
                    Dock = DockStyle.Bottom, 
                    AutoSize = true, 
                    Font = new Font("Segoe UI", 8.5F), 
                    ForeColor = Color.FromArgb(100, 116, 139) 
                };

                pnlInfoCard.Controls.AddRange(new Control[] { lblProv, lblFch });
                pnlInfoWrapper.Controls.Add(pnlInfoCard);

                // =========================================================================
                // 2. BARRA INFERIOR DE TOTALES CON ALINEACIÓN CENTRADA VERTICALMENTE
                // =========================================================================
                Panel pnlBottomWrapper = new Panel 
                { 
                    Dock = DockStyle.Bottom, 
                    Height = 60, 
                    Padding = new Padding(0, 10, 0, 0), 
                    BackColor = Color.Transparent 
                };

                Panel pnlTotalesCard = CrearTarjetaRedondeada(0, 0, 0, 50, Color.FromArgb(240, 253, 244), Color.FromArgb(187, 247, 208));
                pnlTotalesCard.Dock = DockStyle.Fill;
                pnlTotalesCard.Padding = new Padding(16, 0, 16, 0);

                TableLayoutPanel tlpTotales = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent
                };
                tlpTotales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
                tlpTotales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

                Label lblSubtotales = new Label
                {
                    Text = $"Neto: {MonedaHelper.Formatear(compra.MontoNeto, conSigno: true)}   |   IVA (19%): {MonedaHelper.Formatear(compra.MontoIva, conSigno: true)}",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false
                };

                Label lblTotal = new Label
                {
                    Text = $"TOTAL: {MonedaHelper.Formatear(compra.MontoTotal, conSigno: true)}",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(22, 163, 74),
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false
                };

                tlpTotales.Controls.Add(lblSubtotales, 0, 0);
                tlpTotales.Controls.Add(lblTotal, 1, 0);
                pnlTotalesCard.Controls.Add(tlpTotales);
                pnlBottomWrapper.Controls.Add(pnlTotalesCard);

                // =========================================================================
                // 3. GRILLA PRINCIPAL EN TARJETA BLANCA REDONDEADA
                // =========================================================================
                Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
                pnlGridCard.Dock = DockStyle.Fill;
                pnlGridCard.Padding = new Padding(10);

                DataGridView dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowTemplate = { Height = 34 }
                };

                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersHeight = 36;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
                dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
                dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
                dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

                dgv.Columns.Clear();
                dgv.Columns.Add("Item", "DESCRIPCIÓN DEL ÍTEM / PRODUCTO");
                dgv.Columns.Add("Cant", "CANTIDAD");
                dgv.Columns.Add("Costo", "COSTO NETO UNIT.");
                dgv.Columns.Add("Subtotal", "SUBTOTAL NETO");

                var estiloMonedaCentrado = new DataGridViewCellStyle 
                { 
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"), 
                    Format = "$ #,##0", 
                    Alignment = DataGridViewContentAlignment.MiddleCenter 
                };

                // 1. Descripción: alineada a la izquierda (tanto título como contenido)
                dgv.Columns["Item"].FillWeight = 46;
                dgv.Columns["Item"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgv.Columns["Item"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                // 2. Cantidad: centrada vertical y horizontalmente
                dgv.Columns["Cant"].FillWeight = 14;
                dgv.Columns["Cant"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.Columns["Cant"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.Columns["Cant"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                // 3. Costo Neto: centrado con su encabezado
                dgv.Columns["Costo"].FillWeight = 20;
                dgv.Columns["Costo"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.Columns["Costo"].DefaultCellStyle = estiloMonedaCentrado;

                // 4. Subtotal: centrado con su encabezado
                dgv.Columns["Subtotal"].FillWeight = 20;
                dgv.Columns["Subtotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.Columns["Subtotal"].DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"), 
                    Format = "$ #,##0", 
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
                };

                foreach (var d in detalles)
                {
                    dgv.Rows.Add(d.NombreProducto, d.Cantidad, d.PrecioCostoUnitario, d.Subtotal);
                }

                pnlGridCard.Controls.Add(dgv);

                // Orden Z estricto
                pnlMainModal.Controls.Add(pnlGridCard);
                pnlMainModal.Controls.Add(pnlBottomWrapper);
                pnlMainModal.Controls.Add(pnlInfoWrapper);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // MÉTODOS AUXILIARES: TARJETAS SUAVES Y BORDES REDONDEADOS
        // =========================================================================
        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Resize += (s, e) => pnl.Invalidate();
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.BackColor);
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 10);
                using Pen pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
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