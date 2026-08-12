using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;

namespace SISTEMAACTUALIZADO
{
    public class FormHistorialVentas : Form
    {
        private AppDbContext _db = new AppDbContext();

        // Tarjetas KPI
        private Label lblKpiTotalVentas = null!;
        private Label lblKpiCantDocs = null!;
        private Label lblKpiBoletas = null!;
        private Label lblKpiPctBoletas = null!;
        private Label lblKpiFacturas = null!;
        private Label lblKpiPctFacturas = null!;
        private Label lblKpiDevoluciones = null!;
        private Label lblKpiMontoDev = null!;

        // Filtros
        private TextBox txtBuscarFolio = null!;
        private DateTimePicker dtpDesde = null!;
        private DateTimePicker dtpHasta = null!;
        private ComboBox cbTipoDocumento = null!;
        private Button btnBuscar = null!;
        private Button btnLimpiar = null!;

        // Filtros Rápidos
        private Button btnFiltroHoy = null!;
        private Button btnFiltroAyer = null!;
        private Button btnFiltro7Dias = null!;
        private Button btnFiltroEsteMes = null!;
        private Button btnFiltroMesAnterior = null!;

        // DataGridView
        private DataGridView dgvVentas = null!;

        // Resumen Lateral
        private Label lblDetTipoDoc = null!;
        private Label lblDetFolio = null!;
        private Label lblDetFecha = null!;
        private Label lblDetMedio = null!;
        private Label lblDetCliente = null!;
        private Label lblDetNeto = null!;
        private Label lblDetIva = null!;
        private Label lblDetTotal = null!;

        private Button btnReimprimir = null!;
        private Button btnNotaCredito = null!;
        private Label lblPaginadorInfo = null!;

        private TVE2607? _ventaSeleccionada = null;
        private List<TVE2607> _ventasCargadas = new List<TVE2607>();

        public FormHistorialVentas()
        {
            InitializeComponent();
            
            DateTime hoyInicio = DateTime.Today;
            DateTime hoyFin = DateTime.Today.AddDays(1).AddTicks(-1);
            
            dtpDesde.Value = hoyInicio;
            dtpHasta.Value = hoyFin;
            
            CargarVentas(hoyInicio, hoyFin);
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

            // 1. SECCIÓN KPIS DENTRO DE MARCO CON SEPARACIÓN VISUAL
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
            var card4 = CrearCardKPI("↩️", Color.FromArgb(254, 226, 226), "Devoluciones", out lblKpiDevoluciones, out lblKpiMontoDev, "0", "$ 0");

            card1.Margin = new Padding(0, 0, 6, 0);
            card2.Margin = new Padding(6, 0, 6, 0);
            card3.Margin = new Padding(6, 0, 6, 0);
            card4.Margin = new Padding(6, 0, 0, 0);

            pnlKpisLayout.Controls.Add(card1, 0, 0);
            pnlKpisLayout.Controls.Add(card2, 1, 0);
            pnlKpisLayout.Controls.Add(card3, 2, 0);
            pnlKpisLayout.Controls.Add(card4, 3, 0);
            pnlKpiWrapper.Controls.Add(pnlKpisLayout);

            // 2. SECCIÓN FILTROS DENTRO DE MARCO CON SEPARACIÓN VISUAL
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

            txtBuscarFolio = new TextBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5F), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(248, 250, 252), Text = "Ingrese folio, ticket o RUT", ForeColor = Color.Gray };
            txtBuscarFolio.Enter += (s, e) => { if (txtBuscarFolio.Text.StartsWith("Ingrese")) { txtBuscarFolio.Text = ""; txtBuscarFolio.ForeColor = Color.Black; } };
            txtBuscarFolio.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscarFolio.Text)) { txtBuscarFolio.Text = "Ingrese folio, ticket o RUT"; txtBuscarFolio.ForeColor = Color.Gray; } };
            txtBuscarFolio.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { EjcutarBusqueda(); e.SuppressKeyPress = true; } };
            Panel pnlGrpFolio = CrearGrupoConCaja("Folio / Ticket / RUT", txtBuscarFolio, 180);

            dtpDesde = new DateTimePicker { Size = new Size(125, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlGrpDesde = CrearGrupoLimpio("Fecha desde", dtpDesde, 130);

            dtpHasta = new DateTimePicker { Size = new Size(125, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlGrpHasta = CrearGrupoLimpio("Fecha hasta", dtpHasta, 130);

            cbTipoDocumento = new ComboBox { Size = new Size(145, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbTipoDocumento.Items.AddRange(new string[] { "Todos", "Boleta Electrónica", "Factura Electrónica", "Ticket de Atención", "Nota de Crédito" });
            cbTipoDocumento.SelectedIndex = 0;
            Panel pnlGrpTipoDoc = CrearGrupoLimpio("Tipo Documento", cbTipoDocumento, 150);

            btnBuscar = CrearBotonRedondeado("🔍  Buscar", Color.FromArgb(0, 102, 255), Color.White, new Size(95, 34), 8);
            btnBuscar.Margin = new Padding(12, 14, 6, 0);
            btnBuscar.Click += (s, e) => EjcutarBusqueda();

            btnLimpiar = CrearBotonRedondeado("🗑️  Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(90, 34), 8);
            btnLimpiar.Margin = new Padding(0, 14, 0, 0);
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            flowFiltrosFila1.Controls.AddRange(new Control[] { pnlGrpFolio, pnlGrpDesde, pnlGrpHasta, pnlGrpTipoDoc, btnBuscar, btnLimpiar });

            FlowLayoutPanel flowFiltrosFila2 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, WrapContents = false, BackColor = Color.Transparent };
            Label lblFiltrosRapidos = new Label { Text = "Filtros rápidos:", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Margin = new Padding(0, 6, 10, 0), AutoSize = true };

            btnFiltroHoy = CrearPillBoton("📅 Hoy", 65, (s, e) => AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroAyer = CrearPillBoton("📅 Ayer", 65, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-1), DateTime.Today.AddTicks(-1)));
            btnFiltro7Dias = CrearPillBoton("📅 Últimos 7 días", 120, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroEsteMes = CrearPillBoton("📅 Este mes", 90, (s, e) => AplicarFiltroFecha(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroMesAnterior = CrearPillBoton("📅 Mes anterior", 110, (s, e) =>
            {
                var inicioMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                var finMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddTicks(-1);
                AplicarFiltroFecha(inicioMesAnt, finMesAnt);
            });

            flowFiltrosFila2.Controls.AddRange(new Control[] { lblFiltrosRapidos, btnFiltroHoy, btnFiltroAyer, btnFiltro7Dias, btnFiltroEsteMes, btnFiltroMesAnterior });

            pnlFiltrosCard.Controls.Add(flowFiltrosFila2);
            pnlFiltrosCard.Controls.Add(flowFiltrosFila1);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // 3. TABLA Y PANEL RESUMEN LATERAL
            TableLayoutPanel pnlGridAndDetail = new TableLayoutPanel { Dock = DockStyle.Top, Height = 480, ColumnCount = 2, RowCount = 1 };
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73F));
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));

            Panel pnlTableCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlTableCard.Dock = DockStyle.Fill;
            pnlTableCard.Padding = new Padding(10);
            pnlTableCard.Margin = new Padding(0, 0, 10, 0);

            dgvVentas = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvVentas);
            dgvVentas.SelectionChanged += DgvVentas_SelectionChanged;
            dgvVentas.CellFormatting += DgvVentas_CellFormatting;

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

            pnlTableCard.Controls.Add(dgvVentas);
            pnlTableCard.Controls.Add(pnlPaginador);

            // PANEL DETALLE LATERAL
            Panel pnlDetailCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlDetailCard.Dock = DockStyle.Fill;
            pnlDetailCard.Padding = new Padding(14);

            Label lblTituloDetalle = new Label { Text = "📄 Resumen del Documento", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 28 };

            Panel pnlCamposDetalle = new Panel { Dock = DockStyle.Top, Height = 225, Padding = new Padding(0, 4, 0, 0) };
            int yPos = 0;
            lblDetTipoDoc = CrearRenglonDetalle("Tipo Documento", "-", ref yPos, pnlCamposDetalle);
            lblDetFolio = CrearRenglonDetalle("Folio DTE N°", "-", ref yPos, pnlCamposDetalle);
            lblDetFecha = CrearRenglonDetalle("Fecha Emisión", "-", ref yPos, pnlCamposDetalle);
            lblDetMedio = CrearRenglonDetalle("Vendedor", "-", ref yPos, pnlCamposDetalle);
            lblDetCliente = CrearRenglonDetalle("Cliente / RUT", "-", ref yPos, pnlCamposDetalle);
            lblDetNeto = CrearRenglonDetalle("Monto Neto", "$ 0", ref yPos, pnlCamposDetalle);
            lblDetIva = CrearRenglonDetalle("IVA (19%)", "$ 0", ref yPos, pnlCamposDetalle);

            Panel pnlTotalHighlight = CrearTarjetaRedondeada(0, 0, 0, 60, Color.FromArgb(240, 249, 255), Color.FromArgb(186, 230, 253));
            pnlTotalHighlight.Dock = DockStyle.Top;
            pnlTotalHighlight.Padding = new Padding(6);

            Label lblTotalCaption = new Label { Text = "Monto Total", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 16 };
            lblDetTotal = new Label { Text = "$ 0", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlTotalHighlight.Controls.Add(lblDetTotal);
            pnlTotalHighlight.Controls.Add(lblTotalCaption);

            Panel pnlBotonesAccion = new Panel { Dock = DockStyle.Bottom, Height = 95, Padding = new Padding(0, 6, 0, 0) };

            btnReimprimir = CrearBotonRedondeado("🖨️  Reimprimir DTE", Color.FromArgb(0, 102, 255), Color.White, new Size(200, 40), 8);
            btnReimprimir.Dock = DockStyle.Top;
            btnReimprimir.Enabled = false;
            btnReimprimir.Click += BtnReimprimir_Click;

            Panel pnlSpacerBtn = new Panel { Dock = DockStyle.Top, Height = 6 };

            btnNotaCredito = CrearBotonRedondeado("📄  Emitir Nota de Crédito", Color.FromArgb(220, 38, 38), Color.White, new Size(200, 40), 8);
            btnNotaCredito.Dock = DockStyle.Top;
            btnNotaCredito.Enabled = false;
            btnNotaCredito.Click += BtnNotaCredito_Click;

            pnlBotonesAccion.Controls.Add(btnNotaCredito);
            pnlBotonesAccion.Controls.Add(pnlSpacerBtn);
            pnlBotonesAccion.Controls.Add(btnReimprimir);

            pnlDetailCard.Controls.Add(pnlBotonesAccion);
            pnlDetailCard.Controls.Add(pnlTotalHighlight);
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

        private Label CrearRenglonDetalle(string titulo, string valorInicial, ref int y, Panel container)
        {
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, y), AutoSize = true };
            Label lblV = new Label { Text = valorInicial, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, y + 14), AutoSize = true };
            container.Controls.Add(lblT);
            container.Controls.Add(lblV);
            y += 32;
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

        private void DgvVentas_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvVentas.Columns[e.ColumnIndex].Name == "Documento" && e.Value != null)
            {
                string doc = e.Value.ToString() ?? "";
                if (doc.Contains("Boleta"))
                    e.CellStyle!.ForeColor = Color.FromArgb(2, 132, 199);
                else if (doc.Contains("Factura"))
                    e.CellStyle!.ForeColor = Color.FromArgb(124, 58, 237);
                else if (doc.Contains("Ticket"))
                    e.CellStyle!.ForeColor = Color.FromArgb(217, 119, 6);
                else if (doc.Contains("Crédito"))
                    e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);

                e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }

            if (dgvVentas.Columns[e.ColumnIndex].Name == "Estado" && e.Value != null)
            {
                string estado = e.Value.ToString() ?? "";
                if (estado == "Pagado")
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(16, 185, 129);
                }
                else
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(239, 68, 68);
                }
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
            CargarVentas(dtpDesde.Value, dtpHasta.Value);
        }

        private void CargarVentas(DateTime desde, DateTime hasta)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    _ventasCargadas = db.TVE2607
                        .Where(v => v.FecDoc >= desde && v.FecDoc <= hasta)
                        .OrderByDescending(v => v.FecDoc)
                        .ToList();
                }

                CalcularMetricasKPI();
                FiltrarLocalmente();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas desde TVE2607: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalcularMetricasKPI()
        {
            decimal totalVentas = _ventasCargadas.Where(v => !v.status.Contains("Anulado")).Sum(v => v.Total);
            int cantDocs = _ventasCargadas.Count(v => !v.status.Contains("Anulado"));

            var boletas = _ventasCargadas.Where(v => v.Documento.Contains("Boleta") && !v.status.Contains("Anulado")).ToList();
            var facturas = _ventasCargadas.Where(v => v.Documento.Contains("Factura") && !v.status.Contains("Anulado")).ToList();
            var devoluciones = _ventasCargadas.Where(v => v.Documento.Contains("Crédito") || v.status.Contains("Anulado")).ToList();

            lblKpiTotalVentas.Text = $"$ {totalVentas:N0}";
            lblKpiCantDocs.Text = $"Con {cantDocs} documentos";

            lblKpiBoletas.Text = boletas.Count.ToString();
            lblKpiPctBoletas.Text = cantDocs > 0 ? $"{Math.Round((decimal)boletas.Count / cantDocs * 100, 0)}% del total" : "0% del total";

            lblKpiFacturas.Text = facturas.Count.ToString();
            lblKpiPctFacturas.Text = cantDocs > 0 ? $"{Math.Round((decimal)facturas.Count / cantDocs * 100, 0)}% del total" : "0% del total";

            lblKpiDevoluciones.Text = devoluciones.Count.ToString();
            lblKpiMontoDev.Text = $"$ {devoluciones.Sum(d => d.Total):N0}";
        }

        // BÚSQUEDA MANUAL CONSULTA LAS FECHAS SELECCIONADAS EN LOS PICKERS
        private void EjcutarBusqueda()
        {
            DateTime desde = dtpDesde.Value.Date;
            DateTime hasta = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);
            CargarVentas(desde, hasta);
        }

        private void LimpiarFiltros()
        {
            txtBuscarFolio.Text = "Ingrese folio, ticket o RUT";
            txtBuscarFolio.ForeColor = Color.Gray;
            cbTipoDocumento.SelectedIndex = 0;
            AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
        }

        // BÚSQUEDA LOCAL SÓLO EVALÚA NRODTE Y RUT (YA NO BUSCA POR IDTVE)
        private void FiltrarLocalmente()
        {
            var resultado = _ventasCargadas.AsQueryable();

            string textoBusqueda = txtBuscarFolio.Text.Trim().ToLower();
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
                v.idTve,
                v.Documento,
                v.nroDTE,
                v.FecDoc,
                v.Total,
                v.UserDTE,
                RuT = string.IsNullOrEmpty(v.RuT) ? "-" : v.RuT,
                RazonSocial = string.IsNullOrEmpty(v.RazonSocial) ? "Consumidor Final" : v.RazonSocial,
                Estado = v.status.Contains("Anulado") ? "Anulado" : "Pagado",
                ObjetoOriginal = v
            }).ToList();

            dgvVentas.DataSource = listaFinal;

            if (dgvVentas.Columns["idTve"] != null) { dgvVentas.Columns["idTve"].HeaderText = "ID VENTA"; dgvVentas.Columns["idTve"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvVentas.Columns["Documento"] != null) dgvVentas.Columns["Documento"].HeaderText = "DOCUMENTO";
            if (dgvVentas.Columns["nroDTE"] != null) { dgvVentas.Columns["nroDTE"].HeaderText = "FOLIO DTE"; dgvVentas.Columns["nroDTE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvVentas.Columns["FecDoc"] != null) { dgvVentas.Columns["FecDoc"].HeaderText = "FECHA / HORA"; dgvVentas.Columns["FecDoc"].DefaultCellStyle.Format = "dd-MM-yyyy HH:mm"; }
            if (dgvVentas.Columns["Total"] != null) { dgvVentas.Columns["Total"].HeaderText = "TOTAL"; dgvVentas.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; dgvVentas.Columns["Total"].DefaultCellStyle.ForeColor = Color.FromArgb(0, 102, 255); dgvVentas.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
            if (dgvVentas.Columns["UserDTE"] != null) dgvVentas.Columns["UserDTE"].HeaderText = "CAJERO";
            if (dgvVentas.Columns["RuT"] != null) dgvVentas.Columns["RuT"].HeaderText = "CLIENTE / RUT";
            if (dgvVentas.Columns["RazonSocial"] != null) dgvVentas.Columns["RazonSocial"].HeaderText = "RAZÓN SOCIAL";
            if (dgvVentas.Columns["Estado"] != null) { dgvVentas.Columns["Estado"].HeaderText = "ESTADO"; dgvVentas.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }

            if (dgvVentas.Columns["ObjetoOriginal"] != null) dgvVentas.Columns["ObjetoOriginal"].Visible = false;

            lblPaginadorInfo.Text = $"Mostrando {listaFinal.Count} de {_ventasCargadas.Count} registros";
        }

        private void DgvVentas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvVentas.CurrentRow != null && dgvVentas.CurrentRow.DataBoundItem != null)
            {
                dynamic item = dgvVentas.CurrentRow.DataBoundItem;
                TVE2607 venta = item.ObjetoOriginal;
                _ventaSeleccionada = venta;

                btnReimprimir.Enabled = true;
                btnNotaCredito.Enabled = !venta.Documento.Equals("Nota de Crédito Electrónica", StringComparison.OrdinalIgnoreCase) && !venta.status.Equals("Anulado NC", StringComparison.OrdinalIgnoreCase);

                lblDetTipoDoc.Text = venta.Documento;
                lblDetFolio.Text = $"#{venta.nroDTE:D6}";
                lblDetFecha.Text = venta.FecDoc.ToString("dd-MM-yyyy HH:mm");
                lblDetMedio.Text = string.IsNullOrEmpty(venta.UserDTE) ? "Bárbara" : venta.UserDTE;
                lblDetCliente.Text = string.IsNullOrEmpty(venta.RuT) ? "Consumidor Final" : venta.RuT;
                lblDetNeto.Text = $"$ {venta.Neto:N0}";
                lblDetIva.Text = $"$ {venta.IvA:N0}";
                lblDetTotal.Text = $"$ {venta.Total:N0}";
            }
            else
            {
                _ventaSeleccionada = null;
                btnReimprimir.Enabled = false;
                btnNotaCredito.Enabled = false;

                lblDetTipoDoc.Text = "-";
                lblDetFolio.Text = "-";
                lblDetFecha.Text = "-";
                lblDetMedio.Text = "-";
                lblDetCliente.Text = "-";
                lblDetNeto.Text = "$ 0";
                lblDetIva.Text = "$ 0";
                lblDetTotal.Text = "$ 0";
            }
        }

        private void BtnReimprimir_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            var itemsEjemplo = new List<DetalleCarrito>
            {
                new DetalleCarrito { ProductoID = 1, Nombre = "Glosa DTE - " + _ventaSeleccionada.Documento, PrecioUnitario = _ventaSeleccionada.Total, Cantidad = 1 }
            };

            FormTicketModal formTicket = new FormTicketModal(_ventaSeleccionada.nroDTE, _ventaSeleccionada.FecDoc, itemsEjemplo, _ventaSeleccionada.Total, _ventaSeleccionada.Total, 0, "Efectivo");
            formTicket.ShowDialog(this);
        }

        private void BtnNotaCredito_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            Form modalNC = new Form
            {
                Text = $"Emitir Nota de Crédito - Referencia a Folio {_ventaSeleccionada.nroDTE}",
                Size = new Size(460, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = "📄 Emitir Nota de Crédito Electrónica", Location = new Point(25, 18), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(185, 28, 28), AutoSize = true };
            Label lblInfo = new Label { Text = $"Documento de Origen: {_ventaSeleccionada.Documento} N° {_ventaSeleccionada.nroDTE}\nMonto Total: ${_ventaSeleccionada.Total:N0} (Neto: ${_ventaSeleccionada.Neto:N0} | IVA: ${_ventaSeleccionada.IvA:N0})", Location = new Point(25, 48), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), AutoSize = true };

            Label lblCausa = new Label { Text = "Código de Referencia SII:", Location = new Point(25, 100), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbCausa = new ComboBox { Location = new Point(25, 122), Size = new Size(390, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbCausa.Items.AddRange(new string[] { "1 - Anula Documento de Referencia (Anulación / Devolución Total)", "2 - Corregir Texto en Documento (Glosa / Datos)", "3 - Corregir Montos / Descuento Parcial" });
            cbCausa.SelectedIndex = 0;

            Label lblMotivo = new Label { Text = "Motivo / Glosa de la Anulación:", Location = new Point(25, 165), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtMotivo = new TextBox { Text = "Devolución de mercadería / Anulación de venta", Location = new Point(25, 187), Size = new Size(390, 28), Font = new Font("Segoe UI", 9.5F) };

            CheckBox chkRestock = new CheckBox { Text = "🔄 Reintegrar automáticamente los productos al stock del inventario", Location = new Point(25, 230), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161) };

            Button btnEmitirNC = CrearBotonRedondeado("⚡ GENERAR Y EMITIR NOTA DE CRÉDITO", Color.FromArgb(220, 38, 38), Color.White, new Size(390, 48), 8);
            btnEmitirNC.Location = new Point(25, 310);

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
                        TVE2607 nc = new TVE2607
                        {
                            idLocal = _ventaSeleccionada.idLocal,
                            nmbLocal = _ventaSeleccionada.nmbLocal,
                            iddocDTE = 61,
                            Documento = "Nota de Crédito Electrónica",
                            nroDTE = (int)(DateTime.Now.Ticks % 100000),
                            FecDoc = DateTime.Now,
                            HoraDoc = DateTime.Now.ToString("HH:mm:ss"),
                            SubTotal = _ventaSeleccionada.SubTotal,
                            Descuento = _ventaSeleccionada.Descuento,
                            Neto = _ventaSeleccionada.Neto,
                            IvA = _ventaSeleccionada.IvA,
                            Total = _ventaSeleccionada.Total,
                            RuT = _ventaSeleccionada.RuT,
                            RazonSocial = _ventaSeleccionada.RazonSocial,
                            Giro = _ventaSeleccionada.Giro,
                            idREF = _ventaSeleccionada.idTve,
                            nroREF = _ventaSeleccionada.nroDTE,
                            codigoREF = (cbCausa.SelectedIndex + 1).ToString(),
                            UserDTE = _ventaSeleccionada.UserDTE,
                            Vendedor = _ventaSeleccionada.Vendedor,
                            status = "Emitido"
                        };

                        db.TVE2607.Add(nc);
                        db.SaveChanges();

                        var detallesOrigen = db.TVD2607.Where(d => d.idTve == _ventaSeleccionada.idTve).ToList();

                        foreach (var item in detallesOrigen)
                        {
                            TVD2607 ncDetalle = new TVD2607
                            {
                                idTve = nc.idTve,
                                idLocal = item.idLocal,
                                iddocDTE = 61,
                                Documento = "Nota de Crédito Electrónica",
                                NroDTE = nc.nroDTE,
                                FecMoV = DateTime.Now,
                                HoraMoV = DateTime.Now.ToString("HH:mm:ss"),
                                IdProducto = item.IdProducto,
                                NmbProducto = item.NmbProducto,
                                Cantidad = item.Cantidad,
                                Precio = item.Precio,
                                PneTo = item.PneTo,
                                SubTotal = item.SubTotal,
                                SubNeto = item.SubNeto,
                                nmbVendedor = item.nmbVendedor
                            };
                            db.TVD2607.Add(ncDetalle);

                            if (chkRestock.Checked)
                            {
                                var prod = db.Productos.FirstOrDefault(p => p.ProductoID == item.IdProducto);
                                if (prod != null)
                                {
                                    prod.Stock += item.Cantidad;
                                }
                            }
                        }

                        var ventaOriginal = db.TVE2607.FirstOrDefault(v => v.idTve == _ventaSeleccionada.idTve);
                        if (ventaOriginal != null)
                        {
                            ventaOriginal.status = "Anulado NC";
                        }

                        db.SaveChanges();
                    }

                    MessageBox.Show($"¡Nota de Crédito emitida con éxito!\nSe devolvieron los productos al stock.", "DTE Emitido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modalNC.Close();
                    AplicarFiltroFecha(dtpDesde.Value.Date, dtpHasta.Value.Date.AddDays(1).AddTicks(-1));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al emitir Nota de Crédito: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modalNC.Controls.AddRange(new Control[] { lblTitle, lblInfo, lblCausa, cbCausa, lblMotivo, txtMotivo, chkRestock, btnEmitirNC });
            modalNC.ShowDialog();
        }
    }
}