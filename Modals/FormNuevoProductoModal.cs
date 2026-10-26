using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormNuevoProductoModal : Form
    {
        private readonly ProductoService _productoService = new ProductoService();
        private readonly ConfiguracionService _configService = new ConfiguracionService();

        // Campos Base
        private TextBox txtCodigoBarra = null!;
        private TextBox txtNombre = null!;
        private TextBox txtNombreCorto = null!;
        private ComboBox cbCategoria = null!;
        private Button btnNuevaCat = null!;
        private ComboBox cbFamilia = null!;
        private Button btnNuevaFam = null!;
        private TextBox txtPrecioCosto = null!;
        private Label lblTitCosto = null!;
        private Label lblSubCosto = null!;

        // Configuración Producto Pesable
        private CheckBox chkEsPesable = null!;
        private Label lblHeaderDesde = null!;
        private Label lblHeaderHasta = null!;

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

        // Precios, Stock y Merma
        private TextBox txtPrecioUnitario = null!;
        private Label lblTitPrecioVenta = null!;
        private TextBox txtStockActual = null!;
        private Label lblTitStockActual = null!;
        private TextBox txtPorcentajeMerma = null!;
        private TextBox txtStockDisponible = null!;
        private Label lblTitStockDisponible = null!;
        private TextBox txtStockMinimo = null!;
        private PictureBox pbFoto = null!;
        private bool _cargandoDatos = false;

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

            if (_productoAEditar == null)
            {
                txtCodigoBarra.Text = _productoService.ObtenerSiguienteCodigoBarraSugerido();
            }
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Text = _productoAEditar == null ? "Registrar Nuevo Producto" : "Editar Producto";
            this.Size = new Size(820, 840);
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlBorde = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 14),
                BackColor = Color.White
            };

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 10, 0),
                AutoScroll = true
            };

            int anchoSeccion = 740;

            // =========================================================================
            // Fila 1: Código de Barra (200px), Nombre Completo (340px) y Nombre Corto (190px)
            // =========================================================================
            txtCodigoBarra = new TextBox { Width = 190, Font = new Font("Segoe UI", 9.5F) };
            txtNombre = new TextBox { Width = 330, Font = new Font("Segoe UI", 9.5F) };
            txtNombreCorto = new TextBox { Width = 190, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "Para botones POS..." };

            Panel pnlFila1 = new Panel { Size = new Size(anchoSeccion, 56) };
            pnlFila1.Controls.Add(CrearCampo("|||||| Código / SKU", txtCodigoBarra, 0, 190));
            pnlFila1.Controls.Add(CrearCampo("📦 Nombre del Producto", txtNombre, 200, 330));
            pnlFila1.Controls.Add(CrearCampo("🏷️ Nombre Corto (POS)", txtNombreCorto, 540, 190));

            // =========================================================================
            // Fila 2: Categoría con botón [+] y Familia con botón [+]
            // =========================================================================
            Panel pnlFila2 = new Panel { Size = new Size(anchoSeccion, 56) };

            // Panel Izquierdo: Categoría
            Panel pnlCatGroup = new Panel { Location = new Point(0, 0), Size = new Size(360, 52) };
            Label lblCat = new Label { Text = "Categoría", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            cbCategoria = new ComboBox 
            { 
                Location = new Point(0, 20), 
                Width = 320, 
                Font = new Font("Segoe UI", 9.5F), 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            btnNuevaCat = new Button
            {
                Text = "➕",
                Location = new Point(325, 19),
                Size = new Size(32, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevaCat.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnNuevaCat.Click += BtnNuevaCat_Click;
            pnlCatGroup.Controls.AddRange(new Control[] { lblCat, cbCategoria, btnNuevaCat });

            // Panel Derecho: Familia
            Panel pnlFamGroup = new Panel { Location = new Point(380, 0), Size = new Size(360, 52) };
            Label lblFam = new Label { Text = "Familia de Producto", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            cbFamilia = new ComboBox 
            { 
                Location = new Point(0, 20), 
                Width = 320, 
                Font = new Font("Segoe UI", 9.5F), 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            btnNuevaFam = new Button
            {
                Text = "➕",
                Location = new Point(325, 19),
                Size = new Size(32, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnNuevaFam.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnNuevaFam.Click += BtnNuevaFam_Click;
            pnlFamGroup.Controls.AddRange(new Control[] { lblFam, cbFamilia, btnNuevaFam });

            cbCategoria.SelectedIndexChanged += (s, e) => ActualizarFamiliasSegunCategoria();

            pnlFila2.Controls.Add(pnlCatGroup);
            pnlFila2.Controls.Add(pnlFamGroup);

            // Tarjeta Tipo de Venta: Pesable / Unidad
            Panel pnlCardPesable = CrearCardPanel(anchoSeccion, 48);
            chkEsPesable = new CheckBox
            {
                Text = "⚖️ Producto Pesable / A Granel (Se vende por peso: Gramos y Kilos)",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 12),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            chkEsPesable.CheckedChanged += (s, e) => ActualizarEtiquetasSegunModalidadPeso();
            pnlCardPesable.Controls.Add(chkEsPesable);

            // Tarjeta Costo Neto
            Panel pnlCardCosto = CrearCardPanel(anchoSeccion, 68);
            Label lblIconCosto = new Label { Text = "$", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(14, 14), Size = new Size(24, 24), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(241, 245, 249) };
            lblTitCosto = new Label { Text = "Costo Neto Base ($)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(46, 10), AutoSize = true };
            lblSubCosto = new Label { Text = "Costo en pesos sin IVA.", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(46, 46), AutoSize = true };
            txtPrecioCosto = new TextBox { Location = new Point(46, 26), Width = 670, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "0" };
            txtPrecioCosto.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtPrecioCosto.TextChanged += (s, e) => 
            { 
                MonedaHelper.AplicarMascaraEnVivo(txtPrecioCosto);
                RecalcularPrecioSegunLista(); 
            };
            pnlCardCosto.Controls.AddRange(new Control[] { lblIconCosto, lblTitCosto, txtPrecioCosto, lblSubCosto });

            // Tarjeta Modalidad de Precio
            Panel pnlCardModalidad = CrearCardPanel(anchoSeccion, 165);
            Label lblTitMod = new Label { Text = "🏷️ Modalidad de Precio en Punto de Venta (POS)", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(14, 10), AutoSize = true };

            pnlCardListaFija = new Panel { Location = new Point(14, 34), Size = new Size(345, 118), BackColor = Color.FromArgb(239, 246, 255), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            rbModoListaFija = new RadioButton { Text = "Lista Fija", Location = new Point(12, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Checked = true };
            Label lblDescFija = new Label { Text = "El producto se vende siempre en la misma\nlista de precios.", Location = new Point(32, 32), Size = new Size(290, 30), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };
            cbListaPrecioPOS = new ComboBox { Location = new Point(12, 72), Size = new Size(315, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5F) };
            LlenarComboListas(cbListaPrecioPOS);
            cbListaPrecioPOS.SelectedIndex = 0;
            cbListaPrecioPOS.SelectedIndexChanged += (s, e) => RecalcularPrecioSegunLista();
            pnlCardListaFija.Controls.AddRange(new Control[] { rbModoListaFija, lblDescFija, cbListaPrecioPOS });

            pnlCardEscala = new Panel { Location = new Point(375, 34), Size = new Size(350, 118), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            rbModoEscala = new RadioButton { Text = "Venta por Escala / Rango de Cantidades", Location = new Point(12, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Label lblDescEsc = new Label { Text = "El precio cambia según la cantidad comprada.", Location = new Point(32, 32), Size = new Size(300, 16), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };
            Label lblTitCantTr = new Label { Text = "Selecciona la cantidad de tramos:", Location = new Point(12, 54), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            cbCantidadTramos = new ComboBox { Location = new Point(12, 72), Size = new Size(320, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5F), Enabled = false };
            cbCantidadTramos.Items.AddRange(new object[] { "2 tramos", "3 tramos", "4 tramos", "5 tramos" });
            cbCantidadTramos.SelectedIndex = 0;
            cbCantidadTramos.SelectedIndexChanged += (s, e) => RegenerarFilasTramos(cbCantidadTramos.SelectedIndex + 2);
            pnlCardEscala.Controls.AddRange(new Control[] { rbModoEscala, lblDescEsc, lblTitCantTr, cbCantidadTramos });

            pnlCardModalidad.Controls.AddRange(new Control[] { lblTitMod, pnlCardListaFija, pnlCardEscala });

            rbModoListaFija.Click += (s, e) => SeleccionarModo(escala: false);
            rbModoEscala.Click += (s, e) => SeleccionarModo(escala: true);
            pnlCardListaFija.Click += (s, e) => SeleccionarModo(escala: false);
            pnlCardEscala.Click += (s, e) => SeleccionarModo(escala: true);

            // Tarjeta de Tramos
            pnlSeccionTramos = CrearCardPanel(anchoSeccion, 205);
            Label lblTitSecTr = new Label { Text = "📊 Configurar Tramos de Cantidad y Lista de Precios", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(14, 10), AutoSize = true };

            Panel pnlHeaderGrid = new Panel { Location = new Point(14, 32), Size = new Size(710, 24), BackColor = Color.Transparent };
            pnlHeaderGrid.Controls.Add(new Label { Text = "Tramo", Location = new Point(4, 4), Size = new Size(45, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) });

            lblHeaderDesde = new Label { Text = "Desde (unidades)", Location = new Point(70, 4), Size = new Size(130, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            lblHeaderHasta = new Label { Text = "Hasta (unidades)", Location = new Point(230, 4), Size = new Size(130, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };

            pnlHeaderGrid.Controls.Add(lblHeaderDesde);
            pnlHeaderGrid.Controls.Add(lblHeaderHasta);
            pnlHeaderGrid.Controls.Add(new Label { Text = "Lista de Precios", Location = new Point(370, 4), Size = new Size(200, 18), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) });

            flpFilasTramos = new FlowLayoutPanel
            {
                Location = new Point(14, 58),
                Size = new Size(710, 138),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            pnlSeccionTramos.Controls.AddRange(new Control[] { lblTitSecTr, pnlHeaderGrid, flpFilasTramos });

            // =========================================================================
            // Fila Precio de Venta Unitario
            // =========================================================================
            Panel pnlFilaPrecioVenta = new Panel { Size = new Size(anchoSeccion, 56) };
            txtPrecioUnitario = new TextBox { Width = 300, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Text = "0" };
            txtPrecioUnitario.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtPrecioUnitario.TextChanged += (s, e) => MonedaHelper.AplicarMascaraEnVivo(txtPrecioUnitario);

            var campoPrecio = CrearCampoConLabel("💲 Precio Venta Base POS (IVA Inc.)", txtPrecioUnitario, 0, 300);
            lblTitPrecioVenta = campoPrecio.LabelControl;
            pnlFilaPrecioVenta.Controls.Add(campoPrecio.Contenedor);

            // =========================================================================
            // Tarjeta de Inventario: Stock Físico, % Merma, Stock Disponible y Stock Mínimo
            // =========================================================================
            Panel pnlCardInventario = CrearCardPanel(anchoSeccion, 78);

            txtStockActual = new TextBox { Width = 150, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Text = "0" };
            txtStockActual.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
                if (chkEsPesable.Checked && (e.KeyChar == '.' || e.KeyChar == ',') && 
                    !txtStockActual.Text.Contains('.') && !txtStockActual.Text.Contains(','))
                {
                    return;
                }
                e.Handled = true;
            };
            txtStockActual.TextChanged += (s, e) =>
            {
                if (!chkEsPesable.Checked) MonedaHelper.AplicarMascaraEnVivo(txtStockActual);
                CalcularStockDisponibleEnVivo();
            };

            txtPorcentajeMerma = new TextBox { Width = 110, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Text = "0" };
            txtPorcentajeMerma.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
                if ((e.KeyChar == '.' || e.KeyChar == ',') && !txtPorcentajeMerma.Text.Contains('.') && !txtPorcentajeMerma.Text.Contains(','))
                {
                    return;
                }
                e.Handled = true;
            };
            txtPorcentajeMerma.TextChanged += (s, e) => CalcularStockDisponibleEnVivo();

            txtStockDisponible = new TextBox 
            { 
                Width = 150, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                Text = "0", 
                ReadOnly = true, 
                BackColor = Color.FromArgb(240, 253, 244), 
                ForeColor = Color.FromArgb(22, 101, 52) 
            };

            txtStockMinimo = new TextBox { Width = 150, Font = new Font("Segoe UI", 10F), Text = "5" };
            txtStockMinimo.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtStockMinimo.TextChanged += (s, e) => MonedaHelper.AplicarMascaraEnVivo(txtStockMinimo);

            var cStockFisico = CrearCampoConLabel("📦 Stock General (Físico)", txtStockActual, 14, 160);
            lblTitStockActual = cStockFisico.LabelControl;

            var cMerma = CrearCampo("📉 % Merma", txtPorcentajeMerma, 190, 120);

            var cStockDisp = CrearCampoConLabel("🟢 Stock Disponible (Venta)", txtStockDisponible, 330, 160);
            lblTitStockDisponible = cStockDisp.LabelControl;

            var cStockMin = CrearCampo("🔔 Stock Mínimo Alerta", txtStockMinimo, 510, 160);

            pnlCardInventario.Controls.AddRange(new Control[] { cStockFisico.Contenedor, cMerma, cStockDisp.Contenedor, cStockMin });

            // Tarjeta Foto
            Panel pnlCardFoto = CrearCardPanel(anchoSeccion, 74);
            Label lblTitFoto = new Label { Text = "🖼️ Foto del Producto", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(14, 6), AutoSize = true };
            pbFoto = new PictureBox { Location = new Point(14, 24), Size = new Size(42, 42), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(248, 250, 252) };

            Button btnCargarFoto = new Button { Text = "📤 Archivo", Location = new Point(62, 28), Size = new Size(80, 32), BackColor = Color.FromArgb(239, 246, 255), ForeColor = Color.FromArgb(29, 78, 216), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCargarFoto.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnCargarFoto.Click += BtnCargarFoto_Click;

            Button btnBuscarWeb = new Button { Text = "🛒 Jumbo", Location = new Point(146, 28), Size = new Size(85, 32), BackColor = Color.FromArgb(240, 253, 244), ForeColor = Color.FromArgb(22, 101, 52), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscarWeb.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnBuscarWeb.Click += BtnBuscarWeb_Click;

            Button btnBuscarGoogle = new Button 
            { 
                Text = "🔍 Buscar Imagen", 
                Location = new Point(235, 28), 
                Size = new Size(130, 32), 
                BackColor = Color.FromArgb(254, 243, 199), 
                ForeColor = Color.FromArgb(180, 83, 9), 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            btnBuscarGoogle.FlatAppearance.BorderColor = Color.FromArgb(253, 230, 138);
            btnBuscarGoogle.Click += BtnBuscarGoogle_Click;

            Button btnQuitarFoto = new Button { Text = "🗑️ Quitar", Location = new Point(369, 28), Size = new Size(70, 32), BackColor = Color.FromArgb(254, 242, 242), ForeColor = Color.FromArgb(220, 38, 38), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnQuitarFoto.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnQuitarFoto.Click += (s, e) => { _rutaImagenSeleccionada = string.Empty; pbFoto.Image?.Dispose(); pbFoto.Image = null; };

            Label lblFormatos = new Label { Text = "JPG, PNG, WEBP.", Location = new Point(445, 36), AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(148, 163, 184) };
            pnlCardFoto.Controls.AddRange(new Control[] { lblTitFoto, pbFoto, btnCargarFoto, btnBuscarWeb, btnBuscarGoogle, btnQuitarFoto, lblFormatos });

            // Agregar secciones al flow
            flow.Controls.AddRange(new Control[] { 
                pnlFila1, 
                pnlFila2, 
                pnlCardPesable,
                pnlCardCosto, 
                pnlCardModalidad, 
                pnlSeccionTramos, 
                pnlFilaPrecioVenta,
                pnlCardInventario,
                pnlCardFoto 
            });

            // Botones de acción inferiores
            Panel pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0) };
            Button btnGuardar = new Button
            {
                Text = "💾 Guardar Cambios",
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(575, 4)
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
                Location = new Point(455, 4)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            this.CancelButton = btnCancelar;
            pnlBotones.Controls.AddRange(new Control[] { btnGuardar, btnCancelar });

            pnlBorde.Controls.Add(flow);
            pnlBorde.Controls.Add(pnlBotones);
            flow.BringToFront();

            this.Controls.Add(pnlBorde);

            RegenerarFilasTramos(2);
            SeleccionarModo(escala: false);
            ActualizarEtiquetasSegunModalidadPeso();
        }

        private void CalcularStockDisponibleEnVivo()
        {
            if (_cargandoDatos) return;

            decimal stockFisico = 0m;
            if (chkEsPesable != null && chkEsPesable.Checked)
            {
                string txt = txtStockActual.Text.Trim().Replace(",", ".");
                decimal.TryParse(txt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out stockFisico);
            }
            else
            {
                stockFisico = MonedaHelper.Limpiar(txtStockActual.Text);
            }

            string mermaTxt = txtPorcentajeMerma.Text.Trim().Replace(",", ".");
            decimal.TryParse(mermaTxt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pctMerma);

            pctMerma = Math.Max(0m, Math.Min(100m, pctMerma));

            decimal disponible = stockFisico * (1.0m - (pctMerma / 100.0m));
            if (disponible < 0m) disponible = 0m;

            if (chkEsPesable != null && chkEsPesable.Checked)
            {
                txtStockDisponible.Text = disponible.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                txtStockDisponible.Text = MonedaHelper.Formatear(Math.Floor(disponible));
            }
        }

        private void ActualizarEtiquetasSegunModalidadPeso()
        {
            bool esPesable = chkEsPesable.Checked;

            if (esPesable)
            {
                lblTitCosto.Text = "Costo Neto por Kg ($)";
                lblSubCosto.Text = "Costo en pesos por kilogramo sin IVA.";
                lblTitPrecioVenta.Text = "💲 Precio Venta por Kg (IVA Inc.)";
                lblTitStockActual.Text = "Stock General (Kilos)";
                lblTitStockDisponible.Text = "🟢 Disponible (Kilos)";

                if (lblHeaderDesde != null) lblHeaderDesde.Text = "Desde (gramos)";
                if (lblHeaderHasta != null) lblHeaderHasta.Text = "Hasta (gramos)";
            }
            else
            {
                lblTitCosto.Text = "Costo Neto Base ($)";
                lblSubCosto.Text = "Costo en pesos por unidad sin IVA.";
                lblTitPrecioVenta.Text = "💲 Precio Venta Base POS (IVA Inc.)";
                lblTitStockActual.Text = "Stock General (Unidades)";
                lblTitStockDisponible.Text = "🟢 Disponible (Unidades)";

                if (lblHeaderDesde != null) lblHeaderDesde.Text = "Desde (unidades)";
                if (lblHeaderHasta != null) lblHeaderHasta.Text = "Hasta (unidades)";
            }

            CalcularStockDisponibleEnVivo();

            if (!_cargandoDatos && cbCantidadTramos != null)
            {
                int cant = cbCantidadTramos.SelectedIndex >= 0 ? cbCantidadTramos.SelectedIndex + 2 : 2;
                RegenerarFilasTramos(cant);
            }
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
            return CrearCampoConLabel(titulo, ctrl, x, w).Contenedor;
        }

        private (Panel Contenedor, Label LabelControl) CrearCampoConLabel(string titulo, Control ctrl, int x, int w)
        {
            Panel p = new Panel { Location = new Point(x, 0), Size = new Size(w, 52) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 20);
            p.Controls.AddRange(new Control[] { l, ctrl });
            return (p, l);
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

            bool esPesable = chkEsPesable != null && chkEsPesable.Checked;
            decimal pasoSugerido = esPesable ? 1000m : 5m;
            decimal ultimoHasta = pasoSugerido;

            for (int i = 1; i <= totalTramos; i++)
            {
                bool esPrimero = i == 1;
                bool esUltimo = i == totalTramos;

                var fila = new FilaTramoUI(i, esPrimero, esUltimo, ultimoHasta, esPesable);
                _filasTramos.Add(fila);
                flpFilasTramos.Controls.Add(fila.Contenedor);

                ultimoHasta = fila.ValorHasta + pasoSugerido;
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
                var categoriasBD = _configService.ObtenerCategoriasPOS();

                cbCategoria.Items.Clear();
                cbCategoria.Items.Add("Sin Asignar");

                foreach (var cat in categoriasBD)
                {
                    cbCategoria.Items.Add(cat.Nombre);
                }

                if (_productoAEditar == null)
                {
                    cbCategoria.SelectedItem = "Sin Asignar";
                    ActualizarFamiliasSegunCategoria();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar categorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarFamiliasSegunCategoria(string? familiaASeleccionar = null)
        {
            string catNombre = cbCategoria.SelectedItem?.ToString() ?? "";

            cbFamilia.Items.Clear();

            if (string.IsNullOrEmpty(catNombre) || catNombre == "Sin Asignar")
            {
                cbFamilia.Items.Add("General");
                cbFamilia.SelectedIndex = 0;
                cbFamilia.Enabled = false;
                btnNuevaFam.Enabled = false;
                return;
            }

            cbFamilia.Enabled = true;
            btnNuevaFam.Enabled = true;

            var categoriasBD = _configService.ObtenerCategoriasPOS();
            var cat = categoriasBD.FirstOrDefault(c => c.Nombre.Equals(catNombre, StringComparison.OrdinalIgnoreCase));

            cbFamilia.Items.Add("General");

            if (cat != null)
            {
                var familias = cat.Familias.Where(f => f.Activo).OrderBy(f => f.Orden).ToList();
                foreach (var fam in familias)
                {
                    if (!fam.Nombre.Equals("General", StringComparison.OrdinalIgnoreCase))
                    {
                        cbFamilia.Items.Add(fam.Nombre);
                    }
                }
            }

            if (!string.IsNullOrEmpty(familiaASeleccionar) && cbFamilia.Items.Contains(familiaASeleccionar))
            {
                cbFamilia.SelectedItem = familiaASeleccionar;
            }
            else
            {
                cbFamilia.SelectedIndex = 0;
            }
        }

        private void BtnNuevaCat_Click(object? sender, EventArgs e)
        {
            using var modal = new FormNuevaCategoriaModal(_configService);
            if (modal.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(modal.CategoriaCreada))
            {
                CargarListasDesplegables();
                cbCategoria.SelectedItem = modal.CategoriaCreada;
                ActualizarFamiliasSegunCategoria();
            }
        }

        private void BtnNuevaFam_Click(object? sender, EventArgs e)
        {
            string catNombre = cbCategoria.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(catNombre) || catNombre == "Sin Asignar")
            {
                MessageBox.Show("Debe seleccionar una categoría antes de agregar una subfamilia.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var categoriasBD = _configService.ObtenerCategoriasPOS();
            var cat = categoriasBD.FirstOrDefault(c => c.Nombre.Equals(catNombre, StringComparison.OrdinalIgnoreCase));
            if (cat == null) return;

            using var modal = new FormNuevaFamiliaModal(cat.Id, cat.Nombre, _configService);
            if (modal.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(modal.FamiliaCreada))
            {
                ActualizarFamiliasSegunCategoria(modal.FamiliaCreada);
            }
        }

        private void CargarDatosSiEsEdicion()
        {
            if (_productoAEditar != null)
            {
                _cargandoDatos = true;
                try
                {
                    chkEsPesable.Checked = _productoAEditar.EsPesable;
                    txtCodigoBarra.Text = _productoAEditar.CodigoBarra;
                    txtNombre.Text = _productoAEditar.Nombre;
                    txtNombreCorto.Text = _productoAEditar.NmbCorto ?? "";

                    string catExistente = !string.IsNullOrWhiteSpace(_productoAEditar.Categoria) ? _productoAEditar.Categoria : "Sin Asignar";
                    if (!cbCategoria.Items.Contains(catExistente))
                    {
                        cbCategoria.Items.Add(catExistente);
                    }
                    cbCategoria.SelectedItem = catExistente;

                    string famExistente = !string.IsNullOrWhiteSpace(_productoAEditar.NFamilia) ? _productoAEditar.NFamilia : "General";
                    ActualizarFamiliasSegunCategoria(famExistente);

                    decimal factor = _productoAEditar.EsPesable ? 1000m : 1m;
                    txtPrecioCosto.Text = MonedaHelper.Formatear(_productoAEditar.PrecioCosto * factor);
                    txtPrecioUnitario.Text = MonedaHelper.Formatear(_productoAEditar.PrecioUnitario * factor);

                    int listaIdx = Math.Max(1, Math.Min(10, _productoAEditar.ListaDefectoPOS)) - 1;
                    cbListaPrecioPOS.SelectedIndex = listaIdx;

                    if (_productoAEditar.EsPesable)
                    {
                        decimal kilos = _productoAEditar.Stock / 1000m;
                        txtStockActual.Text = kilos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        txtStockActual.Text = MonedaHelper.Formatear(_productoAEditar.Stock);
                    }

                    txtPorcentajeMerma.Text = _productoAEditar.PorcentajeMerma.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                    txtStockMinimo.Text = MonedaHelper.Formatear(_productoAEditar.StockMinimo);

                    _rutaImagenSeleccionada = _productoAEditar.ImagenPath ?? string.Empty;
                    if (!string.IsNullOrEmpty(_rutaImagenSeleccionada))
                    {
                        pbFoto.Image?.Dispose();
                        pbFoto.Image = ImagenHelper.CargarImagenSegura(_rutaImagenSeleccionada);
                    }

                    CargarEscalaDesdeBD(_productoAEditar.ProductoID);
                }
                finally
                {
                    _cargandoDatos = false;
                }

                CalcularStockDisponibleEnVivo();
            }
        }

        private void CargarEscalaDesdeBD(int productoId)
        {
            try
            {
                var reglas = _productoService.ObtenerReglasEscala(productoId);

                if (reglas.Count >= 2)
                {
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
            if (_cargandoDatos) return;

            decimal costoPantalla = MonedaHelper.Limpiar(txtPrecioCosto.Text);
            if (costoPantalla <= 0)
            {
                txtPrecioUnitario.Text = "0";
                return;
            }

            var margenes = ProductoService.ObtenerMargenesConfigurados();
            decimal margenL1 = (_productoAEditar != null && _productoAEditar.MargenGanancia > 0) 
                ? _productoAEditar.MargenGanancia 
                : (margenes.Length > 0 && margenes[0] > 0 ? margenes[0] : 30m);

            decimal precioVentaCalculado = ProductoService.CalcularPrecioVenta(costoPantalla, margenL1, redondear: true);
            txtPrecioUnitario.Text = MonedaHelper.Formatear(precioVentaCalculado);
        }

        private void BtnCargarFoto_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog { Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp" };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    string sku = txtCodigoBarra.Text.Trim();
                    string nombreRelativo = ImagenHelper.CopiarAImagenesYObtenerNombreRelativo(ofd.FileName, sku);

                    _rutaImagenSeleccionada = nombreRelativo;

                    pbFoto.Image?.Dispose();
                    pbFoto.Image = ImagenHelper.CargarImagenSegura(nombreRelativo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar imagen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnBuscarGoogle_Click(object? sender, EventArgs e)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("No se detecta conexión a internet para realizar la búsqueda en línea.", "Sin Conexión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sugerencia = txtNombre.Text.Trim();
            string sku = txtCodigoBarra.Text.Trim();

            using var modalBusqueda = new FormBuscarGoogleModal(sugerencia, sku);
            if (modalBusqueda.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(modalBusqueda.RutaImagenDescargada))
            {
                _rutaImagenSeleccionada = modalBusqueda.RutaImagenDescargada;

                pbFoto.Image?.Dispose();
                pbFoto.Image = ImagenHelper.CargarImagenSegura(_rutaImagenSeleccionada);
                pbFoto.Refresh();
            }
        }

        private void BtnBuscarWeb_Click(object? sender, EventArgs e)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("No se detecta conexión a internet para realizar la búsqueda en línea.", "Sin Conexión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sugerencia = txtNombre.Text.Trim();
            string sku = txtCodigoBarra.Text.Trim();

            using var modalBusqueda = new FormBuscarImagenModal(sugerencia, sku);
            if (modalBusqueda.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(modalBusqueda.RutaImagenDescargada))
            {
                _rutaImagenSeleccionada = modalBusqueda.RutaImagenDescargada;

                pbFoto.Image?.Dispose();
                pbFoto.Image = ImagenHelper.CargarImagenSegura(_rutaImagenSeleccionada);
                pbFoto.Refresh();
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

            decimal costoUI = MonedaHelper.Limpiar(txtPrecioCosto.Text);
            decimal pvpUI = MonedaHelper.Limpiar(txtPrecioUnitario.Text);
            
            bool esPesable = chkEsPesable.Checked;
            decimal divisor = esPesable ? 1000m : 1m;

            int stockFinal = 0;
            if (esPesable)
            {
                string stockTxt = txtStockActual.Text.Trim().Replace(",", ".");
                if (decimal.TryParse(stockTxt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal kilos))
                {
                    stockFinal = (int)Math.Round(kilos * 1000m);
                }
            }
            else
            {
                stockFinal = (int)MonedaHelper.Limpiar(txtStockActual.Text);
            }

            string mermaTxt = txtPorcentajeMerma.Text.Trim().Replace(",", ".");
            decimal.TryParse(mermaTxt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pctMerma);
            pctMerma = Math.Max(0m, Math.Min(100m, pctMerma));

            decimal stockMinDigito = MonedaHelper.Limpiar(txtStockMinimo.Text);
            int stockMin = stockMinDigito > 0 ? (int)stockMinDigito : 5;

            decimal costoBase = costoUI / divisor;
            decimal pvpBase = pvpUI / divisor;

            int listaSeleccionada = rbModoEscala.Checked ? 1 : (cbListaPrecioPOS.SelectedIndex + 1);

            try
            {
                var productoGuardar = _productoAEditar ?? new Producto();

                productoGuardar.EsPesable = esPesable;
                productoGuardar.CodigoBarra = string.IsNullOrWhiteSpace(txtCodigoBarra.Text) 
                    ? _productoService.ObtenerSiguienteCodigoBarraSugerido() 
                    : txtCodigoBarra.Text.Trim();
                productoGuardar.Nombre = nombre;
                productoGuardar.NmbCorto = txtNombreCorto.Text.Trim(); // 👈 Guarda el nombre corto

                string catSeleccionada = cbCategoria.SelectedItem?.ToString() ?? "Sin Asignar";
                string famSeleccionada = cbFamilia.SelectedItem?.ToString() ?? "General";

                productoGuardar.Categoria = catSeleccionada;
                productoGuardar.NFamilia = famSeleccionada;
                productoGuardar.ListaDefectoPOS = listaSeleccionada;

                if (productoGuardar.ProductoID == 0 || productoGuardar.OrdenPOS == 0)
                {
                    if (catSeleccionada == "Sin Asignar")
                    {
                        productoGuardar.OrdenPOS = 0;
                    }
                    else
                    {
                        using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
                        int maxOrden = db.Productos
                            .Where(p => p.Estado && p.Categoria == catSeleccionada)
                            .Max(p => (int?)p.OrdenPOS) ?? 0;

                        productoGuardar.OrdenPOS = maxOrden + 1;
                    }
                }
                
                productoGuardar.Stock = stockFinal;
                productoGuardar.StockMinimo = stockMin;
                productoGuardar.PorcentajeMerma = pctMerma; // 👈 Guarda el % de merma
                productoGuardar.ImagenPath = _rutaImagenSeleccionada;
                productoGuardar.Estado = true;
                productoGuardar.FchUpd = DateTime.Now;

                var margenesConfig = ProductoService.ObtenerMargenesConfigurados();
                productoGuardar.MargenGanancia = margenesConfig.Length > 0 ? margenesConfig[0] : 60m;

                if (costoBase > 0)
                {
                    ProductoService.AplicarNuevoCostoYRecalcularListas(productoGuardar, costoBase);
                }
                else
                {
                    productoGuardar.PrecioCosto = 0;
                }

                if (pvpBase > 0)
                {
                    productoGuardar.PrecioUnitario = pvpBase;
                }

                var tramos = _filasTramos.Select(f => new TramoEscalaDTO
                {
                    NumeroTramo = f.NumeroTramo,
                    Desde = f.ValorDesde,
                    Hasta = f.EsUltimo ? 999999.000m : f.ValorHasta,
                    NumeroLista = f.CbLista.SelectedIndex + 1
                }).ToList();

                ProductoResultado = _productoService.GuardarProducto(productoGuardar, tramos, rbModoEscala.Checked, cantidadFacturaInicial: 0);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class FilaTramoUI
        {
            public int NumeroTramo { get; }
            public Panel Contenedor { get; }
            public NumericUpDown? NumDesde { get; }
            public NumericUpDown? NumHasta { get; }
            public ComboBox CbLista { get; }
            public bool EsPrimero { get; }
            public bool EsUltimo { get; }
            private readonly bool _esPesable;

            public decimal ValorDesde => NumDesde?.Value ?? (EsPrimero ? 1m : 1m);
            public decimal ValorHasta => NumHasta?.Value ?? 999999m;

            public FilaTramoUI(int nroTramo, bool esPrimero, bool esUltimo, decimal sugeridoHasta, bool esPesable = false)
            {
                NumeroTramo = nroTramo;
                EsPrimero = esPrimero;
                EsUltimo = esUltimo;
                _esPesable = esPesable;

                Contenedor = new Panel { Size = new Size(680, 32), Margin = new Padding(0, 0, 0, 4) };

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

                decimal paso = _esPesable ? 1000m : 5m;
                decimal valorInicialDesde = esPrimero ? 1m : (sugeridoHasta - paso + 1m);

                NumDesde = new NumericUpDown
                {
                    Location = new Point(50, 4),
                    Size = new Size(110, 24),
                    Minimum = 1,
                    Maximum = 999999,
                    Value = Math.Max(1m, valorInicialDesde),
                    Enabled = false,
                    TextAlign = HorizontalAlignment.Center,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                Label lblFlecha = new Label { Text = "➔", Location = new Point(175, 6), Size = new Size(20, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184) };

                Control ctrlHasta;
                if (!esUltimo)
                {
                    NumHasta = new NumericUpDown
                    {
                        Location = new Point(210, 4),
                        Size = new Size(110, 24),
                        Minimum = 1,
                        Maximum = 999999,
                        Increment = _esPesable ? 50m : 1m,
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
                        Location = new Point(210, 4),
                        Size = new Size(110, 24),
                        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(71, 85, 105),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.FromArgb(248, 250, 252)
                    };
                }

                CbLista = new ComboBox
                {
                    Location = new Point(340, 4),
                    Size = new Size(280, 24),
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
                        decimal salto = _esPesable ? 500m : 4m;
                        NumHasta.Value = nuevoDesde + salto;
                    }
                }
            }
        }
    }
}