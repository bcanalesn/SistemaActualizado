using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;
using SISTEMAACTUALIZADO.Helpers;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormConfiguracion : Form
    {
        private readonly ConfiguracionService _configService = new ConfiguracionService();
        private readonly DeliveryService _deliveryService = new DeliveryService();

        // --- Controles Pestaña Empresa ---
        private TextBox txtRut = null!;
        private TextBox txtRazonSocial = null!;
        private TextBox txtGiro = null!;
        private TextBox txtDireccion = null!;
        private TextBox txtComuna = null!;
        private TextBox txtCiudad = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtEmail = null!;
        private TextBox txtUnidadSII = null!;
        private TextBox txtResolucionSII = null!;
        private TextBox txtTextoPie = null!;
        private Button btnGuardarEmpresa = null!;

        // --- Controles Pestaña Delivery ---
        private DataGridView dgvZonas = null!;
        private DataGridView dgvComunas = null!;
        private TextBox txtZonaNombre = null!;
        private NumericUpDown numCostoEnvio = null!;
        private NumericUpDown numPedidoMinimo = null!;
        private NumericUpDown numEnvioGratis = null!;
        private CheckBox chkZonaActiva = null!;
        private Button btnGuardarZona = null!;
        private Button btnNuevaZona = null!;

        private TextBox txtComunaNombre = null!;
        private NumericUpDown numTiempoMinutos = null!;
        private CheckBox chkComunaActiva = null!;
        private Button btnGuardarComuna = null!;
        private Button btnNuevaComuna = null!;

        private int _zonaSeleccionadaId = 0;
        private int _comunaSeleccionadaId = 0;

        // --- Controles Pestaña Fidelización ---
        private CheckBox chkProgramaPuntosActivo = null!;
        private NumericUpDown numMontoPorPunto = null!;
        private NumericUpDown numPuntosEntregados = null!;
        private NumericUpDown numMontoMinimoAcumular = null!;
        private NumericUpDown numValorPesoPorPunto = null!;
        private Label lblEjemploAcumulacion = null!;
        private Label lblEjemploCanje = null!;
        private Button btnGuardarPuntosConfig = null!;

        // --- Controles Pestaña Espejo Catálogo POS ---
        private RadioButton rbOrdenCategoria = null!;
        private RadioButton rbOrdenPersonalizado = null!;
        private RadioButton rbOrdenMasVendidos = null!;
        private Panel pnlSeccionCategorias = null!;
        private FlowLayoutPanel flowConfigCategorias = null!;
        private FlowLayoutPanel flowConfigFamilias = null!;
        private FlowLayoutPanel flowLienzoProductos = null!;
        private FlowLayoutPanel flowAlmacenProductos = null!;
        private TextBox txtBuscarAlmacen = null!;
        private RadioButton rbFiltroSinAsignar = null!;
        private RadioButton rbFiltroTodos = null!;
        private Button btnGuardarCatalogoConfig = null!;
        private Point _dragStartPoint = Point.Empty;
        private bool _isDragging = false;

        private string _catEdicionActiva = "";
        private string _famEdicionActiva = "";
        private List<PosCategoria> _categoriasCache = new List<PosCategoria>();

        public FormConfiguracion()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("es-CL");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-CL");

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();

            CargarDatosEmpresa();
            CargarZonasDelivery();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuración General del Negocio y Datos Tributarios";
            this.Size = new Size(1160, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "⚙️ PANEL DE CONFIGURACIÓN DEL SISTEMA",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 16)
            };
            pnlHeader.Controls.Add(lblTitulo);

            TabControl tabPrincipal = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Point(14, 8)
            };

            TabPage tabEmpresa = new TabPage("🏢 Datos de Empresa");
            tabEmpresa.BackColor = Color.FromArgb(248, 250, 252);
            ConstruirPestañaEmpresa(tabEmpresa);

            TabPage tabDelivery = new TabPage("🚚 Zonas y Cobertura Delivery");
            tabDelivery.BackColor = Color.FromArgb(248, 250, 252);
            ConstruirPestañaDelivery(tabDelivery);

            TabPage tabFidelizacion = new TabPage("🎁 Puntos y Fidelización");
            tabFidelizacion.BackColor = Color.FromArgb(248, 250, 252);
            ConstruirPestañaFidelizacion(tabFidelizacion);

            TabPage tabCatalogo = new TabPage("🎨 Orden Catálogo POS");
            tabCatalogo.BackColor = Color.FromArgb(248, 250, 252);
            ConstruirPestañaCatalogoPOS(tabCatalogo);

            tabPrincipal.TabPages.Add(tabEmpresa);
            tabPrincipal.TabPages.Add(tabDelivery);
            tabPrincipal.TabPages.Add(tabFidelizacion);
            tabPrincipal.TabPages.Add(tabCatalogo);

            this.Controls.Add(tabPrincipal);
            this.Controls.Add(pnlHeader);
        }

        // ==========================================
        // 1. PESTAÑA: DATOS DE EMPRESA
        // ==========================================
        private void ConstruirPestañaEmpresa(TabPage tab)
        {
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                AutoScroll = true
            };

            int y = 15;
            CrearCampoEmpresa(pnlBody, "RUT Emisor:", ref txtRut, ref y, "76.543.210-K");
            CrearCampoEmpresa(pnlBody, "Razón Social / Comercial:", ref txtRazonSocial, ref y, "Mi Negocio SpA");
            CrearCampoEmpresa(pnlBody, "Giro Comercial:", ref txtGiro, ref y, "Venta al por menor");
            CrearCampoEmpresa(pnlBody, "Dirección (Casa Matriz):", ref txtDireccion, ref y, "Av. Siempre Viva 123");
            CrearCampoEmpresa(pnlBody, "Comuna:", ref txtComuna, ref y, "Santiago");
            CrearCampoEmpresa(pnlBody, "Ciudad:", ref txtCiudad, ref y, "Santiago");
            CrearCampoEmpresa(pnlBody, "Teléfono de Contacto:", ref txtTelefono, ref y, "+56 9 1234 5678");
            CrearCampoEmpresa(pnlBody, "Correo Electrónico:", ref txtEmail, ref y, "contacto@empresa.cl");
            CrearCampoEmpresa(pnlBody, "Unidad S.I.I.:", ref txtUnidadSII, ref y, "S.I.I. - SANTIAGO CENTRO");
            CrearCampoEmpresa(pnlBody, "Resolución S.I.I.:", ref txtResolucionSII, ref y, "Res. 99 de 2026");
            CrearCampoEmpresa(pnlBody, "Mensaje al Pie:", ref txtTextoPie, ref y, "¡Gracias por su preferencia!");

            btnGuardarEmpresa = new Button
            {
                Text = "💾 Guardar Parámetros de Empresa",
                Location = new Point(220, y + 10),
                Size = new Size(380, 40),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarEmpresa.FlatAppearance.BorderSize = 0;
            btnGuardarEmpresa.Click += BtnGuardarEmpresa_Click;
            pnlBody.Controls.Add(btnGuardarEmpresa);

            tab.Controls.Add(pnlBody);
        }

        private void CrearCampoEmpresa(Panel pnl, string labelText, ref TextBox txt, ref int y, string placeholder)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(15, y + 3),
                Size = new Size(190, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txt = new TextBox
            {
                Location = new Point(220, y),
                Size = new Size(380, 26),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = placeholder
            };

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            y += 35;
        }

        // ==========================================
        // 2. PESTAÑA: GESTIÓN DE DELIVERY
        // ==========================================
        private void ConstruirPestañaDelivery(TabPage tab)
        {
            TableLayoutPanel tlpLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            tlpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            tlpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));

            GroupBox gbZonas = new GroupBox
            {
                Text = "📍 Macro Zonas de Reparto",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 6, 0)
            };

            dgvZonas = CrearGridBasico();
            dgvZonas.Dock = DockStyle.Top;
            dgvZonas.Height = 220;
            dgvZonas.Columns.Add("Id", "ID");
            dgvZonas.Columns["Id"]!.Visible = false;
            dgvZonas.Columns.Add("Nombre", "Zona");
            dgvZonas.Columns.Add("Costo", "Flete ($)");
            dgvZonas.Columns.Add("Minimo", "Mínimo ($)");
            dgvZonas.Columns.Add("Gratis", "Gratis Desde ($)");
            dgvZonas.Columns.Add("Activo", "Activo");

            dgvZonas.Columns["Nombre"]!.FillWeight = 26;
            dgvZonas.Columns["Costo"]!.FillWeight = 20;
            dgvZonas.Columns["Minimo"]!.FillWeight = 20;
            dgvZonas.Columns["Gratis"]!.FillWeight = 22;
            dgvZonas.Columns["Activo"]!.FillWeight = 12;

            dgvZonas.SelectionChanged += DgvZonas_SelectionChanged;

            Panel pnlEditorZona = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };
            int yz = 12;

            pnlEditorZona.Controls.Add(new Label { Text = "Nombre Zona:", Location = new Point(6, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) });
            txtZonaNombre = new TextBox { Location = new Point(140, yz), Size = new Size(220, 24), Font = new Font("Segoe UI", 9F) };
            pnlEditorZona.Controls.Add(txtZonaNombre);
            yz += 34;

            pnlEditorZona.Controls.Add(new Label { Text = "Costo Envío ($):", Location = new Point(6, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) });
            numCostoEnvio = new NumericUpDown { Location = new Point(140, yz), Size = new Size(130, 24), Maximum = 100000, Increment = 500, ThousandsSeparator = true };
            pnlEditorZona.Controls.Add(numCostoEnvio);
            yz += 34;

            pnlEditorZona.Controls.Add(new Label { Text = "Pedido Mínimo ($):", Location = new Point(6, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) });
            numPedidoMinimo = new NumericUpDown { Location = new Point(140, yz), Size = new Size(130, 24), Maximum = 500000, Increment = 1000, ThousandsSeparator = true };
            pnlEditorZona.Controls.Add(numPedidoMinimo);
            yz += 34;

            pnlEditorZona.Controls.Add(new Label { Text = "Envío Gratis Desde:", Location = new Point(6, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) });
            numEnvioGratis = new NumericUpDown { Location = new Point(140, yz), Size = new Size(130, 24), Maximum = 500000, Increment = 1000, ThousandsSeparator = true };
            pnlEditorZona.Controls.Add(numEnvioGratis);
            yz += 34;

            chkZonaActiva = new CheckBox { Text = "Zona Habilitada", Location = new Point(140, yz), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            pnlEditorZona.Controls.Add(chkZonaActiva);
            yz += 36;

            btnGuardarZona = new Button
            {
                Text = "💾 Guardar Zona",
                Location = new Point(140, yz),
                Size = new Size(135, 34),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarZona.FlatAppearance.BorderSize = 0;
            btnGuardarZona.Click += BtnGuardarZona_Click;

            btnNuevaZona = new Button
            {
                Text = "➕ Nueva Zona",
                Location = new Point(285, yz),
                Size = new Size(125, 34),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevaZona.FlatAppearance.BorderSize = 0;
            btnNuevaZona.Click += (s, e) => LimpiarFormularioZona();

            pnlEditorZona.Controls.Add(btnGuardarZona);
            pnlEditorZona.Controls.Add(btnNuevaZona);

            gbZonas.Controls.Add(pnlEditorZona);
            gbZonas.Controls.Add(dgvZonas);
            tlpLayout.Controls.Add(gbZonas, 0, 0);

            GroupBox gbComunas = new GroupBox
            {
                Text = "🏘️ Comunas Asignadas a la Zona",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10),
                Margin = new Padding(6, 0, 0, 0)
            };

            dgvComunas = CrearGridBasico();
            dgvComunas.Dock = DockStyle.Top;
            dgvComunas.Height = 220;
            dgvComunas.Columns.Add("Id", "ID");
            dgvComunas.Columns["Id"]!.Visible = false;
            dgvComunas.Columns.Add("Nombre", "Comuna");
            dgvComunas.Columns.Add("Minutos", "T. Estimado (Min)");
            dgvComunas.Columns.Add("Activo", "Activo");

            dgvComunas.Columns["Nombre"]!.FillWeight = 48;
            dgvComunas.Columns["Minutos"]!.FillWeight = 32;
            dgvComunas.Columns["Activo"]!.FillWeight = 20;

            dgvComunas.SelectionChanged += DgvComunas_SelectionChanged;

            Panel pnlEditorComuna = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };
            int yc = 12;

            pnlEditorComuna.Controls.Add(new Label { Text = "Nombre Comuna:", Location = new Point(6, yc + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) });
            txtComunaNombre = new TextBox { Location = new Point(145, yc), Size = new Size(200, 24), Font = new Font("Segoe UI", 9F) };
            pnlEditorComuna.Controls.Add(txtComunaNombre);
            yc += 34;

            pnlEditorComuna.Controls.Add(new Label { Text = "Tiempo Est. (min):", Location = new Point(6, yc + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) });
            numTiempoMinutos = new NumericUpDown { Location = new Point(145, yc), Size = new Size(90, 24), Minimum = 10, Maximum = 180, Value = 45 };
            pnlEditorComuna.Controls.Add(numTiempoMinutos);
            yc += 34;

            chkComunaActiva = new CheckBox { Text = "Comuna Habilitada", Location = new Point(145, yc), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            pnlEditorComuna.Controls.Add(chkComunaActiva);
            yc += 36;

            btnGuardarComuna = new Button
            {
                Text = "💾 Guardar Comuna",
                Location = new Point(145, yc),
                Size = new Size(140, 34),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarComuna.FlatAppearance.BorderSize = 0;
            btnGuardarComuna.Click += BtnGuardarComuna_Click;

            btnNuevaComuna = new Button
            {
                Text = "➕ Nueva Comuna",
                Location = new Point(295, yc),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevaComuna.FlatAppearance.BorderSize = 0;
            btnNuevaComuna.Click += (s, e) => LimpiarFormularioComuna();

            pnlEditorComuna.Controls.Add(btnGuardarComuna);
            pnlEditorComuna.Controls.Add(btnNuevaComuna);

            gbComunas.Controls.Add(pnlEditorComuna);
            gbComunas.Controls.Add(dgvComunas);
            tlpLayout.Controls.Add(gbComunas, 1, 0);

            tab.Controls.Add(tlpLayout);
        }

        // ==========================================
        // 3. PESTAÑA: PUNTOS Y FIDELIZACIÓN
        // ==========================================
        private void ConstruirPestañaFidelizacion(TabPage tab)
        {
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                AutoScroll = true
            };

            Panel pnlSwitch = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(650, 50),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            chkProgramaPuntosActivo = new CheckBox
            {
                Text = "⚡ Habilitar Programa de Fidelización (Puntos en Ventas y Clientes)",
                Location = new Point(16, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 58, 237),
                Checked = true
            };
            pnlSwitch.Controls.Add(chkProgramaPuntosActivo);
            pnlBody.Controls.Add(pnlSwitch);

            GroupBox gbAcumulacion = new GroupBox
            {
                Text = "📈 Reglas de Acumulación (Ganar Puntos)",
                Location = new Point(20, 80),
                Size = new Size(650, 165),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15)
            };

            Label lblMontoBase = new Label { Text = "Monto de compra para ganar puntos ($):", Location = new Point(20, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            numMontoPorPunto = new NumericUpDown { Location = new Point(340, 30), Size = new Size(130, 24), Minimum = 100, Maximum = 1000000, Increment = 500, Value = 1000, ThousandsSeparator = true };

            Label lblPuntosBase = new Label { Text = "Puntos a entregar por cada bloque:", Location = new Point(20, 68), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            numPuntosEntregados = new NumericUpDown { Location = new Point(340, 66), Size = new Size(130, 24), Minimum = 1, Maximum = 1000, Value = 1 };

            Label lblMinimo = new Label { Text = "Compra mínima para acumular ($):", Location = new Point(20, 104), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            numMontoMinimoAcumular = new NumericUpDown { Location = new Point(340, 102), Size = new Size(130, 24), Minimum = 0, Maximum = 1000000, Increment = 500, Value = 1000, ThousandsSeparator = true };

            lblEjemploAcumulacion = new Label
            {
                Text = "💡 Ejemplo: Por una compra de $ 10.000 el cliente acumulará 10 puntos.",
                Location = new Point(20, 136),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(22, 163, 74)
            };

            gbAcumulacion.Controls.AddRange(new Control[] { lblMontoBase, numMontoPorPunto, lblPuntosBase, numPuntosEntregados, lblMinimo, numMontoMinimoAcumular, lblEjemploAcumulacion });
            pnlBody.Controls.Add(gbAcumulacion);

            GroupBox gbCanje = new GroupBox
            {
                Text = "🎁 Reglas de Canje (Descuentos en Pesos)",
                Location = new Point(20, 260),
                Size = new Size(650, 130),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15)
            };

            Label lblValorPunto = new Label { Text = "Valor en pesos de cada punto ($):", Location = new Point(20, 36), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            numValorPesoPorPunto = new NumericUpDown { Location = new Point(340, 34), Size = new Size(130, 24), Minimum = 1, Maximum = 10000, Increment = 5, Value = 10, DecimalPlaces = 0, ThousandsSeparator = true };

            lblEjemploCanje = new Label
            {
                Text = "💡 Ejemplo: Si el cliente canjea 50 puntos, obtendrá -$ 500 de descuento.",
                Location = new Point(20, 80),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(124, 58, 237)
            };

            gbCanje.Controls.AddRange(new Control[] { lblValorPunto, numValorPesoPorPunto, lblEjemploCanje });
            pnlBody.Controls.Add(gbCanje);

            btnGuardarPuntosConfig = new Button
            {
                Text = "💾 Guardar Parámetros de Fidelización",
                Location = new Point(20, 410),
                Size = new Size(340, 42),
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarPuntosConfig.FlatAppearance.BorderSize = 0;
            btnGuardarPuntosConfig.Click += BtnGuardarPuntosConfig_Click;
            pnlBody.Controls.Add(btnGuardarPuntosConfig);

            Action actualizarEjemplos = () =>
            {
                int montoBase = (int)numMontoPorPunto.Value;
                int pts = (int)numPuntosEntregados.Value;
                decimal valPunto = numValorPesoPorPunto.Value;

                int ejemploCompra = Math.Max(10000, montoBase * 5);
                int puntosGanados = montoBase > 0 ? (ejemploCompra / montoBase) * pts : 0;
                
                string montoCompraStr = MonedaHelper.Formatear(ejemploCompra, conSigno: true);
                string ptsGanadosStr = MonedaHelper.Formatear(puntosGanados);
                lblEjemploAcumulacion.Text = $"💡 Ejemplo: Por una compra de {montoCompraStr} el cliente acumulará {ptsGanadosStr} puntos.";

                int ejemploPtsCanje = 50;
                decimal dctoPesos = ejemploPtsCanje * valPunto;
                string dctoPesosStr = MonedaHelper.Formatear(dctoPesos, conSigno: true);
                lblEjemploCanje.Text = $"💡 Ejemplo: Si el cliente canjea {ejemploPtsCanje} puntos, obtendrá -{dctoPesosStr} de descuento.";
            };

            numMontoPorPunto.ValueChanged += (s, e) => actualizarEjemplos();
            numPuntosEntregados.ValueChanged += (s, e) => actualizarEjemplos();
            numValorPesoPorPunto.ValueChanged += (s, e) => actualizarEjemplos();

            tab.Controls.Add(pnlBody);
            CargarDatosFidelizacionUI();
        }

        // ==========================================
        // 4. PESTAÑA: PERSONALIZADOR ESPEJO DEL CATÁLOGO POS
        // ==========================================
        private void ConstruirPestañaCatalogoPOS(TabPage tab)
        {
            TableLayoutPanel tlpLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(244, 246, 249)
            };
            tlpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F)); // Lienzo Espejo
            tlpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Almacén

            // ----------------------------------------------------
            // 1. PANEL IZQUIERDO: LIENZO ESPEJO DEL PUNTO DE VENTA
            // ----------------------------------------------------
            TableLayoutPanel tlpLienzoWrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpLienzoWrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Fila 0: Selector Superior de Radios (Fijo 42px)
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            // Fila 1: Categorías (Auto-ajustable a las filas que necesite)
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Fila 2: Subfamilias (Auto-ajustable a las filas que necesite)
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Fila 3: Lienzo Blanco de Productos (Ocupa el resto)
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // Fila 4: Footer Guardar (Fijo 48px)
            tlpLienzoWrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            // Fila 0: Selector de Criterio Superior
            Panel pnlTopRadios = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, 8, 10, 6), Margin = new Padding(0) };
            
            string modoActual = _configService.ObtenerModoOrdenCatalogo();

            rbOrdenCategoria = new RadioButton { Text = "📂 Alfabético", Location = new Point(10, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Checked = (modoActual == "CATEGORIA") };
            rbOrdenPersonalizado = new RadioButton { Text = "⭐ Personalizado (Arrastrar)", Location = new Point(140, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Checked = (modoActual == "PERSONALIZADO") };
            rbOrdenMasVendidos = new RadioButton { Text = "🔥 Los Más Vendidos", Location = new Point(340, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Checked = (modoActual == "MAS_VENDIDOS") };

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

            // Fila 1: Categorías (FlowLayoutPanel sin scroll, crece solo)
            flowConfigCategorias = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoScroll = false, 
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 4),
                Padding = new Padding(0)
            };
            tlpLienzoWrapper.Controls.Add(flowConfigCategorias, 0, 1);

            // Fila 2: Familias (FlowLayoutPanel sin scroll, crece solo)
            flowConfigFamilias = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoScroll = false, 
                WrapContents = true,
                Margin = new Padding(0, 2, 0, 4),
                Padding = new Padding(0)
            };
            tlpLienzoWrapper.Controls.Add(flowConfigFamilias, 0, 2);

            // Fila 3: Lienzo de Productos
            flowLienzoProductos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(8),
                Margin = new Padding(0),
                AllowDrop = true
            };
            flowLienzoProductos.DragEnter += (s, e) => { if (e.Data!.GetDataPresent(typeof(Producto))) e.Effect = DragDropEffects.Move; };
            flowLienzoProductos.DragDrop += FlowLienzoProductos_DragDrop;
            tlpLienzoWrapper.Controls.Add(flowLienzoProductos, 0, 3);

            // Fila 4: Footer Guardar
            Panel pnlFooterLienzo = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0), Margin = new Padding(0) };
            btnGuardarCatalogoConfig = new Button
            {
                Text = "💾 Guardar Configuración de Catálogo",
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarCatalogoConfig.FlatAppearance.BorderSize = 0;
            btnGuardarCatalogoConfig.Click += BtnGuardarCatalogoConfig_Click;
            pnlFooterLienzo.Controls.Add(btnGuardarCatalogoConfig);
            tlpLienzoWrapper.Controls.Add(pnlFooterLienzo, 0, 4);

            // ----------------------------------------------------
            // 2. PANEL DERECHO: ALMACÉN DE PRODUCTOS (DEPÓSITO)
            // ----------------------------------------------------
            GroupBox gbAlmacen = new GroupBox
            {
                Text = "📦 Almacén de Productos (Arrastrar al Lienzo)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(8)
            };

            txtBuscarAlmacen = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), PlaceholderText = "🔍 Buscar producto en almacén..." };
            txtBuscarAlmacen.TextChanged += (s, e) => FiltrarAlmacen();

            Panel pnlFiltrosAlmacen = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(0, 4, 0, 0) };
            rbFiltroSinAsignar = new RadioButton { Text = "Sin Asignar", Location = new Point(4, 4), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8F) };
            rbFiltroTodos = new RadioButton { Text = "Todo el Catálogo", Location = new Point(120, 4), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            rbFiltroSinAsignar.CheckedChanged += (s, e) => FiltrarAlmacen();
            rbFiltroTodos.CheckedChanged += (s, e) => FiltrarAlmacen();
            pnlFiltrosAlmacen.Controls.AddRange(new Control[] { rbFiltroSinAsignar, rbFiltroTodos });

            flowAlmacenProductos = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, BackColor = Color.FromArgb(248, 250, 252) };

            gbAlmacen.Controls.Add(flowAlmacenProductos);
            gbAlmacen.Controls.Add(pnlFiltrosAlmacen);
            gbAlmacen.Controls.Add(txtBuscarAlmacen);

            // ----------------------------------------------------
            // 3. AGREGAR A LA VENTANA (EN EL ORDEN CORRECTO)
            // ----------------------------------------------------
            tlpLayout.Controls.Add(tlpLienzoWrapper, 0, 0);
            tlpLayout.Controls.Add(gbAlmacen, 1, 0);
            tab.Controls.Add(tlpLayout);

            CargarDatosEspejoConfig();
        }

        private void AjustarResponsiveCategorias()
        {
            
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

        // =========================================================================
        // 1. CATEGORÍAS: Mismo tamaño compacto de siempre (118 x 38)
        // =========================================================================
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

            // Botón ➕ Nueva Cat SIEMPRE al final
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

        private void IntercambiarPosicionCategoria(int actual, int destino)
        {
            var item = _categoriasCache[actual];
            _categoriasCache.RemoveAt(actual);
            _categoriasCache.Insert(destino, item);

            _configService.GuardarOrdenCategorias(_categoriasCache.Select(c => c.Id).ToList());
            RenderizarCategoriasConfig();
        }

        // Mini diálogo nativo para renombrar sin librerías externas
        private void PromptRenombrarCategoria(PosCategoria cat)
        {
            using Form prompt = new Form
            {
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Renombrar Categoría",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 15, Text = "Nuevo nombre:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 295, Text = cat.Nombre, Font = new Font("Segoe UI", 10F) };
            Button confirmation = new Button() { Text = "Aceptar", Left = 215, Width = 100, Top = 75, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(0, 102, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            confirmation.FlatAppearance.BorderSize = 0;

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nuevo = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != cat.Nombre)
                {
                    _configService.RenombrarCategoria(cat.Id, nuevo);
                    CargarDatosEspejoConfig();
                }
            }
        }

        // Mini diálogo nativo para crear categoría sin librerías externas
        private void PromptNuevaCategoria()
        {
            using Form prompt = new Form
            {
                Width = 380,
                Height = 230,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Nueva Categoría",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblNom = new Label { Left = 20, Top = 15, Text = "Nombre de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtNom = new TextBox { Left = 20, Top = 38, Width = 320, Font = new Font("Segoe UI", 9.5F) };

            Label lblColor = new Label { Left = 20, Top = 72, Text = "Color de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Button btnMuestra = new Button { Left = 20, Top = 95, Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            btnMuestra.FlatAppearance.BorderSize = 1;

            ComboBox cbColor = new ComboBox { Left = 50, Top = 95, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
foreach (var opc in CategoriaColorHelper.OpcionesPaleta) cbColor.Items.Add(opc.Nombre);

            // 1. Conectar el evento PRIMERO
            cbColor.SelectedIndexChanged += (s, e) =>
            {
                if (cbColor.SelectedIndex >= 0)
                {
                    var pal = CategoriaColorHelper.OpcionesPaleta[cbColor.SelectedIndex];
                    btnMuestra.BackColor = pal.Texto;
                    btnMuestra.FlatAppearance.BorderColor = pal.Borde;
                }
            };

            // 2. Asignar el índice DESPUÉS (esto dispara automáticamente el evento y pinta btnMuestra)
            cbColor.SelectedIndex = 0;

            Button btnCrear = new Button { Text = "Crear Categoría", Left = 190, Width = 150, Top = 140, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnCrear.FlatAppearance.BorderSize = 0;

            prompt.Controls.AddRange(new Control[] { lblNom, txtNom, lblColor, btnMuestra, cbColor, btnCrear });
            prompt.AcceptButton = btnCrear;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nombre = txtNom.Text.Trim();
                if (string.IsNullOrWhiteSpace(nombre)) return;

                var pal = CategoriaColorHelper.OpcionesPaleta[Math.Max(0, cbColor.SelectedIndex)];

                using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
                var nueva = new PosCategoria 
                { 
                    Nombre = nombre, 
                    Orden = _categoriasCache.Count + 1,
                    FondoHex = ColorTranslator.ToHtml(pal.Fondo),
                    TextoHex = ColorTranslator.ToHtml(pal.Texto),
                    BordeHex = ColorTranslator.ToHtml(pal.Borde)
                };
                db.PosCategorias.Add(nueva);
                db.SaveChanges();

                CargarDatosEspejoConfig();
            }
        }

        private void PromptEditarCategoria(PosCategoria cat)
        {
            using Form prompt = new Form
            {
                Width = 380,
                Height = 230,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Editar Categoría",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblNom = new Label { Left = 20, Top = 15, Text = "Nombre de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtNom = new TextBox { Left = 20, Top = 38, Width = 320, Text = cat.Nombre, Font = new Font("Segoe UI", 9.5F) };

            Label lblColor = new Label { Left = 20, Top = 72, Text = "Color de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Button btnMuestra = new Button { Left = 20, Top = 95, Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            btnMuestra.FlatAppearance.BorderSize = 1;

            ComboBox cbColor = new ComboBox { Left = 50, Top = 95, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
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

            // 1. Conectar el evento PRIMERO
            cbColor.SelectedIndexChanged += (s, e) =>
            {
                if (cbColor.SelectedIndex >= 0)
                {
                    var pal = CategoriaColorHelper.OpcionesPaleta[cbColor.SelectedIndex];
                    btnMuestra.BackColor = pal.Texto;
                    btnMuestra.FlatAppearance.BorderColor = pal.Borde;
                }
            };

            // 2. Asignar el índice DESPUÉS (dispara el evento y colorea btnMuestra al instante)
            cbColor.SelectedIndex = selectedIdx;

            Button btnGuardar = new Button { Text = "Guardar Cambios", Left = 190, Width = 150, Top = 140, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(0, 102, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnGuardar.FlatAppearance.BorderSize = 0;

            prompt.Controls.AddRange(new Control[] { lblNom, txtNom, lblColor, btnMuestra, cbColor, btnGuardar });
            prompt.AcceptButton = btnGuardar;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nuevoNombre = txtNom.Text.Trim();
                var pal = CategoriaColorHelper.OpcionesPaleta[Math.Max(0, cbColor.SelectedIndex)];

                // Actualizar nombre si cambió
                if (!string.IsNullOrWhiteSpace(nuevoNombre) && nuevoNombre != cat.Nombre)
                {
                    _configService.RenombrarCategoria(cat.Id, nuevoNombre);
                    _catEdicionActiva = nuevoNombre;
                }

                // Actualizar color en base de datos
                _configService.ActualizarColorCategoria(cat.Id, 
                    ColorTranslator.ToHtml(pal.Fondo), 
                    ColorTranslator.ToHtml(pal.Texto), 
                    ColorTranslator.ToHtml(pal.Borde));

                CargarDatosEspejoConfig();
            }
        }

        // =========================================================================
        // 2. FAMILIAS: Botones ajustados a la palabra con lápiz pegado (sin espacios)
        // =========================================================================
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

            // 2. Subfamilias agrupadas con su lápiz
            for (int i = 0; i < familias.Count; i++)
            {
                var fam = familias[i];
                bool estaSeleccionada = fam.Nombre.Equals(_famEdicionActiva, StringComparison.OrdinalIgnoreCase);

                Color backColor = estaSeleccionada ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249);
                Color foreColor = estaSeleccionada ? Color.White : Color.FromArgb(51, 65, 85);
                Color hoverColor = estaSeleccionada ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);

                Button btnFam = new Button
                {
                    Text = fam.Nombre,
                    Height = 28,
                    AutoSize = true,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0),
                    Padding = new Padding(6, 0, 6, 0),
                    Tag = fam
                };
                btnFam.FlatAppearance.BorderSize = 0;
                btnFam.FlatAppearance.MouseOverBackColor = hoverColor;
                btnFam.Click += (s, e) =>
                {
                    _famEdicionActiva = fam.Nombre;
                    RenderizarFamiliasConfig();
                    RefrescarLienzoYAlmacen();
                };

                if (esPersonalizado)
                {
                    // Mini grupo: botón de texto + lápiz siempre unidos en la misma fila
                    FlowLayoutPanel grupo = new FlowLayoutPanel
                    {
                        AutoSize = true,
                        Height = 28,
                        WrapContents = false,
                        FlowDirection = FlowDirection.LeftToRight,
                        Margin = new Padding(0, 0, 6, 4),
                        Padding = new Padding(0),
                        BackColor = Color.Transparent
                    };

                    Point famDragStart = Point.Empty;
                    btnFam.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) famDragStart = e.Location; };
                    btnFam.MouseMove += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left && famDragStart != Point.Empty)
                        {
                            if (Math.Abs(e.X - famDragStart.X) > 4 || Math.Abs(e.Y - famDragStart.Y) > 4)
                            {
                                famDragStart = Point.Empty;
                                grupo.DoDragDrop(fam, DragDropEffects.Move);
                            }
                        }
                    };

                    Button btnEditFam = new Button
                    {
                        Text = "✏️",
                        Height = 28,
                        Width = 22,
                        BackColor = backColor,
                        ForeColor = foreColor,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0),
                        Padding = new Padding(0)
                    };
                    btnEditFam.FlatAppearance.BorderSize = 0;
                    btnEditFam.FlatAppearance.MouseOverBackColor = hoverColor;
                    btnEditFam.Click += (s, e) => PromptRenombrarFamilia(fam);

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
                                        _configService.GuardarOrdenFamilias(familias.Select(x => x.Id).ToList());
                                        cat.Familias = familias;
                                        RenderizarFamiliasConfig();
                                    }
                                }
                            }
                        };
                    }

                    ConfigurarDropFam(grupo);
                    ConfigurarDropFam(btnFam);

                    grupo.Controls.Add(btnFam);
                    grupo.Controls.Add(btnEditFam);
                    flowConfigFamilias.Controls.Add(grupo);
                }
                else
                {
                    btnFam.Margin = new Padding(0, 0, 6, 4);
                    flowConfigFamilias.Controls.Add(btnFam);
                }
            }

            // 3. Botón ➕ Familia SIEMPRE visible al final
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

        private void PromptRenombrarFamilia(PosFamilia fam)
        {
            using Form prompt = new Form
            {
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Renombrar Subfamilia",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };
            Label textLabel = new Label() { Left = 20, Top = 15, Text = "Nuevo nombre para la familia:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 295, Text = fam.Nombre, Font = new Font("Segoe UI", 10F) };
            Button confirmation = new Button() { Text = "Aceptar", Left = 215, Width = 100, Top = 75, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(0, 102, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            confirmation.FlatAppearance.BorderSize = 0;

            prompt.Controls.AddRange(new Control[] { textLabel, textBox, confirmation });
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nuevo = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != fam.Nombre)
                {
                    _configService.RenombrarFamilia(fam.Id, nuevo);
                    CargarDatosEspejoConfig();
                }
            }
        }
        private void PromptNuevaFamilia(PosCategoria cat)
        {
            using Form prompt = new Form
            {
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"Nueva Familia en {cat.Nombre}",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };
            Label textLabel = new Label() { Left = 20, Top = 15, Text = "Nombre de la subfamilia:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 295, Font = new Font("Segoe UI", 10F) };
            Button confirmation = new Button() { Text = "Crear", Left = 215, Width = 100, Top = 75, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            confirmation.FlatAppearance.BorderSize = 0;

            prompt.Controls.AddRange(new Control[] { textLabel, textBox, confirmation });
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string nombre = textBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(nombre)) return;

                _configService.CrearFamilia(cat.Id, nombre);

                // Recargar categorías y familias desde la BD para reflejar la nueva
                _categoriasCache = _configService.ObtenerCategoriasPOS();
                _famEdicionActiva = nombre; // Deja seleccionada la nueva familia creada
                RenderizarFamiliasConfig();
                RefrescarLienzoYAlmacen();
            }
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

            using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
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

                // --- CONFIGURAR DRAG & DROP EN LA TARJETA Y TODOS SUS HIJOS ---
                if (esPersonalizado)
                {
                    Point cardDragStart = Point.Empty;
                    void AsignarEventosArrastre(Control c)
                    {
                        if (c is Button) return; // El botón [X] no arrastra

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

            using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
            var query = db.Productos.Where(p => p.Estado);

            if (soloSinAsignar)
            {
                // Un producto está "Sin Asignar" si no tiene categoría válida,
                // o si su categoría es "Sin Asignar" / "General", o si su OrdenPOS es 0
                query = query.Where(p => 
                    string.IsNullOrEmpty(p.Categoria) || 
                    p.Categoria == "Sin Asignar" || 
                    p.OrdenPOS == 0
                );
            }

            if (!string.IsNullOrEmpty(filtro))
            {
                query = query.Where(p => p.Nombre.ToLower().Contains(filtro) || p.CodigoBarra.Contains(filtro));
            }

            var lista = query.Take(50).ToList();

            flowAlmacenProductos.SuspendLayout();
            flowAlmacenProductos.Controls.Clear();

            foreach (var p in lista)
            {
                Panel item = new Panel 
                { 
                    Size = new Size(275, 42), 
                    BackColor = Color.White, 
                    Margin = new Padding(0, 0, 0, 4), 
                    Cursor = Cursors.Hand 
                };
                item.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, item.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

                Label lbl = new Label 
                { 
                    Text = p.Nombre, 
                    Location = new Point(6, 4), 
                    Size = new Size(180, 18), 
                    AutoSize = false, 
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold) 
                };

                string precioStr = p.EsPesable
                    ? $"{MonedaHelper.Formatear(p.PrecioUnitario * 1000m, conSigno: true)}/Kg"
                    : MonedaHelper.Formatear(p.PrecioUnitario, conSigno: true);

                Label lblSub = new Label 
                { 
                    Text = $"{p.Categoria} • {precioStr}", 
                    Location = new Point(6, 22), 
                    Size = new Size(200, 16), 
                    ForeColor = Color.FromArgb(100, 116, 139), 
                    Font = new Font("Segoe UI", 7F) 
                };

                Label lblDrag = new Label 
                { 
                    Text = "⋮⋮", 
                    Location = new Point(250, 10), 
                    Size = new Size(20, 20), 
                    ForeColor = Color.FromArgb(148, 163, 184), 
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold) 
                };

                Action dragAction = () => item.DoDragDrop(p, DragDropEffects.Move);
                item.MouseDown += (s, e) => dragAction();
                lbl.MouseDown += (s, e) => dragAction();
                lblSub.MouseDown += (s, e) => dragAction();
                lblDrag.MouseDown += (s, e) => dragAction();

                item.Controls.AddRange(new Control[] { lbl, lblSub, lblDrag });
                flowAlmacenProductos.Controls.Add(item);
            }

            flowAlmacenProductos.ResumeLayout();
        }

        private void FlowLienzoProductos_DragDrop(object? sender, DragEventArgs e)
        {
            if (rbOrdenPersonalizado == null || !rbOrdenPersonalizado.Checked) return;

            // Si se suelta en el fondo libre del lienzo, se añade al final de la fila
            if (e.Data!.GetDataPresent(typeof(Producto)))
            {
                var prod = (Producto)e.Data.GetData(typeof(Producto))!;
                int nuevoOrden = flowLienzoProductos.Controls.Count + 1;
                _configService.ReordenarEInsertarProducto(prod.ProductoID, _catEdicionActiva, _famEdicionActiva, nuevoOrden);

                RefrescarLienzoYAlmacen();
            }
        }

        private void BtnGuardarCatalogoConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                string modo = "CATEGORIA";
                if (rbOrdenPersonalizado.Checked) modo = "PERSONALIZADO";
                else if (rbOrdenMasVendidos.Checked) modo = "MAS_VENDIDOS";

                _configService.GuardarModoOrdenCatalogo(modo);
                _configService.GuardarOrdenCategorias(_categoriasCache.Select(c => c.Id).ToList());

                MessageBox.Show("Configuración del catálogo guardada exitosamente.\nLos cambios ya están activos en el Punto de Venta.", "Catálogo POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarDatosFidelizacionUI()
        {
            var cfg = FidelizacionConfig.ObtenerConfiguracionActual();
            chkProgramaPuntosActivo.Checked = cfg.Activo;
            numMontoPorPunto.Value = cfg.MontoBasePorPunto;
            numPuntosEntregados.Value = cfg.PuntosPorBloque;
            numMontoMinimoAcumular.Value = cfg.MontoMinimoAcumulacion;
            numValorPesoPorPunto.Value = cfg.ValorPesoPorPunto;
        }

        private void BtnGuardarPuntosConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                var cfg = new PuntosConfiguracion
                {
                    IdConfig = 1,
                    Activo = chkProgramaPuntosActivo.Checked,
                    MontoBasePorPunto = (int)numMontoPorPunto.Value,
                    PuntosPorBloque = (int)numPuntosEntregados.Value,
                    MontoMinimoAcumulacion = (int)numMontoMinimoAcumular.Value,
                    ValorPesoPorPunto = numValorPesoPorPunto.Value
                };

                FidelizacionConfig.GuardarConfiguracion(cfg, "admin");
                MessageBox.Show("Parámetros del programa de fidelización guardados exitosamente.", "Fidelización", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatosFidelizacionUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración de fidelización: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridView CrearGridBasico()
        {
            var grid = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 },
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.ColumnHeadersHeight = 34;

            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.GridColor = Color.FromArgb(226, 232, 240);

            return grid;
        }

        // ==========================================
        // 5. LÓGICA DE NEGOCIO Y PERSISTENCIA (EMPRESA Y DELIVERY)
        // ==========================================
        private void CargarDatosEmpresa()
        {
            try
            {
                var emp = _configService.ObtenerDatosEmpresa();
                txtRut.Text = emp.Rut;
                txtRazonSocial.Text = emp.RazonSocial;
                txtGiro.Text = emp.Giro;
                txtDireccion.Text = emp.Direccion;
                txtComuna.Text = emp.Comuna;
                txtCiudad.Text = emp.Ciudad;
                txtTelefono.Text = emp.Telefono;
                txtEmail.Text = emp.Email ?? "";
                txtUnidadSII.Text = emp.UnidadSII;
                txtResolucionSII.Text = emp.ResolucionSII;
                txtTextoPie.Text = emp.TextoPieTicket;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos de empresa: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardarEmpresa_Click(object? sender, EventArgs e)
        {
            try
            {
                var datos = new ConfiguracionEmpresa
                {
                    EmpresaID = 1,
                    Rut = txtRut.Text.Trim(),
                    RazonSocial = txtRazonSocial.Text.Trim(),
                    Giro = txtGiro.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    Comuna = txtComuna.Text.Trim(),
                    Ciudad = txtCiudad.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    UnidadSII = txtUnidadSII.Text.Trim(),
                    ResolucionSII = txtResolucionSII.Text.Trim(),
                    TextoPieTicket = txtTextoPie.Text.Trim()
                };

                _configService.GuardarDatosEmpresa(datos);
                MessageBox.Show("Datos fiscales guardados con éxito.", "Empresa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatosEmpresa();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar empresa: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarZonasDelivery()
        {
            try
            {
                dgvZonas.SelectionChanged -= DgvZonas_SelectionChanged;
                dgvZonas.Rows.Clear();
                var zonas = _deliveryService.ObtenerZonasActivas();

                foreach (var z in zonas)
                {
                    dgvZonas.Rows.Add(
                        z.ZonaDeliveryId, 
                        z.Nombre, 
                        MonedaHelper.Formatear(z.CostoEnvio, conSigno: true), 
                        MonedaHelper.Formatear(z.PedidoMinimo, conSigno: true), 
                        MonedaHelper.Formatear(z.MontoEnvioGratis, conSigno: true), 
                        z.Activo ? "Sí" : "No"
                    );
                }

                dgvZonas.SelectionChanged += DgvZonas_SelectionChanged;

                if (dgvZonas.Rows.Count > 0)
                {
                    dgvZonas.ClearSelection();
                    dgvZonas.Rows[0].Selected = true;
                    dgvZonas.CurrentCell = dgvZonas.Rows[0].Cells["Nombre"];
                    CargarDatosFilaZonaSeleccionada(dgvZonas.Rows[0]);
                }
                else
                {
                    LimpiarFormularioZona();
                    dgvComunas.Rows.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar zonas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvZonas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvZonas.CurrentRow == null) return;
            CargarDatosFilaZonaSeleccionada(dgvZonas.CurrentRow);
        }

        private void CargarDatosFilaZonaSeleccionada(DataGridViewRow fila)
        {
            if (fila.Cells["Id"].Value == null) return;

            _zonaSeleccionadaId = Convert.ToInt32(fila.Cells["Id"].Value);
            txtZonaNombre.Text = fila.Cells["Nombre"].Value?.ToString() ?? "";

            numCostoEnvio.Value = ParsearMoneda(fila.Cells["Costo"].Value);
            numPedidoMinimo.Value = ParsearMoneda(fila.Cells["Minimo"].Value);
            numEnvioGratis.Value = ParsearMoneda(fila.Cells["Gratis"].Value);
            chkZonaActiva.Checked = (fila.Cells["Activo"].Value?.ToString() == "Sí");

            CargarComunasDeZona(_zonaSeleccionadaId);
        }

        private static decimal ParsearMoneda(object? valor)
        {
            if (valor == null) return 0m;
            return MonedaHelper.Limpiar(valor.ToString());
        }

        private void CargarComunasDeZona(int zonaId)
        {
            try
            {
                dgvComunas.SelectionChanged -= DgvComunas_SelectionChanged;
                dgvComunas.Rows.Clear();
                LimpiarFormularioComuna();

                using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
                var comunas = db.ComunasDelivery
                    .Where(c => c.ZonaDeliveryId == zonaId)
                    .OrderBy(c => c.Nombre)
                    .ToList();

                foreach (var c in comunas)
                {
                    dgvComunas.Rows.Add(c.ComunaDeliveryId, c.Nombre, c.TiempoEstimadoMinutos, c.Activo ? "Sí" : "No");
                }

                dgvComunas.SelectionChanged += DgvComunas_SelectionChanged;

                if (dgvComunas.Rows.Count > 0)
                {
                    dgvComunas.ClearSelection();
                    dgvComunas.Rows[0].Selected = true;
                    dgvComunas.CurrentCell = dgvComunas.Rows[0].Cells["Nombre"];
                    CargarDatosFilaComunaSeleccionada(dgvComunas.Rows[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar comunas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvComunas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvComunas.CurrentRow == null) return;
            CargarDatosFilaComunaSeleccionada(dgvComunas.CurrentRow);
        }

        private void CargarDatosFilaComunaSeleccionada(DataGridViewRow fila)
        {
            if (fila.Cells["Id"].Value == null) return;

            _comunaSeleccionadaId = Convert.ToInt32(fila.Cells["Id"].Value);
            txtComunaNombre.Text = fila.Cells["Nombre"].Value?.ToString() ?? "";
            
            int minutos = Convert.ToInt32(fila.Cells["Minutos"].Value ?? 45);
            numTiempoMinutos.Value = Math.Min(numTiempoMinutos.Maximum, Math.Max(numTiempoMinutos.Minimum, minutos));
            chkComunaActiva.Checked = (fila.Cells["Activo"].Value?.ToString() == "Sí");
        }

        private void BtnGuardarZona_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtZonaNombre.Text))
            {
                MessageBox.Show("Ingrese el nombre de la zona.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var zona = new ZonaDelivery
                {
                    ZonaDeliveryId = _zonaSeleccionadaId,
                    Nombre = txtZonaNombre.Text.Trim(),
                    CostoEnvio = numCostoEnvio.Value,
                    PedidoMinimo = numPedidoMinimo.Value,
                    MontoEnvioGratis = numEnvioGratis.Value,
                    Activo = chkZonaActiva.Checked
                };

                _deliveryService.GuardarZona(zona);
                MessageBox.Show("Zona guardada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarZonasDelivery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar zona: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardarComuna_Click(object? sender, EventArgs e)
        {
            if (_zonaSeleccionadaId == 0)
            {
                MessageBox.Show("Debe seleccionar una macro zona antes de agregar o editar una comuna.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtComunaNombre.Text))
            {
                MessageBox.Show("Ingrese el nombre de la comuna.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var comuna = new ComunaDelivery
                {
                    ComunaDeliveryId = _comunaSeleccionadaId,
                    ZonaDeliveryId = _zonaSeleccionadaId,
                    Nombre = txtComunaNombre.Text.Trim(),
                    TiempoEstimadoMinutos = (int)numTiempoMinutos.Value,
                    Activo = chkComunaActiva.Checked
                };

                _deliveryService.GuardarComuna(comuna);
                MessageBox.Show("Comuna guardada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarComunasDeZona(_zonaSeleccionadaId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar comuna: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarFormularioZona()
        {
            _zonaSeleccionadaId = 0;
            txtZonaNombre.Clear();
            numCostoEnvio.Value = 0;
            numPedidoMinimo.Value = 0;
            numEnvioGratis.Value = 0;
            chkZonaActiva.Checked = true;
            txtZonaNombre.Focus();
        }

        private void LimpiarFormularioComuna()
        {
            _comunaSeleccionadaId = 0;
            txtComunaNombre.Clear();
            numTiempoMinutos.Value = 45;
            chkComunaActiva.Checked = true;
            txtComunaNombre.Focus();
        }
    }
}