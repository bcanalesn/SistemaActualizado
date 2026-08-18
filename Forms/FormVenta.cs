using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public partial class FormVenta : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private static Dictionary<string, List<DetalleCarrito>> _carritosPorVendedor = new Dictionary<string, List<DetalleCarrito>>();
        private static Dictionary<string, (string Nombre, string Rut)> _clientesPorVendedor = new Dictionary<string, (string, string)>();

        private List<Producto> _productosCache = new List<Producto>();
        private ProductoService _productoService = new ProductoService();
        private VentaService _ventaService = new VentaService();

        // Cabecera
        private TextBox txtBuscar = null!;
        private ComboBox cbVendedor = null!;

        // Categorías, Subfamilias y Productos
        private TableLayoutPanel pnlIzquierda = null!;
        private Panel pnlCatSection = null!;
        private FlowLayoutPanel flowCategorias = null!;
        private FlowLayoutPanel flowSubFamilias = null!;
        private FlowLayoutPanel pnlProductosGrid = null!;

        // Carrito Lateral
        private Label lblCantItemsBadge = null!;
        private Button btnLimpiarCarro = null!;
        private FlowLayoutPanel flowCarritoItems = null!;
        private Panel pnlCarritoVacio = null!;

        // Totales y Acciones
        private Label lblSubtotalValue = null!;
        private Label lblDescuentoValue = null!;
        private Label lblTotalValue = null!;
        private Button btnCliente = null!;
        private Button btnGenerarTicket = null!;

        private string _clienteSeleccionadoNombre = "Consumidor Final";
        private string _clienteSeleccionadoRut = "";
        private Usuario? _usuarioActual;
        private static string _vendedorActualNombre = "Bárbara";

        // Filtros Jerárquicos
        private string _categoriaActivaNombre = "Todas";
        private string _familiaActivaNombre = "Todas";

        private readonly List<(Color Fondo, Color Texto, Color Borde, string Icono)> _paletaColores = new List<(Color, Color, Color, string)>
        {
            (Color.FromArgb(240, 249, 255), Color.FromArgb(2, 132, 199), Color.FromArgb(3, 105, 161), "🥛"),
            (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38), Color.FromArgb(185, 28, 28), "🥩"),
            (Color.FromArgb(255, 247, 237), Color.FromArgb(217, 119, 6), Color.FromArgb(180, 83, 9), "🌾"),
            (Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61), "🥤"),
            (Color.FromArgb(250, 245, 255), Color.FromArgb(147, 51, 234), Color.FromArgb(126, 34, 206), "🧹"),
            (Color.FromArgb(255, 247, 237), Color.FromArgb(234, 88, 12), Color.FromArgb(194, 65, 12), "🍟"),
            (Color.FromArgb(254, 242, 242), Color.FromArgb(180, 83, 9), Color.FromArgb(146, 64, 14), "🍞"),
            (Color.FromArgb(236, 254, 255), Color.FromArgb(8, 145, 178), Color.FromArgb(14, 116, 144), "❄️"),
            (Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61), "🍏"),
            (Color.FromArgb(253, 242, 248), Color.FromArgb(219, 39, 119), Color.FromArgb(190, 24, 93), "🐾")
        };

        public FormVenta(Usuario? usuario = null)
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            _usuarioActual = usuario;
            InitializeComponent();

            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                {
                    CargarCategoriasDesdeBD();
                    CargarProductosDesdeBD();

                    this.BeginInvoke(new Action(() =>
                    {
                        if (txtBuscar != null && txtBuscar.CanFocus)
                        {
                            txtBuscar.Focus();
                        }
                    }));
                }
            };

            this.Shown += (s, e) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (txtBuscar != null && txtBuscar.CanFocus)
                    {
                        txtBuscar.Focus();
                    }
                }));
            };

            CargarCategoriasDesdeBD();
            CargarProductosDesdeBD();
            RestaurarCarritoVendedorActual();

            this.KeyPreview = true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // FOOTER DE ATAJOS
            Panel pnlAtajosFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(238, 242, 246),
                Padding = new Padding(12, 6, 12, 6)
            };

            Label lblAtajosInfo = new Label
            {
                Text = "⏱ Atajos de teclado:   [F2] Buscar   |   [F3] Cliente   |   [F4] Generar Ticket   |   [ESC] Limpiar Carro",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlAtajosFooter.Controls.Add(lblAtajosInfo);

            // CONTENEDOR PRINCIPAL
            Panel pnlCuerpo = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 6),
                BackColor = Color.Transparent
            };

            // DERECHA: CARRITO LATERAL
            Panel pnlDerecha = new Panel
            {
                Dock = DockStyle.Right,
                Width = 340,
                BackColor = Color.White,
                Padding = new Padding(14),
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel pnlCartHeader = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White };
            Label lblTituloCarro = new Label { Text = "VENTA ACTUAL", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, 4), AutoSize = true };

            lblCantItemsBadge = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.FromArgb(226, 232, 240),
                Size = new Size(24, 22),
                Location = new Point(120, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnLimpiarCarro = new Button
            {
                Text = "🗑️ Limpiar",
                Dock = DockStyle.Right,
                Width = 82,
                Height = 28,
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiarCarro.FlatAppearance.BorderSize = 0;
            btnLimpiarCarro.Click += (s, e) => LimpiarCarritoActual();

            pnlCartHeader.Controls.AddRange(new Control[] { lblTituloCarro, lblCantItemsBadge, btnLimpiarCarro });

            pnlCarritoVacio = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Label lblCarroVacioIcon = new Label { Text = "🛒", Font = new Font("Segoe UI", 36F), ForeColor = Color.FromArgb(203, 213, 225), Dock = DockStyle.Top, Height = 90, TextAlign = ContentAlignment.BottomCenter };
            Label lblCarroVacioTit = new Label { Text = "Aún no hay productos en el carrito", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter };
            Label lblCarroVacioSub = new Label { Text = "Seleccione productos para comenzar\nla venta", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(148, 163, 184), Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.TopCenter };
            pnlCarritoVacio.Controls.AddRange(new Control[] { lblCarroVacioSub, lblCarroVacioTit, lblCarroVacioIcon });

            flowCarritoItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(2),
                Visible = false
            };
            flowCarritoItems.SizeChanged += (s, e) => ReajustarAnchoFilasCarrito();

            Panel pnlBottomCheckout = new Panel { Dock = DockStyle.Bottom, Height = 160, Padding = new Padding(0, 6, 0, 0) };
            Panel pnlDesgloseTotales = new Panel { Dock = DockStyle.Top, Height = 85, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(10) };

            Label lblSubtotalCap = new Label { Text = "Subtotal", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(6, 6), AutoSize = true };
            lblSubtotalValue = new Label { Text = "$ 0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(210, 6), AutoSize = true };

            Label lblDescCap = new Label { Text = "Descuento", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(6, 26), AutoSize = true };
            lblDescuentoValue = new Label { Text = "$ 0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(210, 26), AutoSize = true };

            Label lblTotalCap = new Label { Text = "TOTAL", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(6, 50), AutoSize = true };
            lblTotalValue = new Label { Text = "$ 0", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Location = new Point(170, 44), AutoSize = true };

            pnlDesgloseTotales.Controls.AddRange(new Control[] { lblSubtotalCap, lblSubtotalValue, lblDescCap, lblDescuentoValue, lblTotalCap, lblTotalValue });

            Panel pnlBotonesAccion = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 6, 0, 0) };

            btnCliente = new Button
            {
                Text = "👤 Consumidor",
                Location = new Point(0, 6),
                Size = new Size(108, 48),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCliente.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCliente.Click += BtnCliente_Click;

            btnGenerarTicket = new Button
            {
                Text = "🎟️ F4 GENERAR TICKET",
                Location = new Point(114, 6),
                Size = new Size(196, 48),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerarTicket.FlatAppearance.BorderSize = 0;
            btnGenerarTicket.Click += BtnGenerarTicket_Click;

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnCliente, btnGenerarTicket });
            pnlBottomCheckout.Controls.AddRange(new Control[] { pnlBotonesAccion, pnlDesgloseTotales });

            pnlDerecha.Controls.AddRange(new Control[] { flowCarritoItems, pnlCarritoVacio, pnlBottomCheckout, pnlCartHeader });

            // IZQUIERDA: TABLELAYOUTPANEL DINÁMICO
            pnlIzquierda = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0, 0, 14, 0),
                BackColor = Color.Transparent
            };
            pnlIzquierda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlIzquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));  // Fila 0: Buscador
            pnlIzquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));  // Fila 1: Categorías (se recalcula dinámicamente)
            pnlIzquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Fila 2: Productos

            // 1. Buscador y Vendedor
            Panel pnlTopBar = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.Transparent, 
                Margin = new Padding(0, 0, 0, 4) 
            };

            Panel pnlVendedor = new Panel { Dock = DockStyle.Right, Width = 220, Height = 38, BackColor = Color.Transparent };
            Label lblVendIcon = new Label { Text = "👤", Location = new Point(2, 7), AutoSize = true, Font = new Font("Segoe UI", 11F) };

            cbVendedor = new ComboBox
            {
                Location = new Point(32, 5),
                Size = new Size(180, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cbVendedor.Items.AddRange(new string[] { "Bárbara", "Víctor", "Juan", "María" });
            if (!cbVendedor.Items.Contains(_vendedorActualNombre)) _vendedorActualNombre = _usuarioActual?.NombreCompleto ?? "Bárbara";
            cbVendedor.SelectedItem = cbVendedor.Items.Contains(_vendedorActualNombre) ? _vendedorActualNombre : "Bárbara";
            cbVendedor.SelectedIndexChanged += CbVendedor_SelectedIndexChanged;

            pnlVendedor.Controls.AddRange(new Control[] { lblVendIcon, cbVendedor });

            Panel pnlBusquedaBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                Height = 34,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblSearchIcon = new Label { Text = "🔍", Location = new Point(8, 7), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };

            Label lblF2Badge = new Label
            {
                Text = "F2",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(241, 245, 249),
                Size = new Size(26, 20),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };

            txtBuscar = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            txtBuscar.HandleCreated += (s, e) =>
            {
                SendMessage(txtBuscar.Handle, EM_SETCUEBANNER, 1, "Buscar producto por nombre o código de barra...");
            };

            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BuscarYAgregarProducto(); e.SuppressKeyPress = true; } };
            txtBuscar.TextChanged += (s, e) => { FiltrarProductosPorBusquedaInteligente(); };

            Panel pnlTxtContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(32, 6, 32, 0) };
            pnlTxtContainer.Controls.Add(txtBuscar);

            pnlBusquedaBox.Controls.Add(pnlTxtContainer);
            pnlBusquedaBox.Controls.Add(lblSearchIcon);
            pnlBusquedaBox.Controls.Add(lblF2Badge);

            pnlTopBar.Controls.Add(pnlBusquedaBox);
            pnlTopBar.Controls.Add(pnlVendedor);

            // 2. Sección Categorías y Subfamilias
            pnlCatSection = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6)
            };

            Label lblCatTitle = new Label 
            { 
                Text = "CATEGORÍAS", 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Dock = DockStyle.Top, 
                Height = 18 
            };

            flowCategorias = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                WrapContents = true,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            flowSubFamilias = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 4),
                Margin = new Padding(0),
                Visible = false
            };

            pnlCatSection.Controls.Add(flowSubFamilias);
            pnlCatSection.Controls.Add(flowCategorias);
            pnlCatSection.Controls.Add(lblCatTitle);

            // 3. Grid de Productos (Fila inferior)
            Panel pnlProdMain = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.White, 
                Padding = new Padding(12),
                Margin = new Padding(0)
            };

            Label lblProdHeader = new Label 
            { 
                Text = "PRODUCTOS", 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Dock = DockStyle.Top, 
                Height = 24 
            };

            pnlProductosGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 4)
            };

            pnlProdMain.Controls.Add(pnlProductosGrid);
            pnlProdMain.Controls.Add(lblProdHeader);

            pnlIzquierda.Controls.Add(pnlTopBar, 0, 0);
            pnlIzquierda.Controls.Add(pnlCatSection, 0, 1);
            pnlIzquierda.Controls.Add(pnlProdMain, 0, 2);

            pnlIzquierda.SizeChanged += (s, e) => AjustarAnchoYAlturaCategorias();

            pnlCuerpo.Controls.Add(pnlIzquierda);
            pnlCuerpo.Controls.Add(pnlDerecha);

            this.Controls.Add(pnlCuerpo);
            this.Controls.Add(pnlAtajosFooter);
            this.ResumeLayout(false);

            this.Load += (s, e) => AjustarAnchoYAlturaCategorias();
        }

        private void CargarCategoriasDesdeBD()
        {
            flowCategorias.Controls.Clear();

            // Botón Todas
            flowCategorias.Controls.Add(CrearBotonCategoria(
                "Todas", "▦",
                Color.FromArgb(239, 246, 255),
                Color.FromArgb(37, 99, 235),
                Color.FromArgb(29, 78, 216),
                _categoriaActivaNombre == "Todas"
            ));

            var categoriasBD = _productoService.ObtenerCategoriasRegistradas();

            int colorIndex = 0;
            foreach (var cat in categoriasBD)
            {
                if (cat.Equals("Todas", StringComparison.OrdinalIgnoreCase)) continue;

                var estilo = _paletaColores[colorIndex % _paletaColores.Count];
                bool seleccionada = _categoriaActivaNombre.Equals(cat, StringComparison.OrdinalIgnoreCase);

                flowCategorias.Controls.Add(CrearBotonCategoria(
                    cat, estilo.Icono,
                    estilo.Fondo, estilo.Texto, estilo.Borde,
                    seleccionada
                ));

                colorIndex++;
            }

            AjustarAnchoYAlturaCategorias();
            ActualizarSubFamiliasUI();
        }

        private void ActualizarSubFamiliasUI()
        {
            flowSubFamilias.Controls.Clear();

            if (_categoriaActivaNombre == "Todas")
            {
                flowSubFamilias.Visible = false;
                AjustarAnchoYAlturaCategorias();
                return;
            }

            var familiasDeCategoria = _productoService.ObtenerFamiliasPorCategoria(_categoriaActivaNombre);

            if (familiasDeCategoria.Count == 0)
            {
                flowSubFamilias.Visible = false;
                AjustarAnchoYAlturaCategorias();
                return;
            }

            flowSubFamilias.Visible = true;

            Button btnTodasFam = CrearBotonSubFamilia("Todas", _familiaActivaNombre == "Todas");
            flowSubFamilias.Controls.Add(btnTodasFam);

            foreach (var fam in familiasDeCategoria)
            {
                Button btnFam = CrearBotonSubFamilia(fam, _familiaActivaNombre == fam);
                flowSubFamilias.Controls.Add(btnFam);
            }

            AjustarAnchoYAlturaCategorias();
        }

        private Button CrearBotonSubFamilia(string nombreFamilia, bool seleccionada)
        {
            Button btn = new Button
            {
                Text = nombreFamilia,
                Height = 26,
                AutoSize = true,
                BackColor = seleccionada ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249),
                ForeColor = seleccionada ? Color.White : Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 4),
                Padding = new Padding(8, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                _familiaActivaNombre = nombreFamilia;
                ActualizarSubFamiliasUI();
                FiltrarProductosPorJerarquia();
            };

            return btn;
        }

        private void AjustarAnchoYAlturaCategorias()
        {
            if (flowCategorias == null || pnlIzquierda == null || pnlIzquierda.ClientSize.Width < 200) return;

            int anchoDisponible = pnlIzquierda.ClientSize.Width - 20;
            int columnas = Math.Max(1, anchoDisponible / 130);
            int anchoBoton = (anchoDisponible / columnas) - 6;

            foreach (Control c in flowCategorias.Controls)
            {
                if (c is Button b)
                {
                    b.Width = Math.Max(100, anchoBoton);
                    b.Height = 36;
                }
            }

            // Cálculo físico de filas
            int cantBotones = flowCategorias.Controls.Count;
            int filas = (int)Math.Ceiling((double)cantBotones / columnas);
            if (filas < 1) filas = 1;

            int altoCategorias = filas * 42;
            flowCategorias.Height = altoCategorias;

            int altoSubFamilias = (flowSubFamilias.Visible && flowSubFamilias.Controls.Count > 0) ? 34 : 0;
            flowSubFamilias.Height = altoSubFamilias;

            int altoTotalSeccion = 20 + altoCategorias + altoSubFamilias + 8;
            pnlCatSection.Height = altoTotalSeccion;

            // Modificar la altura de la fila en el TableLayoutPanel para empujar Productos
            if (pnlIzquierda.RowStyles.Count > 1)
            {
                pnlIzquierda.RowStyles[1].SizeType = SizeType.Absolute;
                pnlIzquierda.RowStyles[1].Height = altoTotalSeccion;
            }
        }

        private Button CrearBotonCategoria(string nombreCategoria, string icono, Color back, Color fore, Color colorBordeFuerte, bool seleccionada)
        {
            Button btn = new Button
            {
                Name = "cat_" + nombreCategoria.Replace(" ", "_"),
                Text = $"{icono} {nombreCategoria}",
                Height = 36,
                Width = 120,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 6),
                Tag = new CategoriaVisualInfo
                {
                    Nombre = nombreCategoria,
                    Fondo = back,
                    Texto = fore,
                    Borde = colorBordeFuerte
                }
            };

            btn.FlatAppearance.BorderSize = seleccionada ? 2 : 1;
            btn.FlatAppearance.BorderColor = seleccionada ? colorBordeFuerte : Color.FromArgb(226, 232, 240);

            btn.Click += (s, e) =>
            {
                foreach (Button b in flowCategorias.Controls.OfType<Button>())
                {
                    if (b.Tag is CategoriaVisualInfo info)
                    {
                        b.BackColor = info.Fondo;
                        b.ForeColor = info.Texto;
                        b.FlatAppearance.BorderSize = 1;
                        b.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                    }
                }

                if (btn.Tag is CategoriaVisualInfo seleccion)
                {
                    btn.FlatAppearance.BorderSize = 2;
                    btn.FlatAppearance.BorderColor = seleccion.Borde;
                    _categoriaActivaNombre = seleccion.Nombre;
                    _familiaActivaNombre = "Todas";
                    ActualizarSubFamiliasUI();
                    FiltrarProductosPorJerarquia();
                }
            };

            return btn;
        }

        private sealed class CategoriaVisualInfo
        {
            public string Nombre { get; set; } = "";
            public Color Fondo { get; set; }
            public Color Texto { get; set; }
            public Color Borde { get; set; }
        }

        private void CargarProductosDesdeBD()
        {
            _productosCache = _productoService.ObtenerProductosActivos();

            // Restar stock temporal en carritos de vendedores
            foreach (var carritoVendedor in _carritosPorVendedor.Values)
            {
                foreach (var item in carritoVendedor)
                {
                    var prod = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prod != null)
                    {
                        prod.Stock -= item.Cantidad;
                        if (prod.Stock < 0) prod.Stock = 0;
                    }
                }
            }

            FiltrarProductosPorJerarquia();
        }

        private void FiltrarProductosPorJerarquia()
        {
            pnlProductosGrid.Controls.Clear();

            var filtrados = _productosCache.AsQueryable();

            if (_categoriaActivaNombre != "Todas")
            {
                filtrados = filtrados.Where(p => p.Categoria == _categoriaActivaNombre);
            }

            if (_familiaActivaNombre != "Todas")
            {
                filtrados = filtrados.Where(p => p.NFamilia == _familiaActivaNombre);
            }

            var lista = filtrados.ToList();

            foreach (var prod in lista)
            {
                pnlProductosGrid.Controls.Add(CrearTarjetaProductoUI(prod));
            }
        }

        private Panel CrearTarjetaProductoUI(Producto prod)
        {
            Panel card = new Panel
            {
                Size = new Size(130, 155),
                BackColor = Color.White,
                Margin = new Padding(6),
                Cursor = Cursors.Hand
            };

            Control ctrlImagen;
            if (!string.IsNullOrEmpty(prod.ImagenPath) && System.IO.File.Exists(prod.ImagenPath))
            {
                PictureBox pb = new PictureBox
                {
                    Location = new Point(40, 6),
                    Size = new Size(50, 50),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                try { pb.Image = Image.FromFile(prod.ImagenPath); } catch { }
                ctrlImagen = pb;
            }
            else
            {
                ctrlImagen = new Label
                {
                    Text = "📦",
                    Font = new Font("Segoe UI", 20F),
                    Location = new Point(42, 4),
                    Size = new Size(46, 46),
                    TextAlign = ContentAlignment.MiddleCenter
                };
            }

            Label lblNombre = new Label
            {
                Text = prod.Nombre,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(4, 56),
                Size = new Size(122, 30),
                TextAlign = ContentAlignment.TopCenter
            };

            Label lblPrecio = new Label
            {
                Text = $"${prod.PrecioUnitario:N0}",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 255),
                Location = new Point(4, 88),
                Size = new Size(122, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblCodigo = new Label
            {
                Text = $"Cód. {prod.CodigoBarra}",
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(4, 110),
                Size = new Size(122, 16),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Color stockColor = prod.Stock > 10 ? Color.FromArgb(16, 185, 129) : (prod.Stock > 0 ? Color.FromArgb(245, 158, 11) : Color.FromArgb(239, 68, 68));
            Label lblStock = new Label
            {
                Text = $"Stock: {prod.Stock} un.",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = stockColor,
                Location = new Point(4, 128),
                Size = new Size(122, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Action onClick = () => SolicitarCantidadYAgregar(prod);

            card.Click += (s, e) => onClick();
            ctrlImagen.Click += (s, e) => onClick();
            lblNombre.Click += (s, e) => onClick();
            lblPrecio.Click += (s, e) => onClick();
            lblCodigo.Click += (s, e) => onClick();
            lblStock.Click += (s, e) => onClick();

            card.Controls.AddRange(new Control[] { ctrlImagen, lblNombre, lblPrecio, lblCodigo, lblStock });
            return card;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F2)
            {
                txtBuscar.Focus();
                txtBuscar.SelectAll();
                return true;
            }
            else if (keyData == Keys.F3)
            {
                BtnCliente_Click(this, EventArgs.Empty);
                return true;
            }
            else if (keyData == Keys.F4)
            {
                BtnGenerarTicket_Click(this, EventArgs.Empty);
                return true;
            }
            else if (keyData == Keys.Escape)
            {
                LimpiarCarritoActual();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FiltrarProductosPorBusquedaInteligente()
        {
            string query = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(query) || query.StartsWith("Buscar"))
            {
                FiltrarProductosPorJerarquia();
                return;
            }

            pnlProductosGrid.Controls.Clear();

            var queryJerarquica = _productosCache.AsQueryable();

            if (_categoriaActivaNombre != "Todas")
            {
                queryJerarquica = queryJerarquica.Where(p => p.Categoria == _categoriaActivaNombre);
            }

            if (_familiaActivaNombre != "Todas")
            {
                queryJerarquica = queryJerarquica.Where(p => p.NFamilia == _familiaActivaNombre);
            }

            var filtrados = queryJerarquica.Where(p =>
                (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(palabra => palabra.StartsWith(query, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(p.CodigoBarra) && p.CodigoBarra.StartsWith(query, StringComparison.OrdinalIgnoreCase)) ||
                p.ProductoID.ToString().StartsWith(query)
            ).ToList();

            foreach (var prod in filtrados)
            {
                pnlProductosGrid.Controls.Add(CrearTarjetaProductoUI(prod));
            }
        }

        private List<DetalleCarrito> ObtenerCarritoActivo()
        {
            string vendor = cbVendedor.SelectedItem?.ToString() ?? _vendedorActualNombre;
            if (!_carritosPorVendedor.ContainsKey(vendor)) _carritosPorVendedor[vendor] = new List<DetalleCarrito>();
            return _carritosPorVendedor[vendor];
        }

        private void CbVendedor_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _vendedorActualNombre = cbVendedor.SelectedItem?.ToString() ?? "Bárbara";

            if (_clientesPorVendedor.ContainsKey(_vendedorActualNombre))
            {
                var info = _clientesPorVendedor[_vendedorActualNombre];
                _clienteSeleccionadoNombre = info.Nombre;
                _clienteSeleccionadoRut = info.Rut;
            }
            else
            {
                _clienteSeleccionadoNombre = "Consumidor Final";
                _clienteSeleccionadoRut = "";
            }

            btnCliente.Text = $"👤 {_clienteSeleccionadoNombre.Split(' ')[0]}";
            ActualizarCarritoUI();
        }

        private void RestaurarCarritoVendedorActual()
        {
            ActualizarCarritoUI();
        }

        private void BuscarYAgregarProducto()
        {
            string query = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            var queryJerarquica = _productosCache.AsQueryable();

            if (_categoriaActivaNombre != "Todas")
            {
                queryJerarquica = queryJerarquica.Where(p => p.Categoria == _categoriaActivaNombre);
            }

            if (_familiaActivaNombre != "Todas")
            {
                queryJerarquica = queryJerarquica.Where(p => p.NFamilia == _familiaActivaNombre);
            }

            var prod = queryJerarquica.FirstOrDefault(p => 
                p.CodigoBarra.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => w.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
            );

            if (prod != null)
            {
                txtBuscar.Clear();
                SolicitarCantidadYAgregar(prod);
            }
            else
            {
                string ubicacion = _categoriaActivaNombre == "Todas" ? "el catálogo" : $"la categoría '{_categoriaActivaNombre}'";
                if (_familiaActivaNombre != "Todas") ubicacion += $" y familia '{_familiaActivaNombre}'";

                MessageBox.Show($"No se encontró el producto '{query}' en {ubicacion}.", "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscar.SelectAll();
            }
        }

        private void SolicitarCantidadYAgregar(Producto prod)
        {
            if (prod.Stock <= 0)
            {
                MessageBox.Show($"El producto '{prod.Nombre}' no tiene stock disponible.", "Sin Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cart = ObtenerCarritoActivo();
            var itemExistente = cart.FirstOrDefault(c => c.ProductoID == prod.ProductoID);
            int cantidadInicial = itemExistente != null ? itemExistente.Cantidad : 1;

            using (FormCantidadModal modal = new FormCantidadModal(prod.Nombre, prod.PrecioUnitario, cantidadInicial, prod.Stock, prod.ImagenPath))
            {
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    int nuevaCantidad = modal.CantidadSeleccionada;

                    if (nuevaCantidad > prod.Stock)
                    {
                        MessageBox.Show($"Stock insuficiente. Solo hay {prod.Stock} unidades disponibles.", "Stock Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nuevaCantidad = prod.Stock;
                    }

                    if (nuevaCantidad <= 0)
                    {
                        if (itemExistente != null) cart.Remove(itemExistente);
                    }
                    else
                    {
                        decimal precioAplicado = _productoService.ObtenerPrecioSegunCantidad(prod.ProductoID, nuevaCantidad, prod.PrecioUnitario);

                        if (itemExistente != null)
                        {
                            itemExistente.Cantidad = nuevaCantidad;
                            itemExistente.PrecioUnitario = precioAplicado;
                        }
                        else
                        {
                            cart.Add(new DetalleCarrito
                            {
                                ProductoID = prod.ProductoID,
                                Nombre = prod.Nombre,
                                PrecioUnitario = precioAplicado,
                                Cantidad = nuevaCantidad
                            });
                        }
                    }

                    ActualizarCarritoUI();
                }
            }
        }

        private void ActualizarCarritoUI()
        {
            CargarProductosDesdeBD();

            var cart = ObtenerCarritoActivo();
            flowCarritoItems.Controls.Clear();

            if (cart.Count == 0)
            {
                pnlCarritoVacio.Visible = true;
                flowCarritoItems.Visible = false;
            }
            else
            {
                pnlCarritoVacio.Visible = false;
                flowCarritoItems.Visible = true;
            }

            int totalItemsCount = 0;

            foreach (var item in cart)
            {
                totalItemsCount += item.Cantidad;

                Panel row = new Panel
                {
                    Height = 50,
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, 4),
                    Cursor = Cursors.Hand
                };

                Label lblNombre = new Label { Name = "lblNombre", Text = item.Nombre, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(6, 4), AutoSize = false, Size = new Size(110, 18) };
                Label lblPrecioU = new Label { Name = "lblPrecioU", Text = $"${item.PrecioUnitario:N0}", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(6, 24), AutoSize = true };

                var prodOriginal = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);

                Button btnRestar = new Button { Name = "btnRestar", Text = "-", Size = new Size(22, 22), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnRestar.FlatAppearance.BorderSize = 0;
                btnRestar.Click += (s, e) =>
                {
                    item.Cantidad--;
                    if (item.Cantidad <= 0) cart.Remove(item);
                    ActualizarCarritoUI();
                };

                Label lblQty = new Label { Name = "lblQty", Text = item.Cantidad.ToString(), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Size = new Size(22, 18), TextAlign = ContentAlignment.MiddleCenter };

                Button btnSumar = new Button { Name = "btnSumar", Text = "+", Size = new Size(22, 22), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnSumar.FlatAppearance.BorderSize = 0;
                btnSumar.Click += (s, e) =>
                {
                    if (prodOriginal != null && prodOriginal.Stock <= 0)
                    {
                        MessageBox.Show($"No hay más stock disponible de '{item.Nombre}'.", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    item.Cantidad++;
                    ActualizarCarritoUI();
                };

                Label lblSubtotal = new Label { Name = "lblSubtotal", Text = $"${item.Subtotal:N0}", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), AutoSize = true };

                Button btnDeleteRow = new Button { Name = "btnDeleteRow", Text = "✕", Size = new Size(20, 20), FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(156, 163, 175), Cursor = Cursors.Hand };
                btnDeleteRow.FlatAppearance.BorderSize = 0;
                btnDeleteRow.Click += (s, e) =>
                {
                    cart.Remove(item);
                    ActualizarCarritoUI();
                };

                row.Controls.AddRange(new Control[] { lblNombre, lblPrecioU, btnRestar, lblQty, btnSumar, lblSubtotal, btnDeleteRow });
                flowCarritoItems.Controls.Add(row);
            }

            lblCantItemsBadge.Text = totalItemsCount.ToString();

            decimal subtotal = cart.Sum(c => c.Subtotal);
            lblSubtotalValue.Text = $"$ {subtotal:N0}";
            lblDescuentoValue.Text = "$ 0";
            lblTotalValue.Text = $"$ {subtotal:N0}";

            ReajustarAnchoFilasCarrito();
        }

        private void ReajustarAnchoFilasCarrito()
        {
            int anchoContenedor = flowCarritoItems.ClientSize.Width > 180 ? flowCarritoItems.ClientSize.Width - 6 : 280;

            foreach (Control ctrl in flowCarritoItems.Controls)
            {
                if (ctrl is Panel row)
                {
                    row.Width = anchoContenedor;

                    var btnDeleteRow = row.Controls.Find("btnDeleteRow", false).FirstOrDefault();
                    var lblSubtotal = row.Controls.Find("lblSubtotal", false).FirstOrDefault();
                    var btnSumar = row.Controls.Find("btnSumar", false).FirstOrDefault();
                    var lblQty = row.Controls.Find("lblQty", false).FirstOrDefault();
                    var btnRestar = row.Controls.Find("btnRestar", false).FirstOrDefault();
                    var lblNombre = row.Controls.Find("lblNombre", false).FirstOrDefault();

                    if (btnDeleteRow != null) btnDeleteRow.Location = new Point(anchoContenedor - 24, 14);
                    if (lblSubtotal != null) lblSubtotal.Location = new Point(anchoContenedor - 95, 16);
                    if (btnSumar != null) btnSumar.Location = new Point(anchoContenedor - 122, 14);
                    if (lblQty != null) lblQty.Location = new Point(anchoContenedor - 146, 16);
                    if (btnRestar != null) btnRestar.Location = new Point(anchoContenedor - 170, 14);
                    if (lblNombre != null) lblNombre.Width = Math.Max(50, anchoContenedor - 178);
                }
            }
        }

        private void LimpiarCarritoActual()
        {
            var cart = ObtenerCarritoActivo();
            cart.Clear();

            flowCarritoItems.Controls.Clear();
            pnlCarritoVacio.Visible = true;
            flowCarritoItems.Visible = false;

            lblCantItemsBadge.Text = "0";
            lblSubtotalValue.Text = "$ 0";
            lblDescuentoValue.Text = "$ 0";
            lblTotalValue.Text = "$ 0";

            CargarProductosDesdeBD();
        }

        private void BtnCliente_Click(object? sender, EventArgs e)
        {
            using (FormSeleccionarClienteModal modalCliente = new FormSeleccionarClienteModal(_clienteSeleccionadoRut))
            {
                if (modalCliente.ShowDialog(this) == DialogResult.OK)
                {
                    _clienteSeleccionadoNombre = modalCliente.NombreCliente;
                    _clienteSeleccionadoRut = modalCliente.RutCliente;
                    _clientesPorVendedor[_vendedorActualNombre] = (_clienteSeleccionadoNombre, _clienteSeleccionadoRut);
                    btnCliente.Text = $"👤 {modalCliente.NombreCliente.Split(' ')[0]}";
                }
            }
        }

        private void BtnGenerarTicket_Click(object? sender, EventArgs e)
        {
            var cart = ObtenerCarritoActivo();
            if (cart.Count == 0)
            {
                MessageBox.Show("El carro de compras está vacío.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string vendedorNombre = cbVendedor.SelectedItem?.ToString() ?? _vendedorActualNombre;
            decimal total = cart.Sum(c => c.Subtotal);
            var cartCopia = cart.ToList();

            try
            {
                int nroTicket = _ventaService.GenerarTicketVenta(cartCopia, vendedorNombre, _clienteSeleccionadoNombre, _clienteSeleccionadoRut);
                LimpiarCarritoActual();

                FormPreVentaModal modalPreVenta = new FormPreVentaModal(nroTicket, vendedorNombre, _clienteSeleccionadoNombre, total, cartCopia);
                modalPreVenta.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar Ticket de Atención: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}