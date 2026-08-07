using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormProductos : Form
    {
        private AppDbContext _db = new AppDbContext();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private Button btnRefrescar = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEstado = null!;
        private Button btnDemo = null!;

        private DataGridView dgvProductos = null!;
        private Producto? _productoSeleccionado = null;

        public FormProductos()
        {
            InitializeComponent();
            CargarProductos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(15, 12, 15, 12)
            };

            Label lblBuscar = new Label
            {
                Text = "🔍 Buscar:",
                Location = new Point(15, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            txtBuscar = new TextBox
            {
                Location = new Point(85, 18),
                Size = new Size(160, 32),
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarProductos(txtBuscar.Text.Trim()); e.SuppressKeyPress = true; } };

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(255, 17),
                Size = new Size(70, 34),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarProductos(txtBuscar.Text.Trim());

            btnRefrescar = new Button
            {
                Text = "🔄 Recargar",
                Location = new Point(332, 17),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += (s, e) => { txtBuscar.Clear(); CargarProductos(); };

            btnNuevo = new Button
            {
                Text = "➕ Nuevo Producto",
                Dock = DockStyle.Right,
                Width = 135,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = new Button
            {
                Text = "✏️ Editar",
                Dock = DockStyle.Right,
                Width = 85,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += BtnEditar_Click;

            btnEstado = new Button
            {
                Text = "🔄 Estado",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEstado.FlatAppearance.BorderSize = 0;
            btnEstado.Click += BtnEstado_Click;

            btnDemo = new Button
            {
                Text = "✨ Cargar Productos Demo",
                Dock = DockStyle.Right,
                Width = 175,
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDemo.FlatAppearance.BorderSize = 0;
            btnDemo.Click += BtnCargarDemo_Click;

            pnlHeader.Controls.Add(lblBuscar);
            pnlHeader.Controls.Add(txtBuscar);
            pnlHeader.Controls.Add(btnBuscar);
            pnlHeader.Controls.Add(btnRefrescar);
            pnlHeader.Controls.Add(btnDemo);
            pnlHeader.Controls.Add(btnEstado);
            pnlHeader.Controls.Add(btnEditar);
            pnlHeader.Controls.Add(btnNuevo);

            dgvProductos = new DataGridView
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
            ConfigurarEstiloTabla(dgvProductos);
            dgvProductos.SelectionChanged += DgvProductos_SelectionChanged;

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 15, 0, 0)
            };
            pnlGridCard.Controls.Add(dgvProductos);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 38;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProductos(string filtro = "")
        {
            try
            {
                var query = _db.Productos.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    query = query.Where(p => (p.Nombre != null && p.Nombre.Contains(filtro)) || (p.CodigoBarra != null && p.CodigoBarra.Contains(filtro)));
                }

                var productos = query.OrderBy(p => p.Nombre).ToList();
                dgvProductos.DataSource = productos;

                if (dgvProductos.Columns["ProductoID"] != null) dgvProductos.Columns["ProductoID"].HeaderText = "ID";
                if (dgvProductos.Columns["CodigoBarra"] != null) dgvProductos.Columns["CodigoBarra"].HeaderText = "Código de Barra";
                if (dgvProductos.Columns["Nombre"] != null) dgvProductos.Columns["Nombre"].HeaderText = "Nombre del Producto";
                if (dgvProductos.Columns["PrecioUnitario"] != null)
                {
                    dgvProductos.Columns["PrecioUnitario"].HeaderText = "Precio Unitario";
                    dgvProductos.Columns["PrecioUnitario"].DefaultCellStyle.Format = "$#,##0";
                }
                if (dgvProductos.Columns["Stock"] != null) dgvProductos.Columns["Stock"].HeaderText = "Stock Disponible";
                if (dgvProductos.Columns["Estado"] != null) dgvProductos.Columns["Estado"].HeaderText = "Estado";
                if (dgvProductos.Columns["ImagenPath"] != null) dgvProductos.Columns["ImagenPath"].HeaderText = "Ruta Imagen";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvProductos_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvProductos.CurrentRow != null && dgvProductos.CurrentRow.DataBoundItem is Producto producto)
            {
                _productoSeleccionado = producto;
                btnEditar.Enabled = true;
                btnEstado.Enabled = true;
            }
            else
            {
                _productoSeleccionado = null;
                btnEditar.Enabled = false;
                btnEstado.Enabled = false;
            }
        }

        private void BtnCargarDemo_Click(object? sender, EventArgs e)
        {
            try
            {
                var productosDemo = new List<Producto>
                {
                    new Producto { CodigoBarra = "780123456781", Nombre = "Leche Entera 1L", PrecioUnitario = 1150, Stock = 40, Estado = true },
                    new Producto { CodigoBarra = "780123456782", Nombre = "Queso Gauda 250g", PrecioUnitario = 2490, Stock = 25, Estado = true },
                    new Producto { CodigoBarra = "780123456783", Nombre = "Jamón Pierna 200g", PrecioUnitario = 1990, Stock = 20, Estado = true },
                    new Producto { CodigoBarra = "780123456784", Nombre = "Bebida Sprite 1.5L", PrecioUnitario = 1500, Stock = 30, Estado = true },
                    new Producto { CodigoBarra = "780123456785", Nombre = "Galletas Tritón 126g", PrecioUnitario = 850, Stock = 50, Estado = true },
                    new Producto { CodigoBarra = "780123456786", Nombre = "Café Nescafé 170g", PrecioUnitario = 4200, Stock = 15, Estado = true },
                    new Producto { CodigoBarra = "780123456787", Nombre = "Azúcar Blanca 1kg", PrecioUnitario = 1290, Stock = 60, Estado = true },
                    new Producto { CodigoBarra = "780123456788", Nombre = "Aceite Vegetal 900ml", PrecioUnitario = 2190, Stock = 35, Estado = true },
                    new Producto { CodigoBarra = "780123456789", Nombre = "Papas Chips 130g", PrecioUnitario = 1490, Stock = 45, Estado = true },
                    new Producto { CodigoBarra = "780123456790", Nombre = "Yogurt Frutilla 125g", PrecioUnitario = 450, Stock = 80, Estado = true }
                };

                int agregados = 0;
                foreach (var p in productosDemo)
                {
                    bool existe = _db.Productos.Any(x => x.CodigoBarra == p.CodigoBarra || x.Nombre == p.Nombre);
                    if (!existe)
                    {
                        _db.Productos.Add(p);
                        agregados++;
                    }
                }

                if (agregados > 0)
                {
                    _db.SaveChanges();
                    MessageBox.Show($"¡Se agregaron {agregados} productos de prueba exitosamente!", "Éxito Demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarProductos();
                }
                else
                {
                    MessageBox.Show("Los productos de prueba ya existían en tu base de datos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos demo: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            MostrarFormularioModal(null);
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado != null)
            {
                MostrarFormularioModal(_productoSeleccionado);
            }
        }

        private void BtnEstado_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null) return;

            string accion = _productoSeleccionado.Estado ? "desactivar" : "activar";
            var result = MessageBox.Show($"¿Desea {accion} el producto '{_productoSeleccionado.Nombre}'?", "Confirmar cambio", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _productoSeleccionado.Estado = !_productoSeleccionado.Estado;
                    _db.SaveChanges();
                    CargarProductos(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar estado: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MostrarFormularioModal(Producto? producto)
        {
            bool esNuevo = (producto == null);
            Producto p = producto ?? new Producto();
            string rutaImagenTemporal = p.ImagenPath ?? string.Empty;

            Form modal = new Form
            {
                Text = esNuevo ? "Registrar Nuevo Producto" : "Editar Producto",
                Size = new Size(420, 560),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = esNuevo ? "📦 Nuevo Producto" : "✏️ Editar Producto", Location = new Point(25, 15), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };

            PictureBox pbImagen = new PictureBox
            {
                Location = new Point(25, 50),
                Size = new Size(90, 90),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            if (!string.IsNullOrEmpty(p.ImagenPath) && File.Exists(p.ImagenPath))
            {
                try { pbImagen.Image = Image.FromFile(p.ImagenPath); } catch { }
            }

            Button btnCargarFoto = new Button
            {
                Text = "📷 Seleccionar Foto",
                Location = new Point(130, 75),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCargarFoto.FlatAppearance.BorderSize = 0;

            btnCargarFoto.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Seleccionar Imagen del Producto";
                    ofd.Filter = "Archivos de Imagen|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.jfif;*.JPG;*.PNG;*.JFIF|Todos los archivos (*.*)|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string carpetaDestino = Path.Combine(Application.StartupPath, "Imagenes");
                            if (!Directory.Exists(carpetaDestino))
                            {
                                Directory.CreateDirectory(carpetaDestino);
                            }

                            string extension = Path.GetExtension(ofd.FileName);
                            string nombreArchivo = $"prod_{DateTime.Now.Ticks}{extension}";
                            string rutaFinal = Path.Combine(carpetaDestino, nombreArchivo);

                            File.Copy(ofd.FileName, rutaFinal, true);
                            rutaImagenTemporal = rutaFinal;

                            if (pbImagen.Image != null) pbImagen.Image.Dispose();
                            pbImagen.Image = Image.FromFile(rutaFinal);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error al cargar imagen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            Label lblCodigo = new Label { Text = "Código de Barra:", Location = new Point(25, 155), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtCodigo = new TextBox { Text = p.CodigoBarra ?? string.Empty, Location = new Point(25, 177), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Label lblNombre = new Label { Text = "Nombre del Producto:", Location = new Point(25, 217), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtNombre = new TextBox { Text = p.Nombre ?? string.Empty, Location = new Point(25, 239), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Label lblPrecio = new Label { Text = "Precio Unitario ($):", Location = new Point(25, 279), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtPrecio = new TextBox { Text = p.PrecioUnitario.ToString("0"), Location = new Point(25, 301), Size = new Size(165, 30), Font = new Font("Segoe UI", 10F) };

            Label lblStock = new Label { Text = "Stock Inicial:", Location = new Point(210, 279), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numStock = new NumericUpDown { Value = p.Stock, Location = new Point(210, 301), Size = new Size(165, 30), Font = new Font("Segoe UI", 10F), Maximum = 100000 };

            Button btnGuardar = new Button
            {
                Text = esNuevo ? "💾 Guardar Producto" : "💾 Guardar Cambios",
                Location = new Point(25, 440),
                Size = new Size(350, 42),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

            btnGuardar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre del producto es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPrecio.Text.Trim(), out decimal precio) || precio < 0)
                {
                    MessageBox.Show("Ingrese un precio válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                p.CodigoBarra = txtCodigo.Text.Trim();
                p.Nombre = txtNombre.Text.Trim();
                p.PrecioUnitario = precio;
                p.Stock = (int)numStock.Value;
                p.ImagenPath = rutaImagenTemporal;

                try
                {
                    if (esNuevo)
                    {
                        _db.Productos.Add(p);
                    }
                    _db.SaveChanges();

                    MessageBox.Show("Producto guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modal.Close();
                    CargarProductos(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar producto: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modal.Controls.AddRange(new Control[] { lblTitle, pbImagen, btnCargarFoto, lblCodigo, txtCodigo, lblNombre, txtNombre, lblPrecio, txtPrecio, lblStock, numStock, btnGuardar });
            modal.ShowDialog();
        }
    }
}