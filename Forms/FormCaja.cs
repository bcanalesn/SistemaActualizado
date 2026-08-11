using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormCaja : Form
    {
        private readonly CajaService _cajaService = new CajaService();
        private static CajaTurno? _turnoActual = null;
        private Usuario? _usuarioActual;

        // Controles de Encabezado Superior
        private Button btnAbrirCaja = null!;
        private Label lblEstadoTag = null!;
        private Label lblEstadoDetalle = null!;
        private Label lblHoraApertura = null!;
        private Label lblFondoInicial = null!;
        private Button btnVerResumen = null!;

        // Controles de búsqueda y filtros
        private TextBox txtBuscarTicket = null!;
        private Button btnLimpiar = null!;
        private Label lblTotalResultados = null!;
        private Label lblUltimaActualizacion = null!;

        // Grillas
        private DataGridView dgvTicketsPendientes = null!;
        private DataGridView dgvDetalleTicket = null!;

        // Opciones de cobro DTE
        private ComboBox cbTipoDocumentoCobro = null!;
        private ComboBox cbMedioPago = null!;
        private TextBox txtPagaCon = null!;
        private Label lblVuelto = null!;
        private Label lblTotalCobrar = null!;

        // Botones de acción principales
        private Button btnCobrarTicket = null!;
        private Button btnImprimirVistaPrevia = null!;
        private Button btnAnularTicket = null!;

        // Panel de Bloqueo / Opacidad (Caja Cerrada)
        private Panel pnlBloqueoCaja = null!;

        private TVE2607? _ticketSeleccionado = null;

        public FormCaja(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            ActualizarEstadoCajaUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // ==========================================
            // 1. ENCABEZADO SUPERIOR
            // ==========================================
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 15)
            };

            Label lblTitulo = new Label
            {
                Text = "Caja",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, 8),
                AutoSize = true
            };

            btnAbrirCaja = new Button
            {
                Text = "🏪 Abrir Caja",
                Location = new Point(85, 12),
                Size = new Size(115, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAbrirCaja.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnAbrirCaja.Click += BtnAbrirCaja_Click;

            // Panel Metricas
            FlowLayoutPanel flpMetricas = new FlowLayoutPanel
            {
                Location = new Point(380, 5),
                Size = new Size(490, 52),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            Panel cardEstado = CrearCardEncabezado("🟢 CAJA ABIERTA", "Turno #----", out lblEstadoTag, out lblEstadoDetalle);
            Panel cardHora = CrearCardEncabezado("🕒 INICIO TURNO", "--:--", out _, out lblHoraApertura);
            Panel cardFondo = CrearCardEncabezado("🛍️ FONDO INICIAL", "$ 0", out _, out lblFondoInicial);

            flpMetricas.Controls.Add(cardEstado);
            flpMetricas.Controls.Add(cardHora);
            flpMetricas.Controls.Add(cardFondo);

            btnVerResumen = new Button
            {
                Text = "📈 Ver resumen del turno",
                Location = new Point(880, 12),
                Size = new Size(170, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnVerResumen.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnVerResumen.Click += BtnVerResumen_Click;

            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, btnAbrirCaja, flpMetricas, btnVerResumen });

            // ==========================================
            // 2. CUERPO DE TRABAJO (2 COLUMNAS)
            // ==========================================
            Panel pnlWorkArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            TableLayoutPanel gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            // COLUMNA IZQUIERDA
            Panel pnlColIzquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };

            // Panel Búsqueda
            Panel pnlCardBusqueda = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 10) };
            Label lblTitBuscar = new Label { Text = "1. BUSCAR TICKET PENDIENTE", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(12, 6), AutoSize = true };

            txtBuscarTicket = new TextBox { Location = new Point(12, 26), Size = new Size(240, 26), Font = new Font("Segoe UI", 9.5F) };
            txtBuscarTicket.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarTicketsPendientes(txtBuscarTicket.Text.Trim()); e.SuppressKeyPress = true; } };

            Button btnBuscar = new Button { Text = "🔍 Buscar", Location = new Point(258, 25), Size = new Size(80, 28), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarTicketsPendientes(txtBuscarTicket.Text.Trim());

            Button btnRecargar = new Button { Text = "🔄 Recargar", Location = new Point(343, 25), Size = new Size(88, 28), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(37, 99, 235), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRecargar.FlatAppearance.BorderSize = 0;
            btnRecargar.Click += (s, e) => CargarTicketsPendientes();

            btnLimpiar = new Button { Text = "🗑️ Limpiar", Location = new Point(436, 25), Size = new Size(80, 28), BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(239, 68, 68), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => { txtBuscarTicket.Clear(); CargarTicketsPendientes(); };

            pnlCardBusqueda.Controls.AddRange(new Control[] { lblTitBuscar, txtBuscarTicket, btnBuscar, btnRecargar, btnLimpiar });

            // Tabla Tickets Pendientes
            Panel pnlCardPendientes = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 10, 0, 10) };
            Label lblTitPend = new Label { Text = "2. TICKETS PENDIENTES DE PAGO", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Top, Height = 22 };

            dgvTicketsPendientes = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            ConfigurarEstiloTabla(dgvTicketsPendientes);
            dgvTicketsPendientes.SelectionChanged += DgvTicketsPendientes_SelectionChanged;

            Panel pnlStatusFooter = new Panel { Dock = DockStyle.Bottom, Height = 25, BackColor = Color.White };
            lblTotalResultados = new Label { Text = "✔ 0 tickets encontrados", Location = new Point(5, 5), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74) };
            lblUltimaActualizacion = new Label { Text = $"Última actualización: {DateTime.Now:HH:mm:ss} 🔄", Dock = DockStyle.Right, AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
            pnlStatusFooter.Controls.Add(lblTotalResultados);
            pnlStatusFooter.Controls.Add(lblUltimaActualizacion);

            pnlCardPendientes.Controls.Add(dgvTicketsPendientes);
            pnlCardPendientes.Controls.Add(pnlStatusFooter);
            pnlCardPendientes.Controls.Add(lblTitPend);

            // Tabla Detalle Ticket
            Panel pnlCardDetalle = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            Label lblTitDet = new Label { Text = "3. DETALLE DEL TICKET SELECCIONADO", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Top, Height = 22 };

            dgvDetalleTicket = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            ConfigurarEstiloTabla(dgvDetalleTicket);

            pnlCardDetalle.Controls.Add(dgvDetalleTicket);
            pnlCardDetalle.Controls.Add(lblTitDet);

            pnlColIzquierda.Controls.Add(pnlCardDetalle);
            pnlColIzquierda.Controls.Add(pnlCardPendientes);
            pnlColIzquierda.Controls.Add(pnlCardBusqueda);

            // COLUMNA DERECHA (Opciones de Cobro)
            Panel pnlColDerecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };
            Label lblTitCobro = new Label { Text = "4. OPCIONES DE COBRO Y EMISIÓN DTE", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(15, 12), AutoSize = true };

            Label lblTipoDoc = new Label { Text = "TIPO DE DOCUMENTO", Location = new Point(15, 45), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            cbTipoDocumentoCobro = new ComboBox { Location = new Point(15, 63), Size = new Size(180, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbTipoDocumentoCobro.Items.AddRange(new string[] { "Boleta Electrónica", "Factura Electrónica" });
            cbTipoDocumentoCobro.SelectedIndex = 0;

            Label lblMedio = new Label { Text = "MEDIO DE PAGO", Location = new Point(205, 45), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            cbMedioPago = new ComboBox { Location = new Point(205, 63), Size = new Size(180, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbMedioPago.Items.AddRange(new string[] { "Efectivo", "Tarjeta Débito", "Tarjeta Crédito", "Transferencia", "Pago Mixto" });
            cbMedioPago.SelectedIndex = 0;

            Label lblPaga = new Label { Text = "PAGA CON ($)", Location = new Point(15, 105), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtPagaCon = new TextBox { Location = new Point(15, 123), Size = new Size(230, 32), Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            txtPagaCon.TextChanged += TxtPagaCon_TextChanged;

            Panel cardVuelto = new Panel { Location = new Point(255, 105), Size = new Size(130, 50), BackColor = Color.FromArgb(240, 249, 255) };
            Label lblVuelCap = new Label { Text = "VUELTO", Location = new Point(8, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161) };
            lblVuelto = new Label { Text = "$ 0", Location = new Point(8, 22), AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };
            cardVuelto.Controls.AddRange(new Control[] { lblVuelCap, lblVuelto });

            Panel pnlCardTotal = new Panel { Location = new Point(15, 170), Size = new Size(370, 60), BackColor = Color.FromArgb(238, 242, 255) };
            Label lblTotCap = new Label { Text = "TOTAL A COBRAR", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(49, 46, 129) };
            lblTotalCobrar = new Label { Text = "$ 0", Location = new Point(15, 26), AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235) };
            pnlCardTotal.Controls.AddRange(new Control[] { lblTotCap, lblTotalCobrar });

            btnCobrarTicket = new Button
            {
                Text = "⚡ COBRAR Y EMITIR DTE\nConfirmar pago y emitir documento",
                Location = new Point(15, 242),
                Size = new Size(370, 52),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnCobrarTicket.FlatAppearance.BorderSize = 0;
            btnCobrarTicket.Click += BtnCobrarTicket_Click;

            btnImprimirVistaPrevia = new Button
            {
                Text = "🖨️ Imprimir Vista Previa",
                Location = new Point(15, 305),
                Size = new Size(180, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImprimirVistaPrevia.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnImprimirVistaPrevia.Click += BtnImprimirVistaPrevia_Click;

            btnAnularTicket = new Button
            {
                Text = "🚫 Anular Ticket",
                Location = new Point(205, 305),
                Size = new Size(180, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(239, 68, 68),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnularTicket.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnAnularTicket.Click += BtnAnularTicket_Click;

            pnlColDerecha.Controls.AddRange(new Control[] {
                lblTitCobro, lblTipoDoc, cbTipoDocumentoCobro, lblMedio, cbMedioPago,
                lblPaga, txtPagaCon, cardVuelto, pnlCardTotal, btnCobrarTicket,
                btnImprimirVistaPrevia, btnAnularTicket
            });

            gridLayout.Controls.Add(pnlColIzquierda, 0, 0);
            gridLayout.Controls.Add(pnlColDerecha, 1, 0);

            pnlWorkArea.Controls.Add(gridLayout);

            // ==========================================
            // 3. CAPA / PANEL DE BLOQUEO (CAJA CERRADA)
            // ==========================================
            pnlBloqueoCaja = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(160, 241, 245, 249), // Opaco/Translúcido
                Visible = true
            };

            Label lblAvisoBloqueo = new Label
            {
                Text = "🔒 LA CAJA SE ENCUENTRA CERRADA\nPresione el botón 'Abrir Caja' en la barra superior para habilitar cobros.",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlBloqueoCaja.Controls.Add(lblAvisoBloqueo);

            pnlMainContainer.Controls.Add(pnlBloqueoCaja);
            pnlMainContainer.Controls.Add(pnlWorkArea);
            pnlMainContainer.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMainContainer);
            this.ResumeLayout(false);

            CargarTicketsPendientes();
        }

        private Panel CrearCardEncabezado(string titulo, string detalle, out Label lblTag, out Label lblVal)
        {
            Panel pnl = new Panel { Size = new Size(150, 48), BackColor = Color.White, Margin = new Padding(3) };
            lblTag = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), Location = new Point(8, 6), AutoSize = true };
            lblVal = new Label { Text = detalle, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(8, 22), AutoSize = true };
            pnl.Controls.Add(lblTag);
            pnl.Controls.Add(lblVal);
            return pnl;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 252, 231);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 101, 52);
            dgv.RowTemplate.Height = 32;
            dgv.GridColor = Color.FromArgb(241, 245, 249);
        }

        private void ActualizarEstadoCajaUI()
        {
            bool estaAbierta = (_turnoActual != null && _turnoActual.Estado == "Abierta");

            pnlBloqueoCaja.Visible = !estaAbierta;
            if (pnlBloqueoCaja.Visible) pnlBloqueoCaja.BringToFront();

            if (estaAbierta)
            {
                btnAbrirCaja.Text = "🔒 Cerrar Caja";
                btnAbrirCaja.ForeColor = Color.FromArgb(239, 68, 68);

                lblEstadoTag.Text = "🟢 CAJA ABIERTA";
                lblEstadoDetalle.Text = $"Turno #{_turnoActual!.CajaTurnoID}";
                lblHoraApertura.Text = _turnoActual.FechaApertura.ToString("HH:mm:ss");
                lblFondoInicial.Text = $"$ {_turnoActual.MontoInicial:N0}";
            }
            else
            {
                btnAbrirCaja.Text = "🏪 Abrir Caja";
                btnAbrirCaja.ForeColor = Color.FromArgb(37, 99, 235);

                lblEstadoTag.Text = "🔴 CAJA CERRADA";
                lblEstadoDetalle.Text = "Turno --";
                lblHoraApertura.Text = "--:--";
                lblFondoInicial.Text = "$ 0";
            }
        }

        private void BtnAbrirCaja_Click(object? sender, EventArgs e)
        {
            if (_turnoActual != null && _turnoActual.Estado == "Abierta")
            {
                // Cierre de Caja
                decimal ventasEfectivo = _cajaService.CalcularVentasEfectivo(_turnoActual.FechaApertura);
                decimal totalEsperado = _turnoActual.MontoInicial + ventasEfectivo;

                var resp = MessageBox.Show($"¿Desea cerrar la caja?\n\n• Fondo Inicial: ${_turnoActual.MontoInicial:N0}\n• Ventas Efectivo: ${ventasEfectivo:N0}\n• Total Esperado: ${totalEsperado:N0}", "Cierre de Caja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (resp == DialogResult.Yes)
                {
                    _turnoActual = null;
                    ActualizarEstadoCajaUI();
                    MessageBox.Show("Caja cerrada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                // Apertura de Caja Modal Rápido
                Form modalApertura = new Form
                {
                    Text = "Apertura de Caja",
                    Size = new Size(320, 210),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };

                Label lblM = new Label { Text = "Monto Inicial de Caja ($):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                TextBox txtM = new TextBox { Text = "20000", Location = new Point(20, 45), Size = new Size(260, 28), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

                Button btnA = new Button { Text = "🚀 Iniciar Turno", Location = new Point(20, 90), Size = new Size(260, 40), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnA.FlatAppearance.BorderSize = 0;

                btnA.Click += (sa, ea) =>
                {
                    if (decimal.TryParse(txtM.Text.Trim(), out decimal monto) && monto >= 0)
                    {
                        _turnoActual = new CajaTurno
                        {
                            CajaTurnoID = (int)(DateTime.Now.Ticks % 1000),
                            Usuario = _usuarioActual?.NombreUsuario ?? "Bárbara",
                            FechaApertura = DateTime.Now,
                            MontoInicial = monto,
                            Estado = "Abierta"
                        };

                        modalApertura.DialogResult = DialogResult.OK;
                        modalApertura.Close();
                    }
                };

                modalApertura.Controls.AddRange(new Control[] { lblM, txtM, btnA });
                if (modalApertura.ShowDialog(this) == DialogResult.OK)
                {
                    ActualizarEstadoCajaUI();
                }
            }
        }

        private void BtnVerResumen_Click(object? sender, EventArgs e)
        {
            if (_turnoActual == null)
            {
                MessageBox.Show("La caja debe estar abierta para consultar el resumen del turno.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal ventasEfectivo = _cajaService.CalcularVentasEfectivo(_turnoActual.FechaApertura);
            decimal totalEsperado = _turnoActual.MontoInicial + ventasEfectivo;

            MessageBox.Show($"📊 RESUMEN DEL TURNO ACTUAL\n\n" +
                            $"• Cajero Activo: {_turnoActual.Usuario}\n" +
                            $"• Hora Apertura: {_turnoActual.FechaApertura:HH:mm:ss}\n" +
                            $"• Fondo Inicial: ${_turnoActual.MontoInicial:N0}\n" +
                            $"• Ventas Efectivo: ${ventasEfectivo:N0}\n" +
                            $"-----------------------------------\n" +
                            $"• Efectivo Total Esperado: ${totalEsperado:N0}",
                            "Resumen de Turno", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CargarTicketsPendientes(string filtro = "")
        {
            try
            {
                var pendientes = _cajaService.ObtenerTicketsPendientes(filtro);
                dgvTicketsPendientes.DataSource = pendientes;

                if (dgvTicketsPendientes.Columns["nroDTE"] != null) dgvTicketsPendientes.Columns["nroDTE"].HeaderText = "N° TICKET";
                if (dgvTicketsPendientes.Columns["FecDoc"] != null) { dgvTicketsPendientes.Columns["FecDoc"].HeaderText = "HORA"; dgvTicketsPendientes.Columns["FecDoc"].DefaultCellStyle.Format = "HH:mm:ss"; }
                if (dgvTicketsPendientes.Columns["RazonSocial"] != null) dgvTicketsPendientes.Columns["RazonSocial"].HeaderText = "CLIENTE";
                if (dgvTicketsPendientes.Columns["Total"] != null) { dgvTicketsPendientes.Columns["Total"].HeaderText = "TOTAL"; dgvTicketsPendientes.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; }
                if (dgvTicketsPendientes.Columns["UserDTE"] != null) dgvTicketsPendientes.Columns["UserDTE"].HeaderText = "VENDEDOR";

                string[] ocultar = new string[] { "idTve", "idLocal", "nmbLocal", "iddocDTE", "Documento", "nroInT", "SubTotal", "Descuento", "Neto", "Impto1", "Impto2", "Impto3", "IvA", "Vendedor", "nroZ", "Url", "nPAX", "Idcliente", "DNI", "RuT", "dv", "Giro", "Direccion", "idcomuna", "nComuna", "idCiudad", "nCiudad", "Fono1", "Fono2", "email", "status", "idREF", "nroREF", "codigoREF", "FechaREF", "HoraDoc", "Detalles" };
                foreach (var col in ocultar)
                {
                    if (dgvTicketsPendientes.Columns[col] != null) dgvTicketsPendientes.Columns[col].Visible = false;
                }

                lblTotalResultados.Text = $"✔ {pendientes.Count} ticket(s) encontrado(s)";
                lblUltimaActualizacion.Text = $"Última actualización: {DateTime.Now:HH:mm:ss} 🔄";
            }
            catch { }
        }

        private void DgvTicketsPendientes_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvTicketsPendientes.CurrentRow?.DataBoundItem is TVE2607 ticket)
            {
                _ticketSeleccionado = ticket;
                btnCobrarTicket.Enabled = true;
                btnAnularTicket.Enabled = true;

                lblTotalCobrar.Text = $"$ {ticket.Total:N0}";

                var detalles = _cajaService.ObtenerDetallesTicket(ticket.idTve);
                dgvDetalleTicket.DataSource = detalles;

                if (dgvDetalleTicket.Columns["NmbProducto"] != null) dgvDetalleTicket.Columns["NmbProducto"].HeaderText = "PRODUCTO";
                if (dgvDetalleTicket.Columns["IdProducto"] != null) dgvDetalleTicket.Columns["IdProducto"].HeaderText = "CÓDIGO";
                if (dgvDetalleTicket.Columns["Cantidad"] != null) dgvDetalleTicket.Columns["Cantidad"].HeaderText = "CANT.";
                if (dgvDetalleTicket.Columns["Precio"] != null) { dgvDetalleTicket.Columns["Precio"].HeaderText = "PRECIO UNIT."; dgvDetalleTicket.Columns["Precio"].DefaultCellStyle.Format = "$#,##0"; }
                if (dgvDetalleTicket.Columns["SubTotal"] != null) { dgvDetalleTicket.Columns["SubTotal"].HeaderText = "SUBTOTAL"; dgvDetalleTicket.Columns["SubTotal"].DefaultCellStyle.Format = "$#,##0"; }

                string[] ocultarDet = new string[] { "idTvd", "idTve", "idLocal", "iddocDTE", "Documento", "NroDTE", "NroInT", "FecMoV", "HoraMoV", "nmbCorT", "PneTo", "SubNeto", "Costo", "Tcosto", "IdVendedor", "nmbVendedor", "idFamilia", "nFamilia", "idGrupo", "nGrupo", "Unidad", "VentaEncabezado" };
                foreach (var col in ocultarDet)
                {
                    if (dgvDetalleTicket.Columns[col] != null) dgvDetalleTicket.Columns[col].Visible = false;
                }
            }
            else
            {
                _ticketSeleccionado = null;
                btnCobrarTicket.Enabled = false;
                btnAnularTicket.Enabled = false;
                lblTotalCobrar.Text = "$ 0";
                dgvDetalleTicket.DataSource = null;
            }
        }

        private void TxtPagaCon_TextChanged(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            if (decimal.TryParse(txtPagaCon.Text.Trim(), out decimal pagaCon))
            {
                decimal vuelto = pagaCon - _ticketSeleccionado.Total;
                lblVuelto.Text = vuelto >= 0 ? $"$ {vuelto:N0}" : "Insuficiente";
                lblVuelto.ForeColor = vuelto >= 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
            }
            else
            {
                lblVuelto.Text = "$ 0";
            }
        }

        private void BtnCobrarTicket_Click(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            string tipoDoc = cbTipoDocumentoCobro.SelectedItem?.ToString() ?? "Boleta Electrónica";
            string medioPago = cbMedioPago.SelectedItem?.ToString() ?? "Efectivo";

            decimal pagaCon = 0;
            decimal vuelto = 0;

            if (medioPago == "Efectivo")
            {
                if (!decimal.TryParse(txtPagaCon.Text.Trim(), out pagaCon) || pagaCon < _ticketSeleccionado.Total)
                {
                    MessageBox.Show("Ingrese un monto en efectivo suficiente para cobro.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                vuelto = pagaCon - _ticketSeleccionado.Total;
            }

            try
            {
                string usuarioNombre = _usuarioActual?.NombreUsuario ?? "Bárbara";
                int folioOficial = _cajaService.ProcesarCobroTicket(_ticketSeleccionado, tipoDoc, usuarioNombre, _ticketSeleccionado.RuT, _ticketSeleccionado.RazonSocial, "GENERAL");

                var detalles = _cajaService.ObtenerDetallesTicket(_ticketSeleccionado.idTve);
                List<DetalleCarrito> itemsCarrito = detalles.Select(item => new DetalleCarrito
                {
                    ProductoID = item.IdProducto,
                    Nombre = item.NmbProducto ?? "Producto",
                    PrecioUnitario = item.Precio,
                    Cantidad = item.Cantidad
                }).ToList();

                MessageBox.Show($"¡{tipoDoc.ToUpper()} N° {folioOficial} COBRADA CON ÉXITO!", "Cobro Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                FormTicketModal formTicket = new FormTicketModal(_ticketSeleccionado, itemsCarrito, pagaCon, vuelto);
                formTicket.ShowDialog(this);

                txtBuscarTicket.Clear();
                CargarTicketsPendientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar el cobro: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImprimirVistaPrevia_Click(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            var detalles = _cajaService.ObtenerDetallesTicket(_ticketSeleccionado.idTve);
            List<DetalleCarrito> itemsCarrito = detalles.Select(item => new DetalleCarrito
            {
                ProductoID = item.IdProducto,
                Nombre = item.NmbProducto ?? "Producto",
                PrecioUnitario = item.Precio,
                Cantidad = item.Cantidad
            }).ToList();

            FormTicketModal formTicket = new FormTicketModal(_ticketSeleccionado, itemsCarrito, 0, 0);
            formTicket.ShowDialog(this);
        }

        private void BtnAnularTicket_Click(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            var res = MessageBox.Show($"¿Desea anular el Ticket N° {_ticketSeleccionado.nroDTE}?\n\n⚠️ Esta acción devolverá los productos al stock de inventario.", "Anulación de Ticket", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                try
                {
                    _cajaService.AnularTicket(_ticketSeleccionado.idTve);
                    MessageBox.Show($"Ticket N° {_ticketSeleccionado.nroDTE} anulado con éxito.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    CargarTicketsPendientes();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al anular: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}