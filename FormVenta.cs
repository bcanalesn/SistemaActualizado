using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public partial class FormVenta : Form
    {
        private AppDbContext _db = new AppDbContext();
        private List<DetalleCarrito> _carrito = new List<DetalleCarrito>();
        private List<Producto> _productosCache = new List<Producto>();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private DataGridView dgvCarrito = null!;

        private Label lblTotal = null!;
        private ComboBox cbVendedor = null!;
        private Button btnGenerarTicket = null!;
        private Button btnLimpiarCarro = null!;

        private FlowLayoutPanel pnlProductosGrid = null!;
        private Panel pnlProdContainer = null!;

        private Usuario? _usuarioActual;

        public FormVenta(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            CargarProductosDesdeBD();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            // Panel Principal dividido en 2 columnas (Izquierda: Catálogo | Derecha: Pre-ticket / Carrito)
            TableLayoutPanel pnlLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            pnlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            pnlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // ==========================================
            // COLUMNA IZQUIERDA: BÚSQUEDA Y PRODUCTOS
            // ==========================================
            Panel pnlIzquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };

            // 1. BARRA SUPERIOR DE BÚSQUEDA Y VENDEDOR
            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            Label lblVendedorTag = new Label
            {
                Text = "👤 Vendedor:",
                Location = new Point(12, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            cbVendedor = new ComboBox
            {
                Location = new Point(90, 18),
                Size = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbVendedor.Items.AddRange(new string[] { "Bárbara", "Víctor", "Juan", "María" });
            cbVendedor.SelectedItem = _usuarioActual?.NombreCompleto ?? "Bárbara";

            txtBuscar = new TextBox
            {
                Location = new Point(230, 18),
                Size = new Size(240, 28),
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BuscarYAgregarProducto(); e.SuppressKeyPress = true; } };

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(478, 17),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => BuscarYAgregarProducto();

            pnlTopBar.Controls.Add(lblVendedorTag);
            pnlTopBar.Controls.Add(cbVendedor);
            pnlTopBar.Controls.Add(txtBuscar);
            pnlTopBar.Controls.Add(btnBuscar);

            // 2. CATEGORÍAS RÁPIDAS
            Panel pnlCategorias = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(0, 8, 0, 8) };
            FlowLayoutPanel flowCat = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = false };

            flowCat.Controls.Add(CrearBotonCategoria("🥛 Lácteos", Color.FromArgb(224, 242, 254), Color.FromArgb(3, 105, 161)));
            flowCat.Controls.Add(CrearBotonCategoria("🥩 Cecinas", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28)));
            flowCat.Controls.Add(CrearBotonCategoria("🌾 Abarrotes", Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)));
            flowCat.Controls.Add(CrearBotonCategoria("🥤 Bebidas", Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61)));
            flowCat.Controls.Add(CrearBotonCategoria("🧹 Aseo", Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)));

            pnlCategorias.Controls.Add(flowCat);

            // 3. GRILLA DE PRODUCTOS
            pnlProdContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            pnlProductosGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnlProdContainer.Controls.Add(pnlProductosGrid);

            pnlIzquierda.Controls.Add(pnlProdContainer);
            pnlIzquierda.Controls.Add(pnlCategorias);
            pnlIzquierda.Controls.Add(pnlTopBar);

            // ==========================================
            // COLUMNA DERECHA: CARRITO Y BOTOÓN TICKET
            // ==========================================
            Panel pnlDerecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };

            Label lblTituloCarro = new Label
            {
                Text = "📋 TICKET DE ATENCIÓN (PRE-VENTA)",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 30
            };

            dgvCarrito = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvCarrito);

            // Botones de Modificación del Carrito
            Panel pnlAccionesCarro = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(0, 5, 0, 5) };
            Button btnMas = CrearBotonAccionItem("➕ Sumar", Color.FromArgb(241, 245, 249), Color.FromArgb(15, 23, 42), 0);
            Button btnMenos = CrearBotonAccionItem("➖ Restar", Color.FromArgb(241, 245, 249), Color.FromArgb(15, 23, 42), 95);
            Button btnQuitar = CrearBotonAccionItem("🗑️ Eliminar", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28), 190);

            btnMas.Click += (s, e) => ModificarCantidadSeleccionada(1);
            btnMenos.Click += (s, e) => ModificarCantidadSeleccionada(-1);
            btnQuitar.Click += (s, e) => EliminarItemSeleccionado();

            pnlAccionesCarro.Controls.Add(btnMas);
            pnlAccionesCarro.Controls.Add(btnMenos);
            pnlAccionesCarro.Controls.Add(btnQuitar);

            // PANEL INFERIOR DE TOTAL Y GENERACIÓN DE TICKET
            Panel pnlBottomCheckout = new Panel { Dock = DockStyle.Bottom, Height = 140, Padding = new Padding(0, 10, 0, 0) };

            Panel pnlTotalCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(10)
            };

            Label lblTotalCap = new Label { Text = "TOTAL ESTIMADO:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161), Location = new Point(10, 16), AutoSize = true };
            lblTotal = new Label { Text = "$ 0", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199), Location = new Point(140, 10), AutoSize = true };
            pnlTotalCard.Controls.Add(lblTotalCap);
            pnlTotalCard.Controls.Add(lblTotal);

            btnGenerarTicket = new Button
            {
                Text = "🎟️ GENERAR TICKET DE ATENCIÓN",
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerarTicket.FlatAppearance.BorderSize = 0;
            btnGenerarTicket.Click += BtnGenerarTicket_Click;

            btnLimpiarCarro = new Button
            {
                Text = "🔄 Limpiar Carro",
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiarCarro.FlatAppearance.BorderSize = 0;
            btnLimpiarCarro.Click += (s, e) => LimpiarCarrito();

            pnlBottomCheckout.Controls.Add(btnGenerarTicket);
            pnlBottomCheckout.Controls.Add(btnLimpiarCarro);
            pnlBottomCheckout.Controls.Add(pnlTotalCard);

            pnlDerecha.Controls.Add(dgvCarrito);
            pnlDerecha.Controls.Add(pnlAccionesCarro);
            pnlDerecha.Controls.Add(pnlBottomCheckout);
            pnlDerecha.Controls.Add(lblTituloCarro);

            pnlLayout.Controls.Add(pnlIzquierda, 0, 0);
            pnlLayout.Controls.Add(pnlDerecha, 1, 0);

            this.Controls.Add(pnlLayout);
            this.ResumeLayout(false);
        }

        private Button CrearBotonCategoria(string texto, Color back, Color fore)
        {
            Button btn = new Button
            {
                Text = texto,
                Size = new Size(110, 32),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => FiltrarProductosPorCategoria(texto.Split(' ')[1], back);
            return btn;
        }

        private Button CrearBotonAccionItem(string texto, Color back, Color fore, int left)
        {
            Button btn = new Button
            {
                Text = texto,
                Location = new Point(left, 4),
                Size = new Size(88, 32),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
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

            FiltrarProductosPorCategoria("Lácteos", Color.FromArgb(224, 242, 254));
        }

        private void FiltrarProductosPorCategoria(string categoria, Color colorFondo)
        {
            pnlProductosGrid.Controls.Clear();
            pnlProductosGrid.BackColor = colorFondo;
            pnlProdContainer.BackColor = colorFondo;

            var filtrados = _productosCache
                .Where(p => p.Nombre.ToLower().Contains(categoria.ToLower()) ||
                            (categoria == "Lácteos" && (p.Nombre.Contains("Leche") || p.Nombre.Contains("Yogurt") || p.Nombre.Contains("Queso"))) ||
                            (categoria == "Cecinas" && (p.Nombre.Contains("Jamón") || p.Nombre.Contains("Salchicha") || p.Nombre.Contains("Salame"))) ||
                            (categoria == "Bebidas" && (p.Nombre.Contains("Bebida") || p.Nombre.Contains("Sprite") || p.Nombre.Contains("Coca") || p.Nombre.Contains("Jugo"))) ||
                            (categoria == "Abarrotes" && (p.Nombre.Contains("Azúcar") || p.Nombre.Contains("Aceite") || p.Nombre.Contains("Café") || p.Nombre.Contains("Galletas"))) ||
                            (categoria == "Aseo" && (p.Nombre.Contains("Detergente") || p.Nombre.Contains("Cloro") || p.Nombre.Contains("Lavaloza") || p.Nombre.Contains("Papel"))))
                .ToList();

            foreach (var prod in filtrados)
            {
                Button btnCard = new Button
                {
                    Text = $"{prod.Nombre}\n\n${prod.PrecioUnitario:N0}",
                    Size = new Size(150, 68),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(4)
                };
                btnCard.FlatAppearance.BorderColor = Color.FromArgb(180, 200, 220);
                btnCard.FlatAppearance.BorderSize = 1;
                btnCard.Click += (s, e) => AgregarProductoAlCarrito(prod, 1);
                pnlProductosGrid.Controls.Add(btnCard);
            }
        }

        private void BuscarYAgregarProducto()
        {
            string query = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            var prod = _productosCache.FirstOrDefault(p => p.CodigoBarra.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                                                           p.Nombre.ToLower().Contains(query.ToLower()));

            if (prod != null)
            {
                AgregarProductoAlCarrito(prod, 1);
                txtBuscar.Clear();
                txtBuscar.Focus();
            }
            else
            {
                MessageBox.Show($"No se encontró el producto '{query}'.", "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AgregarProductoAlCarrito(Producto prod, int cant)
        {
            var item = _carrito.FirstOrDefault(c => c.ProductoID == prod.ProductoID);
            if (item != null)
            {
                item.Cantidad += cant;
            }
            else
            {
                _carrito.Add(new DetalleCarrito
                {
                    ProductoID = prod.ProductoID,
                    Nombre = prod.Nombre,
                    PrecioUnitario = prod.PrecioUnitario,
                    Cantidad = cant
                });
            }

            ActualizarCarritoUI();
        }

        private void ModificarCantidadSeleccionada(int delta)
        {
            if (dgvCarrito.CurrentRow?.DataBoundItem is DetalleCarrito item)
            {
                item.Cantidad += delta;
                if (item.Cantidad <= 0) _carrito.Remove(item);
                ActualizarCarritoUI();
            }
        }

        private void EliminarItemSeleccionado()
        {
            if (dgvCarrito.CurrentRow?.DataBoundItem is DetalleCarrito item)
            {
                _carrito.Remove(item);
                ActualizarCarritoUI();
            }
        }

        private void ActualizarCarritoUI()
        {
            dgvCarrito.DataSource = null;
            dgvCarrito.DataSource = _carrito.ToList();

            if (dgvCarrito.Columns["ProductoID"] != null) dgvCarrito.Columns["ProductoID"].Visible = false;
            if (dgvCarrito.Columns["Nombre"] != null) dgvCarrito.Columns["Nombre"].HeaderText = "Producto";
            if (dgvCarrito.Columns["PrecioUnitario"] != null) { dgvCarrito.Columns["PrecioUnitario"].HeaderText = "Precio"; dgvCarrito.Columns["PrecioUnitario"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvCarrito.Columns["Cantidad"] != null) dgvCarrito.Columns["Cantidad"].HeaderText = "Cant.";
            if (dgvCarrito.Columns["Subtotal"] != null) { dgvCarrito.Columns["Subtotal"].HeaderText = "Subtotal"; dgvCarrito.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0"; dgvCarrito.Columns["Subtotal"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }

            decimal total = _carrito.Sum(c => c.Subtotal);
            lblTotal.Text = $"$ {total:N0}";
        }

        private void LimpiarCarrito()
        {
            _carrito.Clear();
            ActualizarCarritoUI();
        }

        private void BtnGenerarTicket_Click(object? sender, EventArgs e)
        {
            if (_carrito.Count == 0)
            {
                MessageBox.Show("El carro de compras está vacío.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string vendedorNombre = cbVendedor.SelectedItem?.ToString() ?? "Bárbara";
            decimal total = _carrito.Sum(c => c.Subtotal);
            int nroTicket = (int)(DateTime.Now.Ticks % 1000000);

            try
            {
                // Guardar Ticket de Atención (Pre-Venta) en TVE2607 con status = "Pendiente"
                TVE2607 tve = new TVE2607
                {
                    idLocal = 1,
                    nmbLocal = "Local Principal",
                    iddocDTE = 0, // 0 = Ticket de Atención Pendiente de Pago
                    Documento = "Ticket de Atención",
                    nroDTE = nroTicket,
                    FecDoc = DateTime.Now,
                    SubTotal = Math.Round(total / 1.19m, 0),
                    Neto = Math.Round(total / 1.19m, 0),
                    IvA = total - Math.Round(total / 1.19m, 0),
                    Total = total,
                    UserDTE = vendedorNombre,
                    Vendedor = vendedorNombre,
                    status = "Pendiente" // Pendiente de cobro en caja
                };

                _db.TVE2607.Add(tve);
                _db.SaveChanges(); // Genera tve.idTve

                // Guardar ítems en TVD2607
                foreach (var item in _carrito)
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
                }

                _db.SaveChanges();

                MessageBox.Show($"🎟️ TICKET DE ATENCIÓN N° {nroTicket:D6} GENERADO CON ÉXITO\n\n" +
                                $"• Vendedor: {vendedorNombre}\n" +
                                $"• Total a Pagar en Caja: ${total:N0}\n\n" +
                                $"Indique al cliente pasar a CAJA con el N° de Ticket {nroTicket:D6}.",
                                "Pre-Venta Registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimpiarCarrito();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar Ticket de Atención: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 30;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }
    }
}