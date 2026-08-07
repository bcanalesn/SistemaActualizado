using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public partial class FormVenta : Form
    {
        // Diccionario en memoria para mantener carritos independientes por cada vendedor
        private static Dictionary<string, List<DetalleCarrito>> _carritosPorVendedor = new Dictionary<string, List<DetalleCarrito>>();

        private AppDbContext _db = new AppDbContext();
        private List<Producto> _productosCache = new List<Producto>();

        // Controles Superiores
        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private ComboBox cbVendedor = null!;

        // Controles de Categorías y Grilla de Productos
        private FlowLayoutPanel flowCategorias = null!;
        private FlowLayoutPanel pnlProductosGrid = null!;
        private Panel pnlProdContainer = null!;
        private Label lblCategoriaTitulo = null!;

        // Controles Columna Derecha (Carrito de Venta)
        private Label lblTituloCarro = null!;
        private Label lblCantItemsBadge = null!;
        private Button btnLimpiarCarro = null!;
        private FlowLayoutPanel flowCarritoItems = null!;

        // Totales y Acciones del Carrito
        private Label lblSubtotalValue = null!;
        private Label lblDescuentoValue = null!;
        private Label lblTotalValue = null!;
        private Button btnCliente = null!;
        private Button btnGenerarTicket = null!;

        // Cliente Seleccionado opcional para la venta
        private string _clienteSeleccionadoNombre = "Consumidor Final";
        private string _clienteSeleccionadoRut = "";

        private Usuario? _usuarioActual;
        private static string _vendedorActualNombre = "Bárbara"; // Mantiene el último vendedor entre pestañas
        private string _categoriaActivaNombre = "Todas";

        public FormVenta(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            CargarProductosDesdeBD();
            RestaurarCarritoVendedorActual();

            // Habilitar atajos de teclado globales (F2, F3, F4, ESC)
            this.KeyPreview = true;
            this.KeyDown += FormVenta_KeyDown;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            // TableLayout Principal (65% Catálogo Izquierda | 35% Carrito Derecha)
            TableLayoutPanel pnlLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12)
            };
            pnlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            pnlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            pnlLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F)); // Barra de Atajos

            // =========================================================================
            // COLUMNA IZQUIERDA: BÚSQUEDA, CATEGORÍAS Y GRILLA DE PRODUCTOS
            // =========================================================================
            Panel pnlIzquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };

            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            // Campo de Búsqueda Centrado (Ajustado X=38 para no tapar el ícono)
            Panel pnlBusquedaBox = new Panel
            {
                Size = new Size(420, 36),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlBusquedaBox.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlBusquedaBox.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            Label lblSearchIcon = new Label { Text = "🔍", Location = new Point(8, 8), AutoSize = true, Font = new Font("Segoe UI", 10F) };

            txtBuscar = new TextBox
            {
                Text = "Buscar producto por nombre, código o barra...",
                ForeColor = Color.Gray,
                Location = new Point(38, 7), // Margen corregido para dar espacio al ícono de la lupa
                Size = new Size(305, 24),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            txtBuscar.Enter += (s, e) => { if (txtBuscar.Text.StartsWith("Buscar")) { txtBuscar.Text = ""; txtBuscar.ForeColor = Color.Black; } };
            txtBuscar.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) { txtBuscar.Text = "Buscar producto por nombre, código o barra..."; txtBuscar.ForeColor = Color.Gray; } };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BuscarYAgregarProducto(); e.SuppressKeyPress = true; } };
            
            // BÚSQUEDA INTELIGENTE EN VIVO POR INICIALES AL ESCRIBIR
            txtBuscar.TextChanged += (s, e) => { FiltrarProductosPorBusquedaInteligente(); };

            Label lblF2Badge = new Label
            {
                Text = "F2",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(226, 232, 240),
                Size = new Size(26, 20),
                Location = new Point(350, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(380, 5),
                Size = new Size(34, 26),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => BuscarYAgregarProducto();

            pnlBusquedaBox.Controls.Add(lblSearchIcon);
            pnlBusquedaBox.Controls.Add(txtBuscar);
            pnlBusquedaBox.Controls.Add(lblF2Badge);

            // Seleccionar Vendedor Top Derecha
            Panel pnlVendedorCard = new Panel { Size = new Size(180, 40), Dock = DockStyle.Right, BackColor = Color.Transparent };
            Label lblVendIcon = new Label { Text = "👤", Location = new Point(5, 10), AutoSize = true, Font = new Font("Segoe UI", 11F) };

            cbVendedor = new ComboBox
            {
                Location = new Point(30, 8),
                Size = new Size(140, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            cbVendedor.Items.AddRange(new string[] { "Bárbara", "Víctor", "Juan", "María" });
            
            // Restaurar vendedor previamente seleccionado
            if (!cbVendedor.Items.Contains(_vendedorActualNombre))
            {
                _vendedorActualNombre = _usuarioActual?.NombreCompleto ?? "Bárbara";
            }
            cbVendedor.SelectedItem = cbVendedor.Items.Contains(_vendedorActualNombre) ? _vendedorActualNombre : "Bárbara";
            cbVendedor.SelectedIndexChanged += CbVendedor_SelectedIndexChanged;

            pnlVendedorCard.Controls.Add(lblVendIcon);
            pnlVendedorCard.Controls.Add(cbVendedor);

            pnlTopBar.Controls.Add(pnlBusquedaBox);
            pnlTopBar.Controls.Add(pnlVendedorCard);

            Panel pnlCatSection = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(0, 6, 0, 6) };
            Label lblCatTitle = new Label { Text = "CATEGORÍAS", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 16 };

            flowCategorias = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = false, WrapContents = false };
            
            // Registrar categorías conservando sus colores originales de tema
            flowCategorias.Controls.Add(CrearBotonCategoria("📦 Todas", Color.FromArgb(0, 102, 255), Color.White, true));
            flowCategorias.Controls.Add(CrearBotonCategoria("🥛 Lácteos", Color.FromArgb(224, 242, 254), Color.FromArgb(3, 105, 161), false));
            flowCategorias.Controls.Add(CrearBotonCategoria("🥩 Cecinas", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28), false));
            flowCategorias.Controls.Add(CrearBotonCategoria("🌾 Abarrotes", Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9), false));
            flowCategorias.Controls.Add(CrearBotonCategoria("🥤 Bebidas", Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61), false));
            flowCategorias.Controls.Add(CrearBotonCategoria("🧹 Aseo", Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206), false));

            pnlCatSection.Controls.Add(flowCategorias);
            pnlCatSection.Controls.Add(lblCatTitle);

            pnlProdContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            lblCategoriaTitulo = new Label { Text = "PRODUCTOS", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 22 };

            pnlProductosGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            pnlProdContainer.Controls.Add(pnlProductosGrid);
            pnlProdContainer.Controls.Add(lblCategoriaTitulo);

            pnlIzquierda.Controls.Add(pnlProdContainer);
            pnlIzquierda.Controls.Add(pnlCatSection);
            pnlIzquierda.Controls.Add(pnlTopBar);

            // =========================================================================
            // COLUMNA DERECHA: VENTA ACTUAL (CARRITO), TOTALES Y BOTÓN COBRAR
            // =========================================================================
            Panel pnlDerecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };

            Panel pnlCartHeader = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White };
            lblTituloCarro = new Label { Text = "VENTA ACTUAL", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, 4), AutoSize = true };

            lblCantItemsBadge = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
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
                Width = 85,
                Height = 28,
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiarCarro.FlatAppearance.BorderSize = 0;
            btnLimpiarCarro.Click += (s, e) => LimpiarCarritoActual();

            pnlCartHeader.Controls.Add(lblTituloCarro);
            pnlCartHeader.Controls.Add(lblCantItemsBadge);
            pnlCartHeader.Controls.Add(btnLimpiarCarro);

            flowCarritoItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(4),
                Margin = new Padding(0, 8, 0, 8)
            };
            flowCarritoItems.SizeChanged += (s, e) => ReajustarAnchoFilasCarrito();

            Panel pnlBottomCheckout = new Panel { Dock = DockStyle.Bottom, Height = 175, Padding = new Padding(0, 8, 0, 0) };

            Panel pnlDesgloseTotales = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(12) };

            Label lblSubtotalCap = new Label { Text = "Subtotal", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(10, 10), AutoSize = true };
            lblSubtotalValue = new Label { Text = "$ 0", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(220, 10), AutoSize = true };

            Label lblDescCap = new Label { Text = "Descuento", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(10, 32), AutoSize = true };
            lblDescuentoValue = new Label { Text = "$ 0", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(220, 32), AutoSize = true };

            Label lblTotalCap = new Label { Text = "Total", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, 56), AutoSize = true };
            lblTotalValue = new Label { Text = "$ 0", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Location = new Point(180, 50), AutoSize = true };

            pnlDesgloseTotales.Controls.Add(lblSubtotalCap);
            pnlDesgloseTotales.Controls.Add(lblSubtotalValue);
            pnlDesgloseTotales.Controls.Add(lblDescCap);
            pnlDesgloseTotales.Controls.Add(lblDescuentoValue);
            pnlDesgloseTotales.Controls.Add(lblTotalCap);
            pnlDesgloseTotales.Controls.Add(lblTotalValue);

            Panel pnlBotonesAccion = new Panel { Dock = DockStyle.Bottom, Height = 75, Padding = new Padding(0, 10, 0, 0) };

            btnCliente = new Button
            {
                Text = "👤 Cliente",
                Location = new Point(0, 12),
                Size = new Size(110, 48),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCliente.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCliente.Click += BtnCliente_Click;

            btnGenerarTicket = new Button
            {
                Text = "🎟️ F4 GENERAR TICKET",
                Location = new Point(118, 12),
                Size = new Size(220, 48),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerarTicket.FlatAppearance.BorderSize = 0;
            btnGenerarTicket.Click += BtnGenerarTicket_Click;

            pnlBotonesAccion.Controls.Add(btnCliente);
            pnlBotonesAccion.Controls.Add(btnGenerarTicket);

            pnlBottomCheckout.Controls.Add(pnlBotonesAccion);
            pnlBottomCheckout.Controls.Add(pnlDesgloseTotales);

            pnlDerecha.Controls.Add(flowCarritoItems);
            pnlDerecha.Controls.Add(pnlBottomCheckout);
            pnlDerecha.Controls.Add(pnlCartHeader);

            Panel pnlAtajosFooter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(238, 242, 246),
                Padding = new Padding(8, 6, 8, 6)
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

            pnlLayout.Controls.Add(pnlIzquierda, 0, 0);
            pnlLayout.Controls.Add(pnlDerecha, 1, 0);
            pnlLayout.Controls.Add(pnlAtajosFooter, 0, 1);
            pnlLayout.SetColumnSpan(pnlAtajosFooter, 2);

            this.Controls.Add(pnlLayout);
            this.ResumeLayout(false);

            this.Load += (s, e) => ReajustarAnchoFilasCarrito();
        }

        private void FormVenta_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                txtBuscar.Focus();
                txtBuscar.SelectAll();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                BtnCliente_Click(this, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F4)
            {
                BtnGenerarTicket_Click(this, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                LimpiarCarritoActual();
                e.SuppressKeyPress = true;
            }
        }

        private void FiltrarProductosPorBusquedaInteligente()
        {
            string query = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(query) || query.StartsWith("Buscar"))
            {
                FiltrarProductosPorCategoria(_categoriaActivaNombre, Color.FromArgb(244, 246, 249));
                return;
            }

            pnlProductosGrid.Controls.Clear();
            
            // Búsqueda inteligente por iniciales: productos donde cualquier palabra comience con 'query'
            var filtrados = _productosCache.Where(p =>
                p.Nombre.Split(' ').Any(palabra => palabra.StartsWith(query, StringComparison.OrdinalIgnoreCase)) ||
                p.CodigoBarra.StartsWith(query, StringComparison.OrdinalIgnoreCase) ||
                p.ProductoID.ToString().StartsWith(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            foreach (var prod in filtrados)
            {
                Panel card = CrearTarjetaProductoUI(prod);
                pnlProductosGrid.Controls.Add(card);
            }
        }

        private List<DetalleCarrito> ObtenerCarritoActivo()
        {
            string vendor = cbVendedor.SelectedItem?.ToString() ?? _vendedorActualNombre;
            if (!_carritosPorVendedor.ContainsKey(vendor))
            {
                _carritosPorVendedor[vendor] = new List<DetalleCarrito>();
            }
            return _carritosPorVendedor[vendor];
        }

        private void CbVendedor_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _vendedorActualNombre = cbVendedor.SelectedItem?.ToString() ?? "Bárbara";
            ActualizarCarritoUI();
        }

        private void RestaurarCarritoVendedorActual()
        {
            ActualizarCarritoUI();
        }

        private Button CrearBotonCategoria(string texto, Color back, Color fore, bool seleccionada)
        {
            Button btn = new Button
            {
                Text = texto,
                Size = new Size(105, 32),
                BackColor = seleccionada ? Color.FromArgb(0, 102, 255) : back,
                ForeColor = seleccionada ? Color.White : fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0),
                Tag = new Tuple<Color, Color>(back, fore) // Almacenar colores originales del pastel
            };
            btn.FlatAppearance.BorderSize = seleccionada ? 2 : 0;
            if (seleccionada)
            {
                btn.FlatAppearance.BorderColor = Color.FromArgb(0, 80, 200);
            }

            btn.Click += (s, e) =>
            {
                // Restaurar el color pastel propio de CADA botón
                foreach (Control c in flowCategorias.Controls)
                {
                    if (c is Button b && b.Tag is Tuple<Color, Color> origColors)
                    {
                        b.BackColor = origColors.Item1;
                        b.ForeColor = origColors.Item2;
                        b.FlatAppearance.BorderSize = 0;
                    }
                }

                // Destacar la categoría activa
                btn.BackColor = Color.FromArgb(0, 102, 255);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Color.FromArgb(0, 80, 200);

                string catClean = texto.Split(' ').Length > 1 ? texto.Split(' ')[1] : texto;
                FiltrarProductosPorCategoria(catClean, back);
            };
            return btn;
        }

        private void CargarProductosDesdeBD()
        {
            try
            {
                _productosCache = _db.Productos.Where(p => p.Estado).ToList();
            }
            catch
            {
                _productosCache = new List<Producto>();
            }

            if (_productosCache.Count == 0)
            {
                // Backup Demo con stock precargado
                _productosCache = new List<Producto>
                {
                    new Producto { ProductoID = 1, CodigoBarra = "1001", Nombre = "Leche Entera 1L", PrecioUnitario = 1150, Stock = 50, Estado = true },
                    new Producto { ProductoID = 2, CodigoBarra = "1002", Nombre = "Queso Gauda 250g", PrecioUnitario = 2490, Stock = 40, Estado = true },
                    new Producto { ProductoID = 3, CodigoBarra = "1003", Nombre = "Pan de Molde Blanco", PrecioUnitario = 890, Stock = 60, Estado = true },
                    new Producto { ProductoID = 4, CodigoBarra = "1004", Nombre = "Huevos 12 un.", PrecioUnitario = 2300, Stock = 35, Estado = true },
                    new Producto { ProductoID = 5, CodigoBarra = "1005", Nombre = "Arroz Grado 1kg", PrecioUnitario = 1250, Stock = 80, Estado = true },
                    new Producto { ProductoID = 6, CodigoBarra = "1006", Nombre = "Aceite Vegetal 1L", PrecioUnitario = 1690, Stock = 45, Estado = true },
                    new Producto { ProductoID = 7, CodigoBarra = "1007", Nombre = "Azúcar 1kg", PrecioUnitario = 1190, Stock = 70, Estado = true },
                    new Producto { ProductoID = 8, CodigoBarra = "1008", Nombre = "Café Instantáneo 100g", PrecioUnitario = 2990, Stock = 30, Estado = true },
                    new Producto { ProductoID = 9, CodigoBarra = "1009", Nombre = "Detergente Líquido 3L", PrecioUnitario = 5990, Stock = 20, Estado = true },
                    new Producto { ProductoID = 10, CodigoBarra = "1010", Nombre = "Cloro Gel 900ml", PrecioUnitario = 1390, Stock = 25, Estado = true },
                    new Producto { ProductoID = 11, CodigoBarra = "1011", Nombre = "Lavaloza Menta 750ml", PrecioUnitario = 1850, Stock = 30, Estado = true },
                    new Producto { ProductoID = 12, CodigoBarra = "1012", Nombre = "Papel Higiénico 4 rollos", PrecioUnitario = 2290, Stock = 40, Estado = true }
                };
            }

            FiltrarProductosPorCategoria("Todas", Color.FromArgb(244, 246, 249));
        }

        private void FiltrarProductosPorCategoria(string categoria, Color colorFondo)
        {
            _categoriaActivaNombre = categoria;
            pnlProductosGrid.Controls.Clear();

            var filtrados = _productosCache.AsQueryable();

            if (categoria != "Todas")
            {
                filtrados = filtrados.Where(p => p.Nombre.ToLower().Contains(categoria.ToLower()) ||
                            (categoria == "Lácteos" && (p.Nombre.Contains("Leche") || p.Nombre.Contains("Yogurt") || p.Nombre.Contains("Queso") || p.Nombre.Contains("Huevos"))) ||
                            (categoria == "Cecinas" && (p.Nombre.Contains("Jamón") || p.Nombre.Contains("Salchicha") || p.Nombre.Contains("Salame"))) ||
                            (categoria == "Bebidas" && (p.Nombre.Contains("Bebida") || p.Nombre.Contains("Sprite") || p.Nombre.Contains("Coca") || p.Nombre.Contains("Jugo"))) ||
                            (categoria == "Abarrotes" && (p.Nombre.Contains("Azúcar") || p.Nombre.Contains("Aceite") || p.Nombre.Contains("Café") || p.Nombre.Contains("Pan") || p.Nombre.Contains("Arroz"))) ||
                            (categoria == "Aseo" && (p.Nombre.Contains("Detergente") || p.Nombre.Contains("Cloro") || p.Nombre.Contains("Lavaloza") || p.Nombre.Contains("Papel") || p.Nombre.Contains("Limpio"))));
            }

            var lista = filtrados.ToList();

            foreach (var prod in lista)
            {
                Panel card = CrearTarjetaProductoUI(prod);
                pnlProductosGrid.Controls.Add(card);
            }
        }

        private Panel CrearTarjetaProductoUI(Producto prod)
        {
            Panel card = new Panel
            {
                Size = new Size(168, 126),
                BackColor = Color.White,
                Margin = new Padding(6),
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            Label lblImgBox = new Label
            {
                Text = "📦",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(10, 10),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(241, 245, 249)
            };

            Label lblNombre = new Label
            {
                Text = prod.Nombre,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(55, 10),
                Size = new Size(105, 36),
                TextAlign = ContentAlignment.TopLeft
            };

            Label lblPrecio = new Label
            {
                Text = $"${prod.PrecioUnitario:N0}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 255),
                Location = new Point(10, 58),
                AutoSize = true
            };

            Label lblCodigo = new Label
            {
                Text = $"Cód. {prod.CodigoBarra}",
                Font = new Font("Segoe UI", 7.8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 82),
                AutoSize = true
            };

            Color stockColor = prod.Stock > 10 ? Color.FromArgb(16, 185, 129) : (prod.Stock > 0 ? Color.FromArgb(245, 158, 11) : Color.FromArgb(239, 68, 68));
            Label lblStock = new Label
            {
                Text = $"Stock: {prod.Stock} un.",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = stockColor,
                Location = new Point(10, 102),
                AutoSize = true
            };

            Action onClick = () => SolicitarCantidadYAgregar(prod);

            card.Click += (s, e) => onClick();
            lblImgBox.Click += (s, e) => onClick();
            lblNombre.Click += (s, e) => onClick();
            lblPrecio.Click += (s, e) => onClick();
            lblCodigo.Click += (s, e) => onClick();
            lblStock.Click += (s, e) => onClick();

            card.Controls.Add(lblImgBox);
            card.Controls.Add(lblNombre);
            card.Controls.Add(lblPrecio);
            card.Controls.Add(lblCodigo);
            card.Controls.Add(lblStock);

            return card;
        }

        private void BuscarYAgregarProducto()
        {
            string query = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(query) || query.StartsWith("Buscar")) return;

            var prod = _productosCache.FirstOrDefault(p => p.CodigoBarra.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                                                           p.Nombre.Split(' ').Any(w => w.StartsWith(query, StringComparison.OrdinalIgnoreCase)));

            if (prod != null)
            {
                SolicitarCantidadYAgregar(prod);
                txtBuscar.Text = "Buscar producto por nombre, código o barra...";
                txtBuscar.ForeColor = Color.Gray;
            }
            else
            {
                MessageBox.Show($"No se encontró el producto '{query}'.", "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SolicitarCantidadYAgregar(Producto prod)
        {
            if (prod.Stock <= 0)
            {
                MessageBox.Show($"El producto '{prod.Nombre}' no tiene stock disponible en inventario.", "Sin Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cart = ObtenerCarritoActivo();
            var itemExistente = cart.FirstOrDefault(c => c.ProductoID == prod.ProductoID);
            int cantidadInicial = itemExistente != null ? itemExistente.Cantidad : 1;

            using (FormCantidadModal modal = new FormCantidadModal(prod.Nombre, prod.PrecioUnitario, cantidadInicial, prod.Stock))
            {
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    int nuevaCantidad = modal.CantidadSeleccionada;

                    if (nuevaCantidad > prod.Stock)
                    {
                        MessageBox.Show($"Stock insuficiente. Solo hay {prod.Stock} unidades disponibles de '{prod.Nombre}'.", "Stock Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nuevaCantidad = prod.Stock;
                    }

                    if (nuevaCantidad <= 0)
                    {
                        if (itemExistente != null) cart.Remove(itemExistente);
                    }
                    else
                    {
                        if (itemExistente != null)
                        {
                            itemExistente.Cantidad = nuevaCantidad;
                        }
                        else
                        {
                            cart.Add(new DetalleCarrito
                            {
                                ProductoID = prod.ProductoID,
                                Nombre = prod.Nombre,
                                PrecioUnitario = prod.PrecioUnitario,
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
            var cart = ObtenerCarritoActivo();
            flowCarritoItems.Controls.Clear();

            int totalItemsCount = 0;

            foreach (var item in cart)
            {
                totalItemsCount += item.Cantidad;

                Panel row = new Panel
                {
                    Height = 54,
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, 4),
                    Cursor = Cursors.Hand
                };

                row.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, row.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

                Label lblNombre = new Label { Name = "lblNombre", Text = item.Nombre, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(8, 6), AutoSize = false, Size = new Size(110, 18), Cursor = Cursors.Hand };
                Label lblPrecioU = new Label { Name = "lblPrecioU", Text = $"${item.PrecioUnitario:N0}", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(8, 26), AutoSize = true, Cursor = Cursors.Hand };

                // Permitir editar la cantidad haciendo clic en la fila del carrito o en el texto
                var prodOriginal = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                if (prodOriginal != null)
                {
                    Action editAction = () => SolicitarCantidadYAgregar(prodOriginal);
                    row.Click += (s, e) => editAction();
                    lblNombre.Click += (s, e) => editAction();
                    lblPrecioU.Click += (s, e) => editAction();
                }

                Button btnRestar = new Button { Name = "btnRestar", Text = "-", Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnRestar.FlatAppearance.BorderSize = 0;
                btnRestar.Click += (s, e) =>
                {
                    item.Cantidad--;
                    if (item.Cantidad <= 0) cart.Remove(item);
                    ActualizarCarritoUI();
                };

                Label lblQty = new Label { Name = "lblQty", Text = item.Cantidad.ToString(), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Size = new Size(24, 20), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
                if (prodOriginal != null)
                {
                    lblQty.Click += (s, e) => SolicitarCantidadYAgregar(prodOriginal);
                }

                Button btnSumar = new Button { Name = "btnSumar", Text = "+", Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnSumar.FlatAppearance.BorderSize = 0;
                btnSumar.Click += (s, e) =>
                {
                    if (prodOriginal != null && item.Cantidad >= prodOriginal.Stock)
                    {
                        MessageBox.Show($"No se puede agregar más unidades. Stock disponible: {prodOriginal.Stock} un.", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    item.Cantidad++;
                    ActualizarCarritoUI();
                };

                Label lblSubtotal = new Label { Name = "lblSubtotal", Text = $"${item.Subtotal:N0}", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), AutoSize = true };

                Button btnDeleteRow = new Button { Name = "btnDeleteRow", Text = "✕", Size = new Size(22, 22), FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(156, 163, 175), Cursor = Cursors.Hand };
                btnDeleteRow.FlatAppearance.BorderSize = 0;
                btnDeleteRow.Click += (s, e) =>
                {
                    cart.Remove(item);
                    ActualizarCarritoUI();
                };

                row.Controls.Add(lblNombre);
                row.Controls.Add(lblPrecioU);
                row.Controls.Add(btnRestar);
                row.Controls.Add(lblQty);
                row.Controls.Add(btnSumar);
                row.Controls.Add(lblSubtotal);
                row.Controls.Add(btnDeleteRow);

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
            int anchoContenedor = flowCarritoItems.ClientSize.Width > 200 ? flowCarritoItems.ClientSize.Width - 10 : 300;

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

                    if (btnDeleteRow != null) btnDeleteRow.Location = new Point(anchoContenedor - 26, 15);
                    if (lblSubtotal != null) lblSubtotal.Location = new Point(anchoContenedor - 105, 17);
                    if (btnSumar != null) btnSumar.Location = new Point(anchoContenedor - 133, 14);
                    if (lblQty != null) lblQty.Location = new Point(anchoContenedor - 157, 17);
                    if (btnRestar != null) btnRestar.Location = new Point(anchoContenedor - 181, 14);

                    if (lblNombre != null) lblNombre.Width = Math.Max(60, anchoContenedor - 190);
                }
            }
        }

        private void LimpiarCarritoActual()
        {
            var cart = ObtenerCarritoActivo();
            cart.Clear();
            ActualizarCarritoUI();
        }

        private void BtnCliente_Click(object? sender, EventArgs e)
        {
            using (FormSeleccionarClienteModal modalCliente = new FormSeleccionarClienteModal())
            {
                if (modalCliente.ShowDialog(this) == DialogResult.OK)
                {
                    _clienteSeleccionadoNombre = modalCliente.NombreCliente;
                    _clienteSeleccionadoRut = modalCliente.RutCliente;
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
            int nroTicket = (int)(DateTime.Now.Ticks % 1000000);

            try
            {
                TVE2607 tve = new TVE2607
                {
                    idLocal = 1,
                    nmbLocal = "Local Principal",
                    iddocDTE = 0, // 0 = Ticket de Atención Pendiente
                    Documento = "Ticket de Atención",
                    nroDTE = nroTicket,
                    FecDoc = DateTime.Now,
                    SubTotal = Math.Round(total / 1.19m, 0),
                    Neto = Math.Round(total / 1.19m, 0),
                    IvA = total - Math.Round(total / 1.19m, 0),
                    Total = total,
                    UserDTE = vendedorNombre,
                    Vendedor = vendedorNombre,
                    RuT = _clienteSeleccionadoRut,
                    RazonSocial = _clienteSeleccionadoNombre,
                    status = "Pendiente"
                };

                _db.TVE2607.Add(tve);
                _db.SaveChanges();

                foreach (var item in cart)
                {
                    TVD2607 tvd = new TVD2607
                    {
                        idTve = tve.idTve,
                        idLocal = 1,
                        iddocDTE = 0,
                        Documento = "Ticket de Atención",
                        NroDTE = nroTicket,
                        FecMoV = DateTime.Now,
                        IdProducto = item.ProductoID,
                        NmbProducto = item.Nombre,
                        Cantidad = item.Cantidad,
                        Precio = item.PrecioUnitario,
                        SubTotal = item.Subtotal,
                        nmbVendedor = vendedorNombre
                    };
                    _db.TVD2607.Add(tvd);

                    // DESCUENTO DE STOCK EN BASE DE DATOS
                    var prodBD = _db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prodBD != null)
                    {
                        prodBD.Stock -= item.Cantidad;
                        if (prodBD.Stock < 0) prodBD.Stock = 0;

                        // Sincronizar cache local con la instancia de BD sin duplicar la resta
                        var prodCache = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                        if (prodCache != null)
                        {
                            prodCache.Stock = prodBD.Stock;
                        }
                    }
                }

                _db.SaveChanges();

                // Refrescar la vista de catálogo
                FiltrarProductosPorCategoria(_categoriaActivaNombre, Color.FromArgb(244, 246, 249));

                MessageBox.Show($"🎟️ TICKET DE ATENCIÓN N° {nroTicket:D6} GENERADO CON ÉXITO\n\n" +
                                $"• Vendedor: {vendedorNombre}\n" +
                                $"• Cliente: {_clienteSeleccionadoNombre}\n" +
                                $"• Total a Pagar en Caja: ${total:N0}\n\n" +
                                $"⚠️ Se ha reservado el stock de los productos.\n" +
                                $"Indique al cliente pasar a CAJA con el N° de Ticket {nroTicket:D6}.",
                                "Pre-Venta Registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimpiarCarritoActual();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar Ticket de Atención: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // =========================================================================
    // MODAL DE SELECCIÓN DE CANTIDAD (CON TECLADO FÍSICO Y PANTALLA TÁCTIL)
    // =========================================================================
    public class FormCantidadModal : Form
    {
        private int _cantidad = 1;
        private decimal _precioUnitario;
        private int _stockDisponible;
        private bool _esPrimerDigito = true;
        private Label lblCantidadDisplay = null!;
        private Label lblTotalPreview = null!;
        private Button btnAgregarAccion = null!;
        private Panel pnlKeypadContainer = null!;
        private Button btnToggleKeypad = null!;

        public int CantidadSeleccionada => _cantidad;

        public FormCantidadModal(string nombreProducto, decimal precioUnitario, int cantidadInicial, int stockDisponible = 9999)
        {
            _cantidad = cantidadInicial > 0 ? cantidadInicial : 1;
            _precioUnitario = precioUnitario;
            _stockDisponible = stockDisponible > 0 ? stockDisponible : 1;

            if (_cantidad > _stockDisponible) _cantidad = _stockDisponible;

            InitializeComponent(nombreProducto);
        }

        private void InitializeComponent(string nombreProducto)
        {
            this.SuspendLayout();
            this.Text = "Agregar a la Venta";
            this.Size = new Size(390, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.BackColor = Color.White;
            this.KeyPreview = true;

            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(345, 12),
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                TabStop = false, // Evita capturar el foco del teclado al presionar Enter
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            Label lblIcon = new Label { Text = "📦", Font = new Font("Segoe UI", 22F), Location = new Point(25, 18), Size = new Size(45, 45) };
            Label lblTitle = new Label { Text = nombreProducto, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(75, 18), AutoSize = true };
            Label lblSub = new Label { Text = $"Precio: ${_precioUnitario:N0}  |  Stock disp: {_stockDisponible} un.", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199), Location = new Point(75, 40), AutoSize = true };

            Label lblDivider = new Label { Height = 1, BackColor = Color.FromArgb(226, 232, 240), Location = new Point(15, 68), Width = 355 };

            Label lblCantHeader = new Label { Text = "Cantidad (Escriba con teclado o seleccione botón)", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(15, 76), Width = 355, TextAlign = ContentAlignment.MiddleCenter };

            Button btnMinus = new Button { Text = "─", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(35, 96), Size = new Size(44, 42), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat, TabStop = false, Cursor = Cursors.Hand };
            btnMinus.FlatAppearance.BorderSize = 0;
            btnMinus.Click += (s, e) => { if (_cantidad > 1) { _cantidad--; _esPrimerDigito = false; ActualizarValores(); } btnAgregarAccion.Focus(); };

            lblCantidadDisplay = new Label { Text = _cantidad.ToString(), Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Location = new Point(88, 96), Size = new Size(205, 42), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(248, 250, 252) };
            lblCantidadDisplay.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, lblCantidadDisplay.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            Button btnPlus = new Button { Text = "┼", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(302, 96), Size = new Size(44, 42), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat, TabStop = false, Cursor = Cursors.Hand };
            btnPlus.FlatAppearance.BorderSize = 0;
            btnPlus.Click += (s, e) =>
            {
                if (_cantidad < _stockDisponible)
                {
                    _cantidad++;
                    _esPrimerDigito = false;
                    ActualizarValores();
                }
                else
                {
                    MessageBox.Show($"No puede superar el stock disponible ({_stockDisponible} un.).", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                btnAgregarAccion.Focus();
            };

            Label lblRapidasHeader = new Label { Text = "CANTIDADES RÁPIDAS", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(15, 148), Width = 355, TextAlign = ContentAlignment.MiddleCenter };

            FlowLayoutPanel flowRapidas = new FlowLayoutPanel { Location = new Point(25, 168), Size = new Size(335, 38), WrapContents = false };
            int[] cantidades = new int[] { 1, 2, 3, 5, 10 };

            foreach (int c in cantidades)
            {
                Button btnQ = new Button
                {
                    Text = c.ToString(),
                    Size = new Size(58, 32),
                    BackColor = (_cantidad == c) ? Color.FromArgb(0, 102, 255) : Color.FromArgb(248, 250, 252),
                    ForeColor = (_cantidad == c) ? Color.White : Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Margin = new Padding(3, 0, 3, 0),
                    TabStop = false,
                    Cursor = Cursors.Hand
                };
                btnQ.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                int val = c;
                btnQ.Click += (s, e) =>
                {
                    if (val > _stockDisponible)
                    {
                        MessageBox.Show($"La cantidad ({val}) supera el stock disponible ({_stockDisponible} un.).", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _cantidad = _stockDisponible;
                    }
                    else
                    {
                        _cantidad = val;
                    }

                    _esPrimerDigito = false;
                    foreach (Control ctrl in flowRapidas.Controls)
                    {
                        if (ctrl is Button b)
                        {
                            b.BackColor = Color.FromArgb(248, 250, 252);
                            b.ForeColor = Color.FromArgb(15, 23, 42);
                        }
                    }
                    btnQ.BackColor = Color.FromArgb(0, 102, 255);
                    btnQ.ForeColor = Color.White;
                    ActualizarValores();
                    btnAgregarAccion.Focus();
                };
                flowRapidas.Controls.Add(btnQ);
            }

            btnToggleKeypad = new Button
            {
                Text = "⌨️ Mostrar Teclado Numérico Táctil",
                Location = new Point(25, 218),
                Size = new Size(335, 30),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TabStop = false,
                Cursor = Cursors.Hand
            };
            btnToggleKeypad.FlatAppearance.BorderSize = 0;

            pnlKeypadContainer = new Panel
            {
                Location = new Point(25, 256),
                Size = new Size(335, 130),
                Visible = false,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            ConstruirTecladoNumericoUI(pnlKeypadContainer);

            Panel pnlTotalBox = new Panel { Name = "pnlTotalBox", Location = new Point(25, 258), Size = new Size(335, 40), BackColor = Color.FromArgb(248, 250, 252) };
            pnlTotalBox.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlTotalBox.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            Label lblTotalCap = new Label { Text = "Total", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(12, 10), AutoSize = true };
            lblTotalPreview = new Label { Text = $"${_precioUnitario * _cantidad:N0}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(200, 8), Size = new Size(120, 24), TextAlign = ContentAlignment.MiddleRight };

            pnlTotalBox.Controls.Add(lblTotalCap);
            pnlTotalBox.Controls.Add(lblTotalPreview);

            btnAgregarAccion = new Button
            {
                Name = "btnAgregarAccion",
                Text = $"🛒 Agregar {_cantidad} a la venta",
                Location = new Point(25, 310),
                Size = new Size(335, 48),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnAgregarAccion.FlatAppearance.BorderSize = 0;
            btnAgregarAccion.Click += (s, e) => { ConfirmarYAgregar(); };

            Label lblHintEnter = new Label { Name = "lblHintEnter", Text = "Presione [Enter] para agregar a la venta", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(25, 368), Width = 335, TextAlign = ContentAlignment.MiddleCenter };

            btnToggleKeypad.Click += (s, e) =>
            {
                pnlKeypadContainer.Visible = !pnlKeypadContainer.Visible;
                btnToggleKeypad.Text = pnlKeypadContainer.Visible ? "⌨️ Ocultar Teclado Numérico" : "⌨️ Mostrar Teclado Numérico Táctil";

                if (pnlKeypadContainer.Visible)
                {
                    pnlTotalBox.Location = new Point(25, 396);
                    btnAgregarAccion.Location = new Point(25, 444);
                    lblHintEnter.Location = new Point(25, 498);
                    this.Size = new Size(390, 570);
                }
                else
                {
                    pnlTotalBox.Location = new Point(25, 258);
                    btnAgregarAccion.Location = new Point(25, 310);
                    lblHintEnter.Location = new Point(25, 368);
                    this.Size = new Size(390, 480);
                }
                btnAgregarAccion.Focus();
            };

            this.KeyDown += FormCantidadModal_KeyDown;
            this.Shown += (s, e) => btnAgregarAccion.Focus();

            this.Controls.Add(btnClose);
            this.Controls.Add(lblIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSub);
            this.Controls.Add(lblDivider);
            this.Controls.Add(lblCantHeader);
            this.Controls.Add(btnMinus);
            this.Controls.Add(lblCantidadDisplay);
            this.Controls.Add(btnPlus);
            this.Controls.Add(lblRapidasHeader);
            this.Controls.Add(flowRapidas);
            this.Controls.Add(btnToggleKeypad);
            this.Controls.Add(pnlKeypadContainer);
            this.Controls.Add(pnlTotalBox);
            this.Controls.Add(btnAgregarAccion);
            this.Controls.Add(lblHintEnter);

            this.AcceptButton = btnAgregarAccion;
            this.ResumeLayout(false);
        }

        private void ConstruirTecladoNumericoUI(Panel pnlKeypad)
        {
            pnlKeypad.Controls.Clear();

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

            string[,] btns = new string[,]
            {
                { "7", "8", "9", "C" },
                { "4", "5", "6", "⌫" },
                { "1", "2", "3", "0" }
            };

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    string label = btns[r, c];
                    Button btn = new Button
                    {
                        Text = label,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(2),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TabStop = false, // Evita capturar Enter como pulsación repetida de este botón
                        BackColor = (label == "C" || label == "⌫") ? Color.FromArgb(254, 226, 226) : Color.White,
                        ForeColor = (label == "C" || label == "⌫") ? Color.FromArgb(185, 28, 28) : Color.FromArgb(15, 23, 42)
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                    if (char.IsDigit(label[0]))
                    {
                        int val = int.Parse(label);
                        btn.Click += (s, e) => { ProcesarEntradaDigito(val); ActualizarValores(); btnAgregarAccion.Focus(); };
                    }
                    else if (label == "C")
                    {
                        btn.Click += (s, e) => { _cantidad = 1; _esPrimerDigito = true; ActualizarValores(); btnAgregarAccion.Focus(); };
                    }
                    else if (label == "⌫")
                    {
                        btn.Click += (s, e) =>
                        {
                            _cantidad /= 10;
                            if (_cantidad <= 0) _cantidad = 1;
                            ActualizarValores();
                            btnAgregarAccion.Focus();
                        };
                    }

                    grid.Controls.Add(btn, c, r);
                }
            }

            pnlKeypad.Controls.Add(grid);
        }

        private void FormCantidadModal_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                int digito = e.KeyCode - Keys.D0;
                ProcesarEntradaDigito(digito);
                ActualizarValores();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
            {
                int digito = e.KeyCode - Keys.NumPad0;
                ProcesarEntradaDigito(digito);
                ActualizarValores();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Back)
            {
                _cantidad /= 10;
                if (_cantidad <= 0) _cantidad = 1;
                ActualizarValores();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ConfirmarYAgregar();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                e.SuppressKeyPress = true;
            }
        }

        private void ProcesarEntradaDigito(int digito)
        {
            int nuevaCant;
            if (_esPrimerDigito || _cantidad == 0)
            {
                nuevaCant = digito;
                _esPrimerDigito = false;
            }
            else
            {
                nuevaCant = (_cantidad * 10) + digito;
            }

            if (nuevaCant > _stockDisponible)
            {
                MessageBox.Show($"La cantidad ({nuevaCant}) supera el stock disponible ({_stockDisponible} un.).", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cantidad = _stockDisponible;
            }
            else
            {
                _cantidad = nuevaCant;
            }
        }

        private void ConfirmarYAgregar()
        {
            if (_cantidad <= 0) _cantidad = 1;
            if (_cantidad > _stockDisponible)
            {
                MessageBox.Show($"La cantidad se ajustó al stock máximo disponible ({_stockDisponible} un.).", "Ajuste de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cantidad = _stockDisponible;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ActualizarValores()
        {
            lblCantidadDisplay.Text = _cantidad.ToString();
            lblTotalPreview.Text = $"${_precioUnitario * _cantidad:N0}";
            btnAgregarAccion.Text = $"🛒 Agregar {_cantidad} a la venta";
        }
    }

    // =========================================================================
    // MODAL DE SELECCIÓN RÁPIDA DE CLIENTE
    // =========================================================================
    public class FormSeleccionarClienteModal : Form
    {
        private AppDbContext _db = new AppDbContext();
        public string NombreCliente { get; private set; } = "Consumidor Final";
        public string RutCliente { get; private set; } = "";

        public FormSeleccionarClienteModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Asignar Cliente a la Venta";
            this.Size = new Size(380, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.BackColor = Color.White;

            Label lblT = new Label { Text = "👤 Asignar Cliente", Location = new Point(20, 15), Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true };

            Label lblRut = new Label { Text = "RUT / DNI Cliente:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtRut = new TextBox { Text = "76.543.210-K", Location = new Point(20, 70), Size = new Size(320, 26), Font = new Font("Segoe UI", 9.5F) };

            Label lblNom = new Label { Text = "Razón Social / Nombre Completo:", Location = new Point(20, 105), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtNom = new TextBox { Text = "CLIENTE DE PRUEBA SPALD", Location = new Point(20, 125), Size = new Size(320, 26), Font = new Font("Segoe UI", 9.5F) };

            Button btnConfirmar = new Button { Text = "✔ Confirmar Cliente", Location = new Point(20, 180), Size = new Size(320, 42), BackColor = Color.FromArgb(0, 102, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, DialogResult = DialogResult.OK };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += (s, e) =>
            {
                NombreCliente = txtNom.Text.Trim();
                RutCliente = txtRut.Text.Trim();
            };

            Button btnAnonimo = new Button { Text = "👤 Consumidor Final (Sin RUT)", Location = new Point(20, 228), Size = new Size(320, 30), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(51, 65, 85), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand, DialogResult = DialogResult.OK };
            btnAnonimo.FlatAppearance.BorderSize = 0;
            btnAnonimo.Click += (s, e) =>
            {
                NombreCliente = "Consumidor Final";
                RutCliente = "";
            };

            this.Controls.Add(lblT);
            this.Controls.Add(lblRut);
            this.Controls.Add(txtRut);
            this.Controls.Add(lblNom);
            this.Controls.Add(txtNom);
            this.Controls.Add(btnConfirmar);
            this.Controls.Add(btnAnonimo);

            this.AcceptButton = btnConfirmar;
        }
    }
}