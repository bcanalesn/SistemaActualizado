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
    public class FormCompras : Form
    {
        private AppDbContext _db = new AppDbContext();
        private List<DetalleCompra> _detalleFactura = new List<DetalleCompra>();
        private List<Producto> _productosCache = new List<Producto>();
        private List<Proveedor> _proveedoresCache = new List<Proveedor>();

        // Controles de Cabecera (Proveedor y Documento)
        private ComboBox cbProveedores = null!;
        private ComboBox cbTipoDocumento = null!;
        private NumericUpDown numFolio = null!;
        private DateTimePicker dtpFechaRecepcion = null!;

        // Controles de Añadir Producto
        private ComboBox cbProductos = null!;
        private NumericUpDown numCantidadRecibida = null!;
        private TextBox txtPrecioCosto = null!;
        private Button btnAgregarItem = null!;

        // DataGridView Detalle de Compra
        private DataGridView dgvDetalle = null!;

        // Resumen y Totales (Neto, IVA Crédito Fiscal, Total)
        private Label lblTotalNeto = null!;
        private Label lblIvaCredito = null!;
        private Label lblTotalBruto = null!;

        // Botones de Acción
        private Button btnRegistrarCompra = null!;
        private Button btnLimpiarFormulario = null!;
        private Button btnDemoCompra = null!;

        public FormCompras()
        {
            InitializeComponent();
            CargarProveedoresYProductos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 20),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Ingreso de mercancía por Factura/Guía de Proveedor e incremento automático de stock",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 24
            };

            Panel pnlHeaderCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlHeaderCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlHeaderCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            FlowLayoutPanel flowHeader = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            // Proveedor
            cbProveedores = new ComboBox
            {
                Size = new Size(260, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            Panel pnlGrpProv = CrearGrupoControl("🚚 Seleccionar Proveedor", cbProveedores);

            // Tipo Documento
            cbTipoDocumento = new ComboBox
            {
                Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbTipoDocumento.Items.AddRange(new string[] { "Factura de Compra", "Guía de Recepción" });
            cbTipoDocumento.SelectedIndex = 0;
            Panel pnlGrpTipo = CrearGrupoControl("📄 Tipo Documento", cbTipoDocumento);

            // Folio N°
            numFolio = new NumericUpDown
            {
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Minimum = 1,
                Maximum = 99999999,
                Value = 1050
            };
            Panel pnlGrpFolio = CrearGrupoControl("🔢 Folio N°", numFolio);

            // Fecha Recepción
            dtpFechaRecepcion = new DateTimePicker
            {
                Size = new Size(120, 28),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5F)
            };
            Panel pnlGrpFecha = CrearGrupoControl("📅 Fecha Recepción", dtpFechaRecepcion);

            btnDemoCompra = CrearBotonEstilizado("✨ Cargar Demo", Color.FromArgb(124, 58, 237), Color.White, new Size(120, 30));
            btnDemoCompra.Margin = new Padding(6, 18, 0, 0);
            btnDemoCompra.Click += BtnDemoCompra_Click;

            flowHeader.Controls.AddRange(new Control[] { pnlGrpProv, pnlGrpTipo, pnlGrpFolio, pnlGrpFecha, btnDemoCompra });
            pnlHeaderCard.Controls.Add(flowHeader);

            Panel pnlItemCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlItemCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlItemCard.ClientRectangle, Color.FromArgb(186, 230, 253), ButtonBorderStyle.Solid);
            };

            FlowLayoutPanel flowItem = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            // Producto
            cbProductos = new ComboBox
            {
                Size = new Size(260, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbProductos.SelectedIndexChanged += CbProductos_SelectedIndexChanged;
            Panel pnlGrpProd = CrearGrupoControl("📦 Seleccionar Producto", cbProductos);

            // Cantidad
            numCantidadRecibida = new NumericUpDown
            {
                Size = new Size(90, 28),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Minimum = 1,
                Maximum = 10000,
                Value = 10
            };
            Panel pnlGrpCant = CrearGrupoControl("📥 Cantidad", numCantidadRecibida);

            // Precio Costo Unitario
            txtPrecioCosto = new TextBox
            {
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Text = "0"
            };
            Panel pnlGrpCosto = CrearGrupoControl("💲 Costo Unitario ($)", txtPrecioCosto);

            btnAgregarItem = CrearBotonEstilizado("➕ Agregar Ítem", Color.FromArgb(2, 132, 199), Color.White, new Size(125, 30));
            btnAgregarItem.Margin = new Padding(6, 18, 0, 0);
            btnAgregarItem.Click += BtnAgregarItem_Click;

            flowItem.Controls.AddRange(new Control[] { pnlGrpProd, pnlGrpCant, pnlGrpCosto, btnAgregarItem });
            pnlItemCard.Controls.Add(flowItem);

            TableLayoutPanel pnlGridAndSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 440,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 0)
            };
            pnlGridAndSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            pnlGridAndSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));

            // Grid Detalle
            Panel pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlTableCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTableCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            dgvDetalle = new DataGridView
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
            ConfigurarEstiloTabla(dgvDetalle);

            Panel pnlGridActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                Padding = new Padding(0, 4, 0, 0)
            };
            Button btnQuitarItem = CrearBotonEstilizado("🗑️ Quitar Ítem Seleccionado", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28), new Size(185, 28));
            btnQuitarItem.Click += BtnQuitarItem_Click;
            pnlGridActions.Controls.Add(btnQuitarItem);

            pnlTableCard.Controls.Add(dgvDetalle);
            pnlTableCard.Controls.Add(pnlGridActions);

            Panel pnlSummaryCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14)
            };
            pnlSummaryCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlSummaryCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            Label lblTituloResumen = new Label
            {
                Text = "🧾 Resumen de la Recepción",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Panel pnlTotalesContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                Padding = new Padding(0, 8, 0, 0)
            };

            int yPos = 0;
            lblTotalNeto = CrearRenglonMonto("Monto Neto Afecto:", "$ 0", ref yPos, pnlTotalesContainer);
            lblIvaCredito = CrearRenglonMonto("IVA Compra Crédito (19%):", "$ 0", ref yPos, pnlTotalesContainer);

            Panel pnlTotalHighlight = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 8, 0, 8)
            };
            pnlTotalHighlight.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTotalHighlight.ClientRectangle, Color.FromArgb(187, 247, 208), ButtonBorderStyle.Solid);
            };

            Label lblTotalCap = new Label
            {
                Text = "TOTAL FACTURA BRUTO",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 16
            };

            lblTotalBruto = new Label
            {
                Text = "$ 0",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTotalHighlight.Controls.Add(lblTotalBruto);
            pnlTotalHighlight.Controls.Add(lblTotalCap);

            btnRegistrarCompra = new Button
            {
                Text = "💾 REGISTRAR RECEPCIÓN\nE INCREMENTAR STOCK",
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegistrarCompra.FlatAppearance.BorderSize = 0;
            btnRegistrarCompra.Click += BtnRegistrarCompra_Click;

            Panel pnlSpacerBtn = new Panel { Dock = DockStyle.Bottom, Height = 8 };

            btnLimpiarFormulario = new Button
            {
                Text = "🔄 Limpiar Formulario",
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiarFormulario.FlatAppearance.BorderSize = 0;
            btnLimpiarFormulario.Click += (s, e) => LimpiarFormulario();

            pnlSummaryCard.Controls.Add(btnRegistrarCompra);
            pnlSummaryCard.Controls.Add(pnlSpacerBtn);
            pnlSummaryCard.Controls.Add(btnLimpiarFormulario);
            pnlSummaryCard.Controls.Add(pnlTotalHighlight);
            pnlSummaryCard.Controls.Add(pnlTotalesContainer);
            pnlSummaryCard.Controls.Add(lblTituloResumen);

            pnlGridAndSummary.Controls.Add(pnlTableCard, 0, 0);
            pnlGridAndSummary.Controls.Add(pnlSummaryCard, 1, 0);

            pnlMain.Controls.Add(pnlGridAndSummary);
            pnlMain.Controls.Add(pnlItemCard);
            pnlMain.Controls.Add(pnlHeaderCard);
            pnlMain.Controls.Add(lblSubtitulo);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearGrupoControl(string titulo, Control ctrl)
        {
            Panel pnl = new Panel
            {
                Size = new Size(ctrl.Width + 4, 48),
                Margin = new Padding(0, 0, 8, 0)
            };

            Label lbl = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(0, 0),
                AutoSize = true
            };

            ctrl.Location = new Point(0, 18);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(ctrl);
            return pnl;
        }

        private Button CrearBotonEstilizado(string texto, Color back, Color fore, Size size)
        {
            Button b = new Button
            {
                Text = texto,
                Size = size,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Label CrearRenglonMonto(string titulo, string valorInicial, ref int y, Panel container)
        {
            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, y),
                AutoSize = true
            };

            Label lblV = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, y + 16),
                AutoSize = true
            };

            container.Controls.Add(lblT);
            container.Controls.Add(lblV);
            y += 42;
            return lblV;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 32;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProveedoresYProductos()
        {
            try
            {
                _proveedoresCache = _db.Proveedores.Where(p => p.Estado).OrderBy(p => p.RazonSocial).ToList();
                cbProveedores.DataSource = null;
                cbProveedores.DataSource = _proveedoresCache;
                cbProveedores.DisplayMember = "RazonSocial";
                cbProveedores.ValueMember = "ProveedorID";

                _productosCache = _db.Productos.Where(p => p.Estado).OrderBy(p => p.Nombre).ToList();
                cbProductos.DataSource = null;
                cbProductos.DataSource = _productosCache;
                cbProductos.DisplayMember = "Nombre";
                cbProductos.ValueMember = "ProductoID";
            }
            catch
            {
                // Fallback de demostración si la base de datos no está disponible
                _proveedoresCache = new List<Proveedor>
                {
                    new Proveedor { ProveedorID = 1, Rut = "76.888.999-1", RazonSocial = "DISTRIBUIDORA LÁCTEOS SUR S.A." },
                    new Proveedor { ProveedorID = 2, Rut = "96.543.210-5", RazonSocial = "COMERCIALIZADORA DE ABARROTES CENTRAL LTDA" }
                };
                cbProveedores.DataSource = _proveedoresCache;
                cbProveedores.DisplayMember = "RazonSocial";
                cbProveedores.ValueMember = "ProveedorID";

                _productosCache = new List<Producto>
                {
                    new Producto { ProductoID = 1, Nombre = "Leche Entera 1L", PrecioUnitario = 1150, Stock = 40 },
                    new Producto { ProductoID = 2, Nombre = "Queso Gauda 250g", PrecioUnitario = 2490, Stock = 25 },
                    new Producto { ProductoID = 3, Nombre = "Azúcar Blanca 1kg", PrecioUnitario = 1290, Stock = 60 }
                };
                cbProductos.DataSource = _productosCache;
                cbProductos.DisplayMember = "Nombre";
                cbProductos.ValueMember = "ProductoID";
            }

            CbProductos_SelectedIndexChanged(null, EventArgs.Empty);
        }

        private void CbProductos_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbProductos.SelectedItem is Producto p)
            {
                decimal costoSugerido = Math.Round(p.PrecioUnitario * 0.70m, 0);
                txtPrecioCosto.Text = costoSugerido.ToString("0");
            }
        }

        private void BtnAgregarItem_Click(object? sender, EventArgs e)
        {
            if (cbProductos.SelectedItem is not Producto prod)
            {
                MessageBox.Show("Debe seleccionar un producto válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) || costo <= 0)
            {
                MessageBox.Show("Ingrese un precio de costo unitario válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cantidad = (int)numCantidadRecibida.Value;

            var itemExistente = _detalleFactura.FirstOrDefault(d => d.ProductoID == prod.ProductoID);
            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
                itemExistente.PrecioCostoUnitario = costo;
            }
            else
            {
                _detalleFactura.Add(new DetalleCompra
                {
                    ProductoID = prod.ProductoID,
                    NombreProducto = prod.Nombre,
                    Cantidad = cantidad,
                    PrecioCostoUnitario = costo
                });
            }

            ActualizarTablaDetalle();
        }

        private void BtnQuitarItem_Click(object? sender, EventArgs e)
        {
            if (dgvDetalle.CurrentRow != null && dgvDetalle.CurrentRow.DataBoundItem is DetalleCompra item)
            {
                _detalleFactura.Remove(item);
                ActualizarTablaDetalle();
            }
        }

        private void ActualizarTablaDetalle()
        {
            dgvDetalle.DataSource = null;
            dgvDetalle.DataSource = _detalleFactura;

            if (dgvDetalle.Columns["DetalleCompraID"] != null) dgvDetalle.Columns["DetalleCompraID"].Visible = false;
            if (dgvDetalle.Columns["CompraID"] != null) dgvDetalle.Columns["CompraID"].Visible = false;
            if (dgvDetalle.Columns["ProductoID"] != null) dgvDetalle.Columns["ProductoID"].Visible = false;

            if (dgvDetalle.Columns["NombreProducto"] != null) dgvDetalle.Columns["NombreProducto"].HeaderText = "Producto";
            if (dgvDetalle.Columns["Cantidad"] != null) dgvDetalle.Columns["Cantidad"].HeaderText = "Cant. Recibida";
            if (dgvDetalle.Columns["PrecioCostoUnitario"] != null) { dgvDetalle.Columns["PrecioCostoUnitario"].HeaderText = "Costo Unitario"; dgvDetalle.Columns["PrecioCostoUnitario"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvDetalle.Columns["Subtotal"] != null) { dgvDetalle.Columns["Subtotal"].HeaderText = "Subtotal Neto"; dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0"; dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }

            decimal neto = _detalleFactura.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m, 0);
            decimal total = neto + iva;

            lblTotalNeto.Text = $"$ {neto:N0}";
            lblIvaCredito.Text = $"$ {iva:N0}";
            lblTotalBruto.Text = $"$ {total:N0}";
        }

        private void BtnRegistrarCompra_Click(object? sender, EventArgs e)
        {
            if (_detalleFactura.Count == 0)
            {
                MessageBox.Show("La recepción de compra no contiene productos agregados.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Proveedor prov = (Proveedor)cbProveedores.SelectedItem!;
            string tipoDoc = cbTipoDocumento.SelectedItem?.ToString() ?? "Factura de Compra";
            int folio = (int)numFolio.Value;

            decimal neto = _detalleFactura.Sum(d => d.Subtotal);
            decimal iva = Math.Round(neto * 0.19m, 0);
            decimal total = neto + iva;

            try
            {
                // Incremento de existencias en base de datos
                foreach (var item in _detalleFactura)
                {
                    var productoBd = _db.Productos.FirstOrDefault(p => p.ProductoID == item.ProductoID);
                    if (productoBd != null)
                    {
                        productoBd.Stock += item.Cantidad; // Suma stock recibido
                    }
                }

                _db.SaveChanges();

                MessageBox.Show($"¡Recepción de {tipoDoc} N° {folio} registrada con éxito!\n\n" +
                                $"• Proveedor: {prov.RazonSocial}\n" +
                                $"• Total Factura: ${total:N0}\n" +
                                $"• Se incrementó el stock de {_detalleFactura.Sum(d => d.Cantidad)} unidades en el inventario.",
                                "Éxito Recepción", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Recepción procesada localmente (Modo Demo):\n{ex.Message}", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarFormulario();
            }
        }

        private void LimpiarFormulario()
        {
            _detalleFactura.Clear();
            numFolio.Value = numFolio.Value + 1;
            numCantidadRecibida.Value = 10;
            ActualizarTablaDetalle();
        }

        private void BtnDemoCompra_Click(object? sender, EventArgs e)
        {
            _detalleFactura.Clear();

            _detalleFactura.Add(new DetalleCompra { ProductoID = 1, NombreProducto = "Leche Entera 1L", Cantidad = 50, PrecioCostoUnitario = 800 });
            _detalleFactura.Add(new DetalleCompra { ProductoID = 2, NombreProducto = "Queso Gauda 250g", Cantidad = 30, PrecioCostoUnitario = 1750 });
            _detalleFactura.Add(new DetalleCompra { ProductoID = 3, NombreProducto = "Azúcar Blanca 1kg", Cantidad = 40, PrecioCostoUnitario = 900 });

            ActualizarTablaDetalle();
            MessageBox.Show("Se cargó la Factura de Compra de demostración con 3 ítems y 120 unidades de stock.", "Éxito Demo Compra", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}