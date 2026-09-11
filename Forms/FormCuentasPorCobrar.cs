using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
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

        private Button btnRegistrarAbono = null!;
        private Button btnHistorialPagos = null!;
        private Button btnDesbloquearCliente = null!;

        private Label lblKpiTotalDeuda = null!;
        private Label lblKpiDeudaVencida = null!;
        private Label lblKpiDocsPendientes = null!;
        private Label lblKpiClientesMora = null!;

        private DataGridView dgvCxC = null!;
        private Label lblFooterStatus = null!;

        private List<ItemCuentaPorCobrarDTO> _cxcCargadas = new List<ItemCuentaPorCobrarDTO>();

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
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            Panel pnlKpis = CrearSeccionKpis();
            Panel pnlFiltros = CrearSeccionFiltros();
            Panel pnlGrilla = CrearSeccionGrilla();

            // Orden Z estricto para evitar solapamientos en bordes
            pnlMain.Controls.Add(pnlGrilla);
            pnlMain.Controls.Add(pnlFiltros);
            pnlMain.Controls.Add(pnlKpis);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearSeccionKpis()
        {
            Panel pnlWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

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

            var card1 = CrearCardKpi("💰", Color.FromArgb(239, 246, 255), "Total Cartera Deuda", out lblKpiTotalDeuda, "$ 0", "Saldo pendiente total");
            var card2 = CrearCardKpi("⚠️", Color.FromArgb(254, 242, 242), "Deuda Vencida (Mora)", out lblKpiDeudaVencida, "$ 0", "Documentos fuera de plazo");
            var card3 = CrearCardKpi("📄", Color.FromArgb(254, 243, 199), "Documentos Pendientes", out lblKpiDocsPendientes, "0", "Facturas y Boletas a plazo");
            var card4 = CrearCardKpi("👥", Color.FromArgb(243, 232, 255), "Clientes con Mora", out lblKpiClientesMora, "0", "Clientes restringidos");

            card1.Margin = new Padding(0, 0, 6, 0);
            card2.Margin = new Padding(6, 0, 6, 0);
            card3.Margin = new Padding(6, 0, 6, 0);
            card4.Margin = new Padding(6, 0, 0, 0);

            tlp.Controls.Add(card1, 0, 0);
            tlp.Controls.Add(card2, 1, 0);
            tlp.Controls.Add(card3, 2, 0);
            tlp.Controls.Add(card4, 3, 0);

            tlp.Resize += (s, e) => tlp.Invalidate(true);
            pnlWrapper.Controls.Add(tlp);
            return pnlWrapper;
        }

        private Panel CrearCardKpi(string icon, Color iconBg, string titulo, out Label lblValor, string valInit, string subInit)
        {
            Panel pnl = CrearTarjetaRedondeada(0, 0, 0, 80, Color.White, Color.FromArgb(226, 232, 240));
            pnl.Dock = DockStyle.Fill;
            pnl.Padding = new Padding(12);

            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 12F),
                BackColor = iconBg,
                Size = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(12, 12)
            };

            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(56, 10),
                AutoSize = true
            };

            lblValor = new Label
            {
                Text = valInit,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(56, 26),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = subInit,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(56, 52),
                AutoSize = true
            };

            pnl.Controls.AddRange(new Control[] { lblIcon, lblT, lblValor, lblSub });
            return pnl;
        }

        private Panel CrearSeccionFiltros()
        {
            Panel pnlWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                Padding = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 115, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 8, 16, 8);

            FlowLayoutPanel flp1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            txtBuscar = new TextBox
            {
                Size = new Size(200, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "Razón Social, RUT, Folio..."
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarCuentasPorCobrar(); e.SuppressKeyPress = true; } };
            Panel pnlB = CrearGrupoConCaja("BUSCAR CLIENTE / FOLIO / RUT", txtBuscar, 215);

            cbEstado = new ComboBox
            {
                Size = new Size(135, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbEstado.Items.AddRange(new object[] { "Todos", "PENDIENTE", "PARCIAL", "VENCIDA", "PAGADA" });
            cbEstado.SelectedIndex = 0;
            cbEstado.SelectedIndexChanged += (s, e) => CargarCuentasPorCobrar();
            Panel pnlEst = CrearGrupoLimpio("ESTADO DEUDA", cbEstado, 140);

            dtpDesde = new DateTimePicker { Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlD = CrearGrupoLimpio("EMISIÓN DESDE", dtpDesde, 125);

            dtpHasta = new DateTimePicker { Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };
            Panel pnlH = CrearGrupoLimpio("EMISIÓN HASTA", dtpHasta, 125);

            btnBuscar = CrearBoton("🔍  Filtrar", Color.FromArgb(37, 99, 235), Color.White, new Size(95, 34), 8);
            btnBuscar.Margin = new Padding(8, 14, 6, 0);
            btnBuscar.Click += (s, e) => CargarCuentasPorCobrar();

            btnLimpiar = CrearBoton("🧹  Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(90, 34), 8);
            btnLimpiar.Margin = new Padding(0, 14, 0, 0);
            btnLimpiar.Click += (s, e) => { txtBuscar.Clear(); cbEstado.SelectedIndex = 0; ConfigurarFiltroEsteMes(); CargarCuentasPorCobrar(); };

            flp1.Controls.AddRange(new Control[] { pnlB, pnlEst, pnlD, pnlH, btnBuscar, btnLimpiar });

            Panel pnlFila2 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            FlowLayoutPanel flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnDesbloquearCliente = CrearBoton("🔓 Desbloqueo Excepcional", Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9), new Size(185, 28), 6);
            btnDesbloquearCliente.Margin = new Padding(6, 2, 0, 0);
            btnDesbloquearCliente.Click += BtnDesbloquearCliente_Click;

            btnHistorialPagos = CrearBoton("📋 Historial de Pagos", Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59), new Size(150, 28), 6);
            btnHistorialPagos.Margin = new Padding(6, 2, 0, 0);
            btnHistorialPagos.Click += BtnHistorialPagos_Click;

            btnRegistrarAbono = CrearBoton("💵 Registrar Pago / Abono", Color.FromArgb(16, 185, 129), Color.White, new Size(180, 28), 6);
            btnRegistrarAbono.Margin = new Padding(0, 2, 0, 0);
            btnRegistrarAbono.Click += BtnRegistrarAbono_Click;

            flpAcciones.Controls.AddRange(new Control[] { btnDesbloquearCliente, btnHistorialPagos, btnRegistrarAbono });
            pnlFila2.Controls.Add(flpAcciones);

            pnlFiltrosCard.Controls.Add(pnlFila2);
            pnlFiltrosCard.Controls.Add(flp1);
            pnlWrapper.Controls.Add(pnlFiltrosCard);
            return pnlWrapper;
        }

        private Panel CrearSeccionGrilla()
        {
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

            dgvCxC = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 }
            };
            ConfigurarColumnasGrilla();

            Panel pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblFooterStatus = new Label
            {
                Text = "Mostrando 0 cuentas por cobrar",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFoot.Controls.Add(lblFooterStatus);

            pnlGridCard.Controls.Add(dgvCxC);
            pnlGridCard.Controls.Add(pnlFoot);
            return pnlGridCard;
        }

        private void ConfigurarColumnasGrilla()
        {
            dgvCxC.EnableHeadersVisualStyles = false;
            dgvCxC.ColumnHeadersHeight = 36;
            dgvCxC.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvCxC.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCxC.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvCxC.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvCxC.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvCxC.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvCxC.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvCxC.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

            var estiloMoneda = new DataGridViewCellStyle
            {
                FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                Format = "$ #,##0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            };

            dgvCxC.Columns.Clear();
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "CxCID", HeaderText = "ID", Visible = false });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Doc", HeaderText = "DOCUMENTO", FillWeight = 16 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "CLIENTE / RAZÓN SOCIAL", FillWeight = 26 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rut", HeaderText = "RUT", FillWeight = 14 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Emision", HeaderText = "EMISIÓN", FillWeight = 12 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vencimiento", HeaderText = "VENCIMIENTO", FillWeight = 13 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mora", HeaderText = "MORA", FillWeight = 12 });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "TOTAL ORIGINAL", FillWeight = 15, DefaultCellStyle = estiloMoneda });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Abonado", HeaderText = "ABONADO", FillWeight = 14, DefaultCellStyle = estiloMoneda });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Saldo",
                HeaderText = "SALDO PENDIENTE",
                FillWeight = 16,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                    Format = "$ #,##0",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 38, 38),
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvCxC.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "ESTADO", FillWeight = 13 });
        }

        private void CargarCuentasPorCobrar()
        {
            try
            {
                DateTime fD = dtpDesde.Value.Date;
                DateTime fH = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);
                string filtro = txtBuscar.Text.Trim();
                string estSel = cbEstado.SelectedIndex > 0 ? cbEstado.SelectedItem!.ToString()! : "Todos";

                var resumen = CreditoService.ObtenerCartera(fD, fH, filtro, estSel);
                _cxcCargadas = resumen.Items;

                dgvCxC.Rows.Clear();
                foreach (var c in _cxcCargadas)
                {
                    dgvCxC.Rows.Add(
                        c.CxCID,
                        c.DocumentoNombre,
                        c.ClienteNombre,
                        c.Rut,
                        c.FechaEmision.ToString("dd/MM/yyyy"),
                        c.FechaVencimiento.ToString("dd/MM/yyyy"),
                        c.MoraTexto,
                        c.MontoOriginal,
                        c.MontoAbonado,
                        c.SaldoPendiente,
                        c.Estado
                    );
                }

                lblKpiTotalDeuda.Text = MonedaHelper.Formatear(resumen.TotalDeuda, conSigno: true);
                lblKpiDeudaVencida.Text = MonedaHelper.Formatear(resumen.DeudaVencida, conSigno: true);
                lblKpiDocsPendientes.Text = resumen.DocsPendientes.ToString("N0", new System.Globalization.CultureInfo("es-CL"));
                lblKpiClientesMora.Text = resumen.ClientesMora.ToString("N0", new System.Globalization.CultureInfo("es-CL"));

                lblFooterStatus.Text = $"Mostrando {_cxcCargadas.Count} documento(s) por cobrar.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Cuentas por Cobrar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            Label lblTit = new Label { Text = $"Documento: {item.DocumentoNombre}\nSaldo Pendiente: {MonedaHelper.Formatear(item.SaldoPendiente, conSigno: true)}", Location = new Point(20, 15), Size = new Size(320, 38), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblM = new Label { Text = "Monto a Abonar ($):", Location = new Point(20, 65), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtM = new TextBox { Text = MonedaHelper.Formatear(item.SaldoPendiente), Location = new Point(20, 85), Size = new Size(320, 26), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            txtM.KeyPress += (s, ev) => { if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar)) ev.Handled = true; };
            txtM.TextChanged += (s, ev) => MonedaHelper.AplicarMascaraEnVivo(txtM);

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
                decimal montoAbono = MonedaHelper.Limpiar(txtM.Text);
                if (montoAbono <= 0)
                {
                    MessageBox.Show("Ingrese un monto válido a abonar.", "Monto Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    CreditoService.ProcesarPagoCliente(item.IdCliente, montoAbono, cbMed.SelectedItem!.ToString()!, txtComp.Text.Trim(), "Abono desde módulo CxC", "ADMIN", new List<int> { item.CxCID });
                    MessageBox.Show("¡Pago registrado exitosamente!", "Abono Aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modalAbono.DialogResult = DialogResult.OK;
                    modalAbono.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al procesar el pago: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            try
            {
                var pagos = CreditoService.ObtenerHistorialPagosRecientes(50);

                Form modalHist = new Form { Text = "Historial de Pagos y Abonos Recientes", Size = new Size(720, 440), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
                DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

                dgv.Columns.Add("ID", "N° PAGO");
                dgv.Columns.Add("Fecha", "FECHA");
                dgv.Columns.Add("Monto", "MONTO");
                dgv.Columns.Add("Medio", "MEDIO PAGO");
                dgv.Columns.Add("Comprobante", "COMPROBANTE");
                dgv.Columns.Add("Usuario", "COBRADOR");

                dgv.Columns["Monto"].DefaultCellStyle = new DataGridViewCellStyle
                {
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                    Format = "$ #,##0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                };

                foreach (var p in pagos)
                {
                    dgv.Rows.Add(p.PagoID, p.FechaPago.ToString("dd/MM/yyyy HH:mm"), p.MontoTotalPago, p.MedioPago, p.NroComprobante ?? "--", p.UsuarioCobrador);
                }

                modalHist.Controls.Add(dgv);
                modalHist.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar historial: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            var detalle = CreditoService.ObtenerDetalleCrediticioCliente(item.IdCliente);
            var cliente = detalle.Cliente;
            if (cliente == null) return;

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
                       $"• Facturas Vencidas: {detalle.CantidadVencidas} documento(s)\n" +
                       $"• Monto Vencido: {MonedaHelper.Formatear(detalle.DeudaVencida, conSigno: true)}\n" +
                       $"• Cupo Total: {MonedaHelper.Formatear(cliente.CupoCredito, conSigno: true)}",
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

                try
                {
                    CreditoService.CambiarEstadoCrediticio(cliente.IdCliente, estadoSeleccionado, txtMot.Text.Trim(), "ADMIN");
                    MessageBox.Show($"El estado crediticio del cliente '{cliente.RazonSocial}' se actualizó a: {estadoSeleccionado}.", "Estado Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modalEstado.DialogResult = DialogResult.OK;
                    modalEstado.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar estado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
    }
}