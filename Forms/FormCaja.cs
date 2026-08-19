using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private static CajaTurno? _turnoActual;
        private Usuario? _usuarioActual;

        private Button btnAbrirCaja = null!;
        private Label lblEstadoTag = null!;
        private Label lblEstadoDetalle = null!;
        private Label lblHoraApertura = null!;
        private Label lblFondoInicial = null!;
        private Label lblVueltoEntregado = null!;

        private TextBox txtBuscarTicket = null!;
        private Label lblTotalResultados = null!;
        private Label lblUltimaActualizacion = null!;

        private DataGridView dgvTicketsPendientes = null!;
        private DataGridView dgvDetalleTicket = null!;

        // SECCIÓN 1: BOTONES TIPO DE DOCUMENTO
        private Button btnDocBoleta = null!;
        private Button btnDocFactura = null!;
        private string _tipoDocSeleccionado = "Boleta Electrónica";

        // SECCIÓN 2: BOTONES MEDIO DE PAGO
        private Button btnPagoEfectivo = null!;
        private Button btnPagoDebito = null!;
        private Button btnPagoCredito = null!;
        private Button btnPagoTransferencia = null!;
        private Button btnPagoMultiple = null!;
        private string _medioPagoSeleccionado = "Efectivo";

        // SECCIÓN 3: DATOS DE COBRO
        private TextBox txtPagaCon = null!;
        private Label lblVuelto = null!;
        private Label lblTotalCobrar = null!;

        // SECCIÓN 4: ACCIONES PRINCIPALES
        private Button btnCobrarTicket = null!;
        private Button btnImprimirVistaPrevia = null!;
        private Button btnAnularTicket = null!;

        private Panel pnlBloqueoCaja = null!;

        private TVE2607? _ticketSeleccionado;

        // ACUMULADOR DE VUELTO ENTREGADO
        private static decimal _totalVueltoEntregadoTurno;

        // Variables Pago Múltiple
        private decimal _pagoEfectivo;
        private decimal _pagoTarjeta;
        private decimal _pagoTransferencia;
        private decimal _vueltoMixto;
        private bool _pagoMixtoConfirmado = false;

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
                Padding = new Padding(12, 5, 12, 12),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // ==========================================
            // 1. ENCABEZADO SUPERIOR
            // ==========================================
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 5)
            };

            Label lblTitulo = new Label
            {
                Text = "Caja",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, 2),
                AutoSize = true
            };

            btnAbrirCaja = new Button
            {
                Text = "🏪 Abrir Caja",
                Location = new Point(80, 6),
                Size = new Size(115, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAbrirCaja.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnAbrirCaja.Click += BtnAbrirCaja_Click;

            FlowLayoutPanel flpMetricas = new FlowLayoutPanel
            {
                Location = new Point(270, 0),
                Size = new Size(590, 48),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            Panel cardEstado = CrearCardEncabezado("🟢 CAJA ABIERTA", "Turno #----", out lblEstadoTag, out lblEstadoDetalle);
            Panel cardHora = CrearCardEncabezado("🕒 INICIO TURNO", "--:--", out _, out lblHoraApertura);
            Panel cardFondo = CrearCardEncabezado("🛍️ FONDO INICIAL", "$ 0", out _, out lblFondoInicial);
            Panel cardVueltoTurno = CrearCardEncabezado("💸 VUELTO ENTREGADO", "$ 0", out _, out lblVueltoEntregado);

            flpMetricas.Controls.Add(cardEstado);
            flpMetricas.Controls.Add(cardHora);
            flpMetricas.Controls.Add(cardFondo);
            flpMetricas.Controls.Add(cardVueltoTurno);

            Button btnVerResumen = new Button
            {
                Text = "📈 Ver resumen del turno",
                Location = new Point(870, 6),
                Size = new Size(170, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnVerResumen.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnVerResumen.Click += BtnVerResumen_Click;

            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, btnAbrirCaja, flpMetricas, btnVerResumen });

            // ==========================================
            // CUERPO DE TRABAJO (2 COLUMNAS)
            // ==========================================
            Panel pnlWorkArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            TableLayoutPanel gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // COLUMNA IZQUIERDA
            Panel pnlColIzquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };

            // 1. Panel Búsqueda
            Panel pnlCardBusqueda = CrearContenedorSeccion(2, 58, Color.FromArgb(226, 232, 240));
            Label lblTitBuscar = new Label { Text = "BUSCAR TICKET PENDIENTE", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, 5), AutoSize = true };

            // Se redujo el ancho del TextBox a 150px para dar espacio horizontal
            txtBuscarTicket = new TextBox { Location = new Point(10, 24), Size = new Size(150, 26), Font = new Font("Segoe UI", 9.5F) };
            txtBuscarTicket.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarTicketsPendientes(txtBuscarTicket.Text.Trim()); e.SuppressKeyPress = true; } };

            Button btnBuscar = new Button { Text = "🔍 Buscar", Location = new Point(168, 23), Size = new Size(76, 28), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarTicketsPendientes(txtBuscarTicket.Text.Trim());

            Button btnRecargar = new Button { Text = "🔄 Recargar", Location = new Point(250, 23), Size = new Size(84, 28), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(37, 99, 235), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRecargar.FlatAppearance.BorderSize = 0;
            btnRecargar.Click += (s, e) => CargarTicketsPendientes();

            Button btnLimpiar = new Button { Text = "🗑️ Limpiar", Location = new Point(340, 23), Size = new Size(76, 28), BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(239, 68, 68), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => { txtBuscarTicket.Clear(); CargarTicketsPendientes(); };

            pnlCardBusqueda.Controls.AddRange(new Control[] { lblTitBuscar, txtBuscarTicket, btnBuscar, btnRecargar, btnLimpiar });

            // 2. Panel Tickets Pendientes
            Panel pnlCardPendientes = CrearContenedorSeccion(68, 210, Color.FromArgb(226, 232, 240));
            Label lblTitPend = new Label { Text = "TICKETS PENDIENTES DE PAGO", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 22 };

            dgvTicketsPendientes = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            ConfigurarEstiloTabla(dgvTicketsPendientes);
            dgvTicketsPendientes.SelectionChanged += DgvTicketsPendientes_SelectionChanged;

            Panel pnlStatusFooter = new Panel { Dock = DockStyle.Bottom, Height = 25, BackColor = Color.White };
            lblTotalResultados = new Label { Text = "✔ 0 tickets encontrados", Location = new Point(5, 5), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74) };
            lblUltimaActualizacion = new Label { Text = $"Última actualización: {DateTime.Now:HH:mm:ss} 🔄", Dock = DockStyle.Right, AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(15, 23, 42) };
            pnlStatusFooter.Controls.Add(lblTotalResultados);
            pnlStatusFooter.Controls.Add(lblUltimaActualizacion);

            pnlCardPendientes.Controls.Add(dgvTicketsPendientes);
            pnlCardPendientes.Controls.Add(pnlStatusFooter);
            pnlCardPendientes.Controls.Add(lblTitPend);

            // 3. Panel Detalle Ticket sin fila extra vacía
            Panel pnlCardDetalle = CrearContenedorSeccion(286, 230, Color.FromArgb(226, 232, 240));
            Label lblTitDet = new Label { Text = "DETALLE DEL TICKET SELECCIONADO", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 22 };

            dgvDetalleTicket = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false, // DESACTIVA FILA VACÍA INFERIOR
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false
            };

            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdProducto", DataPropertyName = "IdProducto", HeaderText = "CÓDIGO", FillWeight = 20 });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "NmbProducto", DataPropertyName = "NmbProducto", HeaderText = "PRODUCTO", FillWeight = 45 });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", DataPropertyName = "Cantidad", HeaderText = "CANT.", FillWeight = 12 });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", DataPropertyName = "Precio", HeaderText = "PRECIO UNIT.", FillWeight = 23, DefaultCellStyle = new DataGridViewCellStyle { Format = "$#,##0" } });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", DataPropertyName = "SubTotal", HeaderText = "SUBTOTAL", FillWeight = 23, DefaultCellStyle = new DataGridViewCellStyle { Format = "$#,##0" } });

            ConfigurarEstiloTabla(dgvDetalleTicket);

            pnlCardDetalle.Controls.Add(dgvDetalleTicket);
            pnlCardDetalle.Controls.Add(lblTitDet);

            pnlColIzquierda.Controls.Add(pnlCardDetalle);
            pnlColIzquierda.Controls.Add(pnlCardPendientes);
            pnlColIzquierda.Controls.Add(pnlCardBusqueda);

            // =========================================================================
            // COLUMNA DERECHA
            // =========================================================================
            Panel pnlColDerecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), AutoScroll = false };

            // SECCIÓN 1: TIPO DE DOCUMENTO
            Panel pnlSec1 = CrearContenedorSeccion(2, 98, Color.FromArgb(124, 58, 237));
            Panel circle1 = CrearBadgeNumero("1", Color.FromArgb(124, 58, 237), new Point(10, 8));
            Label lblT1 = new Label { Text = "TIPO DE DOCUMENTO", Location = new Point(42, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237) };
            Label lblSub1 = new Label { Text = "Selecciona el tipo de documento a emitir", Location = new Point(42, 22), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };

            btnDocBoleta = CrearBotonTarjeVisual("📄", "BOLETA", "ELECTRÓNICA", new Point(10, 42), new Size(205, 45), Color.FromArgb(124, 58, 237), esActivo: true);
            btnDocBoleta.Click += (s, e) => SeleccionarTipoDocumento("Boleta Electrónica");

            btnDocFactura = CrearBotonTarjeVisual("📑", "FACTURA", "ELECTRÓNICA", new Point(223, 42), new Size(205, 45), Color.FromArgb(124, 58, 237), esActivo: false);
            btnDocFactura.Click += (s, e) => SeleccionarTipoDocumento("Factura Electrónica");

            pnlSec1.Controls.AddRange(new Control[] { circle1, lblT1, lblSub1, btnDocBoleta, btnDocFactura });

            // SECCIÓN 2: MEDIO DE PAGO
            Panel pnlSec2 = CrearContenedorSeccion(108, 115, Color.FromArgb(203, 213, 225));
            Panel circle2 = CrearBadgeNumero("2", Color.FromArgb(37, 99, 235), new Point(10, 8));
            Label lblT2 = new Label { Text = "MEDIO DE PAGO", Location = new Point(42, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235) };
            Label lblSub2 = new Label { Text = "Selecciona el medio de pago", Location = new Point(42, 22), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };

            btnPagoEfectivo = CrearBotonPagoVectorial("EFECTIVO", "EFECTIVO", new Point(8, 42), new Size(80, 62), Color.FromArgb(16, 185, 129), () => _medioPagoSeleccionado == "Efectivo");
            btnPagoEfectivo.Click += (s, e) => SeleccionarMedioPago("Efectivo");

            btnPagoDebito = CrearBotonPagoVectorial("DÉBITO", "DÉBITO", new Point(92, 42), new Size(80, 62), Color.FromArgb(37, 99, 235), () => _medioPagoSeleccionado == "Débito");
            btnPagoDebito.Click += (s, e) => SeleccionarMedioPago("Débito");

            btnPagoCredito = CrearBotonPagoVectorial("CRÉDITO", "CRÉDITO", new Point(176, 42), new Size(80, 62), Color.FromArgb(124, 58, 237), () => _medioPagoSeleccionado == "Crédito");
            btnPagoCredito.Click += (s, e) => SeleccionarMedioPago("Crédito");

            btnPagoTransferencia = CrearBotonPagoVectorial("TRANSFERENCIA", "TRANSFERENCIA", new Point(260, 42), new Size(88, 62), Color.FromArgb(13, 148, 136), () => _medioPagoSeleccionado == "Transferencia");
            btnPagoTransferencia.Click += (s, e) => SeleccionarMedioPago("Transferencia");

            btnPagoMultiple = CrearBotonPagoVectorial("PAGO MÚLTIPLE", "PAGO\nMÚLTIPLE", new Point(352, 42), new Size(76, 62), Color.FromArgb(234, 88, 12), () => _medioPagoSeleccionado == "Pago Múltiple");
            btnPagoMultiple.Click += (s, e) => SeleccionarMedioPago("Pago Múltiple");

            pnlSec2.Controls.AddRange(new Control[] { circle2, lblT2, lblSub2, btnPagoEfectivo, btnPagoDebito, btnPagoCredito, btnPagoTransferencia, btnPagoMultiple });

            // SECCIÓN 3: DATOS DE COBRO (ALINEACIÓN DE PAGA CON CORREGIDA)
            Panel pnlDatosCobroCard = CrearContenedorSeccion(231, 162, Color.FromArgb(16, 185, 129));
            Panel circle3 = CrearBadgeNumero("3", Color.FromArgb(16, 185, 129), new Point(10, 8));
            Label lblT3 = new Label { Text = "DATOS DE COBRO", Location = new Point(42, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };
            Label lblSub3 = new Label { Text = "Ingresa el monto recibido (solo para Efectivo o Pago Múltiple)", Location = new Point(42, 22), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };

            Panel pnlPagaCon = new Panel { Location = new Point(10, 42), Size = new Size(210, 52), BackColor = Color.FromArgb(248, 250, 252) };
            pnlPagaCon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlPagaCon.Width - 1, pnlPagaCon.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1.5f);
                e.Graphics.DrawPath(pen, p);
            };

            Label lblP = new Label { Text = "PAGA CON", Location = new Point(8, 4), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
            Label lblSigno = new Label { Text = "$", Location = new Point(8, 22), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtPagaCon = new TextBox { Location = new Point(28, 20), Size = new Size(172, 26), Font = new Font("Segoe UI", 11F, FontStyle.Bold), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(248, 250, 252) };
            txtPagaCon.TextChanged += TxtPagaCon_TextChanged;
            pnlPagaCon.Controls.AddRange(new Control[] { lblP, lblSigno, txtPagaCon });

            Panel pnlVueltoCard = new Panel { Location = new Point(226, 42), Size = new Size(202, 52), BackColor = Color.FromArgb(240, 253, 244) };
            pnlVueltoCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlVueltoCard.Width - 1, pnlVueltoCard.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(Color.FromArgb(187, 247, 208), 1.5f);
                e.Graphics.DrawPath(pen, p);
            };

            Label lblVIcon = new Label { Text = "💵 VUELTO", Location = new Point(8, 5), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 101, 52) };
            lblVuelto = new Label { Text = "$0", Location = new Point(70, 12), Size = new Size(124, 30), Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), TextAlign = ContentAlignment.MiddleRight };
            pnlVueltoCard.Controls.AddRange(new Control[] { lblVIcon, lblVuelto });

            Panel pnlTotalCard = new Panel { Location = new Point(10, 100), Size = new Size(418, 50), BackColor = Color.FromArgb(245, 243, 255) };
            pnlTotalCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlTotalCard.Width - 1, pnlTotalCard.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(Color.FromArgb(221, 214, 254), 1.5f);
                e.Graphics.DrawPath(pen, p);
            };

            Panel pnlTotCircle = CrearBadgeNumero("$", Color.FromArgb(124, 58, 237), new Point(8, 10));
            Label lblTotCap = new Label { Text = "TOTAL A COBRAR", Location = new Point(42, 5), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237) };
            lblTotalCobrar = new Label { Text = "$0", Location = new Point(42, 18), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237) };
            pnlTotalCard.Controls.AddRange(new Control[] { pnlTotCircle, lblTotCap, lblTotalCobrar });

            pnlDatosCobroCard.Controls.AddRange(new Control[] { circle3, lblT3, lblSub3, pnlPagaCon, pnlVueltoCard, pnlTotalCard });

            // SECCIÓN 4: ACCIONES PRINCIPALES
            Panel pnlSec4 = CrearContenedorSeccion(401, 118, Color.FromArgb(234, 88, 12));
            Panel circle4 = CrearBadgeNumero("4", Color.FromArgb(234, 88, 12), new Point(10, 8));
            Label lblT4 = new Label { Text = "ACCIONES PRINCIPALES", Location = new Point(42, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(234, 88, 12) };

            btnCobrarTicket = new Button
            {
                Text = "⚡  COBRAR Y EMITIR DTE",
                Location = new Point(10, 38),
                Size = new Size(418, 38),
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
                Text = "🖨️  IMPRIMIR VISTA PREVIA",
                Location = new Point(10, 80),
                Size = new Size(202, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImprimirVistaPrevia.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnImprimirVistaPrevia.Click += BtnImprimirVistaPrevia_Click;

            btnAnularTicket = new Button
            {
                Text = "🚫  ANULAR TICKET",
                Location = new Point(220, 80),
                Size = new Size(208, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(239, 68, 68),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnularTicket.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnAnularTicket.Click += BtnAnularTicket_Click;

            pnlSec4.Controls.AddRange(new Control[] { circle4, lblT4, btnCobrarTicket, btnImprimirVistaPrevia, btnAnularTicket });

            pnlColDerecha.Controls.AddRange(new Control[] { pnlSec1, pnlSec2, pnlDatosCobroCard, pnlSec4 });

            gridLayout.Controls.Add(pnlColIzquierda, 0, 0);
            gridLayout.Controls.Add(pnlColDerecha, 1, 0);

            pnlWorkArea.Controls.Add(gridLayout);

            // CAPA DE BLOQUEO (CAJA CERRADA)
            pnlBloqueoCaja = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(160, 241, 245, 249),
                Visible = true
            };

            Label lblAvisoBloqueo = new Label
            {
                Text = "🔒 LA CAJA SE ENCUENTRA CERRADA\nPresione el botón 'Abrir Caja' en la barra superior para habilitar cobros.",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
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

        // RESETEA LA VISTA Y DETALLE AL COMPLETAR COBRO O ANULACIÓN
        private void LimpiarSeleccionVista()
        {
            _ticketSeleccionado = null;
            _pagoMixtoConfirmado = false;
            
            dgvDetalleTicket.DataSource = null;
            
            lblTotalCobrar.Text = "$0";
            lblVuelto.Text = "$0";
            txtPagaCon.Clear();
            
            btnCobrarTicket.Enabled = false;
            btnAnularTicket.Enabled = false;
            
            SeleccionarMedioPago("Efectivo");
        }

        // ==========================================
        // DIBUJADO VECTORIAL Y ESQUINAS REDONDEADAS
        // ==========================================
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

        private Panel CrearContenedorSeccion(int y, int alto, Color colorBorde)
        {
            Panel pnl = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(440, alto),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(rect, 8);
                using Pen pen = new Pen(colorBorde, 1.5f);
                e.Graphics.DrawPath(pen, path);
            };
            return pnl;
        }

        private Panel CrearBadgeNumero(string texto, Color colorFondo, Point ubicacion)
        {
            Panel pnl = new Panel
            {
                Location = ubicacion,
                Size = new Size(24, 24),
                BackColor = Color.Transparent
            };

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using SolidBrush brush = new SolidBrush(colorFondo);
                e.Graphics.FillEllipse(brush, 0, 0, pnl.Width - 1, pnl.Height - 1);

                using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using Font font = new Font("Segoe UI", 9F, FontStyle.Bold);
                e.Graphics.DrawString(texto, font, Brushes.White, new RectangleF(0, 0, pnl.Width, pnl.Height), sf);
            };
            return pnl;
        }

        private Button CrearBotonTarjeVisual(string icono, string linea1, string linea2, Point loc, Size tamano, Color colorTema, bool esActivo)
        {
            Button btn = new Button
            {
                Text = $"{icono}   {linea1}\n    {linea2}",
                Location = loc,
                Size = tamano,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            AplicarEstiloBotonDocumento(btn, colorTema, esActivo);
            return btn;
        }

        private void AplicarEstiloBotonDocumento(Button btn, Color colorTema, bool esActivo)
        {
            if (esActivo)
            {
                btn.BackColor = colorTema;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
            }
            else
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(15, 23, 42);
                btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                btn.FlatAppearance.BorderSize = 1;
            }
        }

        private Button CrearBotonPagoVectorial(string tipo, string titulo, Point loc, Size tamano, Color colorTema, Func<bool> fnEstaSeleccionado)
        {
            Button btn = new Button
            {
                Location = loc,
                Size = tamano,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.White
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                bool esActivo = fnEstaSeleccionado();

                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(rect, 8);

                if (esActivo && tipo == "PAGO MÚLTIPLE")
                {
                    using SolidBrush bg = new SolidBrush(Color.FromArgb(254, 243, 199));
                    g.FillPath(bg, path);
                    using Pen p = new Pen(colorTema, 2.5f);
                    g.DrawPath(p, path);
                }
                else if (esActivo)
                {
                    using SolidBrush bg = new SolidBrush(Color.FromArgb(240, 253, 244));
                    g.FillPath(bg, path);
                    using Pen p = new Pen(colorTema, 2.5f);
                    g.DrawPath(p, path);
                }
                else
                {
                    using SolidBrush bg = new SolidBrush(Color.White);
                    g.FillPath(bg, path);
                    using Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.5f);
                    g.DrawPath(p, path);
                }

                int iconX = (btn.Width - 30) / 2;
                int iconY = 8;
                Color colorIcono = (esActivo || tipo == "PAGO MÚLTIPLE") ? colorTema : Color.FromArgb(100, 116, 139);

                DibujarIconoVectorial(g, tipo, iconX, iconY, colorIcono);

                using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using Font font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
                using SolidBrush brushText = new SolidBrush(Color.FromArgb(15, 23, 42));

                RectangleF rectTexto = new RectangleF(0, 30, btn.Width, btn.Height - 30);
                g.DrawString(titulo, font, brushText, rectTexto, sf);
            };

            return btn;
        }

        private void DibujarIconoVectorial(Graphics g, string tipo, int x, int y, Color color)
        {
            using SolidBrush brush = new SolidBrush(color);
            using Pen pen = new Pen(color, 2f);

            switch (tipo)
            {
                case "EFECTIVO":
                    using (GraphicsPath bPath = CrearRutaRedondeada(new Rectangle(x, y + 2, 30, 18), 3))
                    {
                        g.FillPath(brush, bPath);
                    }
                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(whiteB, x + 10, y + 6, 10, 10);
                    }
                    g.FillEllipse(brush, x + 12, y + 8, 6, 6);
                    break;

                case "DÉBITO":
                case "CRÉDITO":
                    using (GraphicsPath tPath = CrearRutaRedondeada(new Rectangle(x + 1, y + 2, 28, 18), 3))
                    {
                        g.FillPath(brush, tPath);
                    }
                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(whiteB, x + 1, y + 6, 28, 4);
                        g.FillRectangle(whiteB, x + 5, y + 13, 6, 3);
                    }
                    break;

                case "TRANSFERENCIA":
                    Point[] techo = { new Point(x + 15, y + 2), new Point(x + 2, y + 8), new Point(x + 28, y + 8) };
                    g.FillPolygon(brush, techo);
                    g.FillRectangle(brush, x + 4, y + 9, 22, 2);
                    g.FillRectangle(brush, x + 6, y + 12, 3, 6);
                    g.FillRectangle(brush, x + 12, y + 12, 3, 6);
                    g.FillRectangle(brush, x + 18, y + 12, 3, 6);
                    g.FillRectangle(brush, x + 3, y + 18, 24, 2);
                    break;

                case "PAGO MÚLTIPLE":
                    pen.Width = 2.5f;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.ArrowAnchor;

                    g.FillRectangle(brush, x + 2, y + 9, 8, 4);
                    g.DrawLine(pen, new Point(x + 8, y + 11), new Point(x + 22, y + 4));
                    g.DrawLine(pen, new Point(x + 8, y + 11), new Point(x + 22, y + 18));
                    break;
            }
        }

        private void SeleccionarTipoDocumento(string tipoDoc)
        {
            _tipoDocSeleccionado = tipoDoc;
            AplicarEstiloBotonDocumento(btnDocBoleta, Color.FromArgb(124, 58, 237), _tipoDocSeleccionado == "Boleta Electrónica");
            AplicarEstiloBotonDocumento(btnDocFactura, Color.FromArgb(124, 58, 237), _tipoDocSeleccionado == "Factura Electrónica");
        }

        private void SeleccionarMedioPago(string medio)
        {
            _medioPagoSeleccionado = medio;

            btnPagoEfectivo.Invalidate();
            btnPagoDebito.Invalidate();
            btnPagoCredito.Invalidate();
            btnPagoTransferencia.Invalidate();
            btnPagoMultiple.Invalidate();

            if (_medioPagoSeleccionado == "Pago Múltiple")
            {
                if (_ticketSeleccionado == null)
                {
                    MessageBox.Show("Seleccione un ticket de la lista antes de configurar el Pago Múltiple.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SeleccionarMedioPago("Efectivo");
                    return;
                }

                AbrirModalPagoMultiplesMedios(_ticketSeleccionado.Total);
            }
            else
            {
                _pagoMixtoConfirmado = false;
                txtPagaCon.Enabled = (_medioPagoSeleccionado == "Efectivo");

                if (_medioPagoSeleccionado != "Efectivo" && _ticketSeleccionado != null)
                {
                    txtPagaCon.Text = _ticketSeleccionado.Total.ToString();
                    lblVuelto.Text = "$0";
                }
            }
        }

        private Panel CrearCardEncabezado(string titulo, string detalle, out Label lblTag, out Label lblVal)
        {
            Panel pnl = new Panel { Size = new Size(140, 44), BackColor = Color.White, Margin = new Padding(3) };
            lblTag = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), Location = new Point(8, 4), AutoSize = true };
            lblVal = new Label { Text = detalle, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(8, 20), AutoSize = true };
            pnl.Controls.Add(lblTag);
            pnl.Controls.Add(lblVal);
            return pnl;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersHeight = 32;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 252, 231);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 101, 52);
            dgv.RowTemplate.Height = 32;
            dgv.GridColor = Color.FromArgb(226, 232, 240);

            dgv.CellFormatting += (s, e) =>
            {
                if (dgv.Columns[e.ColumnIndex].HeaderCell.Style.BackColor != Color.FromArgb(30, 41, 59))
                {
                    dgv.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(30, 41, 59);
                    dgv.Columns[e.ColumnIndex].HeaderCell.Style.ForeColor = Color.White;
                }
            };
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
                lblVueltoEntregado.Text = $"$ {_totalVueltoEntregadoTurno:N0}";
            }
            else
            {
                btnAbrirCaja.Text = "🏪 Abrir Caja";
                btnAbrirCaja.ForeColor = Color.FromArgb(37, 99, 235);

                lblEstadoTag.Text = "🔴 CAJA CERRADA";
                lblEstadoDetalle.Text = "Turno --";
                lblHoraApertura.Text = "--:--";
                lblFondoInicial.Text = "$ 0";
                lblVueltoEntregado.Text = "$ 0";
            }
        }

        private void AbrirModalPagoMultiplesMedios(decimal totalTicket)
        {
            Form modalMixto = new Form
            {
                Text = "Configurar Pago Múltiple / Combinado",
                Size = new Size(380, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblT = new Label { Text = $"💳 TOTAL TICKET: ${totalTicket:N0}", Location = new Point(20, 15), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235), AutoSize = true };

            Label lblEf = new Label { Text = "Monto en Efectivo ($):", Location = new Point(20, 52), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtEf = new TextBox { Text = "0", Location = new Point(20, 72), Size = new Size(320, 26), Font = new Font("Segoe UI", 10F) };

            Label lblTar = new Label { Text = "Monto en Tarjeta (Débito/Crédito) ($):", Location = new Point(20, 107), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtTar = new TextBox { Text = "0", Location = new Point(20, 127), Size = new Size(320, 26), Font = new Font("Segoe UI", 10F) };

            Label lblTrans = new Label { Text = "Monto en Transferencia ($):", Location = new Point(20, 162), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtTrans = new TextBox { Text = "0", Location = new Point(20, 182), Size = new Size(320, 26), Font = new Font("Segoe UI", 10F) };

            Label lblEstadoSuma = new Label { Text = $"Falta por cubrir: ${totalTicket:N0}", Location = new Point(20, 220), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68) };

            Action recalcular = () =>
            {
                decimal.TryParse(txtEf.Text.Trim(), out decimal ef);
                decimal.TryParse(txtTar.Text.Trim(), out decimal tar);
                decimal.TryParse(txtTrans.Text.Trim(), out decimal tr);

                decimal sumaTotal = ef + tar + tr;
                decimal diferencia = totalTicket - (tar + tr);

                if (sumaTotal < totalTicket)
                {
                    lblEstadoSuma.Text = $"Falta por cubrir: ${(totalTicket - sumaTotal):N0}";
                    lblEstadoSuma.ForeColor = Color.FromArgb(239, 68, 68);
                }
                else if (ef > diferencia && ef > 0)
                {
                    decimal vuelto = ef - diferencia;
                    lblEstadoSuma.Text = $" Cubierto | Vuelto Efectivo: ${vuelto:N0}";
                    lblEstadoSuma.ForeColor = Color.FromArgb(22, 163, 74);
                }
                else if (sumaTotal == totalTicket)
                {
                    lblEstadoSuma.Text = "✔ Total cubierto exactamente";
                    lblEstadoSuma.ForeColor = Color.FromArgb(22, 163, 74);
                }
                else
                {
                    lblEstadoSuma.Text = $" Exceso en tarjeta/transferencia (+${(sumaTotal - totalTicket):N0})";
                    lblEstadoSuma.ForeColor = Color.FromArgb(239, 68, 68);
                }
            };

            txtEf.TextChanged += (s, e) => recalcular();
            txtTar.TextChanged += (s, e) => recalcular();
            txtTrans.TextChanged += (s, e) => recalcular();

            Button btnAplicar = new Button
            {
                Text = "✔ Confirmar Pago Múltiple",
                Location = new Point(20, 255),
                Size = new Size(320, 42),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAplicar.FlatAppearance.BorderSize = 0;

            btnAplicar.Click += (s, e) =>
            {
                decimal.TryParse(txtEf.Text.Trim(), out _pagoEfectivo);
                decimal.TryParse(txtTar.Text.Trim(), out _pagoTarjeta);
                decimal.TryParse(txtTrans.Text.Trim(), out _pagoTransferencia);

                decimal sumaTotal = _pagoEfectivo + _pagoTarjeta + _pagoTransferencia;
                decimal cobroEnTarjetas = _pagoTarjeta + _pagoTransferencia;

                if (cobroEnTarjetas > totalTicket)
                {
                    MessageBox.Show("El monto en tarjeta/transferencia no puede superar el total del ticket.", "Monto Excedido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (sumaTotal < totalTicket)
                {
                    MessageBox.Show($"La suma de los pagos (${sumaTotal:N0}) no cubre el total del ticket (${totalTicket:N0}).", "Monto Incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _vueltoMixto = sumaTotal - totalTicket;
                _pagoMixtoConfirmado = true;

                txtPagaCon.Text = _pagoEfectivo.ToString();
                lblVuelto.Text = $"${_vueltoMixto:N0}";

                modalMixto.DialogResult = DialogResult.OK;
                modalMixto.Close();
            };

            modalMixto.Controls.AddRange(new Control[] { lblT, lblEf, txtEf, lblTar, txtTar, lblTrans, txtTrans, lblEstadoSuma, btnAplicar });
            
            if (modalMixto.ShowDialog(this) != DialogResult.OK)
            {
                SeleccionarMedioPago("Efectivo");
            }
        }

        private void BtnAbrirCaja_Click(object? sender, EventArgs e)
        {
            if (_turnoActual != null && _turnoActual.Estado == "Abierta")
            {
                decimal ventasEfectivo = _cajaService.CalcularVentasEfectivo(_turnoActual.FechaApertura);
                decimal totalEsperado = _turnoActual.MontoInicial + ventasEfectivo;

                var resp = MessageBox.Show($"¿Desea cerrar la caja?\n\n• Fondo Inicial: ${_turnoActual.MontoInicial:N0}\n• Ventas Efectivo: ${ventasEfectivo:N0}\n• Total Esperado: ${totalEsperado:N0}\n• Vuelto Entregado: ${_totalVueltoEntregadoTurno:N0}", "Cierre de Caja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (resp == DialogResult.Yes)
                {
                    _turnoActual = null;
                    _totalVueltoEntregadoTurno = 0;
                    ActualizarEstadoCajaUI();
                    MessageBox.Show("Caja cerrada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
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

                Label lblM = new Label { Text = "Monto Inicial de Caja ($):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
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

                        _totalVueltoEntregadoTurno = 0;
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

            bool esAdmin = (_usuarioActual?.Rol != null && _usuarioActual.Rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase)) 
                           || (_usuarioActual?.NombreUsuario != null && _usuarioActual.NombreUsuario.Equals("barbara", StringComparison.OrdinalIgnoreCase));

            if (!esAdmin)
            {
                MessageBox.Show("Acceso denegado: El Resumen del Turno solo está disponible para usuarios con perfil Administrador.", "Permiso Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            FormResumenTurnoModal modalResumen = new FormResumenTurnoModal(_turnoActual);
            modalResumen.ShowDialog(this);
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

                lblTotalCobrar.Text = $"${ticket.Total:N0}";

                var detalles = _cajaService.ObtenerDetallesTicket(ticket.idTve);
                dgvDetalleTicket.DataSource = detalles;
            }
            else
            {
                LimpiarSeleccionVista();
            }
        }

        private void TxtPagaCon_TextChanged(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;
            if (_pagoMixtoConfirmado) return;

            if (decimal.TryParse(txtPagaCon.Text.Trim(), out decimal pagaCon))
            {
                decimal vuelto = pagaCon - _ticketSeleccionado.Total;
                lblVuelto.Text = vuelto >= 0 ? $"${vuelto:N0}" : "$0";
                lblVuelto.ForeColor = vuelto >= 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
            }
            else
            {
                lblVuelto.Text = "$0";
            }
        }

        private void BtnCobrarTicket_Click(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            string tipoDoc = _tipoDocSeleccionado;
            string medioPago = _medioPagoSeleccionado;

            decimal pagaCon = 0;
            decimal vuelto = 0;

            if (medioPago.Contains("Múltiple"))
            {
                if (!_pagoMixtoConfirmado)
                {
                    MessageBox.Show("Por favor configure el desglose del Pago Múltiple.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                pagaCon = _pagoEfectivo + _pagoTarjeta + _pagoTransferencia;
                vuelto = _vueltoMixto;
                medioPago = $"Múltiple (Efec: ${_pagoEfectivo:N0} | Tarj: ${_pagoTarjeta:N0} | Transf: ${_pagoTransferencia:N0})";
            }
            else if (medioPago == "Efectivo")
            {
                if (!decimal.TryParse(txtPagaCon.Text.Trim(), out pagaCon) || pagaCon < _ticketSeleccionado.Total)
                {
                    MessageBox.Show("Ingrese un monto en efectivo suficiente para cobro.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                vuelto = pagaCon - _ticketSeleccionado.Total;
            }
            else
            {
                pagaCon = _ticketSeleccionado.Total;
                vuelto = 0;
            }

            try
            {
                int folioOficial = _cajaService.ProcesarCobroTicket(_ticketSeleccionado, tipoDoc, medioPago, _ticketSeleccionado.RuT, _ticketSeleccionado.RazonSocial, "GENERAL", vuelto);

                _totalVueltoEntregadoTurno += vuelto;

                var detalles = _cajaService.ObtenerDetallesTicket(_ticketSeleccionado.idTve);
                List<DetalleCarrito> itemsCarrito = detalles.Select(item => new DetalleCarrito
                {
                    ProductoID = item.IdProducto,
                    Nombre = item.NmbProducto ?? "Producto",
                    PrecioUnitario = item.Precio,
                    Cantidad = item.Cantidad
                }).ToList();

                MessageBox.Show($"¡{tipoDoc.ToUpper()} N° {folioOficial} COBRADA CON ÉXITO!\n\n" +
                                $"• Medio de Pago: {medioPago}\n" +
                                $"• Total Cobrado: ${_ticketSeleccionado.Total:N0}\n" +
                                $"• Vuelto: ${vuelto:N0}",
                                "Cobro Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                FormTicketModal formTicket = new FormTicketModal(_ticketSeleccionado, itemsCarrito, pagaCon, vuelto);
                formTicket.ShowDialog(this);

                txtBuscarTicket.Clear();
                CargarTicketsPendientes();
                LimpiarSeleccionVista();
                ActualizarEstadoCajaUI();
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
                    LimpiarSeleccionVista();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al anular: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}