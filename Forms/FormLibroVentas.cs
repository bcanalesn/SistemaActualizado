using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormLibroVentas : Form
    {
        // Tarjetas KPI Superiores
        private Label lblKpiTotalVentas = null!;
        private Label lblKpiCantDocs = null!;
        private Label lblKpiBoletas = null!;
        private Label lblKpiPctBoletas = null!;
        private Label lblKpiFacturas = null!;
        private Label lblKpiPctFacturas = null!;
        private Label lblKpiDevoluciones = null!;
        private Label lblKpiMontoDev = null!;

        // Controles de Filtro
        private TextBox txtBuscar = null!;
        private DateTimePicker dtpDesde = null!;
        private DateTimePicker dtpHasta = null!;
        private ComboBox cbTipoDocumento = null!;
        private Button btnBuscar = null!;
        private Button btnLimpiar = null!;
        private Button btnExportarCSV = null!;
        private Button btnImprimirF29Top = null!;

        // Filtros Rápidos
        private Button btnFiltroHoy = null!;
        private Button btnFiltroAyer = null!;
        private Button btnFiltro7Dias = null!;
        private Button btnFiltroEsteMes = null!;
        private Button btnFiltroMesAnterior = null!;

        // DataGridView y Paginador
        private DataGridView dgvLibro = null!;
        private Label lblPaginadorInfo = null!;

        // Panel Lateral Resumen Contable F29
        private Label lblResumenNeto = null!;
        private Label lblResumenIva = null!;
        private Label lblResumenNotasCredito = null!;
        private Label lblResumenTotalDebito = null!;
        private Button btnImprimirF29 = null!;
        private Button btnNotaCredito = null!;

        private List<TVE2607> _ventasCargadas = new List<TVE2607>();

        public FormLibroVentas()
        {
            InitializeComponent();

            DateTime hoyInicio = DateTime.Today;
            DateTime hoyFin = DateTime.Today.AddDays(1).AddTicks(-1);

            dtpDesde.Value = hoyInicio;
            dtpHasta.Value = hoyFin;

            CargarLibroVentas(hoyInicio, hoyFin);
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

            // 1. SECCIÓN SUPERIOR DE KPIS
            Panel pnlKpiWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            TableLayoutPanel pnlKpisLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            pnlKpisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var card1 = CrearCardKPI("💲", Color.FromArgb(220, 252, 231), "Ventas del período", out lblKpiTotalVentas, out lblKpiCantDocs, "$ 0", "Con 0 documentos");
            var card2 = CrearCardKPI("🧾", Color.FromArgb(224, 242, 254), "Boletas emitidas", out lblKpiBoletas, out lblKpiPctBoletas, "0", "0% del total");
            var card3 = CrearCardKPI("📄", Color.FromArgb(243, 232, 255), "Facturas emitidas", out lblKpiFacturas, out lblKpiPctFacturas, "0", "0% del total");
            var card4 = CrearCardKPI("↩️", Color.FromArgb(254, 226, 226), "Devoluciones (N.C.)", out lblKpiDevoluciones, out lblKpiMontoDev, "0", "$ 0");

            card1.Margin = new Padding(0, 0, 6, 0);
            card2.Margin = new Padding(6, 0, 6, 0);
            card3.Margin = new Padding(6, 0, 6, 0);
            card4.Margin = new Padding(6, 0, 0, 0);

            pnlKpisLayout.Controls.Add(card1, 0, 0);
            pnlKpisLayout.Controls.Add(card2, 1, 0);
            pnlKpisLayout.Controls.Add(card3, 2, 0);
            pnlKpisLayout.Controls.Add(card4, 3, 0);
            pnlKpiWrapper.Controls.Add(pnlKpisLayout);

            // 2. SECCIÓN FILTROS Y ACCIONES
            Panel pnlFiltrosWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 118, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 10, 16, 10);

            FlowLayoutPanel flowFiltrosFila1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, WrapContents = false, BackColor = Color.Transparent };

            txtBuscar = new TextBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5F), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(248, 250, 252), Text = "Ingrese folio o RUT", ForeColor = Color.Gray };
            txtBuscar.Enter += (s, e) => { if (txtBuscar.Text.StartsWith("Ingrese")) { txtBuscar.Text = ""; txtBuscar.ForeColor = Color.Black; } };
            txtBuscar.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) { txtBuscar.Text = "Ingrese folio o RUT"; txtBuscar.ForeColor = Color.Gray; } };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { EjcutarBusqueda(); e.SuppressKeyPress = true; } };
            Panel pnlGrpFolio = CrearGrupoConCaja("Folio DTE / RUT", txtBuscar, 175);

            dtpDesde = new DateTimePicker { Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlGrpDesde = CrearGrupoLimpio("Fecha desde", dtpDesde, 125);

            dtpHasta = new DateTimePicker { Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlGrpHasta = CrearGrupoLimpio("Fecha hasta", dtpHasta, 125);

            cbTipoDocumento = new ComboBox { Size = new Size(145, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbTipoDocumento.Items.AddRange(new string[] { "Todos", "Boleta Electrónica", "Factura Electrónica", "Nota de Crédito" });
            cbTipoDocumento.SelectedIndex = 0;
            Panel pnlGrpTipoDoc = CrearGrupoLimpio("Tipo Documento", cbTipoDocumento, 150);

            btnBuscar = CrearBotonRedondeado("🔍  Buscar", Color.FromArgb(0, 102, 255), Color.White, new Size(92, 34), 8);
            btnBuscar.Margin = new Padding(8, 14, 6, 0);
            btnBuscar.Click += (s, e) => EjcutarBusqueda();

            btnLimpiar = CrearBotonRedondeado("🗑️  Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(88, 34), 8);
            btnLimpiar.Margin = new Padding(0, 14, 0, 0);
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            flowFiltrosFila1.Controls.AddRange(new Control[] { pnlGrpFolio, pnlGrpDesde, pnlGrpHasta, pnlGrpTipoDoc, btnBuscar, btnLimpiar });

            FlowLayoutPanel flowFiltrosFila2 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, BackColor = Color.Transparent };
            Label lblFiltrosRapidos = new Label { Text = "Filtros rápidos:", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Margin = new Padding(0, 6, 8, 0), AutoSize = true };

            btnFiltroHoy = CrearPillBoton("📅 Hoy", 65, (s, e) => AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroAyer = CrearPillBoton("📅 Ayer", 65, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-1), DateTime.Today.AddTicks(-1)));
            btnFiltro7Dias = CrearPillBoton("📅 Últimos 7 días", 115, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroEsteMes = CrearPillBoton("📅 Este mes", 85, (s, e) => AplicarFiltroFecha(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroMesAnterior = CrearPillBoton("📅 Mes anterior", 105, (s, e) =>
            {
                var inicioMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                var finMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddTicks(-1);
                AplicarFiltroFecha(inicioMesAnt, finMesAnt);
            });

            btnExportarCSV = CrearBotonRedondeado("📥  Exportar CSV", Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52), new Size(125, 28), 6);
            btnExportarCSV.Margin = new Padding(18, 2, 6, 0);
            btnExportarCSV.Click += BtnExportarCSV_Click;

            btnImprimirF29Top = CrearBotonRedondeado("🖨️  Imprimir F29", Color.FromArgb(15, 23, 42), Color.White, new Size(115, 28), 6);
            btnImprimirF29Top.Margin = new Padding(0, 2, 0, 0);
            btnImprimirF29Top.Click += (s, e) => MessageBox.Show("Generando reporte F29 oficial para impresión...", "Imprimir F29", MessageBoxButtons.OK, MessageBoxIcon.Information);

            flowFiltrosFila2.Controls.AddRange(new Control[] { lblFiltrosRapidos, btnFiltroHoy, btnFiltroAyer, btnFiltro7Dias, btnFiltroEsteMes, btnFiltroMesAnterior, btnExportarCSV, btnImprimirF29Top });

            pnlFiltrosCard.Controls.Add(flowFiltrosFila2);
            pnlFiltrosCard.Controls.Add(flowFiltrosFila1);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // 3. TABLA Y PANEL LATERAL RESUMEN CONTABLE
            TableLayoutPanel pnlGridAndDetail = new TableLayoutPanel { Dock = DockStyle.Top, Height = 480, ColumnCount = 2, RowCount = 1 };
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73F));
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));

            Panel pnlTableCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlTableCard.Dock = DockStyle.Fill;
            pnlTableCard.Padding = new Padding(10);
            pnlTableCard.Margin = new Padding(0, 0, 10, 0);

            dgvLibro = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvLibro);
            dgvLibro.SelectionChanged += DgvLibro_SelectionChanged;
            dgvLibro.CellFormatting += DgvLibro_CellFormatting;
            dgvLibro.CellDoubleClick += DgvLibro_CellDoubleClick;

            Panel pnlPaginador = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(4, 8, 4, 0) };
            lblPaginadorInfo = new Label { Text = "Mostrando registros...", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true };

            FlowLayoutPanel flowNumPaginas = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };
            Button btnPagPrev = CrearBotonRedondeado("<", Color.FromArgb(248, 250, 252), Color.FromArgb(100, 116, 139), new Size(32, 30), 6);
            Button btnPag1 = CrearBotonRedondeado("1", Color.FromArgb(0, 102, 255), Color.White, new Size(32, 30), 6);
            Button btnPagNext = CrearBotonRedondeado(">", Color.FromArgb(248, 250, 252), Color.FromArgb(100, 116, 139), new Size(32, 30), 6);

            btnPagPrev.Margin = new Padding(0, 0, 4, 0);
            btnPag1.Margin = new Padding(0, 0, 4, 0);
            btnPagNext.Margin = new Padding(0, 0, 0, 0);

            flowNumPaginas.Controls.AddRange(new Control[] { btnPagPrev, btnPag1, btnPagNext });

            pnlPaginador.Controls.Add(lblPaginadorInfo);
            pnlPaginador.Controls.Add(flowNumPaginas);

            pnlTableCard.Controls.Add(dgvLibro);
            pnlTableCard.Controls.Add(pnlPaginador);

            // PANEL LATERAL RESUMEN CONTABLE
            Panel pnlDetailCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlDetailCard.Dock = DockStyle.Fill;
            pnlDetailCard.Padding = new Padding(14);

            Label lblTituloDetalle = new Label { Text = "📄 Resumen del Período", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 30 };

            Panel pnlCamposDetalle = new Panel { Dock = DockStyle.Top, Height = 175, Padding = new Padding(0, 6, 0, 0) };
            int yPos = 0;
            lblResumenNeto = CrearRenglonResumen("Total Neto (Afecto)", "$ 0", Color.FromArgb(22, 101, 52), ref yPos, pnlCamposDetalle);
            lblResumenIva = CrearRenglonResumen("Débito Fiscal IVA (19%)", "$ 0", Color.FromArgb(2, 132, 199), ref yPos, pnlCamposDetalle);
            lblResumenNotasCredito = CrearRenglonResumen("Descuentos por N.C.", "$ 0", Color.FromArgb(220, 38, 38), ref yPos, pnlCamposDetalle);

            Panel pnlTotalDebitoHighlight = CrearTarjetaRedondeada(0, 0, 0, 75, Color.FromArgb(240, 249, 255), Color.FromArgb(186, 230, 253));
            pnlTotalDebitoHighlight.Dock = DockStyle.Top;
            pnlTotalDebitoHighlight.Padding = new Padding(6);

            Label lblTotalDebitoCap = new Label { Text = "Débito Fiscal a Declarar (F29)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 18 };
            lblResumenTotalDebito = new Label { Text = "$ 0", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlTotalDebitoHighlight.Controls.Add(lblResumenTotalDebito);
            pnlTotalDebitoHighlight.Controls.Add(lblTotalDebitoCap);

            Panel pnlBotonesAccion = new Panel { Dock = DockStyle.Bottom, Height = 95, Padding = new Padding(0, 6, 0, 0) };

            btnImprimirF29 = CrearBotonRedondeado("🖨️  Imprimir F29", Color.FromArgb(0, 102, 255), Color.White, new Size(200, 40), 8);
            btnImprimirF29.Dock = DockStyle.Top;
            btnImprimirF29.Click += (s, e) => MessageBox.Show("Generando reporte F29...", "Impresión F29", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Panel pnlSpacerBtn = new Panel { Dock = DockStyle.Top, Height = 6 };

            btnNotaCredito = CrearBotonRedondeado("📄  Emitir Nota de Crédito", Color.FromArgb(220, 38, 38), Color.White, new Size(200, 40), 8);
            btnNotaCredito.Dock = DockStyle.Top;
            btnNotaCredito.Enabled = false;
            btnNotaCredito.Click += BtnNotaCredito_Click;

            pnlBotonesAccion.Controls.Add(btnNotaCredito);
            pnlBotonesAccion.Controls.Add(pnlSpacerBtn);
            pnlBotonesAccion.Controls.Add(btnImprimirF29);

            pnlDetailCard.Controls.Add(pnlBotonesAccion);
            pnlDetailCard.Controls.Add(pnlTotalDebitoHighlight);
            pnlDetailCard.Controls.Add(pnlCamposDetalle);
            pnlDetailCard.Controls.Add(lblTituloDetalle);

            pnlGridAndDetail.Controls.Add(pnlTableCard, 0, 0);
            pnlGridAndDetail.Controls.Add(pnlDetailCard, 1, 0);

            pnlMain.Controls.Add(pnlGridAndDetail);
            pnlMain.Controls.Add(pnlFiltrosWrapper);
            pnlMain.Controls.Add(pnlKpiWrapper);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearCardKPI(string icon, Color iconBg, string titulo, out Label lblValor, out Label lblSub, string valInit, string subInit)
        {
            Panel pnl = CrearTarjetaRedondeada(0, 0, 0, 80, Color.White, Color.FromArgb(226, 232, 240));
            pnl.Dock = DockStyle.Fill;
            pnl.Padding = new Padding(12);

            Label lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 12F), BackColor = iconBg, Size = new Size(36, 36), TextAlign = ContentAlignment.MiddleCenter, Location = new Point(12, 12) };

            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(56, 10), AutoSize = true };
            lblValor = new Label { Text = valInit, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(56, 26), AutoSize = true };
            lblSub = new Label { Text = subInit, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(56, 52), AutoSize = true };

            pnl.Controls.AddRange(new Control[] { lblIcon, lblT, lblValor, lblSub });
            return pnl;
        }

        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 10);
                using Pen pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
        }

        private Panel CrearGrupoConCaja(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

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
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            control.Location = new Point(0, 18);
            control.Width = ancho;

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private Button CrearBotonRedondeado(string texto, Color back, Color fore, Size size, int radio)
        {
            Button b = new Button { Text = texto, Size = size, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, radio);
                b.Region = new Region(p);
            };
            return b;
        }

        private Button CrearPillBoton(string texto, int ancho, EventHandler onClick)
        {
            Button b = CrearBotonRedondeado(texto, Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105), new Size(ancho, 25), 12);
            b.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            b.Margin = new Padding(0, 2, 6, 0);
            b.Click += onClick;
            return b;
        }

        private Label CrearRenglonResumen(string titulo, string valorInicial, Color valorColor, ref int y, Panel container)
        {
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, y), AutoSize = true };
            Label lblV = new Label { Text = valorInicial, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = valorColor, Location = new Point(140, y - 1), AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            container.Controls.Add(lblT);
            container.Controls.Add(lblV);
            y += 36;
            return lblV;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersHeight = 36;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 36;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void DgvLibro_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvLibro.Columns[e.ColumnIndex].Name == "Documento" && e.Value != null)
            {
                string doc = e.Value.ToString() ?? "";
                if (doc.Contains("Boleta"))
                    e.CellStyle!.ForeColor = Color.FromArgb(2, 132, 199);
                else if (doc.Contains("Factura"))
                    e.CellStyle!.ForeColor = Color.FromArgb(124, 58, 237);
                else if (doc.Contains("Crédito"))
                    e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);

                e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }
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

        private void AplicarFiltroFecha(DateTime desde, DateTime hasta)
        {
            dtpDesde.Value = desde.Date;
            dtpHasta.Value = hasta.Date.AddDays(1).AddTicks(-1);
            CargarLibroVentas(dtpDesde.Value, dtpHasta.Value);
        }

        private void CargarLibroVentas(DateTime desde, DateTime hasta)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // FILTRADO ESTRICTO: NO SE INCLUYEN TICKETS DE ATENCIÓN EN EL LIBRO DE VENTAS OFICIAL
                    _ventasCargadas = db.TVE2607
                        .Where(v => v.FecDoc >= desde && v.FecDoc <= hasta && 
                                    v.Documento != "Ticket de Atención" && 
                                    v.iddocDTE != 0)
                        .OrderByDescending(v => v.FecDoc)
                        .ToList();
                }

                CalcularMetricas();
                FiltrarLocalmente();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el Libro de Ventas: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalcularMetricas()
        {
            var validas = _ventasCargadas.Where(v => !v.status.Contains("Anulado")).ToList();

            var boletas = validas.Where(v => v.Documento.Contains("Boleta")).ToList();
            var facturas = validas.Where(v => v.Documento.Contains("Factura")).ToList();
            
            // Solo Notas de Crédito emitidas oficialmente (iddocDTE == 61)
            var notasCredito = _ventasCargadas.Where(v => v.iddocDTE == 61 || v.Documento.Contains("Crédito")).ToList();

            decimal totalVentas = validas.Where(v => v.iddocDTE != 61).Sum(v => v.Total);
            int cantDocs = validas.Count;

            decimal netoTotal = validas.Where(v => v.iddocDTE != 61).Sum(v => v.Neto);
            decimal ivaBoletas = boletas.Sum(v => v.IvA);
            decimal ivaFacturas = facturas.Sum(v => v.IvA);
            decimal ivaNotasCredito = notasCredito.Sum(v => v.IvA);
            decimal debitoFiscalTotal = (ivaFacturas + ivaBoletas) - ivaNotasCredito;

            // KPIs Superiores
            lblKpiTotalVentas.Text = $"$ {totalVentas:N0}";
            lblKpiCantDocs.Text = $"Con {cantDocs} documentos";

            lblKpiBoletas.Text = boletas.Count.ToString();
            lblKpiPctBoletas.Text = cantDocs > 0 ? $"{Math.Round((decimal)boletas.Count / cantDocs * 100, 0)}% del total" : "0% del total";

            lblKpiFacturas.Text = facturas.Count.ToString();
            lblKpiPctFacturas.Text = cantDocs > 0 ? $"{Math.Round((decimal)facturas.Count / cantDocs * 100, 0)}% del total" : "0% del total";

            lblKpiDevoluciones.Text = notasCredito.Count.ToString();
            lblKpiMontoDev.Text = $"$ {notasCredito.Sum(d => d.Total):N0}";

            // Panel Lateral F29
            lblResumenNeto.Text = $"$ {netoTotal:N0}";
            lblResumenIva.Text = $"$ {debitoFiscalTotal:N0}";
            lblResumenNotasCredito.Text = $"$ {notasCredito.Sum(d => d.Total):N0}";
            lblResumenTotalDebito.Text = $"$ {debitoFiscalTotal:N0}";
        }

        private void EjcutarBusqueda()
        {
            DateTime desde = dtpDesde.Value.Date;
            DateTime hasta = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);
            CargarLibroVentas(desde, hasta);
        }

        private void LimpiarFiltros()
        {
            txtBuscar.Text = "Ingrese folio o RUT";
            txtBuscar.ForeColor = Color.Gray;
            cbTipoDocumento.SelectedIndex = 0;
            AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void FiltrarLocalmente()
        {
            var resultado = _ventasCargadas.AsQueryable();

            string textoBusqueda = txtBuscar.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(textoBusqueda) && !textoBusqueda.StartsWith("ingrese"))
            {
                resultado = resultado.Where(v => v.nroDTE.ToString().Contains(textoBusqueda) ||
                                                 (!string.IsNullOrEmpty(v.RuT) && v.RuT.ToLower().Contains(textoBusqueda)));
            }

            string tipoSel = cbTipoDocumento.SelectedItem?.ToString() ?? "Todos";
            if (tipoSel != "Todos")
            {
                resultado = resultado.Where(v => v.Documento.Contains(tipoSel));
            }

            var listaFinal = resultado.Select(v => new
            {
                Documento = v.Documento,
                FolioDTE = v.nroDTE,
                FechaEmision = v.FecDoc,
                Neto = v.Neto,
                IVA = v.IvA,
                Total = v.Total,
                RUT = string.IsNullOrEmpty(v.RuT) ? "-" : v.RuT,
                ObjetoOriginal = v
            }).ToList();

            dgvLibro.DataSource = listaFinal;

            if (dgvLibro.Columns["Documento"] != null) dgvLibro.Columns["Documento"].HeaderText = "TIPO DOCUMENTO";
            if (dgvLibro.Columns["FolioDTE"] != null) { dgvLibro.Columns["FolioDTE"].HeaderText = "FOLIO DTE"; dgvLibro.Columns["FolioDTE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvLibro.Columns["FechaEmision"] != null) { dgvLibro.Columns["FechaEmision"].HeaderText = "FECHA EMISIÓN"; dgvLibro.Columns["FechaEmision"].DefaultCellStyle.Format = "dd-MM-yyyy HH:mm"; }
            if (dgvLibro.Columns["Neto"] != null) { dgvLibro.Columns["Neto"].HeaderText = "NETO"; dgvLibro.Columns["Neto"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvLibro.Columns["IVA"] != null) { dgvLibro.Columns["IVA"].HeaderText = "IVA (19%)"; dgvLibro.Columns["IVA"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvLibro.Columns["Total"] != null) { dgvLibro.Columns["Total"].HeaderText = "TOTAL"; dgvLibro.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; dgvLibro.Columns["Total"].DefaultCellStyle.ForeColor = Color.FromArgb(0, 102, 255); dgvLibro.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
            if (dgvLibro.Columns["RUT"] != null) dgvLibro.Columns["RUT"].HeaderText = "RUT CLIENTE";

            if (dgvLibro.Columns["ObjetoOriginal"] != null) dgvLibro.Columns["ObjetoOriginal"].Visible = false;

            lblPaginadorInfo.Text = $"Mostrando {listaFinal.Count} de {_ventasCargadas.Count} documentos oficiales";
        }

        private void DgvLibro_SelectionChanged(object? sender, EventArgs e)
        {
            var seleccionadas = ObtenerVentasSeleccionadas();
            btnNotaCredito.Enabled = seleccionadas.Any(v => v.iddocDTE != 61 && !v.status.Equals("Anulado NC", StringComparison.OrdinalIgnoreCase));
        }

        private List<TVE2607> ObtenerVentasSeleccionadas()
        {
            var lista = new List<TVE2607>();
            if (dgvLibro.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvLibro.SelectedRows)
                {
                    if (row.DataBoundItem != null)
                    {
                        dynamic item = row.DataBoundItem;
                        if (item.ObjetoOriginal is TVE2607 v)
                        {
                            lista.Add(v);
                        }
                    }
                }
            }
            return lista;
        }

        // DOBLE CLIC ABRE EL MODAL MODULARIZADO
        private void DgvLibro_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            dynamic item = dgvLibro.Rows[e.RowIndex].DataBoundItem;
            TVE2607 venta = item.ObjetoOriginal;

            using (var modal = new FormDetalleVentaModal(venta))
            {
                modal.ShowDialog(this);
            }
        }

        private void BtnExportarCSV_Click(object? sender, EventArgs e)
        {
            if (_ventasCargadas.Count == 0)
            {
                MessageBox.Show("No hay datos en el Libro de Ventas para exportar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"Libro_Ventas_{DateTime.Now:yyyyMMdd_HHmm}.csv" };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Tipo Documento;Folio DTE;Fecha Emision;RUT Cliente;Razon Social;Neto;IVA;Total");

                    foreach (var v in _ventasCargadas)
                    {
                        string rut = string.IsNullOrEmpty(v.RuT) ? "66666666-6" : v.RuT;
                        string razon = string.IsNullOrEmpty(v.RazonSocial) ? "Consumidor Final" : v.RazonSocial;
                        sb.AppendLine($"{v.Documento};{v.nroDTE};{v.FecDoc:dd/MM/yyyy HH:mm};{rut};{razon};{v.Neto};{v.IvA};{v.Total}");
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"¡Libro de Ventas exportado exitosamente a:\n{sfd.FileName}", "Exportación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar CSV: {ex.Message}", "Error de Exportación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnNotaCredito_Click(object? sender, EventArgs e)
        {
            var ventasParaAnular = ObtenerVentasSeleccionadas()
                .Where(v => v.iddocDTE != 61 && !v.status.Equals("Anulado NC", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (ventasParaAnular.Count == 0)
            {
                MessageBox.Show("Seleccione una o más Boletas / Facturas válidas para emitir Nota de Crédito.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal totalConsolidado = ventasParaAnular.Sum(v => v.Total);
            decimal netoConsolidado = ventasParaAnular.Sum(v => v.Neto);
            decimal ivaConsolidado = ventasParaAnular.Sum(v => v.IvA);
            string foliosRef = string.Join(", ", ventasParaAnular.Select(v => $"#{v.nroDTE}"));

            Form modalNC = new Form
            {
                Text = $"Emitir Nota de Crédito Múltiple ({ventasParaAnular.Count} documentos)",
                Size = new Size(500, 510),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = "📄 Emitir Nota de Crédito Electrónica", Location = new Point(25, 18), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(185, 28, 28), AutoSize = true };
            Label lblInfo = new Label
            {
                Text = $"Documentos seleccionados: {ventasParaAnular.Count} ({foliosRef})\nMonto Total a Anular: ${totalConsolidado:N0} (Neto: ${netoConsolidado:N0} | IVA: ${ivaConsolidado:N0})",
                Location = new Point(25, 48),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true
            };

            Label lblCausa = new Label { Text = "Código de Referencia SII:", Location = new Point(25, 105), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbCausa = new ComboBox { Location = new Point(25, 127), Size = new Size(430, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbCausa.Items.AddRange(new string[] { "1 - Anula Documento de Referencia (Anulación / Devolución Total)", "2 - Corregir Texto en Documento (Glosa / Datos)", "3 - Corregir Montos / Descuento Parcial" });
            cbCausa.SelectedIndex = 0;

            Label lblMotivo = new Label { Text = "Motivo / Glosa de la Anulación:", Location = new Point(25, 170), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtMotivo = new TextBox { Text = "Devolución masiva de mercadería / Anulación de ventas", Location = new Point(25, 192), Size = new Size(430, 28), Font = new Font("Segoe UI", 9.5F) };

            CheckBox chkRestock = new CheckBox { Text = "🔄 Reintegrar automáticamente todos los productos al inventario", Location = new Point(25, 235), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161) };

            Button btnEmitirNC = CrearBotonRedondeado("⚡ GENERAR Y EMITIR NOTA DE CRÉDITO CONSOLIDADA", Color.FromArgb(220, 38, 38), Color.White, new Size(430, 48), 8);
            btnEmitirNC.Location = new Point(25, 330);

            btnEmitirNC.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtMotivo.Text))
                {
                    MessageBox.Show("Debe ingresar un motivo para la emisión.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (var db = new AppDbContext())
                    {
                        var notaCreditoService = new NotaCreditoService(db);
                        var idsVentas = ventasParaAnular.Select(v => v.idTve);
                        int totalEmitidas = notaCreditoService.EmitirNotasCredito(idsVentas, txtMotivo.Text.Trim(), (cbCausa.SelectedIndex + 1).ToString(), chkRestock.Checked);

                        if (totalEmitidas == 0)
                        {
                            MessageBox.Show("No se emitieron Notas de Crédito. Verifique que las ventas seleccionadas sean válidas.", "Sin cambios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    MessageBox.Show($"¡Se emitieron las Notas de Crédito exitosamente para {ventasParaAnular.Count} documento(s)!", "Proceso Exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modalNC.Close();
                    AplicarFiltroFecha(dtpDesde.Value.Date, dtpHasta.Value.Date.AddDays(1).AddTicks(-1));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al emitir Notas de Crédito: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modalNC.Controls.AddRange(new Control[] { lblTitle, lblInfo, lblCausa, cbCausa, lblMotivo, txtMotivo, chkRestock, btnEmitirNC });
            modalNC.ShowDialog();
        }
    }
}