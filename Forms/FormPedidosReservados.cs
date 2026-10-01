using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data; // 👈 AGREGAR ESTA LÍNEA
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormPedidosReservados : Form
    {
        private readonly ReservaService _reservaService = new ReservaService();
        private readonly CajaService _cajaService = new CajaService();
        private readonly TicketPrintService _ticketPrintService = new TicketPrintService();
        private readonly Usuario _usuarioActual;

        private DataGridView dgvReservas = null!;
        private DataGridView dgvDetalle = null!;
        private DataGridView dgvAbonos = null!;
        private TextBox txtBuscar = null!;
        private ComboBox cbFiltroEstado = null!;
        private Label lblContadorFooter = null!;

        private Button btnMarcarListo = null!;
        private Button btnRegistrarAbono = null!;
        private Button btnEntregarLiquidar = null!;
        private Button btnReimprimir = null!;
        private Button btnAnular = null!;

        private Reserva? _reservaSeleccionada;
        private List<Reserva> _listaReservas = new List<Reserva>();

        public FormPedidosReservados(Usuario? usuario = null)
        {
            _usuarioActual = usuario ?? new Usuario { NombreUsuario = "admin" };

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            this.Shown += (s, e) => CargarReservas();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 12),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // 1. ENCABEZADO
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            Label lblTitulo = new Label
            {
                Text = "📅 Pedidos por Encargo y Reservas",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, 2),
                AutoSize = true
            };
            Label lblSub = new Label
            {
                Text = "Seguimiento de pedidos futuros, control de abonos, producción y entrega con boleta",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(2, 30),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblSub });

            // 2. FILTROS
            Panel pnlFiltros = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(10, 10, 10, 10) };
            pnlFiltros.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlFiltros.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblFiltroEst = new Label { Text = "ESTADO:", Location = new Point(10, 18), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            cbFiltroEstado = new ComboBox
            {
                Location = new Point(75, 14),
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbFiltroEstado.Items.AddRange(new object[] { "Todos", "Reservado", "Listo para Retiro", "Entregado", "Anulado" });
            cbFiltroEstado.SelectedIndex = 0;
            cbFiltroEstado.SelectedIndexChanged += (s, e) => CargarReservas();

            Label lblBus = new Label { Text = "BUSCAR:", Location = new Point(255, 18), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            txtBuscar = new TextBox
            {
                Location = new Point(315, 15),
                Size = new Size(260, 25),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Código, Cliente, RUT..."
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarReservas(); e.SuppressKeyPress = true; } };

            Button btnBuscar = new Button { Text = "🔍 Buscar", Location = new Point(585, 14), Size = new Size(85, 27), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarReservas();

            Button btnRecargar = new Button { Text = "🔄 Recargar", Location = new Point(678, 14), Size = new Size(95, 27), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(30, 41, 59), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRecargar.FlatAppearance.BorderSize = 0;
            btnRecargar.Click += (s, e) => { txtBuscar.Clear(); cbFiltroEstado.SelectedIndex = 0; CargarReservas(); };

            pnlFiltros.Controls.AddRange(new Control[] { lblFiltroEst, cbFiltroEstado, lblBus, txtBuscar, btnBuscar, btnRecargar });

            // 3. CUERPO (GRILLA SUPERIOR + PANELES INFERIORES)
            TableLayoutPanel gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

            // GRILLA SUPERIOR: LISTA DE RESERVAS
            Panel pnlCardReservas = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8), Margin = new Padding(0, 8, 0, 8) };
            pnlCardReservas.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardReservas.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            dgvReservas = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false
            };
            ConfigurarEstiloTabla(dgvReservas);
            ConfigurarColumnasReservas();
            dgvReservas.SelectionChanged += DgvReservas_SelectionChanged;

            pnlCardReservas.Controls.Add(dgvReservas);

            // PANEL INFERIOR (DETALLE IZQUIERDA + HISTORIAL PAGOS + ACCIONES DERECHA)
            TableLayoutPanel pnlBottomSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            pnlBottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F)); // Detalle Ítems
            pnlBottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F)); // Historial Abonos
            pnlBottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F)); // Botones de Acción

            // A) Detalle de Ítems
            Panel pnlCardDet = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8), Margin = new Padding(0, 0, 6, 0) };
            pnlCardDet.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardDet.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            Label lblTitDet = new Label { Text = "PRODUCTOS DEL PEDIDO", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 20 };
            dgvDetalle = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, MultiSelect = false, RowHeadersVisible = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AutoGenerateColumns = false };
            ConfigurarEstiloTabla(dgvDetalle);
            ConfigurarColumnasDetalle();
            pnlCardDet.Controls.AddRange(new Control[] { dgvDetalle, lblTitDet });

            // B) Historial de Abonos
            Panel pnlCardAbonos = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8), Margin = new Padding(0, 0, 6, 0) };
            pnlCardAbonos.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardAbonos.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            Label lblTitAbo = new Label { Text = "HISTORIAL DE PAGOS / ABONOS", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 20 };
            dgvAbonos = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, MultiSelect = false, RowHeadersVisible = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AutoGenerateColumns = false };
            ConfigurarEstiloTabla(dgvAbonos);
            ConfigurarColumnasAbonos();
            pnlCardAbonos.Controls.AddRange(new Control[] { dgvAbonos, lblTitAbo });

            // C) Botones de Acción
            Panel pnlCardAcciones = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            pnlCardAcciones.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardAcciones.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            btnEntregarLiquidar = CrearBotonAccion("🚀 ENTREGAR Y COBRAR", Color.FromArgb(16, 185, 129), Color.White, 0);
            btnEntregarLiquidar.Click += BtnEntregarLiquidar_Click;

            btnRegistrarAbono = CrearBotonAccion("💵 Registrar Abono", Color.FromArgb(37, 99, 235), Color.White, 46);
            btnRegistrarAbono.Click += BtnRegistrarAbono_Click;

            btnMarcarListo = CrearBotonAccion("📦 Marcar Listo para Retiro", Color.FromArgb(124, 58, 237), Color.White, 92);
            btnMarcarListo.Click += BtnMarcarListo_Click;

            btnReimprimir = CrearBotonAccion("🖨️ Reimprimir Ticket", Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59), 138);
            btnReimprimir.Click += BtnReimprimir_Click;

            btnAnular = CrearBotonAccion("🚫 Anular Reserva", Color.FromArgb(254, 242, 242), Color.FromArgb(239, 68, 68), 184);
            btnAnular.Click += BtnAnular_Click;

            pnlCardAcciones.Controls.AddRange(new Control[] { btnEntregarLiquidar, btnRegistrarAbono, btnMarcarListo, btnReimprimir, btnAnular });

            pnlBottomSplit.Controls.Add(pnlCardDet, 0, 0);
            pnlBottomSplit.Controls.Add(pnlCardAbonos, 1, 0);
            pnlBottomSplit.Controls.Add(pnlCardAcciones, 2, 0);

            gridLayout.Controls.Add(pnlCardReservas, 0, 0);
            gridLayout.Controls.Add(pnlBottomSplit, 0, 1);

            // FOOTER
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(4, 4, 4, 0) };
            lblContadorFooter = new Label { Text = "0 reservas registradas", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Fill };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlMain.Controls.AddRange(new Control[] { gridLayout, pnlFiltros, pnlHeader, pnlFooter });
            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void ConfigurarColumnasReservas()
        {
            dgvReservas.Columns.Clear();
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodigoReserva", DataPropertyName = "CodigoReserva", HeaderText = "CÓDIGO", FillWeight = 20 });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreCliente", DataPropertyName = "NombreCliente", HeaderText = "CLIENTE", FillWeight = 32 });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "TelefonoCliente", DataPropertyName = "TelefonoCliente", HeaderText = "TELÉFONO", FillWeight = 18 });

            var colEntrega = new DataGridViewTextBoxColumn { Name = "FechaEntregaPactada", DataPropertyName = "FechaEntregaPactada", HeaderText = "FECHA COMPROMISO", FillWeight = 24 };
            colEntrega.DefaultCellStyle.Format = "dd-MM-yyyy HH:mm";
            dgvReservas.Columns.Add(colEntrega);

            var estiloMoneda = new DataGridViewCellStyle { FormatProvider = new System.Globalization.CultureInfo("es-CL"), Format = "$ #,##0", Alignment = DataGridViewContentAlignment.MiddleRight };
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPedido", DataPropertyName = "TotalPedido", HeaderText = "TOTAL", FillWeight = 18, DefaultCellStyle = estiloMoneda });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAbonado", DataPropertyName = "TotalAbonado", HeaderText = "ABONADO", FillWeight = 18, DefaultCellStyle = estiloMoneda });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaldoPendiente", DataPropertyName = "SaldoPendiente", HeaderText = "SALDO", FillWeight = 18, DefaultCellStyle = estiloMoneda });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", DataPropertyName = "Estado", HeaderText = "ESTADO", FillWeight = 22 });

            dgvReservas.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvReservas.Rows.Count) return;
                string col = dgvReservas.Columns[e.ColumnIndex].Name;

                if (col == "Estado" && e.Value != null)
                {
                    string est = e.Value.ToString()!;
                    if (est == "Reservado") { e.CellStyle.ForeColor = Color.FromArgb(234, 88, 12); e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
                    else if (est == "Listo para Retiro") { e.CellStyle.ForeColor = Color.FromArgb(37, 99, 235); e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
                    else if (est == "Entregado") { e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74); e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
                    else if (est == "Anulado") { e.CellStyle.ForeColor = Color.FromArgb(239, 68, 68); }
                }
                else if (col == "SaldoPendiente" && e.Value is decimal saldo)
                {
                    e.CellStyle.ForeColor = saldo > 0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(22, 163, 74);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            };
        }

        private void ConfigurarColumnasDetalle()
        {
            dgvDetalle.Columns.Clear();
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreProducto", DataPropertyName = "NombreProducto", HeaderText = "PRODUCTO", FillWeight = 50 });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", DataPropertyName = "Cantidad", HeaderText = "CANT.", FillWeight = 20 });
            var estiloM = new DataGridViewCellStyle { FormatProvider = new System.Globalization.CultureInfo("es-CL"), Format = "$ #,##0", Alignment = DataGridViewContentAlignment.MiddleRight };
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", DataPropertyName = "Subtotal", HeaderText = "SUBTOTAL", FillWeight = 30, DefaultCellStyle = estiloM });

            dgvDetalle.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvDetalle.Rows.Count) return;
                if (dgvDetalle.Columns[e.ColumnIndex].Name == "Cantidad" && dgvDetalle.Rows[e.RowIndex].DataBoundItem is ReservaDetalle item)
                {
                    e.Value = item.EsPesable ? $"{item.Cantidad}g" : $"{item.Cantidad}x";
                    e.FormattingApplied = true;
                }
            };
        }

        private void ConfigurarColumnasAbonos()
        {
            dgvAbonos.Columns.Clear();
            var colF = new DataGridViewTextBoxColumn { Name = "FechaPago", DataPropertyName = "FechaPago", HeaderText = "FECHA", FillWeight = 35 };
            colF.DefaultCellStyle.Format = "dd-MM HH:mm";
            dgvAbonos.Columns.Add(colF);

            var estiloM = new DataGridViewCellStyle { FormatProvider = new System.Globalization.CultureInfo("es-CL"), Format = "$ #,##0", Alignment = DataGridViewContentAlignment.MiddleRight };
            dgvAbonos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", DataPropertyName = "Monto", HeaderText = "MONTO", FillWeight = 35, DefaultCellStyle = estiloM });
            dgvAbonos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedioPago", DataPropertyName = "MedioPago", HeaderText = "MEDIO", FillWeight = 30 });
        }

        private void CargarReservas()
        {
            try
            {
                string estado = cbFiltroEstado.SelectedItem?.ToString() ?? "Todos";
                string query = txtBuscar.Text.Trim();

                _listaReservas = _reservaService.ObtenerReservas(estado, query);
                dgvReservas.DataSource = null;
                dgvReservas.DataSource = _listaReservas;

                lblContadorFooter.Text = $"Mostrando {_listaReservas.Count} reservas registradas";

                if (_listaReservas.Count > 0)
                {
                    dgvReservas.Rows[0].Selected = true;
                    CargarSubDetalles(_listaReservas[0]);
                }
                else
                {
                    _reservaSeleccionada = null;
                    dgvDetalle.DataSource = null;
                    dgvAbonos.DataSource = null;
                    ActualizarBotonesAccion();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reservas: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvReservas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvReservas.CurrentRow?.DataBoundItem is Reserva res)
            {
                _reservaSeleccionada = res;
                CargarSubDetalles(res);
            }
        }

        private void CargarSubDetalles(Reserva res)
        {
            try
            {
                var detalles = _reservaService.ObtenerDetallesReserva(res.IdReserva);
                dgvDetalle.DataSource = detalles;

                var pagos = _reservaService.ObtenerPagosReserva(res.IdReserva);
                dgvAbonos.DataSource = pagos;

                ActualizarBotonesAccion();
            }
            catch { }
        }

        private void ActualizarBotonesAccion()
        {
            if (_reservaSeleccionada == null)
            {
                btnEntregarLiquidar.Enabled = false;
                btnRegistrarAbono.Enabled = false;
                btnMarcarListo.Enabled = false;
                btnReimprimir.Enabled = false;
                btnAnular.Enabled = false;
                return;
            }

            bool esActiva = _reservaSeleccionada.Estado == "Reservado" || _reservaSeleccionada.Estado == "Listo para Retiro";
            btnEntregarLiquidar.Enabled = esActiva;
            btnRegistrarAbono.Enabled = esActiva && _reservaSeleccionada.SaldoPendiente > 0;
            btnMarcarListo.Enabled = _reservaSeleccionada.Estado == "Reservado";
            btnReimprimir.Enabled = true;
            btnAnular.Enabled = esActiva;
        }

        private void BtnMarcarListo_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var r = MessageBox.Show($"¿Marcar el pedido N° {_reservaSeleccionada.CodigoReserva} como 'Listo para Retiro'?", "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                _reservaService.MarcarComoListoParaRetiro(_reservaSeleccionada.IdReserva);
                CargarReservas();
            }
        }

        private void BtnRegistrarAbono_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null || _reservaSeleccionada.SaldoPendiente <= 0) return;

            var turno = _cajaService.ObtenerTurnoAbierto(_usuarioActual.NombreUsuario);
            if (turno == null || turno.Estado != "Abierta")
            {
                MessageBox.Show("Debe tener una caja abierta para registrar ingresos de dinero por abonos.", "Caja Cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using Form modalAbono = new Form
            {
                Text = $"Registrar Abono - {_reservaSeleccionada.CodigoReserva}",
                Size = new Size(360, 260),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblInf = new Label { Text = $"Saldo actual: {MonedaHelper.Formatear(_reservaSeleccionada.SaldoPendiente, conSigno: true)}", Location = new Point(20, 15), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
            Label lblM = new Label { Text = "Monto a abonar ($):", Location = new Point(20, 48), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtM = new TextBox { Location = new Point(20, 70), Size = new Size(300, 26), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), MaxLength = 10 };
            txtM.TextChanged += (s, ev) => MonedaHelper.AplicarMascaraEnVivo(txtM);

            Label lblMp = new Label { Text = "Medio de Pago:", Location = new Point(20, 106), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            ComboBox cbMp = new ComboBox { Location = new Point(20, 128), Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbMp.Items.AddRange(new object[] { "Efectivo", "Débito", "Tarjeta Crédito", "Transferencia" });
            cbMp.SelectedIndex = 0;

            Button btnGuardar = new Button { Text = "✔ Guardar Abono", Location = new Point(20, 168), Size = new Size(300, 38), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnGuardar.FlatAppearance.BorderSize = 0;

            btnGuardar.Click += (s, ev) =>
            {
                decimal monto = MonedaHelper.Limpiar(txtM.Text);
                if (monto <= 0)
                {
                    MessageBox.Show("Ingrese un monto válido.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (monto > _reservaSeleccionada.SaldoPendiente)
                {
                    MessageBox.Show("El abono no puede superar el saldo pendiente.", "Monto Excedido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _reservaService.RegistrarAbono(_reservaSeleccionada.IdReserva, monto, cbMp.SelectedItem?.ToString() ?? "Efectivo", turno.CajaTurnoID, _usuarioActual.NombreUsuario);
                MessageBox.Show("Abono registrado e ingresado al turno de caja exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                modalAbono.DialogResult = DialogResult.OK;
                modalAbono.Close();
            };

            modalAbono.AcceptButton = btnGuardar;
            modalAbono.Controls.AddRange(new Control[] { lblInf, lblM, txtM, lblMp, cbMp, btnGuardar });

            if (modalAbono.ShowDialog(this) == DialogResult.OK)
            {
                CargarReservas();
            }
        }

        private void BtnEntregarLiquidar_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var turno = _cajaService.ObtenerTurnoAbierto(_usuarioActual.NombreUsuario);
            if (turno == null || turno.Estado != "Abierta")
            {
                MessageBox.Show("Debe tener un turno de caja abierto para liquidar el pedido y emitir la boleta.", "Caja Cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal saldo = _reservaSeleccionada.SaldoPendiente;
            string medioPagoFinal = "Efectivo";

            // Si aún queda saldo pendiente por cancelar al momento de retirar
            if (saldo > 0)
            {
                using Form modalCobro = new Form
                {
                    Text = $"Cobro Saldo Final - {_reservaSeleccionada.CodigoReserva}",
                    Size = new Size(360, 240),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };

                Label lblT = new Label { Text = $"SALDO A LIQUIDAR: {MonedaHelper.Formatear(saldo, conSigno: true)}", Location = new Point(20, 16), Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237), AutoSize = true };
                Label lblM = new Label { Text = "Medio de Pago del Saldo:", Location = new Point(20, 52), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
                ComboBox cbM = new ComboBox { Location = new Point(20, 74), Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
                cbM.Items.AddRange(new object[] { "Efectivo", "Débito", "Tarjeta Crédito", "Transferencia" });
                cbM.SelectedIndex = 0;

                Button btnOk = new Button { Text = "✔ Confirmar Pago y Emitir Boleta", Location = new Point(20, 130), Size = new Size(300, 42), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Click += (s, ev) =>
                {
                    medioPagoFinal = cbM.SelectedItem?.ToString() ?? "Efectivo";
                    modalCobro.DialogResult = DialogResult.OK;
                    modalCobro.Close();
                };

                modalCobro.Controls.AddRange(new Control[] { lblT, lblM, cbM, btnOk });
                if (modalCobro.ShowDialog(this) != DialogResult.OK) return;
            }

            try
            {
                // 1. Obtener detalles de la reserva y mapearlos a DetalleCarrito
                var detalles = _reservaService.ObtenerDetallesReserva(_reservaSeleccionada.IdReserva);
                List<DetalleCarrito> itemsCarrito = detalles.Select(d => new DetalleCarrito
                {
                    ProductoID = d.ProductoID,
                    Nombre = d.NombreProducto,
                    PrecioUnitario = d.PrecioUnitario,
                    Cantidad = d.Cantidad,
                    EsPesable = d.EsPesable
                }).ToList();

                // 2. Generar el ticket en TVE2607; devuelve el NroTicket generado
                var ventaService = new VentaService();
                int nroTicketGenerado = ventaService.GenerarTicketVenta(
                    itemsCarrito, 
                    _reservaSeleccionada.UsuarioRegistro, 
                    _reservaSeleccionada.NombreCliente, 
                    _reservaSeleccionada.RutCliente
                );

                // 3. Obtener el ticket directamente por su NroTicket
                TVE2607? ticketVenta;
                using (var db = new AppDbContext())
                {
                    ticketVenta = db.TVE2607.FirstOrDefault(t => t.NroTicket == nroTicketGenerado);
                }

                if (ticketVenta == null)
                {
                    MessageBox.Show($"No se encontró en base de datos el ticket N° {nroTicketGenerado}.", "Error Ticket", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 4. Procesar cobro oficial y emisión de Boleta Electrónica en CajaService
                string medioTexto = saldo > 0 ? $"Reserva (Abonos + {medioPagoFinal})" : "Reserva (Abonos Previos)";

                int folioBoleta = _cajaService.ProcesarCobroTicket(
                    ticketVenta, 
                    "Boleta Electrónica", 
                    medioTexto, 
                    0, 
                    turno.CajaTurnoID, 
                    _usuarioActual.NombreUsuario
                );

                // 5. Finalizar la reserva en su tabla propia y registrar el abono final (si hubo)
                _reservaService.EntregarYDescontarStock(
                    _reservaSeleccionada.IdReserva, 
                    saldo, 
                    medioPagoFinal, 
                    turno.CajaTurnoID, 
                    _usuarioActual.NombreUsuario, 
                    folioBoleta
                );

                MessageBox.Show($"¡PEDIDO ENTREGADO CON ÉXITO!\n\n• Boleta Electrónica: N° {folioBoleta}\n• Total: {MonedaHelper.Formatear(_reservaSeleccionada.TotalPedido, conSigno: true)}\n• Estado actualizado a 'Entregado'.", "Entrega Finalizada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 6. Visualizar modal del ticket final
                try
                {
                    FormTicketModal modalTicket = new FormTicketModal(ticketVenta, itemsCarrito, _reservaSeleccionada.TotalPedido, 0);
                    modalTicket.ShowDialog(this);
                }
                catch { }

                CargarReservas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al liquidar encargo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReimprimir_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var detalles = _reservaService.ObtenerDetallesReserva(_reservaSeleccionada.IdReserva);
            _reservaSeleccionada.Detalles = detalles;

            _ticketPrintService.ImprimirComprobanteReserva(_reservaSeleccionada, null, _usuarioActual.NombreUsuario);
        }

        private void BtnAnular_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var r = MessageBox.Show($"¿Está seguro de anular la Reserva N° {_reservaSeleccionada.CodigoReserva}?\n\n⚠️ Los abonos quedarán registrados en el historial de la caja.", "Anulación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                _reservaService.AnularReserva(_reservaSeleccionada.IdReserva);
                CargarReservas();
            }
        }

        private Button CrearBotonAccion(string texto, Color bg, Color fore, int top)
        {
            Button btn = new Button
            {
                Text = texto,
                Location = new Point(10, top + 8),
                Size = new Size(205, 38),
                BackColor = bg,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 30;
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }
    }
}