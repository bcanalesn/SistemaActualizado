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
        // --- Controles Pestaña Catálogo POS ---
        private RadioButton rbOrdenCategoria = null!;
        private RadioButton rbOrdenPersonalizado = null!;
        private RadioButton rbOrdenMasVendidos = null!;
        private Panel pnlContenedorPersonalizado = null!;
        private TextBox txtBuscarProductoOrden = null!;
        private DataGridView dgvOrdenProductos = null!;
        private Button btnSubirOrden = null!;
        private Button btnBajarOrden = null!;
        private Button btnGuardarCatalogoConfig = null!;
        private Label lblTotalItemsOrden = null!;

        private List<Producto> _productosOrdenMemoria = new List<Producto>();

        public FormConfiguracion()
        {
            // Forzar cultura chilena para que los separadores de miles sean puntos en todos los controles
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("es-CL");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-CL");

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarDatosEmpresa();
            CargarZonasDelivery();
            CargarConfiguracionCatalogoUI();
        }
        private void InitializeComponent()
        {
            this.Text = "Configuración General del Negocio y Datos Tributarios";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            // Header Principal
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

            // TabControl Principal
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
            tabPrincipal.TabPages.Add(tabCatalogo); // 👈 Cuarta pestaña

            this.Controls.Add(tabPrincipal);
            this.Controls.Add(pnlHeader);
        }
        // ==========================================
        // MÉTODOS AUXILIARES DE DISEÑO GRÁFICO
        // ==========================================
        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel 
            { 
                Location = new Point(x, y), 
                Size = new Size(ancho, alto), 
                BackColor = colorFondo 
            };

            pnl.Resize += (s, e) => pnl.Invalidate();

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.BackColor);

                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using var p = CrearRutaRedondeada(r, 8);
                using var pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
        }

        private System.Drawing.Drawing2D.GraphicsPath CrearRutaRedondeada(Rectangle rect, int radio)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
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
            // TableLayoutPanel responsivo: 54% Zonas, 46% Comunas (evita cualquier excepción de splitter)
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

            // PANEL IZQUIERDO: ZONAS
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

            // PANEL DERECHO: COMUNAS
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

            // Switch General
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

            // GroupBox Acumulación
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

            // GroupBox Canje
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

            // Botón Guardar
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

            // Actualización dinámica de los textos de ejemplo con MonedaHelper
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
        // 4. PESTAÑA: ORDEN DEL CATÁLOGO POS
        // ==========================================
        private void ConstruirPestañaCatalogoPOS(TabPage tab)
        {
            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // 1. TARJETA SELECTOR DE MODALIDAD
            Panel pnlOpcionesCard = CrearTarjetaRedondeada(0, 0, 0, 105, Color.White, Color.FromArgb(226, 232, 240));
            pnlOpcionesCard.Dock = DockStyle.Top;
            pnlOpcionesCard.Padding = new Padding(16, 12, 16, 10);

            Label lblTitModo = new Label
            {
                Text = "CRITERIO DE VISUALIZACIÓN DE PRODUCTOS EN EL PUNTO DE VENTA",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 20
            };

            FlowLayoutPanel flowRadios = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0),
                BackColor = Color.Transparent
            };

            rbOrdenCategoria = new RadioButton
            {
                Text = "📂 Por Categorías (Alfabético tradicional)",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(0, 0, 24, 0),
                Cursor = Cursors.Hand
            };

            rbOrdenPersonalizado = new RadioButton
            {
                Text = "⭐ Personalizado por el Local (Tu propio orden)",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Margin = new Padding(0, 0, 24, 0),
                Cursor = Cursors.Hand
            };

            rbOrdenMasVendidos = new RadioButton
            {
                Text = "🔥 Los Más Vendidos (Top Ventas primero)",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Cursor = Cursors.Hand
            };

            rbOrdenCategoria.CheckedChanged += (s, e) => ToggleModoCatalogo();
            rbOrdenPersonalizado.CheckedChanged += (s, e) => ToggleModoCatalogo();
            rbOrdenMasVendidos.CheckedChanged += (s, e) => ToggleModoCatalogo();

            flowRadios.Controls.AddRange(new Control[] { rbOrdenCategoria, rbOrdenPersonalizado, rbOrdenMasVendidos });
            pnlOpcionesCard.Controls.Add(flowRadios);
            pnlOpcionesCard.Controls.Add(lblTitModo);

            // 2. PANEL CONTENEDOR DE EDICIÓN PERSONALIZADA
            pnlContenedorPersonalizado = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 12, 0, 0),
                BackColor = Color.Transparent
            };

            Panel pnlCardGrilla = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardGrilla.Dock = DockStyle.Fill;
            pnlCardGrilla.Padding = new Padding(12);

            // Header con buscador y contador
            Panel pnlGridHeader = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };

            txtBuscarProductoOrden = new TextBox
            {
                Location = new Point(0, 6),
                Size = new Size(320, 26),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "🔍 Filtrar lista para ubicar producto..."
            };
            txtBuscarProductoOrden.TextChanged += (s, e) => FiltrarGrillaOrden();

            lblTotalItemsOrden = new Label
            {
                Dock = DockStyle.Right,
                AutoSize = false,
                Width = 350,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlGridHeader.Controls.Add(txtBuscarProductoOrden);
            pnlGridHeader.Controls.Add(lblTotalItemsOrden);

            // Grilla de Orden
            dgvOrdenProductos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 36 },
                Font = new Font("Segoe UI", 9F)
            };
            ConfigurarEstiloTablaCatalogo(dgvOrdenProductos);

            // Evento para cuando el usuario edita la celda "Posición" directamente con el teclado
            dgvOrdenProductos.CellEndEdit += DgvOrdenProductos_CellEndEdit;

            // Panel de Botones Laterales / Acciones (Subir, Bajar)
            Panel pnlBotonesLaterales = new Panel { Dock = DockStyle.Right, Width = 90, Padding = new Padding(8, 0, 0, 0) };

            btnSubirOrden = new Button
            {
                Text = "▲\nSubir",
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubirOrden.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnSubirOrden.Click += (s, e) => MoverFilaSeleccionada(-1);

            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 10 };

            btnBajarOrden = new Button
            {
                Text = "▼\nBajar",
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBajarOrden.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnBajarOrden.Click += (s, e) => MoverFilaSeleccionada(1);

            pnlBotonesLaterales.Controls.Add(btnBajarOrden);
            pnlBotonesLaterales.Controls.Add(pnlSpacer);
            pnlBotonesLaterales.Controls.Add(btnSubirOrden);

            pnlCardGrilla.Controls.Add(dgvOrdenProductos);
            pnlCardGrilla.Controls.Add(pnlBotonesLaterales);
            pnlCardGrilla.Controls.Add(pnlGridHeader);
            pnlContenedorPersonalizado.Controls.Add(pnlCardGrilla);

            // 3. FOOTER CON BOTÓN GUARDAR
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(0, 10, 0, 0) };

            btnGuardarCatalogoConfig = new Button
            {
                Text = "💾 Guardar Configuración de Catálogo",
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarCatalogoConfig.FlatAppearance.BorderSize = 0;
            btnGuardarCatalogoConfig.Click += BtnGuardarCatalogoConfig_Click;

            pnlFooter.Controls.Add(btnGuardarCatalogoConfig);

            pnlMain.Controls.Add(pnlContenedorPersonalizado);
            pnlMain.Controls.Add(pnlOpcionesCard);
            pnlMain.Controls.Add(pnlFooter);
            tab.Controls.Add(pnlMain);

            CargarConfiguracionCatalogoUI();
        }

        private void ConfigurarEstiloTablaCatalogo(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            
            // Color normal de los encabezados
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;

            // ✔ Candados: Evitar que se pinte de azul al hacer clic en el título
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42); // Mismo fondo oscuro
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;                // Mismo texto blanco

            // Desactivar el ordenamiento nativo por clic para que no seleccione la columna en azul
            // (y respete siempre el criterio del radio button seleccionado arriba)
            dgv.AllowUserToOrderColumns = false;

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;

            dgv.Columns.Clear();

            // Columna 1: Posición (Editable directamente)
            var colPos = new DataGridViewTextBoxColumn
            {
                Name = "OrdenPOS",
                HeaderText = "POSICIÓN (1 - N)",
                FillWeight = 16,
                ReadOnly = false
            };
            colPos.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colPos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colPos.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            colPos.DefaultCellStyle.ForeColor = Color.FromArgb(37, 99, 235);
            dgv.Columns.Add(colPos);

            // Columna 2: Nombre Producto
            var colNom = new DataGridViewTextBoxColumn
            {
                Name = "Nombre",
                HeaderText = "PRODUCTO",
                FillWeight = 44,
                ReadOnly = true
            };
            colNom.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colNom.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.Columns.Add(colNom);

            // Columna 3: Categoría
            var colCat = new DataGridViewTextBoxColumn
            {
                Name = "Categoria",
                HeaderText = "CATEGORÍA",
                FillWeight = 22,
                ReadOnly = true
            };
            colCat.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colCat.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns.Add(colCat);

            // Columna 4: Precio Venta
            var colPrecio = new DataGridViewTextBoxColumn
            {
                Name = "Precio",
                HeaderText = "PRECIO",
                FillWeight = 18,
                ReadOnly = true
            };
            colPrecio.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colPrecio.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colPrecio.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.Columns.Add(colPrecio);

            // Columna oculta con ID
            var colId = new DataGridViewTextBoxColumn { Name = "ProductoID", Visible = false };
            dgv.Columns.Add(colId);
        }

        private void CargarConfiguracionCatalogoUI()
        {
            string modo = _configService.ObtenerModoOrdenCatalogo();
            rbOrdenCategoria.Checked = (modo == "CATEGORIA");
            rbOrdenPersonalizado.Checked = (modo == "PERSONALIZADO");
            rbOrdenMasVendidos.Checked = (modo == "MAS_VENDIDOS");

            _productosOrdenMemoria = _configService.ObtenerProductosParaConfigurarOrden();
            RenderizarGrillaOrden();
            ToggleModoCatalogo();
        }

        private void ToggleModoCatalogo()
        {
            bool esPersonalizado = rbOrdenPersonalizado.Checked;
            pnlContenedorPersonalizado.Enabled = esPersonalizado;
            btnSubirOrden.Enabled = esPersonalizado;
            btnBajarOrden.Enabled = esPersonalizado;
            dgvOrdenProductos.ReadOnly = !esPersonalizado;

            pnlContenedorPersonalizado.BackColor = esPersonalizado ? Color.White : Color.FromArgb(241, 245, 249);
            
            if (!esPersonalizado)
            {
                lblTotalItemsOrden.Text = rbOrdenMasVendidos.Checked 
                    ? "🔥 Modo Automático: El orden se calcula según las boletas emitidas."
                    : "📂 Modo Automático: Los productos se ordenan alfabéticamente de la A a la Z.";
            }
            else
            {
                lblTotalItemsOrden.Text = $"⭐ Modo Manual: Arrastre o digite la posición (1 al {_productosOrdenMemoria.Count})";
            }

            // Refrescar la grilla para que refleje el orden según el radio button seleccionado
            RenderizarGrillaOrden(txtBuscarProductoOrden?.Text?.Trim() ?? "");
        }

        private void RenderizarGrillaOrden(string filtro = "")
        {
            dgvOrdenProductos.Rows.Clear();
            int total = _productosOrdenMemoria.Count;

            // Determinar qué modo está seleccionado actualmente en la vista
            string modoActual = "CATEGORIA";
            if (rbOrdenPersonalizado != null && rbOrdenPersonalizado.Checked)
            {
                modoActual = "PERSONALIZADO";
            }
            else if (rbOrdenMasVendidos != null && rbOrdenMasVendidos.Checked)
            {
                modoActual = "MAS_VENDIDOS";
            }

            // Aplicar exactamente el mismo criterio de ordenamiento del servicio
            var listaOrdenada = _configService.AplicarCriterioOrdenPOS(_productosOrdenMemoria, modoActual);

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string f = filtro.ToLower();
                listaOrdenada = listaOrdenada.Where(p => p.Nombre.ToLower().Contains(f) || p.Categoria.ToLower().Contains(f));
            }

            foreach (var p in listaOrdenada)
            {
                string precioStr = p.EsPesable
                    ? $"{MonedaHelper.Formatear(p.PrecioUnitario * 1000m, conSigno: true)}/Kg"
                    : MonedaHelper.Formatear(p.PrecioUnitario, conSigno: true);

                dgvOrdenProductos.Rows.Add(p.OrdenPOS, p.Nombre, p.Categoria, precioStr, p.ProductoID);
            }
        }

        private void FiltrarGrillaOrden()
        {
            RenderizarGrillaOrden(txtBuscarProductoOrden.Text.Trim());
        }

        private void MoverFilaSeleccionada(int direccion)
        {
            if (dgvOrdenProductos.CurrentRow == null) return;

            int prodId = Convert.ToInt32(dgvOrdenProductos.CurrentRow.Cells["ProductoID"].Value);
            int indexMemoria = _productosOrdenMemoria.FindIndex(p => p.ProductoID == prodId);

            if (indexMemoria < 0) return;

            int nuevoIndex = indexMemoria + direccion;
            if (nuevoIndex < 0 || nuevoIndex >= _productosOrdenMemoria.Count) return;

            // Intercambiar en la lista de memoria
            var item = _productosOrdenMemoria[indexMemoria];
            _productosOrdenMemoria.RemoveAt(indexMemoria);
            _productosOrdenMemoria.Insert(nuevoIndex, item);

            // Reasignar posiciones correlativas 1..N
            for (int i = 0; i < _productosOrdenMemoria.Count; i++)
            {
                _productosOrdenMemoria[i].OrdenPOS = i + 1;
            }

            RenderizarGrillaOrden(txtBuscarProductoOrden.Text.Trim());

            // Reseleccionar el ítem movido
            SeleccionarFilaPorProductoId(prodId);
        }

        private void DgvOrdenProductos_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvOrdenProductos.Columns[e.ColumnIndex].Name != "OrdenPOS") return;

            var fila = dgvOrdenProductos.Rows[e.RowIndex];
            int prodId = Convert.ToInt32(fila.Cells["ProductoID"].Value);
            string nuevoValorStr = fila.Cells["OrdenPOS"].Value?.ToString() ?? "";

            int totalProductos = _productosOrdenMemoria.Count;

            if (!int.TryParse(nuevoValorStr, out int nuevaPosicion))
            {
                RenderizarGrillaOrden(txtBuscarProductoOrden.Text.Trim());
                return;
            }

            // Ajustar al rango dinámico 1 a N
            nuevaPosicion = Math.Max(1, Math.Min(totalProductos, nuevaPosicion));

            int indexActual = _productosOrdenMemoria.FindIndex(p => p.ProductoID == prodId);
            if (indexActual >= 0)
            {
                var prod = _productosOrdenMemoria[indexActual];
                _productosOrdenMemoria.RemoveAt(indexActual);

                // Insertar en la posición deseada (índice 0-based)
                _productosOrdenMemoria.Insert(nuevaPosicion - 1, prod);

                // Reindexar toda la lista para que sea 100% limpia y sin huecos
                for (int i = 0; i < _productosOrdenMemoria.Count; i++)
                {
                    _productosOrdenMemoria[i].OrdenPOS = i + 1;
                }

                RenderizarGrillaOrden(txtBuscarProductoOrden.Text.Trim());
                SeleccionarFilaPorProductoId(prodId);
            }
        }

        private void SeleccionarFilaPorProductoId(int prodId)
        {
            foreach (DataGridViewRow row in dgvOrdenProductos.Rows)
            {
                if (row.Cells["ProductoID"].Value != null && Convert.ToInt32(row.Cells["ProductoID"].Value) == prodId)
                {
                    dgvOrdenProductos.ClearSelection();
                    row.Selected = true;
                    dgvOrdenProductos.CurrentCell = row.Cells["OrdenPOS"];
                    break;
                }
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

                // Si está en modo personalizado, persistir las posiciones 1..N
                if (modo == "PERSONALIZADO")
                {
                    var ordenes = _productosOrdenMemoria
                        .Select(p => (p.ProductoID, p.OrdenPOS))
                        .ToList();

                    _configService.GuardarOrdenPersonalizadoProductos(ordenes);
                }

                MessageBox.Show("Configuración del catálogo guardada exitosamente.\nLos cambios se reflejarán de inmediato en el Punto de Venta.", "Catálogo POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        // 3. LÓGICA DE NEGOCIO Y PERSISTENCIA
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

            // Parseo seguro de moneda
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