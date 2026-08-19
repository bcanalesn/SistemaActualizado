using System;
using System.Drawing;
using System.Windows.Forms;
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

        // Botones del Menú Lateral (Orden Operativo sin Proveedores en el menú principal)
        private Button btnCaja = new Button();
        private Button btnVentas = new Button();
        private Button btnLibroVentas = new Button();
        private Button btnFolios = new Button();
        private Button btnCompras = new Button();
        private Button btnClientes = new Button();
        private Button btnProductos = new Button();
        private Button btnUsuarios = new Button();
        private Button btnReportes = new Button();
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

            // Inicio lógico: Abrir Control de Caja primero
            AbrirFormEnContent(new FormCaja(_usuarioActual), "Apertura y Cierre de Caja", btnCaja);
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

            // Logotipo
            this.lblLogo.Text = "⚡ POS SYSTEM";
            this.lblLogo.Dock = DockStyle.Top;
            this.lblLogo.Height = 70;
            this.lblLogo.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            this.lblLogo.ForeColor = Color.White;
            this.lblLogo.TextAlign = ContentAlignment.MiddleCenter;

            Panel pnlNav = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            // Posicionamiento de botones sin botón de proveedores
            int yPos = 10;
            int espaciado = 48;

            ConfigurarBotonSidebar(this.btnCaja, "💵  Control de Caja", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnVentas, "🛒  Punto de Venta", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnLibroVentas, "📚  Libro Ventas LVE", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnFolios, "📄  Control de Folios", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnCompras, "📥  Recepción Compras", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnClientes, "👥  Clientes", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnProductos, "📦  Productos", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnUsuarios, "👤  Usuarios", yPos); yPos += espaciado;
            ConfigurarBotonSidebar(this.btnReportes, "📊  Reportes", yPos);

            pnlNav.Controls.Add(this.btnReportes);
            pnlNav.Controls.Add(this.btnUsuarios);
            pnlNav.Controls.Add(this.btnProductos);
            pnlNav.Controls.Add(this.btnClientes);
            pnlNav.Controls.Add(this.btnCompras);
            pnlNav.Controls.Add(this.btnFolios);
            pnlNav.Controls.Add(this.btnLibroVentas);
            pnlNav.Controls.Add(this.btnVentas);
            pnlNav.Controls.Add(this.btnCaja);

            Panel pnlBottomNav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 115,
                BackColor = Color.Transparent
            };

            ConfigurarBotonSidebar(this.btnCerrarSesion, "🔒  Cerrar Sesión", 10);
            ConfigurarBotonSidebar(this.btnSalir, "🚪  Salir", 60);

            this.btnCaja.Click += (s, e) => AbrirFormEnContent(new FormCaja(_usuarioActual), "Apertura y Cierre de Caja", btnCaja);
            this.btnVentas.Click += (s, e) => AbrirFormEnContent(new FormVenta(_usuarioActual), "Punto de Venta DTE", btnVentas);
            this.btnLibroVentas.Click += (s, e) => AbrirFormEnContent(new FormLibroVentas(), "Libro de Ventas Electrónico (LVE) y Formulario F29", btnLibroVentas);
            this.btnFolios.Click += (s, e) => AbrirFormEnContent(new FormFolios(), "Control de Folios Autorizados (SII)", btnFolios);
            this.btnCompras.Click += (s, e) => AbrirFormEnContent(new FormCompras(), "Recepción de Compras e Incremento de Stock", btnCompras);
            this.btnClientes.Click += (s, e) => AbrirFormEnContent(new FormClientes(), "Gestión de Clientes (CRM)", btnClientes);
            this.btnProductos.Click += (s, e) => AbrirFormEnContent(new FormProductos(), "Gestión de Productos e Inventario", btnProductos);
            this.btnUsuarios.Click += (s, e) => AbrirFormEnContent(new FormUsuarios(), "Gestión de Cuentas de Usuarios", btnUsuarios);
            this.btnReportes.Click += (s, e) => AbrirFormEnContent(new FormReportes(), "Reportes y Estadísticas de Ventas", btnReportes);

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

            pnlBottomNav.Controls.Add(this.btnCerrarSesion);
            pnlBottomNav.Controls.Add(this.btnSalir);

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

            string nombreUser = _usuarioActual?.NombreCompleto ?? "Barbara";
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
        }

        private void AplicarPermisosPorRol()
        {
            if (_usuarioActual != null && _usuarioActual.Rol.Equals("Cajero", StringComparison.OrdinalIgnoreCase))
            {
                btnUsuarios.Visible = false;
                btnReportes.Visible = false;
                btnFolios.Visible = false;
                btnLibroVentas.Visible = false;
                btnCompras.Visible = false;
            }
        }

        private void ConfigurarBotonSidebar(Button btn, string texto, int top)
        {
            btn.Text = texto;
            btn.Location = new Point(10, top);
            btn.Size = new Size(215, 42);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
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
                btnCaja, btnVentas, btnLibroVentas, btnFolios, 
                btnCompras, btnClientes, btnProductos, 
                btnUsuarios, btnReportes, btnCerrarSesion 
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