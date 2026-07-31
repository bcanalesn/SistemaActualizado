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

        private TextBox txtCodigoBarra = null!;
        private NumericUpDown numCantidad = null!;
        private Button btnBuscar = null!;
        private Button btnAgregar = null!;
        private DataGridView dgvCarrito = null!;

        private FlowLayoutPanel pnlCategorias = null!;
        private Panel pnlProdContainer = null!;
        private FlowLayoutPanel pnlProductosGrid = null!;

        private Label lblTotalPagar = null!;
        private ComboBox cbMedioPago = null!;
        private Panel pnlPagoEfectivo = null!;
        private TextBox txtPagoCon = null!;
        private Label lblVuelto = null!;
        private Button btnProcesarCobro = null!;
        private Button btnCancelarVenta = null!;

        private string _categoriaSeleccionada = "";

        public FormVenta(Usuario? usuario = null)
        {
            InitializeComponent();
            CargarProductosEInventario();
            GenerarBotonesCategorias();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            // Contenedor Principal
            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            // ==========================================
            // SECCIÓN DERECHA: PANEL DE COBRO (ANCHO FIJO SEGURO 260px)
            // ==========================================
            Panel pnlDerechoCard = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            Label lblTituloResumen = new Label
            {
                Text = "RESUMEN DE VENTA",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Panel pnlCardTotal = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, 6, 0, 6),
                Padding = new Padding(10)
            };

            Label lblTotalLabel = new Label
            {
                Text = "TOTAL A PAGAR",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 16
            };

            lblTotalPagar = new Label
            {
                Text = "$ 0",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlCardTotal.Controls.Add(lblTotalPagar);
            pnlCardTotal.Controls.Add(lblTotalLabel);

            Label lblMedioPago = new Label
            {
                Text = "Medio de Pago:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(12, 128),
                AutoSize = true
            };

            cbMedioPago = new ComboBox
            {
                Location = new Point(12, 148),
                Size = new Size(230, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbMedioPago.Items.AddRange(new string[] { "Efectivo", "Tarjeta Débito / Crédito", "Transferencia / QR" });
            cbMedioPago.SelectedIndex = 0;
            cbMedioPago.SelectedIndexChanged += (s, e) => ToggleMedioPago();

            pnlPagoEfectivo = new Panel
            {
                Location = new Point(12, 182),
                Size = new Size(230, 90),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            Label lblPagoCon = new Label { Text = "PAGA CON ($):", Location = new Point(8, 6), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            txtPagoCon = new TextBox { Location = new Point(8, 24), Size = new Size(214, 26), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            txtPagoCon.TextChanged += (s, e) => CalcularVuelto();

            Label lblVueltoLabel = new Label { Text = "VUELTO:", Location = new Point(8, 60), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            lblVuelto = new Label { Text = "$ 0", Location = new Point(70, 58), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };

            pnlPagoEfectivo.Controls.AddRange(new Control[] { lblPagoCon, txtPagoCon, lblVueltoLabel, lblVuelto });

            btnProcesarCobro = new Button
            {
                Text = "💳 PROCESAR COBRO",
                Location = new Point(12, 282),
                Size = new Size(230, 46),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnProcesarCobro.FlatAppearance.BorderSize = 0;
            btnProcesarCobro.Click += BtnProcesarCobro_Click;

            btnCancelarVenta = new Button
            {
                Text = "❌ Cancelar Venta",
                Location = new Point(12, 336),
                Size = new Size(230, 30),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelarVenta.FlatAppearance.BorderSize = 0;
            btnCancelarVenta.Click += (s, e) => LimpiarCarrito();

            pnlDerechoCard.Controls.Add(btnCancelarVenta);
            pnlDerechoCard.Controls.Add(btnProcesarCobro);
            pnlDerechoCard.Controls.Add(pnlPagoEfectivo);
            pnlDerechoCard.Controls.Add(cbMedioPago);
            pnlDerechoCard.Controls.Add(lblMedioPago);
            pnlDerechoCard.Controls.Add(pnlCardTotal);
            pnlDerechoCard.Controls.Add(lblTituloResumen);

            // ==========================================
            // SECCIÓN IZQUIERDA: BÚSQUEDA, CATEGORÍAS Y CARRITO
            // ==========================================
            Panel pnlIzquierdo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };

            FlowLayoutPanel pnlBusquedaCard = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(8, 10, 8, 8),
                WrapContents = false,
                AutoScroll = false
            };

            Label lblEscaneo = new Label
            {
                Text = "🔍 BÚSQUEDA:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Margin = new Padding(2, 6, 4, 0),
                AutoSize = true
            };

            txtCodigoBarra = new TextBox
            {
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 4, 0)
            };
            txtCodigoBarra.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AgregarProductoAlCarrito(txtCodigoBarra.Text.Trim(), (int)numCantidad.Value); e.SuppressKeyPress = true; } };

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Size = new Size(75, 28),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => AgregarProductoAlCarrito(txtCodigoBarra.Text.Trim(), (int)numCantidad.Value);

            Label lblCant = new Label
            {
                Text = "CANT:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Margin = new Padding(2, 6, 4, 0),
                AutoSize = true
            };

            numCantidad = new NumericUpDown
            {
                Size = new Size(50, 28),
                Font = new Font("Segoe UI", 9.5F),
                Value = 1,
                Minimum = 1,
                Maximum = 100,
                Margin = new Padding(0, 2, 6, 0)
            };

            btnAgregar = new Button
            {
                Text = "➕ Agregar",
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += (s, e) => AgregarProductoAlCarrito(txtCodigoBarra.Text.Trim(), (int)numCantidad.Value);

            pnlBusquedaCard.Controls.Add(lblEscaneo);
            pnlBusquedaCard.Controls.Add(txtCodigoBarra);
            pnlBusquedaCard.Controls.Add(btnBuscar);
            pnlBusquedaCard.Controls.Add(lblCant);
            pnlBusquedaCard.Controls.Add(numCantidad);
            pnlBusquedaCard.Controls.Add(btnAgregar);

            Panel pnlCatContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Margin = new Padding(0, 6, 0, 0),
                BackColor = Color.Transparent
            };

            pnlCategorias = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 4)
            };
            pnlCatContainer.Controls.Add(pnlCategorias);

            pnlProdContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = Color.White,
                Padding = new Padding(6),
                Visible = false
            };

            pnlProductosGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlProdContainer.Controls.Add(pnlProductosGrid);

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 6, 0, 0)
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

            // Botones Rápidos para Editar Ítems
            Panel pnlAccionesCarrito = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            Button btnSumar = new Button
            {
                Text = "➕ Sumar 1",
                Location = new Point(0, 4),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(224, 242, 254),
                ForeColor = Color.FromArgb(3, 105, 161),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSumar.FlatAppearance.BorderSize = 0;
            btnSumar.Click += (s, e) => ModificarCantidadSeleccionada(1);

            Button btnRestar = new Button
            {
                Text = "➖ Restar 1",
                Location = new Point(102, 4),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(254, 243, 199),
                ForeColor = Color.FromArgb(180, 83, 9),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRestar.FlatAppearance.BorderSize = 0;
            btnRestar.Click += (s, e) => ModificarCantidadSeleccionada(-1);

            Button btnQuitar = new Button
            {
                Text = "🗑️ Eliminar",
                Location = new Point(204, 4),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuitar.FlatAppearance.BorderSize = 0;
            btnQuitar.Click += (s, e) => ModificarCantidadSeleccionada(-999);

            pnlAccionesCarrito.Controls.Add(btnSumar);
            pnlAccionesCarrito.Controls.Add(btnRestar);
            pnlAccionesCarrito.Controls.Add(btnQuitar);

            pnlGridCard.Controls.Add(dgvCarrito);
            pnlGridCard.Controls.Add(pnlAccionesCarrito);

            pnlIzquierdo.Controls.Add(pnlGridCard);
            pnlIzquierdo.Controls.Add(pnlProdContainer);
            pnlIzquierdo.Controls.Add(pnlCatContainer);
            pnlIzquierdo.Controls.Add(pnlBusquedaCard);

            // Agregar a contenedor principal en orden estricto de Docking
            pnlMain.Controls.Add(pnlIzquierdo);
            pnlMain.Controls.Add(pnlDerechoCard);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void GenerarBotonesCategorias()
        {
            pnlCategorias.Controls.Clear();

            // Lista de categorías con colores personalizados para estado inactivo y activo
            var categorias = new List<(string Nombre, string Icono, Color ColorInactivo, Color ColorTexto, Color ColorActivo)>
            {
                ("Lácteos", "🥛", Color.FromArgb(224, 242, 254), Color.FromArgb(3, 105, 161), Color.FromArgb(2, 132, 199)),
                ("Cecinas", "🥓", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28), Color.FromArgb(220, 38, 38)),
                ("Abarrotes", "🌾", Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9), Color.FromArgb(217, 119, 6)),
                ("Bebidas", "🥤", Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61), Color.FromArgb(22, 163, 74)),
                ("Aseo", "🧹", Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206), Color.FromArgb(147, 51, 234))
            };

            foreach (var cat in categorias)
            {
                Button btn = new Button
                {
                    Text = $"{cat.Icono} {cat.Nombre}",
                    Size = new Size(110, 34),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3, 0, 3, 0),
                    Tag = cat
                };
                btn.FlatAppearance.BorderSize = 0;

                ActualizarEstiloBotonCategoria(btn, false);

                btn.Click += (s, e) =>
                {
                    if (_categoriaSeleccionada == cat.Nombre)
                    {
                        _categoriaSeleccionada = "";
                        pnlProdContainer.Visible = false;
                    }
                    else
                    {
                        _categoriaSeleccionada = cat.Nombre;
                        pnlProdContainer.Visible = true;
                        FiltrarProductosPorCategoria(_categoriaSeleccionada);
                    }

                    foreach (Control ctrl in pnlCategorias.Controls)
                    {
                        if (ctrl is Button b)
                        {
                            if (b.Tag is ValueTuple<string, string, Color, Color, Color> tuple)
                            {
                                ActualizarEstiloBotonCategoria(b, tuple.Item1 == _categoriaSeleccionada);
                            }
                        }
                    }
                };

                pnlCategorias.Controls.Add(btn);
            }
        }

        private void ActualizarEstiloBotonCategoria(Button btn, bool esSeleccionado)
        {
            if (btn.Tag is ValueTuple<string, string, Color, Color, Color> cat)
            {
                if (esSeleccionado)
                {
                    btn.BackColor = cat.Item5; // Color activo brillante
                    btn.ForeColor = Color.White;
                }
                else
                {
                    btn.BackColor = cat.Item3; // Color inactivo suave / pastel
                    btn.ForeColor = cat.Item4; // Color de texto distintivo
                }
            }
        }

        private void FiltrarProductosPorCategoria(string categoria)
        {
            pnlProductosGrid.Controls.Clear();

            List<Producto> filtrados = _productosCache
                .Where(p => p.Nombre.ToLower().Contains(categoria.ToLower()) ||
                            (categoria == "Lácteos" && (p.Nombre.Contains("Leche") || p.Nombre.Contains("Yogurt") || p.Nombre.Contains("Queso"))) ||
                            (categoria == "Cecinas" && (p.Nombre.Contains("Jamón") || p.Nombre.Contains("Queso"))) ||
                            (categoria == "Bebidas" && (p.Nombre.Contains("Bebida") || p.Nombre.Contains("Sprite") || p.Nombre.Contains("Coca"))) ||
                            (categoria == "Abarrotes" && (p.Nombre.Contains("Azúcar") || p.Nombre.Contains("Aceite") || p.Nombre.Contains("Café") || p.Nombre.Contains("Galletas"))))
                .ToList();

            foreach (var prod in filtrados)
            {
                Button btnCard = new Button
                {
                    Text = $"{prod.Nombre}\n${prod.PrecioUnitario:N0}",
                    Size = new Size(115, 50),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3)
                };
                btnCard.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                btnCard.FlatAppearance.BorderSize = 1;

                btnCard.Click += (s, e) => AgregarProductoDirecto(prod, 1);
                pnlProductosGrid.Controls.Add(btnCard);
            }
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 28;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProductosEInventario()
        {
            try
            {
                _productosCache = _db.Productos.Where(p => p.Estado).ToList();

                var coleccion = new AutoCompleteStringCollection();
                foreach (var p in _productosCache)
                {
                    if (!string.IsNullOrEmpty(p.Nombre)) coleccion.Add(p.Nombre);
                    if (!string.IsNullOrEmpty(p.CodigoBarra)) coleccion.Add(p.CodigoBarra);
                }

                txtCodigoBarra.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCodigoBarra.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtCodigoBarra.AutoCompleteCustomSource = coleccion;
            }
            catch { }
        }

        private void AgregarProductoDirecto(Producto prod, int cantidad)
        {
            var existente = _carrito.FirstOrDefault(c => c.ProductoID == prod.ProductoID);
            if (existente != null)
            {
                existente.Cantidad += cantidad;
            }
            else
            {
                _carrito.Add(new DetalleCarrito
                {
                    ProductoID = prod.ProductoID,
                    Nombre = prod.Nombre,
                    PrecioUnitario = prod.PrecioUnitario,
                    Cantidad = cantidad
                });
            }

            ActualizarTablaCarrito();
        }

        private void AgregarProductoAlCarrito(string codigoOBusqueda, int cantidad)
        {
            if (string.IsNullOrWhiteSpace(codigoOBusqueda)) return;

            var prod = _productosCache.FirstOrDefault(p =>
                p.CodigoBarra.Equals(codigoOBusqueda, StringComparison.OrdinalIgnoreCase) ||
                p.Nombre.Equals(codigoOBusqueda, StringComparison.OrdinalIgnoreCase));

            if (prod != null)
            {
                AgregarProductoDirecto(prod, cantidad);
                txtCodigoBarra.Clear();
                txtCodigoBarra.Focus();
                numCantidad.Value = 1;
            }
            else
            {
                MessageBox.Show($"No se encontró el producto '{codigoOBusqueda}'.", "Producto No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ModificarCantidadSeleccionada(int delta)
        {
            if (dgvCarrito.CurrentRow != null && dgvCarrito.CurrentRow.DataBoundItem is DetalleCarrito item)
            {
                item.Cantidad += delta;
                if (item.Cantidad <= 0)
                {
                    _carrito.Remove(item);
                }
                ActualizarTablaCarrito();
            }
        }

        private void ActualizarTablaCarrito()
        {
            dgvCarrito.DataSource = null;
            dgvCarrito.DataSource = _carrito;

            if (dgvCarrito.Columns["ProductoID"] != null) dgvCarrito.Columns["ProductoID"].Visible = false;
            if (dgvCarrito.Columns["Nombre"] != null) dgvCarrito.Columns["Nombre"].HeaderText = "Descripción";
            if (dgvCarrito.Columns["PrecioUnitario"] != null) { dgvCarrito.Columns["PrecioUnitario"].HeaderText = "Precio"; dgvCarrito.Columns["PrecioUnitario"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvCarrito.Columns["Cantidad"] != null) dgvCarrito.Columns["Cantidad"].HeaderText = "Cant.";
            if (dgvCarrito.Columns["Subtotal"] != null) { dgvCarrito.Columns["Subtotal"].HeaderText = "Subtotal"; dgvCarrito.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0"; }

            decimal total = _carrito.Sum(c => c.Subtotal);
            lblTotalPagar.Text = $"$ {total:N0}";
            CalcularVuelto();
        }

        private void ToggleMedioPago()
        {
            pnlPagoEfectivo.Visible = cbMedioPago.SelectedItem?.ToString() == "Efectivo";
        }

        private void CalcularVuelto()
        {
            decimal total = _carrito.Sum(c => c.Subtotal);
            if (decimal.TryParse(txtPagoCon.Text.Trim(), out decimal pagaCon))
            {
                decimal vuelto = pagaCon - total;
                lblVuelto.Text = vuelto >= 0 ? $"$ {vuelto:N0}" : "$ 0";
                lblVuelto.ForeColor = vuelto >= 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(220, 38, 38);
            }
            else
            {
                lblVuelto.Text = "$ 0";
            }
        }

        private void BtnProcesarCobro_Click(object? sender, EventArgs e)
        {
            if (_carrito.Count == 0)
            {
                MessageBox.Show("El carrito de compras está vacío.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal total = _carrito.Sum(c => c.Subtotal);
            decimal pagaCon = total;
            decimal vuelto = 0;

            if (cbMedioPago.SelectedItem?.ToString() == "Efectivo")
            {
                if (!decimal.TryParse(txtPagoCon.Text.Trim(), out pagaCon) || pagaCon < total)
                {
                    MessageBox.Show("El monto en 'Paga con' es insuficiente.", "Monto Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                vuelto = pagaCon - total;
            }

            try
            {
                Venta nuevaVenta = new Venta
                {
                    Fecha = DateTime.Now,
                    Total = total,
                    MedioPago = cbMedioPago.SelectedItem?.ToString() ?? "Efectivo"
                };

                _db.Ventas.Add(nuevaVenta);
                _db.SaveChanges();

                FormTicket ticket = new FormTicket(nuevaVenta.VentaID, nuevaVenta.Fecha, _carrito, total, pagaCon, vuelto, nuevaVenta.MedioPago);
                ticket.ShowDialog();

                LimpiarCarrito();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la venta: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarCarrito()
        {
            _carrito.Clear();
            txtPagoCon.Clear();
            ActualizarTablaCarrito();
        }
    }
}