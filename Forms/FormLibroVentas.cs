using System;
using System.Collections.Generic;
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

        private ComboBox cbMes = null!;
        private ComboBox cbAnio = null!;
        private Button btnConsultar = null!;
        private Button btnExportarCSV = null!;
        private Button btnImprimirResumen = null!;

        private Label lblKpiNeto = null!;
        private Label lblKpiIva = null!;
        private Label lblKpiNotasCredito = null!;
        private Label lblKpiTotalVentas = null!;

        private DataGridView dgvLibro = null!;
        private Label lblPaginadorInfo = null!;

        private Label lblF29BoletasCant = null!;
        private Label lblF29BoletasIva = null!;
        private Label lblF29FacturasCant = null!;
        private Label lblF29FacturasIva = null!;
        private Label lblF29NotasCreditoCant = null!;
        private Label lblF29NotasCreditoIva = null!;
        private Label lblF29DebitoFiscalTotal = null!;

        private List<TVE2607> _ventasMes = new List<TVE2607>();

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

            Label lblSubtitulo = new Label
            {
                Text = "Consolidado contable de DTEs y cálculo de Débito Fiscal IVA (Formulario F29) desde TVE2607",
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

            cbMes = new ComboBox { Size = new Size(130, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbMes.Items.AddRange(new string[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" });
            Panel pnlGrpMes = CrearGrupoFiltro("Mes Tributario", cbMes);

            cbAnio = new ComboBox { Size = new Size(95, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlGrpAnio = CrearGrupoFiltro("Año", cbAnio);

            btnConsultar = CrearBotonEstilizado("🔍 Consultar", Color.FromArgb(2, 132, 199), Color.White, new Size(100, 32));
            btnConsultar.Margin = new Padding(6, 18, 6, 0);
            btnConsultar.Click += (s, e) => CargarLibroVentas();

            btnExportarCSV = CrearBotonEstilizado("📥 Exportar CSV", Color.FromArgb(16, 185, 129), Color.White, new Size(125, 32));
            btnExportarCSV.Margin = new Padding(6, 18, 6, 0);
            btnExportarCSV.Click += BtnExportarCSV_Click;

            btnImprimirResumen = CrearBotonEstilizado("🖨️ Imprimir F29", Color.FromArgb(51, 65, 85), Color.White, new Size(125, 32));
            btnImprimirResumen.Margin = new Padding(6, 18, 6, 0);

            flowFiltros.Controls.AddRange(new Control[] { pnlGrpMes, pnlGrpAnio, btnConsultar, btnExportarCSV, btnImprimirResumen });
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

            Panel pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlTableCard.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlTableCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

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

            Panel pnlPaginador = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(5, 5, 5, 0) };
            lblPaginadorInfo = new Label { Text = "Mostrando registros...", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true };
            pnlPaginador.Controls.Add(lblPaginadorInfo);

            pnlTableCard.Controls.Add(dgvLibro);
            pnlTableCard.Controls.Add(pnlPaginador);

            Panel pnlF29Card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14) };
            pnlF29Card.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlF29Card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            Label lblTituloF29 = new Label { Text = "🇨🇱 Previsualización F29 (SII)", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 28 };
            Label lblSubF29 = new Label { Text = "Resumen por casillas para la Declaración Mensual de IVA", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 22 };

            Panel pnlCasillas = new Panel { Dock = DockStyle.Top, Height = 310, Padding = new Padding(0, 8, 0, 0) };
            int yPos = 0;
            CrearRenglonCasilla("Casilla 111 — Facturas Emitidas", out lblF29FacturasCant, out lblF29FacturasIva, ref yPos, pnlCasillas);
            CrearRenglonCasilla("Casilla 110 — Boletas Emitidas", out lblF29BoletasCant, out lblF29BoletasIva, ref yPos, pnlCasillas);
            CrearRenglonCasilla("Casilla 509 — Notas de Crédito", out lblF29NotasCreditoCant, out lblF29NotasCreditoIva, ref yPos, pnlCasillas);

            Panel pnlTotalDebitoCard = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(240, 249, 255), Padding = new Padding(10, 6, 10, 6) };
            pnlTotalDebitoCard.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlTotalDebitoCard.ClientRectangle, Color.FromArgb(186, 230, 253), ButtonBorderStyle.Solid); };

            Label lblTotalDebitoCap = new Label { Text = "DÉBITO FISCAL A DECLARAR (F29)", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 16 };
            lblF29DebitoFiscalTotal = new Label { Text = "$ 0", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

            pnlTotalDebitoCard.Controls.Add(lblF29DebitoFiscalTotal);
            pnlTotalDebitoCard.Controls.Add(lblTotalDebitoCap);

            pnlF29Card.Controls.Add(pnlTotalDebitoCard);
            pnlF29Card.Controls.Add(pnlCasillas);
            pnlF29Card.Controls.Add(lblSubF29);
            pnlF29Card.Controls.Add(lblTituloF29);

            pnlGridAndF29.Controls.Add(pnlTableCard, 0, 0);
            pnlGridAndF29.Controls.Add(pnlF29Card, 1, 0);

            pnlMain.Controls.Add(pnlGridAndF29);
            pnlMain.Controls.Add(gridKpis);
            pnlMain.Controls.Add(pnlFiltrosCard);
            pnlMain.Controls.Add(lblSubtitulo);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearGrupoFiltro(string titulo, Control control)
        {
            Panel pnl = new Panel { Size = new Size(control.Width + 4, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };
            control.Location = new Point(0, 18);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private Button CrearBotonEstilizado(string texto, Color back, Color fore, Size size)
        {
            Button b = new Button { Text = texto, Size = size, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Panel CrearKpiCard(string titulo, string valorInicial, Color colorAcento, out Label lblValor)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12, 10, 12, 10) };
            pnl.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 18 };
            lblValor = new Label { Text = valorInicial, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = colorAcento, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            pnl.Controls.Add(lblValor);
            pnl.Controls.Add(lblT);
            return pnl;
        }

        private void CrearRenglonCasilla(string titulo, out Label lblCant, out Label lblIva, ref int y, Panel container)
        {
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, y), AutoSize = true };
            lblCant = new Label { Text = "Cant: 0 folios", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, y + 18), AutoSize = true };
            lblIva = new Label { Text = "IVA: $ 0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(0, y + 34), AutoSize = true };
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

        private void InicializarFiltrosFechas()
        {
            int mesActual = DateTime.Today.Month;
            cbMes.SelectedIndex = mesActual - 1;

            int anioActual = DateTime.Today.Year;
            cbAnio.Items.Clear();
            for (int a = anioActual - 2; a <= anioActual + 1; a++) cbAnio.Items.Add(a.ToString());
            cbAnio.SelectedItem = anioActual.ToString();
        }

        private void CargarLibroVentas()
        {
            int mes = cbMes.SelectedIndex + 1;
            int anio = Convert.ToInt32(cbAnio.SelectedItem ?? DateTime.Today.Year.ToString());

            try
            {
                _ventasMes = _db.TVE2607
                    .Where(v => v.FecDoc.Month == mes && v.FecDoc.Year == anio)
                    .OrderBy(v => v.FecDoc)
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

            if (dgvLibro.Columns["nroDTE"] != null) { dgvLibro.Columns["nroDTE"].HeaderText = "Folio DTE"; dgvLibro.Columns["nroDTE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvLibro.Columns["Documento"] != null) dgvLibro.Columns["Documento"].HeaderText = "Tipo Documento";
            if (dgvLibro.Columns["FecDoc"] != null) { dgvLibro.Columns["FecDoc"].HeaderText = "Fecha Emisión"; dgvLibro.Columns["FecDoc"].DefaultCellStyle.Format = "dd-MM-yyyy HH:mm"; }
            if (dgvLibro.Columns["RuT"] != null) dgvLibro.Columns["RuT"].HeaderText = "RUT Cliente";
            if (dgvLibro.Columns["Neto"] != null) { dgvLibro.Columns["Neto"].HeaderText = "Neto"; dgvLibro.Columns["Neto"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvLibro.Columns["IvA"] != null) { dgvLibro.Columns["IvA"].HeaderText = "IVA (19%)"; dgvLibro.Columns["IvA"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvLibro.Columns["Total"] != null) { dgvLibro.Columns["Total"].HeaderText = "Total"; dgvLibro.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; dgvLibro.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }

            string[] ocultar = new string[] { "idTve", "idLocal", "nmbLocal", "iddocDTE", "nroInT", "SubTotal", "Descuento", "Impto1", "Impto2", "Impto3", "UserDTE", "Vendedor", "nroZ", "Url", "nPAX", "Idcliente", "DNI", "dv", "RazonSocial", "Giro", "Direccion", "idcomuna", "nComuna", "idCiudad", "nCiudad", "Fono1", "Fono2", "email", "status", "idREF", "nroREF", "codigoREF", "FechaREF", "HoraDoc", "Detalles" };
            foreach (var col in ocultar)
            {
                if (dgvLibro.Columns[col] != null) dgvLibro.Columns[col].Visible = false;
            }

            var boletas = _ventasMes.Where(v => v.Documento != null && v.Documento.Contains("Boleta")).ToList();
            var facturas = _ventasMes.Where(v => v.Documento != null && v.Documento.Contains("Factura")).ToList();
            var notasCredito = _ventasMes.Where(v => v.Documento != null && v.Documento.Contains("Nota de Crédito")).ToList();

            decimal netoTotal = facturas.Sum(v => v.Neto) + boletas.Sum(v => v.Neto) - notasCredito.Sum(v => v.Neto);
            decimal ivaBoletas = boletas.Sum(v => v.IvA);
            decimal ivaFacturas = facturas.Sum(v => v.IvA);
            decimal ivaNotasCredito = notasCredito.Sum(v => v.IvA);
            decimal debitoFiscalTotal = (ivaFacturas + ivaBoletas) - ivaNotasCredito;
            decimal totalVentas = facturas.Sum(v => v.Total) + boletas.Sum(v => v.Total) - notasCredito.Sum(v => v.Total);

            lblKpiNeto.Text = $"$ {netoTotal:N0}";
            lblKpiIva.Text = $"$ {debitoFiscalTotal:N0}";
            lblKpiNotasCredito.Text = $"$ {notasCredito.Sum(v => v.Total):N0}";
            lblKpiTotalVentas.Text = $"$ {totalVentas:N0}";

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

            SaveFileDialog sfd = new SaveFileDialog { Filter = "Archivo CSV (*.csv)|*.csv", FileName = $"Libro_Ventas_{cbMes.SelectedItem}_{cbAnio.SelectedItem}.csv" };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Folio DTE;Tipo Documento;Fecha Emision;RUT Cliente;Razon Social;Neto;IVA;Total");

                    foreach (var v in _ventasMes)
                    {
                        string rut = string.IsNullOrEmpty(v.RuT) ? "66666666-6" : v.RuT;
                        string razon = string.IsNullOrEmpty(v.RazonSocial) ? "Consumidor Final" : v.RazonSocial;
                        sb.AppendLine($"{v.nroDTE};{v.Documento};{v.FecDoc:dd/MM/yyyy HH:mm};{rut};{razon};{v.Neto};{v.IvA};{v.Total}");
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
    }
}