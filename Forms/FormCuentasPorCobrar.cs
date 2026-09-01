using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormCuentasPorCobrar : Form
    {
        private TextBox txtBuscar = null!;
        private ComboBox cbEstado = null!;
        private DateTimePicker dtpDesde = null!;
        private DateTimePicker dtpHasta = null!;
        private Button btnBuscar = null!;
        private Button btnLimpiar = null!;

        // Botonera de acciones
        private Button btnRegistrarAbono = null!;
        private Button btnHistorialPagos = null!;
        private Button btnDesbloquearCliente = null!;

        // KPIs
        private Label lblKpiTotalDeuda = null!;
        private Label lblKpiDeudaVencida = null!;
        private Label lblKpiDocsPendientes = null!;
        private Label lblKpiClientesMora = null!;

        private DataGridView dgvCxC = null!;
        private Label lblFooterStatus = null!;

        private List<CuentaPorCobrar> _cxcCargadas = new List<CuentaPorCobrar>();

        public FormCuentasPorCobrar()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            ConfigurarFiltroEsteMes();
            CargarCuentasPorCobrar();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 16) };

            Panel pnlKpis = CrearSeccionKpis();
            Panel pnlFiltros = CrearSeccionFiltros();
            Panel pnlGrilla = CrearSeccionGrilla();

            pnlMain.Controls.Add(pnlGrilla);
            pnlMain.Controls.Add(pnlFiltros);
            pnlMain.Controls.Add(pnlKpis);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearSeccionKpis()
        {
            Panel pnlWrapper = new Panel { Dock = DockStyle.Top, Height = 80, Margin = new Padding(0, 0, 0, 12) };
            TableLayoutPanel tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            tlp.Controls.Add(CrearCardKpi("💰", Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), "Total Cartera Deuda", out lblKpiTotalDeuda, "$ 0", "Saldo pendiente total"), 0, 0);
            tlp.Controls.Add(CrearCardKpi("⚠️", Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38), "Deuda Vencida (Mora)", out lblKpiDeudaVencida, "$ 0", "Documentos fuera de plazo"), 1, 0);
            tlp.Controls.Add(CrearCardKpi("📄", Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6), "Documentos Pendientes", out lblKpiDocsPendientes, "0", "Facturas y Boletas a plazo"), 2, 0);
            tlp.Controls.Add(CrearCardKpi("👥", Color.FromArgb(243, 232, 255), Color.FromArgb(147, 51, 234), "Clientes con Mora", out lblKpiClientesMora, "0", "Clientes restringidos"), 3, 0);

            pnlWrapper.Controls.Add(tlp);
            return pnlWrapper;
        }

        private Panel CrearCardKpi(string icono, Color bgIcon, Color iconColor, string titulo, out Label lblValor, string valIni, string subtitulo)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(4, 0, 4, 0), Padding = new Padding(10, 8, 10, 8) };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblIcon = new Label { Text = icono, Font = new Font("Segoe UI", 12F), BackColor = bgIcon, ForeColor = iconColor, Size = new Size(36, 36), Location = new Point(10, 12), TextAlign = ContentAlignment.MiddleCenter };
            Label lblTit = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(50, 6), AutoSize = true };
            lblValor = new Label { Text = valIni, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(48, 22), AutoSize = true };
            Label lblSub = new Label { Text = subtitulo, Font = new Font("Segoe UI", 7F), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(50, 44), AutoSize = true };

            card.Controls.AddRange(new Control[] { lblIcon, lblTit, lblValor, lblSub });
            return card;
        }

        private Panel CrearSeccionFiltros()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.White, Padding = new Padding(14, 8, 14, 8), Margin = new Padding(0, 0, 0, 10) };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            FlowLayoutPanel flp1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, WrapContents = false };

            Panel pnlB = new Panel { Size = new Size(230, 44), Margin = new Padding(0, 0, 8, 0) };
            Label l1 = new Label { Text = "BUSCAR CLIENTE / FOLIO / RUT", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            txtBuscar = new TextBox { PlaceholderText = "🔍 Razón Social, RUT, Folio...", Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) CargarCuentasPorCobrar(); };
            pnlB.Controls.AddRange(new Control[] { txtBuscar, l1 });

            Panel pnlEst = new Panel { Size = new Size(130, 44), Margin = new Padding(0, 0, 8, 0) };
            Label l2 = new Label { Text = "ESTADO DEUDA", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            cbEstado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            cbEstado.Items.AddRange(new object[] { "Todos", "PENDIENTE", "PARCIAL", "VENCIDA", "PAGADA" });
            cbEstado.SelectedIndex = 0;
            pnlEst.Controls.AddRange(new Control[] { cbEstado, l2 });

            Panel pnlD = new Panel { Size = new Size(110, 44), Margin = new Padding(0, 0, 8, 0) };
            Label l3 = new Label { Text = "EMISIÓN DESDE", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            dtpDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            pnlD.Controls.AddRange(new Control[] { dtpDesde, l3 });

            Panel pnlH = new Panel { Size = new Size(110, 44), Margin = new Padding(0, 0, 10, 0) };
            Label l4 = new Label { Text = "EMISIÓN HASTA", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top };
            dtpHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Top, Font = new Font("Segoe UI", 8.5F) };
            pnlH.Controls.AddRange(new Control[] { dtpHasta, l4 });

            btnBuscar = new Button { Text = "🔍 Filtrar", Size = new Size(80, 26), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 12, 6, 0) };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarCuentasPorCobrar();

            btnLimpiar = new Button { Text = "🧹 Limpiar", Size = new Size(80, 26), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 12, 0, 0) };
            btnLimpiar.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnLimpiar.Click += (s, e) => { txtBuscar.Clear(); cbEstado.SelectedIndex = 0; ConfigurarFiltroEsteMes(); CargarCuentasPorCobrar(); };

            flp1.Controls.AddRange(new Control[] { pnlB, pnlEst, pnlD, pnlH, btnBuscar, btnLimpiar });

            // Fila 2: Botonera de Acciones
            Panel pnlFila2 = new Panel { Dock = DockStyle.Bottom, Height = 34 };
            FlowLayoutPanel flpAcciones = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false };

            btnRegistrarAbono = new Button { Text = "💵 Registrar Pago / Abono", Height = 28, AutoSize = true, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0) };
            btnRegistrarAbono.FlatAppearance.BorderSize = 0;
            btnRegistrarAbono.Click += BtnRegistrarAbono_Click;

            btnHistorialPagos = new Button { Text = "📋 Historial de Pagos", Height = 28, AutoSize = true, BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(30, 41, 59), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0) };
            btnHistorialPagos.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnHistorialPagos.Click += BtnHistorialPagos_Click;

            btnDesbloquearCliente = new Button { Text = "🔓 Desbloqueo Excepcional", Height = 28, AutoSize = true, BackColor = Color.FromArgb(254, 243, 199), ForeColor = Color.FromArgb(180, 83, 9), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnDesbloquearCliente.FlatAppearance.BorderColor = Color.FromArgb(252, 211, 77);
            btnDesbloquearCliente.Click += BtnDesbloquearCliente_Click;

            flpAcciones.Controls.AddRange(new Control[] { btnRegistrarAbono, btnHistorialPagos, btnDesbloquearCliente });
            pnlFila2.Controls.Add(flpAcciones);

            pnl.Controls.Add(pnlFila2);
            pnl.Controls.Add(flp1);
            return pnl;
        }

        private Panel CrearSeccionGrilla()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Panel pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 24, Padding = new Padding(0, 4, 0, 0) };
            lblFooterStatus = new Label { Text = "Mostrando 0 cuentas por cobrar", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Left, AutoSize = true };
            pnlFoot.Controls.Add(lblFooterStatus);

            dgvCxC = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 },
                GridColor = Color.FromArgb(241, 245, 249)
            };

            ConfigurarColumnasGrilla();

            pnl.Controls.Add(dgvCxC);
            pnl.Controls.Add(pnlFoot);
            return pnl;
        }

        private void ConfigurarColumnasGrilla()
        {
            dgvCxC.EnableHeadersVisualStyles = false;
            dgvCxC.ColumnHeadersHeight = 32;
            dgvCxC.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvCxC.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCxC.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

            dgvCxC.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvCxC.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

            dgvCxC.Columns.Clear();
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "CxCID", HeaderText = "ID", Visible = false });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Doc", HeaderText = "DOCUMENTO", FillWeight = 16 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "CLIENTE / RAZÓN SOCIAL", FillWeight = 26 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rut", HeaderText = "RUT", FillWeight = 14 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Emision", HeaderText = "EMISIÓN", FillWeight = 12 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vencimiento", HeaderText = "VENCIMIENTO", FillWeight = 13 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mora", HeaderText = "MORA", FillWeight = 12 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "TOTAL ORIGINAL", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Format = "$#,##0" } });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Abonado", HeaderText = "ABONADO", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Format = "$#,##0" } });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Saldo", HeaderText = "SALDO PENDIENTE", FillWeight = 16, DefaultCellStyle = new DataGridViewCellStyle { Format = "$#,##0", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38) } });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "ESTADO", FillWeight = 13 });
        }

        private void CargarCuentasPorCobrar()
        {
            try
            {
                CreditoService.ActualizarEstadosMorosidadDiario();

                using var db = new AppDbContext();
                DateTime fD = dtpDesde.Value.Date;
                DateTime fH = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);

                var query = db.CuentasPorCobrar
                    .Where(c => c.FechaEmision >= fD && c.FechaEmision <= fH)
                    .AsQueryable();

                string filtro = txtBuscar.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(filtro))
                {
                    query = query.Where(c => (c.Cliente != null && (c.Cliente.RazonSocial.ToLower().Contains(filtro) || c.Cliente.Rut.Contains(filtro))) || c.FolioDoc.ToString().Contains(filtro));
                }

                if (cbEstado.SelectedIndex > 0)
                {
                    string estSel = cbEstado.SelectedItem!.ToString()!;
                    query = query.Where(c => c.Estado == estSel);
                }

                _cxcCargadas = query.OrderBy(c => c.FechaVencimiento).ToList();

                var idsClientes = _cxcCargadas.Select(c => c.IdCliente).Distinct().ToList();
                var clientesDict = db.Clientes.Where(c => idsClientes.Contains(c.IdCliente)).ToDictionary(c => c.IdCliente, c => c);

                dgvCxC.Rows.Clear();
                DateTime hoy = DateTime.Today;

                foreach (var c in _cxcCargadas)
                {
                    clientesDict.TryGetValue(c.IdCliente, out var cli);
                    string nomCli = cli?.RazonSocial ?? "Cliente General";
                    string rutCli = cli != null ? RutHelper.Formatear(cli.Rut) : "--";

                    int diasMora = (hoy > c.FechaVencimiento.Date && c.SaldoPendiente > 0) ? (hoy - c.FechaVencimiento.Date).Days : 0;
                    string strMora = diasMora > 0 ? $"🔴 {diasMora} d" : "🟢 Al día";

                    string nomDoc = c.TipoDTE == 33 ? $"Factura #{c.FolioDoc}" : $"Boleta #{c.FolioDoc}";

                    dgvCxC.Rows.Add(
                        c.CxCID,
                        nomDoc,
                        nomCli,
                        rutCli,
                        c.FechaEmision.ToString("dd/MM/yyyy"),
                        c.FechaVencimiento.ToString("dd/MM/yyyy"),
                        strMora,
                        c.MontoOriginal,
                        c.MontoAbonado,
                        c.SaldoPendiente,
                        c.Estado
                    );
                }

                decimal totalDeuda = db.CuentasPorCobrar.Where(c => c.Estado != "PAGADA" && c.Estado != "ANULADA").Sum(c => c.SaldoPendiente);
                decimal deudaVencida = db.CuentasPorCobrar.Where(c => c.FechaVencimiento < hoy && c.SaldoPendiente > 0).Sum(c => c.SaldoPendiente);
                int docsPendientes = db.CuentasPorCobrar.Count(c => c.Estado != "PAGADA" && c.Estado != "ANULADA");
                int clientesMora = db.Clientes.Count(c => c.EstadoCrediticio == "MOROSO" || c.EstadoCrediticio == "BLOQUEADO");

                lblKpiTotalDeuda.Text = $"$ {totalDeuda:N0}";
                lblKpiDeudaVencida.Text = $"$ {deudaVencida:N0}";
                lblKpiDocsPendientes.Text = docsPendientes.ToString("N0");
                lblKpiClientesMora.Text = clientesMora.ToString("N0");

                lblFooterStatus.Text = $"Mostrando {_cxcCargadas.Count} documento(s) por cobrar.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Cuentas por Cobrar: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRegistrarAbono_Click(object? sender, EventArgs e)
        {
            if (dgvCxC.CurrentRow == null || dgvCxC.CurrentRow.Index < 0)
            {
                MessageBox.Show("Seleccione un documento de la lista para registrar el abono o pago.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cxcId = Convert.ToInt32(dgvCxC.CurrentRow.Cells["CxCID"].Value);
            var item = _cxcCargadas.FirstOrDefault(c => c.CxCID == cxcId);
            if (item == null) return;

            if (item.SaldoPendiente <= 0)
            {
                MessageBox.Show("El documento seleccionado ya se encuentra pagado en su totalidad.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using Form modalAbono = new Form
            {
                Text = "Registrar Pago / Abono",
                Size = new Size(380, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTit = new Label { Text = $"Documento: {(item.TipoDTE == 33 ? "Factura" : "Boleta")} #{item.FolioDoc}\nSaldo Pendiente: ${item.SaldoPendiente:N0}", Location = new Point(20, 15), Size = new Size(320, 38), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
            
            Label lblM = new Label { Text = "Monto a Abonar ($):", Location = new Point(20, 65), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtM = new TextBox { Text = item.SaldoPendiente.ToString("0"), Location = new Point(20, 85), Size = new Size(320, 26), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };

            Label lblMed = new Label { Text = "Medio de Pago:", Location = new Point(20, 120), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            ComboBox cbMed = new ComboBox { Location = new Point(20, 140), Size = new Size(320, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbMed.Items.AddRange(new object[] { "EFECTIVO", "TRANSFERENCIA", "DÉBITO", "CRÉDITO", "CHEQUE" });
            cbMed.SelectedIndex = 1;

            Label lblComp = new Label { Text = "N° Comprobante / Transacción:", Location = new Point(20, 175), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtComp = new TextBox { Location = new Point(20, 195), Size = new Size(320, 26), Font = new Font("Segoe UI", 9.5F) };

            Button btnGuardar = new Button { Text = "✔ Confirmar Abono", Location = new Point(20, 245), Size = new Size(320, 40), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (sa, ea) =>
            {
                if (!decimal.TryParse(txtM.Text.Trim(), out decimal montoAbono) || montoAbono <= 0)
                {
                    MessageBox.Show("Ingrese un monto válido a abonar.", "Monto Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var db = new AppDbContext();
                    CreditoService.ProcesarPagoCliente(item.IdCliente, montoAbono, cbMed.SelectedItem!.ToString()!, txtComp.Text.Trim(), "Abono desde módulo CxC", "ADMIN", new List<int> { item.CxCID }, db);
                    MessageBox.Show("¡Pago registrado exitosamente!", "Abono Aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modalAbono.DialogResult = DialogResult.OK;
                    modalAbono.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al procesar el pago: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modalAbono.Controls.AddRange(new Control[] { lblTit, lblM, txtM, lblMed, cbMed, lblComp, txtComp, btnGuardar });

            if (modalAbono.ShowDialog(this) == DialogResult.OK)
            {
                CargarCuentasPorCobrar();
            }
        }

        private void BtnHistorialPagos_Click(object? sender, EventArgs e)
        {
            using var db = new AppDbContext();
            var pagos = db.PagosClientes.OrderByDescending(p => p.FechaPago).Take(50).ToList();

            Form modalHist = new Form { Text = "Historial de Pagos y Abonos Recientes", Size = new Size(720, 440), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
            DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            
            dgv.Columns.Add("ID", "N° PAGO");
            dgv.Columns.Add("Fecha", "FECHA");
            dgv.Columns.Add("Monto", "MONTO");
            dgv.Columns.Add("Medio", "MEDIO PAGO");
            dgv.Columns.Add("Comprobante", "COMPROBANTE");
            dgv.Columns.Add("Usuario", "COBRADOR");

            dgv.Columns["Monto"].DefaultCellStyle.Format = "$#,##0";

            foreach (var p in pagos)
            {
                dgv.Rows.Add(p.PagoID, p.FechaPago.ToString("dd/MM/yyyy HH:mm"), p.MontoTotalPago, p.MedioPago, p.NroComprobante ?? "--", p.UsuarioCobrador);
            }

            modalHist.Controls.Add(dgv);
            modalHist.ShowDialog(this);
        }

        private void BtnDesbloquearCliente_Click(object? sender, EventArgs e)
        {
            if (dgvCxC.CurrentRow == null || dgvCxC.CurrentRow.Index < 0)
            {
                MessageBox.Show("Seleccione un documento de la lista para gestionar el estado crediticio del cliente.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cxcId = Convert.ToInt32(dgvCxC.CurrentRow.Cells["CxCID"].Value);
            var item = _cxcCargadas.FirstOrDefault(c => c.CxCID == cxcId);
            if (item == null) return;

            using var db = new AppDbContext();
            var cliente = db.Clientes.Find(item.IdCliente);
            if (cliente == null) return;

            DateTime hoy = DateTime.Today;
            var facturasVencidas = db.CuentasPorCobrar
                .Where(c => c.IdCliente == cliente.IdCliente && (c.Estado == "PENDIENTE" || c.Estado == "PARCIAL" || c.Estado == "VENCIDA") && c.FechaVencimiento < hoy)
                .ToList();

            decimal deudaVencidaTotal = facturasVencidas.Sum(f => f.SaldoPendiente);

            using Form modalEstado = new Form
            {
                Text = "Gestión de Estado Crediticio y Desbloqueo",
                Size = new Size(460, 420),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTit = new Label 
            { 
                Text = $"Cliente: {cliente.RazonSocial}\nRUT: {RutHelper.Formatear(cliente.Rut)}", 
                Location = new Point(20, 12), 
                Size = new Size(400, 36), 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(15, 23, 42) 
            };

            Panel pnlInfo = new Panel 
            { 
                Location = new Point(20, 52), 
                Size = new Size(400, 80), 
                BackColor = Color.FromArgb(248, 250, 252), 
                BorderStyle = BorderStyle.FixedSingle, 
                Padding = new Padding(8) 
            };

            Label lblDetalle = new Label
            {
                Text = $"• Estado Crediticio Actual: [ {cliente.EstadoCrediticio} ]\n" +
                       $"• Facturas Vencidas: {facturasVencidas.Count} documento(s)\n" +
                       $"• Monto Vencido: ${deudaVencidaTotal:N0}\n" +
                       $"• Cupo Total: ${cliente.CupoCredito:N0}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = cliente.EstadoCrediticio == "ACTIVO" ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38),
                Dock = DockStyle.Fill
            };
            pnlInfo.Controls.Add(lblDetalle);

            Label lblAccion = new Label { Text = "Acción / Nuevo Estado:", Location = new Point(20, 140), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            ComboBox cbNuevoEstado = new ComboBox
            {
                Location = new Point(20, 160),
                Size = new Size(400, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbNuevoEstado.Items.AddRange(new object[] { "ACTIVO (Desbloquear / Permitir compras)", "BLOQUEADO (Suspender línea de crédito)", "MOROSO (En mora por documentos vencidos)" });
            cbNuevoEstado.SelectedIndex = cliente.EstadoCrediticio == "ACTIVO" ? 1 : 0;

            Label lblMot = new Label { Text = "Motivo del cambio de estado:", Location = new Point(20, 195), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtMot = new TextBox 
            { 
                Location = new Point(20, 215), 
                Size = new Size(400, 70), 
                Multiline = true, 
                Font = new Font("Segoe UI", 9F), 
                Text = cliente.EstadoCrediticio == "ACTIVO" ? "Bloqueo preventivo por decisión administrativa." : "Autorización gerencial por compromiso de pago." 
            };

            Button btnGuardar = new Button 
            { 
                Text = "💾 Aplicar Cambio de Estado", 
                Location = new Point(20, 305), 
                Size = new Size(400, 42), 
                BackColor = Color.FromArgb(16, 185, 129), 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

            btnGuardar.Click += (sa, ea) =>
            {
                string estadoSeleccionado = cbNuevoEstado.SelectedIndex switch
                {
                    0 => "ACTIVO",
                    1 => "BLOQUEADO",
                    _ => "MOROSO"
                };

                if (estadoSeleccionado == cliente.EstadoCrediticio)
                {
                    MessageBox.Show("El cliente ya tiene asignado ese estado.", "Sin Cambios", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string estadoAnt = cliente.EstadoCrediticio ?? "ACTIVO";
                cliente.EstadoCrediticio = estadoSeleccionado;

                db.HistorialCondicionesCredito.Add(new HistorialCondicionesCredito
                {
                    IdCliente = cliente.IdCliente,
                    DiasCreditoAnterior = cliente.DiasCreditoHabiles,
                    DiasCreditoNuevo = cliente.DiasCreditoHabiles,
                    CupoAnterior = cliente.CupoCredito,
                    CupoNuevo = cliente.CupoCredito,
                    EstadoAnterior = estadoAnt,
                    EstadoNuevo = estadoSeleccionado,
                    Motivo = txtMot.Text.Trim(),
                    FechaCambio = DateTime.Now,
                    UsuarioResponsable = "ADMIN"
                });

                db.SaveChanges();

                MessageBox.Show($"El estado crediticio del cliente '{cliente.RazonSocial}' se actualizó a: {estadoSeleccionado}.", "Estado Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                modalEstado.DialogResult = DialogResult.OK;
                modalEstado.Close();
            };

            modalEstado.Controls.AddRange(new Control[] { lblTit, pnlInfo, lblAccion, cbNuevoEstado, lblMot, txtMot, btnGuardar });

            if (modalEstado.ShowDialog(this) == DialogResult.OK)
            {
                CargarCuentasPorCobrar();
            }
        }

        private void ConfigurarFiltroEsteMes()
        {
            DateTime hoy = DateTime.Today;
            dtpDesde.Value = new DateTime(hoy.Year, hoy.Month, 1);
            dtpHasta.Value = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
        }
    }
}