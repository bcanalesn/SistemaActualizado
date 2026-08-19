using System;
using System.Collections.Generic;
using System.Drawing;
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
        private Button btnMargenes = null!;
        private Button btnColumnasVisibles = null!;
        private ContextMenuStrip menuListasVisibles = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEliminar = null!;
        private List<Producto> _listaProductos = new List<Producto>();

        // Configuración de visibilidad de las listas 2 a 10 (Por defecto activas 2 a 5)
        private bool[] _listasVisibles = new bool[] { true, true, true, true, false, false, false, false, false };

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

            // Barra Superior de Controles
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(12, 12, 12, 12),
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
                Width = 460,
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
                Size = new Size(200, 28), 
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 3, 6, 0)
            };
            txtBuscar.TextChanged += (s, e) => FiltrarProductosInteligente();

            btnBuscar = CrearBoton("Buscar", Color.FromArgb(30, 41, 59), Color.White, (s, e) => FiltrarProductosInteligente());
            btnRecargar = CrearBoton("🔄 Recargar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), (s, e) => CargarProductos());

            pnlLeft.Controls.AddRange(new Control[] { lblBuscar, txtBuscar, btnBuscar, btnRecargar });

            // Sección Derecha: Acciones y Configuración de Listas
            FlowLayoutPanel pnlRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 650,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnEliminar = CrearBoton("🗑️ Eliminar", Color.FromArgb(239, 68, 68), Color.White, BtnEliminar_Click);
            btnEditar = CrearBoton("✏️ Editar", Color.FromArgb(2, 132, 199), Color.White, BtnEditar_Click);
            btnNuevo = CrearBoton("➕ Nuevo Producto", Color.FromArgb(16, 185, 129), Color.White, BtnNuevo_Click);
            btnMargenes = CrearBoton("⚙️ Márgenes %", Color.FromArgb(245, 158, 11), Color.White, BtnMargenes_Click);

            // Menú desplegable para activar/desactivar qué listas ver
            CrearMenuListasVisibles();
            btnColumnasVisibles = CrearBoton("👁️ Listas Visibles ▼", Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59), (s, e) =>
            {
                menuListasVisibles.Show(btnColumnasVisibles, new Point(0, btnColumnasVisibles.Height));
            });

            pnlRight.Controls.AddRange(new Control[] { btnEliminar, btnEditar, btnNuevo, btnMargenes, btnColumnasVisibles });

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
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };
            ConfigurarEstiloTabla(dgvProductos);
            dgvProductos.DoubleClick += (s, e) => BtnEditar_Click(s, e);

            pnlGridCard.Controls.Add(dgvProductos);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlTop);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void CrearMenuListasVisibles()
        {
            menuListasVisibles = new ContextMenuStrip();

            var itemL1 = new ToolStripMenuItem("Lista 1 (Precio Venta) [Fijo]") { Checked = true, Enabled = false };
            menuListasVisibles.Items.Add(itemL1);

            for (int i = 2; i <= 10; i++)
            {
                int index = i - 2;
                var item = new ToolStripMenuItem($"Lista {i}")
                {
                    Checked = _listasVisibles[index],
                    CheckOnClick = true
                };

                item.CheckedChanged += (s, e) =>
                {
                    _listasVisibles[index] = item.Checked;
                    ActualizarGrilla(_listaProductos);
                };

                menuListasVisibles.Items.Add(item);
            }
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
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                AutoSize = true,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(3, 2, 3, 2)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;

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

            foreach (DataGridViewColumn col in dgvProductos.Columns)
            {
                col.Visible = false;
            }

            void ConfigurarCol(string prop, string titulo, int dispIndex, int ancho, 
                              DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft, 
                              string formato = "", Color? fore = null, bool bold = false, bool visible = true)
            {
                if (dgvProductos.Columns[prop] is DataGridViewColumn c)
                {
                    c.Visible = visible;
                    c.HeaderText = titulo;
                    c.DisplayIndex = dispIndex;
                    c.Width = ancho;
                    c.DefaultCellStyle.Alignment = align;

                    if (!string.IsNullOrEmpty(formato)) c.DefaultCellStyle.Format = formato;
                    if (fore.HasValue) c.DefaultCellStyle.ForeColor = fore.Value;
                    if (bold) c.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }

            int orden = 0;

            // 1° Descripción Producto
            ConfigurarCol("Nombre", "Descripción Producto", orden++, 220);

            // 2° Categoría
            ConfigurarCol("Categoria", "Categoría", orden++, 110);

            // 3° Familia
            ConfigurarCol("NFamilia", "Familia", orden++, 110);

            // 4° Stock
            ConfigurarCol("Stock", "Stock", orden++, 75, DataGridViewContentAlignment.MiddleRight, "#,##0", null, true);

            // 5° Costo Neto
            ConfigurarCol("PrecioCosto", "Costo Neto", orden++, 95, DataGridViewContentAlignment.MiddleRight, "$#,##0");

            // 6° Precio Venta (Lista 1)
            ConfigurarCol("PrecioUnitario", "Precio Venta (L1)", orden++, 125, DataGridViewContentAlignment.MiddleRight, "$#,##0", Color.FromArgb(16, 185, 129), true);

            // Listas 2 a 10 (Dinámicas según el menú de selección)
            for (int i = 2; i <= 10; i++)
            {
                string propName = $"Precio{i}";
                bool estaVisible = _listasVisibles[i - 2];
                ConfigurarCol(propName, $"Lista {i}", orden++, 105, DataGridViewContentAlignment.MiddleRight, "$#,##0", Color.FromArgb(2, 132, 199), false, estaVisible);
            }
            // Justo antes del SKU:
            ConfigurarCol("ListaDefectoPOS", "Lista Activa POS", orden++, 100, DataGridViewContentAlignment.MiddleCenter, "Lista #0", Color.FromArgb(147, 51, 234), true);

            // SKU / Código Barra al final
            ConfigurarCol("CodigoBarra", "SKU / Código Barra", orden++, 130);
        }

        private void FiltrarProductosInteligente()
        {
            string q = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                ActualizarGrilla(_listaProductos);
                return;
            }

            var filtrados = _listaProductos.Where(p =>
                (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(palabra => palabra.StartsWith(q, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(p.CodigoBarra) && p.CodigoBarra.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            ActualizarGrilla(filtrados);
        }

        private void BtnMargenes_Click(object? sender, EventArgs e)
        {
            var categorias = _listaProductos.Select(p => p.Categoria).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();

            using (var modal = new FormConfigurarMargenesModal(categorias))
            {
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    CargarProductos();
                }
            }
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