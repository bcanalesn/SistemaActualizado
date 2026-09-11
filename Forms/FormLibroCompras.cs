using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormLibroCompras : Form
    {
        private readonly LibroContableService _libroService = new LibroContableService();
        private readonly ProveedorService _proveedorService = new ProveedorService();
        private readonly ProductoService _productoService = new ProductoService();

        // Controles de Filtros
        private TextBox txtFiltroTexto = null!;
        private DateTimePicker dtpFechaDesde = null!;
        private DateTimePicker dtpFechaHasta = null!;
        private ComboBox cbTipoDoc = null!;
        private ComboBox cbEstado = null!;
        private Button btnBuscar = null!;
        private Button btnLimpiar = null!;
        private Button btnExportarCsv = null!;
        private Button btnExportarPdf = null!;

        // Filtros Rápidos
        private Button btnFiltroHoy = null!;
        private Button btnFiltroAyer = null!;
        private Button btnFiltro7Dias = null!;
        private Button btnFiltroEsteMes = null!;
        private Button btnFiltroMesAnterior = null!;

        // Tarjetas KPI Superiores
        private Label lblKpiTotalBrutoMonto = null!;
        private Label lblKpiComprasNetoMonto = null!;
        private Label lblKpiFacturasCant = null!;
        private Label lblKpiIvaMonto = null!;
        private Label lblKpiProveedoresCant = null!;

        // Tabla Principal
        private DataGridView dgvLibro = null!;
        private Label lblContadorFooter = null!;

        private List<Compra> _comprasCargadas = new List<Compra>();
        private int _filaImpresionActual = 0;
        private int _nroPaginaPdf = 1;

        public FormLibroCompras()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            ConfigurarFiltroEsteMes();
            CargarDatosLibro();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            // =========================================================================
            // 1. KPIS SUPERIORES (92px idéntico a Libro de Ventas)
            // =========================================================================
            Panel pnlKpisWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            var card1 = CrearCardKPI("💰", Color.FromArgb(220, 252, 231), "Total Compras", out lblKpiTotalBrutoMonto, "$ 0", "Total bruto (con IVA)");
            var card2 = CrearCardKPI("💲", Color.FromArgb(240, 253, 244), "Total Neto", out lblKpiComprasNetoMonto, "$ 0", "Total neto del período");
            var card3 = CrearCardKPI("📄", Color.FromArgb(239, 246, 255), "Facturas Recibidas", out lblKpiFacturasCant, "0", "100% del total");
            var card4 = CrearCardKPI("🏷️", Color.FromArgb(243, 232, 255), "Total IVA", out lblKpiIvaMonto, "$ 0", "Total IVA recuperable");
            var card5 = CrearCardKPI("👥", Color.FromArgb(254, 243, 199), "Proveedores Activos", out lblKpiProveedoresCant, "0", "Con compras en el período");

            card1.Margin = new Padding(0, 0, 5, 0);
            card2.Margin = new Padding(5, 0, 5, 0);
            card3.Margin = new Padding(5, 0, 5, 0);
            card4.Margin = new Padding(5, 0, 5, 0);
            card5.Margin = new Padding(5, 0, 0, 0);

            tlpKpis.Controls.Add(card1, 0, 0);
            tlpKpis.Controls.Add(card2, 1, 0);
            tlpKpis.Controls.Add(card3, 2, 0);
            tlpKpis.Controls.Add(card4, 3, 0);
            tlpKpis.Controls.Add(card5, 4, 0);

            tlpKpis.Resize += (s, e) => tlpKpis.Invalidate(true);
            pnlKpisWrapper.Controls.Add(tlpKpis);

            // =========================================================================
            // 2. SECCIÓN FILTROS Y ACCIONES
            // =========================================================================
            Panel pnlFiltrosWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                Padding = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 115, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 8, 16, 8);

            FlowLayoutPanel flowFiltrosFila1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            txtFiltroTexto = new TextBox
            {
                Size = new Size(180, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "Proveedor, folio o RUT..."
            };
            txtFiltroTexto.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarDatosLibro(); e.SuppressKeyPress = true; } };
            Panel pnlTxt = CrearGrupoConCaja("PROVEEDOR / FOLIO / RUT", txtFiltroTexto, 195);

            dtpFechaDesde = new DateTimePicker { Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlDesde = CrearGrupoLimpio("FECHA DESDE", dtpFechaDesde, 125);

            dtpFechaHasta = new DateTimePicker { Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlHasta = CrearGrupoLimpio("FECHA HASTA", dtpFechaHasta, 125);

            cbTipoDoc = new ComboBox { Size = new Size(155, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbTipoDoc.Items.AddRange(new string[] { "Todos", "Factura de Compra", "Factura Exenta", "Boleta de Compra", "Guía de Recepción" });
            cbTipoDoc.SelectedIndex = 0;
            Panel pnlTipo = CrearGrupoLimpio("TIPO DOCUMENTO", cbTipoDoc, 160);

            cbEstado = new ComboBox { Size = new Size(110, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbEstado.Items.AddRange(new string[] { "Todos", "Recibida", "Anulada" });
            cbEstado.SelectedIndex = 0;
            Panel pnlEstado = CrearGrupoLimpio("ESTADO", cbEstado, 115);

            btnBuscar = CrearBoton("🔍  Buscar", Color.FromArgb(190, 24, 93), Color.White, new Size(95, 34), 8);
            btnBuscar.Margin = new Padding(8, 14, 6, 0);
            btnBuscar.Click += (s, e) => CargarDatosLibro();

            btnLimpiar = CrearBoton("🗑️  Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(90, 34), 8);
            btnLimpiar.Margin = new Padding(0, 14, 0, 0);
            btnLimpiar.Click += BtnLimpiar_Click;

            flowFiltrosFila1.Controls.AddRange(new Control[] { pnlTxt, pnlDesde, pnlHasta, pnlTipo, pnlEstado, btnBuscar, btnLimpiar });

            Panel pnlFila2Acciones = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            FlowLayoutPanel flowRapidos = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            Label lblFRap = new Label
            {
                Text = "Filtros rápidos:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Margin = new Padding(0, 6, 8, 0),
                AutoSize = true
            };

            btnFiltroHoy = CrearPillBoton("📅 Hoy", 65, (s, e) => { dtpFechaDesde.Value = DateTime.Today; dtpFechaHasta.Value = DateTime.Today; CargarDatosLibro(); });
            btnFiltroAyer = CrearPillBoton("📅 Ayer", 65, (s, e) => { dtpFechaDesde.Value = DateTime.Today.AddDays(-1); dtpFechaHasta.Value = DateTime.Today.AddDays(-1); CargarDatosLibro(); });
            btnFiltro7Dias = CrearPillBoton("📅 Últimos 7 días", 115, (s, e) => { dtpFechaDesde.Value = DateTime.Today.AddDays(-7); dtpFechaHasta.Value = DateTime.Today; CargarDatosLibro(); });
            btnFiltroEsteMes = CrearPillBoton("📅 Este mes", 85, (s, e) => { ConfigurarFiltroEsteMes(); CargarDatosLibro(); });
            btnFiltroMesAnterior = CrearPillBoton("📅 Mes anterior", 105, (s, e) => { ConfigurarFiltroMesAnterior(); CargarDatosLibro(); });

            flowRapidos.Controls.AddRange(new Control[] { lblFRap, btnFiltroHoy, btnFiltroAyer, btnFiltro7Dias, btnFiltroEsteMes, btnFiltroMesAnterior });

            FlowLayoutPanel flowExport = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnExportarPdf = CrearBoton("📄 Exportar PDF", Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38), new Size(125, 28), 6);
            btnExportarPdf.Margin = new Padding(6, 2, 0, 0);
            btnExportarPdf.Click += (s, e) => GenerarReportePdf();

            btnExportarCsv = CrearBoton("📥 Exportar CSV", Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), new Size(125, 28), 6);
            btnExportarCsv.Margin = new Padding(0, 2, 0, 0);
            btnExportarCsv.Click += (s, e) => ExportarACSV();

            flowExport.Controls.AddRange(new Control[] { btnExportarPdf, btnExportarCsv });

            pnlFila2Acciones.Controls.Add(flowExport);
            pnlFila2Acciones.Controls.Add(flowRapidos);

            pnlFiltrosCard.Controls.Add(pnlFila2Acciones);
            pnlFiltrosCard.Controls.Add(flowFiltrosFila1);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // =========================================================================
            // 3. TARJETA DE GRILLA REDONDEADA
            // =========================================================================
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

            dgvLibro = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowTemplate = { Height = 34 }
            };
            dgvLibro.DoubleClick += DgvLibro_DoubleClick;
            ConfigurarColumnasTabla();

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 documentos",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlGridCard.Controls.Add(dgvLibro);
            pnlGridCard.Controls.Add(pnlFooter);

            // =========================================================================
            // APILAMIENTO EN ORDEN TOP CORRECTO (Z-INDEX)
            // =========================================================================
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlFiltrosWrapper);
            pnlMain.Controls.Add(pnlKpisWrapper);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearCardKPI(string icon, Color iconBg, string titulo, out Label lblValor, string valInit, string subInit)
        {
            Panel pnl = CrearTarjetaRedondeada(0, 0, 0, 80, Color.White, Color.FromArgb(226, 232, 240));
            pnl.Dock = DockStyle.Fill;
            pnl.Padding = new Padding(12);

            Label lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 12F), BackColor = iconBg, Size = new Size(36, 36), TextAlign = ContentAlignment.MiddleCenter, Location = new Point(12, 12) };
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(56, 10), AutoSize = true };
            lblValor = new Label { Text = valInit, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(56, 26), AutoSize = true };
            Label lblSub = new Label { Text = subInit, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(56, 52), AutoSize = true };

            pnl.Controls.AddRange(new Control[] { lblIcon, lblT, lblValor, lblSub });
            return pnl;
        }

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

        private Panel CrearGrupoConCaja(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            Panel pnlInputBox = new Panel { Location = new Point(0, 18), Size = new Size(ancho, 28), BackColor = Color.FromArgb(248, 250, 252) };
            pnlInputBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlInputBox.Width - 1, pnlInputBox.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(r, 6);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            control.Location = new Point(6, 3);
            control.Width = ancho - 12;
            pnlInputBox.Controls.Add(control);

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(pnlInputBox);
            return pnl;
        }

        private Panel CrearGrupoLimpio(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            control.Location = new Point(0, 18);
            control.Width = ancho;

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private Button CrearBoton(string texto, Color back, Color fore, Size size, int radio = 0)
        {
            Button b = new Button 
            { 
                Text = texto, 
                Size = size, 
                BackColor = back, 
                ForeColor = fore, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Button CrearPillBoton(string texto, int ancho, EventHandler onClick)
        {
            Button b = CrearBoton(texto, Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105), new Size(ancho, 25), 12);
            b.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            b.Margin = new Padding(0, 2, 6, 0);
            b.Click += onClick;
            return b;
        }

        private void ConfigurarColumnasTabla()
        {
            dgvLibro.EnableHeadersVisualStyles = false;
            dgvLibro.ColumnHeadersHeight = 36;
            dgvLibro.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvLibro.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvLibro.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvLibro.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvLibro.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvLibro.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvLibro.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvLibro.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

            dgvLibro.Columns.Clear();
            dgvLibro.Columns.Add("TipoDoc", "TIPO DTE");
            dgvLibro.Columns.Add("Folio", "N° FOLIO");
            dgvLibro.Columns.Add("Fecha", "FECHA EMISIÓN");
            dgvLibro.Columns.Add("Proveedor", "PROVEEDOR / RAZÓN SOCIAL");
            dgvLibro.Columns.Add("Rut", "RUT");
            dgvLibro.Columns.Add("Neto", "NETO");
            dgvLibro.Columns.Add("Iva", "IVA (19%)");
            dgvLibro.Columns.Add("Total", "TOTAL");
            dgvLibro.Columns.Add("Estado", "ESTADO");

            dgvLibro.Columns["TipoDoc"].Width = 85;
            dgvLibro.Columns["TipoDoc"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvLibro.Columns["TipoDoc"].DefaultCellStyle.ForeColor = Color.FromArgb(37, 99, 235);
            dgvLibro.Columns["TipoDoc"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvLibro.Columns["Folio"].Width = 100;
            dgvLibro.Columns["Folio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvLibro.Columns["Folio"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvLibro.Columns["Fecha"].Width = 115;
            dgvLibro.Columns["Fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvLibro.Columns["Proveedor"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvLibro.Columns["Proveedor"].MinimumWidth = 220;

            dgvLibro.Columns["Rut"].Width = 120;
            dgvLibro.Columns["Neto"].Width = 120;
            dgvLibro.Columns["Neto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvLibro.Columns["Neto"].DefaultCellStyle.Format = "$#,##0";

            dgvLibro.Columns["Iva"].Width = 110;
            dgvLibro.Columns["Iva"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvLibro.Columns["Iva"].DefaultCellStyle.Format = "$#,##0";

            dgvLibro.Columns["Total"].Width = 130;
            dgvLibro.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvLibro.Columns["Total"].DefaultCellStyle.Format = "$#,##0";
            dgvLibro.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvLibro.Columns["Total"].DefaultCellStyle.ForeColor = Color.FromArgb(22, 163, 74);

            dgvLibro.Columns["Estado"].Width = 100;
            dgvLibro.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void CargarDatosLibro()
        {
            try
            {
                DateTime fDesde = dtpFechaDesde.Value.Date;
                DateTime fHasta = dtpFechaHasta.Value.Date.AddDays(1).AddTicks(-1);

                string filtro = txtFiltroTexto.Text.Trim();
                string tipoSel = cbTipoDoc.SelectedIndex > 0 ? cbTipoDoc.SelectedItem!.ToString()! : "Todos";
                string estadoSel = cbEstado.SelectedIndex > 0 ? cbEstado.SelectedItem!.ToString()! : "Todos";

                var resumen = _libroService.ObtenerLibroCompras(fDesde, fHasta, filtro, tipoSel, estadoSel);
                _comprasCargadas = resumen.Compras;

                dgvLibro.Rows.Clear();
                foreach (var c in _comprasCargadas)
                {
                    string codigoSii = ObtenerCodigoDte(c.TipoDocumento);
                    dgvLibro.Rows.Add(
                        codigoSii,
                        c.NroFacturaProveedor,
                        c.FechaEmision.ToString("dd-MM-yyyy"),
                        c.RazonSocialProveedor,
                        c.RutProveedor,
                        c.MontoNeto,
                        c.MontoIva,
                        c.MontoTotal,
                        c.Estado
                    );
                }

                lblKpiTotalBrutoMonto.Text = $"$ {resumen.TotalBruto:N0}";
                lblKpiComprasNetoMonto.Text = $"$ {resumen.TotalNeto:N0}";
                lblKpiFacturasCant.Text = resumen.TotalFacturas.ToString("N0");
                lblKpiIvaMonto.Text = $"$ {resumen.TotalIva:N0}";
                lblKpiProveedoresCant.Text = resumen.TotalProveedores.ToString("N0");

                lblContadorFooter.Text = $"Mostrando {_comprasCargadas.Count} documento(s) en el período seleccionado.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el Libro de Compras: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ObtenerCodigoDte(string? tipoDoc)
        {
            if (string.IsNullOrEmpty(tipoDoc)) return "33";
            string t = tipoDoc.ToLower();
            if (t.Contains("exenta")) return "34";
            if (t.Contains("boleta")) return "39";
            if (t.Contains("guía") || t.Contains("guia")) return "52";
            return "33";
        }

        private void ConfigurarFiltroEsteMes()
        {
            DateTime hoy = DateTime.Today;
            dtpFechaDesde.Value = new DateTime(hoy.Year, hoy.Month, 1);
            dtpFechaHasta.Value = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
        }

        private void ConfigurarFiltroMesAnterior()
        {
            DateTime mesPasado = DateTime.Today.AddMonths(-1);
            dtpFechaDesde.Value = new DateTime(mesPasado.Year, mesPasado.Month, 1);
            dtpFechaHasta.Value = new DateTime(mesPasado.Year, mesPasado.Month, DateTime.DaysInMonth(mesPasado.Year, mesPasado.Month));
        }

        private void BtnLimpiar_Click(object? sender, EventArgs e)
        {
            txtFiltroTexto.Clear();
            cbTipoDoc.SelectedIndex = 0;
            cbEstado.SelectedIndex = 0;
            ConfigurarFiltroEsteMes();
            CargarDatosLibro();
        }

        private void ExportarACSV()
        {
            if (_comprasCargadas.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar en el período actual.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Libro_Compras_{DateTime.Today:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("TIPO_DTE;FOLIO;FECHA_EMISION;RUT_PROVEEDOR;RAZON_SOCIAL;MONTO_NETO;MONTO_IVA;MONTO_TOTAL;ESTADO");

                    foreach (var c in _comprasCargadas)
                    {
                        sb.AppendLine($"{ObtenerCodigoDte(c.TipoDocumento)};{c.NroFacturaProveedor};{c.FechaEmision:dd-MM-yyyy};{c.RutProveedor};\"{c.RazonSocialProveedor}\";{c.MontoNeto};{c.MontoIva};{c.MontoTotal};{c.Estado}");
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Libro de Compras exportado exitosamente en formato CSV.", "Exportación Completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar CSV: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void GenerarReportePdf()
        {
            if (_comprasCargadas.Count == 0)
            {
                MessageBox.Show("No hay registros para generar el informe PDF en este período.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_Libro_Compras_{dtpFechaDesde.Value:yyyyMMdd}_{dtpFechaHasta.Value:yyyyMMdd}.pdf"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _filaImpresionActual = 0;
                    _nroPaginaPdf = 1;

                    PrintDocument doc = new PrintDocument();
                    doc.DefaultPageSettings.Landscape = true;
                    doc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
                    doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    doc.PrinterSettings.PrintToFile = true;
                    doc.PrinterSettings.PrintFileName = sfd.FileName;

                    doc.PrintPage += Doc_PrintPage;
                    doc.Print();

                    var res = MessageBox.Show("Reporte PDF generado exitosamente.\n\n¿Desea abrir el archivo generado ahora?", "Exportación PDF", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (res == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics!;
            int left = e.MarginBounds.Left;
            int right = e.MarginBounds.Right;
            int top = e.MarginBounds.Top;
            int width = e.MarginBounds.Width;
            int y = top;

            if (_nroPaginaPdf == 1)
            {
                using Font fontTit = new Font("Segoe UI", 16, FontStyle.Bold);
                using Font fontSub = new Font("Segoe UI", 9, FontStyle.Regular);
                using Brush brushTit = new SolidBrush(Color.FromArgb(15, 23, 42));
                using Brush brushSub = new SolidBrush(Color.FromArgb(100, 116, 139));

                g.DrawString("SISTEMA POS MODERNO - LIBRO DE COMPRAS", fontTit, brushTit, left, y);
                y += 24;

                string rango = $"Período: {dtpFechaDesde.Value:dd/MM/yyyy} al {dtpFechaHasta.Value:dd/MM/yyyy}  |  Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}";
                g.DrawString(rango, fontSub, brushSub, left, y);
                y += 22;

                decimal totalBruto = _comprasCargadas.Sum(c => c.MontoTotal);
                decimal netoTot = _comprasCargadas.Sum(c => c.MontoNeto);
                decimal ivaTot = _comprasCargadas.Sum(c => c.MontoIva);
                int cantDoc = _comprasCargadas.Count;
                int provs = _comprasCargadas.Select(c => c.RutProveedor).Distinct().Count();

                Rectangle kpiRect = new Rectangle(left, y, width, 32);
                using Brush bgKpi = new SolidBrush(Color.FromArgb(241, 245, 249));
                using Pen penKpi = new Pen(Color.FromArgb(203, 213, 225));
                g.FillRectangle(bgKpi, kpiRect);
                g.DrawRectangle(penKpi, kpiRect);

                using Font fontKpi = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                using Brush brushKpi = new SolidBrush(Color.FromArgb(30, 41, 59));
                string resumenTexto = $"Documentos: {cantDoc}   |   Proveedores: {provs}   |   Neto: ${netoTot:N0}   |   IVA (19%): ${ivaTot:N0}   |   TOTAL COMPRAS: ${totalBruto:N0}";
                g.DrawString(resumenTexto, fontKpi, brushKpi, left + 10, y + 8);
                y += 42;
            }

            int[] colWidths = new int[] { 60, 80, 85, 320, 100, 100, 90, 110 };
            string[] headers = new string[] { "TIPO", "FOLIO", "FECHA", "PROVEEDOR / RAZÓN SOCIAL", "RUT", "NETO", "IVA (19%)", "TOTAL" };

            Rectangle headerRect = new Rectangle(left, y, width, 24);
            using Brush bgHead = new SolidBrush(Color.FromArgb(15, 23, 42));
            g.FillRectangle(bgHead, headerRect);

            using Font fontHeader = new Font("Segoe UI", 8F, FontStyle.Bold);
            using Brush brushHeadTxt = new SolidBrush(Color.White);

            int curX = left;
            for (int i = 0; i < headers.Length; i++)
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = (i >= 5) ? StringAlignment.Far : (i == 0 || i == 1 || i == 2 ? StringAlignment.Center : StringAlignment.Near),
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(headers[i], fontHeader, brushHeadTxt, new RectangleF(curX + 4, y, colWidths[i] - 8, 24), sf);
                curX += colWidths[i];
            }
            y += 26;

            using Font fontRow = new Font("Segoe UI", 8F, FontStyle.Regular);
            using Font fontRowBold = new Font("Segoe UI", 8F, FontStyle.Bold);
            using Brush brushText = new SolidBrush(Color.FromArgb(15, 23, 42));
            using Brush brushAlt = new SolidBrush(Color.FromArgb(248, 250, 252));
            using Pen penLine = new Pen(Color.FromArgb(226, 232, 240));

            while (_filaImpresionActual < _comprasCargadas.Count)
            {
                if (y + 24 > e.MarginBounds.Bottom - 30)
                {
                    e.HasMorePages = true;
                    _nroPaginaPdf++;
                    return;
                }

                var c = _comprasCargadas[_filaImpresionActual];

                if (_filaImpresionActual % 2 == 1)
                {
                    g.FillRectangle(brushAlt, new Rectangle(left, y, width, 22));
                }
                g.DrawLine(penLine, left, y + 22, right, y + 22);

                curX = left;
                string[] cValores = new string[]
                {
                    ObtenerCodigoDte(c.TipoDocumento),
                    c.NroFacturaProveedor.ToString(),
                    c.FechaEmision.ToString("dd/MM/yyyy"),
                    c.RazonSocialProveedor,
                    c.RutProveedor,
                    $"${c.MontoNeto:N0}",
                    $"${c.MontoIva:N0}",
                    $"${c.MontoTotal:N0}"
                };

                for (int i = 0; i < cValores.Length; i++)
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = (i >= 5) ? StringAlignment.Far : (i == 0 || i == 1 || i == 2 ? StringAlignment.Center : StringAlignment.Near),
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    Font f = (i == 1 || i == 7) ? fontRowBold : fontRow;
                    g.DrawString(cValores[i], f, brushText, new RectangleF(curX + 4, y, colWidths[i] - 8, 22), sf);
                    curX += colWidths[i];
                }

                y += 22;
                _filaImpresionActual++;
            }

            using Font fontFoot = new Font("Segoe UI", 7.5F, FontStyle.Italic);
            using Brush brushFoot = new SolidBrush(Color.FromArgb(148, 163, 184));
            g.DrawString($"Página {_nroPaginaPdf}  -  Sistema POS Moderno", fontFoot, brushFoot, left, e.MarginBounds.Bottom - 14);

            e.HasMorePages = false;
        }

        private void DgvLibro_DoubleClick(object? sender, EventArgs e)
        {
            if (dgvLibro.CurrentRow != null && dgvLibro.CurrentRow.Index >= 0 && dgvLibro.CurrentRow.Index < _comprasCargadas.Count)
            {
                var compraSeleccionada = _comprasCargadas[dgvLibro.CurrentRow.Index];
                using var modal = new FormDetalleCompraModal(compraSeleccionada.CompraID);
                modal.ShowDialog(this);
            }
        }
    }
}