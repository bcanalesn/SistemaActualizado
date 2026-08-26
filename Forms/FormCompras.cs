using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;
using SISTEMAACTUALIZADO.Modals;

namespace SISTEMAACTUALIZADO
{
    public class FormCompras : Form
    {
        private readonly CompraService _compraService = new CompraService();
        private List<DetalleCompra> _detalleFactura = new List<DetalleCompra>();
        private List<Producto> _productosCache = new List<Producto>();
        private List<Proveedor> _proveedoresCache = new List<Proveedor>();

        private bool _esModoMercaderia = true;

        // Selectores de Modo (Tarjetas)
        private Panel pnlCardMercaderia = null!;
        private Panel pnlCardGasto = null!;
        private Label lblBadgeMercaderiaCheck = null!;
        private Label lblBadgeGastoCheck = null!;
        private Label lblTagDocumento = null!;

        // Paso 1: Proveedor y Documento
        private Proveedor? _proveedorSeleccionado = null;
        private TextBox txtBuscarProveedor = null!;
        private Button btnLimpiarBuscadorProv = null!;
        private Button btnNuevoProveedor = null!;
        private Button btnEditarProveedor = null!;
        private ListBox lstSugerenciasProveedores = null!;
        private ComboBox cbTipoDocumento = null!;
        private TextBox txtNroDocumento = null!;
        private DateTimePicker dtpFechaEmision = null!;
        private const string PLACEHOLDER_PROV = "🔍 Buscar por RUT o Razón Social...";

        // Paso 2 (Modo Mercadería)
        private Panel pnlPaso2Mercaderia = null!;
        private Producto? _productoSeleccionado = null;
        private TextBox txtBuscarProducto = null!;
        private Button btnLimpiarBuscadorProd = null!;
        private Button btnNuevoProducto = null!;
        private Button btnEditarProducto = null!;
        private ListBox lstSugerenciasProductos = null!;
        private TextBox txtCantidadRecibida = null!;
        private TextBox txtPrecioCosto = null!;
        private TextBox txtPvpSugerido = null!;
        private Label lblPreviewPVP = null!;
        private const string PLACEHOLDER_PROD = "🔍 Buscar por Código o Nombre...";

        // Paso 2 (Modo Gasto Interno)
        private Panel pnlPaso2Gasto = null!;
        private TextBox txtDescripcionGasto = null!;
        private ComboBox cbCategoriaGasto = null!;
        private TextBox txtMontoNetoGasto = null!;
        private CheckBox chkCapitalizaActivo = null!;
        private TextBox txtNombreActivoFijo = null!;

        // Paso 3: Grilla y Totales
        private Label lblTituloGrilla = null!;
        private Label lblContadorLineas = null!;
        private DataGridView dgvDetalle = null!;
        private Label lblTotalNeto = null!;
        private Label lblIvaCredito = null!;
        private Label lblTotalBruto = null!;
        private Panel pnlAlertaDestino = null!;
        private Label lblAlertaDestinoTexto = null!;
        private Button btnRegistrarCompra = null!;

        public FormCompras()
        {
            InitializeComponent();
            CargarDatosIniciales();
            AplicarModoVisual(true);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 16), AutoScroll = true };

            // Panel Lateral de Totales y Confirmación
            Panel pnlDerecha = CrearPanelResumenLateral();

            // Panel Izquierdo
            Panel pnlIzquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 0) };

            Panel pnlModos = CrearBarraSelectoresModo();
            Panel pnlPaso1 = CrearPaso1Documento();
            CrearPaso2Detalle();
            Panel pnlGrilla = CrearBloqueGrilla();

            pnlIzquierda.Controls.Add(pnlGrilla);
            pnlIzquierda.Controls.Add(pnlPaso2Gasto);
            pnlIzquierda.Controls.Add(pnlPaso2Mercaderia);
            pnlIzquierda.Controls.Add(pnlPaso1);
            pnlIzquierda.Controls.Add(pnlModos);

            pnlMain.Controls.Add(pnlIzquierda);
            pnlMain.Controls.Add(pnlDerecha);

            // Listas Predictivas Flotantes
            lstSugerenciasProveedores = new ListBox { Size = new Size(320, 130), Location = new Point(36, 175), Font = new Font("Segoe UI", 9F), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            lstSugerenciasProveedores.Click += LstSugerenciasProveedores_Click;

            lstSugerenciasProductos = new ListBox { Size = new Size(320, 130), Location = new Point(36, 280), Font = new Font("Segoe UI", 9F), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            lstSugerenciasProductos.Click += LstSugerenciasProductos_Click;

            pnlMain.Controls.Add(lstSugerenciasProductos);
            pnlMain.Controls.Add(lstSugerenciasProveedores);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearBarraSelectoresModo()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 78, Margin = new Padding(0, 0, 0, 10) };

            pnlCardMercaderia = new Panel { Location = new Point(0, 4), Size = new Size(390, 68), BackColor = Color.FromArgb(239, 246, 255), Cursor = Cursors.Hand };
            pnlCardMercaderia.Paint += (s, e) => {
                Color border = _esModoMercaderia ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240);
                ControlPaint.DrawBorder(e.Graphics, pnlCardMercaderia.ClientRectangle, border, _esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid, border, _esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid, border, _esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid, border, _esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid);
            };

            Label lblIconM = new Label { Text = "📦", Font = new Font("Segoe UI", 16F), Location = new Point(12, 16), AutoSize = true };
            Label lblTitM = new Label { Text = "Mercadería para venta", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(52, 14), AutoSize = true };
            Label lblSubM = new Label { Text = "Aumenta stock de productos", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(52, 36), AutoSize = true };
            lblBadgeMercaderiaCheck = new Label { Text = "✔", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(37, 99, 235), Size = new Size(22, 22), Location = new Point(350, 22), TextAlign = ContentAlignment.MiddleCenter };
            pnlCardMercaderia.Controls.AddRange(new Control[] { lblIconM, lblTitM, lblSubM, lblBadgeMercaderiaCheck });
            pnlCardMercaderia.Click += (s, e) => AplicarModoVisual(true);
            foreach (Control c in pnlCardMercaderia.Controls) c.Click += (s, e) => AplicarModoVisual(true);

            pnlCardGasto = new Panel { Location = new Point(410, 4), Size = new Size(420, 68), BackColor = Color.White, Cursor = Cursors.Hand };
            pnlCardGasto.Paint += (s, e) => {
                Color border = !_esModoMercaderia ? Color.FromArgb(124, 58, 237) : Color.FromArgb(226, 232, 240);
                ControlPaint.DrawBorder(e.Graphics, pnlCardGasto.ClientRectangle, border, !_esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid, border, !_esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid, border, !_esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid, border, !_esModoMercaderia ? 2 : 1, ButtonBorderStyle.Solid);
            };

            Label lblIconG = new Label { Text = "📄", Font = new Font("Segoe UI", 16F), Location = new Point(12, 16), AutoSize = true };
            Label lblTitG = new Label { Text = "Gasto / insumo interno", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(52, 14), AutoSize = true };
            Label lblSubG = new Label { Text = "No afecta stock — solo Libro de Compras", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(52, 36), AutoSize = true };
            lblBadgeGastoCheck = new Label { Text = "", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(226, 232, 240), Size = new Size(22, 22), Location = new Point(380, 22), TextAlign = ContentAlignment.MiddleCenter };
            pnlCardGasto.Controls.AddRange(new Control[] { lblIconG, lblTitG, lblSubG, lblBadgeGastoCheck });
            pnlCardGasto.Click += (s, e) => AplicarModoVisual(false);
            foreach (Control c in pnlCardGasto.Controls) c.Click += (s, e) => AplicarModoVisual(false);

            pnl.Controls.AddRange(new Control[] { pnlCardMercaderia, pnlCardGasto });
            return pnl;
        }

        private Panel CrearPaso1Documento()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8), Margin = new Padding(0, 0, 0, 10) };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            lblTagDocumento = new Label { Text = "📦 DOCUMENTO DE MERCADERÍA", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235), BackColor = Color.FromArgb(239, 246, 255), Location = new Point(16, 8), AutoSize = true, Padding = new Padding(4, 2, 4, 2) };

            Panel pnlProv = new Panel { Location = new Point(16, 32), Size = new Size(280, 52) };
            Label lp = new Label { Text = "1. PROVEEDOR (RUT O NOMBRE)", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, 0), AutoSize = true };
            txtBuscarProveedor = new TextBox { Text = PLACEHOLDER_PROV, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), Location = new Point(0, 18), Size = new Size(185, 24) };
            ConfigurarEventosBuscadorProveedor();
            btnLimpiarBuscadorProv = CrearBotonIcono("🗑️", 188, 18, (s, e) => ResetearBuscadorProveedor());
            btnNuevoProveedor = CrearBotonAccionRapida("➕", Color.FromArgb(2, 132, 199), 218, 18, BtnNuevoProveedor_Click);
            btnEditarProveedor = CrearBotonAccionRapida("✏️", Color.FromArgb(245, 158, 11), 248, 18, BtnEditarProveedor_Click);
            pnlProv.Controls.AddRange(new Control[] { lp, txtBuscarProveedor, btnLimpiarBuscadorProv, btnNuevoProveedor, btnEditarProveedor });

            Label lt = new Label { Text = "2. TIPO DOC.", Location = new Point(310, 32), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            cbTipoDocumento = new ComboBox { Location = new Point(310, 50), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTipoDocumento.Items.AddRange(new string[] { "Factura de Compra", "Factura Exenta", "Boleta de Compra", "Guía de Recepción" });
            cbTipoDocumento.SelectedIndex = 0;

            Label ln = new Label { Text = "3. N° DE DOCUMENTO", Location = new Point(475, 32), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtNroDocumento = new TextBox { Location = new Point(475, 50), Size = new Size(110, 26), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtNroDocumento.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };

            Label lf = new Label { Text = "4. FECHA EMISIÓN", Location = new Point(600, 32), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            dtpFechaEmision = new DateTimePicker { Location = new Point(600, 50), Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            Button btnListaProv = new Button { Text = "📖 Lista Proveedores", Location = new Point(730, 48), Size = new Size(130, 28), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnListaProv.FlatAppearance.BorderSize = 0;
            btnListaProv.Click += BtnListaProveedores_Click;

            pnl.Controls.AddRange(new Control[] { lblTagDocumento, pnlProv, lt, cbTipoDocumento, ln, txtNroDocumento, lf, dtpFechaEmision, btnListaProv });
            return pnl;
        }

        private void CrearPaso2Detalle()
        {
            // Mercadería
            pnlPaso2Mercaderia = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8), Margin = new Padding(0, 0, 0, 10) };
            pnlPaso2Mercaderia.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlPaso2Mercaderia.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Panel pnlProd = new Panel { Location = new Point(16, 10), Size = new Size(280, 52) };
            Label lpr = new Label { Text = "SELECCIONAR PRODUCTO (CÓDIGO O NOMBRE)", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, 0), AutoSize = true };
            txtBuscarProducto = new TextBox { Text = PLACEHOLDER_PROD, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), Location = new Point(0, 18), Size = new Size(185, 24) };
            ConfigurarEventosBuscadorProducto();
            btnLimpiarBuscadorProd = CrearBotonIcono("🗑️", 188, 18, (s, e) => ResetearBuscadorProducto());
            btnNuevoProducto = CrearBotonAccionRapida("➕", Color.FromArgb(2, 132, 199), 218, 18, BtnNuevoProducto_Click);
            btnEditarProducto = CrearBotonAccionRapida("✏️", Color.FromArgb(245, 158, 11), 248, 18, BtnEditarProducto_Click);
            pnlProd.Controls.AddRange(new Control[] { lpr, txtBuscarProducto, btnLimpiarBuscadorProd, btnNuevoProducto, btnEditarProducto });

            Label lc = new Label { Text = "CANT. RECIBIDA", Location = new Point(310, 10), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtCantidadRecibida = new TextBox { Location = new Point(310, 28), Size = new Size(70, 24), Text = "1", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
            txtCantidadRecibida.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };

            Label lcost = new Label { Text = "COSTO NETO ($)", Location = new Point(395, 10), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtPrecioCosto = new TextBox { Location = new Point(395, 28), Size = new Size(95, 24), Text = "0", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtPrecioCosto.TextChanged += (s, e) => CalcularPreviewPrecioVenta();

            lblPreviewPVP = new Label { Text = "PVP Sug: $0", Location = new Point(395, 54), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };

            Label lpvp = new Label { Text = "PRECIO VENTA (IVA INC.)", Location = new Point(505, 10), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtPvpSugerido = new TextBox { Location = new Point(505, 28), Size = new Size(110, 24), Font = new Font("Segoe UI", 9F), PlaceholderText = "Auto-sugerido" };

            Button btnAddM = new Button { Text = "+ Agregar", Location = new Point(630, 24), Size = new Size(95, 30), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddM.FlatAppearance.BorderSize = 0;
            btnAddM.Click += BtnAgregarMercaderia_Click;

            pnlPaso2Mercaderia.Controls.AddRange(new Control[] { pnlProd, lc, txtCantidadRecibida, lcost, txtPrecioCosto, lblPreviewPVP, lpvp, txtPvpSugerido, btnAddM });

            // Gasto Interno
            pnlPaso2Gasto = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8), Margin = new Padding(0, 0, 0, 10), Visible = false };
            pnlPaso2Gasto.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlPaso2Gasto.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label ldesc = new Label { Text = "DESCRIPCIÓN DEL GASTO", Location = new Point(16, 8), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtDescripcionGasto = new TextBox { Location = new Point(16, 26), Size = new Size(280, 24), Font = new Font("Segoe UI", 9F), PlaceholderText = "Ej: Materiales reparación escritorio recepción" };

            Label lcat = new Label { Text = "CATEGORÍA / CUENTA CONTABLE", Location = new Point(310, 8), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            cbCategoriaGasto = new ComboBox { Location = new Point(310, 26), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cbCategoriaGasto.Items.AddRange(new string[] { "Mantención de activos", "Materiales de oficina", "Insumos y Aseo", "Servicios Básicos", "Mobiliario y Equipamiento", "Gastos Operacionales Varios" });
            cbCategoriaGasto.SelectedIndex = 0;

            Label lm = new Label { Text = "MONTO NETO ($)", Location = new Point(505, 8), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtMontoNetoGasto = new TextBox { Location = new Point(505, 26), Size = new Size(110, 24), Text = "0", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            chkCapitalizaActivo = new CheckBox { Text = "¿Se capitaliza a un activo fijo existente?", Location = new Point(16, 60), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237) };
            txtNombreActivoFijo = new TextBox { Location = new Point(280, 58), Size = new Size(180, 22), Font = new Font("Segoe UI", 8.5F), Visible = false, PlaceholderText = "Seleccionar activo..." };
            chkCapitalizaActivo.CheckedChanged += (s, e) => txtNombreActivoFijo.Visible = chkCapitalizaActivo.Checked;

            Button btnAddG = new Button { Text = "+ Agregar", Location = new Point(630, 24), Size = new Size(95, 30), BackColor = Color.FromArgb(124, 58, 237), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddG.FlatAppearance.BorderSize = 0;
            btnAddG.Click += BtnAgregarGasto_Click;

            pnlPaso2Gasto.Controls.AddRange(new Control[] { ldesc, txtDescripcionGasto, lcat, cbCategoriaGasto, lm, txtMontoNetoGasto, chkCapitalizaActivo, txtNombreActivoFijo, btnAddG });
        }

        private Panel CrearBloqueGrilla()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14) };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Panel pnlHead = new Panel { Dock = DockStyle.Top, Height = 28 };
            lblTituloGrilla = new Label { Text = "Productos en esta recepción", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, 2), AutoSize = true };
            lblContadorLineas = new Label { Text = "0 líneas", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), BackColor = Color.FromArgb(241, 245, 249), Location = new Point(230, 2), AutoSize = true, Padding = new Padding(4, 2, 4, 2) };
            pnlHead.Controls.AddRange(new Control[] { lblTituloGrilla, lblContadorLineas });

            dgvDetalle = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 }
            };
            dgvDetalle.EnableHeadersVisualStyles = false;
            dgvDetalle.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvDetalle.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            dgvDetalle.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            dgvDetalle.CellClick += DgvDetalle_CellClick;

            pnl.Controls.Add(dgvDetalle);
            pnl.Controls.Add(pnlHead);
            return pnl;
        }

        private Panel CrearPanelResumenLateral()
        {
            Panel pnl = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.White, Padding = new Padding(16) };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblT = new Label { Text = "CONFIRMAR REGISTRO", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 24 };

            Panel pn = new Panel { Dock = DockStyle.Top, Height = 26 };
            Label lnt = new Label { Text = "Neto Documento", ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, 4), AutoSize = true };
            lblTotalNeto = new Label { Text = "$ 0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, AutoSize = true };
            pn.Controls.AddRange(new Control[] { lnt, lblTotalNeto });

            Panel pi = new Panel { Dock = DockStyle.Top, Height = 26 };
            Label lit = new Label { Text = "IVA (19%)", ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, 4), AutoSize = true };
            lblIvaCredito = new Label { Text = "$ 0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, AutoSize = true };
            pi.Controls.AddRange(new Control[] { lit, lblIvaCredito });

            Panel pbox = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = Color.FromArgb(240, 253, 244), Margin = new Padding(0, 10, 0, 10), Padding = new Padding(8) };
            Label lbtit = new Label { Text = "TOTAL A PAGAR", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
            lblTotalBruto = new Label { Text = "$ 0", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pbox.Controls.AddRange(new Control[] { lblTotalBruto, lbtit });

            pnlAlertaDestino = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(8) };
            lblAlertaDestinoTexto = new Label { Text = "📦 Este documento incrementará el stock de productos y quedará registrado en el Libro de Compras.", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(37, 99, 235), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            pnlAlertaDestino.Controls.Add(lblAlertaDestinoTexto);

            btnRegistrarCompra = new Button { Text = "PROCESAR RECEPCIÓN\nE INCREMENTAR STOCK", Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRegistrarCompra.FlatAppearance.BorderSize = 0;
            btnRegistrarCompra.Click += BtnRegistrarCompra_Click;

            pnl.Controls.AddRange(new Control[] { pbox, pi, pn, lblT, pnlAlertaDestino, btnRegistrarCompra });
            return pnl;
        }

        private void AplicarModoVisual(bool esMercaderia)
        {
            _esModoMercaderia = esMercaderia;

            if (esMercaderia)
            {
                pnlCardMercaderia.BackColor = Color.FromArgb(239, 246, 255);
                lblBadgeMercaderiaCheck.Text = "✔";
                lblBadgeMercaderiaCheck.BackColor = Color.FromArgb(37, 99, 235);

                pnlCardGasto.BackColor = Color.White;
                lblBadgeGastoCheck.Text = "";
                lblBadgeGastoCheck.BackColor = Color.FromArgb(226, 232, 240);

                lblTagDocumento.Text = "📦 DOCUMENTO DE MERCADERÍA";
                lblTagDocumento.ForeColor = Color.FromArgb(37, 99, 235);
                lblTagDocumento.BackColor = Color.FromArgb(239, 246, 255);

                pnlPaso2Mercaderia.Visible = true;
                pnlPaso2Gasto.Visible = false;

                lblTituloGrilla.Text = "Productos en esta recepción";
                ConfigurarColumnasGrillaMercaderia();

                pnlAlertaDestino.BackColor = Color.FromArgb(239, 246, 255);
                lblAlertaDestinoTexto.Text = "📦 Este documento incrementará el stock de productos y quedará registrado en el Libro de Compras.";
                lblAlertaDestinoTexto.ForeColor = Color.FromArgb(37, 99, 235);

                btnRegistrarCompra.Text = "PROCESAR RECEPCIÓN\nE INCREMENTAR STOCK";
                btnRegistrarCompra.BackColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                pnlCardMercaderia.BackColor = Color.White;
                lblBadgeMercaderiaCheck.Text = "";
                lblBadgeMercaderiaCheck.BackColor = Color.FromArgb(226, 232, 240);

                pnlCardGasto.BackColor = Color.FromArgb(250, 245, 255);
                lblBadgeGastoCheck.Text = "✔";
                lblBadgeGastoCheck.BackColor = Color.FromArgb(124, 58, 237);

                lblTagDocumento.Text = "📄 DOCUMENTO DE GASTO / INSUMO";
                lblTagDocumento.ForeColor = Color.FromArgb(124, 58, 237);
                lblTagDocumento.BackColor = Color.FromArgb(250, 245, 255);

                pnlPaso2Mercaderia.Visible = false;
                pnlPaso2Gasto.Visible = true;

                lblTituloGrilla.Text = "Gastos en este documento";
                ConfigurarColumnasGrillaGasto();

                pnlAlertaDestino.BackColor = Color.FromArgb(250, 245, 255);
                lblAlertaDestinoTexto.Text = "📄 Este documento NO afectará el stock. Solo quedará registrado en el Libro de Compras como gasto.";
                lblAlertaDestinoTexto.ForeColor = Color.FromArgb(124, 58, 237);

                btnRegistrarCompra.Text = "REGISTRAR GASTO EN\nLIBRO DE COMPRAS";
                btnRegistrarCompra.BackColor = Color.FromArgb(124, 58, 237);
            }

            pnlCardMercaderia.Invalidate();
            pnlCardGasto.Invalidate();
            RefrescarGrilla();
        }

        private void ConfigurarColumnasGrillaMercaderia()
        {
            dgvDetalle.Columns.Clear();
            dgvDetalle.Columns.Add("Producto", "PRODUCTO");
            dgvDetalle.Columns.Add("Cant", "CANT.");
            dgvDetalle.Columns.Add("CostoNeto", "COSTO NETO");
            dgvDetalle.Columns.Add("PvpSugerido", "PVP SUGERIDO");
            dgvDetalle.Columns.Add("Subtotal", "SUBTOTAL");

            var btnEliminar = new DataGridViewButtonColumn { Name = "Eliminar", HeaderText = "", Text = "✕", UseColumnTextForButtonValue = true, Width = 35 };
            btnEliminar.FlatStyle = FlatStyle.Flat;
            dgvDetalle.Columns.Add(btnEliminar);

            dgvDetalle.Columns["Cant"].Width = 60;
            dgvDetalle.Columns["CostoNeto"].DefaultCellStyle.Format = "$#,##0";
            dgvDetalle.Columns["PvpSugerido"].DefaultCellStyle.Format = "$#,##0";
            dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0";
        }

        private void ConfigurarColumnasGrillaGasto()
        {
            dgvDetalle.Columns.Clear();
            dgvDetalle.Columns.Add("Descripcion", "DESCRIPCIÓN");
            dgvDetalle.Columns.Add("Categoria", "CATEGORÍA");
            dgvDetalle.Columns.Add("MontoNeto", "MONTO NETO");

            var btnEliminar = new DataGridViewButtonColumn { Name = "Eliminar", HeaderText = "", Text = "✕", UseColumnTextForButtonValue = true, Width = 35 };
            btnEliminar.FlatStyle = FlatStyle.Flat;
            dgvDetalle.Columns.Add(btnEliminar);

            dgvDetalle.Columns["MontoNeto"].DefaultCellStyle.Format = "$#,##0";
        }

        private void RefrescarGrilla()
        {
            dgvDetalle.Rows.Clear();
            var filtrados = _detalleFactura.Where(d => _esModoMercaderia ? d.TipoItem == "MERCADERIA" : d.TipoItem == "GASTO").ToList();

            foreach (var item in filtrados)
            {
                if (_esModoMercaderia)
                {
                    dgvDetalle.Rows.Add(item.NombreProducto, item.Cantidad, item.PrecioCostoUnitario, item.PvpSugerido, item.Subtotal);
                }
                else
                {
                    dgvDetalle.Rows.Add(item.DescripcionGasto, item.CategoriaGasto, item.Subtotal);
                }
            }

            lblContadorLineas.Text = $"{filtrados.Count} líneas";

            decimal neto = filtrados.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m);
            decimal total = neto + iva;

            lblTotalNeto.Text = $"$ {neto:N0}";
            lblIvaCredito.Text = $"$ {iva:N0}";
            lblTotalBruto.Text = $"$ {total:N0}";
        }

        private void DgvDetalle_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvDetalle.Columns[e.ColumnIndex].Name == "Eliminar")
            {
                var filtrados = _detalleFactura.Where(d => _esModoMercaderia ? d.TipoItem == "MERCADERIA" : d.TipoItem == "GASTO").ToList();
                if (e.RowIndex < filtrados.Count)
                {
                    _detalleFactura.Remove(filtrados[e.RowIndex]);
                    RefrescarGrilla();
                }
            }
        }

        private void BtnAgregarMercaderia_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                MessageBox.Show("Busque y seleccione un producto.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProducto.Focus();
                return;
            }

            if (!int.TryParse(txtCantidadRecibida.Text.Trim(), out int cant) || cant <= 0 || !decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) || costo <= 0)
            {
                MessageBox.Show("Ingrese una cantidad y costo válidos.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal.TryParse(txtPvpSugerido.Text.Trim(), out decimal pvp);

            _detalleFactura.Add(new DetalleCompra
            {
                TipoItem = "MERCADERIA",
                ProductoID = _productoSeleccionado.ProductoID,
                NombreProducto = _productoSeleccionado.Nombre,
                Cantidad = cant,
                PrecioCostoUnitario = costo,
                PvpSugerido = pvp > 0 ? pvp : _productoSeleccionado.PrecioUnitario,
                Subtotal = cant * costo,
                AfectaStock = true
            });

            RefrescarGrilla();
            ResetearBuscadorProducto();
            txtCantidadRecibida.Text = "1";
            txtBuscarProducto.Focus();
        }

        private void BtnAgregarGasto_Click(object? sender, EventArgs e)
        {
            string desc = txtDescripcionGasto.Text.Trim();
            if (string.IsNullOrEmpty(desc) || !decimal.TryParse(txtMontoNetoGasto.Text.Trim(), out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Indique una descripción y monto neto válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _detalleFactura.Add(new DetalleCompra
            {
                TipoItem = "GASTO",
                ProductoID = null,
                NombreProducto = desc,
                DescripcionGasto = desc,
                CategoriaGasto = cbCategoriaGasto.SelectedItem?.ToString() ?? "Gastos Operacionales",
                Cantidad = 1,
                PrecioCostoUnitario = monto,
                Subtotal = monto,
                ActivoFijoRef = chkCapitalizaActivo.Checked ? txtNombreActivoFijo.Text.Trim() : null,
                AfectaStock = false
            });

            RefrescarGrilla();
            txtDescripcionGasto.Clear();
            txtMontoNetoGasto.Text = "0";
            chkCapitalizaActivo.Checked = false;
            txtNombreActivoFijo.Clear();
        }

        private void BtnRegistrarCompra_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null)
            {
                MessageBox.Show("Seleccione un proveedor.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtNroDocumento.Text.Trim(), out int folio) || folio <= 0)
            {
                MessageBox.Show("Ingrese un número de documento válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var itemsProcesar = _detalleFactura.Where(d => _esModoMercaderia ? d.TipoItem == "MERCADERIA" : d.TipoItem == "GASTO").ToList();
            if (itemsProcesar.Count == 0)
            {
                MessageBox.Show("Agregue al menos una línea al documento.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal neto = itemsProcesar.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m);
            decimal total = neto + iva;

            Compra compra = new Compra
            {
                TipoCompra = _esModoMercaderia ? "MERCADERIA" : "GASTO_INTERNO",
                TipoDocumento = cbTipoDocumento.SelectedItem?.ToString() ?? "Factura de Compra",
                RutProveedor = _proveedorSeleccionado.Rut,
                RazonSocialProveedor = _proveedorSeleccionado.RazonSocial,
                NroFacturaProveedor = folio,
                FechaEmision = dtpFechaEmision.Value,
                FechaRecepcion = DateTime.Now,
                MontoNeto = neto,
                MontoIva = iva,
                MontoTotal = total,
                UsuarioReceptor = "Bárbara",
                Estado = "Recibida"
            };

            try
            {
                _compraService.RegistrarFacturaCompra(compra, itemsProcesar, actualizarPreciosVenta: true);

                string mensaje = _esModoMercaderia 
                    ? $"Documento N° {folio} procesado.\n• Stock incrementado en {itemsProcesar.Sum(i => i.Cantidad)} unidades."
                    : $"Gasto Operacional N° {folio} registrado en el Libro de Compras.\n• No se alteró el inventario de venta.";

                MessageBox.Show(mensaje, "Documento Registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _detalleFactura.RemoveAll(d => _esModoMercaderia ? d.TipoItem == "MERCADERIA" : d.TipoItem == "GASTO");
                txtNroDocumento.Clear();
                ResetearBuscadorProveedor();
                RefrescarGrilla();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar compra: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // HELPERS Y BÚSQUEDAS
        // ==========================================
        private void CargarDatosIniciales()
        {
            using var db = new AppDbContext();
            _proveedoresCache = db.Proveedores.Where(p => p.Estado).OrderBy(p => p.RazonSocial).ToList();
            _productosCache = db.Productos.Where(p => p.Estado).OrderBy(p => p.Nombre).ToList();
        }

        private void ConfigurarEventosBuscadorProveedor()
        {
            txtBuscarProveedor.GotFocus += (s, e) => { if (txtBuscarProveedor.Text == PLACEHOLDER_PROV) { txtBuscarProveedor.Text = ""; txtBuscarProveedor.ForeColor = Color.FromArgb(15, 23, 42); txtBuscarProveedor.Font = new Font("Segoe UI", 9F); } };
            txtBuscarProveedor.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscarProveedor.Text)) ResetearBuscadorProveedor(); };
            txtBuscarProveedor.TextChanged += (s, e) => {
                if (txtBuscarProveedor.Text == PLACEHOLDER_PROV) return;
                string q = txtBuscarProveedor.Text.Trim().ToLower();
                if (q.Length >= 1)
                {
                    var f = _proveedoresCache.Where(p => p.RazonSocial.ToLower().Contains(q) || p.Rut.ToLower().Contains(q)).Take(6).ToList();
                    lstSugerenciasProveedores.DataSource = f.Count > 0 ? f : null;
                    lstSugerenciasProveedores.DisplayMember = "RazonSocial";
                    lstSugerenciasProveedores.Visible = f.Count > 0;
                    lstSugerenciasProveedores.BringToFront();
                }
                else lstSugerenciasProveedores.Visible = false;
            };
        }

        private void ResetearBuscadorProveedor()
        {
            _proveedorSeleccionado = null;
            txtBuscarProveedor.Text = PLACEHOLDER_PROV;
            txtBuscarProveedor.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            txtBuscarProveedor.ForeColor = Color.FromArgb(148, 163, 184);
            lstSugerenciasProveedores.Visible = false;
        }

        private void LstSugerenciasProveedores_Click(object? sender, EventArgs e)
        {
            if (lstSugerenciasProveedores.SelectedItem is Proveedor p)
            {
                _proveedorSeleccionado = p;
                txtBuscarProveedor.Text = $"{p.RazonSocial} ({p.Rut})";
                txtBuscarProveedor.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                txtBuscarProveedor.ForeColor = Color.FromArgb(37, 99, 235);
                lstSugerenciasProveedores.Visible = false;
                txtNroDocumento.Focus();
            }
        }

        private void ConfigurarEventosBuscadorProducto()
        {
            txtBuscarProducto.GotFocus += (s, e) => { if (txtBuscarProducto.Text == PLACEHOLDER_PROD) { txtBuscarProducto.Text = ""; txtBuscarProducto.ForeColor = Color.FromArgb(15, 23, 42); txtBuscarProducto.Font = new Font("Segoe UI", 9F); } };
            txtBuscarProducto.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscarProducto.Text)) ResetearBuscadorProducto(); };
            txtBuscarProducto.TextChanged += (s, e) => {
                if (txtBuscarProducto.Text == PLACEHOLDER_PROD) return;
                string q = txtBuscarProducto.Text.Trim().ToLower();
                if (q.Length >= 1)
                {
                    var f = _productosCache.Where(p => p.Nombre.ToLower().Contains(q) || p.CodigoBarra.ToLower().Contains(q)).Take(6).ToList();
                    lstSugerenciasProductos.DataSource = f.Count > 0 ? f : null;
                    lstSugerenciasProductos.DisplayMember = "Nombre";
                    lstSugerenciasProductos.Visible = f.Count > 0;
                    lstSugerenciasProductos.BringToFront();
                }
                else lstSugerenciasProductos.Visible = false;
            };
        }

        private void ResetearBuscadorProducto()
        {
            _productoSeleccionado = null;
            txtBuscarProducto.Text = PLACEHOLDER_PROD;
            txtBuscarProducto.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            txtBuscarProducto.ForeColor = Color.FromArgb(148, 163, 184);
            lstSugerenciasProductos.Visible = false;
            txtPrecioCosto.Text = "0";
            lblPreviewPVP.Text = "PVP Sug: $0";
            txtPvpSugerido.Clear();
        }

        private void LstSugerenciasProductos_Click(object? sender, EventArgs e)
        {
            if (lstSugerenciasProductos.SelectedItem is Producto p)
            {
                _productoSeleccionado = p;
                txtBuscarProducto.Text = $"{p.Nombre} (SKU: {p.CodigoBarra})";
                txtBuscarProducto.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                txtBuscarProducto.ForeColor = Color.FromArgb(37, 99, 235);
                lstSugerenciasProductos.Visible = false;

                decimal costo = p.PrecioCosto > 0 ? p.PrecioCosto : Math.Round(p.PrecioUnitario / 1.19m / 1.30m, 0);
                txtPrecioCosto.Text = costo.ToString("0");
                CalcularPreviewPrecioVenta();

                txtCantidadRecibida.Focus();
                txtCantidadRecibida.SelectAll();
            }
        }

        private void CalcularPreviewPrecioVenta()
        {
            if (decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) && costo > 0)
            {
                decimal pvp = Math.Round(costo * 1.30m * 1.19m / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
                lblPreviewPVP.Text = $"PVP Sug: ${pvp:N0}";
                txtPvpSugerido.Text = pvp.ToString("0");
            }
        }

        private void BtnNuevoProveedor_Click(object? sender, EventArgs e)
        {
            using var modal = new FormNuevoProveedorModal();
            if (modal.ShowDialog(this) == DialogResult.OK && modal.ProveedorResultado != null)
            {
                CargarDatosIniciales();
                _proveedorSeleccionado = modal.ProveedorResultado;
                txtBuscarProveedor.Text = $"{modal.ProveedorResultado.RazonSocial} ({modal.ProveedorResultado.Rut})";
                txtNroDocumento.Focus();
            }
        }

        private void BtnEditarProveedor_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null) return;
            using var modal = new FormNuevoProveedorModal(_proveedorSeleccionado);
            if (modal.ShowDialog(this) == DialogResult.OK && modal.ProveedorResultado != null)
            {
                CargarDatosIniciales();
                _proveedorSeleccionado = modal.ProveedorResultado;
                txtBuscarProveedor.Text = $"{modal.ProveedorResultado.RazonSocial} ({modal.ProveedorResultado.Rut})";
            }
        }

        private void BtnNuevoProducto_Click(object? sender, EventArgs e)
        {
            using var modal = new FormNuevoProductoModal();
            if (modal.ShowDialog(this) == DialogResult.OK && modal.ProductoResultado != null)
            {
                _productoSeleccionado = modal.ProductoResultado;
                txtBuscarProducto.Text = $"{modal.ProductoResultado.Nombre} (Nuevo)";
                txtPrecioCosto.Text = modal.ProductoResultado.PrecioCosto.ToString("0");
                CalcularPreviewPrecioVenta();
                txtCantidadRecibida.Text = modal.CantidadFactura.ToString();
            }
        }

        private void BtnEditarProducto_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null) return;
            using var modal = new FormNuevoProductoModal(_productoSeleccionado);
            if (modal.ShowDialog(this) == DialogResult.OK && modal.ProductoResultado != null)
            {
                CargarDatosIniciales();
                _productoSeleccionado = modal.ProductoResultado;
                txtBuscarProducto.Text = $"{modal.ProductoResultado.Nombre}";
                txtPrecioCosto.Text = modal.ProductoResultado.PrecioCosto.ToString("0");
                CalcularPreviewPrecioVenta();
            }
        }

        private void BtnListaProveedores_Click(object? sender, EventArgs e)
        {
            using var modal = new FormListaProveedoresModal();
            if (modal.ShowDialog(this) == DialogResult.OK && modal.ProveedorSeleccionado != null)
            {
                CargarDatosIniciales();
                _proveedorSeleccionado = modal.ProveedorSeleccionado;
                txtBuscarProveedor.Text = $"{modal.ProveedorSeleccionado.RazonSocial} ({modal.ProveedorSeleccionado.Rut})";
                txtNroDocumento.Focus();
            }
        }

        private Button CrearBotonIcono(string t, int x, int y, EventHandler h)
        {
            Button b = new Button { Text = t, Size = new Size(26, 26), Location = new Point(x, y), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            b.Click += h;
            return b;
        }

        private Button CrearBotonAccionRapida(string t, Color bg, int x, int y, EventHandler h)
        {
            Button b = new Button { Text = t, Size = new Size(26, 26), Location = new Point(x, y), BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += h;
            return b;
        }
    }
}