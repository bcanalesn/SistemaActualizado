using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormNuevoProductoModal : Form
    {
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private TextBox txtCodigoBarra = null!;
        private TextBox txtNombre = null!;
        private TextBox txtCategoria = null!;
        private TextBox txtPrecioCosto = null!;
        private TextBox txtMargenGanancia = null!;
        private TextBox txtPrecioUnitario = null!;
        private TextBox txtStockInicial = null!;
        private TextBox txtStockMinimo = null!;
        private Button btnGuardar = null!;
        private Button btnCancelar = null!;
        private Label lblTitulo = null!;

        private readonly Producto? _productoAEditar;
        public Producto? ProductoResultado { get; private set; }
        public int CantidadFactura { get; private set; } = 1;

        public FormNuevoProductoModal(Producto? producto = null)
        {
            _productoAEditar = producto;
            InitializeComponent();
            CargarDatosSiEsEdicion();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(480, 550);
            this.BackColor = Color.White;

            Panel pnlBorde = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 14, 22, 18),
                BackColor = Color.White
            };
            pnlBorde.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlBorde.ClientRectangle, Color.FromArgb(203, 213, 225), ButtonBorderStyle.Solid);
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            pnlHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            lblTitulo = new Label
            {
                Text = _productoAEditar == null ? "📦 Registrar Nuevo Producto en Catálogo" : "✏️ Editar Producto",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            lblTitulo.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            Button btnCerrar = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(btnCerrar);

            FlowLayoutPanel flowCampos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0),
                AutoScroll = true
            };

            txtCodigoBarra = new TextBox { Width = 425, Font = new Font("Segoe UI", 9.5F) };
            txtNombre = new TextBox { Width = 425, Font = new Font("Segoe UI", 9.5F) };
            txtCategoria = new TextBox { Width = 425, Font = new Font("Segoe UI", 9.5F), Text = "General" };

            txtPrecioCosto = new TextBox { Width = 205, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtPrecioCosto.TextChanged += (s, e) => RecalcularPrecioVenta();

            txtMargenGanancia = new TextBox { Width = 205, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "30" };
            txtMargenGanancia.TextChanged += (s, e) => RecalcularPrecioVenta();

            txtPrecioUnitario = new TextBox { Width = 425, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtStockInicial = new TextBox { Width = 205, Font = new Font("Segoe UI", 9.5F), Text = "1" };
            txtStockMinimo = new TextBox { Width = 205, Font = new Font("Segoe UI", 9.5F), Text = "5" };

            Panel pnlCostoMargen = new Panel { Size = new Size(430, 52) };
            pnlCostoMargen.Controls.Add(CrearSubCampo("Costo Neto ($):", txtPrecioCosto, 0));
            pnlCostoMargen.Controls.Add(CrearSubCampo("Margen (%):", txtMargenGanancia, 220));

            Panel pnlStocks = new Panel { Size = new Size(430, 52) };
            string labelStock = _productoAEditar == null ? "Cant. Recibida en Factura:" : "Stock Actual:";
            pnlStocks.Controls.Add(CrearSubCampo(labelStock, txtStockInicial, 0));
            pnlStocks.Controls.Add(CrearSubCampo("Stock Mínimo Alerta:", txtStockMinimo, 220));

            flowCampos.Controls.Add(CrearRenglonCampo("Código de Barra / SKU:", txtCodigoBarra));
            flowCampos.Controls.Add(CrearRenglonCampo("Nombre del Producto:", txtNombre));
            flowCampos.Controls.Add(CrearRenglonCampo("Categoría:", txtCategoria));
            flowCampos.Controls.Add(pnlCostoMargen);
            flowCampos.Controls.Add(CrearRenglonCampo("Precio Venta Público (IVA Incluido):", txtPrecioUnitario));
            flowCampos.Controls.Add(pnlStocks);

            Panel pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 45 };

            btnGuardar = new Button
            {
                Text = _productoAEditar == null ? "💾 Guardar y Usar" : "💾 Guardar Cambios",
                Size = new Size(180, 38),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(245, 4)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(135, 4)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            pnlBotones.Controls.Add(btnGuardar);
            pnlBotones.Controls.Add(btnCancelar);

            pnlBorde.Controls.Add(flowCampos);
            pnlBorde.Controls.Add(pnlBotones);
            pnlBorde.Controls.Add(pnlHeader);

            this.Controls.Add(pnlBorde);
        }

        private Panel CrearRenglonCampo(string titulo, Control ctrl)
        {
            Panel p = new Panel { Size = new Size(430, 52), Margin = new Padding(0, 0, 0, 4) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 18);
            p.Controls.Add(l);
            p.Controls.Add(ctrl);
            return p;
        }

        private Panel CrearSubCampo(string titulo, Control ctrl, int x)
        {
            Panel p = new Panel { Location = new Point(x, 0), Size = new Size(205, 52) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 18);
            p.Controls.Add(l);
            p.Controls.Add(ctrl);
            return p;
        }

        private void RecalcularPrecioVenta()
        {
            if (decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) &&
                decimal.TryParse(txtMargenGanancia.Text.Trim(), out decimal margen) && costo > 0)
            {
                decimal neto = costo * (1 + (margen / 100m));
                decimal pvp = neto * 1.19m;
                decimal pvpRedondeado = Math.Round(pvp / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
                txtPrecioUnitario.Text = pvpRedondeado.ToString("0");
            }
        }

        private void CargarDatosSiEsEdicion()
        {
            if (_productoAEditar != null)
            {
                txtCodigoBarra.Text = _productoAEditar.CodigoBarra;
                txtNombre.Text = _productoAEditar.Nombre;
                txtCategoria.Text = _productoAEditar.Categoria;
                txtPrecioCosto.Text = _productoAEditar.PrecioCosto.ToString("0");
                txtMargenGanancia.Text = _productoAEditar.MargenGanancia.ToString("0");
                txtPrecioUnitario.Text = _productoAEditar.PrecioUnitario.ToString("0");
                txtStockInicial.Text = _productoAEditar.Stock.ToString();
                txtStockMinimo.Text = _productoAEditar.StockMinimo.ToString();
                txtStockInicial.Enabled = false;
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo);
            decimal.TryParse(txtMargenGanancia.Text.Trim(), out decimal margen);
            decimal.TryParse(txtPrecioUnitario.Text.Trim(), out decimal pvp);
            int.TryParse(txtStockInicial.Text.Trim(), out int cantFactura);
            int.TryParse(txtStockMinimo.Text.Trim(), out int stockMin);

            if (cantFactura <= 0) cantFactura = 1;
            CantidadFactura = cantFactura;

            try
            {
                if (_productoAEditar == null)
                {
                    // Producto nuevo: creado en memoria (ID = 0)
                    var nuevoEnMemoria = new Producto
                    {
                        ProductoID = 0,
                        CodigoBarra = string.IsNullOrWhiteSpace(txtCodigoBarra.Text) ? "GEN-" + DateTime.Now.Ticks.ToString().Substring(12) : txtCodigoBarra.Text.Trim(),
                        Nombre = nombre,
                        Categoria = string.IsNullOrWhiteSpace(txtCategoria.Text) ? "General" : txtCategoria.Text.Trim(),
                        PrecioCosto = costo,
                        MargenGanancia = margen > 0 ? margen : 30m,
                        PrecioUnitario = pvp,
                        Stock = 0,
                        StockMinimo = stockMin > 0 ? stockMin : 5,
                        ImagenPath = "",
                        Estado = true
                    };

                    ProductoResultado = nuevoEnMemoria;
                }
                else
                {
                    // Edición de producto existente en base de datos
                    using var db = new AppDbContext();
                    var prodBd = db.Productos.Find(_productoAEditar.ProductoID);
                    if (prodBd != null)
                    {
                        prodBd.CodigoBarra = txtCodigoBarra.Text.Trim();
                        prodBd.Nombre = nombre;
                        prodBd.Categoria = txtCategoria.Text.Trim();
                        prodBd.PrecioCosto = costo;
                        prodBd.MargenGanancia = margen;
                        prodBd.PrecioUnitario = pvp;
                        prodBd.StockMinimo = stockMin;
                        db.SaveChanges();
                        ProductoResultado = prodBd;
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                string mensajeDetalle = ex.InnerException != null ? $"\nDetalle: {ex.InnerException.Message}" : "";
                MessageBox.Show($"Error al preparar producto:\n{ex.Message}{mensajeDetalle}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}