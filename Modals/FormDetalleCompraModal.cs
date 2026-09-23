using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormDetalleCompraModal : Form
    {
        private readonly CompraService _compraService = new CompraService();
        private readonly ProductoService _productoService = new ProductoService();
        private readonly int _compraId;

        private Compra? _compra;
        private List<DetalleCompra> _detallesEditables = new List<DetalleCompra>();
        private List<Producto> _productosCache = new List<Producto>();

        // Cabecera editable
        private TextBox txtFolio = null!;
        private ComboBox cbTipoDoc = null!;
        private DateTimePicker dtpFecha = null!;
        private Label lblProveedorInfo = null!;

        // Grilla y Totales
        private DataGridView dgvDetalle = null!;
        private Label lblSubtotales = null!;
        private Label lblTotal = null!;

        // Buscador de Producto para agregar o reemplazar
        private Producto? _productoBuscadorSeleccionado = null;
        private TextBox txtBuscarProducto = null!;
        private TextBox txtCantidadItem = null!;
        private TextBox txtCostoItem = null!;
        private Button btnAgregarItem = null!;
        private ListBox lstSugerenciasProd = null!;
        private const string PLACEHOLDER_PROD = "🔍 Buscar producto para agregar (Código o Nombre)...";

        // Botones de acción
        private Button btnGuardarCambios = null!;
        private Button btnCancelar = null!;
        private Button btnEliminarItem = null!;

        public bool CambiosGuardados { get; private set; } = false;

        public FormDetalleCompraModal(int compraId)
        {
            _compraId = compraId;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarDatosIniciales();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Edición y Detalle de Compra";
            this.Size = new Size(920, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // 1. LISTBOX DE SUGERENCIAS FLOTANTE
            lstSugerenciasProd = new ListBox
            {
                Size = new Size(340, 140),
                Font = new Font("Segoe UI", 9F),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstSugerenciasProd.Click += (s, e) => ConfirmarSeleccionProductoBuscador();
            lstSugerenciasProd.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ConfirmarSeleccionProductoBuscador();
                }
            };

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };

            // 2. CABECERA DOCUMENTO
            Panel pnlCabecera = CrearTarjetaRedondeada(0, 0, 0, 80, Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240));
            pnlCabecera.Dock = DockStyle.Top;
            pnlCabecera.Padding = new Padding(12, 8, 12, 8);

            lblProveedorInfo = new Label
            {
                Text = "PROVEEDOR: Cargando...",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Height = 22
            };

            TableLayoutPanel tlpCampos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            tlpCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            cbTipoDoc = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbTipoDoc.Items.AddRange(new string[] { "Factura de Compra", "Factura Exenta", "Boleta de Compra", "Guía de Recepción" });

            txtFolio = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtFolio.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };

            dtpFecha = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9F) };

            tlpCampos.Controls.Add(EnvolverCampo("Tipo Documento", cbTipoDoc), 0, 0);
            tlpCampos.Controls.Add(EnvolverCampo("N° Folio", txtFolio), 1, 0);
            tlpCampos.Controls.Add(EnvolverCampo("Fecha Emisión", dtpFecha), 2, 0);

            pnlCabecera.Controls.Add(tlpCampos);
            pnlCabecera.Controls.Add(lblProveedorInfo);

            // 3. BARRA AGREGAR / REEMPLAZAR PRODUCTO
            Panel pnlAgregarProd = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(0, 8, 0, 8),
                BackColor = Color.Transparent
            };

            TableLayoutPanel tlpAdd = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            tlpAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
            tlpAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            tlpAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            tlpAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));
            tlpAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));

            txtBuscarProducto = new TextBox { Dock = DockStyle.Fill, Text = PLACEHOLDER_PROD, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.FromArgb(148, 163, 184) };
            ConfigurarBuscadorProducto();

            txtCantidadItem = new TextBox { Dock = DockStyle.Fill, Text = "1", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
            txtCantidadItem.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtCantidadItem.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { txtCostoItem.Focus(); txtCostoItem.SelectAll(); e.Handled = true; e.SuppressKeyPress = true; } };

            txtCostoItem = new TextBox { Dock = DockStyle.Fill, Text = "0", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
            txtCostoItem.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtCostoItem.TextChanged += (s, e) => MonedaHelper.AplicarMascaraEnVivo(txtCostoItem);
            txtCostoItem.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnAgregarItem.PerformClick(); e.Handled = true; e.SuppressKeyPress = true; } };

            btnAgregarItem = new Button { Text = "➕ Agregar", Dock = DockStyle.Fill, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAgregarItem.FlatAppearance.BorderSize = 0;
            btnAgregarItem.Click += BtnAgregarItem_Click;

            btnEliminarItem = new Button { Text = "🗑️ Quitar", Dock = DockStyle.Fill, BackColor = Color.FromArgb(254, 226, 226), ForeColor = Color.FromArgb(220, 38, 38), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnEliminarItem.FlatAppearance.BorderSize = 0;
            btnEliminarItem.Click += BtnEliminarItem_Click;

            tlpAdd.Controls.Add(txtBuscarProducto, 0, 0);
            tlpAdd.Controls.Add(txtCantidadItem, 1, 0);
            tlpAdd.Controls.Add(txtCostoItem, 2, 0);
            tlpAdd.Controls.Add(btnAgregarItem, 3, 0);
            tlpAdd.Controls.Add(btnEliminarItem, 4, 0);
            pnlAgregarProd.Controls.Add(tlpAdd);

            // 4. GRILLA PRINCIPAL
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(8);

            dgvDetalle = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowTemplate = { Height = 32 },
                GridColor = Color.FromArgb(241, 245, 249)
            };
            ConfigurarColumnasGrilla();
            pnlGridCard.Controls.Add(dgvDetalle);

            // 5. BARRA INFERIOR DE TOTALES Y BOTONES
            Panel pnlBottomWrapper = new Panel { Dock = DockStyle.Bottom, Height = 95, Padding = new Padding(0, 8, 0, 0), BackColor = Color.Transparent };

            Panel pnlTotalesCard = CrearTarjetaRedondeada(0, 0, 0, 42, Color.FromArgb(240, 253, 244), Color.FromArgb(187, 247, 208));
            pnlTotalesCard.Dock = DockStyle.Top;
            pnlTotalesCard.Padding = new Padding(14, 0, 14, 0);

            lblSubtotales = new Label { Dock = DockStyle.Left, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), TextAlign = ContentAlignment.MiddleLeft, AutoSize = true };
            lblTotal = new Label { Dock = DockStyle.Right, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), TextAlign = ContentAlignment.MiddleRight, AutoSize = true };
            pnlTotalesCard.Controls.AddRange(new Control[] { lblSubtotales, lblTotal });

            Panel pnlBotonesAccion = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };

            btnGuardarCambios = new Button { Text = "💾 Guardar Modificación y Ajustar Stock", Dock = DockStyle.Right, Width = 280, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnGuardarCambios.FlatAppearance.BorderSize = 0;
            btnGuardarCambios.Click += BtnGuardarCambios_Click;

            btnCancelar = new Button { Text = "✕ Cancelar", Dock = DockStyle.Left, Width = 110, BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();
            this.CancelButton = btnCancelar;

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnCancelar, btnGuardarCambios });

            pnlBottomWrapper.Controls.AddRange(new Control[] { pnlBotonesAccion, pnlTotalesCard });

            // APILAMIENTO DE CONTROLES
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlAgregarProd);
            pnlMain.Controls.Add(pnlCabecera);
            pnlMain.Controls.Add(pnlBottomWrapper);

            this.Controls.Add(lstSugerenciasProd);
            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel EnvolverCampo(string titulo, Control ctrl)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 0, 4, 0) };
            Label l = new Label { Text = titulo, Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            ctrl.Dock = DockStyle.Top;
            ctrl.Height = 24;
            p.Controls.AddRange(new Control[] { ctrl, l });
            return p;
        }

        private void ConfigurarColumnasGrilla()
        {
            dgvDetalle.EnableHeadersVisualStyles = false;
            dgvDetalle.ColumnHeadersHeight = 34;
            dgvDetalle.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvDetalle.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvDetalle.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvDetalle.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvDetalle.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvDetalle.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvDetalle.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvDetalle.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

            dgvDetalle.Columns.Clear();

            var colItem = new DataGridViewTextBoxColumn { Name = "Item", HeaderText = "DESCRIPCIÓN DEL ÍTEM / PRODUCTO", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 220 };
            colItem.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colItem.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            var colCant = new DataGridViewTextBoxColumn { Name = "Cant", HeaderText = "CANTIDAD", Width = 95 };
            colCant.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colCant.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colCant.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            var colCosto = new DataGridViewTextBoxColumn { Name = "Costo", HeaderText = "COSTO NETO UNIT.", Width = 130 };
            colCosto.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colCosto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colCosto.DefaultCellStyle.Format = "$#,##0";

            var colSub = new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "SUBTOTAL NETO", Width = 130, ReadOnly = true };
            colSub.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colSub.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colSub.DefaultCellStyle.Format = "$#,##0";
            colSub.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvDetalle.Columns.AddRange(new DataGridViewColumn[] { colItem, colCant, colCosto, colSub });

            // Recálculo automático en grilla al cambiar cantidades o costos
            dgvDetalle.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _detallesEditables.Count)
                {
                    var item = _detallesEditables[e.RowIndex];
                    if (dgvDetalle.Columns[e.ColumnIndex].Name == "Cant")
                    {
                        int.TryParse(dgvDetalle.Rows[e.RowIndex].Cells["Cant"].Value?.ToString(), out int nuevaCant);
                        item.Cantidad = nuevaCant > 0 ? nuevaCant : 1;
                        item.Subtotal = item.Cantidad * item.PrecioCostoUnitario;
                    }
                    else if (dgvDetalle.Columns[e.ColumnIndex].Name == "Costo")
                    {
                        decimal nuevoCosto = MonedaHelper.Limpiar(dgvDetalle.Rows[e.RowIndex].Cells["Costo"].Value?.ToString() ?? "0");
                        item.PrecioCostoUnitario = nuevoCosto;
                        item.Subtotal = item.Cantidad * nuevoCosto;
                    }

                    RefrescarGrilla();
                }
            };
        }

        private void CargarDatosIniciales()
        {
            try
            {
                _productosCache = _productoService.ObtenerProductosActivos();
                _compra = _compraService.ObtenerCompraPorId(_compraId);
                if (_compra == null) return;

                this.Text = $"Editar Compra — Folio N° {_compra.NroFacturaProveedor} ({_compra.RazonSocialProveedor})";
                lblProveedorInfo.Text = $"PROVEEDOR: {_compra.RazonSocialProveedor} (RUT: {_compra.RutProveedor})  •  ORIGEN: {(_compra.TipoCompra == "MERCADERIA" ? "Mercadería de Venta (Afecta Stock)" : "Gasto Interno")}";

                cbTipoDoc.SelectedItem = _compra.TipoDocumento;
                if (cbTipoDoc.SelectedIndex < 0) cbTipoDoc.SelectedIndex = 0;

                txtFolio.Text = _compra.NroFacturaProveedor.ToString();
                dtpFecha.Value = _compra.FechaEmision;

                _detallesEditables = _compraService.ObtenerDetallesCompra(_compraId);
                RefrescarGrilla();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefrescarGrilla()
        {
            dgvDetalle.Rows.Clear();
            foreach (var d in _detallesEditables)
            {
                dgvDetalle.Rows.Add(d.NombreProducto, d.Cantidad, d.PrecioCostoUnitario, d.Subtotal);
            }

            decimal neto = _detallesEditables.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m);
            decimal total = neto + iva;

            lblSubtotales.Text = $"Neto: {MonedaHelper.Formatear(neto, conSigno: true)}   |   IVA (19%): {MonedaHelper.Formatear(iva, conSigno: true)}";
            lblTotal.Text = $"TOTAL: {MonedaHelper.Formatear(total, conSigno: true)}";
        }

        private void ConfigurarBuscadorProducto()
        {
            txtBuscarProducto.GotFocus += (s, e) => { if (txtBuscarProducto.Text == PLACEHOLDER_PROD) { txtBuscarProducto.Text = ""; txtBuscarProducto.ForeColor = Color.FromArgb(15, 23, 42); txtBuscarProducto.Font = new Font("Segoe UI", 9F); } };
            txtBuscarProducto.LostFocus += (s, e) => { if (!lstSugerenciasProd.Focused && string.IsNullOrWhiteSpace(txtBuscarProducto.Text)) ResetearBuscadorProducto(); };

            txtBuscarProducto.TextChanged += (s, e) =>
            {
                if (txtBuscarProducto.Text == PLACEHOLDER_PROD) return;
                string q = txtBuscarProducto.Text.Trim().ToLower();
                if (q.Length >= 1)
                {
                    var f = _productosCache.Where(p => p.Nombre.ToLower().Contains(q) || p.CodigoBarra.ToLower().Contains(q)).Take(6).ToList();
                    lstSugerenciasProd.DataSource = f.Count > 0 ? f : null;
                    lstSugerenciasProd.DisplayMember = "Nombre";
                    if (f.Count > 0)
                    {
                        Point pt = txtBuscarProducto.Parent.PointToScreen(new Point(txtBuscarProducto.Left, txtBuscarProducto.Bottom));
                        lstSugerenciasProd.Location = this.PointToClient(pt);
                        lstSugerenciasProd.Width = Math.Max(txtBuscarProducto.Width, 340);
                        lstSugerenciasProd.SelectedIndex = 0;
                    }
                    lstSugerenciasProd.Visible = f.Count > 0;
                    lstSugerenciasProd.BringToFront();
                }
                else lstSugerenciasProd.Visible = false;
            };

            txtBuscarProducto.KeyDown += (s, e) =>
            {
                if (lstSugerenciasProd.Visible && lstSugerenciasProd.Items.Count > 0)
                {
                    if (e.KeyCode == Keys.Down)
                    {
                        if (lstSugerenciasProd.SelectedIndex < lstSugerenciasProd.Items.Count - 1) lstSugerenciasProd.SelectedIndex++;
                        e.Handled = true; e.SuppressKeyPress = true;
                    }
                    else if (e.KeyCode == Keys.Up)
                    {
                        if (lstSugerenciasProd.SelectedIndex > 0) lstSugerenciasProd.SelectedIndex--;
                        e.Handled = true; e.SuppressKeyPress = true;
                    }
                    else if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true; e.SuppressKeyPress = true;
                        ConfirmarSeleccionProductoBuscador();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        lstSugerenciasProd.Visible = false;
                        e.Handled = true; e.SuppressKeyPress = true;
                    }
                }
            };
        }

        private void ConfirmarSeleccionProductoBuscador()
        {
            if (lstSugerenciasProd.SelectedItem is Producto p)
            {
                _productoBuscadorSeleccionado = p;
                txtBuscarProducto.Text = $"{p.Nombre} ({p.CodigoBarra})";
                txtBuscarProducto.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                txtBuscarProducto.ForeColor = Color.FromArgb(37, 99, 235);
                lstSugerenciasProd.Visible = false;

                txtCostoItem.Text = MonedaHelper.Formatear(p.PrecioCosto);
                txtCantidadItem.Focus();
                txtCantidadItem.SelectAll();
            }
        }

        private void ResetearBuscadorProducto()
        {
            _productoBuscadorSeleccionado = null;
            txtBuscarProducto.Text = PLACEHOLDER_PROD;
            txtBuscarProducto.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            txtBuscarProducto.ForeColor = Color.FromArgb(148, 163, 184);
            lstSugerenciasProd.Visible = false;
            txtCantidadItem.Text = "1";
            txtCostoItem.Text = "0";
        }

        private void BtnAgregarItem_Click(object? sender, EventArgs e)
        {
            if (_productoBuscadorSeleccionado == null)
            {
                MessageBox.Show("Busque y seleccione un producto.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarProducto.Focus();
                return;
            }

            int.TryParse(txtCantidadItem.Text.Trim(), out int cant);
            decimal costo = MonedaHelper.Limpiar(txtCostoItem.Text);

            if (cant <= 0 || costo <= 0)
            {
                MessageBox.Show("Ingrese una cantidad y costo válidos.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _detallesEditables.Add(new DetalleCompra
            {
                TipoItem = "MERCADERIA",
                ProductoID = _productoBuscadorSeleccionado.ProductoID,
                NombreProducto = _productoBuscadorSeleccionado.Nombre,
                Cantidad = cant,
                PrecioCostoUnitario = costo,
                Subtotal = cant * costo,
                AfectaStock = true
            });

            RefrescarGrilla();
            ResetearBuscadorProducto();
            txtBuscarProducto.Focus();
        }

        private void BtnEliminarItem_Click(object? sender, EventArgs e)
        {
            if (dgvDetalle.CurrentRow != null && dgvDetalle.CurrentRow.Index >= 0 && dgvDetalle.CurrentRow.Index < _detallesEditables.Count)
            {
                var item = _detallesEditables[dgvDetalle.CurrentRow.Index];
                var confirm = MessageBox.Show($"¿Desea quitar '{item.NombreProducto}' de esta recepción?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    _detallesEditables.RemoveAt(dgvDetalle.CurrentRow.Index);
                    RefrescarGrilla();
                }
            }
        }

        private void BtnGuardarCambios_Click(object? sender, EventArgs e)
        {
            if (_compra == null) return;

            if (!int.TryParse(txtFolio.Text.Trim(), out int folio) || folio <= 0)
            {
                MessageBox.Show("Ingrese un número de folio válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_detallesEditables.Count == 0)
            {
                MessageBox.Show("El documento debe tener al menos una línea registrada.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("¿Confirmar modificación de esta recepción?\n\n• El stock antiguo será revertido y se aplicará el stock y costos actuales.\n• Los precios de venta (Listas 1 a 10) se actualizarán si el costo varió.", "Confirmar Edición", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _compra.TipoDocumento = cbTipoDoc.SelectedItem?.ToString() ?? "Factura de Compra";
            _compra.NroFacturaProveedor = folio;
            _compra.FechaEmision = dtpFecha.Value;

            try
            {
                _compraService.ActualizarFacturaCompra(_compra, _detallesEditables);
                CambiosGuardados = true;
                MessageBox.Show("Recepción de compra actualizada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar compra: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Resize += (s, e) => pnl.Invalidate();
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.BackColor);
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
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
    }
}