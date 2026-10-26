using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormEditorCatalogoPOSModal : Form
    {
        private readonly ConfiguracionService _configService = new ConfiguracionService();

        private RadioButton rbOrdenCategoria = null!;
        private RadioButton rbOrdenPersonalizado = null!;
        private RadioButton rbOrdenMasVendidos = null!;
        private FlowLayoutPanel flowConfigCategorias = null!;
        private FlowLayoutPanel flowConfigFamilias = null!;
        private FlowLayoutPanel flowLienzoProductos = null!;
        private FlowLayoutPanel flowAlmacenProductos = null!;
        private TextBox txtBuscarAlmacen = null!;
        private RadioButton rbFiltroSinAsignar = null!;
        private RadioButton rbFiltroTodos = null!;

        private string _catEdicionActiva = "";
        private string _famEdicionActiva = "";
        private List<PosCategoria> _categoriasCache = new List<PosCategoria>();

        public FormEditorCatalogoPOSModal()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarDatosEspejoConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "Editor Visual de Catálogo POS - Pantalla Completa";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None; // Modo inmersivo: tapa menús laterales
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F);

            // 1. HEADER SUPERIOR CON BOTÓN DE CERRAR
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 8, 16, 8)
            };

            Label lblTitulo = new Label
            {
                Text = "🎨 PERSONALIZADOR VISUAL DE CATÁLOGO POS",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 14)
            };

            Button btnCerrar = new Button
            {
                Text = "✕ Cerrar y Volver",
                Dock = DockStyle.Right,
                Width = 140,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(btnCerrar);

            // 2. LAYOUT PRINCIPAL (75% LIENZO - 25% ALMACÉN)
            TableLayoutPanel tlpLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(244, 246, 249)
            };
            tlpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76F));
            tlpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));

            // PANEL IZQUIERDO: LIENZO ESPEJO
            TableLayoutPanel tlpLienzoWrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpLienzoWrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F)); // Radios
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Categorías
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Familias
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Productos Grid

            // Radios de Criterio
            Panel pnlTopRadios = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, 8, 10, 6), Margin = new Padding(0) };
            string modoActual = _configService.ObtenerModoOrdenCatalogo();

            rbOrdenCategoria = new RadioButton { Text = "📂 Alfabético", Location = new Point(10, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Checked = (modoActual == "CATEGORIA") };
            rbOrdenPersonalizado = new RadioButton { Text = "⭐ Personalizado (Arrastrar)", Location = new Point(140, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Checked = (modoActual == "PERSONALIZADO") };
            rbOrdenMasVendidos = new RadioButton { Text = "🔥 Los Más Vendidos", Location = new Point(350, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Checked = (modoActual == "MAS_VENDIDOS") };

            Action alternarModo = () =>
            {
                string m = "CATEGORIA";
                if (rbOrdenPersonalizado.Checked) m = "PERSONALIZADO";
                else if (rbOrdenMasVendidos.Checked) m = "MAS_VENDIDOS";
                _configService.GuardarModoOrdenCatalogo(m);

                RenderizarCategoriasConfig();
                RenderizarFamiliasConfig();
                RefrescarLienzoYAlmacen();
            };

            rbOrdenCategoria.Click += (s, e) => alternarModo();
            rbOrdenPersonalizado.Click += (s, e) => alternarModo();
            rbOrdenMasVendidos.Click += (s, e) => alternarModo();

            pnlTopRadios.Controls.AddRange(new Control[] { rbOrdenCategoria, rbOrdenPersonalizado, rbOrdenMasVendidos });
            tlpLienzoWrapper.Controls.Add(pnlTopRadios, 0, 0);

            // Flow Categorías (Crece según las filas que necesite)
            flowConfigCategorias = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoScroll = false,
                WrapContents = true,
                Margin = new Padding(0, 6, 0, 6),
                Padding = new Padding(0)
            };
            tlpLienzoWrapper.Controls.Add(flowConfigCategorias, 0, 1);

            // Flow Familias (Crece según las filas que necesite)
            flowConfigFamilias = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoScroll = false,
                WrapContents = true,
                Margin = new Padding(0, 2, 0, 6),
                Padding = new Padding(0)
            };
            tlpLienzoWrapper.Controls.Add(flowConfigFamilias, 0, 2);

            // Flow Productos Lienzo
            flowLienzoProductos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0),
                AllowDrop = true
            };
            flowLienzoProductos.DragEnter += (s, e) => { if (e.Data!.GetDataPresent(typeof(Producto))) e.Effect = DragDropEffects.Move; };
            flowLienzoProductos.DragDrop += FlowLienzoProductos_DragDrop;
            tlpLienzoWrapper.Controls.Add(flowLienzoProductos, 0, 3);

            // PANEL DERECHO: ALMACÉN
            GroupBox gbAlmacen = new GroupBox
            {
                Text = "📦 Almacén de Productos (Arrastrar al Lienzo)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(8)
            };

            txtBuscarAlmacen = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "🔍 Buscar en almacén..." };
            txtBuscarAlmacen.TextChanged += (s, e) => FiltrarAlmacen();

            Panel pnlFiltrosAlmacen = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(0, 4, 0, 0) };
            rbFiltroSinAsignar = new RadioButton { Text = "Sin Asignar", Location = new Point(4, 4), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8F) };
            rbFiltroTodos = new RadioButton { Text = "Todo el Catálogo", Location = new Point(120, 4), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            rbFiltroSinAsignar.CheckedChanged += (s, e) => FiltrarAlmacen();
            rbFiltroTodos.CheckedChanged += (s, e) => FiltrarAlmacen();
            pnlFiltrosAlmacen.Controls.AddRange(new Control[] { rbFiltroSinAsignar, rbFiltroTodos });

            flowAlmacenProductos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            gbAlmacen.Controls.Add(flowAlmacenProductos);
            gbAlmacen.Controls.Add(pnlFiltrosAlmacen);
            gbAlmacen.Controls.Add(txtBuscarAlmacen);

            tlpLayout.Controls.Add(tlpLienzoWrapper, 0, 0);
            tlpLayout.Controls.Add(gbAlmacen, 1, 0);

            this.Controls.Add(tlpLayout);
            this.Controls.Add(pnlHeader);
        }

        private void CargarDatosEspejoConfig()
        {
            _categoriasCache = _configService.ObtenerCategoriasPOS();
            if (_categoriasCache.Count > 0 && string.IsNullOrEmpty(_catEdicionActiva))
            {
                _catEdicionActiva = _categoriasCache[0].Nombre;
            }
            RenderizarCategoriasConfig();
            RenderizarFamiliasConfig();
            RefrescarLienzoYAlmacen();
        }

        private void RenderizarCategoriasConfig()
        {
            flowConfigCategorias.SuspendLayout();
            flowConfigCategorias.Controls.Clear();
            bool esPersonalizado = rbOrdenPersonalizado != null && rbOrdenPersonalizado.Checked;

            for (int i = 0; i < _categoriasCache.Count; i++)
            {
                var cat = _categoriasCache[i];
                bool estaSeleccionada = cat.Nombre.Equals(_catEdicionActiva, StringComparison.OrdinalIgnoreCase);

                Color back = ColorTranslator.FromHtml(cat.FondoHex);
                Color fore = ColorTranslator.FromHtml(cat.TextoHex);
                Color bordeFuerte = ColorTranslator.FromHtml(cat.BordeHex);

                if (esPersonalizado)
                {
                    Panel pnlCat = new Panel
                    {
                        Size = new Size(118, 38),
                        Margin = new Padding(0, 0, 6, 6),
                        BackColor = back,
                        AllowDrop = true,
                        Tag = cat
                    };

                    pnlCat.Paint += (s, e) =>
                    {
                        Rectangle r = new Rectangle(0, 0, pnlCat.Width - 1, pnlCat.Height - 1);
                        using Pen pen = new Pen(estaSeleccionada ? bordeFuerte : Color.FromArgb(226, 232, 240), estaSeleccionada ? 2.5f : 1f);
                        e.Graphics.DrawRectangle(pen, r);
                    };

                    Button btnSelect = new Button
                    {
                        Text = cat.Nombre,
                        Location = new Point(0, 0),
                        Size = new Size(92, 38),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = fore,
                        Font = new Font("Segoe UI", estaSeleccionada ? 8.5F : 8F, estaSeleccionada ? FontStyle.Bold : FontStyle.Regular),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0)
                    };
                    btnSelect.FlatAppearance.BorderSize = 0;
                    btnSelect.Click += (s, e) =>
                    {
                        _catEdicionActiva = cat.Nombre;
                        _famEdicionActiva = "";
                        RenderizarCategoriasConfig();
                        RenderizarFamiliasConfig();
                        RefrescarLienzoYAlmacen();
                    };

                    Point dragStart = Point.Empty;
                    btnSelect.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
                    btnSelect.MouseMove += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left && dragStart != Point.Empty)
                        {
                            if (Math.Abs(e.X - dragStart.X) > 4 || Math.Abs(e.Y - dragStart.Y) > 4)
                            {
                                dragStart = Point.Empty;
                                pnlCat.DoDragDrop(cat, DragDropEffects.Move);
                            }
                        }
                    };

                    Button btnEdit = new Button
                    {
                        Text = "✏️",
                        Location = new Point(92, 0),
                        Size = new Size(26, 38),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = fore,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0)
                    };
                    btnEdit.FlatAppearance.BorderSize = 0;
                    btnEdit.Click += (s, e) => PromptEditarCategoria(cat);

                    pnlCat.Controls.Add(btnSelect);
                    pnlCat.Controls.Add(btnEdit);

                    void ConfigurarDrop(Control c)
                    {
                        c.AllowDrop = true;
                        c.DragEnter += (s, e) => { if (e.Data!.GetDataPresent(typeof(PosCategoria))) e.Effect = DragDropEffects.Move; };
                        c.DragDrop += (s, e) =>
                        {
                            if (e.Data!.GetDataPresent(typeof(PosCategoria)))
                            {
                                var fuente = (PosCategoria)e.Data.GetData(typeof(PosCategoria))!;
                                if (fuente.Id != cat.Id)
                                {
                                    int idxOrigen = _categoriasCache.FindIndex(x => x.Id == fuente.Id);
                                    int idxDestino = _categoriasCache.FindIndex(x => x.Id == cat.Id);
                                    if (idxOrigen >= 0 && idxDestino >= 0)
                                    {
                                        _categoriasCache.RemoveAt(idxOrigen);
                                        _categoriasCache.Insert(idxDestino, fuente);
                                        _configService.GuardarOrdenCategorias(_categoriasCache.Select(x => x.Id).ToList());
                                        RenderizarCategoriasConfig();
                                    }
                                }
                            }
                        };
                    }

                    ConfigurarDrop(pnlCat);
                    ConfigurarDrop(btnSelect);
                    flowConfigCategorias.Controls.Add(pnlCat);
                }
                else
                {
                    Button btnCat = new Button
                    {
                        Text = cat.Nombre,
                        Size = new Size(118, 38),
                        Margin = new Padding(0, 0, 6, 6),
                        BackColor = back,
                        ForeColor = fore,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", estaSeleccionada ? 8.5F : 8F, estaSeleccionada ? FontStyle.Bold : FontStyle.Regular),
                        Cursor = Cursors.Hand
                    };
                    btnCat.FlatAppearance.BorderSize = estaSeleccionada ? 2 : 1;
                    btnCat.FlatAppearance.BorderColor = estaSeleccionada ? bordeFuerte : Color.FromArgb(226, 232, 240);
                    btnCat.Click += (s, e) =>
                    {
                        _catEdicionActiva = cat.Nombre;
                        _famEdicionActiva = "";
                        RenderizarCategoriasConfig();
                        RenderizarFamiliasConfig();
                        RefrescarLienzoYAlmacen();
                    };
                    flowConfigCategorias.Controls.Add(btnCat);
                }
            }

            if (esPersonalizado)
            {
                Button btnNueva = new Button
                {
                    Text = "➕ Nueva Cat",
                    Size = new Size(110, 38),
                    Margin = new Padding(0, 0, 6, 6),
                    BackColor = Color.FromArgb(16, 185, 129),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnNueva.FlatAppearance.BorderSize = 0;
                btnNueva.Click += (s, e) => PromptNuevaCategoria();
                flowConfigCategorias.Controls.Add(btnNueva);
            }

            flowConfigCategorias.ResumeLayout();
        }

        private void RenderizarFamiliasConfig()
        {
            flowConfigFamilias.SuspendLayout();
            flowConfigFamilias.Controls.Clear();
            var cat = _categoriasCache.FirstOrDefault(c => c.Nombre.Equals(_catEdicionActiva, StringComparison.OrdinalIgnoreCase));
            if (cat == null)
            {
                flowConfigFamilias.ResumeLayout();
                return;
            }

            bool esPersonalizado = rbOrdenPersonalizado != null && rbOrdenPersonalizado.Checked;
            var familias = cat.Familias.Where(f => f.Activo).OrderBy(f => f.Orden).ToList();

            // 1. Botón "Todas"
            bool todasSeleccionada = string.IsNullOrEmpty(_famEdicionActiva);
            Button btnTodas = new Button
            {
                Text = "Todas",
                Height = 28,
                AutoSize = true,
                BackColor = todasSeleccionada ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249),
                ForeColor = todasSeleccionada ? Color.White : Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 4),
                Padding = new Padding(6, 0, 6, 0)
            };
            btnTodas.FlatAppearance.BorderSize = 0;
            btnTodas.Click += (s, e) =>
            {
                _famEdicionActiva = "";
                RenderizarFamiliasConfig();
                RefrescarLienzoYAlmacen();
            };
            flowConfigFamilias.Controls.Add(btnTodas);

            // 2. Subfamilias compactas
            Font fontFam = new Font("Segoe UI", 8F, FontStyle.Bold);

            for (int i = 0; i < familias.Count; i++)
            {
                var fam = familias[i];
                bool estaSeleccionada = fam.Nombre.Equals(_famEdicionActiva, StringComparison.OrdinalIgnoreCase);

                Color backColor = estaSeleccionada ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249);
                Color foreColor = estaSeleccionada ? Color.White : Color.FromArgb(51, 65, 85);
                Color hoverColor = estaSeleccionada ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);

                if (esPersonalizado)
                {
                    int anchoTexto = TextRenderer.MeasureText(fam.Nombre, fontFam).Width + 14;
                    int anchoTotal = anchoTexto + 24;

                    Panel pnlFam = new Panel
                    {
                        Size = new Size(anchoTotal, 28),
                        Margin = new Padding(0, 0, 6, 4),
                        BackColor = backColor,
                        AllowDrop = true,
                        Tag = fam
                    };

                    Button btnSelectFam = new Button
                    {
                        Text = fam.Nombre,
                        Location = new Point(0, 0),
                        Size = new Size(anchoTexto, 28),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = foreColor,
                        Font = fontFam,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0)
                    };
                    btnSelectFam.FlatAppearance.BorderSize = 0;
                    btnSelectFam.FlatAppearance.MouseOverBackColor = hoverColor;

                    btnSelectFam.Click += (s, e) =>
                    {
                        _famEdicionActiva = fam.Nombre;
                        RenderizarFamiliasConfig();
                        RefrescarLienzoYAlmacen();
                    };

                    Point famDragStart = Point.Empty;
                    btnSelectFam.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) famDragStart = e.Location; };
                    btnSelectFam.MouseMove += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left && famDragStart != Point.Empty)
                        {
                            if (Math.Abs(e.X - famDragStart.X) > 4 || Math.Abs(e.Y - famDragStart.Y) > 4)
                            {
                                famDragStart = Point.Empty;
                                pnlFam.DoDragDrop(fam, DragDropEffects.Move);
                            }
                        }
                    };

                    Button btnEditFam = new Button
                    {
                        Text = "✏️",
                        Location = new Point(anchoTexto, 0),
                        Size = new Size(24, 28),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = foreColor,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0)
                    };
                    btnEditFam.FlatAppearance.BorderSize = 0;
                    btnEditFam.FlatAppearance.MouseOverBackColor = hoverColor;
                    btnEditFam.Click += (s, e) => PromptRenombrarFamilia(fam);

                    pnlFam.Controls.Add(btnSelectFam);
                    pnlFam.Controls.Add(btnEditFam);

                    void ConfigurarDropFam(Control c)
                    {
                        c.AllowDrop = true;
                        c.DragEnter += (s, e) => { if (e.Data!.GetDataPresent(typeof(PosFamilia))) e.Effect = DragDropEffects.Move; };
                        c.DragDrop += (s, e) =>
                        {
                            if (e.Data!.GetDataPresent(typeof(PosFamilia)))
                            {
                                var fuente = (PosFamilia)e.Data.GetData(typeof(PosFamilia))!;
                                if (fuente.Id != fam.Id)
                                {
                                    int idxO = familias.FindIndex(x => x.Id == fuente.Id);
                                    int idxD = familias.FindIndex(x => x.Id == fam.Id);
                                    if (idxO >= 0 && idxD >= 0)
                                    {
                                        familias.RemoveAt(idxO);
                                        familias.Insert(idxD, fuente);

                                        // 👈 1. Actualizar el correlativo numérico (.Orden) en memoria
                                        for (int k = 0; k < familias.Count; k++)
                                        {
                                            familias[k].Orden = k + 1;
                                        }

                                        // 👈 2. Guardar en Base de Datos de inmediato[cite: 27]
                                        _configService.GuardarOrdenFamilias(familias.Select(x => x.Id).ToList());
                                        cat.Familias = familias;

                                        // 👈 3. Re-renderizar la barra de familias en el instante
                                        RenderizarFamiliasConfig();
                                    }
                                }
                            }
                        };
                    }

                    ConfigurarDropFam(pnlFam);
                    ConfigurarDropFam(btnSelectFam);

                    flowConfigFamilias.Controls.Add(pnlFam);
                }
                else
                {
                    Button btnFam = new Button
                    {
                        Text = fam.Nombre,
                        Height = 28,
                        AutoSize = true,
                        BackColor = backColor,
                        ForeColor = foreColor,
                        FlatStyle = FlatStyle.Flat,
                        Font = fontFam,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0, 0, 6, 4),
                        Padding = new Padding(6, 0, 6, 0)
                    };
                    btnFam.FlatAppearance.BorderSize = 0;
                    btnFam.FlatAppearance.MouseOverBackColor = hoverColor;
                    btnFam.Click += (s, e) =>
                    {
                        _famEdicionActiva = fam.Nombre;
                        RenderizarFamiliasConfig();
                        RefrescarLienzoYAlmacen();
                    };
                    flowConfigFamilias.Controls.Add(btnFam);
                }
            }

            // 3. Botón ➕ Familia
            if (esPersonalizado)
            {
                Button btnNuevaFam = new Button
                {
                    Text = "➕ Familia",
                    Height = 28,
                    AutoSize = true,
                    BackColor = Color.FromArgb(16, 185, 129),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 6, 4),
                    Padding = new Padding(6, 0, 6, 0)
                };
                btnNuevaFam.FlatAppearance.BorderSize = 0;
                btnNuevaFam.Click += (s, e) => PromptNuevaFamilia(cat);
                flowConfigFamilias.Controls.Add(btnNuevaFam);
            }

            flowConfigFamilias.ResumeLayout();
        }

        private void RefrescarLienzoYAlmacen()
        {
            if (flowLienzoProductos == null) return;

            bool esPersonalizado = rbOrdenPersonalizado != null && rbOrdenPersonalizado.Checked;

            flowLienzoProductos.AllowDrop = esPersonalizado;
            if (flowAlmacenProductos != null) flowAlmacenProductos.Enabled = esPersonalizado;
            if (txtBuscarAlmacen != null) txtBuscarAlmacen.Enabled = esPersonalizado;
            if (rbFiltroSinAsignar != null) rbFiltroSinAsignar.Enabled = esPersonalizado;
            if (rbFiltroTodos != null) rbFiltroTodos.Enabled = esPersonalizado;

            using var db = new AppDbContext();
            var todosProductos = db.Productos.Where(p => p.Estado).ToList();

            var productosFiltrados = todosProductos
                .Where(p => p.Categoria.Equals(_catEdicionActiva, StringComparison.OrdinalIgnoreCase) &&
                           (string.IsNullOrEmpty(_famEdicionActiva) || p.NFamilia == _famEdicionActiva))
                .ToList();

            string modoActual = "CATEGORIA";
            if (rbOrdenPersonalizado != null && rbOrdenPersonalizado.Checked) modoActual = "PERSONALIZADO";
            else if (rbOrdenMasVendidos != null && rbOrdenMasVendidos.Checked) modoActual = "MAS_VENDIDOS";

            var productosEnLienzo = _configService.AplicarCriterioOrdenPOS(productosFiltrados, modoActual).ToList();

            flowLienzoProductos.SuspendLayout();
            flowLienzoProductos.Controls.Clear();

            for (int i = 0; i < productosEnLienzo.Count; i++)
            {
                var prod = productosEnLienzo[i];
                int posicionVisible = i + 1;

                Panel card = new Panel
                {
                    Size = new Size(130, 168),
                    BackColor = Color.White,
                    Margin = new Padding(6),
                    Tag = prod,
                    AllowDrop = esPersonalizado,
                    Cursor = esPersonalizado ? Cursors.SizeAll : Cursors.Default
                };
                card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

                Label lblPos = new Label
                {
                    Text = $"#{posicionVisible}",
                    Location = new Point(4, 4),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    BackColor = Color.FromArgb(239, 246, 255)
                };
                card.Controls.Add(lblPos);

                if (esPersonalizado)
                {
                    Button btnQuitar = new Button
                    {
                        Text = "✕",
                        Size = new Size(20, 20),
                        Location = new Point(106, 2),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(239, 68, 68),
                        Cursor = Cursors.Hand,
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                    };
                    btnQuitar.FlatAppearance.BorderSize = 0;
                    btnQuitar.Click += (s, e) =>
                    {
                        _configService.AsignarProductoACategoriaYFamilia(prod.ProductoID, "Sin Asignar", "General", 0);
                        RefrescarLienzoYAlmacen();
                    };
                    card.Controls.Add(btnQuitar);
                }

                Control ctrlImagen;
                Image? imgProd = ImagenHelper.CargarImagenSegura(prod.ImagenPath);

                if (imgProd != null)
                {
                    ctrlImagen = new PictureBoxCalidadAlta
                    {
                        Location = new Point(10, 22),
                        Size = new Size(110, 60),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Transparent,
                        Image = imgProd
                    };
                }
                else
                {
                    ctrlImagen = new Label
                    {
                        Text = prod.EsPesable ? "⚖️" : "📦",
                        Font = new Font("Segoe UI", 18F),
                        Location = new Point(10, 22),
                        Size = new Size(110, 60),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                }
                card.Controls.Add(ctrlImagen);

                Label lblNom = new Label
                {
                    Text = prod.Nombre,
                    Location = new Point(4, 84),
                    Size = new Size(122, 28),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    TextAlign = ContentAlignment.TopCenter,
                    ForeColor = Color.FromArgb(15, 23, 42)
                };
                card.Controls.Add(lblNom);

                string precioStr = prod.EsPesable
                    ? $"{MonedaHelper.Formatear(prod.PrecioUnitario * 1000m, conSigno: true)}/Kg"
                    : MonedaHelper.Formatear(prod.PrecioUnitario, conSigno: true);

                Label lblPrecio = new Label
                {
                    Text = precioStr,
                    Location = new Point(4, 114),
                    Size = new Size(122, 18),
                    ForeColor = Color.FromArgb(0, 102, 255),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                card.Controls.Add(lblPrecio);

                string stockTexto = prod.EsPesable
                    ? (prod.Stock >= 1000 ? $"Stock: {(prod.Stock / 1000.0):0.0} kg" : $"Stock: {prod.Stock} g")
                    : $"Stock: {prod.Stock} un.";

                Label lblStock = new Label
                {
                    Text = stockTexto,
                    Location = new Point(4, 134),
                    Size = new Size(122, 16),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 7.5F),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                card.Controls.Add(lblStock);

                if (esPersonalizado)
                {
                    Point cardDragStart = Point.Empty;
                    void AsignarEventosArrastre(Control c)
                    {
                        if (c is Button) return;

                        c.Cursor = Cursors.SizeAll;
                        c.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) cardDragStart = e.Location; };
                        c.MouseMove += (s, e) =>
                        {
                            if (e.Button == MouseButtons.Left && cardDragStart != Point.Empty)
                            {
                                if (Math.Abs(e.X - cardDragStart.X) > 4 || Math.Abs(e.Y - cardDragStart.Y) > 4)
                                {
                                    cardDragStart = Point.Empty;
                                    card.DoDragDrop(prod, DragDropEffects.Move);
                                }
                            }
                        };

                        c.AllowDrop = true;
                        c.DragEnter += (s, e) => { if (e.Data!.GetDataPresent(typeof(Producto))) e.Effect = DragDropEffects.Move; };
                        c.DragDrop += (s, e) =>
                        {
                            if (e.Data!.GetDataPresent(typeof(Producto)))
                            {
                                var prodSoltado = (Producto)e.Data.GetData(typeof(Producto))!;
                                _configService.ReordenarEInsertarProducto(prodSoltado.ProductoID, _catEdicionActiva, _famEdicionActiva, posicionVisible);
                                RefrescarLienzoYAlmacen();
                            }
                        };
                    }

                    AsignarEventosArrastre(card);
                    foreach (Control child in card.Controls)
                    {
                        AsignarEventosArrastre(child);
                    }
                }

                flowLienzoProductos.Controls.Add(card);
            }

            flowLienzoProductos.ResumeLayout();
            FiltrarAlmacen();
        }

        private void FiltrarAlmacen()
        {
            if (txtBuscarAlmacen == null || flowAlmacenProductos == null) return;

            if (rbOrdenPersonalizado != null && !rbOrdenPersonalizado.Checked)
            {
                flowAlmacenProductos.Controls.Clear();
                return;
            }

            string filtro = txtBuscarAlmacen.Text.Trim().ToLower();
            bool soloSinAsignar = rbFiltroSinAsignar != null && rbFiltroSinAsignar.Checked;

            using var db = new AppDbContext();
            
            // 1. Filtrar productos según la pestaña seleccionada
            var query = db.Productos.Where(p => p.Estado);

            if (soloSinAsignar)
            {
                query = query.Where(p =>
                    string.IsNullOrEmpty(p.Categoria) ||
                    p.Categoria == "Sin Asignar" ||
                    p.OrdenPOS == 0
                );
            }

            var productosBase = query.ToList();

            // 2. Coincidencia estricta: que comience con la palabra buscada
            List<Producto> lista;
            if (!string.IsNullOrEmpty(filtro))
            {
                lista = productosBase.Where(p =>
                    (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Any(palabra => palabra.StartsWith(filtro, StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrEmpty(p.CodigoBarra) && p.CodigoBarra.StartsWith(filtro, StringComparison.OrdinalIgnoreCase))
                )
                .Take(50)
                .ToList();
            }
            else
            {
                lista = productosBase.Take(50).ToList();
            }

            flowAlmacenProductos.SuspendLayout();
            flowAlmacenProductos.Controls.Clear();

            foreach (var p in lista)
            {
                // Tarjeta ligeramente más alta para alojar la miniatura
                Panel item = new Panel
                {
                    Size = new Size(245, 48),
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, 4),
                    Cursor = Cursors.Hand
                };
                item.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, item.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

                // Miniatura de imagen o emoji
                Control ctrlImg;
                Image? imgProd = ImagenHelper.CargarImagenSegura(p.ImagenPath);

                if (imgProd != null)
                {
                    ctrlImg = new PictureBoxCalidadAlta
                    {
                        Location = new Point(5, 5),
                        Size = new Size(38, 38),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.FromArgb(248, 250, 252),
                        Image = imgProd,
                        Cursor = Cursors.Hand
                    };
                }
                else
                {
                    ctrlImg = new Label
                    {
                        Text = p.EsPesable ? "⚖️" : "📦",
                        Font = new Font("Segoe UI", 12F),
                        Location = new Point(5, 5),
                        Size = new Size(38, 38),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.FromArgb(248, 250, 252),
                        Cursor = Cursors.Hand
                    };
                }

                Label lbl = new Label
                {
                    Text = p.Nombre,
                    Location = new Point(48, 5),
                    Size = new Size(165, 18),
                    AutoSize = false,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                };

                string precioStr = p.EsPesable
                    ? $"{MonedaHelper.Formatear(p.PrecioUnitario * 1000m, conSigno: true)}/Kg"
                    : MonedaHelper.Formatear(p.PrecioUnitario, conSigno: true);

                Label lblSub = new Label
                {
                    Text = $"{p.Categoria} • {precioStr}",
                    Location = new Point(48, 25),
                    Size = new Size(165, 16),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 7F)
                };

                Label lblDrag = new Label
                {
                    Text = "⋮⋮",
                    Location = new Point(220, 14),
                    Size = new Size(20, 20),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold)
                };

                // Asignar inicio de Drag & Drop en todos los elementos del ítem
                Action dragAction = () => item.DoDragDrop(p, DragDropEffects.Move);
                item.MouseDown += (s, e) => dragAction();
                ctrlImg.MouseDown += (s, e) => dragAction();
                lbl.MouseDown += (s, e) => dragAction();
                lblSub.MouseDown += (s, e) => dragAction();
                lblDrag.MouseDown += (s, e) => dragAction();

                item.Controls.AddRange(new Control[] { ctrlImg, lbl, lblSub, lblDrag });
                flowAlmacenProductos.Controls.Add(item);
            }

            flowAlmacenProductos.ResumeLayout();
        }

        private void FlowLienzoProductos_DragDrop(object? sender, DragEventArgs e)
        {
            if (rbOrdenPersonalizado == null || !rbOrdenPersonalizado.Checked) return;

            if (e.Data!.GetDataPresent(typeof(Producto)))
            {
                var prod = (Producto)e.Data.GetData(typeof(Producto))!;
                int nuevoOrden = flowLienzoProductos.Controls.Count + 1;
                _configService.ReordenarEInsertarProducto(prod.ProductoID, _catEdicionActiva, _famEdicionActiva, nuevoOrden);

                RefrescarLienzoYAlmacen();
            }
        }

        private void PromptNuevaCategoria()
        {
            using var modal = new FormNuevaCategoriaModal(_configService);
            if (modal.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(modal.CategoriaCreada))
            {
                _catEdicionActiva = modal.CategoriaCreada;
                CargarDatosEspejoConfig();
            }
        }

        private void PromptEditarCategoria(PosCategoria cat)
        {
            using Form prompt = new Form
            {
                Width = 400,
                Height = 230,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Editar Categoría",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblNom = new Label { Left = 20, Top = 15, Text = "Nombre de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtNom = new TextBox { Left = 20, Top = 38, Width = 345, Text = cat.Nombre, Font = new Font("Segoe UI", 9.5F) };

            Label lblColor = new Label { Left = 20, Top = 72, Text = "Color de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Button btnMuestra = new Button { Left = 20, Top = 95, Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            btnMuestra.FlatAppearance.BorderSize = 1;

            ComboBox cbColor = new ComboBox { Left = 50, Top = 95, Width = 315, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            int selectedIdx = 0;

            for (int i = 0; i < CategoriaColorHelper.OpcionesPaleta.Count; i++)
            {
                var opc = CategoriaColorHelper.OpcionesPaleta[i];
                cbColor.Items.Add(opc.Nombre);
                if (ColorTranslator.ToHtml(opc.Texto).Equals(cat.TextoHex, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIdx = i;
                }
            }

            cbColor.SelectedIndexChanged += (s, e) =>
            {
                if (cbColor.SelectedIndex >= 0)
                {
                    var pal = CategoriaColorHelper.OpcionesPaleta[cbColor.SelectedIndex];
                    btnMuestra.BackColor = pal.Texto;
                    btnMuestra.FlatAppearance.BorderColor = pal.Borde;
                }
            };
            cbColor.SelectedIndex = selectedIdx;

            Button btnEliminar = new Button
            {
                Text = "🗑️ Eliminar",
                Left = 20,
                Width = 110,
                Top = 140,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    $"¿Seguro que deseas eliminar la categoría \"{cat.Nombre}\"?\n\nLos productos asociados quedarán \"Sin Asignar\" en el almacén.",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    _configService.EliminarCategoria(cat.Id);
                    _categoriasCache = _configService.ObtenerCategoriasPOS();
                    _catEdicionActiva = _categoriasCache.FirstOrDefault()?.Nombre ?? "";
                    _famEdicionActiva = "";

                    prompt.DialogResult = DialogResult.Abort;
                    prompt.Close();

                    RenderizarCategoriasConfig();
                    RenderizarFamiliasConfig();
                    RefrescarLienzoYAlmacen();
                }
            };

            Button btnGuardar = new Button
            {
                Text = "Guardar Cambios",
                Left = 215,
                Width = 150,
                Top = 140,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

            prompt.Controls.AddRange(new Control[] { lblNom, txtNom, lblColor, btnMuestra, cbColor, btnEliminar, btnGuardar });
            prompt.AcceptButton = btnGuardar;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nuevoNombre = txtNom.Text.Trim();
                var pal = CategoriaColorHelper.OpcionesPaleta[Math.Max(0, cbColor.SelectedIndex)];

                if (!string.IsNullOrWhiteSpace(nuevoNombre) && nuevoNombre != cat.Nombre)
                {
                    _configService.RenombrarCategoria(cat.Id, nuevoNombre);
                    _catEdicionActiva = nuevoNombre;
                }

                _configService.ActualizarColorCategoria(cat.Id,
                    ColorTranslator.ToHtml(pal.Fondo),
                    ColorTranslator.ToHtml(pal.Texto),
                    ColorTranslator.ToHtml(pal.Borde));

                CargarDatosEspejoConfig();
            }
        }

        private void PromptNuevaFamilia(PosCategoria cat)
        {
            using var modal = new FormNuevaFamiliaModal(cat.Id, cat.Nombre, _configService);
            if (modal.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(modal.FamiliaCreada))
            {
                _categoriasCache = _configService.ObtenerCategoriasPOS();
                _famEdicionActiva = modal.FamiliaCreada;
                RenderizarFamiliasConfig();
                RefrescarLienzoYAlmacen();
            }
        }

        private void PromptRenombrarFamilia(PosFamilia fam)
        {
            using Form prompt = new Form
            {
                Width = 380,
                Height = 170,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Editar Subfamilia",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label textLabel = new Label { Left = 20, Top = 15, Text = "Nombre de la subfamilia:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox textBox = new TextBox { Left = 20, Top = 40, Width = 325, Text = fam.Nombre, Font = new Font("Segoe UI", 10F) };

            Button btnEliminar = new Button
            {
                Text = "🗑️ Eliminar",
                Left = 20,
                Width = 100,
                Top = 80,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    $"¿Seguro que deseas eliminar la subfamilia \"{fam.Nombre}\"?\n\nLos productos se mantendrán en esta categoría y se mostrarán en \"Todas\".",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    _configService.EliminarFamilia(fam.Id);
                    _categoriasCache = _configService.ObtenerCategoriasPOS();
                    _famEdicionActiva = "";

                    prompt.DialogResult = DialogResult.Abort;
                    prompt.Close();

                    RenderizarFamiliasConfig();
                    RefrescarLienzoYAlmacen();
                }
            };

            Button confirmation = new Button
            {
                Text = "Guardar",
                Left = 225,
                Width = 120,
                Top = 80,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            confirmation.FlatAppearance.BorderSize = 0;

            prompt.Controls.AddRange(new Control[] { textLabel, textBox, btnEliminar, confirmation });
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nuevo = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != fam.Nombre)
                {
                    _configService.RenombrarFamilia(fam.Id, nuevo);
                    if (_famEdicionActiva == fam.Nombre) _famEdicionActiva = nuevo;
                    CargarDatosEspejoConfig();
                }
            }
        }
    }
}