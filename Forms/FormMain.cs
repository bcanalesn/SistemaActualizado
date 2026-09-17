using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Forms;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormMain : Form
    {
        private Panel pnlSidebar = null!;
        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Label lblLogo = null!;
        private Label lblTituloVista = null!;
        private Label lblUsuarioInfo = null!;

        // Botones del Menú Lateral
        private Button btnCaja = new Button();
        private Button btnVentas = new Button();
        private Button btnLibroVentas = new Button();
        private Button btnLibroCompras = new Button();
        private Button btnFolios = new Button();
        private Button btnCompras = new Button();
        private Button btnClientes = new Button();
        private Button btnCuentasPorCobrar = new Button();
        private Button btnProductos = new Button();
        private Button btnUsuarios = new Button();
        private Button btnReportes = new Button();
        private Button btnConfiguracion = new Button();
        private Button btnCerrarSesion = new Button();
        private Button btnSalir = new Button();

        private Form? _formActivo = null;
        private Usuario? _usuarioActual;

        public bool EsCerrarSesion { get; private set; } = false;

        public FormMain(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            AplicarPermisosPorRol();

            // Pantalla inicial según el rol del usuario
            string rol = _usuarioActual?.Rol ?? "Administrador";

            if (rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase))
            {
                AbrirFormEnContent(new FormVenta(_usuarioActual), "Punto de Venta DTE", btnVentas);
            }
            else if (rol.Equals("Bodeguero", StringComparison.OrdinalIgnoreCase))
            {
                AbrirFormEnContent(new FormCompras(), "Recepción de Compras e Incremento de Stock", btnCompras);
            }
            else
            {
                // Administrador, Supervisor y Cajero inician en Control de Caja
                AbrirFormEnContent(new FormCaja(_usuarioActual), "Apertura y Cierre de Caja", btnCaja);
            }
        }

        private void InitializeComponent()
        {
            this.pnlSidebar = new Panel();
            this.lblLogo = new Label();
            this.pnlHeader = new Panel();
            this.lblTituloVista = new Label();
            this.pnlContent = new Panel();

            this.SuspendLayout();

            this.Text = "Sistema POS Moderno - Control DTE e Inventario";
            this.Size = new Size(1360, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1150, 700);
            this.BackColor = Color.FromArgb(244, 246, 249);

            // Sidebar
            this.pnlSidebar.Dock = DockStyle.Left;
            this.pnlSidebar.Width = 240;
            this.pnlSidebar.BackColor = Color.FromArgb(24, 28, 36);

            // Logotipo Superior Fijo
            this.lblLogo.Text = "⚡ POS SYSTEM";
            this.lblLogo.Dock = DockStyle.Top;
            this.lblLogo.Height = 70;
            this.lblLogo.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            this.lblLogo.ForeColor = Color.White;
            this.lblLogo.TextAlign = ContentAlignment.MiddleCenter;

            // Contenedor Central Desplazable de Vistas
            FlowLayoutPanel pnlNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 4)
            };

            ConfigurarBotonSidebar(this.btnCaja, "💵  Control de Caja");
            ConfigurarBotonSidebar(this.btnVentas, "🛒  Punto de Venta");
            ConfigurarBotonSidebar(this.btnLibroVentas, "📚  Libro Ventas LVE");
            ConfigurarBotonSidebar(this.btnLibroCompras, "📕  Libro de Compras");
            ConfigurarBotonSidebar(this.btnFolios, "📄  Control de Folios");
            ConfigurarBotonSidebar(this.btnCompras, "📥  Recepción Compras");
            ConfigurarBotonSidebar(this.btnClientes, "👥  Clientes");
            ConfigurarBotonSidebar(this.btnCuentasPorCobrar, "💳  Cuentas por Cobrar");
            ConfigurarBotonSidebar(this.btnProductos, "📦  Productos");
            ConfigurarBotonSidebar(this.btnUsuarios, "👤  Usuarios");
            ConfigurarBotonSidebar(this.btnReportes, "📊  Reportes");
            ConfigurarBotonSidebar(this.btnConfiguracion, "⚙️  Configuración");

            // Orden natural de arriba hacia abajo
            pnlNav.Controls.Add(this.btnCaja);
            pnlNav.Controls.Add(this.btnVentas);
            pnlNav.Controls.Add(this.btnLibroVentas);
            pnlNav.Controls.Add(this.btnLibroCompras);
            pnlNav.Controls.Add(this.btnFolios);
            pnlNav.Controls.Add(this.btnCompras);
            pnlNav.Controls.Add(this.btnClientes);
            pnlNav.Controls.Add(this.btnCuentasPorCobrar);
            pnlNav.Controls.Add(this.btnProductos);
            pnlNav.Controls.Add(this.btnUsuarios);
            pnlNav.Controls.Add(this.btnReportes);
            pnlNav.Controls.Add(this.btnConfiguracion);

            // Contenedor Fijo Inferior (Cerrar Sesión y Salir)
            Panel pnlBottomNav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 98,
                BackColor = Color.Transparent
            };

            ConfigurarBotonInferior(this.btnCerrarSesion, "🔒  Cerrar Sesión", 6);
            ConfigurarBotonInferior(this.btnSalir, "🚪  Salir", 50);

            pnlBottomNav.Controls.Add(this.btnCerrarSesion);
            pnlBottomNav.Controls.Add(this.btnSalir);

            // Eventos de Navegación
            this.btnCaja.Click += (s, e) => AbrirFormEnContent(new FormCaja(_usuarioActual), "Apertura y Cierre de Caja", btnCaja);
            this.btnVentas.Click += (s, e) => AbrirFormEnContent(new FormVenta(_usuarioActual), "Punto de Venta DTE", btnVentas);
            this.btnLibroVentas.Click += (s, e) => AbrirFormEnContent(new FormLibroVentas(), "Libro de Ventas Electrónico (LVE) y Formulario F29", btnLibroVentas);
            this.btnLibroCompras.Click += (s, e) => AbrirFormEnContent(new FormLibroCompras(), "Libro de Compras y Control de Facturas Recibidas", btnLibroCompras);
            this.btnFolios.Click += (s, e) => AbrirFormEnContent(new FormFolios(), "Control de Folios Autorizados (SII)", btnFolios);
            this.btnCompras.Click += (s, e) => AbrirFormEnContent(new FormCompras(), "Recepción de Compras e Incremento de Stock", btnCompras);
            this.btnClientes.Click += (s, e) => AbrirFormEnContent(new FormClientes(), "Gestión de Clientes (CRM)", btnClientes);
            this.btnCuentasPorCobrar.Click += (s, e) => AbrirFormEnContent(new SISTEMAACTUALIZADO.Forms.FormCuentasPorCobrar(), "Gestión de Cuentas por Cobrar y Control de Crédito Comercial", btnCuentasPorCobrar);
            this.btnProductos.Click += (s, e) => AbrirFormEnContent(new FormProductos(), "Gestión de Productos e Inventario", btnProductos);
            this.btnUsuarios.Click += (s, e) => AbrirFormEnContent(new FormUsuarios(), "Gestión de Cuentas de Usuarios", btnUsuarios);
            this.btnReportes.Click += (s, e) => AbrirFormEnContent(new FormReportes(), "Reportes y Estadísticas de Ventas", btnReportes);
            this.btnConfiguracion.Click += (s, e) => AbrirFormEnContent(new FormConfiguracion(), "Configuración General del Negocio y Datos Tributarios", btnConfiguracion);

            this.btnCerrarSesion.Click += (s, e) =>
            {
                var result = MessageBox.Show("¿Está seguro que desea cerrar sesión?", "Cerrar Sesión", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    EsCerrarSesion = true;
                    this.Close();
                }
            };

            this.btnSalir.Click += (s, e) => Application.Exit();

            // Ensamblaje del Sidebar en orden estricto de capas
            this.pnlSidebar.Controls.Add(pnlNav);
            this.pnlSidebar.Controls.Add(pnlBottomNav);
            this.pnlSidebar.Controls.Add(this.lblLogo);

            // Header Superior
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 65;
            this.pnlHeader.BackColor = Color.White;
            this.pnlHeader.Padding = new Padding(25, 0, 20, 0);

            this.lblTituloVista.Text = "Apertura y Cierre de Caja";
            this.lblTituloVista.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            this.lblTituloVista.ForeColor = Color.FromArgb(30, 41, 59);
            this.lblTituloVista.Dock = DockStyle.Left;
            this.lblTituloVista.TextAlign = ContentAlignment.MiddleLeft;
            this.lblTituloVista.AutoSize = true;

            string nombreUser = _usuarioActual?.NombreCompleto ?? "Bárbara";
            string rolUser = _usuarioActual?.Rol ?? "Administrador";

            this.lblUsuarioInfo = new Label
            {
                Text = $"👤 Sesión Activa: {nombreUser} ({rolUser})",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 255),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            this.pnlHeader.Controls.Add(this.lblTituloVista);
            this.pnlHeader.Controls.Add(this.lblUsuarioInfo);

            // Contenedor Central
            this.pnlContent.Dock = DockStyle.Fill;
            this.pnlContent.BackColor = Color.FromArgb(244, 246, 249);
            this.pnlContent.Padding = new Padding(15);

            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlSidebar);

            this.ResumeLayout(false);

            // Asegura que al iniciar el scroll esté en el primer ítem superior
            pnlNav.AutoScrollPosition = Point.Empty;
        }

        private void AplicarPermisosPorRol()
        {
            string rol = _usuarioActual?.Rol ?? "Administrador";

            // 1. VENDEDOR: Únicamente Punto de Venta
            if (rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase))
            {
                btnCaja.Visible = false;
                btnVentas.Visible = true;
                btnLibroVentas.Visible = false;
                btnLibroCompras.Visible = false;
                btnFolios.Visible = false;
                btnCompras.Visible = false;
                btnClientes.Visible = false;
                btnCuentasPorCobrar.Visible = false;
                btnProductos.Visible = false;
                btnUsuarios.Visible = false;
                btnReportes.Visible = false;
                btnConfiguracion.Visible = false;
                return;
            }

            // 2. BODEGUERO: Únicamente Recepción de Compras
            if (rol.Equals("Bodeguero", StringComparison.OrdinalIgnoreCase))
            {
                btnCaja.Visible = false;
                btnVentas.Visible = false;
                btnLibroVentas.Visible = false;
                btnLibroCompras.Visible = false;
                btnFolios.Visible = false;
                btnCompras.Visible = true;
                btnClientes.Visible = false;
                btnCuentasPorCobrar.Visible = false;
                btnProductos.Visible = false;
                btnUsuarios.Visible = false;
                btnReportes.Visible = false;
                btnConfiguracion.Visible = false;
                return;
            }

            // 3. CAJERO: Control de Caja y Punto de Venta
            if (rol.Equals("Cajero", StringComparison.OrdinalIgnoreCase))
            {
                btnCaja.Visible = true;
                btnVentas.Visible = true;
                btnLibroVentas.Visible = false;
                btnLibroCompras.Visible = false;
                btnFolios.Visible = false;
                btnCompras.Visible = false;
                btnClientes.Visible = false;
                btnCuentasPorCobrar.Visible = false;
                btnProductos.Visible = false;
                btnUsuarios.Visible = false;
                btnReportes.Visible = false;
                btnConfiguracion.Visible = false;
                return;
            }

            // 4. ADMINISTRADOR Y SUPERVISOR: Acceso total a todas las vistas
            btnCaja.Visible = true;
            btnVentas.Visible = true;
            btnLibroVentas.Visible = true;
            btnLibroCompras.Visible = true;
            btnFolios.Visible = true;
            btnCompras.Visible = true;
            btnClientes.Visible = true;
            btnCuentasPorCobrar.Visible = true;
            btnProductos.Visible = true;
            btnUsuarios.Visible = true;
            btnReportes.Visible = true;
            btnConfiguracion.Visible = true;
        }

        private void ConfigurarBotonSidebar(Button btn, string texto)
        {
            btn.Text = texto;
            btn.Size = new Size(220, 38);
            btn.Margin = new Padding(10, 2, 10, 2);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.ForeColor = Color.FromArgb(160, 174, 192);
            btn.BackColor = Color.Transparent;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(12, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
        }

        private void ConfigurarBotonInferior(Button btn, string texto, int top)
        {
            btn.Text = texto;
            btn.Location = new Point(10, top);
            btn.Size = new Size(220, 38);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.ForeColor = Color.FromArgb(160, 174, 192);
            btn.BackColor = Color.Transparent;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(12, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
        }

        private void AbrirFormEnContent(Form formHijo, string titulo, Button botonSeleccionado)
        {
            if (_formActivo != null) _formActivo.Close();

            ResaltarBotonActivo(botonSeleccionado);
            _formActivo = formHijo;
            lblTituloVista.Text = titulo;

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;

            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(formHijo);
            pnlContent.Tag = formHijo;
            formHijo.Show();
        }

        private void ResaltarBotonActivo(Button btn)
        {
            Button[] botones = new[] { 
                btnCaja, btnVentas, btnLibroVentas, btnLibroCompras, btnFolios, 
                btnCompras, btnClientes, btnCuentasPorCobrar, btnProductos, 
                btnUsuarios, btnReportes, btnConfiguracion, btnCerrarSesion 
            };

            foreach (var b in botones)
            {
                if (b != null)
                {
                    b.BackColor = (b == btn) ? Color.FromArgb(0, 102, 255) : Color.Transparent;
                    b.ForeColor = (b == btn) ? Color.White : Color.FromArgb(160, 174, 192);
                }
            }
        }
    }
}