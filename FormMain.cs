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

        private Button btnVentas = new Button();
        private Button btnCaja = new Button();
        private Button btnHistorial = new Button();
        private Button btnLibroVentas = new Button(); // Módulo Libro de Ventas LVE F29
        private Button btnFolios = new Button(); // Módulo de Folios SII
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
            AbrirFormEnContent(new FormVenta(_usuarioActual), "Punto de Venta DTE", btnVentas);
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
            this.Size = new Size(1340, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 680);
            this.BackColor = Color.FromArgb(244, 246, 249);

            this.pnlSidebar.Dock = DockStyle.Left;
            this.pnlSidebar.Width = 230;
            this.pnlSidebar.BackColor = Color.FromArgb(24, 28, 36);

            this.lblLogo.Text = "⚡ POS SYSTEM";
            this.lblLogo.Dock = DockStyle.Top;
            this.lblLogo.Height = 75;
            this.lblLogo.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            this.lblLogo.ForeColor = Color.White;
            this.lblLogo.TextAlign = ContentAlignment.MiddleCenter;

            Panel pnlNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 520,
                BackColor = Color.Transparent
            };

            ConfigurarBotonSidebar(this.btnVentas, "🛒  Punto de Venta", 10);
            ConfigurarBotonSidebar(this.btnCaja, "💵  Control de Caja", 60);
            ConfigurarBotonSidebar(this.btnHistorial, "📜  Historial Ventas", 110);
            ConfigurarBotonSidebar(this.btnLibroVentas, "📚  Libro Ventas LVE", 160);
            ConfigurarBotonSidebar(this.btnFolios, "📄  Control de Folios", 210);
            ConfigurarBotonSidebar(this.btnClientes, "👥  Clientes", 260);
            ConfigurarBotonSidebar(this.btnProductos, "📦  Productos", 310);
            ConfigurarBotonSidebar(this.btnUsuarios, "👤  Usuarios", 360);
            ConfigurarBotonSidebar(this.btnReportes, "📊  Reportes", 410);

            pnlNav.Controls.Add(this.btnReportes);
            pnlNav.Controls.Add(this.btnUsuarios);
            pnlNav.Controls.Add(this.btnProductos);
            pnlNav.Controls.Add(this.btnClientes);
            pnlNav.Controls.Add(this.btnFolios);
            pnlNav.Controls.Add(this.btnLibroVentas);
            pnlNav.Controls.Add(this.btnHistorial);
            pnlNav.Controls.Add(this.btnCaja);
            pnlNav.Controls.Add(this.btnVentas);

            Panel pnlBottomNav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                BackColor = Color.Transparent
            };

            ConfigurarBotonSidebar(this.btnCerrarSesion, "🔒  Cerrar Sesión", 10);
            ConfigurarBotonSidebar(this.btnSalir, "🚪  Salir", 65);

            this.btnVentas.Click += (s, e) => AbrirFormEnContent(new FormVenta(_usuarioActual), "Punto de Venta DTE", btnVentas);
            this.btnCaja.Click += (s, e) => AbrirFormEnContent(new FormCaja(_usuarioActual), "Apertura y Cierre de Caja", btnCaja);
            this.btnHistorial.Click += (s, e) => AbrirFormEnContent(new FormHistorialVentas(), "Historial y Devoluciones de Ventas", btnHistorial);
            this.btnLibroVentas.Click += (s, e) => AbrirFormEnContent(new FormLibroVentas(), "Libro de Ventas Electrónico (LVE) y Formulario F29", btnLibroVentas);
            this.btnFolios.Click += (s, e) => AbrirFormEnContent(new FormFolios(), "Control de Folios Autorizados (SII)", btnFolios);
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

            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 65;
            this.pnlHeader.BackColor = Color.White;
            this.pnlHeader.Padding = new Padding(25, 0, 20, 0);

            this.lblTituloVista.Text = "Punto de Venta DTE";
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
            }
        }

        private void ConfigurarBotonSidebar(Button btn, string texto, int top)
        {
            btn.Text = texto;
            btn.Location = new Point(10, top);
            btn.Size = new Size(210, 48);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btn.ForeColor = Color.FromArgb(160, 174, 192);
            btn.BackColor = Color.Transparent;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(15, 0, 0, 0);
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
            Button[] botones = new[] { btnVentas, btnCaja, btnHistorial, btnLibroVentas, btnFolios, btnClientes, btnProductos, btnUsuarios, btnReportes, btnCerrarSesion };
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