using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormLibroCompras : Form
    {
        // Controles de Filtros
        private TextBox txtFiltroTexto = null!;
        private DateTimePicker dtpFechaDesde = null!;
        private DateTimePicker dtpFechaHasta = null!;
        private ComboBox cbTipoDoc = null!;
        private ComboBox cbEstado = null!;
        private Button btnBuscar = null!;
        private Button btnLimpiar = null!;
        private Button btnExportarCsv = null!;
        private Button btnExportarExcel = null!;

        // Filtros Rápidos
        private Button btnFiltroHoy = null!;
        private Button btnFiltroAyer = null!;
        private Button btnFiltro7Dias = null!;
        private Button btnFiltroEsteMes = null!;
        private Button btnFiltroMesAnterior = null!;

        // Tarjetas KPI Superiores
        private Label lblKpiComprasMonto = null!;
        private Label lblKpiFacturasCant = null!;
        private Label lblKpiIvaMonto = null!;
        private Label lblKpiProveedoresCant = null!;

        // Tabla Principal
        private DataGridView dgvLibro = null!;
        private Label lblContadorFooter = null!;

        private List<Compra> _comprasCargadas = new List<Compra>();

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
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 10, 16, 14),
                AutoScroll = true
            };

            // 1. Tarjetas KPI Superiores (Optimizadas en altura)
            Panel pnlKpis = CrearTarjetasKpi();

            // 2. Tarjeta de Filtros
            Panel pnlFiltros = CrearSeccionFiltros();

            // 3. Grilla Principal (Aprovecha toda la altura)
            Panel pnlGrilla = CrearSeccionGrilla();

            pnlMain.Controls.Add(pnlGrilla);
            pnlMain.Controls.Add(pnlFiltros);
            pnlMain.Controls.Add(pnlKpis);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearTarjetasKpi()
        {
            Panel pnlWrapper = new Panel { Dock = DockStyle.Top, Height = 86, Margin = new Padding(0, 0, 0, 10) };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // KPI 1: Total Compras
            tlp.Controls.Add(CrearCardKpi("💲", Color.FromArgb(220, 252, 231), Color.FromArgb(22, 163, 74), "Compras del período", out lblKpiComprasMonto, "$ 0", "Total neto"), 0, 0);

            // KPI 2: Cantidad Facturas
            tlp.Controls.Add(CrearCardKpi("📄", Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), "Facturas recibidas", out lblKpiFacturasCant, "0", "100% del total"), 1, 0);

            // KPI 3: IVA Crédito
            tlp.Controls.Add(CrearCardKpi("🏷️", Color.FromArgb(243, 232, 255), Color.FromArgb(147, 51, 234), "IVA Crédito Fiscal", out lblKpiIvaMonto, "$ 0", "Total IVA recuperable"), 2, 0);

            // KPI 4: Proveedores Activos
            tlp.Controls.Add(CrearCardKpi("👥", Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6), "Proveedores activos", out lblKpiProveedoresCant, "0", "Con compras en el período"), 3, 0);

            pnlWrapper.Controls.Add(tlp);
            return pnlWrapper;
        }

        private Panel CrearCardKpi(string icono, Color bgIcon, Color iconColor, string titulo, out Label lblValor, string valorInicial, string subtitulo)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4, 0, 4, 0),
                Padding = new Padding(12, 8, 12, 8)
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblIcon = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 13F),
                BackColor = bgIcon,
                ForeColor = iconColor,
                Size = new Size(38, 38),
                Location = new Point(12, 12),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblTit = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(58, 8),
                AutoSize = true
            };

            lblValor = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(56, 24),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(58, 48),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblTit, lblValor, lblSub });
            return card;
        }

        private Panel CrearSeccionFiltros()
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.White,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(0, 0, 0, 10)
            };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            // Fila 1: Filtros
            FlowLayoutPanel flp1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            Panel pnlTxt = new Panel { Size = new Size(240, 46), Margin = new Padding(0, 0, 8, 0) };
            Label l1 = new Label { Text = "PROVEEDOR / FOLIO / RUT", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            txtFiltroTexto = new TextBox { PlaceholderText = "🔍 Ingrese proveedor, folio o RUT...", Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            txtFiltroTexto.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) CargarDatosLibro(); };
            pnlTxt.Controls.AddRange(new Control[] { txtFiltroTexto, l1 });

            Panel pnlDesde = new Panel { Size = new Size(115, 46), Margin = new Padding(0, 0, 8, 0) };
            Label l2 = new Label { Text = "FECHA DESDE", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            dtpFechaDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            pnlDesde.Controls.AddRange(new Control[] { dtpFechaDesde, l2 });

            Panel pnlHasta = new Panel { Size = new Size(115, 46), Margin = new Padding(0, 0, 8, 0) };
            Label l3 = new Label { Text = "FECHA HASTA", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            dtpFechaHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            pnlHasta.Controls.AddRange(new Control[] { dtpFechaHasta, l3 });

            Panel pnlTipo = new Panel { Size = new Size(150, 46), Margin = new Padding(0, 0, 8, 0) };
            Label l4 = new Label { Text = "TIPO DOCUMENTO", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            cbTipoDoc = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            cbTipoDoc.Items.AddRange(new string[] { "Todos", "Factura de Compra", "Factura Exenta", "Boleta de Compra", "Guía de Recepción" });
            cbTipoDoc.SelectedIndex = 0;
            pnlTipo.Controls.AddRange(new Control[] { cbTipoDoc, l4 });

            Panel pnlEstado = new Panel { Size = new Size(110, 46), Margin = new Padding(0, 0, 10, 0) };
            Label l5 = new Label { Text = "ESTADO", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            cbEstado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            cbEstado.Items.AddRange(new string[] { "Todos", "Recibida", "Anulada" });
            cbEstado.SelectedIndex = 0;
            pnlEstado.Controls.AddRange(new Control[] { cbEstado, l5 });

            btnBuscar = new Button { Text = "🔍 Buscar", Size = new Size(95, 26), BackColor = Color.FromArgb(190, 24, 93), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 14, 6, 0) };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarDatosLibro();

            btnLimpiar = new Button { Text = "🧹 Limpiar", Size = new Size(90, 26), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 14, 0, 0) };
            btnLimpiar.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnLimpiar.Click += BtnLimpiar_Click;

            flp1.Controls.AddRange(new Control[] { pnlTxt, pnlDesde, pnlHasta, pnlTipo, pnlEstado, btnBuscar, btnLimpiar });

            // Fila 2: Filtros Rápidos y Exportar
            Panel pnlFila2 = new Panel { Dock = DockStyle.Bottom, Height = 32 };

            FlowLayoutPanel flpRapidos = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
            Label lblFRap = new Label { Text = "Filtros rápidos:", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Margin = new Padding(0, 5, 6, 0), AutoSize = true };

            btnFiltroHoy = CrearBotonFiltroRapido("📅 Hoy", (s, e) => { dtpFechaDesde.Value = DateTime.Today; dtpFechaHasta.Value = DateTime.Today; CargarDatosLibro(); });
            btnFiltroAyer = CrearBotonFiltroRapido("📅 Ayer", (s, e) => { dtpFechaDesde.Value = DateTime.Today.AddDays(-1); dtpFechaHasta.Value = DateTime.Today.AddDays(-1); CargarDatosLibro(); });
            btnFiltro7Dias = CrearBotonFiltroRapido("📅 Últimos 7 días", (s, e) => { dtpFechaDesde.Value = DateTime.Today.AddDays(-7); dtpFechaHasta.Value = DateTime.Today; CargarDatosLibro(); });
            btnFiltroEsteMes = CrearBotonFiltroRapido("📅 Este mes", (s, e) => { ConfigurarFiltroEsteMes(); CargarDatosLibro(); });
            btnFiltroMesAnterior = CrearBotonFiltroRapido("📅 Mes anterior", (s, e) => { ConfigurarFiltroMesAnterior(); CargarDatosLibro(); });

            flpRapidos.Controls.AddRange(new Control[] { lblFRap, btnFiltroHoy, btnFiltroAyer, btnFiltro7Dias, btnFiltroEsteMes, btnFiltroMesAnterior });

            FlowLayoutPanel flpExport = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false };
            btnExportarCsv = new Button { Text = "📄 Exportar CSV", Size = new Size(115, 26), BackColor = Color.FromArgb(240, 253, 244), ForeColor = Color.FromArgb(22, 163, 74), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0) };
            btnExportarCsv.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnExportarCsv.Click += (s, e) => ExportarACSV();

            btnExportarExcel = new Button { Text = "📊 Exportar Excel", Size = new Size(120, 26), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(30, 41, 59), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0) };
            btnExportarExcel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnExportarExcel.Click += (s, e) => ExportarACSV();

            flpExport.Controls.AddRange(new Control[] { btnExportarCsv, btnExportarExcel });

            pnlFila2.Controls.Add(flpRapidos);
            pnlFila2.Controls.Add(flpExport);

            pnl.Controls.Add(pnlFila2);
            pnl.Controls.Add(flp1);
            return pnl;
        }

        private Panel CrearSeccionGrilla()
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(0, 6, 0, 0) };
            lblContadorFooter = new Label { Text = "Mostrando 0 documentos", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Left, AutoSize = true };
            pnlFooter.Controls.Add(lblContadorFooter);

            dgvLibro = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, // Se configuran anchos exactos
                RowTemplate = { Height = 32 },
                GridColor = Color.FromArgb(241, 245, 249)
            };
            
            dgvLibro.DoubleClick += DgvLibro_DoubleClick;

            ConfigurarColumnasTabla();

            pnl.Controls.Add(dgvLibro);
            pnl.Controls.Add(pnlFooter);
            return pnl;
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

            // Anchos y Alineaciones bien proporcionados para evitar texto cortado
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
            dgvLibro.Columns["Rut"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

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
                using var db = new AppDbContext();
                DateTime fDesde = dtpFechaDesde.Value.Date;
                DateTime fHasta = dtpFechaHasta.Value.Date.AddDays(1).AddTicks(-1);

                var query = db.Compras
                    .Where(c => c.FechaEmision >= fDesde && c.FechaEmision <= fHasta)
                    .AsQueryable();

                string filtro = txtFiltroTexto.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(filtro))
                {
                    query = query.Where(c => c.RazonSocialProveedor.ToLower().Contains(filtro) ||
                                             c.RutProveedor.ToLower().Contains(filtro) ||
                                             c.NroFacturaProveedor.ToString().Contains(filtro));
                }

                if (cbTipoDoc.SelectedIndex > 0)
                {
                    string tipoSel = cbTipoDoc.SelectedItem!.ToString()!;
                    query = query.Where(c => c.TipoDocumento == tipoSel);
                }

                if (cbEstado.SelectedIndex > 0)
                {
                    string estadoSel = cbEstado.SelectedItem!.ToString()!;
                    query = query.Where(c => c.Estado == estadoSel);
                }

                _comprasCargadas = query.OrderByDescending(c => c.FechaEmision).ToList();

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

                decimal totalNeto = _comprasCargadas.Sum(c => c.MontoNeto);
                decimal totalIva = _comprasCargadas.Sum(c => c.MontoIva);
                int totalFacturas = _comprasCargadas.Count;
                int totalProveedores = _comprasCargadas.Select(c => c.RutProveedor).Distinct().Count();

                lblKpiComprasMonto.Text = $"$ {totalNeto:N0}";
                lblKpiFacturasCant.Text = totalFacturas.ToString("N0");
                lblKpiIvaMonto.Text = $"$ {totalIva:N0}";
                lblKpiProveedoresCant.Text = totalProveedores.ToString("N0");

                lblContadorFooter.Text = $"Mostrando {_comprasCargadas.Count} documento(s) en el período seleccionado.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el Libro de Compras: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private Button CrearBotonFiltroRapido(string texto, EventHandler onClick)
        {
            Button b = new Button
            {
                Text = texto,
                Height = 26,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                AutoSize = true,
                Margin = new Padding(0, 0, 4, 0)
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            b.Click += onClick;
            return b;
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
                    MessageBox.Show("Libro de Compras exportado exitosamente.", "Exportación Completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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