using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormLibroVentas : Form
    {
        private AppDbContext _db = new AppDbContext();

        // Controles de Filtro
        private ComboBox cbMes = null!;
        private ComboBox cbAnio = null!;
        private Button btnConsultar = null!;
        private Button btnExportarCSV = null!;
        private Button btnImprimirResumen = null!;
        private Button btnCargarDemoLVE = null!;

        // Tarjetas KPI Tributarias
        private Label lblKpiNeto = null!;
        private Label lblKpiIva = null!;
        private Label lblKpiNotasCredito = null!;
        private Label lblKpiTotalVentas = null!;

        // DataGridView y Resumen
        private DataGridView dgvLibro = null!;
        private Label lblPaginadorInfo = null!;

        // Tarjeta Resumen F29
        private Label lblF29BoletasCant = null!;
        private Label lblF29BoletasIva = null!;
        private Label lblF29FacturasCant = null!;
        private Label lblF29FacturasIva = null!;
        private Label lblF29NotasCreditoCant = null!;
        private Label lblF29NotasCreditoIva = null!;
        private Label lblF29DebitoFiscalTotal = null!;

        private List<Venta> _ventasMes = new List<Venta>();

        public FormLibroVentas()
        {
            InitializeComponent();
            InicializarFiltrosFechas();
            CargarLibroVentas();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 20),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            // Subtítulo del Módulo
            Label lblSubtitulo = new Label
            {
                Text = "Consolidado contable de DTEs y cálculo de Débito Fiscal IVA (Formulario F29 SII Chile)",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 24
            };

            Panel pnlFiltrosCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 14)
            };
            pnlFiltrosCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlFiltrosCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            FlowLayoutPanel flowFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            // Grupo Mes
            cbMes = new ComboBox
            {
                Size = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbMes.Items.AddRange(new string[] {
                "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
            });
            Panel pnlGrpMes = CrearGrupoFiltro("Mes Tributario", cbMes);

            // Grupo Año
            cbAnio = new ComboBox
            {
                Size = new Size(95, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            Panel pnlGrpAnio = CrearGrupoFiltro("Año", cbAnio);

            btnConsultar = CrearBotonEstilizado("🔍 Consultar", Color.FromArgb(2, 132, 199), Color.White, new Size(100, 32));
            btnConsultar.Margin = new Padding(6, 18, 6, 0);
            btnConsultar.Click += (s, e) => CargarLibroVentas();

            btnExportarCSV = CrearBotonEstilizado("📥 Exportar CSV", Color.FromArgb(16, 185, 129), Color.White, new Size(125, 32));
            btnExportarCSV.Margin = new Padding(6, 18, 6, 0);
            btnExportarCSV.Click += BtnExportarCSV_Click;

            btnImprimirResumen = CrearBotonEstilizado("🖨️ Imprimir F29", Color.FromArgb(51, 65, 85), Color.White, new Size(125, 32));
            btnImprimirResumen.Margin = new Padding(6, 18, 6, 0);
            btnImprimirResumen.Click += BtnImprimirResumen_Click;

            btnCargarDemoLVE = CrearBotonEstilizado("✨ Cargar Demo LVE", Color.FromArgb(124, 58, 237), Color.White, new Size(130, 32));
            btnCargarDemoLVE.Margin = new Padding(6, 18, 6, 0);
            btnCargarDemoLVE.Click += BtnCargarDemoLVE_Click;

            flowFiltros.Controls.AddRange(new Control[] {
                pnlGrpMes, pnlGrpAnio, btnConsultar, btnExportarCSV, btnImprimirResumen, btnCargarDemoLVE
            });
            pnlFiltrosCard.Controls.Add(flowFiltros);

            TableLayoutPanel gridKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 14)
            };
            gridKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            gridKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            gridKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            gridKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            Panel cardNeto = CrearKpiCard("🟢 TOTAL NETO (AFECTO)", "$ 0", Color.FromArgb(16, 185, 129), out lblKpiNeto);
            Panel cardIva = CrearKpiCard("🔵 DÉBITO FISCAL IVA (19%)", "$ 0", Color.FromArgb(2, 132, 199), out lblKpiIva);
            Panel cardNc = CrearKpiCard("🔴 DESCUENTOS POR N.C.", "$ 0", Color.FromArgb(220, 38, 38), out lblKpiNotasCredito);
            Panel cardTotal = CrearKpiCard("💳 TOTAL VENTAS PERIODO", "$ 0", Color.FromArgb(15, 23, 42), out lblKpiTotalVentas);

            gridKpis.Controls.Add(cardNeto, 0, 0);
            gridKpis.Controls.Add(cardIva, 1, 0);
            gridKpis.Controls.Add(cardNc, 2, 0);
            gridKpis.Controls.Add(cardTotal, 3, 0);

            TableLayoutPanel pnlGridAndF29 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 490,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            pnlGridAndF29.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            pnlGridAndF29.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            // A) TABLA LIBRO DE VENTAS
            Panel pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlTableCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTableCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvLibro);
            dgvLibro.CellFormatting += DgvLibro_CellFormatting;

            Panel pnlPaginador = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Padding = new Padding(5, 5, 5, 0)
            };

            lblPaginadorInfo = new Label
            {
                Text = "Mostrando registros...",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            pnlPaginador.Controls.Add(lblPaginadorInfo);

            pnlTableCard.Controls.Add(dgvLibro);
            pnlTableCard.Controls.Add(pnlPaginador);

            // B) PREVISUALIZACIÓN CASILLAS F29
            Panel pnlF29Card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14)
            };
            pnlF29Card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlF29Card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            Label lblTituloF29 = new Label
            {
                Text = "🇨🇱 Previsualización F29 (SII)",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 28
            };

            Label lblSubF29 = new Label
            {
                Text = "Resumen por casillas para la Declaración Mensual de IVA",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 22
            };

            Panel pnlCasillas = new Panel
            {
                Dock = DockStyle.Top,
                Height = 310,
                Padding = new Padding(0, 8, 0, 0)
            };

            int yPos = 0;
            CrearRenglonCasilla("Casilla 111 — Facturas Emitidas", out lblF29FacturasCant, out lblF29FacturasIva, ref yPos, pnlCasillas);
            CrearRenglonCasilla("Casilla 110 — Boletas Emitidas", out lblF29BoletasCant, out lblF29BoletasIva, ref yPos, pnlCasillas);
            CrearRenglonCasilla("Casilla 509 — Notas de Crédito", out lblF29NotasCreditoCant, out lblF29NotasCreditoIva, ref yPos, pnlCasillas);

            // Destacado Total Débito Fiscal
            Panel pnlTotalDebitoCard = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlTotalDebitoCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTotalDebitoCard.ClientRectangle, Color.FromArgb(186, 230, 253), ButtonBorderStyle.Solid);
            };

            Label lblTotalDebitoCap = new Label
            {
                Text = "DÉBITO FISCAL A DECLARAR (F29)",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(3, 105, 161),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 16
            };

            lblF29DebitoFiscalTotal = new Label
            {
                Text = "$ 0",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(2, 132, 199),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTotalDebitoCard.Controls.Add(lblF29DebitoFiscalTotal);
            pnlTotalDebitoCard.Controls.Add(lblTotalDebitoCap);

            pnlF29Card.Controls.Add(pnlTotalDebitoCard);
            pnlF29Card.Controls.Add(pnlCasillas);
            pnlF29Card.Controls.Add(lblSubF29);
            pnlF29Card.Controls.Add(lblTituloF29);

            pnlGridAndF29.Controls.Add(pnlTableCard, 0, 0);
            pnlGridAndF29.Controls.Add(pnlF29Card, 1, 0);

            // Footer
            Label lblFooter = new Label
            {
                Text = "🛡️ Sistema de Contabilidad y DTE   |   Libro de Ventas Electrónico v3.0",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlMain.Controls.Add(pnlGridAndF29);
            pnlMain.Controls.Add(gridKpis);
            pnlMain.Controls.Add(pnlFiltrosCard);
            pnlMain.Controls.Add(lblSubtitulo);

            this.Controls.Add(pnlMain);
            this.Controls.Add(lblFooter);

            this.ResumeLayout(false);
        }

        private Panel CrearGrupoFiltro(string titulo, Control control)
        {
            Panel pnl = new Panel
            {
                Size = new Size(control.Width + 4, 48),
                Margin = new Padding(0, 0, 8, 0)
            };

            Label lbl = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(0, 0),
                AutoSize = true
            };

            control.Location = new Point(0, 18);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);

            return pnl;
        }

        private Button CrearBotonEstilizado(string texto, Color back, Color fore, Size size)
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

        private Panel CrearKpiCard(string titulo, string valorInicial, Color colorAcento, out Label lblValor)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 10, 12, 10)
            };
            pnl.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 18
            };

            lblValor = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = colorAcento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(lblValor);
            pnl.Controls.Add(lblT);
            return pnl;
        }

        private void CrearRenglonCasilla(string titulo, out Label lblCant, out Label lblIva, ref int y, Panel container)
        {
            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, y),
                AutoSize = true
            };

            lblCant = new Label
            {
                Text = "Cant: 0 folios",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, y + 18),
                AutoSize = true
            };

            lblIva = new Label
            {
                Text = "IVA: $ 0",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(0, y + 34),
                AutoSize = true
            };

            container.Controls.Add(lblT);
            container.Controls.Add(lblCant);
            container.Controls.Add(lblIva);

            y += 65;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 32;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void DgvLibro_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = dgvLibro.Columns[e.ColumnIndex].Name;

            if (colName == "TipoDocumento" && e.Value != null)
            {
                string tipo = e.Value.ToString() ?? "";
                if (tipo.Contains("Nota de Crédito"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (tipo.Contains("Factura"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(3, 105, 161);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            }
        }

        private void InicializarFiltrosFechas()
        {
            int mesActual = DateTime.Today.Month;
            cbMes.SelectedIndex = mesActual - 1;

            int anioActual = DateTime.Today.Year;
            cbAnio.Items.Clear();
            for (int a = anioActual - 2; a <= anioActual + 1; a++)
            {
                cbAnio.Items.Add(a.ToString());
            }
            cbAnio.SelectedItem = anioActual.ToString();
        }

        private void CargarLibroVentas()
        {
            int mes = cbMes.SelectedIndex + 1;
            int anio = Convert.ToInt32(cbAnio.SelectedItem ?? DateTime.Today.Year.ToString());

            try
            {
                _ventasMes = _db.Ventas
                    .Where(v => v.Fecha.Month == mes && v.Fecha.Year == anio)
                    .OrderBy(v => v.Fecha)
                    .ToList();

                ActualizarGridYCalculos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el Libro de Ventas: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarGridYCalculos()
        {
            dgvLibro.DataSource = null;
            dgvLibro.DataSource = _ventasMes;

            // Formatear Columnas
            if (dgvLibro.Columns["FolioDTE"] != null) { dgvLibro.Columns["FolioDTE"].HeaderText = "Folio DTE"; dgvLibro.Columns["FolioDTE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvLibro.Columns["TipoDocumento"] != null) dgvLibro.Columns["TipoDocumento"].HeaderText = "Tipo Documento";
            if (dgvLibro.Columns["Fecha"] != null) { dgvLibro.Columns["Fecha"].HeaderText = "Fecha Emisión"; dgvLibro.Columns["Fecha"].DefaultCellStyle.Format = "dd-MM-yyyy HH:mm"; }
            if (dgvLibro.Columns["RutCliente"] != null) dgvLibro.Columns["RutCliente"].HeaderText = "RUT Cliente";
            if (dgvLibro.Columns["Neto"] != null) { dgvLibro.Columns["Neto"].HeaderText = "Neto"; dgvLibro.Columns["Neto"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvLibro.Columns["IVA"] != null) { dgvLibro.Columns["IVA"].HeaderText = "IVA (19%)"; dgvLibro.Columns["IVA"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvLibro.Columns["Total"] != null) { dgvLibro.Columns["Total"].HeaderText = "Total"; dgvLibro.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; dgvLibro.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }

            // Ocultar campos internos
            if (dgvLibro.Columns["VentaID"] != null) dgvLibro.Columns["VentaID"].Visible = false;
            if (dgvLibro.Columns["MedioPago"] != null) dgvLibro.Columns["MedioPago"].Visible = false;
            if (dgvLibro.Columns["Usuario"] != null) dgvLibro.Columns["Usuario"].Visible = false;
            if (dgvLibro.Columns["RazonSocial"] != null) dgvLibro.Columns["RazonSocial"].Visible = false;
            if (dgvLibro.Columns["Giro"] != null) dgvLibro.Columns["Giro"].Visible = false;
            if (dgvLibro.Columns["idREF"] != null) dgvLibro.Columns["idREF"].Visible = false;
            if (dgvLibro.Columns["nroREF"] != null) dgvLibro.Columns["nroREF"].Visible = false;
            if (dgvLibro.Columns["codigoREF"] != null) dgvLibro.Columns["codigoREF"].Visible = false;

            // Cálculos Totales
            var boletas = _ventasMes.Where(v => v.TipoDocumento.Contains("Boleta")).ToList();
            var facturas = _ventasMes.Where(v => v.TipoDocumento.Contains("Factura")).ToList();
            var notasCredito = _ventasMes.Where(v => v.TipoDocumento.Contains("Nota de Crédito")).ToList();

            decimal netoTotal = facturas.Sum(v => v.Neto) + boletas.Sum(v => v.Neto) - notasCredito.Sum(v => v.Neto);
            decimal ivaBoletas = boletas.Sum(v => v.IVA);
            decimal ivaFacturas = facturas.Sum(v => v.IVA);
            decimal ivaNotasCredito = notasCredito.Sum(v => v.IVA);
            decimal debitoFiscalTotal = (ivaFacturas + ivaBoletas) - ivaNotasCredito;
            decimal totalVentas = facturas.Sum(v => v.Total) + boletas.Sum(v => v.Total) - notasCredito.Sum(v => v.Total);

            // Actualizar KPIs
            lblKpiNeto.Text = $"$ {netoTotal:N0}";
            lblKpiIva.Text = $"$ {debitoFiscalTotal:N0}";
            lblKpiNotasCredito.Text = $"$ {notasCredito.Sum(v => v.Total):N0}";
            lblKpiTotalVentas.Text = $"$ {totalVentas:N0}";

            // Actualizar Desglose F29
            lblF29FacturasCant.Text = $"Cant: {facturas.Count} DTEs";
            lblF29FacturasIva.Text = $"IVA: $ {ivaFacturas:N0}";

            lblF29BoletasCant.Text = $"Cant: {boletas.Count} DTEs";
            lblF29BoletasIva.Text = $"IVA: $ {ivaBoletas:N0}";

            lblF29NotasCreditoCant.Text = $"Cant: {notasCredito.Count} DTEs";
            lblF29NotasCreditoIva.Text = $"IVA Descuento: -$ {ivaNotasCredito:N0}";

            lblF29DebitoFiscalTotal.Text = $"$ {debitoFiscalTotal:N0}";

            lblPaginadorInfo.Text = $"Mostrando {_ventasMes.Count} documentos en el período";
        }

        private void BtnExportarCSV_Click(object? sender, EventArgs e)
        {
            if (_ventasMes.Count == 0)
            {
                MessageBox.Show("No hay datos en el Libro de Ventas para exportar.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Libro_Ventas_{cbMes.SelectedItem}_{cbAnio.SelectedItem}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Folio DTE;Tipo Documento;Fecha Emision;RUT Cliente;Razon Social;Neto;IVA;Total");

                    foreach (var v in _ventasMes)
                    {
                        string rut = string.IsNullOrEmpty(v.RutCliente) ? "66666666-6" : v.RutCliente;
                        string razon = string.IsNullOrEmpty(v.RazonSocial) ? "Consumidor Final" : v.RazonSocial;
                        sb.AppendLine($"{v.FolioDTE};{v.TipoDocumento};{v.Fecha:dd/MM/yyyy HH:mm};{rut};{razon};{v.Neto};{v.IVA};{v.Total}");
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

        private void BtnImprimirResumen_Click(object? sender, EventArgs e)
        {
            if (_ventasMes.Count == 0)
            {
                MessageBox.Show("No hay datos cargados para generar el resumen de impresión F29.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                PrintDocument pd = new PrintDocument();
                pd.DocumentName = $"Resumen_F29_{cbMes.SelectedItem}_{cbAnio.SelectedItem}";
                pd.PrintPage += Pd_PrintPageF29;

                PrintPreviewDialog previewDialog = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 850,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterScreen,
                    Text = $"Vista Previa de Impresión — Resumen F29 SII ({cbMes.SelectedItem} {cbAnio.SelectedItem})"
                };

                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la vista previa de impresión: {ex.Message}", "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pd_PrintPageF29(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics!;
            Font fontHeaderTitle = new Font("Segoe UI", 16F, FontStyle.Bold);
            Font fontHeaderSub = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font fontSectionTitle = new Font("Segoe UI", 11F, FontStyle.Bold);
            Font fontBodyBold = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            Font fontBodyRegular = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            Font fontTableHead = new Font("Segoe UI", 9F, FontStyle.Bold);

            float y = 40;
            float marginX = 50;
            float pageWidth = e.PageBounds.Width - (marginX * 2);

            // Membrete Oficial
            g.DrawString("SERVICIO DE IMPUESTOS INTERNOS (SII)", fontHeaderSub, Brushes.Gray, marginX, y); y += 18;
            g.DrawString("FORMULARIO 29 — DECLARACIÓN MENSUAL Y PAGO CALZADO DE IVA", fontHeaderTitle, Brushes.Black, marginX, y); y += 30;
            g.DrawLine(Pens.DarkBlue, marginX, y, marginX + pageWidth, y); y += 15;

            // Datos de la Empresa y Período
            string mesSel = cbMes.SelectedItem?.ToString() ?? "";
            string anioSel = cbAnio.SelectedItem?.ToString() ?? "";

            g.DrawString($"R.U.T. EMPRESA: 76.543.210-K", fontBodyBold, Brushes.Black, marginX, y);
            g.DrawString($"PERÍODO TRIBUTARIO: {mesSel.ToUpper()} / {anioSel}", fontBodyBold, Brushes.Black, marginX + 350, y); y += 20;
            g.DrawString($"RAZÓN SOCIAL: SISTEMA POS DEMO S.A.", fontBodyRegular, Brushes.Black, marginX, y);
            g.DrawString($"FECHA EMISIÓN INFORME: {DateTime.Now:dd/MM/yyyy HH:mm}", fontBodyRegular, Brushes.Black, marginX + 350, y); y += 30;

            g.DrawRectangle(Pens.Gray, marginX, y, pageWidth, 28);
            using (SolidBrush headerBg = new SolidBrush(Color.FromArgb(240, 244, 248)))
            using (SolidBrush textTitleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillRectangle(headerBg, marginX + 1, y + 1, pageWidth - 1, 27);
                g.DrawString("DESGLOSE DE DÉBITO FISCAL POR TIPO DE DOCUMENTO (SECCIÓN VENTAS)", fontSectionTitle, textTitleBrush, marginX + 10, y + 5);
            }
            y += 40;

            // Tabla de Casillas F29
            var boletas = _ventasMes.Where(v => v.TipoDocumento.Contains("Boleta")).ToList();
            var facturas = _ventasMes.Where(v => v.TipoDocumento.Contains("Factura")).ToList();
            var notasCredito = _ventasMes.Where(v => v.TipoDocumento.Contains("Nota de Crédito")).ToList();

            decimal ivaFacturas = facturas.Sum(v => v.IVA);
            decimal ivaBoletas = boletas.Sum(v => v.IVA);
            decimal ivaNC = notasCredito.Sum(v => v.IVA);
            decimal totalDebito = (ivaFacturas + ivaBoletas) - ivaNC;

            float col1 = marginX + 10;
            float col2 = marginX + 280;
            float col3 = marginX + 380;
            float col4 = marginX + 500;

            // Encabezado Tabla
            g.DrawString("CONCEPTO / CASILLA SII", fontTableHead, Brushes.Black, col1, y);
            g.DrawString("CANT. FOLIOS", fontTableHead, Brushes.Black, col2, y);
            g.DrawString("BASE NETO", fontTableHead, Brushes.Black, col3, y);
            g.DrawString("DÉBITO FISCAL IVA", fontTableHead, Brushes.Black, col4, y);
            y += 20;
            g.DrawLine(Pens.LightGray, marginX, y, marginX + pageWidth, y); y += 10;

            // Filas
            g.DrawString("Casilla 111 — Facturas Emitidas", fontBodyRegular, Brushes.Black, col1, y);
            g.DrawString($"{facturas.Count}", fontBodyRegular, Brushes.Black, col2, y);
            g.DrawString($"${facturas.Sum(v => v.Neto):N0}", fontBodyRegular, Brushes.Black, col3, y);
            g.DrawString($"${ivaFacturas:N0}", fontBodyBold, Brushes.Black, col4, y); y += 22;

            g.DrawString("Casilla 110 — Boletas Emitidas", fontBodyRegular, Brushes.Black, col1, y);
            g.DrawString($"{boletas.Count}", fontBodyRegular, Brushes.Black, col2, y);
            g.DrawString($"${boletas.Sum(v => v.Neto):N0}", fontBodyRegular, Brushes.Black, col3, y);
            g.DrawString($"${ivaBoletas:N0}", fontBodyBold, Brushes.Black, col4, y); y += 22;

            g.DrawString("Casilla 509 — Notas de Crédito (Deducción)", fontBodyRegular, Brushes.DarkRed, col1, y);
            g.DrawString($"{notasCredito.Count}", fontBodyRegular, Brushes.DarkRed, col2, y);
            g.DrawString($"-${notasCredito.Sum(v => v.Neto):N0}", fontBodyRegular, Brushes.DarkRed, col3, y);
            g.DrawString($"-${ivaNC:N0}", fontBodyBold, Brushes.DarkRed, col4, y); y += 25;

            g.DrawLine(Pens.Black, marginX, y, marginX + pageWidth, y); y += 12;

            // Total Final
            using (SolidBrush blueAccentBrush = new SolidBrush(Color.FromArgb(2, 132, 199)))
            {
                g.DrawString("DÉBITO FISCAL DETERMINADO A DECLARAR (F29):", fontSectionTitle, blueAccentBrush, col1, y);
                g.DrawString($"${totalDebito:N0}", fontHeaderTitle, blueAccentBrush, col4 - 10, y - 5);
            }
            y += 45;

            // Pie de Página Firma
            g.DrawLine(Pens.DarkGray, marginX + 220, y + 60, marginX + 500, y + 60);
            g.DrawString("Firma Contador / Representante Legal", fontBodyRegular, Brushes.Gray, marginX + 250, y + 65);
            g.DrawString("Documento impreso desde el Sistema POS Moderno v3.0 — Válido para contabilidad interna", fontHeaderSub, Brushes.LightGray, marginX, e.PageBounds.Height - 50);
        }

        private void BtnCargarDemoLVE_Click(object? sender, EventArgs e)
        {
            try
            {
                int mes = cbMes.SelectedIndex + 1;
                int anio = Convert.ToInt32(cbAnio.SelectedItem ?? DateTime.Today.Year.ToString());
                DateTime baseDate = new DateTime(anio, mes, 5, 10, 30, 0);

                var ventasDemoLVE = new List<Venta>
                {
                    new Venta { Fecha = baseDate, Total = 15000, Neto = 12605, IVA = 2395, MedioPago = "Efectivo", TipoDocumento = "Boleta Electrónica", FolioDTE = 2001, Usuario = "barbara" },
                    new Venta { Fecha = baseDate.AddDays(2), Total = 85000, Neto = 71429, IVA = 13571, MedioPago = "Transferencia", TipoDocumento = "Factura Electrónica", FolioDTE = 601, RutCliente = "76.123.456-7", RazonSocial = "INVERSIONES DEL SUR SPALTD", Usuario = "barbara" },
                    new Venta { Fecha = baseDate.AddDays(5), Total = 22000, Neto = 18487, IVA = 3513, MedioPago = "Tarjeta Débito", TipoDocumento = "Boleta Electrónica", FolioDTE = 2002, Usuario = "victor" },
                    new Venta { Fecha = baseDate.AddDays(8), Total = 12000, Neto = 10084, IVA = 1916, MedioPago = "Nota de Crédito (Efectivo)", TipoDocumento = "Nota de Crédito Electrónica", FolioDTE = 301, idREF = 2001, nroREF = 2001, codigoREF = "1", Usuario = "barbara" }
                };

                int agregados = 0;
                foreach (var v in ventasDemoLVE)
                {
                    bool existe = _db.Ventas.Any(x => x.FolioDTE == v.FolioDTE && x.TipoDocumento == v.TipoDocumento);
                    if (!existe)
                    {
                        _db.Ventas.Add(v);
                        agregados++;
                    }
                }

                if (agregados > 0)
                {
                    _db.SaveChanges();
                    MessageBox.Show($"¡Se generaron {agregados} DTEs contables de prueba para {cbMes.SelectedItem} {cbAnio.SelectedItem}!", "Éxito Demo LVE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarLibroVentas();
                }
                else
                {
                    MessageBox.Show($"Los datos de prueba para el período {cbMes.SelectedItem} {cbAnio.SelectedItem} ya existen.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos demo LVE: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}