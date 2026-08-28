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
        private Button btnQuitarItem = null!;
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
            // Recomendado en el shell principal (Program.cs / Form contenedor):
            // Application.SetHighDpiMode(HighDpiMode.PerMonitorV2) + this.AutoScaleMode = AutoScaleMode.Dpi;
            // para que el escalado de Windows (125%/150%) no desalinee los controles.

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

            // Listas Predictivas Flotantes.
            // Ya NO se posicionan con Point fijo: ahora se ubican dinámicamente
            // justo debajo del cuadro de búsqueda real (ver PosicionarListaSugerencias),
            // porque con layout responsivo el cuadro puede estar en cualquier X/Y.
            lstSugerenciasProveedores = new ListBox { Size = new Size(320, 130), Font = new Font("Segoe UI", 9F), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            lstSugerenciasProveedores.Click += LstSugerenciasProveedores_Click;

            lstSugerenciasProductos = new ListBox { Size = new Size(320, 130), Font = new Font("Segoe UI", 9F), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            lstSugerenciasProductos.Click += LstSugerenciasProductos_Click;

            pnlMain.Controls.Add(lstSugerenciasProductos);
            pnlMain.Controls.Add(lstSugerenciasProveedores);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ==========================================
        // BARRA DE SELECTORES DE MODO (RESPONSIVA)
        // ==========================================
        private Panel CrearBarraSelectoresModo()
        {
            Panel pnl = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 78, 
                Margin = new Padding(0, 0, 0, 14), 
                Padding = new Padding(0, 0, 0, 8) 
            };

            TableLayoutPanel tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlCardMercaderia = CrearTarjetaModo("📦", "Mercadería para venta", "Aumenta stock de productos", out lblBadgeMercaderiaCheck, esActivoInicial: true);
            pnlCardMercaderia.Margin = new Padding(0, 2, 6, 2);
            pnlCardMercaderia.Paint += (s, e) =>
            {
                Color border = _esModoMercaderia ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240);
                int w = _esModoMercaderia ? 2 : 1;
                ControlPaint.DrawBorder(e.Graphics, pnlCardMercaderia.ClientRectangle, border, w, ButtonBorderStyle.Solid, border, w, ButtonBorderStyle.Solid, border, w, ButtonBorderStyle.Solid, border, w, ButtonBorderStyle.Solid);
            };
            AsignarClickRecursivo(pnlCardMercaderia, (s, e) => AplicarModoVisual(true));

            pnlCardGasto = CrearTarjetaModo("📄", "Gasto / insumo interno", "No afecta stock — solo Libro de Compras", out lblBadgeGastoCheck, esActivoInicial: false);
            pnlCardGasto.Margin = new Padding(6, 2, 0, 2);
            pnlCardGasto.Paint += (s, e) =>
            {
                Color border = !_esModoMercaderia ? Color.FromArgb(124, 58, 237) : Color.FromArgb(226, 232, 240);
                int w = !_esModoMercaderia ? 2 : 1;
                ControlPaint.DrawBorder(e.Graphics, pnlCardGasto.ClientRectangle, border, w, ButtonBorderStyle.Solid, border, w, ButtonBorderStyle.Solid, border, w, ButtonBorderStyle.Solid, border, w, ButtonBorderStyle.Solid);
            };
            AsignarClickRecursivo(pnlCardGasto, (s, e) => AplicarModoVisual(false));

            tabla.Controls.Add(pnlCardMercaderia, 0, 0);
            tabla.Controls.Add(pnlCardGasto, 1, 0);

            pnl.Controls.Add(tabla);
            return pnl;
        }

        private Panel CrearTarjetaModo(string icono, string titulo, string subtitulo, out Label badge, bool esActivoInicial)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = esActivoInicial ? Color.FromArgb(239, 246, 255) : Color.White, Cursor = Cursors.Hand };

            Label lblIcon = new Label { Text = icono, Font = new Font("Segoe UI", 16F), Dock = DockStyle.Left, Width = 46, TextAlign = ContentAlignment.MiddleCenter };

            Label badgeLocal = new Label
            {
                Text = esActivoInicial ? "✔" : "",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = esActivoInicial ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240),
                Size = new Size(22, 22),
                Dock = DockStyle.Right,
                Margin = new Padding(0, 22, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            badge = badgeLocal;

            Panel pnlTextos = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 14, 0, 0) };
            Label lblTit = new Label { Text = titulo, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, AutoSize = true };
            Label lblSub = new Label { Text = subtitulo, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, AutoSize = true };
            pnlTextos.Controls.Add(lblSub);
            pnlTextos.Controls.Add(lblTit);

            card.Controls.Add(pnlTextos);
            card.Controls.Add(badgeLocal);
            card.Controls.Add(lblIcon);
            return card;
        }

        private void AsignarClickRecursivo(Control contenedor, EventHandler handler)
        {
            contenedor.Click += handler;
            foreach (Control hijo in contenedor.Controls)
            {
                hijo.Click += handler;
                if (hijo.HasChildren) AsignarClickRecursivo(hijo, handler);
            }
        }

        // ==========================================
        // PASO 1: DOCUMENTO (RESPONSIVO)
        // ==========================================
        private Panel CrearPaso1Documento()
        {
            Panel pnl = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 92, 
                BackColor = Color.White, 
                Padding = new Padding(16, 30, 16, 8), 
                Margin = new Padding(0, 0, 0, 14) 
            };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            lblTagDocumento = new Label
            {
                Text = "📦 DOCUMENTO DE MERCADERÍA",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                BackColor = Color.FromArgb(239, 246, 255),
                AutoSize = true,
                Padding = new Padding(4, 2, 4, 2),
                Location = new Point(16, 8)
            };

            TableLayoutPanel tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F)); // Proveedor
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F)); // Tipo Doc
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F)); // N° Documento
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Fecha
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F)); // Botón Lista Proveedores
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Columna 1: Proveedor
            Panel pnlProv = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
            Label lp = new Label { Text = "1. PROVEEDOR (RUT O NOMBRE)", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            
            Panel pnlProvFila = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(0, 2, 0, 0) };
            txtBuscarProveedor = new TextBox { Text = PLACEHOLDER_PROV, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), Dock = DockStyle.Fill };
            ConfigurarEventosBuscadorProveedor();
            
            FlowLayoutPanel pnlBotonesProv = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
            btnLimpiarBuscadorProv = CrearBotonIcono("🗑️", 0, 0, (s, e) => ResetearBuscadorProveedor());
            btnNuevoProveedor = CrearBotonAccionRapida("➕", Color.FromArgb(2, 132, 199), 0, 0, BtnNuevoProveedor_Click);
            btnEditarProveedor = CrearBotonAccionRapida("✏️", Color.FromArgb(245, 158, 11), 0, 0, BtnEditarProveedor_Click);
            pnlBotonesProv.Controls.AddRange(new Control[] { btnLimpiarBuscadorProv, btnNuevoProveedor, btnEditarProveedor });
            
            pnlProvFila.Controls.Add(txtBuscarProveedor);
            pnlProvFila.Controls.Add(pnlBotonesProv);
            pnlProv.Controls.Add(pnlProvFila);
            pnlProv.Controls.Add(lp);

            // Columnas 2 a 4
            cbTipoDocumento = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbTipoDocumento.Items.AddRange(new string[] { "Factura de Compra", "Factura Exenta", "Boleta de Compra", "Guía de Recepción" });
            cbTipoDocumento.SelectedIndex = 0;
            Panel pnlTipo = EnvolverConLabel("2. TIPO DOC.", cbTipoDocumento);

            txtNroDocumento = new TextBox { Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtNroDocumento.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            Panel pnlFolio = EnvolverConLabel("3. N° DE DOCUMENTO", txtNroDocumento);

            dtpFechaEmision = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            Panel pnlFecha = EnvolverConLabel("4. FECHA EMISIÓN", dtpFechaEmision);

            // Columna 5: Botón Lista Proveedores
            Panel pnlBtnLista = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 0) };
            Label lblEspacioBtn = new Label { Text = " ", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F) };
            Button btnListaProv = new Button { Text = "📖 Lista Proveedores", Dock = DockStyle.Top, Height = 26, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnListaProv.FlatAppearance.BorderSize = 0;
            btnListaProv.Click += BtnListaProveedores_Click;
            pnlBtnLista.Controls.Add(btnListaProv);
            pnlBtnLista.Controls.Add(lblEspacioBtn);

            tabla.Controls.Add(pnlProv, 0, 0);
            tabla.Controls.Add(pnlTipo, 1, 0);
            tabla.Controls.Add(pnlFolio, 2, 0);
            tabla.Controls.Add(pnlFecha, 3, 0);
            tabla.Controls.Add(pnlBtnLista, 4, 0);

            pnl.Controls.Add(tabla);
            pnl.Controls.Add(lblTagDocumento);
            return pnl;
        }

        /// <summary>Envuelve un control con su label superior, ambos dockeados (patrón reutilizado en todo el formulario).</summary>
        private Panel EnvolverConLabel(string texto, Control control)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 6, 0) };
            Label l = new Label { Text = texto, Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            control.Height = 26;
            control.Dock = DockStyle.Top;
            p.Controls.Add(control);
            p.Controls.Add(l);
            return p;
        }

        // ==========================================
        // PASO 2: DETALLE (RESPONSIVO)
        // ==========================================
        private void CrearPaso2Detalle()
        {
            // ---------- Mercadería ----------
            pnlPaso2Mercaderia = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 92, 
                BackColor = Color.White, 
                Padding = new Padding(16, 12, 16, 8), 
                Margin = new Padding(0, 0, 0, 14) 
            };
            pnlPaso2Mercaderia.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlPaso2Mercaderia.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            TableLayoutPanel tablaM = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            tablaM.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F)); // Producto
            tablaM.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13F)); // Cantidad
            tablaM.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F)); // Costo Neto
            tablaM.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Precio Venta
            tablaM.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Botón
            tablaM.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Columna Producto
            Panel pnlProd = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
            Label lpr = new Label { Text = "SELECCIONAR PRODUCTO (CÓDIGO O NOMBRE)", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            
            Panel pnlProdFila = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(0, 2, 0, 0) };
            txtBuscarProducto = new TextBox { Text = PLACEHOLDER_PROD, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), Dock = DockStyle.Fill };
            ConfigurarEventosBuscadorProducto();
            
            FlowLayoutPanel pnlBotonesProd = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
            btnLimpiarBuscadorProd = CrearBotonIcono("🗑️", 0, 0, (s, e) => ResetearBuscadorProducto());
            btnNuevoProducto = CrearBotonAccionRapida("➕", Color.FromArgb(2, 132, 199), 0, 0, BtnNuevoProducto_Click);
            btnEditarProducto = CrearBotonAccionRapida("✏️", Color.FromArgb(245, 158, 11), 0, 0, BtnEditarProducto_Click);
            pnlBotonesProd.Controls.AddRange(new Control[] { btnLimpiarBuscadorProd, btnNuevoProducto, btnEditarProducto });
            
            pnlProdFila.Controls.Add(txtBuscarProducto);
            pnlProdFila.Controls.Add(pnlBotonesProd);
            pnlProd.Controls.Add(pnlProdFila);
            pnlProd.Controls.Add(lpr);

            txtCantidadRecibida = new TextBox { Text = "1", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
            txtCantidadRecibida.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            Panel pnlCant = EnvolverConLabel("CANT. RECIBIDA", txtCantidadRecibida);

            Panel pnlCosto = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 6, 0) };
            Label lcost = new Label { Text = "COSTO NETO ($)", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            txtPrecioCosto = new TextBox { Text = "0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Dock = DockStyle.Top, Height = 24 };
            txtPrecioCosto.TextChanged += (s, e) => CalcularPreviewPrecioVenta();
            lblPreviewPVP = new Label { Text = "PVP Sug: $0", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };
            pnlCosto.Controls.Add(lblPreviewPVP);
            pnlCosto.Controls.Add(txtPrecioCosto);
            pnlCosto.Controls.Add(lcost);

            txtPvpSugerido = new TextBox { Font = new Font("Segoe UI", 9F), PlaceholderText = "Auto-sugerido" };
            Panel pnlPvp = EnvolverConLabel("PRECIO VENTA (IVA INC.)", txtPvpSugerido);

            Panel pnlBtnAdd = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 0) };
            Label lblEspacioBtnAdd = new Label { Text = " ", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F) };
            Button btnAddM = new Button { Text = "+ Agregar", Dock = DockStyle.Top, Height = 26, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddM.FlatAppearance.BorderSize = 0;
            btnAddM.Click += BtnAgregarMercaderia_Click;
            pnlBtnAdd.Controls.Add(btnAddM);
            pnlBtnAdd.Controls.Add(lblEspacioBtnAdd);

            tablaM.Controls.Add(pnlProd, 0, 0);
            tablaM.Controls.Add(pnlCant, 1, 0);
            tablaM.Controls.Add(pnlCosto, 2, 0);
            tablaM.Controls.Add(pnlPvp, 3, 0);
            tablaM.Controls.Add(pnlBtnAdd, 4, 0);

            pnlPaso2Mercaderia.Controls.Add(tablaM);

            // ---------- Gasto Interno ----------
            pnlPaso2Gasto = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 105, 
                BackColor = Color.White, 
                Padding = new Padding(16, 12, 16, 8), 
                Margin = new Padding(0, 0, 0, 14), 
                Visible = false 
            };
            pnlPaso2Gasto.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlPaso2Gasto.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            TableLayoutPanel tablaG = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
            tablaG.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F)); // Descripción
            tablaG.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F)); // Categoría
            tablaG.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F)); // Monto
            tablaG.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F)); // Botón
            tablaG.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tablaG.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            txtDescripcionGasto = new TextBox { Font = new Font("Segoe UI", 9F), PlaceholderText = "Ej: Materiales reparación escritorio recepción" };
            Panel pnlDesc = EnvolverConLabel("DESCRIPCIÓN DEL GASTO", txtDescripcionGasto);

            cbCategoriaGasto = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbCategoriaGasto.Items.AddRange(new string[] { "Mantención de activos", "Materiales de oficina", "Insumos y Aseo", "Servicios Básicos", "Mobiliario y Equipamiento", "Gastos Operacionales Varios" });
            cbCategoriaGasto.SelectedIndex = 0;
            Panel pnlCat = EnvolverConLabel("CATEGORÍA / CUENTA CONTABLE", cbCategoriaGasto);

            txtMontoNetoGasto = new TextBox { Text = "0", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Panel pnlMonto = EnvolverConLabel("MONTO NETO ($)", txtMontoNetoGasto);

            Panel pnlBtnAddG = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 0) };
            Label lblEspacioBtnAddG = new Label { Text = " ", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F) };
            Button btnAddG = new Button { Text = "+ Agregar", Dock = DockStyle.Top, Height = 26, BackColor = Color.FromArgb(124, 58, 237), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddG.FlatAppearance.BorderSize = 0;
            btnAddG.Click += BtnAgregarGasto_Click;
            pnlBtnAddG.Controls.Add(btnAddG);
            pnlBtnAddG.Controls.Add(lblEspacioBtnAddG);

            FlowLayoutPanel pnlCapitaliza = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            chkCapitalizaActivo = new CheckBox { Text = "¿Se capitaliza a un activo fijo existente?", AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237) };
            txtNombreActivoFijo = new TextBox { Width = 200, Font = new Font("Segoe UI", 8.5F), Visible = false, PlaceholderText = "Seleccionar activo...", Margin = new Padding(10, 2, 0, 0) };
            chkCapitalizaActivo.CheckedChanged += (s, e) => txtNombreActivoFijo.Visible = chkCapitalizaActivo.Checked;
            pnlCapitaliza.Controls.Add(chkCapitalizaActivo);
            pnlCapitaliza.Controls.Add(txtNombreActivoFijo);

            tablaG.Controls.Add(pnlDesc, 0, 0);
            tablaG.Controls.Add(pnlCat, 1, 0);
            tablaG.Controls.Add(pnlMonto, 2, 0);
            tablaG.Controls.Add(pnlBtnAddG, 3, 0);
            tablaG.SetColumnSpan(pnlCapitaliza, 4);
            tablaG.Controls.Add(pnlCapitaliza, 0, 1);

            pnlPaso2Gasto.Controls.Add(tablaG);
        }
        private Panel CrearBloqueGrilla()
        {
            Panel pnl = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.White, 
                Padding = new Padding(14, 10, 14, 10), 
                Margin = new Padding(0) 
            };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            // Barra Superior de la Grilla (Título + Contador + Botón Quitar)
            Panel pnlHead = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(0, 0, 0, 4) };
            lblTituloGrilla = new Label { Text = "Productos en esta recepción", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, 4), AutoSize = true };
            lblContadorLineas = new Label { Text = "0 líneas", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), BackColor = Color.FromArgb(241, 245, 249), Location = new Point(220, 5), AutoSize = true, Padding = new Padding(4, 2, 4, 2) };

            btnQuitarItem = new Button
            {
                Text = "🗑️ Quitar Seleccionado",
                Dock = DockStyle.Right,
                Width = 160,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuitarItem.FlatAppearance.BorderSize = 0;
            btnQuitarItem.Click += (s, e) =>
            {
                var filtrados = _detalleFactura.Where(d => _esModoMercaderia ? d.TipoItem == "MERCADERIA" : d.TipoItem == "GASTO").ToList();
                if (dgvDetalle.CurrentRow != null && dgvDetalle.CurrentRow.Index >= 0 && dgvDetalle.CurrentRow.Index < filtrados.Count)
                {
                    _detalleFactura.Remove(filtrados[dgvDetalle.CurrentRow.Index]);
                    RefrescarGrilla();
                }
            };

            pnlHead.Controls.AddRange(new Control[] { lblTituloGrilla, lblContadorLineas, btnQuitarItem });

            // DataGridView Estilo Catálogo Oficial
            dgvDetalle = new DataGridView
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
                RowTemplate = { Height = 32 },
                GridColor = Color.FromArgb(241, 245, 249)
            };

            // Estilo de Encabezados (Oscuro #0F172A fijo que no cambia al hacer clic)
            dgvDetalle.EnableHeadersVisualStyles = false;
            dgvDetalle.ColumnHeadersHeight = 34;
            dgvDetalle.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvDetalle.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvDetalle.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42); // Evita el fondo azul al seleccionar
            dgvDetalle.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvDetalle.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            // Estilo de Filas Seleccionadas (Celeste Suave #E0F2FE con texto visible #0F172A)
            dgvDetalle.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvDetalle.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvDetalle.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

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
            dgvDetalle.Columns.Add("Producto", "Descripción Producto");
            dgvDetalle.Columns.Add("Cant", "Cantidad");
            dgvDetalle.Columns.Add("CostoNeto", "Costo Neto");
            dgvDetalle.Columns.Add("PvpSugerido", "Precio Venta (PVP)");
            dgvDetalle.Columns.Add("Subtotal", "Subtotal Neto");

            // Alineaciones y anchos
            dgvDetalle.Columns["Cant"].Width = 80;
            dgvDetalle.Columns["Cant"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvDetalle.Columns["Cant"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvDetalle.Columns["CostoNeto"].Width = 110;
            dgvDetalle.Columns["CostoNeto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDetalle.Columns["CostoNeto"].DefaultCellStyle.Format = "$#,##0";

            // Precio de venta destacado en verde (como en el catálogo)
            dgvDetalle.Columns["PvpSugerido"].Width = 130;
            dgvDetalle.Columns["PvpSugerido"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDetalle.Columns["PvpSugerido"].DefaultCellStyle.Format = "$#,##0";
            dgvDetalle.Columns["PvpSugerido"].DefaultCellStyle.ForeColor = Color.FromArgb(22, 163, 74);
            dgvDetalle.Columns["PvpSugerido"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvDetalle.Columns["Subtotal"].Width = 110;
            dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0";
        }

        private void ConfigurarColumnasGrillaGasto()
        {
            dgvDetalle.Columns.Clear();
            dgvDetalle.Columns.Add("Descripcion", "Descripción del Gasto");
            dgvDetalle.Columns.Add("Categoria", "Categoría / Cuenta");
            dgvDetalle.Columns.Add("MontoNeto", "Monto Neto");

            dgvDetalle.Columns["Categoria"].Width = 180;
            dgvDetalle.Columns["MontoNeto"].Width = 130;
            dgvDetalle.Columns["MontoNeto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDetalle.Columns["MontoNeto"].DefaultCellStyle.Format = "$#,##0";
            dgvDetalle.Columns["MontoNeto"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
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

        /// <summary>
        /// Ubica una lista de sugerencias flotante justo debajo del cuadro de búsqueda real,
        /// convirtiendo coordenadas de pantalla a coordenadas del contenedor de la lista.
        /// Reemplaza el Point fijo original, que dejaba de coincidir apenas el layout se volvió responsivo.
        /// </summary>
        private void PosicionarListaSugerencias(ListBox lista, Control referenciaCampo)
        {
            if (lista.Parent == null || referenciaCampo.Parent == null) return;
            Point puntoPantalla = referenciaCampo.Parent.PointToScreen(new Point(referenciaCampo.Left, referenciaCampo.Bottom));
            Point puntoLocal = lista.Parent.PointToClient(puntoPantalla);
            lista.Location = puntoLocal;
            lista.Width = Math.Max(referenciaCampo.Width, 260);
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
                    if (f.Count > 0) PosicionarListaSugerencias(lstSugerenciasProveedores, txtBuscarProveedor);
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
                    if (f.Count > 0) PosicionarListaSugerencias(lstSugerenciasProductos, txtBuscarProducto);
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

                // 1. Cargar costo actual registrado
                decimal costo = p.PrecioCosto > 0 ? p.PrecioCosto : (p.PrecioUnitario > 0 ? Math.Round(p.PrecioUnitario / 1.19m / (1 + (p.MargenGanancia / 100m)), 0) : 0);
                txtPrecioCosto.Text = costo.ToString("0");

                // 2. Si el costo no cambia, mantener exactamente el PrecioUnitario (Lista 1) actual; si cambia, recalcular con su margen
                if (p.PrecioUnitario > 0 && costo == p.PrecioCosto)
                {
                    lblPreviewPVP.Text = $"PVP Sug: ${p.PrecioUnitario:N0}";
                    txtPvpSugerido.Text = p.PrecioUnitario.ToString("0");
                }
                else
                {
                    CalcularPreviewPrecioVenta();
                }

                txtCantidadRecibida.Focus();
                txtCantidadRecibida.SelectAll();
            }
        }

        private void CalcularPreviewPrecioVenta()
        {
            if (_productoSeleccionado == null) return;

            if (decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) && costo > 0)
            {
                // Usa el margen real guardado en el producto (ej: 59.7%)
                decimal margen = _productoSeleccionado.MargenGanancia > 0 ? _productoSeleccionado.MargenGanancia : 30m;

                // Misma fórmula oficial de FormConfigurarMargenesModal
                decimal neto = costo * (1 + (margen / 100m));
                decimal bruto = neto * 1.19m;
                decimal pvpSugerido = Math.Round(bruto / 10m, MidpointRounding.AwayFromZero) * 10m;

                lblPreviewPVP.Text = $"PVP Sug: ${pvpSugerido:N0}";
                txtPvpSugerido.Text = pvpSugerido.ToString("0");
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
            Button b = new Button 
            { 
                Text = t, 
                Size = new Size(26, 26), 
                Margin = new Padding(2, 0, 0, 0),
                BackColor = Color.FromArgb(241, 245, 249), 
                FlatStyle = FlatStyle.Flat, 
                Cursor = Cursors.Hand 
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            b.Click += h;
            return b;
        }

        private Button CrearBotonAccionRapida(string t, Color bg, int x, int y, EventHandler h)
        {
            Button b = new Button 
            { 
                Text = t, 
                Size = new Size(26, 26), 
                Margin = new Padding(2, 0, 0, 0),
                BackColor = bg, 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += h;
            return b;
        }
    }
}