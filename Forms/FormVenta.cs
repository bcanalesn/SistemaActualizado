using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private static Dictionary<string, List<DetalleCarrito>> _carritosPorVendedor = new Dictionary<string, List<DetalleCarrito>>();

        private AppDbContext _db = new AppDbContext();
        private List<Producto> _productosCache = new List<Producto>();
        private VentaService _ventaService = new VentaService();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private ComboBox cbVendedor = null!;

        private FlowLayoutPanel flowCategorias = null!;
        private FlowLayoutPanel pnlProductosGrid = null!;
        private Panel pnlProdContainer = null!;
        private Label lblCategoriaTitulo = null!;

        private Label lblTituloCarro = null!;
        private Label lblCantItemsBadge = null!;
        private Button btnLimpiarCarro = null!;
        private FlowLayoutPanel flowCarritoItems = null!;

        private Label lblSubtotalValue = null!;
        private Label lblDescuentoValue = null!;
        private Label lblTotalValue = null!;
        private Button btnCliente = null!;
        private Button btnGenerarTicket = null!;

        private string _clienteSeleccionadoNombre = "Consumidor Final";
        private string _clienteSeleccionadoRut = "";

        private Usuario? _usuarioActual;
        private static string _vendedorActualNombre = "Bárbara";
        private string _categoriaActivaNombre = "Todas";

        public FormVenta(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            CargarProductosDesdeBD();
            RestaurarCarritoVendedorActual();

            this.KeyPreview = true;
            this.KeyDown += FormVenta_KeyDown;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

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
            pnlLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

            Panel pnlIzquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };

            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

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
                Location = new Point(38, 7),
                Size = new Size(305, 24),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            txtBuscar.Enter += (s, e) => { if (txtBuscar.Text.StartsWith("Buscar")) { txtBuscar.Text = ""; txtBuscar.ForeColor = Color.Black; } };
            txtBuscar.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) { txtBuscar.Text = "Buscar producto por nombre, código o barra..."; txtBuscar.ForeColor = Color.Gray; } };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BuscarYAgregarProducto(); e.SuppressKeyPress = true; } };
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

        // Mantenemos los clientes asignados por cada vendedor en memoria
        private static Dictionary<string, (string Nombre, string Rut)> _clientesPorVendedor = new Dictionary<string, (string, string)>();

        private void CbVendedor_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _vendedorActualNombre = cbVendedor.SelectedItem?.ToString() ?? "Bárbara";

            // Cargar el cliente correspondiente al vendedor seleccionado
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
                Tag = new Tuple<Color, Color>(back, fore)
            };
            btn.FlatAppearance.BorderSize = seleccionada ? 2 : 0;
            if (seleccionada)
            {
                btn.FlatAppearance.BorderColor = Color.FromArgb(0, 80, 200);
            }

            btn.Click += (s, e) =>
            {
                foreach (Control c in flowCategorias.Controls)
                {
                    if (c is Button b && b.Tag is Tuple<Color, Color> origColors)
                    {
                        b.BackColor = origColors.Item1;
                        b.ForeColor = origColors.Item2;
                        b.FlatAppearance.BorderSize = 0;
                    }
                }

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

            Control ctrlImagen;
            if (!string.IsNullOrEmpty(prod.ImagenPath) && System.IO.File.Exists(prod.ImagenPath))
            {
                PictureBox pb = new PictureBox
                {
                    Location = new Point(8, 8),
                    Size = new Size(42, 42),
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
                    Font = new Font("Segoe UI", 18F),
                    Location = new Point(10, 10),
                    Size = new Size(40, 40),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(241, 245, 249)
                };
            }

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
            ctrlImagen.Click += (s, e) => onClick();
            lblNombre.Click += (s, e) => onClick();
            lblPrecio.Click += (s, e) => onClick();
            lblCodigo.Click += (s, e) => onClick();
            lblStock.Click += (s, e) => onClick();

            card.Controls.Add(ctrlImagen);
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

            using (FormCantidadModal modal = new FormCantidadModal(prod.Nombre, prod.PrecioUnitario, cantidadInicial, prod.Stock, prod.ImagenPath))
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
            using (FormSeleccionarClienteModal modalCliente = new FormSeleccionarClienteModal(_clienteSeleccionadoRut))
            {
                if (modalCliente.ShowDialog(this) == DialogResult.OK)
                {
                    _clienteSeleccionadoNombre = modalCliente.NombreCliente;
                    _clienteSeleccionadoRut = modalCliente.RutCliente;

                    // Guardar el cliente específico para este vendedor
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

            try
            {
                // Delegamos el proceso al servicio de ventas
                int nroTicket = _ventaService.GenerarTicketVenta(cart, vendedorNombre, _clienteSeleccionadoNombre, _clienteSeleccionadoRut);

                // Sincronizar cache local de productos con los nuevos stocks
                foreach (var item in cart)
                {
                    var prodCache = _productosCache.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (prodCache != null)
                    {
                        prodCache.Stock -= item.Cantidad;
                        if (prodCache.Stock < 0) prodCache.Stock = 0;
                    }
                }

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
}