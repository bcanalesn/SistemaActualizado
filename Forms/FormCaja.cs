using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormCaja : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;
        private readonly CajaService _cajaService = new CajaService();
        private static CajaTurno? _turnoActual;
        private Usuario? _usuarioActual;

        private Button btnAbrirCaja = null!;
        private Label lblEstadoTag = null!;
        private Label lblEstadoDetalle = null!;
        private Label lblHoraApertura = null!;
        private Label lblFondoInicial = null!;

        private TextBox txtBuscarTicket = null!;
        private Label lblTotalResultados = null!;
        private Label lblUltimaActualizacion = null!;

        private DataGridView dgvTicketsPendientes = null!;
        private DataGridView dgvDetalleTicket = null!;

        // SECCIÓN 1: BOTONES TIPO DE DOCUMENTO
        private Button btnDocBoleta = null!;
        private Button btnDocFactura = null!;
        private string _tipoDocSeleccionado = "Boleta Electrónica";

        // SECCIÓN 2: BOTONES MEDIO DE PAGO (6 Medios)
        private Button btnPagoEfectivo = null!;
        private Button btnPagoDebito = null!;
        private Button btnPagoCredito = null!;
        private Button btnPagoCreditoComercial = null!;
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
            
            string nomUser = _usuarioActual?.NombreUsuario ?? "admin";
            _turnoActual = _cajaService.ObtenerTurnoAbierto(nomUser);
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
                Location = new Point(210, 0),
                Size = new Size(500, 48),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            Panel cardEstado = CrearCardEncabezado("🟢 CAJA ABIERTA", "Turno #----", out lblEstadoTag, out lblEstadoDetalle);
            Panel cardHora = CrearCardEncabezado("🕒 INICIO TURNO", "--:--", out _, out lblHoraApertura);
            Panel cardFondo = CrearCardEncabezado("🛍️ FONDO INICIAL", "$ 0", out _, out lblFondoInicial);

            flpMetricas.Controls.Add(cardEstado);
            flpMetricas.Controls.Add(cardHora);
            flpMetricas.Controls.Add(cardFondo);

            Button btnVerResumen = new Button
            {
                Text = "📈 Ver resumen del turno",
                Dock = DockStyle.Right,
                Width = 175,
                Height = 36,
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
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 510F));

            // COLUMNA IZQUIERDA
            TableLayoutPanel pnlColIzquierda = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(0, 0, 10, 0)
            };
            pnlColIzquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            pnlColIzquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlColIzquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // 1. Buscador
            Panel pnlCardBusqueda = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 8) };
            pnlCardBusqueda.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardBusqueda.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            
            Label lblTitBuscar = new Label { Text = "BUSCAR TICKET PENDIENTE", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, 6), AutoSize = true };

            txtBuscarTicket = new TextBox { Location = new Point(10, 26), Size = new Size(200, 26), Font = new Font("Segoe UI", 9.5F), PlaceholderText = "N° Ticket o Cliente..." };
            txtBuscarTicket.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarTicketsPendientes(txtBuscarTicket.Text.Trim()); e.SuppressKeyPress = true; } };

            Button btnBuscar = new Button { Text = "🔍 Buscar", Location = new Point(218, 25), Size = new Size(76, 28), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarTicketsPendientes(txtBuscarTicket.Text.Trim());

            Button btnRecargar = new Button { Text = "🔄 Recargar", Location = new Point(300, 25), Size = new Size(84, 28), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(37, 99, 235), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRecargar.FlatAppearance.BorderSize = 0;
            btnRecargar.Click += (s, e) => CargarTicketsPendientes();

            Button btnLimpiar = new Button { Text = "🗑️ Limpiar", Location = new Point(390, 25), Size = new Size(76, 28), BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(239, 68, 68), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => { txtBuscarTicket.Clear(); CargarTicketsPendientes(); };

            pnlCardBusqueda.Controls.AddRange(new Control[] { lblTitBuscar, txtBuscarTicket, btnBuscar, btnRecargar, btnLimpiar });

            // 2. Grilla Tickets Pendientes
            Panel pnlCardPendientes = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(8) };
            pnlCardPendientes.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardPendientes.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            
            Label lblTitPend = new Label { Text = "TICKETS PENDIENTES DE PAGO", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 22 };

            dgvTicketsPendientes = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            ConfigurarEstiloTabla(dgvTicketsPendientes);
            dgvTicketsPendientes.SelectionChanged += DgvTicketsPendientes_SelectionChanged;

            Panel pnlStatusFooter = new Panel { Dock = DockStyle.Bottom, Height = 24, BackColor = Color.White };
            lblTotalResultados = new Label { Text = "✔ 0 tickets encontrados", Location = new Point(2, 4), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74) };
            lblUltimaActualizacion = new Label { Text = $"Última actualización: {DateTime.Now:HH:mm:ss} 🔄", Dock = DockStyle.Right, AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139) };
            pnlStatusFooter.Controls.AddRange(new Control[] { lblTotalResultados, lblUltimaActualizacion });

            pnlCardPendientes.Controls.Add(dgvTicketsPendientes);
            pnlCardPendientes.Controls.Add(pnlStatusFooter);
            pnlCardPendientes.Controls.Add(lblTitPend);

            // 3. Grilla Detalle Ticket
            Panel pnlCardDetalle = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
            pnlCardDetalle.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardDetalle.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            
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
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false
            };

            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdProducto", DataPropertyName = "IdProducto", HeaderText = "CÓDIGO", FillWeight = 20 });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "NmbProducto", DataPropertyName = "NmbProducto", HeaderText = "PRODUCTO", FillWeight = 45 });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", DataPropertyName = "Cantidad", HeaderText = "CANT.", FillWeight = 12 });
            var estiloMonedaCL = new DataGridViewCellStyle 
            { 
                FormatProvider = new System.Globalization.CultureInfo("es-CL"), 
                Format = "$ #,##0", 
                Alignment = DataGridViewContentAlignment.MiddleRight 
            };

            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", DataPropertyName = "Precio", HeaderText = "PRECIO UNIT.", FillWeight = 23, DefaultCellStyle = estiloMonedaCL });
            dgvDetalleTicket.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", DataPropertyName = "SubTotal", HeaderText = "SUBTOTAL", FillWeight = 23, DefaultCellStyle = estiloMonedaCL });
            ConfigurarEstiloTabla(dgvDetalleTicket);

            pnlCardDetalle.Controls.Add(dgvDetalleTicket);
            pnlCardDetalle.Controls.Add(lblTitDet);

            pnlColIzquierda.Controls.Add(pnlCardBusqueda, 0, 0);
            pnlColIzquierda.Controls.Add(pnlCardPendientes, 0, 1);
            pnlColIzquierda.Controls.Add(pnlCardDetalle, 0, 2);

            // ==========================================
            // COLUMNA DERECHA (PAGOS Y ACCIONES)
            // ==========================================
            Panel pnlColDerecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), AutoScroll = true };
            const int panelW = 485;

            // SECCIÓN 1: TIPO DE DOCUMENTO
            Panel pnlSec1 = CrearContenedorSeccion(2, 98, panelW, Color.FromArgb(124, 58, 237));
            Panel circle1 = CrearBadgeNumero("1", Color.FromArgb(124, 58, 237), new Point(10, 8));
            Label lblT1 = new Label { Text = "TIPO DE DOCUMENTO", Location = new Point(42, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237) };
            Label lblSub1 = new Label { Text = "Selecciona el tipo de documento a emitir", Location = new Point(42, 22), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };

            int docBtnW = (panelW - 30) / 2;
            btnDocBoleta = CrearBotonTarjeVisual("📄", "BOLETA", "ELECTRÓNICA", new Point(10, 42), new Size(docBtnW, 45), Color.FromArgb(124, 58, 237), esActivo: true);
            btnDocBoleta.Click += (s, e) => SeleccionarTipoDocumento("Boleta Electrónica");

            btnDocFactura = CrearBotonTarjeVisual("📑", "FACTURA", "ELECTRÓNICA", new Point(10 + docBtnW + 10, 42), new Size(docBtnW, 45), Color.FromArgb(124, 58, 237), esActivo: false);
            btnDocFactura.Click += (s, e) => SeleccionarTipoDocumento("Factura Electrónica");

            pnlSec1.Controls.AddRange(new Control[] { circle1, lblT1, lblSub1, btnDocBoleta, btnDocFactura });

            // SECCIÓN 2: MEDIO DE PAGO (2 Filas x 3 Botones)
            Panel pnlSec2 = CrearContenedorSeccion(108, 142, panelW, Color.FromArgb(203, 213, 225));
            Panel circle2 = CrearBadgeNumero("2", Color.FromArgb(37, 99, 235), new Point(10, 8));
            Label lblT2 = new Label { Text = "MEDIO DE PAGO", Location = new Point(42, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235) };
            Label lblSub2 = new Label { Text = "Selecciona el medio de pago", Location = new Point(42, 22), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };

            int btnPW3 = (panelW - 32) / 3;
            int btnH = 44;

            // Fila 1
            btnPagoEfectivo = CrearBotonPagoVectorial("EFECTIVO", "EFECTIVO", new Point(8, 40), new Size(btnPW3, btnH), Color.FromArgb(16, 185, 129), () => _medioPagoSeleccionado == "Efectivo");
            btnPagoEfectivo.Click += (s, e) => SeleccionarMedioPago("Efectivo");

            btnPagoDebito = CrearBotonPagoVectorial("DÉBITO", "DÉBITO", new Point(8 + btnPW3 + 6, 40), new Size(btnPW3, btnH), Color.FromArgb(37, 99, 235), () => _medioPagoSeleccionado == "Débito");
            btnPagoDebito.Click += (s, e) => SeleccionarMedioPago("Débito");

            btnPagoCredito = CrearBotonPagoVectorial("CRÉDITO", "TARJETA CRÉDITO", new Point(8 + (btnPW3 + 6) * 2, 40), new Size(btnPW3, btnH), Color.FromArgb(124, 58, 237), () => _medioPagoSeleccionado == "Tarjeta Crédito");
            btnPagoCredito.Click += (s, e) => SeleccionarMedioPago("Tarjeta Crédito");

            // Fila 2
            btnPagoCreditoComercial = CrearBotonPagoVectorial("CRÉDITO COMERCIAL", "A PLAZO / CRÉDITO", new Point(8, 88), new Size(btnPW3, btnH), Color.FromArgb(234, 88, 12), () => _medioPagoSeleccionado == "Crédito Comercial");
            btnPagoCreditoComercial.Click += (s, e) => SeleccionarMedioPago("Crédito Comercial");

            btnPagoTransferencia = CrearBotonPagoVectorial("TRANSFERENCIA", "TRANSFERENCIA", new Point(8 + btnPW3 + 6, 88), new Size(btnPW3, btnH), Color.FromArgb(13, 148, 136), () => _medioPagoSeleccionado == "Transferencia");
            btnPagoTransferencia.Click += (s, e) => SeleccionarMedioPago("Transferencia");

            btnPagoMultiple = CrearBotonPagoVectorial("PAGO MÚLTIPLE", "PAGO MÚLTIPLE", new Point(8 + (btnPW3 + 6) * 2, 88), new Size(btnPW3, btnH), Color.FromArgb(100, 116, 139), () => _medioPagoSeleccionado == "Pago Múltiple");
            btnPagoMultiple.Click += (s, e) => SeleccionarMedioPago("Pago Múltiple");

            pnlSec2.Controls.AddRange(new Control[] { circle2, lblT2, lblSub2, btnPagoEfectivo, btnPagoDebito, btnPagoCredito, btnPagoCreditoComercial, btnPagoTransferencia, btnPagoMultiple });

            // SECCIÓN 3: DATOS DE COBRO
            Panel pnlDatosCobroCard = CrearContenedorSeccion(258, 162, panelW, Color.FromArgb(16, 185, 129));
            Panel circle3 = CrearBadgeNumero("3", Color.FromArgb(16, 185, 129), new Point(10, 8));
            Label lblT3 = new Label { Text = "DATOS DE COBRO", Location = new Point(42, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };
            Label lblSub3 = new Label { Text = "Ingresa el monto recibido (solo para Efectivo o Pago Múltiple)", Location = new Point(42, 22), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };

            int cW = (panelW - 30) / 2;
            Panel pnlPagaCon = new Panel { Location = new Point(10, 42), Size = new Size(cW, 52), BackColor = Color.FromArgb(248, 250, 252) };
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
            txtPagaCon = new TextBox { Location = new Point(28, 20), Size = new Size(cW - 36, 26), Font = new Font("Segoe UI", 11F, FontStyle.Bold), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(248, 250, 252),MaxLength = 10 };
            txtPagaCon.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            txtPagaCon.TextChanged += TxtPagaCon_TextChanged;
            pnlPagaCon.Controls.AddRange(new Control[] { lblP, lblSigno, txtPagaCon });

            Panel pnlVueltoCard = new Panel { Location = new Point(10 + cW + 10, 42), Size = new Size(cW, 52), BackColor = Color.FromArgb(240, 253, 244) };
            pnlVueltoCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlVueltoCard.Width - 1, pnlVueltoCard.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(Color.FromArgb(187, 247, 208), 1.5f);
                e.Graphics.DrawPath(pen, p);
            };

            Label lblVIcon = new Label { Text = "💵 VUELTO", Location = new Point(8, 5), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 101, 52) };
            lblVuelto = new Label { Text = "$0", Location = new Point(50, 12), Size = new Size(cW - 55, 30), Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), TextAlign = ContentAlignment.MiddleRight };
            pnlVueltoCard.Controls.AddRange(new Control[] { lblVIcon, lblVuelto });

            Panel pnlTotalCard = new Panel { Location = new Point(10, 100), Size = new Size(panelW - 20, 50), BackColor = Color.FromArgb(245, 243, 255) };
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
            Panel pnlSec4 = CrearContenedorSeccion(428, 118, panelW, Color.FromArgb(234, 88, 12));
            Panel circle4 = CrearBadgeNumero("4", Color.FromArgb(234, 88, 12), new Point(10, 8));
            Label lblT4 = new Label { Text = "ACCIONES PRINCIPALES", Location = new Point(42, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(234, 88, 12) };

            btnCobrarTicket = new Button
            {
                Text = "⚡  COBRAR Y EMITIR DTE",
                Location = new Point(10, 38),
                Size = new Size(panelW - 20, 38),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnCobrarTicket.FlatAppearance.BorderSize = 0;
            btnCobrarTicket.Click += BtnCobrarTicket_Click;

            int actionW = (panelW - 30) / 2;
            btnImprimirVistaPrevia = new Button
            {
                Text = "🖨️  IMPRIMIR VISTA PREVIA",
                Location = new Point(10, 80),
                Size = new Size(actionW, 30),
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
                Location = new Point(10 + actionW + 10, 80),
                Size = new Size(actionW, 30),
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

        private Panel CrearContenedorSeccion(int y, int alto, int ancho, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(6, y), Size = new Size(ancho, alto), BackColor = Color.White, Margin = new Padding(0, 0, 0, 10) };
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
            Panel pnl = new Panel { Location = ubicacion, Size = new Size(24, 24), BackColor = Color.Transparent };
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
            Button btn = new Button { Location = loc, Size = tamano, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.White };
            btn.FlatAppearance.BorderSize = 0;

            btn.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool esActivo = fnEstaSeleccionado();

                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(rect, 8);

                if (esActivo)
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

                int iconX = (btn.Width - 28) / 2;
                int iconY = 6;
                Color colorIcono = esActivo ? colorTema : Color.FromArgb(100, 116, 139);

                DibujarIconoVectorial(g, tipo, iconX, iconY, colorIcono);

                using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using Font font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
                using SolidBrush brushText = new SolidBrush(Color.FromArgb(15, 23, 42));

                RectangleF rectTexto = new RectangleF(0, 24, btn.Width, btn.Height - 24);
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
                    using (GraphicsPath bPath = CrearRutaRedondeada(new Rectangle(x, y + 2, 28, 16), 3))
                    {
                        g.FillPath(brush, bPath);
                    }
                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(whiteB, x + 9, y + 5, 10, 10);
                    }
                    break;

                case "DÉBITO":
                case "CRÉDITO":
                    using (GraphicsPath tPath = CrearRutaRedondeada(new Rectangle(x + 1, y + 2, 26, 16), 3))
                    {
                        g.FillPath(brush, tPath);
                    }
                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(whiteB, x + 1, y + 5, 26, 3);
                        g.FillRectangle(whiteB, x + 4, y + 11, 5, 3);
                    }
                    break;

                case "CRÉDITO COMERCIAL":
                    using (GraphicsPath cPath = CrearRutaRedondeada(new Rectangle(x + 1, y + 2, 26, 16), 3))
                    {
                        g.FillPath(brush, cPath);
                    }
                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    {
                        using Font fontIcon = new Font("Segoe UI", 6.5F, FontStyle.Bold);
                        using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("30d", fontIcon, whiteB, new RectangleF(x + 1, y + 2, 26, 16), sf);
                    }
                    break;

                case "TRANSFERENCIA":
                    Point[] techo = { new Point(x + 14, y + 2), new Point(x + 2, y + 7), new Point(x + 26, y + 7) };
                    g.FillPolygon(brush, techo);
                    g.FillRectangle(brush, x + 4, y + 8, 20, 2);
                    g.FillRectangle(brush, x + 5, y + 11, 3, 5);
                    g.FillRectangle(brush, x + 12, y + 11, 3, 5);
                    g.FillRectangle(brush, x + 19, y + 11, 3, 5);
                    g.FillRectangle(brush, x + 3, y + 16, 22, 2);
                    break;

                case "PAGO MÚLTIPLE":
                    pen.Width = 2.5f;
                    g.FillRectangle(brush, x + 2, y + 8, 8, 4);
                    g.DrawLine(pen, new Point(x + 8, y + 10), new Point(x + 20, y + 4));
                    g.DrawLine(pen, new Point(x + 8, y + 10), new Point(x + 20, y + 16));
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
            btnPagoCreditoComercial.Invalidate();
            btnPagoTransferencia.Invalidate();
            btnPagoMultiple.Invalidate();

            if (_medioPagoSeleccionado == "Pago Múltiple")
            {
                if (_ticketSeleccionado == null)
                {
                    MessageBox.Show("Seleccione un ticket antes de configurar el Pago Múltiple.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SeleccionarMedioPago("Efectivo");
                    return;
                }
                AbrirModalPagoMultiplesMedios(_ticketSeleccionado.Total);
            }
            else
            {
                ActualizarDatosCobroSegunTicket();
            }
        }

        private void ActualizarDatosCobroSegunTicket()
        {
            _pagoMixtoConfirmado = false;

            if (_ticketSeleccionado == null)
            {
                txtPagaCon.Text = "";
                lblVuelto.Text = "$ 0";
                return;
            }

            if (_medioPagoSeleccionado == "Efectivo")
            {
                txtPagaCon.Enabled = true;
                txtPagaCon.Text = MonedaHelper.Formatear(_ticketSeleccionado.Total); // Ej: "3.990"
                lblVuelto.Text = "$ 0";
            }
            else if (_medioPagoSeleccionado == "Crédito Comercial")
            {
                txtPagaCon.Enabled = false;
                txtPagaCon.Text = "0 (A Plazo)";
                lblVuelto.Text = "$ 0";
            }
            else
            {
                txtPagaCon.Enabled = false;
                txtPagaCon.Text = MonedaHelper.Formatear(_ticketSeleccionado.Total);
                lblVuelto.Text = "$ 0";
            }
        }

        private Panel CrearCardEncabezado(string titulo, string detalle, out Label lblTag, out Label lblVal)
        {
            Panel pnl = new Panel { Size = new Size(150, 44), BackColor = Color.White, Margin = new Padding(3) };
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
                lblFondoInicial.Text = _turnoActual.MontoInicial.ToString("$ #,##0", new System.Globalization.CultureInfo("es-CL"));
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

        private void AbrirModalPagoMultiplesMedios(decimal totalTicket)
        {
            Form modalMixto = new Form
            {
                Text = "Configurar Pago Múltiple",
                Size = new Size(380, 390),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblT = new Label 
            { 
                Text = $"💳 TOTAL TICKET: {MonedaHelper.Formatear(totalTicket, conSigno: true)}", 
                Location = new Point(20, 15), 
                Font = new Font("Segoe UI", 12F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(37, 99, 235), 
                AutoSize = true 
            };

            // 1. Campo Efectivo
            Label lblEf = new Label { Text = "Monto en Efectivo ($):", Location = new Point(20, 52), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtEf = CrearInputMonedaModal(20, 72, 320);

            // 2. Campo Tarjeta
            Label lblTar = new Label { Text = "Monto en Tarjeta ($):", Location = new Point(20, 110), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtTar = CrearInputMonedaModal(20, 130, 320);

            // 3. Campo Transferencia
            Label lblTrans = new Label { Text = "Monto en Transferencia ($):", Location = new Point(20, 168), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtTrans = CrearInputMonedaModal(20, 188, 320);

            Label lblEstadoSuma = new Label 
            { 
                Text = $"Falta por cubrir: {MonedaHelper.Formatear(totalTicket, conSigno: true)}", 
                Location = new Point(20, 230), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(239, 68, 68) 
            };

            // Recálculo dinámico en tiempo real
            Action recalcular = () =>
            {
                decimal ef = MonedaHelper.Limpiar(txtEf.Text);
                decimal tar = MonedaHelper.Limpiar(txtTar.Text);
                decimal tr = MonedaHelper.Limpiar(txtTrans.Text);

                decimal electronico = tar + tr;
                decimal sumaTotal = ef + electronico;

                // Regla de negocio: Tarjeta + Transferencia no pueden superar el total del ticket
                if (electronico > totalTicket)
                {
                    lblEstadoSuma.Text = "⚠️ Tarjeta y Transferencia no pueden superar el total";
                    lblEstadoSuma.ForeColor = Color.FromArgb(239, 68, 68);
                    return;
                }

                if (sumaTotal < totalTicket)
                {
                    decimal restante = totalTicket - sumaTotal;
                    lblEstadoSuma.Text = $"Falta por cubrir: {MonedaHelper.Formatear(restante, conSigno: true)}";
                    lblEstadoSuma.ForeColor = Color.FromArgb(239, 68, 68);
                }
                else
                {
                    decimal saldoRestanteParaEfectivo = totalTicket - electronico;
                    decimal vuelto = ef > saldoRestanteParaEfectivo ? ef - saldoRestanteParaEfectivo : 0;
                    lblEstadoSuma.Text = $"✔ Total cubierto | Vuelto: {MonedaHelper.Formatear(vuelto, conSigno: true)}";
                    lblEstadoSuma.ForeColor = Color.FromArgb(22, 163, 74);
                }
            };

            txtEf.TextChanged += (s, e) => { MonedaHelper.AplicarMascaraEnVivo(txtEf); recalcular(); };
            txtTar.TextChanged += (s, e) => { MonedaHelper.AplicarMascaraEnVivo(txtTar); recalcular(); };
            txtTrans.TextChanged += (s, e) => { MonedaHelper.AplicarMascaraEnVivo(txtTrans); recalcular(); };

            Button btnAplicar = new Button
            {
                Text = "✔ Confirmar Pago Múltiple",
                Location = new Point(20, 275),
                Size = new Size(320, 44),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAplicar.FlatAppearance.BorderSize = 0;

            btnAplicar.Click += (s, e) =>
            {
                decimal ef = MonedaHelper.Limpiar(txtEf.Text);
                decimal tar = MonedaHelper.Limpiar(txtTar.Text);
                decimal tr = MonedaHelper.Limpiar(txtTrans.Text);

                decimal sumaTotal = ef + tar + tr;
                if (sumaTotal < totalTicket)
                {
                    MessageBox.Show($"La suma ingresada ({MonedaHelper.Formatear(sumaTotal, conSigno: true)}) no cubre el total ({MonedaHelper.Formatear(totalTicket, conSigno: true)}).", "Monto Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if ((tar + tr) > totalTicket)
                {
                    MessageBox.Show("El pago con Tarjeta y Transferencia no puede ser mayor al total a cobrar.", "Monto Electrónico Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _pagoEfectivo = ef;
                _pagoTarjeta = tar;
                _pagoTransferencia = tr;

                decimal saldoCobradoPorEfectivo = totalTicket - (tar + tr);
                _vueltoMixto = ef > saldoCobradoPorEfectivo ? ef - saldoCobradoPorEfectivo : 0;
                _pagoMixtoConfirmado = true;

                txtPagaCon.Text = MonedaHelper.Formatear(_pagoEfectivo);
                lblVuelto.Text = MonedaHelper.Formatear(_vueltoMixto, conSigno: true);

                modalMixto.DialogResult = DialogResult.OK;
                modalMixto.Close();
            };

            modalMixto.Controls.AddRange(new Control[] { lblT, lblEf, txtEf, lblTar, txtTar, lblTrans, txtTrans, lblEstadoSuma, btnAplicar });

            if (modalMixto.ShowDialog(this) != DialogResult.OK)
            {
                SeleccionarMedioPago("Efectivo");
            }
        }

        private TextBox CrearInputMonedaModal(int x, int y, int ancho)
        {
            TextBox txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 26),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                MaxLength = 10 // Máximo 8 dígitos más puntos ($99.999.999)
            };

            txt.KeyPress += (s, e) =>
            {
                // Solo permitir números y tecla de borrar
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            return txt;
        }

        private void BtnAbrirCaja_Click(object? sender, EventArgs e)
        {
            if (_turnoActual != null && _turnoActual.Estado == "Abierta")
            {
                decimal ventasEfectivo = _cajaService.CalcularVentasEfectivo(_turnoActual.CajaTurnoID);
                decimal fondoInicial = _turnoActual.MontoInicial;
                decimal efectivoEsperado = fondoInicial + ventasEfectivo;

                // Modal de Arqueo y Cierre
                using Form modalCierre = new Form
                {
                    Text = "Arqueo y Cierre de Caja",
                    Size = new Size(380, 410),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };

                Label lblT = new Label { Text = "🔒 ARQUEO Y CIERRE DE TURNO", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
                Label lblPrompt = new Label { Text = "Ingrese el Efectivo Físico Contado ($):", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
                TextBox txtEfectivoReal = new TextBox { Location = new Point(20, 72), Size = new Size(325, 28), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

                Label lblResultadoDif = new Label { Text = "Esperando conteo...", Location = new Point(20, 110), Size = new Size(325, 22), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
                Label lblObs = new Label { Text = "Motivo del descuadre (Obligatorio si no cuadra):", Location = new Point(20, 140), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Visible = false };
                TextBox txtObs = new TextBox { Location = new Point(20, 162), Size = new Size(325, 60), Multiline = true, Font = new Font("Segoe UI", 9F), Visible = false };

                txtEfectivoReal.TextChanged += (s, ev) =>
                {
                    if (decimal.TryParse(txtEfectivoReal.Text.Trim(), out decimal real))
                    {
                        decimal dif = real - efectivoEsperado;
                        if (dif == 0)
                        {
                            lblResultadoDif.Text = "✔ Caja Cuadrada ($0)";
                            lblResultadoDif.ForeColor = Color.FromArgb(22, 163, 74);
                            lblObs.Visible = false;
                            txtObs.Visible = false;
                        }
                        else if (dif < 0)
                        {
                            lblResultadoDif.Text = $"⚠️ Faltante: -${Math.Abs(dif):N0}";
                            lblResultadoDif.ForeColor = Color.FromArgb(239, 68, 68);
                            lblObs.Visible = true;
                            txtObs.Visible = true;
                        }
                        else
                        {
                            lblResultadoDif.Text = $"ℹ️ Sobrante: +${dif:N0}";
                            lblResultadoDif.ForeColor = Color.FromArgb(234, 88, 12);
                            lblObs.Visible = true;
                            txtObs.Visible = true;
                        }
                    }
                    else
                    {
                        lblResultadoDif.Text = "Ingrese un monto válido";
                        lblResultadoDif.ForeColor = Color.FromArgb(100, 116, 139);
                    }
                };

                Button btnConfirmar = new Button
                {
                    Text = "🔒 Confirmar Cierre",
                    Location = new Point(20, 240),
                    Size = new Size(325, 42),
                    BackColor = Color.FromArgb(239, 68, 68),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnConfirmar.FlatAppearance.BorderSize = 0;

                btnConfirmar.Click += (s, ev) =>
                {
                    if (!decimal.TryParse(txtEfectivoReal.Text.Trim(), out decimal realDeclarado))
                    {
                        MessageBox.Show("Ingrese el monto contado.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    decimal dif = realDeclarado - efectivoEsperado;
                    if (dif != 0 && string.IsNullOrWhiteSpace(txtObs.Text))
                    {
                        MessageBox.Show("Debe indicar el motivo del descuadre.", "Justificación Obligatoria", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _cajaService.CerrarTurno(_turnoActual.CajaTurnoID, realDeclarado, txtObs.Text.Trim());
                    _turnoActual = null;
                    ActualizarEstadoCajaUI();
                    modalCierre.Close();
                    MessageBox.Show($"Caja cerrada exitosamente.\n\n• Esperado: ${efectivoEsperado:N0}\n• Declarado: ${realDeclarado:N0}\n• Diferencia: ${(dif >= 0 ? "+$" : "-$")}{Math.Abs(dif):N0}", "Turno Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                modalCierre.Controls.AddRange(new Control[] { lblT, lblPrompt, txtEfectivoReal, lblResultadoDif, lblObs, txtObs, btnConfirmar });
                modalCierre.ShowDialog(this);
            }
            else
            {
                // Modal Apertura
                using Form modalApertura = new Form
                {
                    Text = "Apertura de Caja",
                    Size = new Size(320, 215),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };

                Label lblM = new Label { Text = "Monto Inicial de Caja ($):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                TextBox txtM = new TextBox { Location = new Point(20, 45), Size = new Size(260, 28), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
                Button btnA = new Button { Text = "🚀 Iniciar Turno", Location = new Point(20, 95), Size = new Size(260, 40), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnA.FlatAppearance.BorderSize = 0;

                btnA.Click += (sa, ea) =>
                {
                    if (decimal.TryParse(txtM.Text.Trim(), out decimal monto) && monto >= 0)
                    {
                        string usuario = _usuarioActual?.NombreUsuario ?? "admin";
                        _turnoActual = _cajaService.AbrirTurno(usuario, monto);
                        modalApertura.DialogResult = DialogResult.OK;
                        modalApertura.Close();
                    }
                    else
                    {
                        MessageBox.Show("Ingrese un monto numérico válido.", "Monto Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show("La caja debe estar abierta para consultar el resumen.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                dgvTicketsPendientes.DataSource = null;
                dgvTicketsPendientes.DataSource = pendientes;

                if (dgvTicketsPendientes.Columns["nroDTE"] != null) dgvTicketsPendientes.Columns["nroDTE"].HeaderText = "N° TICKET";
                if (dgvTicketsPendientes.Columns["FecDoc"] != null) { dgvTicketsPendientes.Columns["FecDoc"].HeaderText = "HORA"; dgvTicketsPendientes.Columns["FecDoc"].DefaultCellStyle.Format = "HH:mm:ss"; }
                if (dgvTicketsPendientes.Columns["RazonSocial"] != null) dgvTicketsPendientes.Columns["RazonSocial"].HeaderText = "CLIENTE";
                if (dgvTicketsPendientes.Columns["Total"] != null) 
                { 
                    dgvTicketsPendientes.Columns["Total"].HeaderText = "TOTAL"; 
                    dgvTicketsPendientes.Columns["Total"].DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        FormatProvider = new System.Globalization.CultureInfo("es-CL"), 
                        Format = "$ #,##0", 
                        Alignment = DataGridViewContentAlignment.MiddleRight 
                    };
                }
                if (dgvTicketsPendientes.Columns["UserDTE"] != null) dgvTicketsPendientes.Columns["UserDTE"].HeaderText = "VENDEDOR";

                string[] ocultar = new string[] { "idTve", "CajaTurnoID", "idLocal", "nmbLocal", "iddocDTE", "Documento", "nroInT", "SubTotal", "Descuento", "Neto", "Impto1", "Impto2", "Impto3", "IvA", "Vendedor", "nroZ", "Url", "nPAX", "Idcliente", "DNI", "RuT", "dv", "Giro", "Direccion", "idcomuna", "nComuna", "idCiudad", "nCiudad", "Fono1", "Fono2", "email", "status", "idREF", "nroREF", "codigoREF", "FechaREF", "HoraDoc", "Detalles", "MedioPago", "Vuelto" };
                foreach (var col in ocultar)
                {
                    if (dgvTicketsPendientes.Columns[col] != null) dgvTicketsPendientes.Columns[col].Visible = false;
                }

                lblTotalResultados.Text = $"✔ {pendientes.Count} ticket(s) encontrado(s)";
                lblUltimaActualizacion.Text = $"Última actualización: {DateTime.Now:HH:mm:ss} 🔄";

                if (pendientes.Count > 0)
                {
                    dgvTicketsPendientes.ClearSelection();
                    dgvTicketsPendientes.Rows[0].Selected = true;

                    var primeraColVisible = dgvTicketsPendientes.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
                    if (primeraColVisible != null)
                    {
                        dgvTicketsPendientes.CurrentCell = dgvTicketsPendientes.Rows[0].Cells[primeraColVisible.Index];
                    }

                    _ticketSeleccionado = pendientes[0];
                    btnCobrarTicket.Enabled = true;
                    btnAnularTicket.Enabled = true;
                    lblTotalCobrar.Text = $"${_ticketSeleccionado.Total:N0}";

                    var detalles = _cajaService.ObtenerDetallesTicket(_ticketSeleccionado.idTve);
                    dgvDetalleTicket.DataSource = detalles;
                    ActualizarDatosCobroSegunTicket();
                }
                else
                {
                    LimpiarSeleccionVista();
                }
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
                ActualizarDatosCobroSegunTicket();
            }
            else
            {
                LimpiarSeleccionVista();
            }
        }

        private void TxtPagaCon_TextChanged(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null || _pagoMixtoConfirmado) return;

            MonedaHelper.AplicarMascaraEnVivo(txtPagaCon);
            decimal pagaCon = MonedaHelper.Limpiar(txtPagaCon.Text);

            decimal vuelto = pagaCon - _ticketSeleccionado.Total;
            lblVuelto.Text = MonedaHelper.Formatear(vuelto >= 0 ? vuelto : 0, conSigno: true);
            lblVuelto.ForeColor = vuelto >= 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
        }

        private void BtnCobrarTicket_Click(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null || _turnoActual == null) return;

            string tipoDoc = _tipoDocSeleccionado;
            string medioPago = _medioPagoSeleccionado;
            decimal pagaCon = 0;
            decimal vuelto = 0;

            // Validación preventiva de Factura
            if (tipoDoc.Contains("Factura") && (string.IsNullOrWhiteSpace(_ticketSeleccionado.RuT) || _ticketSeleccionado.RuT.Contains("66.666.666")))
            {
                MessageBox.Show("Para emitir Factura Electrónica el ticket debe tener asignado un cliente formal con RUT y Razón Social.", "Factura Requiere Cliente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (medioPago == "Crédito Comercial")
            {
                using var db = new AppDbContext();
                string rutLimpio = RutHelper.Limpiar(_ticketSeleccionado.RuT ?? "");
                var cliente = db.Clientes.FirstOrDefault(c => (c.IdCliente == _ticketSeleccionado.Idcliente && c.IdCliente > 0) || (c.Rut == rutLimpio || c.Rut == _ticketSeleccionado.RuT));
                if (cliente == null)
                {
                    MessageBox.Show("El ticket no tiene un cliente válido registrado para otorgar crédito comercial.", "Cliente Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var resCredito = CreditoService.ValidarVentaCredito(cliente, _ticketSeleccionado.Total, db);
                if (!resCredito.EsValido)
                {
                    MessageBox.Show(resCredito.MensajeError, "Crédito Comercial Denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
            }
            else if (medioPago.Contains("Múltiple"))
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
                pagaCon = MonedaHelper.Limpiar(txtPagaCon.Text);
                if (pagaCon < _ticketSeleccionado.Total)
                {
                    MessageBox.Show("Ingrese un monto en efectivo suficiente para el cobro.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                string cajero = _usuarioActual?.NombreUsuario ?? "admin";
                
                // El servicio asigna folios, busca cliente, completa giro/dirección y actualiza el objeto en memoria
                int folioOficial = _cajaService.ProcesarCobroTicket(
                    _ticketSeleccionado, tipoDoc, medioPago, vuelto, _turnoActual.CajaTurnoID, cajero
                );

                if (_medioPagoSeleccionado == "Crédito Comercial")
                {
                    using var db = new AppDbContext();
                    string rutLimpio = RutHelper.Limpiar(_ticketSeleccionado.RuT ?? "");
                    var cliente = db.Clientes.FirstOrDefault(c => (c.IdCliente == _ticketSeleccionado.Idcliente && c.IdCliente > 0) || (c.Rut == rutLimpio || c.Rut == _ticketSeleccionado.RuT));
                    if (cliente != null)
                    {
                        CreditoService.RegistrarFacturaCredito(_ticketSeleccionado.idTve, folioOficial, DateTime.Now, cliente, _ticketSeleccionado.Total, cajero, db);
                    }
                }

                var detalles = _cajaService.ObtenerDetallesTicket(_ticketSeleccionado.idTve);
                List<DetalleCarrito> itemsCarrito = detalles.Select(item => new DetalleCarrito
                {
                    ProductoID = item.IdProducto,
                    Nombre = item.NmbProducto ?? "Producto",
                    PrecioUnitario = item.Precio,
                    Cantidad = item.Cantidad
                }).ToList();

                MessageBox.Show($"¡{tipoDoc.ToUpper()} N° {folioOficial} PROCESADA CON ÉXITO!\n\n" +
                                $"• Medio de Pago: {medioPago}\n" +
                                $"• Total: ${_ticketSeleccionado.Total:N0}\n" +
                                $"• Vuelto: ${vuelto:N0}",
                                "Cobro Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Ahora _ticketSeleccionado viaja con iddocDTE = 33/39 y sus datos de cliente completos
                FormTicketModal formTicket = new FormTicketModal(_ticketSeleccionado, itemsCarrito, pagaCon, vuelto);
                formTicket.ShowDialog(this);

                txtBuscarTicket.Clear();
                CargarTicketsPendientes();
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

            var res = MessageBox.Show($"¿Desea anular el Ticket N° {_ticketSeleccionado.nroDTE}?\n\n⚠️ Esta acción devolverá los productos al inventario.", "Anulación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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