using System;
using System.Collections.Generic;
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

        // Campos Base
        private TextBox txtCodigoBarra = null!;
        private TextBox txtNombre = null!;
        private ComboBox cbCategoria = null!;
        private ComboBox cbFamilia = null!;
        private TextBox txtPrecioCosto = null!;

        // Modalidad Precio POS
        private RadioButton rbModoListaFija = null!;
        private RadioButton rbModoEscala = null!;
        private Panel pnlCardListaFija = null!;
        private Panel pnlCardEscala = null!;
        private ComboBox cbListaPrecioPOS = null!;
        private ComboBox cbCantidadTramos = null!;

        // Sección Tramos Dinámicos
        private Panel pnlSeccionTramos = null!;
        private FlowLayoutPanel flpFilasTramos = null!;
        private List<FilaTramoUI> _filasTramos = new List<FilaTramoUI>();

        // Precios y Stock
        private TextBox txtPrecioUnitario = null!;
        private TextBox txtStockActual = null!;
        private TextBox txtStockMinimo = null!;
        private PictureBox pbFoto = null!;

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
            this.Size = new Size(680, 780);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlBorde = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 20),
                BackColor = Color.White
            };
            pnlBorde.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlBorde.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            // Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
            pnlHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            Label lblTitulo = new Label
            {
                Text = _productoAEditar == null ? "📦 Registrar Nuevo Producto" : "✏️ Editar Producto",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
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
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(btnCerrar);

            // FlowLayout Principal
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 0),
                AutoScroll = true
            };

            // 1. Fila Código y Nombre
            txtCodigoBarra = new TextBox { Width = 295, Font = new Font("Segoe UI", 10F) };
            txtNombre = new TextBox { Width = 295, Font = new Font("Segoe UI", 10F) };
            Panel pnlFila1 = new Panel { Size = new Size(610, 56) };
            pnlFila1.Controls.Add(CrearCampo("|||||| Código de Barra / SKU", txtCodigoBarra, 0, 295));
            pnlFila1.Controls.Add(CrearCampo("📦 Nombre del Producto", txtNombre, 315, 295));

            // 2. Fila Categoría y Familia
            cbCategoria = new ComboBox { Width = 295, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDown };
            cbFamilia = new ComboBox { Width = 295, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDown };
            Panel pnlFila2 = new Panel { Size = new Size(610, 56) };
            pnlFila2.Controls.Add(CrearCampo("Categoría", cbCategoria, 0, 295));
            pnlFila2.Controls.Add(CrearCampo("Familia de Producto", cbFamilia, 315, 295));

            // 3. Tarjeta Costo Neto
            Panel pnlCardCosto = CrearCardPanel(610, 68);
            Label lblIconCosto = new Label { Text = "$", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(14, 14), Size = new Size(24, 24), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(241, 245, 249) };
            Label lblTitCosto = new Label { Text = "Costo Neto Base ($)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(46, 10), AutoSize = true };
            Label lblSubCosto = new Label { Text = "Costo en pesos sin IVA.", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(46, 46), AutoSize = true };
            txtPrecioCosto = new TextBox { Location = new Point(46, 26), Width = 540, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtPrecioCosto.TextChanged += (s, e) => RecalcularPrecioSegunLista();
            pnlCardCosto.Controls.AddRange(new Control[] { lblIconCosto, lblTitCosto, txtPrecioCosto, lblSubCosto });

            // 4. Modalidad de Precio POS
            Panel pnlCardModalidad = CrearCardPanel(610, 165);
            Label lblTitMod = new Label { Text = "🏷️ Modalidad de Precio en Punto de Venta (POS)", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(14, 10), AutoSize = true };

            // SubCard Izquierda: Lista Fija
            pnlCardListaFija = new Panel { Location = new Point(14, 34), Size = new Size(280, 118), BackColor = Color.FromArgb(248, 250, 252), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            rbModoListaFija = new RadioButton { Text = "Lista Fija", Location = new Point(12, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Checked = true };
            Label lblDescFija = new Label { Text = "El producto se vende siempre en la misma\nlista de precios.", Location = new Point(32, 32), Size = new Size(235, 30), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };
            cbListaPrecioPOS = new ComboBox { Location = new Point(12, 72), Size = new Size(254, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5F) };
            LlenarComboListas(cbListaPrecioPOS);
            cbListaPrecioPOS.SelectedIndex = 0;
            cbListaPrecioPOS.SelectedIndexChanged += (s, e) => RecalcularPrecioSegunLista();
            pnlCardListaFija.Controls.AddRange(new Control[] { rbModoListaFija, lblDescFija, cbListaPrecioPOS });

            // SubCard Derecha: Rango de Cantidades
            pnlCardEscala = new Panel { Location = new Point(306, 34), Size = new Size(290, 118), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            rbModoEscala = new RadioButton { Text = "Venta por Escala / Rango de Cantidades", Location = new Point(12, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Label lblDescEsc = new Label { Text = "El precio cambia según la cantidad comprada.", Location = new Point(32, 32), Size = new Size(245, 16), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };
            Label lblTitCantTr = new Label { Text = "Selecciona la cantidad de tramos:", Location = new Point(12, 54), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            cbCantidadTramos = new ComboBox { Location = new Point(12, 72), Size = new Size(264, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5F), Enabled = false };
            cbCantidadTramos.Items.AddRange(new object[] { "2 tramos", "3 tramos", "4 tramos", "5 tramos" });
            cbCantidadTramos.SelectedIndex = 0;
            cbCantidadTramos.SelectedIndexChanged += (s, e) => RegenerarFilasTramos(cbCantidadTramos.SelectedIndex + 2);
            pnlCardEscala.Controls.AddRange(new Control[] { rbModoEscala, lblDescEsc, lblTitCantTr, cbCantidadTramos });

            pnlCardModalidad.Controls.AddRange(new Control[] { lblTitMod, pnlCardListaFija, pnlCardEscala });

            // Exclusión mutua garantizada al hacer clic en RadioButtons o en sus Cards
            rbModoListaFija.Click += (s, e) => SeleccionarModo(escala: false);
            rbModoEscala.Click += (s, e) => SeleccionarModo(escala: true);
            pnlCardListaFija.Click += (s, e) => SeleccionarModo(escala: false);
            pnlCardEscala.Click += (s, e) => SeleccionarModo(escala: true);

            // 5. Sección Configurar Tramos Dinámicos
            pnlSeccionTramos = CrearCardPanel(610, 205);
            Label lblTitSecTr = new Label { Text = "📊 Configurar Tramos de Cantidad y Lista de Precios", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(14, 10), AutoSize = true };

            Panel pnlHeaderGrid = new Panel { Location = new Point(14, 32), Size = new Size(580, 24), BackColor = Color.Transparent };
            pnlHeaderGrid.Controls.Add(new Label { Text = "Tramo", Location = new Point(4, 4), Size = new Size(45, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) });
            pnlHeaderGrid.Controls.Add(new Label { Text = "Desde (unidades)", Location = new Point(70, 4), Size = new Size(110, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) });
            pnlHeaderGrid.Controls.Add(new Label { Text = "Hasta (unidades)", Location = new Point(230, 4), Size = new Size(110, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) });
            pnlHeaderGrid.Controls.Add(new Label { Text = "Lista de Precios", Location = new Point(360, 4), Size = new Size(180, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) });

            flpFilasTramos = new FlowLayoutPanel
            {
                Location = new Point(14, 58),
                Size = new Size(580, 138),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            pnlSeccionTramos.Controls.AddRange(new Control[] { lblTitSecTr, pnlHeaderGrid, flpFilasTramos });

            // 6. Fila Precios y Stocks (Stock Actual Editable)
            Panel pnlFilaPrecios = new Panel { Size = new Size(610, 60) };
            txtPrecioUnitario = new TextBox { Width = 230, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Text = "0" };
            txtStockActual = new TextBox { Width = 150, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Text = "0" };
            txtStockMinimo = new TextBox { Width = 150, Font = new Font("Segoe UI", 10F), Text = "5" };

            pnlFilaPrecios.Controls.Add(CrearCampo("💲 Precio Venta Base POS (IVA Inc.)", txtPrecioUnitario, 0, 230));
            pnlFilaPrecios.Controls.Add(CrearCampo("📦 Stock Actual (Editable)", txtStockActual, 245, 150));
            pnlFilaPrecios.Controls.Add(CrearCampo("🔔 Stock Mínimo Alerta", txtStockMinimo, 410, 150));

            // 7. Sección Foto
            Panel pnlCardFoto = CrearCardPanel(610, 74);
            Label lblTitFoto = new Label { Text = "🖼️ Foto del Producto", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(14, 6), AutoSize = true };
            pbFoto = new PictureBox { Location = new Point(14, 24), Size = new Size(42, 42), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(248, 250, 252) };
            Button btnCargarFoto = new Button { Text = "📤 Seleccionar imagen", Location = new Point(64, 28), Size = new Size(150, 32), BackColor = Color.FromArgb(239, 246, 255), ForeColor = Color.FromArgb(29, 78, 216), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCargarFoto.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnCargarFoto.Click += BtnCargarFoto_Click;

            Button btnQuitarFoto = new Button { Text = "🗑️ Quitar", Location = new Point(220, 28), Size = new Size(80, 32), BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(220, 38, 38), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnQuitarFoto.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnQuitarFoto.Click += (s, e) => { _rutaImagenSeleccionada = string.Empty; pbFoto.Image?.Dispose(); pbFoto.Image = null; };

            Label lblFormatos = new Label { Text = "Formatos permitidos: JPG, PNG. Máx. 2MB.", Location = new Point(310, 36), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(148, 163, 184) };
            pnlCardFoto.Controls.AddRange(new Control[] { lblTitFoto, pbFoto, btnCargarFoto, btnQuitarFoto, lblFormatos });

            // Agregar todo al Flow
            flow.Controls.AddRange(new Control[] { pnlFila1, pnlFila2, pnlCardCosto, pnlCardModalidad, pnlSeccionTramos, pnlFilaPrecios, pnlCardFoto });

            // Botonera Inferior
            Panel pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.Transparent };
            Button btnGuardar = new Button
            {
                Text = "💾 Guardar Cambios",
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(430, 6)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            Button btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(310, 6)
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            pnlBotones.Controls.AddRange(new Control[] { btnGuardar, btnCancelar });

            pnlBorde.Controls.AddRange(new Control[] { flow, pnlBotones, pnlHeader });
            this.Controls.Add(pnlBorde);

            RegenerarFilasTramos(2);
            SeleccionarModo(escala: false);
        }

        private void SeleccionarModo(bool escala)
        {
            rbModoListaFija.Checked = !escala;
            rbModoEscala.Checked = escala;

            pnlCardListaFija.BackColor = !escala ? Color.FromArgb(239, 246, 255) : Color.White;
            pnlCardEscala.BackColor = escala ? Color.FromArgb(239, 246, 255) : Color.White;

            cbListaPrecioPOS.Enabled = !escala;
            cbCantidadTramos.Enabled = escala;
            pnlSeccionTramos.Visible = escala;

            RecalcularPrecioSegunLista();
        }

        private Panel CrearCardPanel(int w, int h)
        {
            Panel p = new Panel { Size = new Size(w, h), BackColor = Color.White, Margin = new Padding(0, 0, 0, 8) };
            p.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            return p;
        }

        private Panel CrearCampo(string titulo, Control ctrl, int x, int w)
        {
            Panel p = new Panel { Location = new Point(x, 0), Size = new Size(w, 52) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 20);
            p.Controls.AddRange(new Control[] { l, ctrl });
            return p;
        }

        private void LlenarComboListas(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.AddRange(new object[]
            {
                "Lista 1 (Público General)",
                "Lista 2 (Clientes Frecuentes)",
                "Lista 3 (Mayorista 2)",
                "Lista 4 (Distribuidor)",
                "Lista 5 (Liquidación / Oferta)",
                "Lista 6", "Lista 7", "Lista 8", "Lista 9", "Lista 10"
            });
        }

        private void RegenerarFilasTramos(int totalTramos)
        {
            flpFilasTramos.SuspendLayout();
            flpFilasTramos.Controls.Clear();
            _filasTramos.Clear();

            decimal ultimoHasta = 5;

            for (int i = 1; i <= totalTramos; i++)
            {
                bool esPrimero = i == 1;
                bool esUltimo = i == totalTramos;

                var fila = new FilaTramoUI(i, esPrimero, esUltimo, ultimoHasta);
                _filasTramos.Add(fila);
                flpFilasTramos.Controls.Add(fila.Contenedor);

                ultimoHasta = fila.ValorHasta + 5;
            }

            for (int i = 0; i < _filasTramos.Count - 1; i++)
            {
                int nextIdx = i + 1;
                var actual = _filasTramos[i];
                var siguiente = _filasTramos[nextIdx];

                if (actual.NumHasta != null)
                {
                    actual.NumHasta.ValueChanged += (s, e) =>
                    {
                        siguiente.ActualizarDesde(actual.NumHasta.Value + 1);
                    };
                }
            }

            flpFilasTramos.ResumeLayout();
        }

        private void CargarListasDesplegables()
        {
            try
            {
                using var db = new AppDbContext();
                var categorias = db.Productos.Where(p => p.Estado && !string.IsNullOrEmpty(p.Categoria)).Select(p => p.Categoria).Distinct().OrderBy(c => c).ToList();
                var familias = db.Productos.Where(p => p.Estado && !string.IsNullOrEmpty(p.NFamilia)).Select(p => p.NFamilia!).Distinct().OrderBy(f => f).ToList();

                cbCategoria.Items.Clear();
                cbCategoria.Items.AddRange(categorias.Count > 0 ? categorias.ToArray() : new string[] { "Abarrotes", "Lácteos", "Bebidas", "Aseo", "General" });

                cbFamilia.Items.Clear();
                cbFamilia.Items.AddRange(familias.Count > 0 ? familias.ToArray() : new string[] { "Aderezos", "Leche", "Quesos", "General" });

                cbCategoria.Text = "Abarrotes";
                cbFamilia.Text = "Aderezos";
            }
            catch { }
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

                int listaIdx = Math.Max(1, Math.Min(10, _productoAEditar.ListaDefectoPOS)) - 1;
                cbListaPrecioPOS.SelectedIndex = listaIdx;

                txtPrecioUnitario.Text = _productoAEditar.PrecioUnitario.ToString("0");
                txtStockActual.Text = _productoAEditar.Stock.ToString();
                txtStockMinimo.Text = _productoAEditar.StockMinimo.ToString();

                _rutaImagenSeleccionada = _productoAEditar.ImagenPath ?? string.Empty;
                if (!string.IsNullOrEmpty(_rutaImagenSeleccionada) && File.Exists(_rutaImagenSeleccionada))
                {
                    try
                    {
                        using var stream = new MemoryStream(File.ReadAllBytes(_rutaImagenSeleccionada));
                        pbFoto.Image = new Bitmap(stream);
                    }
                    catch { }
                }

                CargarEscalaDesdeBD(_productoAEditar.ProductoID);
                RecalcularPrecioSegunLista();
            }
        }

        private void CargarEscalaDesdeBD(int productoId)
        {
            try
            {
                using var db = new AppDbContext();
                // Cargar todas las reglas del producto, incluso si están en Bloqueo = 1
                var reglas = db.PreciosQ
                    .Where(pq => pq.IdProducto == productoId)
                    .OrderBy(pq => pq.Qini)
                    .ToList();

                if (reglas.Count >= 2)
                {
                    // Si al menos una regla no está bloqueada, marcamos Modo Escala; si están bloqueadas, Lista Fija
                    bool estaActivo = reglas.Any(r => r.Bloqueo == 0);
                    SeleccionarModo(escala: estaActivo);

                    int cant = Math.Min(5, reglas.Count);
                    cbCantidadTramos.SelectedIndex = cant - 2;
                    RegenerarFilasTramos(cant);

                    for (int i = 0; i < cant; i++)
                    {
                        var r = reglas[i];
                        if (_filasTramos[i].NumHasta != null)
                            _filasTramos[i].NumHasta!.Value = Math.Max(1, r.Qfin);

                        _filasTramos[i].CbLista.SelectedIndex = ObtenerIndiceDesdeIdPrecio(r.IdPrecio);
                    }
                }
                else
                {
                    SeleccionarModo(escala: false);
                }
            }
            catch { }
        }

        private int ObtenerIndiceDesdeIdPrecio(string? idPrecio)
        {
            if (string.IsNullOrEmpty(idPrecio)) return 0;
            string clean = idPrecio.Trim().ToLower().Replace("precio", "").Replace("d", "1");
            if (int.TryParse(clean, out int idx) && idx >= 1 && idx <= 10)
                return idx - 1;
            return 0;
        }

        private void RecalcularPrecioSegunLista()
        {
            if (!decimal.TryParse(txtPrecioCosto.Text.Trim(), out decimal costo) || costo <= 0) return;

            int nroLista = rbModoEscala.Checked && _filasTramos.Count > 0
                ? (_filasTramos[0].CbLista.SelectedIndex + 1)
                : (cbListaPrecioPOS.SelectedIndex + 1);

            if (_productoAEditar != null)
            {
                decimal precio = ObtenerPrecioPorListaNumero(_productoAEditar, nroLista);
                txtPrecioUnitario.Text = precio.ToString("0");
            }
            else
            {
                decimal neto = costo * 1.35m;
                txtPrecioUnitario.Text = (Math.Round(neto * 1.19m / 10m, 0, MidpointRounding.AwayFromZero) * 10m).ToString("0");
            }
        }

        private decimal ObtenerPrecioPorListaNumero(Producto p, int nroLista)
        {
            return nroLista switch
            {
                1 => p.PrecioUnitario,
                2 => p.Precio2 > 0 ? p.Precio2 : p.PrecioUnitario,
                3 => p.Precio3 > 0 ? p.Precio3 : p.PrecioUnitario,
                4 => p.Precio4 > 0 ? p.Precio4 : p.PrecioUnitario,
                5 => p.Precio5 > 0 ? p.Precio5 : p.PrecioUnitario,
                6 => p.Precio6 > 0 ? p.Precio6 : p.PrecioUnitario,
                7 => p.Precio7 > 0 ? p.Precio7 : p.PrecioUnitario,
                8 => p.Precio8 > 0 ? p.Precio8 : p.PrecioUnitario,
                9 => p.Precio9 > 0 ? p.Precio9 : p.PrecioUnitario,
                10 => p.Precio10 > 0 ? p.Precio10 : p.PrecioUnitario,
                _ => p.PrecioUnitario
            };
        }

        private void BtnCargarFoto_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog { Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp" };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                _rutaImagenSeleccionada = ofd.FileName;
                try
                {
                    using var stream = new MemoryStream(File.ReadAllBytes(_rutaImagenSeleccionada));
                    pbFoto.Image?.Dispose();
                    pbFoto.Image = new Bitmap(stream);
                }
                catch { }
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
            decimal.TryParse(txtPrecioUnitario.Text.Trim(), out decimal pvp);
            int.TryParse(txtStockActual.Text.Trim(), out int stockActual);
            int.TryParse(txtStockMinimo.Text.Trim(), out int stockMin);

            int listaSeleccionada = rbModoEscala.Checked ? 1 : (cbListaPrecioPOS.SelectedIndex + 1);

            try
            {
                using var db = new AppDbContext();

                if (_productoAEditar == null)
                {
                    var nuevoProd = new Producto
                    {
                        CodigoBarra = string.IsNullOrWhiteSpace(txtCodigoBarra.Text) ? "GEN-" + DateTime.Now.Ticks.ToString().Substring(12) : txtCodigoBarra.Text.Trim(),
                        Nombre = nombre,
                        Categoria = cbCategoria.Text.Trim(),
                        NFamilia = cbFamilia.Text.Trim(),
                        PrecioCosto = costo,
                        ListaDefectoPOS = listaSeleccionada,
                        PrecioUnitario = pvp,
                        Stock = stockActual,
                        StockMinimo = stockMin > 0 ? stockMin : 5,
                        ImagenPath = _rutaImagenSeleccionada,
                        Estado = true,
                        FchUpd = DateTime.Now
                    };

                    db.Productos.Add(nuevoProd);
                    db.SaveChanges();

                    GuardarReglasEscalaBD(db, nuevoProd.ProductoID, nuevoProd.Nombre);
                    ProductoResultado = nuevoProd;
                }
                else
                {
                    var prodBd = db.Productos.Find(_productoAEditar.ProductoID);
                    if (prodBd != null)
                    {
                        prodBd.CodigoBarra = txtCodigoBarra.Text.Trim();
                        prodBd.Nombre = nombre;
                        prodBd.Categoria = cbCategoria.Text.Trim();
                        prodBd.NFamilia = cbFamilia.Text.Trim();
                        prodBd.PrecioCosto = costo;
                        prodBd.ListaDefectoPOS = listaSeleccionada;
                        prodBd.Stock = stockActual;
                        prodBd.StockMinimo = stockMin;
                        prodBd.ImagenPath = _rutaImagenSeleccionada;
                        prodBd.FchUpd = DateTime.Now;
                        prodBd.Sincro = 0;

                        GuardarReglasEscalaBD(db, prodBd.ProductoID, prodBd.Nombre);
                        db.SaveChanges();

                        ProductoResultado = prodBd;
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GuardarReglasEscalaBD(AppDbContext db, int productoId, string nombreProducto)
        {
            var existentes = db.PreciosQ.Where(pq => pq.IdProducto == productoId).ToList();

            // Determinar el estado de bloqueo: si está en Escala se activa (0), si está en Lista Fija se suspende (1)
            int bloqueoEstado = rbModoEscala.Checked ? 0 : 1;

            if (_filasTramos.Count >= 2)
            {
                // Limpiar para actualizar la configuración completa sin perderla
                if (existentes.Count > 0)
                {
                    db.PreciosQ.RemoveRange(existentes);
                }

                var prod = db.Productos.Find(productoId);

                for (int i = 0; i < _filasTramos.Count; i++)
                {
                    var f = _filasTramos[i];
                    decimal qIni = f.ValorDesde;
                    decimal qFin = f.EsUltimo ? 999999.000m : f.ValorHasta;
                    int nroLista = f.CbLista.SelectedIndex + 1;
                    decimal precioValor = prod != null ? ObtenerPrecioPorListaNumero(prod, nroLista) : 0m;

                    db.PreciosQ.Add(new PrecioQ
                    {
                        IdProducto = productoId,
                        NProducto = nombreProducto,
                        Qini = qIni,
                        Qfin = qFin,
                        NPrecio = precioValor,
                        IdPrecio = nroLista.ToString(),
                        Bloqueo = bloqueoEstado, // 0 = Activo en POS | 1 = Guardado pero suspendido
                        FchMod = DateTime.Now,
                        HoraMod = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
            }
        }

        // ================= CLASE AUXILIAR PARA CADA FILA DE TRAMO =================
        private class FilaTramoUI
        {
            public Panel Contenedor { get; }
            public NumericUpDown? NumDesde { get; }
            public NumericUpDown? NumHasta { get; }
            public ComboBox CbLista { get; }
            public bool EsPrimero { get; }
            public bool EsUltimo { get; }

            public decimal ValorDesde => NumDesde?.Value ?? (EsPrimero ? 1m : 1m);
            public decimal ValorHasta => NumHasta?.Value ?? 999999m;

            public FilaTramoUI(int nroTramo, bool esPrimero, bool esUltimo, decimal sugeridoHasta)
            {
                EsPrimero = esPrimero;
                EsUltimo = esUltimo;

                Contenedor = new Panel { Size = new Size(560, 32), Margin = new Padding(0, 0, 0, 4) };

                // Badge N° Tramo
                Label lblBadge = new Label
                {
                    Text = nroTramo.ToString(),
                    Size = new Size(24, 24),
                    Location = new Point(4, 4),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Desde (unidades)
                NumDesde = new NumericUpDown
                {
                    Location = new Point(50, 4),
                    Size = new Size(95, 24),
                    Minimum = 1,
                    Maximum = 999999,
                    Value = esPrimero ? 1 : (sugeridoHasta - 4),
                    Enabled = false,
                    TextAlign = HorizontalAlignment.Center,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                Label lblFlecha = new Label { Text = "➔", Location = new Point(160, 6), Size = new Size(20, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184) };

                // Hasta (unidades) o Símbolo Infinito
                Control ctrlHasta;
                if (!esUltimo)
                {
                    NumHasta = new NumericUpDown
                    {
                        Location = new Point(195, 4),
                        Size = new Size(95, 24),
                        Minimum = 1,
                        Maximum = 999999,
                        Value = sugeridoHasta,
                        TextAlign = HorizontalAlignment.Center,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                    };
                    ctrlHasta = NumHasta;
                }
                else
                {
                    ctrlHasta = new Label
                    {
                        Text = "∞",
                        Location = new Point(195, 4),
                        Size = new Size(95, 24),
                        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(71, 85, 105),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.FromArgb(248, 250, 252)
                    };
                }

                // Combo Lista de Precios
                CbLista = new ComboBox
                {
                    Location = new Point(310, 4),
                    Size = new Size(210, 24),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 8.5F)
                };
                CbLista.Items.AddRange(new object[]
                {
                    "Lista 1 (Público General)",
                    "Lista 2 (Clientes Frecuentes)",
                    "Lista 3 (Mayorista 2)",
                    "Lista 4 (Distribuidor)",
                    "Lista 5 (Liquidación / Oferta)",
                    "Lista 6", "Lista 7", "Lista 8", "Lista 9", "Lista 10"
                });
                CbLista.SelectedIndex = Math.Min(9, nroTramo - 1);

                Contenedor.Controls.AddRange(new Control[] { lblBadge, NumDesde, lblFlecha, ctrlHasta, CbLista });
            }

            public void ActualizarDesde(decimal nuevoDesde)
            {
                if (NumDesde != null)
                {
                    NumDesde.Value = nuevoDesde;
                    if (NumHasta != null && NumHasta.Value <= nuevoDesde)
                    {
                        NumHasta.Value = nuevoDesde + 4;
                    }
                }
            }
        }
    }
}