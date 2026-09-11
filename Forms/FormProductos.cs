using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormProductos : Form
    {
        private readonly ProductoService _productoService = new ProductoService();

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
        private Label lblContadorFooter = null!;
        private List<Producto> _listaProductos = new List<Producto>();

        private bool[] _listasVisibles = new bool[] { true, true, true, true, false, false, false, false, false };

        public FormProductos()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            this.Shown += (s, e) => CargarProductos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            // =========================================================================
            // 1. ENCABEZADO Y ACCIONES PRINCIPALES
            // =========================================================================
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 60, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 0, 0, 10) 
            };

            Panel pnlTitulos = new Panel
            {
                Dock = DockStyle.Left,
                Width = 430,
                BackColor = Color.Transparent
            };

            Label lblTitulo = new Label
            {
                Text = "📦 Catálogo de Productos",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Label lblSubtitulo = new Label
            {
                Text = "Gestión de inventario, stock, listas de precios y márgenes comerciales",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlTitulos.Controls.Add(lblSubtitulo);
            pnlTitulos.Controls.Add(lblTitulo);

            FlowLayoutPanel pnlBotonesAccion = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };

            btnNuevo = CrearBoton("➕ Nuevo Producto", Color.FromArgb(16, 185, 129), Color.White, new Size(150, 34), 6);
            btnNuevo.Margin = new Padding(4, 0, 0, 0);
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = CrearBoton("✏️ Editar", Color.FromArgb(2, 132, 199), Color.White, new Size(95, 34), 6);
            btnEditar.Margin = new Padding(4, 0, 0, 0);
            btnEditar.Click += BtnEditar_Click;

            btnMargenes = CrearBoton("⚙️ Márgenes %", Color.FromArgb(245, 158, 11), Color.White, new Size(130, 34), 6);
            btnMargenes.Margin = new Padding(4, 0, 0, 0);
            btnMargenes.Click += BtnMargenes_Click;

            btnEliminar = CrearBoton("🗑️ Eliminar", Color.FromArgb(239, 68, 68), Color.White, new Size(100, 34), 6);
            btnEliminar.Margin = new Padding(4, 0, 0, 0);
            btnEliminar.Click += BtnEliminar_Click;

            CrearMenuListasVisibles();
            btnColumnasVisibles = CrearBoton("👁️ Listas Visibles ▼", Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59), new Size(150, 34), 6);
            btnColumnasVisibles.Margin = new Padding(4, 0, 0, 0);
            btnColumnasVisibles.Click += (s, e) =>
            {
                menuListasVisibles.Show(btnColumnasVisibles, new Point(0, btnColumnasVisibles.Height));
            };

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnMargenes, btnColumnasVisibles, btnEliminar });
            pnlHeader.Controls.Add(pnlBotonesAccion);
            pnlHeader.Controls.Add(pnlTitulos);

            // =========================================================================
            // 2. BARRA DE FILTROS Y BÚSQUEDA
            // =========================================================================
            Panel pnlFiltrosWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 72, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 12, 16, 12);

            FlowLayoutPanel flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            txtBuscar = new TextBox
            {
                Size = new Size(340, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "Buscar por nombre, SKU o código de barra..."
            };
            txtBuscar.TextChanged += (s, e) => FiltrarProductosInteligente();
            Panel pnlGrpBuscar = CrearGrupoConCaja("BUSCAR PRODUCTO", txtBuscar, 355);

            btnBuscar = CrearBoton("🔍 Buscar", Color.FromArgb(30, 41, 59), Color.White, new Size(95, 32), 6);
            btnBuscar.Margin = new Padding(8, 14, 4, 0);
            btnBuscar.Click += (s, e) => FiltrarProductosInteligente();

            btnRecargar = CrearBoton("🔄 Recargar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(105, 32), 6);
            btnRecargar.Margin = new Padding(4, 14, 0, 0);
            btnRecargar.Click += (s, e) => CargarProductos();

            flpFiltros.Controls.AddRange(new Control[] { pnlGrpBuscar, btnBuscar, btnRecargar });
            pnlFiltrosCard.Controls.Add(flpFiltros);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // =========================================================================
            // 3. TARJETA DE GRILLA PRINCIPAL REDONDEADA
            // =========================================================================
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

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
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowTemplate = { Height = 34 }
            };
            ConfigurarEstiloTabla(dgvProductos);
            dgvProductos.DoubleClick += (s, e) => BtnEditar_Click(s, e);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 productos registrados",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlGridCard.Controls.Add(dgvProductos);
            pnlGridCard.Controls.Add(pnlFooter);

            // Orden Z estricto
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlFiltrosWrapper);
            pnlMain.Controls.Add(pnlHeader);

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

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProductos()
        {
            try
            {
                _listaProductos = _productoService.ObtenerProductosActivos();
                ActualizarGrilla(_listaProductos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    if (bold) c.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            }

            int orden = 0;
            ConfigurarCol("Nombre", "Descripción Producto", orden++, 240);
            ConfigurarCol("Categoria", "Categoría", orden++, 120);
            ConfigurarCol("NFamilia", "Familia", orden++, 120);
            ConfigurarCol("Stock", "Stock", orden++, 80, DataGridViewContentAlignment.MiddleRight, "#,##0", null, true);
            ConfigurarCol("PrecioCosto", "Costo Neto", orden++, 105, DataGridViewContentAlignment.MiddleRight, "$#,##0");
            ConfigurarCol("PrecioUnitario", "Precio Venta (L1)", orden++, 130, DataGridViewContentAlignment.MiddleRight, "$#,##0", Color.FromArgb(16, 185, 129), true);

            for (int i = 2; i <= 10; i++)
            {
                string propName = $"Precio{i}";
                bool estaVisible = _listasVisibles[i - 2];
                ConfigurarCol(propName, $"Lista {i}", orden++, 110, DataGridViewContentAlignment.MiddleRight, "$#,##0", Color.FromArgb(2, 132, 199), false, estaVisible);
            }

            ConfigurarCol("ListaDefectoPOS", "Lista Activa POS", orden++, 110, DataGridViewContentAlignment.MiddleCenter, "Lista #0", Color.FromArgb(124, 58, 237), true);
            ConfigurarCol("CodigoBarra", "SKU / Código Barra", orden++, 140);

            lblContadorFooter.Text = $"Mostrando {lista.Count:N0} de {_listaProductos.Count:N0} productos registrados";

            if (dgvProductos.Rows.Count > 0)
            {
                var primeraColumnaVisible = dgvProductos.Columns.Cast<DataGridViewColumn>()
                    .OrderBy(c => c.DisplayIndex)
                    .FirstOrDefault(c => c.Visible);

                if (primeraColumnaVisible != null)
                {
                    dgvProductos.ClearSelection();
                    dgvProductos.CurrentCell = dgvProductos.Rows[0].Cells[primeraColumnaVisible.Index];
                    dgvProductos.Rows[0].Selected = true;
                }
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
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    CargarProductos();
                }
            }
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            Producto? prod = null;

            if (dgvProductos.CurrentRow?.DataBoundItem is Producto pCurrent)
            {
                prod = pCurrent;
            }
            else if (dgvProductos.SelectedRows.Count > 0 && dgvProductos.SelectedRows[0].DataBoundItem is Producto pSelected)
            {
                prod = pSelected;
            }

            if (prod == null)
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
                    _productoService.EliminarProducto(prod.ProductoID);
                    MessageBox.Show("Producto eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarProductos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =========================================================================
        // MÉTODOS AUXILIARES DE DISEÑO MODERNO Y BORDES REDONDEADOS
        // =========================================================================
        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Resize += (s, e) => pnl.Invalidate();
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.BackColor);
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 10);
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

        private Panel CrearGrupoConCaja(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            Panel pnlInputBox = new Panel { Location = new Point(0, 18), Size = new Size(ancho, 28), BackColor = Color.FromArgb(248, 250, 252) };
            pnlInputBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlInputBox.Width - 1, pnlInputBox.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(r, 6);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            control.Location = new Point(6, 3);
            control.Width = ancho - 12;
            pnlInputBox.Controls.Add(control);

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(pnlInputBox);
            return pnl;
        }

        private Button CrearBoton(string texto, Color back, Color fore, Size size, int radio = 0)
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
    }
}