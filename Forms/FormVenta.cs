using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
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
        private readonly UsuarioService _usuarioService = new UsuarioService();

        private static Dictionary<string, List<DetalleCarrito>> _carritosPorVendedor = new Dictionary<string, List<DetalleCarrito>>();
        private static Dictionary<string, Cliente?> _clientesPorVendedor = new Dictionary<string, Cliente?>();
        private Cliente? _clienteActual = null;
        private int _listaClienteActivo = 1;

        private List<Producto> _productosCache = new List<Producto>();
        private ProductoService _productoService = new ProductoService();
        private VentaService _ventaService = new VentaService();

        // Control de Venta en Espera / Edición
        private int _idTveEnEdicion = 0;

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
        private FlowLayoutPanel flowCarritoItems = null!;
        private Panel pnlCarritoVacio = null!;

        // Totales y Acciones
        private Label lblSubtotalValue = null!;
        private Label lblDescuentoValue = null!;
        private Label lblTotalValue = null!;
        private Button btnCliente = null!;

        private string _clienteSeleccionadoNombre = "Consumidor Final";
        private string _clienteSeleccionadoRut = "";
        private Usuario? _usuarioActual;
        private static string _vendedorActualNombre = "Bárbara";

        // Filtros Jerárquicos
        private string _categoriaActivaNombre = "Todas";
        private string _familiaActivaNombre = "Todas";

        private readonly List<(Color Fondo, Color Texto, Color Borde)> _paletaColores = new List<(Color, Color, Color)>
        {
            // 1. Azul Cielo
            (Color.FromArgb(240, 249, 255), Color.FromArgb(2, 132, 199), Color.FromArgb(3, 105, 161)),
            // 2. Rojo Carmesí
            (Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38), Color.FromArgb(185, 28, 28)),
            // 3. Ámbar / Dorado
            (Color.FromArgb(255, 251, 235), Color.FromArgb(217, 119, 6), Color.FromArgb(180, 83, 9)),
            // 4. Verde Esmeralda
            (Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61)),
            // 5. Púrpura Intenso
            (Color.FromArgb(250, 245, 255), Color.FromArgb(147, 51, 234), Color.FromArgb(126, 34, 206)),
            // 6. Naranja Coral
            (Color.FromArgb(255, 247, 237), Color.FromArgb(234, 88, 12), Color.FromArgb(194, 65, 12)),
            // 7. Marrón Cálido
            (Color.FromArgb(254, 242, 242), Color.FromArgb(180, 83, 9), Color.FromArgb(146, 64, 14)),
            // 8. Cian / Turquesa
            (Color.FromArgb(236, 254, 255), Color.FromArgb(8, 145, 178), Color.FromArgb(14, 116, 144)),
            // 9. Verde Lima
            (Color.FromArgb(247, 254, 231), Color.FromArgb(101, 163, 13), Color.FromArgb(77, 124, 15)),
            // 10. Rosa Fucsia
            (Color.FromArgb(253, 242, 248), Color.FromArgb(219, 39, 119), Color.FromArgb(190, 24, 93)),
            // 11. Índigo Profundo
            (Color.FromArgb(238, 242, 255), Color.FromArgb(79, 70, 229), Color.FromArgb(67, 56, 202)),
            // 12. Verde Teal
            (Color.FromArgb(240, 253, 250), Color.FromArgb(13, 148, 136), Color.FromArgb(15, 118, 110)),
            // 13. Violeta Pastel
            (Color.FromArgb(245, 243, 255), Color.FromArgb(124, 58, 237), Color.FromArgb(109, 40, 217)),
            // 14. Rosa Palo / Salmón
            (Color.FromArgb(255, 241, 242), Color.FromArgb(225, 29, 72), Color.FromArgb(190, 18, 60)),
            // 15. Amarillo Ocre
            (Color.FromArgb(254, 252, 232), Color.FromArgb(202, 138, 4), Color.FromArgb(161, 98, 7)),
            // 16. Azul Marino Claro
            (Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216)),
            // 17. Menta Suave
            (Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105), Color.FromArgb(4, 120, 87)),
            // 18. Pizarra / Gris Elegante
            (Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105), Color.FromArgb(51, 65, 85)),
            // 19. Magenta
            (Color.FromArgb(253, 244, 255), Color.FromArgb(192, 38, 211), Color.FromArgb(162, 28, 175)),
            // 20. Cobre Metálico
            (Color.FromArgb(255, 248, 240), Color.FromArgb(194, 65, 12), Color.FromArgb(154, 52, 18))
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
                    CargarListaVendedoresBD();
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
            ActualizarCarritoUI();

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

            Button btnLimpiarCarro = new Button
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

            Button btnGenerarTicket = new Button
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
            pnlIzquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            pnlIzquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            pnlIzquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 1. Buscador, Vendedor y Botón de Espera
            Panel pnlTopBar = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.Transparent, 
                Margin = new Padding(0, 0, 0, 4) 
            };

            FlowLayoutPanel flpAccionesSuperiores = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            Label lblVendIcon = new Label 
            { 
                Text = "👤", 
                Size = new Size(20, 28), 
                Font = new Font("Segoe UI", 10F), 
                TextAlign = ContentAlignment.MiddleCenter, 
                Margin = new Padding(0, 4, 2, 0) 
            };

            cbVendedor = new ComboBox
            {
                Size = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(0, 4, 6, 0)
            };
            CargarListaVendedoresBD();
            cbVendedor.SelectedIndexChanged += CbVendedor_SelectedIndexChanged;

            Button btnVerMisTickets = new Button
            {
                Text = "📋 Tickets",
                Size = new Size(82, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 4, 0)
            };
            btnVerMisTickets.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnVerMisTickets.Click += (s, e) =>
            {
                string vendedor = cbVendedor.SelectedItem?.ToString() ?? _vendedorActualNombre;
                var listaVend = cbVendedor.Items.Cast<string>().ToList();

                using var modalHistorial = new FormHistorialTicketsModal(vendedor, listaVend);
                modalHistorial.ShowDialog(this);
            };

            Button btnVentasEnEspera = new Button
            {
                Text = "⏸️ Ventas en Espera",
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(254, 243, 199),
                ForeColor = Color.FromArgb(180, 83, 9),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 0, 0)
            };
            btnVentasEnEspera.FlatAppearance.BorderColor = Color.FromArgb(253, 230, 138);
            btnVentasEnEspera.Click += BtnVentasEnEspera_Click;

            flpAccionesSuperiores.Controls.AddRange(new Control[] { lblVendIcon, cbVendedor, btnVerMisTickets, btnVentasEnEspera });

            Panel pnlBusquedaBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                Height = 34,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblSearchIcon = new Label { Text = "🔍", Location = new Point(6, 7), AutoSize = true, Font = new Font("Segoe UI", 9F) };

            Label lblF2Badge = new Label
            {
                Text = "F2",
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(241, 245, 249),
                Size = new Size(24, 18),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };

            txtBuscar = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            txtBuscar.HandleCreated += (s, e) =>
            {
                SendMessage(txtBuscar.Handle, EM_SETCUEBANNER, 1, "Buscar producto...");
            };

            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BuscarYAgregarProducto(); e.SuppressKeyPress = true; } };
            txtBuscar.TextChanged += (s, e) => { FiltrarProductosPorBusquedaInteligente(); };

            Panel pnlTxtContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 6, 28, 0) };
            pnlTxtContainer.Controls.Add(txtBuscar);

            pnlBusquedaBox.Controls.Add(pnlTxtContainer);
            pnlBusquedaBox.Controls.Add(lblSearchIcon);
            pnlBusquedaBox.Controls.Add(lblF2Badge);

            pnlTopBar.Controls.Add(pnlBusquedaBox);
            pnlTopBar.Controls.Add(flpAccionesSuperiores);

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

            // 3. Grid de Productos
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

        private static bool EsProductoPorGramos(Producto prod)
        {
            if (prod == null) return false;
            string n = prod.Nombre?.ToLower() ?? "";
            return n.Contains("(gr)") || n.Contains("(g)") || n.Contains("/gr") || n.Contains("granel");
        }

        private void CargarCategoriasDesdeBD()
        {
            flowCategorias.Controls.Clear();

            flowCategorias.Controls.Add(CrearBotonCategoria(
                "Todas",
                Color.FromArgb(239, 246, 255),
                Color.FromArgb(37, 99, 235),
                Color.FromArgb(29, 78, 216),
                _categoriaActivaNombre == "Todas"
            ));

            var categoriasBD = _productoService.ObtenerCategoriasRegistradas();

            foreach (var cat in categoriasBD)
            {
                if (cat.Equals("Todas", StringComparison.OrdinalIgnoreCase)) continue;

                int colorHash = Math.Abs(cat.Trim().ToLowerInvariant().GetHashCode());
                var estilo = _paletaColores[colorHash % _paletaColores.Count];
                bool seleccionada = _categoriaActivaNombre.Equals(cat, StringComparison.OrdinalIgnoreCase);

                flowCategorias.Controls.Add(CrearBotonCategoria(
                    cat,
                    estilo.Fondo, estilo.Texto, estilo.Borde,
                    seleccionada
                ));
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

        private decimal ObtenerPrecioActivoProducto(Producto prod)
        {
            int clienteId = _clienteActual?.IdCliente ?? 0;
            return _productoService.ObtenerPrecioProductoConCliente(prod, _listaClienteActivo, 1, clienteId);
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

            int cantBotones = flowCategorias.Controls.Count;
            int filas = (int)Math.Ceiling((double)cantBotones / columnas);
            if (filas < 1) filas = 1;

            int altoCategorias = filas * 42;
            flowCategorias.Height = altoCategorias;

            int altoSubFamilias = (flowSubFamilias.Visible && flowSubFamilias.Controls.Count > 0) ? 34 : 0;
            flowSubFamilias.Height = altoSubFamilias;

            int altoTotalSeccion = 20 + altoCategorias + altoSubFamilias + 8;
            pnlCatSection.Height = altoTotalSeccion;

            if (pnlIzquierda.RowStyles.Count > 1)
            {
                pnlIzquierda.RowStyles[1].SizeType = SizeType.Absolute;
                pnlIzquierda.RowStyles[1].Height = altoTotalSeccion;
            }
        }

        private Button CrearBotonCategoria(string nombreCategoria, Color back, Color fore, Color colorBordeFuerte, bool seleccionada)
        {
            Button btn = new Button
            {
                Name = "cat_" + nombreCategoria.Replace(" ", "_"),
                Text = nombreCategoria,
                Height = 36,
                Width = 120,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
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
            int clienteId = _clienteActual?.IdCliente ?? 0;

            foreach (var carritoVendedor in _carritosPorVendedor.Values)
            {
                foreach (var item in carritoVendedor)
                {
                    var prod = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prod != null)
                    {
                        item.PrecioLista1 = prod.PrecioUnitario;
                        item.PrecioUnitario = _productoService.ObtenerPrecioProductoConCliente(prod, _listaClienteActivo, item.Cantidad, clienteId);
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

            IEnumerable<Producto> filtrados = _productosCache;

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

            bool esPesable = EsProductoPorGramos(prod);
            

            Control ctrlImagen;
            Image? imgProd = ImagenHelper.CargarImagenSegura(prod.ImagenPath);

            if (imgProd != null)
            {
                PictureBox pb = new PictureBox
                {
                    Location = new Point(40, 6),
                    Size = new Size(50, 50),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Image = imgProd
                };
                ctrlImagen = pb;
            }
            else
            {
                ctrlImagen = new Label
                {
                    Text = esPesable ? "⚖️" : "📦",
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

            decimal precioVenta = ObtenerPrecioActivoProducto(prod);
            Color colorPrecio = prod.ListaDefectoPOS > 1 ? Color.FromArgb(2, 132, 199) : Color.FromArgb(0, 102, 255);

            string textoPrecio = esPesable 
                ? $"{MonedaHelper.Formatear(precioVenta, conSigno: true)}/g" 
                : MonedaHelper.Formatear(precioVenta, conSigno: true);

            Label lblPrecio = new Label
            {
                Text = textoPrecio,
                Font = new Font("Segoe UI", esPesable ? 9.5F : 10.5F, FontStyle.Bold),
                ForeColor = colorPrecio,
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
            string stockTexto = esPesable 
                ? (prod.Stock >= 1000 ? $"Stock: {(prod.Stock / 1000.0):0.0} kg" : $"Stock: {prod.Stock} g") 
                : $"Stock: {prod.Stock} un.";

            Label lblStock = new Label
            {
                Text = stockTexto,
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

            IEnumerable<Producto> queryJerarquica = _productosCache;

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
                AsignarClienteActivo(_clientesPorVendedor[_vendedorActualNombre]);
            }
            else
            {
                AsignarClienteActivo(null);
            }
        }

        private void BuscarYAgregarProducto()
        {
            string query = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            IEnumerable<Producto> queryJerarquica = _productosCache;

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

            bool esPorGramos = EsProductoPorGramos(prod);
            var cart = ObtenerCarritoActivo();
            var itemExistente = cart.FirstOrDefault(c => c.ProductoID == prod.ProductoID);
            int cantidadInicial = itemExistente != null ? itemExistente.Cantidad : (esPorGramos ? 250 : 1);

            decimal precioBaseVenta = ObtenerPrecioActivoProducto(prod);

            using (FormCantidadModal modal = new FormCantidadModal(prod.Nombre, precioBaseVenta, cantidadInicial, prod.Stock, prod.ImagenPath, esPorGramos))
            {
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    int nuevaCantidad = modal.CantidadSeleccionada;

                    if (nuevaCantidad > prod.Stock)
                    {
                        string unidad = esPorGramos ? "gramos" : "unidades";
                        MessageBox.Show($"Stock insuficiente. Solo hay {prod.Stock} {unidad} disponibles.", "Stock Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nuevaCantidad = prod.Stock;
                    }

                    if (nuevaCantidad <= 0)
                    {
                        if (itemExistente != null) cart.Remove(itemExistente);
                    }
                    else
                    {
                        int clienteId = _clienteActual?.IdCliente ?? 0;
                        decimal precioFinal = _productoService.ObtenerPrecioProductoConCliente(prod, _listaClienteActivo, nuevaCantidad, clienteId);

                        if (itemExistente != null)
                        {
                            itemExistente.Cantidad = nuevaCantidad;
                            itemExistente.PrecioLista1 = prod.PrecioUnitario;
                            itemExistente.PrecioUnitario = precioFinal;
                        }
                        else
                        {
                            cart.Add(new DetalleCarrito
                            {
                                ProductoID = prod.ProductoID,
                                Nombre = prod.Nombre,
                                PrecioLista1 = prod.PrecioUnitario,
                                PrecioUnitario = precioFinal,
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
            int clienteId = _clienteActual?.IdCliente ?? 0;

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

                var prodOriginal = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                bool esGramos = prodOriginal != null && EsProductoPorGramos(prodOriginal);
                int pasoCarrito = esGramos ? 50 : 1;

                Label lblNombre = new Label { Name = "lblNombre", Text = item.Nombre, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(6, 4), AutoSize = false, Size = new Size(110, 18) };
                
                string txtUnit = esGramos 
                    ? $"{MonedaHelper.Formatear(item.PrecioUnitario, conSigno: true)}/g" 
                    : MonedaHelper.Formatear(item.PrecioUnitario, conSigno: true);

                Label lblPrecioU = new Label { Name = "lblPrecioU", Text = txtUnit, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(6, 24), AutoSize = true };

                Button btnRestar = new Button { Name = "btnRestar", Text = "-", Size = new Size(22, 22), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnRestar.FlatAppearance.BorderSize = 0;
                btnRestar.Click += (s, e) =>
                {
                    item.Cantidad -= pasoCarrito;
                    if (item.Cantidad <= 0)
                    {
                        cart.Remove(item);
                    }
                    else
                    {
                        if (prodOriginal != null)
                        {
                            item.PrecioUnitario = _productoService.ObtenerPrecioProductoConCliente(prodOriginal, _listaClienteActivo, item.Cantidad, clienteId);
                        }
                    }
                    ActualizarCarritoUI();
                };

                string txtCantidadBadge = esGramos ? $"{item.Cantidad}g" : item.Cantidad.ToString();
                Label lblQty = new Label { Name = "lblQty", Text = txtCantidadBadge, Font = new Font("Segoe UI", esGramos ? 7.5F : 8.5F, FontStyle.Bold), Size = new Size(esGramos ? 44 : 22, 18), TextAlign = ContentAlignment.MiddleCenter };

                Button btnSumar = new Button { Name = "btnSumar", Text = "+", Size = new Size(22, 22), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnSumar.FlatAppearance.BorderSize = 0;
                btnSumar.Click += (s, e) =>
                {
                    if (prodOriginal != null && prodOriginal.Stock < (item.Cantidad + pasoCarrito))
                    {
                        MessageBox.Show($"No hay más stock disponible de '{item.Nombre}'.", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    item.Cantidad += pasoCarrito;

                    if (prodOriginal != null)
                    {
                        item.PrecioUnitario = _productoService.ObtenerPrecioProductoConCliente(prodOriginal, _listaClienteActivo, item.Cantidad, clienteId);
                    }

                    ActualizarCarritoUI();
                };

                Label lblSubtotal = new Label { Name = "lblSubtotal", Text = MonedaHelper.Formatear(item.Subtotal, conSigno: true), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), AutoSize = true };

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

            decimal subtotalBruto = cart.Sum(c => c.SubtotalLista1);
            decimal totalPagar = cart.Sum(c => c.Subtotal);
            decimal ahorroDescuento = subtotalBruto - totalPagar;
            if (ahorroDescuento < 0) ahorroDescuento = 0;

            lblSubtotalValue.Text = MonedaHelper.Formatear(subtotalBruto, conSigno: true);
            lblDescuentoValue.Text = ahorroDescuento > 0 ? $"-{MonedaHelper.Formatear(ahorroDescuento, conSigno: true)}" : "$ 0";
            lblTotalValue.Text = MonedaHelper.Formatear(totalPagar, conSigno: true);

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
                    if (lblQty != null) lblQty.Location = new Point(anchoContenedor - 156, 16);
                    if (btnRestar != null) btnRestar.Location = new Point(anchoContenedor - 180, 14);
                    if (lblNombre != null) lblNombre.Width = Math.Max(50, anchoContenedor - 188);
                }
            }
        }

        private void LimpiarCarritoActual()
        {
            _idTveEnEdicion = 0;
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

        private void CargarListaVendedoresBD()
        {
            cbVendedor.Items.Clear();
            var usuariosVenta = _usuarioService.ObtenerUsuariosParaVenta();

            foreach (var u in usuariosVenta)
            {
                string display = !string.IsNullOrWhiteSpace(u.NombreCompleto) ? u.NombreCompleto : u.NombreUsuario;
                cbVendedor.Items.Add(display);
            }

            if (cbVendedor.Items.Count == 0)
            {
                cbVendedor.Items.Add(_usuarioActual?.NombreCompleto ?? "Vendedor");
            }

            string usuarioSesion = _usuarioActual?.NombreCompleto ?? _usuarioActual?.NombreUsuario ?? "";
            int indexSesion = -1;

            for (int i = 0; i < cbVendedor.Items.Count; i++)
            {
                if (cbVendedor.Items[i]?.ToString()?.Equals(usuarioSesion, StringComparison.OrdinalIgnoreCase) == true)
                {
                    indexSesion = i;
                    break;
                }
            }

            cbVendedor.SelectedIndex = indexSesion >= 0 ? indexSesion : 0;
            _vendedorActualNombre = cbVendedor.SelectedItem?.ToString() ?? "Vendedor";
        }

        private void BtnCliente_Click(object? sender, EventArgs e)
        {
            using (FormSeleccionarClienteModal modalCliente = new FormSeleccionarClienteModal(_clienteSeleccionadoRut))
            {
                if (modalCliente.ShowDialog(this) == DialogResult.OK)
                {
                    AsignarClienteActivo(modalCliente.ClienteSeleccionado);
                }
            }
        }

        private void BtnVentasEnEspera_Click(object? sender, EventArgs e)
        {
            var cart = ObtenerCarritoActivo();
            string vendedorNombre = cbVendedor.SelectedItem?.ToString() ?? _vendedorActualNombre;

            if (cart.Count > 0)
            {
                string identificador = "";

                bool esConsumidorFinal = string.IsNullOrWhiteSpace(_clienteSeleccionadoRut) || 
                                         _clienteSeleccionadoNombre.StartsWith("Consumidor Final", StringComparison.OrdinalIgnoreCase);

                if (esConsumidorFinal)
                {
                    using (Form modalPrompt = new Form
                    {
                        Text = "Poner Venta en Espera",
                        Size = new Size(360, 190),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.White,
                        KeyPreview = true
                    })
                    {
                        Label lbl = new Label { Text = "Referencia para Consumidor Final:", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                        TextBox txt = new TextBox { Text = "Cliente en sala", Location = new Point(20, 42), Size = new Size(300, 26), Font = new Font("Segoe UI", 10F) };
                        Button btnOk = new Button { Text = "✔ Confirmar Pausa", Location = new Point(20, 85), Size = new Size(300, 38), BackColor = Color.FromArgb(245, 158, 11), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                        btnOk.FlatAppearance.BorderSize = 0;
                        btnOk.Click += (s, ev) => { modalPrompt.DialogResult = DialogResult.OK; modalPrompt.Close(); };

                        modalPrompt.AcceptButton = btnOk;

                        txt.KeyDown += (s, ev) =>
                        {
                            if (ev.KeyCode == Keys.Enter)
                            {
                                btnOk.PerformClick();
                                ev.SuppressKeyPress = true;
                            }
                        };

                        modalPrompt.Controls.AddRange(new Control[] { lbl, txt, btnOk });
                        if (modalPrompt.ShowDialog(this) != DialogResult.OK) return;

                        identificador = string.IsNullOrWhiteSpace(txt.Text) ? "Cliente en sala" : txt.Text.Trim();
                    }
                }

                var cartCopia = cart.ToList();

                try
                {
                    _ventaService.PonerVentaEnEspera(cartCopia, vendedorNombre, _clienteSeleccionadoNombre, _clienteSeleccionadoRut, identificador, _idTveEnEdicion);
                    _idTveEnEdicion = 0;
                    LimpiarCarritoActual();
                    MessageBox.Show("Venta guardada en espera. Terminal libre.", "Venta Pausada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    string detalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    MessageBox.Show($"Error al pausar venta:\n{detalle}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            using var modal = new FormVentasEnEsperaModal(vendedorNombre);
            if (modal.ShowDialog(this) == DialogResult.OK && modal.IdTveSeleccionado > 0)
            {
                var resultado = _ventaService.RecuperarVentaEnEspera(modal.IdTveSeleccionado, vendedorNombre);
                if (!resultado.Exito)
                {
                    MessageBox.Show(resultado.Mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _idTveEnEdicion = modal.IdTveSeleccionado;

                cart.Clear();
                cart.AddRange(resultado.Carrito);

                using (var db = new AppDbContext())
                {
                    string rutLimpio = RutHelper.Limpiar(resultado.Rut);
                    var clienteBD = db.Clientes.FirstOrDefault(c => c.IdCliente == resultado.IdCliente || (!string.IsNullOrEmpty(rutLimpio) && c.Rut == rutLimpio));
                    AsignarClienteActivo(clienteBD);
                }

                if (_clienteActual == null)
                {
                    _clienteSeleccionadoNombre = resultado.Cliente;
                    btnCliente.Text = $"👤 {_clienteSeleccionadoNombre.Split(' ')[0]}";
                }

                ActualizarCarritoUI();
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
                int nroTicket;
                if (_idTveEnEdicion > 0)
                {
                    nroTicket = _ventaService.ActualizarTicketExistenteACaja(_idTveEnEdicion, cartCopia, vendedorNombre, _clienteSeleccionadoNombre, _clienteSeleccionadoRut);
                    _idTveEnEdicion = 0;
                }
                else
                {
                    nroTicket = _ventaService.GenerarTicketVenta(cartCopia, vendedorNombre, _clienteSeleccionadoNombre, _clienteSeleccionadoRut);
                }

                LimpiarCarritoActual();

                FormPreVentaModal modalPreVenta = new FormPreVentaModal(nroTicket, vendedorNombre, _clienteSeleccionadoNombre, total, cartCopia);
                modalPreVenta.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar Ticket de Atención: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AsignarClienteActivo(Cliente? cliente)
        {
            _clienteActual = cliente;
            _clientesPorVendedor[_vendedorActualNombre] = cliente;

            if (cliente != null)
            {
                _clienteSeleccionadoNombre = cliente.RazonSocial;
                _clienteSeleccionadoRut = cliente.Rut;
                _listaClienteActivo = cliente.ListaPrecioDefecto > 0 ? cliente.ListaPrecioDefecto : 1;

                var promociones = _productoService.ObtenerPromocionesVigentesCliente(cliente.IdCliente);
                if (promociones.Count > 0)
                {
                    string detalle = string.Join("\n", promociones.Select(p => $"• {p.NombreProducto}: {MonedaHelper.Formatear(p.PrecioPactado, conSigno: true)} (Válido hasta {p.Fin:dd/MM/yyyy})"));
                    MessageBox.Show($"¡El cliente cuenta con Precios Especiales Vigentes!\n\n{detalle}", "Precios Especiales del Cliente", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                _clienteSeleccionadoNombre = "Consumidor Final";
                _clienteSeleccionadoRut = "";
                _listaClienteActivo = 1;
            }

            btnCliente.Text = $"👤 {_clienteSeleccionadoNombre.Split(' ')[0]}";

            CargarProductosDesdeBD();
            ActualizarCarritoUI();
        }
    }
}