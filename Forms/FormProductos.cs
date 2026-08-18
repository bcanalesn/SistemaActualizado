using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;

namespace SISTEMAACTUALIZADO
{
    public class FormProductos : Form
    {
        private DataGridView dgvProductos = null!;
        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private Button btnRecargar = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEliminar = null!;
        private List<Producto> _listaProductos = new List<Producto>();

        public FormProductos()
        {
            InitializeComponent();
            this.Shown += (s, e) => CargarProductos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            // Barra Superior de Controles Adaptable
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(15, 12, 15, 12),
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlTop.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTop.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            // Sección Izquierda: Búsqueda
            FlowLayoutPanel pnlLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 480,
                Height = 40,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            Label lblBuscar = new Label 
            { 
                Text = "🔍 Buscar:", 
                AutoSize = true, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 7, 6, 0)
            };

            txtBuscar = new TextBox 
            { 
                Size = new Size(220, 28), 
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 3, 6, 0)
            };
            txtBuscar.TextChanged += (s, e) => FiltrarProductosInteligente();

            btnBuscar = CrearBoton("Buscar", Color.FromArgb(30, 41, 59), Color.White, (s, e) => FiltrarProductosInteligente());
            btnRecargar = CrearBoton("🔄 Recargar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), (s, e) => CargarProductos());

            pnlLeft.Controls.AddRange(new Control[] { lblBuscar, txtBuscar, btnBuscar, btnRecargar });

            // Sección Derecha: Botones de Acción
            FlowLayoutPanel pnlRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 430,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnEliminar = CrearBoton("🗑️ Eliminar", Color.FromArgb(239, 68, 68), Color.White, BtnEliminar_Click);
            btnEditar = CrearBoton("✏️ Editar", Color.FromArgb(2, 132, 199), Color.White, BtnEditar_Click);
            btnNuevo = CrearBoton("➕ Nuevo Producto", Color.FromArgb(16, 185, 129), Color.White, BtnNuevo_Click);

            pnlRight.Controls.AddRange(new Control[] { btnEliminar, btnEditar, btnNuevo });

            pnlTop.Controls.Add(pnlRight);
            pnlTop.Controls.Add(pnlLeft);

            // Grilla de Productos
            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0, 10, 0, 0)
            };
            pnlGridCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlGridCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            dgvProductos = new DataGridView
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
            ConfigurarEstiloTabla(dgvProductos);
            dgvProductos.DoubleClick += (s, e) => BtnEditar_Click(s, e);

            pnlGridCard.Controls.Add(dgvProductos);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlTop);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Button CrearBoton(string texto, Color back, Color fore, EventHandler onClick)
        {
            Button b = new Button
            {
                Text = texto,
                Height = 32,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                AutoSize = true,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(4, 2, 4, 2)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;

            // Encabezados fijos: mantienen el color oscuro incluso al seleccionar la fila
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;

            // Filas
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 32;
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProductos()
        {
            try
            {
                using var db = new AppDbContext();
                _listaProductos = db.Productos.Where(p => p.Estado).OrderBy(p => p.Nombre).ToList();
                ActualizarGrilla(_listaProductos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarGrilla(List<Producto> lista)
        {
            dgvProductos.DataSource = null;
            dgvProductos.DataSource = lista;

            // Ocultar columnas secundarias
            string[] columnasOcultar = new string[] 
            { 
                "NmbLargo", "NmbCorto", "IdFamilia", "Categoria1", "Categoria2", "Categoria3", 
                "StockMinimo", "ImagenPath", "Estado", "Activo" 
            };

            foreach (string col in columnasOcultar)
            {
                if (dgvProductos.Columns[col] != null)
                {
                    dgvProductos.Columns[col].Visible = false;
                }
            }

            // Configurar columnas principales
            if (dgvProductos.Columns["ProductoID"] != null) 
            { 
                dgvProductos.Columns["ProductoID"].Width = 55; 
                dgvProductos.Columns["ProductoID"].HeaderText = "ID";
                dgvProductos.Columns["ProductoID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvProductos.Columns["CodigoBarra"] != null) 
            { 
                dgvProductos.Columns["CodigoBarra"].Width = 125; 
                dgvProductos.Columns["CodigoBarra"].HeaderText = "SKU/Barra"; 
            }

            if (dgvProductos.Columns["Nombre"] != null) 
            { 
                dgvProductos.Columns["Nombre"].HeaderText = "Descripción Producto";
                dgvProductos.Columns["Nombre"].FillWeight = 160;
            }

            if (dgvProductos.Columns["Categoria"] != null) 
            { 
                dgvProductos.Columns["Categoria"].Width = 110;
                dgvProductos.Columns["Categoria"].HeaderText = "Categoría"; 
            }

            if (dgvProductos.Columns["NFamilia"] != null) 
            { 
                dgvProductos.Columns["NFamilia"].Width = 110;
                dgvProductos.Columns["NFamilia"].HeaderText = "Familia"; 
            }

            if (dgvProductos.Columns["PrecioUnitario"] != null) 
            { 
                dgvProductos.Columns["PrecioUnitario"].Width = 120; 
                dgvProductos.Columns["PrecioUnitario"].HeaderText = "Precio Venta (IVA)"; 
                dgvProductos.Columns["PrecioUnitario"].DefaultCellStyle.Format = "$#,##0"; 
                dgvProductos.Columns["PrecioUnitario"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); 
                dgvProductos.Columns["PrecioUnitario"].DefaultCellStyle.ForeColor = Color.FromArgb(16, 185, 129);
                dgvProductos.Columns["PrecioUnitario"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgvProductos.Columns["Stock"] != null) 
            { 
                dgvProductos.Columns["Stock"].Width = 80; 
                dgvProductos.Columns["Stock"].HeaderText = "Stock"; 
                dgvProductos.Columns["Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; 
                dgvProductos.Columns["Stock"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); 
            }

            if (dgvProductos.Columns["PrecioCosto"] != null) 
            { 
                dgvProductos.Columns["PrecioCosto"].Width = 95; 
                dgvProductos.Columns["PrecioCosto"].HeaderText = "Costo Neto"; 
                dgvProductos.Columns["PrecioCosto"].DefaultCellStyle.Format = "$#,##0";
                dgvProductos.Columns["PrecioCosto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgvProductos.Columns["MargenGanancia"] != null) 
            { 
                dgvProductos.Columns["MargenGanancia"].Width = 85; 
                dgvProductos.Columns["MargenGanancia"].HeaderText = "Margen %"; 
                dgvProductos.Columns["MargenGanancia"].DefaultCellStyle.Format = "0.0'%'";
                dgvProductos.Columns["MargenGanancia"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void FiltrarProductosInteligente()
        {
            string q = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                ActualizarGrilla(_listaProductos);
                return;
            }

            // Búsqueda inteligente restringida únicamente a:
            // 1. Cada palabra de la descripción del producto (prefijo / StartsWith)
            // 2. Código de barra / SKU
            var filtrados = _listaProductos.Where(p =>
                (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(palabra => palabra.StartsWith(q, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(p.CodigoBarra) && p.CodigoBarra.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            ActualizarGrilla(filtrados);
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            using (var modal = new FormNuevoProductoModal())
            {
                if (modal.ShowDialog(this) == DialogResult.OK && modal.ProductoResultado != null)
                {
                    try
                    {
                        using var db = new AppDbContext();
                        if (modal.ProductoResultado.ProductoID == 0)
                        {
                            modal.ProductoResultado.Stock = modal.CantidadFactura;
                            db.Productos.Add(modal.ProductoResultado);
                            db.SaveChanges();
                        }
                        
                        CargarProductos();
                        MessageBox.Show("Producto registrado exitosamente en el catálogo.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar en catálogo: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            if (dgvProductos.CurrentRow?.DataBoundItem is not Producto prod)
            {
                MessageBox.Show("Seleccione un producto para editar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var modal = new FormNuevoProductoModal(prod))
            {
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    CargarProductos();
                }
            }
        }

        private void BtnEliminar_Click(object? sender, EventArgs e)
        {
            if (dgvProductos.CurrentRow?.DataBoundItem is not Producto prod)
            {
                MessageBox.Show("Seleccione un producto para eliminar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"¿Está seguro de eliminar el producto '{prod.Nombre}'?\n\nEsta acción lo dará de baja del catálogo y del punto de venta.", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using var db = new AppDbContext();
                    var p = db.Productos.Find(prod.ProductoID);
                    if (p != null)
                    {
                        p.Estado = false;
                        db.SaveChanges();
                        MessageBox.Show("Producto eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        CargarProductos();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}