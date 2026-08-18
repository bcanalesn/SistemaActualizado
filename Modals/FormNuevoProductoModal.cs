using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
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
        private ComboBox cbCategoria = null!;
        private ComboBox cbFamilia = null!;
        private TextBox txtPrecioCosto = null!;
        private TextBox txtMargenGanancia = null!;
        private TextBox txtPrecioUnitario = null!;
        private TextBox txtStockInicial = null!;
        private TextBox txtStockMinimo = null!;
        private PictureBox pbFoto = null!;
        private Button btnCargarFoto = null!;
        private Button btnQuitarFoto = null!;
        private Button btnGuardar = null!;
        private Button btnCancelar = null!;
        private Label lblTitulo = null!;

        private string _rutaImagenSeleccionada = string.Empty;
        private readonly Producto? _productoAEditar;
        public Producto? ProductoResultado { get; private set; }
        public int CantidadFactura { get; private set; } = 1;

        public FormNuevoProductoModal(Producto? producto = null)
        {
            _productoAEditar = producto;
            InitializeComponent();
            CargarListasDesplegables();
            CargarDatosSiEsEdicion();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(520, 640);
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

            // Encabezado
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            pnlHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            lblTitulo = new Label
            {
                Text = _productoAEditar == null ? "📦 Registrar Nuevo Producto" : "✏️ Editar Producto",
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

            // Contenedor principal de campos
            FlowLayoutPanel flowCampos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0),
                AutoScroll = true
            };

            txtCodigoBarra = new TextBox { Width = 465, Font = new Font("Segoe UI", 9.5F) };
            txtNombre = new TextBox { Width = 465, Font = new Font("Segoe UI", 9.5F) };

            cbCategoria = new ComboBox { Width = 225, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDown };
            cbFamilia = new ComboBox { Width = 225, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDown };

            txtPrecioCosto = new TextBox { Width = 225, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtPrecioCosto.TextChanged += (s, e) => RecalcularPrecioVenta();

            txtMargenGanancia = new TextBox { Width = 225, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "30" };
            txtMargenGanancia.TextChanged += (s, e) => RecalcularPrecioVenta();

            txtPrecioUnitario = new TextBox { Width = 465, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtStockInicial = new TextBox { Width = 225, Font = new Font("Segoe UI", 9.5F), Text = "1" };
            txtStockMinimo = new TextBox { Width = 225, Font = new Font("Segoe UI", 9.5F), Text = "5" };

            // Panel dual para Categoría y Familia
            Panel pnlCatFam = new Panel { Size = new Size(470, 52) };
            pnlCatFam.Controls.Add(CrearSubCampo("Categoría:", cbCategoria, 0));
            pnlCatFam.Controls.Add(CrearSubCampo("Familia de Producto:", cbFamilia, 240));

            // Panel dual para Costo y Margen
            Panel pnlCostoMargen = new Panel { Size = new Size(470, 52) };
            pnlCostoMargen.Controls.Add(CrearSubCampo("Costo Neto ($):", txtPrecioCosto, 0));
            pnlCostoMargen.Controls.Add(CrearSubCampo("Margen (%):", txtMargenGanancia, 240));

            // Panel dual para Stocks
            Panel pnlStocks = new Panel { Size = new Size(470, 52) };
            string labelStock = _productoAEditar == null ? "Cant. Factura / Inicial:" : "Stock Actual:";
            pnlStocks.Controls.Add(CrearSubCampo(labelStock, txtStockInicial, 0));
            pnlStocks.Controls.Add(CrearSubCampo("Stock Mínimo Alerta:", txtStockMinimo, 240));

            // Sección Foto del Producto
            Panel pnlFotoSection = new Panel { Size = new Size(470, 75), Margin = new Padding(0, 4, 0, 4) };
            Label lblFotoTitle = new Label { Text = "Foto del Producto:", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };

            pbFoto = new PictureBox
            {
                Location = new Point(0, 18),
                Size = new Size(54, 54),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            btnCargarFoto = new Button
            {
                Text = "📁 Seleccionar Imagen...",
                Location = new Point(62, 26),
                Size = new Size(160, 32),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCargarFoto.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCargarFoto.Click += BtnCargarFoto_Click;

            btnQuitarFoto = new Button
            {
                Text = "Quitar",
                Location = new Point(228, 26),
                Size = new Size(70, 32),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuitarFoto.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnQuitarFoto.Click += (s, e) =>
            {
                _rutaImagenSeleccionada = string.Empty;
                pbFoto.Image = null;
            };

            pnlFotoSection.Controls.AddRange(new Control[] { lblFotoTitle, pbFoto, btnCargarFoto, btnQuitarFoto });

            // Agregar renglones al layout
            flowCampos.Controls.Add(CrearRenglonCampo("Código de Barra / SKU:", txtCodigoBarra));
            flowCampos.Controls.Add(CrearRenglonCampo("Nombre del Producto:", txtNombre));
            flowCampos.Controls.Add(pnlCatFam);
            flowCampos.Controls.Add(pnlCostoMargen);
            flowCampos.Controls.Add(CrearRenglonCampo("Precio Venta Público (IVA Incluido):", txtPrecioUnitario));
            flowCampos.Controls.Add(pnlStocks);
            flowCampos.Controls.Add(pnlFotoSection);

            // Botonera Inferior
            Panel pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 48 };

            btnGuardar = new Button
            {
                Text = _productoAEditar == null ? "💾 Guardar y Usar" : "💾 Guardar Cambios",
                Size = new Size(180, 38),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(285, 4)
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
                Location = new Point(175, 4)
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
            Panel p = new Panel { Size = new Size(470, 52), Margin = new Padding(0, 0, 0, 4) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 18);
            p.Controls.Add(l);
            p.Controls.Add(ctrl);
            return p;
        }

        private Panel CrearSubCampo(string titulo, Control ctrl, int x)
        {
            Panel p = new Panel { Location = new Point(x, 0), Size = new Size(225, 52) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 18);
            p.Controls.Add(l);
            p.Controls.Add(ctrl);
            return p;
        }

        private void CargarListasDesplegables()
        {
            try
            {
                using var db = new AppDbContext();

                var categorias = db.Productos
                    .Where(p => p.Estado && !string.IsNullOrEmpty(p.Categoria))
                    .Select(p => p.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                var familias = db.Productos
                    .Where(p => p.Estado && !string.IsNullOrEmpty(p.NFamilia))
                    .Select(p => p.NFamilia!)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToList();

                cbCategoria.Items.Clear();
                cbCategoria.Items.AddRange(categorias.Count > 0 ? categorias.ToArray() : new string[] { "General", "Abarrotes", "Lácteos", "Bebidas", "Cecinas", "Aseo", "Snacks" });

                cbFamilia.Items.Clear();
                cbFamilia.Items.AddRange(familias.Count > 0 ? familias.ToArray() : new string[] { "General", "Lácteos", "Cecinas", "Abarrotes", "Bebidas", "Aseo", "Snacks", "Panadería", "Congelados" });

                cbCategoria.Text = "General";
                cbFamilia.Text = "General";
            }
            catch
            {
                cbCategoria.Text = "General";
                cbFamilia.Text = "General";
            }
        }

        private void CargarDatosSiEsEdicion()
        {
            if (_productoAEditar != null)
            {
                txtCodigoBarra.Text = _productoAEditar.CodigoBarra;
                txtNombre.Text = _productoAEditar.Nombre;
                cbCategoria.Text = !string.IsNullOrWhiteSpace(_productoAEditar.Categoria) ? _productoAEditar.Categoria : "General";
                cbFamilia.Text = !string.IsNullOrWhiteSpace(_productoAEditar.NFamilia) ? _productoAEditar.NFamilia : cbCategoria.Text;
                txtPrecioCosto.Text = _productoAEditar.PrecioCosto.ToString("0");
                txtMargenGanancia.Text = _productoAEditar.MargenGanancia.ToString("0");
                txtPrecioUnitario.Text = _productoAEditar.PrecioUnitario.ToString("0");
                txtStockInicial.Text = _productoAEditar.Stock.ToString();
                txtStockMinimo.Text = _productoAEditar.StockMinimo.ToString();
                txtStockInicial.Enabled = false;

                _rutaImagenSeleccionada = _productoAEditar.ImagenPath ?? string.Empty;
                if (!string.IsNullOrEmpty(_rutaImagenSeleccionada) && File.Exists(_rutaImagenSeleccionada))
                {
                    try { pbFoto.Image = Image.FromFile(_rutaImagenSeleccionada); } catch { }
                }
            }
        }

        private void BtnCargarFoto_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Todos los archivos (*.*)|*.*";
                ofd.Title = "Seleccionar Imagen del Producto";

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    _rutaImagenSeleccionada = ofd.FileName;
                    try
                    {
                        using var imgStream = File.OpenRead(_rutaImagenSeleccionada);
                        pbFoto.Image = Image.FromStream(imgStream);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo cargar la imagen: {ex.Message}", "Error de Imagen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
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

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cat = string.IsNullOrWhiteSpace(cbCategoria.Text) ? "General" : cbCategoria.Text.Trim();
            string fam = string.IsNullOrWhiteSpace(cbFamilia.Text) ? cat : cbFamilia.Text.Trim();

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
                    // Producto nuevo en memoria (ID = 0)
                    var nuevoEnMemoria = new Producto
                    {
                        ProductoID = 0,
                        CodigoBarra = string.IsNullOrWhiteSpace(txtCodigoBarra.Text) ? "GEN-" + DateTime.Now.Ticks.ToString().Substring(12) : txtCodigoBarra.Text.Trim(),
                        Nombre = nombre,
                        Categoria = cat,
                        NFamilia = fam,
                        PrecioCosto = costo,
                        MargenGanancia = margen > 0 ? margen : 30m,
                        PrecioUnitario = pvp,
                        Stock = 0,
                        StockMinimo = stockMin > 0 ? stockMin : 5,
                        ImagenPath = _rutaImagenSeleccionada,
                        Estado = true
                    };

                    ProductoResultado = nuevoEnMemoria;
                }
                else
                {
                    // Edición directa en la base de datos
                    using var db = new AppDbContext();
                    var prodBd = db.Productos.Find(_productoAEditar.ProductoID);
                    if (prodBd != null)
                    {
                        prodBd.CodigoBarra = txtCodigoBarra.Text.Trim();
                        prodBd.Nombre = nombre;
                        prodBd.Categoria = cat;
                        prodBd.NFamilia = fam;
                        prodBd.PrecioCosto = costo;
                        prodBd.MargenGanancia = margen;
                        prodBd.PrecioUnitario = pvp;
                        prodBd.StockMinimo = stockMin;
                        prodBd.ImagenPath = _rutaImagenSeleccionada;
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
                MessageBox.Show($"Error al preparar producto:\n{ex.Message}{mensajeDetalle}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}