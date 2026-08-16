using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        // 1. Buscador Inteligente Proveedor
        private Proveedor? _proveedorSeleccionado = null;
        private TextBox txtBuscarProveedor = null!;
        private Button btnLimpiarBuscadorProv = null!;
        private Button btnNuevoProveedor = null!;
        private Button btnEditarProveedor = null!;
        private ListBox lstSugerenciasProveedores = null!;
        private const string PLACEHOLDER_PROV = "🔍 Buscar por RUT o Razón Social...";

        // Controles Documento
        private ComboBox cbTipoDocumento = null!;
        private TextBox txtNroDocumento = null!;
        private DateTimePicker dtpFechaRecepcion = null!;
        private Button btnAgregarDocumento = null!;

        // 2. Buscador Inteligente Producto
        private Producto? _productoSeleccionado = null;
        private TextBox txtBuscarProducto = null!;
        private Button btnLimpiarBuscadorProd = null!;
        private Button btnNuevoProducto = null!;
        private Button btnEditarProducto = null!;
        private ListBox lstSugerenciasProductos = null!;
        private const string PLACEHOLDER_PROD = "🔍 Buscar por Código o Nombre...";

        // Controles de Ítems
        private TextBox txtCantidadRecibida = null!; // Sin flechas
        private TextBox txtPrecioCosto = null!;
        private Label lblPreviewPVP = null!;
        private Button btnAgregarItem = null!;

        // Grilla y Totales
        private DataGridView dgvDetalle = null!;
        private Label lblTotalNeto = null!;
        private Label lblIvaCredito = null!;
        private Label lblTotalBruto = null!;
        private Button btnRegistrarCompra = null!;
        private Button btnLimpiarFormulario = null!;

        public FormCompras()
        {
            InitializeComponent();
            CargarDatosIniciales();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 20),
                AutoScroll = true
            };

            // ==========================================
            // TARJETA PASO 1: DATOS DEL DOCUMENTO
            // ==========================================
            Panel pnlPaso1 = CrearTarjeta(80, "PASO 1: DATOS DEL DOCUMENTO DE COMPRA", Color.FromArgb(30, 41, 59));
            FlowLayoutPanel flowHeader = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false };

            Panel pnlProvContainer = new Panel { Size = new Size(345, 52), Margin = new Padding(0, 0, 8, 0) };
            Label lblP = new Label { Text = "1. Proveedor (RUT o Nombre)", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };

            txtBuscarProveedor = new TextBox
            {
                Size = new Size(215, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = PLACEHOLDER_PROV,
                Location = new Point(0, 16)
            };
            ConfigurarEventosBuscadorProveedor();

            btnLimpiarBuscadorProv = CrearBotonIcono("🗑️", 217, 16, (s, e) => ResetearBuscadorProveedor());
            btnNuevoProveedor = CrearBotonAccionRapida("➕", Color.FromArgb(2, 132, 199), 246, 16, BtnNuevoProveedor_Click);
            btnEditarProveedor = CrearBotonAccionRapida("✏️", Color.FromArgb(245, 158, 11), 276, 16, BtnEditarProveedor_Click);

            pnlProvContainer.Controls.AddRange(new Control[] { lblP, txtBuscarProveedor, btnLimpiarBuscadorProv, btnNuevoProveedor, btnEditarProveedor });

            cbTipoDocumento = new ComboBox { Size = new Size(140, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTipoDocumento.Items.AddRange(new string[] { "Factura de Compra", "Guía de Recepción" });
            cbTipoDocumento.SelectedIndex = 0;

            txtNroDocumento = new TextBox { Size = new Size(95, 28), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtNroDocumento.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtNroDocumento.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtBuscarProducto.Focus(); };

            dtpFechaRecepcion = new DateTimePicker { Size = new Size(105, 28), Format = DateTimePickerFormat.Short };

            // Reemplaza btnAgregarDocumento por este botón:
            btnAgregarDocumento = new Button
            {
                Text = "📋 Lista Proveedores",
                Size = new Size(145, 28),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 16, 0, 0)
            };
            btnAgregarDocumento.FlatAppearance.BorderSize = 0;
            btnAgregarDocumento.Click += BtnListaProveedores_Click;

            flowHeader.Controls.AddRange(new Control[] {
                pnlProvContainer,
                CrearCampo("2. Tipo Doc.", cbTipoDocumento),
                CrearCampo("3. N° de Documento", txtNroDocumento),
                CrearCampo("4. Fecha Emisión", dtpFechaRecepcion),
                btnAgregarDocumento
            });
            pnlPaso1.Controls.Add(flowHeader);

            // ==========================================
            // TARJETA PASO 2: AGREGAR PRODUCTOS
            // ==========================================
            Panel pnlPaso2 = CrearTarjeta(85, "PASO 2: AGREGAR PRODUCTOS A LA FACTURA", Color.FromArgb(2, 132, 199));
            pnlPaso2.Margin = new Padding(0, 10, 0, 10);
            FlowLayoutPanel flowItem = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };

            // Panel Buscador Producto + Botones
            Panel pnlProdContainer = new Panel { Size = new Size(345, 52), Margin = new Padding(0, 0, 8, 0) };
            Label lblProd = new Label { Text = "Seleccionar Producto (Código o Nombre)", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };

            txtBuscarProducto = new TextBox
            {
                Size = new Size(215, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = PLACEHOLDER_PROD,
                Location = new Point(0, 16)
            };
            ConfigurarEventosBuscadorProducto();

            btnLimpiarBuscadorProd = CrearBotonIcono("🗑️", 217, 16, (s, e) => ResetearBuscadorProducto());
            btnNuevoProducto = CrearBotonAccionRapida("➕", Color.FromArgb(2, 132, 199), 246, 16, BtnNuevoProducto_Click);
            btnEditarProducto = CrearBotonAccionRapida("✏️", Color.FromArgb(245, 158, 11), 276, 16, BtnEditarProducto_Click);

            pnlProdContainer.Controls.AddRange(new Control[] { lblProd, txtBuscarProducto, btnLimpiarBuscadorProd, btnNuevoProducto, btnEditarProducto });

            // Cantidad Recibida (TextBox numérico sin flechas)
            txtCantidadRecibida = new TextBox
            {
                Size = new Size(65, 28),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Text = "1",
                TextAlign = HorizontalAlignment.Center
            };
            txtCantidadRecibida.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtCantidadRecibida.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPrecioCosto.Focus(); };

            txtPrecioCosto = new TextBox { Size = new Size(95, 28), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtPrecioCosto.TextChanged += (s, e) => CalcularPreviewPrecioVenta();
            txtPrecioCosto.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnAgregarItem_Click(s, e); };

            lblPreviewPVP = new Label { Size = new Size(130, 28), Text = "PVP Sug: $0", ForeColor = Color.FromArgb(16, 185, 129), Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

            btnAgregarItem = new Button
            {
                Text = "➕ AGREGAR A LA LISTA",
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 16, 0, 0)
            };
            btnAgregarItem.FlatAppearance.BorderSize = 0;
            btnAgregarItem.Click += BtnAgregarItem_Click;

            flowItem.Controls.AddRange(new Control[] {
                pnlProdContainer,
                CrearCampo("Cant. Recibida", txtCantidadRecibida),
                CrearCampo("Costo Neto ($)", txtPrecioCosto),
                CrearCampo("Precio Venta (IVA Inc.)", lblPreviewPVP),
                btnAgregarItem
            });
            pnlPaso2.Controls.Add(flowItem);

            // ==========================================
            // PASO 3: GRILLA DE DETALLE Y TOTALES
            // ==========================================
            TableLayoutPanel pnlBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0)
            };
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));

            // Grilla
            Panel pnlTableCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), Margin = new Padding(0, 0, 10, 0) };
            dgvDetalle = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvDetalle);

            Button btnQuitar = new Button { Text = "🗑️ Quitar Ítem", Size = new Size(120, 26), BackColor = Color.FromArgb(254, 226, 226), ForeColor = Color.FromArgb(185, 28, 28), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Dock = DockStyle.Bottom };
            btnQuitar.FlatAppearance.BorderSize = 0;
            btnQuitar.Click += (s, e) =>
            {
                if (dgvDetalle.CurrentRow?.DataBoundItem is DetalleCompra item)
                {
                    _detalleFactura.Remove(item);
                    ActualizarTablaDetalle();
                }
            };
            pnlTableCard.Controls.Add(dgvDetalle);
            pnlTableCard.Controls.Add(btnQuitar);

            // Resumen Lateral
            Panel pnlSummaryCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14) };
            Label lblResumenTit = new Label { Text = "PASO 3: CONFIRMAR RECEPCIÓN", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

            Panel pnlTotales = new Panel { Dock = DockStyle.Top, Height = 110 };
            int y = 5;
            lblTotalNeto = CrearRenglon("Neto Factura:", "$ 0", ref y, pnlTotales);
            lblIvaCredito = CrearRenglon("IVA Crédito (19%):", "$ 0", ref y, pnlTotales);

            Panel pnlTotalBox = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(6) };
            lblTotalBruto = new Label { Text = "$ 0", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlTotalBox.Controls.Add(lblTotalBruto);

            btnRegistrarCompra = new Button
            {
                Text = "💾 PROCESAR RECEPCIÓN\nE INCREMENTAR STOCK",
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegistrarCompra.FlatAppearance.BorderSize = 0;
            btnRegistrarCompra.Click += BtnRegistrarCompra_Click;

            btnLimpiarFormulario = new Button
            {
                Text = "🔄 Limpiar Todo",
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnLimpiarFormulario.FlatAppearance.BorderSize = 0;
            btnLimpiarFormulario.Click += (s, e) => LimpiarFormularioCompleto();

            pnlSummaryCard.Controls.Add(btnRegistrarCompra);
            pnlSummaryCard.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 6 });
            pnlSummaryCard.Controls.Add(btnLimpiarFormulario);
            pnlSummaryCard.Controls.Add(pnlTotalBox);
            pnlSummaryCard.Controls.Add(pnlTotales);
            pnlSummaryCard.Controls.Add(lblResumenTit);

            pnlBottom.Controls.Add(pnlTableCard, 0, 0);
            pnlBottom.Controls.Add(pnlSummaryCard, 1, 0);

            // Listas Predictivas Flotantes
            lstSugerenciasProveedores = new ListBox { Size = new Size(320, 130), Location = new Point(34, 88), Font = new Font("Segoe UI", 9F), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            lstSugerenciasProveedores.Click += LstSugerenciasProveedores_Click;
            lstSugerenciasProveedores.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LstSugerenciasProveedores_Click(s, e); if (e.KeyCode == Keys.Escape) lstSugerenciasProveedores.Visible = false; };

            lstSugerenciasProductos = new ListBox { Size = new Size(320, 130), Location = new Point(34, 178), Font = new Font("Segoe UI", 9F), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            lstSugerenciasProductos.Click += LstSugerenciasProductos_Click;
            lstSugerenciasProductos.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LstSugerenciasProductos_Click(s, e); if (e.KeyCode == Keys.Escape) lstSugerenciasProductos.Visible = false; };

            pnlMain.Controls.Add(lstSugerenciasProductos);
            pnlMain.Controls.Add(lstSugerenciasProveedores);
            pnlMain.Controls.Add(pnlBottom);
            pnlMain.Controls.Add(pnlPaso2);
            pnlMain.Controls.Add(pnlPaso1);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ========================================================
        // BUSCADOR PROVEEDOR
        // ========================================================
        private void ConfigurarEventosBuscadorProveedor()
        {
            txtBuscarProveedor.GotFocus += (s, e) =>
            {
                if (txtBuscarProveedor.Text == PLACEHOLDER_PROV)
                {
                    txtBuscarProveedor.Text = "";
                    txtBuscarProveedor.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                    txtBuscarProveedor.ForeColor = Color.FromArgb(15, 23, 42);
                }
            };

            txtBuscarProveedor.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtBuscarProveedor.Text)) ResetearBuscadorProveedor();
            };

            txtBuscarProveedor.TextChanged += (s, e) =>
            {
                if (txtBuscarProveedor.Text == PLACEHOLDER_PROV) return;

                string q = txtBuscarProveedor.Text.Trim().ToLower();
                if (q.Length >= 1)
                {
                    var f = _proveedoresCache.Where(p => p.RazonSocial.ToLower().Contains(q) || p.Rut.ToLower().Contains(q)).Take(7).ToList();
                    if (f.Count > 0)
                    {
                        lstSugerenciasProveedores.DataSource = null;
                        lstSugerenciasProveedores.DataSource = f;
                        lstSugerenciasProveedores.DisplayMember = "RazonSocial";
                        lstSugerenciasProveedores.ValueMember = "ProveedorID";
                        lstSugerenciasProveedores.Visible = true;
                        lstSugerenciasProveedores.BringToFront();
                    }
                    else lstSugerenciasProveedores.Visible = false;
                }
                else { lstSugerenciasProveedores.Visible = false; _proveedorSeleccionado = null; }
            };

            txtBuscarProveedor.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstSugerenciasProveedores.Visible && lstSugerenciasProveedores.Items.Count > 0) lstSugerenciasProveedores.Focus();
                if (e.KeyCode == Keys.Escape) lstSugerenciasProveedores.Visible = false;
                if (e.KeyCode == Keys.Enter && _proveedorSeleccionado != null) txtNroDocumento.Focus();
            };
        }

        private void ResetearBuscadorProveedor()
        {
            _proveedorSeleccionado = null;
            txtBuscarProveedor.Text = PLACEHOLDER_PROV;
            txtBuscarProveedor.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            txtBuscarProveedor.ForeColor = Color.FromArgb(148, 163, 184);
            lstSugerenciasProveedores.Visible = false;
        }

        private void LstSugerenciasProveedores_Click(object? sender, EventArgs e)
        {
            if (lstSugerenciasProveedores.SelectedItem is Proveedor p)
            {
                _proveedorSeleccionado = p;
                txtBuscarProveedor.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                txtBuscarProveedor.ForeColor = Color.FromArgb(2, 132, 199);
                txtBuscarProveedor.Text = $"{p.RazonSocial} ({p.Rut})";
                lstSugerenciasProveedores.Visible = false;
                txtNroDocumento.Focus();
            }
        }

        // ========================================================
        // BUSCADOR PRODUCTO
        // ========================================================
        private void ConfigurarEventosBuscadorProducto()
        {
            txtBuscarProducto.GotFocus += (s, e) =>
            {
                if (txtBuscarProducto.Text == PLACEHOLDER_PROD)
                {
                    txtBuscarProducto.Text = "";
                    txtBuscarProducto.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                    txtBuscarProducto.ForeColor = Color.FromArgb(15, 23, 42);
                }
            };

            txtBuscarProducto.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtBuscarProducto.Text)) ResetearBuscadorProducto();
            };

            txtBuscarProducto.TextChanged += (s, e) =>
            {
                if (txtBuscarProducto.Text == PLACEHOLDER_PROD) return;

                string q = txtBuscarProducto.Text.Trim().ToLower();
                if (q.Length >= 1)
                {
                    var f = _productosCache.Where(p => p.Nombre.ToLower().Contains(q) || p.CodigoBarra.ToLower().Contains(q)).Take(7).ToList();
                    if (f.Count > 0)
                    {
                        lstSugerenciasProductos.DataSource = null;
                        lstSugerenciasProductos.DataSource = f;
                        lstSugerenciasProductos.DisplayMember = "Nombre";
                        lstSugerenciasProductos.ValueMember = "ProductoID";
                        lstSugerenciasProductos.Visible = true;
                        lstSugerenciasProductos.BringToFront();
                    }
                    else lstSugerenciasProductos.Visible = false;
                }
                else { lstSugerenciasProductos.Visible = false; _productoSeleccionado = null; }
            };

            txtBuscarProducto.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstSugerenciasProductos.Visible && lstSugerenciasProductos.Items.Count > 0) lstSugerenciasProductos.Focus();
                if (e.KeyCode == Keys.Escape) lstSugerenciasProductos.Visible = false;
                if (e.KeyCode == Keys.Enter && _productoSeleccionado != null) txtCantidadRecibida.Focus();
            };
        }

        private void ResetearBuscadorProducto()
        {
            _productoSeleccionado = null;
            txtBuscarProducto.Text = PLACEHOLDER_PROD;
            txtBuscarProducto.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            txtBuscarProducto.ForeColor = Color.FromArgb(148, 163, 184);
            lstSugerenciasProductos.Visible = false;
            txtPrecioCosto.Text = "0";
            lblPreviewPVP.Text = "PVP Sug: $0";
        }

        private void LstSugerenciasProductos_Click(object? sender, EventArgs e)
        {
            if (lstSugerenciasProductos.SelectedItem is Producto p)
            {
                _productoSeleccionado = p;
                txtBuscarProducto.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                txtBuscarProducto.ForeColor = Color.FromArgb(2, 132, 199);
                txtBuscarProducto.Text = $"{p.Nombre} (SKU: {p.CodigoBarra})";
                lstSugerenciasProductos.Visible = false;

                decimal costo = p.PrecioCosto > 0 ? p.PrecioCosto : Math.Round(p.PrecioUnitario / 1.19m / 1.30m, 0);
                txtPrecioCosto.Text = costo.ToString("0");
                CalcularPreviewPrecioVenta();

                txtCantidadRecibida.Focus();
                txtCantidadRecibida.SelectAll();
            }
        }

        // ========================================================
        // CARGA DE DATOS Y MODALES
        // ========================================================
        private void CargarDatosIniciales()
        {
            using var db = new AppDbContext();
            try
            {
                _proveedoresCache = db.Proveedores.Where(p => p.Estado).OrderBy(p => p.RazonSocial).ToList();
                _productosCache = db.Productos.Where(p => p.Estado).OrderBy(p => p.Nombre).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNuevoProveedor_Click(object? sender, EventArgs e)
        {
            using (var modal = new FormNuevoProveedorModal())
            {
                if (modal.ShowDialog(this) == DialogResult.OK && modal.ProveedorResultado != null)
                {
                    CargarDatosIniciales();
                    _proveedorSeleccionado = modal.ProveedorResultado;
                    txtBuscarProveedor.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    txtBuscarProveedor.ForeColor = Color.FromArgb(2, 132, 199);
                    txtBuscarProveedor.Text = $"{modal.ProveedorResultado.RazonSocial} ({modal.ProveedorResultado.Rut})";
                    txtNroDocumento.Focus();
                }
            }
        }

        private void BtnEditarProveedor_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null)
            {
                MessageBox.Show("Busque y seleccione primero un proveedor antes de editar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProveedor.Focus();
                return;
            }

            using (var modal = new FormNuevoProveedorModal(_proveedorSeleccionado))
            {
                if (modal.ShowDialog(this) == DialogResult.OK && modal.ProveedorResultado != null)
                {
                    CargarDatosIniciales();
                    _proveedorSeleccionado = modal.ProveedorResultado;
                    txtBuscarProveedor.Text = $"{modal.ProveedorResultado.RazonSocial} ({modal.ProveedorResultado.Rut})";
                }
            }
        }

        private void BtnNuevoProducto_Click(object? sender, EventArgs e)
    {
            using (var modal = new FormNuevoProductoModal())
            {
                if (modal.ShowDialog(this) == DialogResult.OK && modal.ProductoResultado != null)
                {
                    _productoSeleccionado = modal.ProductoResultado;
                    txtBuscarProducto.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    txtBuscarProducto.ForeColor = Color.FromArgb(2, 132, 199);
                    txtBuscarProducto.Text = $"{modal.ProductoResultado.Nombre} (Nuevo ítem a registrar)";

                    decimal costo = modal.ProductoResultado.PrecioCosto;
                    txtPrecioCosto.Text = costo.ToString("0");
                    CalcularPreviewPrecioVenta();

                    txtCantidadRecibida.Text = modal.CantidadFactura.ToString();

                    txtPrecioCosto.Focus();
                    txtPrecioCosto.SelectAll();
                }
            }
        }
        private void BtnEditarProducto_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                MessageBox.Show("Busque y seleccione primero un producto antes de editar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProducto.Focus();
                return;
            }

            using (var modal = new FormNuevoProductoModal(_productoSeleccionado))
            {
                if (modal.ShowDialog(this) == DialogResult.OK && modal.ProductoResultado != null)
                {
                    CargarDatosIniciales();
                    _productoSeleccionado = modal.ProductoResultado;
                    txtBuscarProducto.Text = $"{modal.ProductoResultado.Nombre} (SKU: {modal.ProductoResultado.CodigoBarra})";
                    txtPrecioCosto.Text = modal.ProductoResultado.PrecioCosto.ToString("0");
                    CalcularPreviewPrecioVenta();
                }
            }
        }

        private void BtnListaProveedores_Click(object? sender, EventArgs e)
        {
            using (var modal = new FormListaProveedoresModal())
            {
                if (modal.ShowDialog(this) == DialogResult.OK && modal.ProveedorSeleccionado != null)
                {
                    CargarDatosIniciales();
                    _proveedorSeleccionado = modal.ProveedorSeleccionado;
                    txtBuscarProveedor.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    txtBuscarProveedor.ForeColor = Color.FromArgb(2, 132, 199);
                    txtBuscarProveedor.Text = $"{modal.ProveedorSeleccionado.RazonSocial} ({modal.ProveedorSeleccionado.Rut})";
                    txtNroDocumento.Focus();
                }
            }
        }

        private void CalcularPreviewPrecioVenta()
        {
            if (decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) && _productoSeleccionado != null)
            {
                decimal margen = _productoSeleccionado.MargenGanancia > 0 ? _productoSeleccionado.MargenGanancia : 30.00m;
                decimal netoVenta = costo * (1 + (margen / 100m));
                decimal pvpCalculado = netoVenta * 1.19m;
                decimal pvpRedondeado = Math.Round(pvpCalculado / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
                lblPreviewPVP.Text = $"PVP Sug: ${pvpRedondeado:N0}";
            }
        }

        private void BtnAgregarItem_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                MessageBox.Show("Busque y seleccione un producto.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProducto.Focus();
                return;
            }

            if (!int.TryParse(txtCantidadRecibida.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCantidadRecibida.Focus();
                return;
            }

            if (!decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) || costo <= 0)
            {
                MessageBox.Show("Ingrese un precio de costo válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrecioCosto.Focus();
                return;
            }

            var item = _detalleFactura.FirstOrDefault(d => d.ProductoID == _productoSeleccionado.ProductoID);

            if (item != null)
            {
                item.Cantidad += cantidad;
                item.PrecioCostoUnitario = costo;
                item.Subtotal = item.Cantidad * costo;
            }
            else
            {
                _detalleFactura.Add(new DetalleCompra
                {
                    ProductoID = _productoSeleccionado.ProductoID,
                    NombreProducto = _productoSeleccionado.Nombre,
                    Cantidad = cantidad,
                    PrecioCostoUnitario = costo,
                    Subtotal = cantidad * costo
                });
            }

            ActualizarTablaDetalle();
            ResetearBuscadorProducto();
            txtCantidadRecibida.Text = "1";
            txtBuscarProducto.Focus();
        }

        private void ActualizarTablaDetalle()
        {
            dgvDetalle.DataSource = null;
            dgvDetalle.DataSource = _detalleFactura;

            if (dgvDetalle.Columns["IdDetalleCompra"] != null) dgvDetalle.Columns["IdDetalleCompra"].Visible = false;
            if (dgvDetalle.Columns["CompraID"] != null) dgvDetalle.Columns["CompraID"].Visible = false;
            if (dgvDetalle.Columns["ProductoID"] != null) dgvDetalle.Columns["ProductoID"].Visible = false;
            if (dgvDetalle.Columns["Compra"] != null) dgvDetalle.Columns["Compra"].Visible = false;
            if (dgvDetalle.Columns["Producto"] != null) dgvDetalle.Columns["Producto"].Visible = false;

            if (dgvDetalle.Columns["NombreProducto"] != null) dgvDetalle.Columns["NombreProducto"].HeaderText = "Producto";
            if (dgvDetalle.Columns["Cantidad"] != null) dgvDetalle.Columns["Cantidad"].HeaderText = "Cant.";
            if (dgvDetalle.Columns["PrecioCostoUnitario"] != null) { dgvDetalle.Columns["PrecioCostoUnitario"].HeaderText = "Costo Unit."; dgvDetalle.Columns["PrecioCostoUnitario"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvDetalle.Columns["Subtotal"] != null) { dgvDetalle.Columns["Subtotal"].HeaderText = "Subtotal"; dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0"; }

            decimal neto = _detalleFactura.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m, 0);
            decimal total = neto + iva;

            lblTotalNeto.Text = $"$ {neto:N0}";
            lblIvaCredito.Text = $"$ {iva:N0}";
            lblTotalBruto.Text = $"$ {total:N0}";
        }

        private void BtnRegistrarCompra_Click(object? sender, EventArgs e)
{
            // 1. Validar que exista un proveedor seleccionado
            if (_proveedorSeleccionado == null)
            {
                MessageBox.Show("Debe buscar y seleccionar un proveedor antes de procesar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProveedor.Focus();
                return;
            }

            // 2. Validar que el N° de Documento no esté vacío y sea numérico
            if (string.IsNullOrWhiteSpace(txtNroDocumento.Text) || !int.TryParse(txtNroDocumento.Text.Trim(), out int folio) || folio <= 0)
            {
                MessageBox.Show("Debe ingresar un N° de Documento válido (solo números mayores a 0).", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNroDocumento.Focus();
                return;
            }

            // 3. 🔴 VALIDACIÓN DE FACTURA DUPLICADA EN BASE DE DATOS
            using (var db = new AppDbContext())
            {
                // Limpiamos formato de RUT para comparar sin puntos ni guiones si fuera necesario
                string rutProv = _proveedorSeleccionado.Rut.Trim();

                bool facturaYaExiste = db.Compras.Any(c => 
                    c.RutProveedor.Trim().ToLower() == rutProv.ToLower() && 
                    c.NroFacturaProveedor == folio
                );

                if (facturaYaExiste)
                {
                    MessageBox.Show(
                        $"⛔ FACTURA DUPLICADA:\n\n" +
                        $"El documento N° {folio} ya se encuentra registrado para el proveedor:\n" +
                        $"'{_proveedorSeleccionado.RazonSocial}' (RUT: {_proveedorSeleccionado.Rut}).\n\n" +
                        $"Por favor verifique el número de factura para evitar ingresos dobles de stock.",
                        "Documento ya Utilizado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop
                    );
                    txtNroDocumento.Focus();
                    txtNroDocumento.SelectAll();
                    return; // ⛔ Detiene el proceso y no guarda nada
                }
            }

            // 4. Validar que la factura tenga ítems en la tabla
            if (_detalleFactura.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto a la lista antes de procesar la recepción.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProducto.Focus();
                return;
            }

            // 5. Cálculos de totales
            decimal neto = _detalleFactura.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m, 0);
            decimal total = neto + iva;

            Compra compra = new Compra
            {
                RutProveedor = _proveedorSeleccionado.Rut,
                RazonSocialProveedor = _proveedorSeleccionado.RazonSocial,
                NroFacturaProveedor = folio,
                FechaEmision = dtpFechaRecepcion.Value,
                FechaRecepcion = DateTime.Now,
                MontoNeto = neto,
                MontoIva = iva,
                MontoTotal = total,
                UsuarioReceptor = "Bárbara",
                Estado = "Recibida"
            };

            try
            {
                _compraService.RegistrarFacturaCompra(compra, _detalleFactura, actualizarPreciosVenta: true);

                MessageBox.Show(
                    $"¡Documento N° {folio} procesado exitosamente!\n\n" +
                    $"• Proveedor: {_proveedorSeleccionado.RazonSocial}\n" +
                    $"• Stock incrementado en {_detalleFactura.Sum(d => d.Cantidad)} unidades.\n" +
                    $"• Precios y costos actualizados correctamente en inventario.", 
                    "Recepción Guardada", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information
                );

                LimpiarFormularioCompleto();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarFormularioCompleto()
        {
            _detalleFactura.Clear();
            ResetearBuscadorProveedor();
            ResetearBuscadorProducto();
            txtNroDocumento.Text = "";
            txtCantidadRecibida.Text = "1";
            ActualizarTablaDetalle();
        }

        // ========================================================
        // HELPERS VISUALES
        // ========================================================
        private Button CrearBotonIcono(string texto, int x, int y, EventHandler onClick)
        {
            Button b = new Button
            {
                Text = texto,
                Size = new Size(28, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand,
                Location = new Point(x, y)
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            b.Click += onClick;
            return b;
        }

        private Button CrearBotonAccionRapida(string texto, Color back, int x, int y, EventHandler onClick)
        {
            Button b = new Button
            {
                Text = texto,
                Size = new Size(28, 28),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(x, y)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private Panel CrearTarjeta(int height, string titulo, Color colorBorde)
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Color.White, Padding = new Padding(12, 6, 12, 6), Margin = new Padding(0, 0, 0, 8) };
            pnl.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
                e.Graphics.FillRectangle(new SolidBrush(colorBorde), 0, 0, 4, pnl.Height);
            };
            return pnl;
        }

        private Panel CrearCampo(string label, Control ctrl)
        {
            Panel p = new Panel { Size = new Size(ctrl.Width + 4, 46), Margin = new Padding(0, 0, 8, 0) };
            Label l = new Label { Text = label, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 16);
            p.Controls.Add(l);
            p.Controls.Add(ctrl);
            return p;
        }

        private Label CrearRenglon(string titulo, string valor, ref int y, Panel c)
        {
            Label lt = new Label { Text = titulo, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, y), AutoSize = true };
            Label lv = new Label { Text = valor, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, y + 14), AutoSize = true };
            c.Controls.Add(lt);
            c.Controls.Add(lv);
            y += 38;
            return lv;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 30;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 28;
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }
    }
}