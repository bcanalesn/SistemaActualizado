using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormUsuarios : Form
    {
        private readonly UsuarioService _usuarioService = new UsuarioService();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private Button btnRefrescar = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEstado = null!;
        private Label lblContadorFooter = null!;

        private DataGridView dgvUsuarios = null!;
        private Usuario? _usuarioSeleccionado = null;

        public FormUsuarios()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarUsuarios();
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
                Text = "👤 Cuentas de Usuario",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Label lblSubtitulo = new Label
            {
                Text = "Control de acceso, gestión de contraseñas y roles del personal",
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

            btnNuevo = CrearBoton("➕ Nuevo Usuario", Color.FromArgb(16, 185, 129), Color.White, new Size(145, 34), 6);
            btnNuevo.Margin = new Padding(4, 0, 0, 0);
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = CrearBoton("✏️ Editar / Clave", Color.FromArgb(2, 132, 199), Color.White, new Size(130, 34), 6);
            btnEditar.Margin = new Padding(4, 0, 0, 0);
            btnEditar.Enabled = false;
            btnEditar.Click += BtnEditar_Click;

            btnEstado = CrearBoton("🔄 Activar / Bloquear", Color.FromArgb(245, 158, 11), Color.White, new Size(160, 34), 6);
            btnEstado.Margin = new Padding(4, 0, 0, 0);
            btnEstado.Enabled = false;
            btnEstado.Click += BtnEstado_Click;

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnEstado });
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
                Size = new Size(260, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "Nombre de usuario o nombre completo..."
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarUsuarios(txtBuscar.Text.Trim()); e.SuppressKeyPress = true; } };
            Panel pnlGrpBuscar = CrearGrupoConCaja("BUSCAR USUARIO", txtBuscar, 275);

            btnBuscar = CrearBoton("🔍 Buscar", Color.FromArgb(30, 41, 59), Color.White, new Size(95, 32), 6);
            btnBuscar.Margin = new Padding(8, 14, 4, 0);
            btnBuscar.Click += (s, e) => CargarUsuarios(txtBuscar.Text.Trim());

            btnRefrescar = CrearBoton("🔄 Recargar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(105, 32), 6);
            btnRefrescar.Margin = new Padding(4, 14, 0, 0);
            btnRefrescar.Click += (s, e) => { txtBuscar.Clear(); CargarUsuarios(); };

            flpFiltros.Controls.AddRange(new Control[] { pnlGrpBuscar, btnBuscar, btnRefrescar });
            pnlFiltrosCard.Controls.Add(flpFiltros);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // =========================================================================
            // 3. TARJETA DE GRILLA PRINCIPAL REDONDEADA
            // =========================================================================
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

            dgvUsuarios = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false,
                RowTemplate = { Height = 34 }
            };
            ConfigurarEstiloYColumnasTabla();
            dgvUsuarios.SelectionChanged += DgvUsuarios_SelectionChanged;

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 cuentas registradas",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlGridCard.Controls.Add(dgvUsuarios);
            pnlGridCard.Controls.Add(pnlFooter);

            // Orden Z estricto
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlFiltrosWrapper);
            pnlMain.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void ConfigurarEstiloYColumnasTabla()
        {
            dgvUsuarios.EnableHeadersVisualStyles = false;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvUsuarios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvUsuarios.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvUsuarios.ColumnHeadersHeight = 36;

            dgvUsuarios.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgvUsuarios.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgvUsuarios.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvUsuarios.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvUsuarios.RowTemplate.Height = 34;
            dgvUsuarios.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvUsuarios.GridColor = Color.FromArgb(226, 232, 240);

            dgvUsuarios.Columns.Clear();

            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "UsuarioID", 
                HeaderText = "ID", 
                DataPropertyName = "UsuarioID", 
                FillWeight = 8, 
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } 
            });

            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "NombreUsuario", 
                HeaderText = "USUARIO (LOGIN)", 
                DataPropertyName = "NombreUsuario", 
                FillWeight = 22, 
                DefaultCellStyle = 
                { 
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(37, 99, 235) 
                } 
            });

            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "NombreCompleto", 
                HeaderText = "NOMBRE COMPLETO", 
                DataPropertyName = "NombreCompleto", 
                FillWeight = 38 
            });

            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Rol", 
                HeaderText = "ROL / PERFIL", 
                DataPropertyName = "Rol", 
                FillWeight = 18, 
                DefaultCellStyle = 
                { 
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(124, 58, 237) 
                } 
            });

            dgvUsuarios.Columns.Add(new DataGridViewCheckBoxColumn 
            { 
                Name = "Estado", 
                HeaderText = "ACTIVO", 
                DataPropertyName = "Estado", 
                FillWeight = 14 
            });
        }

        private void CargarUsuarios(string filtro = "")
        {
            try
            {
                var lista = _usuarioService.ObtenerUsuarios(filtro) ?? new System.Collections.Generic.List<Usuario>();
                dgvUsuarios.DataSource = null;
                dgvUsuarios.DataSource = lista;

                lblContadorFooter.Text = $"Mostrando {lista.Count:N0} cuenta(s) de usuario registrada(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvUsuarios_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvUsuarios.CurrentRow != null && dgvUsuarios.CurrentRow.DataBoundItem is Usuario usuario)
            {
                _usuarioSeleccionado = usuario;
                btnEditar.Enabled = true;
                btnEstado.Enabled = true;
            }
            else
            {
                _usuarioSeleccionado = null;
                btnEditar.Enabled = false;
                btnEstado.Enabled = false;
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            MostrarModalUsuario(null);
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            if (_usuarioSeleccionado != null)
            {
                MostrarModalUsuario(_usuarioSeleccionado);
            }
        }

        private void BtnEstado_Click(object? sender, EventArgs e)
        {
            if (_usuarioSeleccionado == null) return;

            string accion = _usuarioSeleccionado.Estado ? "desactivar" : "activar";
            var result = MessageBox.Show($"¿Desea {accion} la cuenta de '{_usuarioSeleccionado.NombreUsuario}'?", "Confirmar estado", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _usuarioService.AlternarEstado(_usuarioSeleccionado.UsuarioID);
                    CargarUsuarios(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar estado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MostrarModalUsuario(Usuario? usuario)
        {
            bool esNuevo = (usuario == null);
            Usuario u = usuario ?? new Usuario();

            Form modal = new Form
            {
                Text = esNuevo ? "Crear Nueva Cuenta de Usuario" : "Editar Usuario y Contraseña",
                Size = new Size(400, 440),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = esNuevo ? "👤 Crear Cuenta de Usuario" : "✏️ Editar Usuario", Location = new Point(25, 20), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblUser = new Label { Text = "Nombre de Usuario (Login):", Location = new Point(25, 60), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtUser = new TextBox { Text = u.NombreUsuario, Location = new Point(25, 82), Size = new Size(330, 30), Font = new Font("Segoe UI", 10F) };

            Label lblNombre = new Label { Text = "Nombre Completo:", Location = new Point(25, 122), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtNombre = new TextBox { Text = u.NombreCompleto, Location = new Point(25, 144), Size = new Size(330, 30), Font = new Font("Segoe UI", 10F) };

            Label lblPass = new Label { Text = "Contraseña:", Location = new Point(25, 184), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtPass = new TextBox { Text = u.Clave, Location = new Point(25, 206), Size = new Size(330, 30), Font = new Font("Segoe UI", 10F), UseSystemPasswordChar = true };

            Label lblRol = new Label { Text = "Rol / Nivel de Acceso:", Location = new Point(25, 246), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbRol = new ComboBox { Location = new Point(25, 268), Size = new Size(330, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cbRol.Items.AddRange(new string[] { "Administrador", "Cajero" });
            cbRol.SelectedItem = string.IsNullOrEmpty(u.Rol) ? "Cajero" : u.Rol;

            Button btnGuardar = CrearBoton(esNuevo ? "💾 Registrar Usuario" : "💾 Guardar Cambios", Color.FromArgb(0, 102, 255), Color.White, new Size(330, 42), 8);
            btnGuardar.Location = new Point(25, 325);

            btnGuardar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
                {
                    MessageBox.Show("El usuario y la contraseña son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                u.NombreUsuario = txtUser.Text.Trim();
                u.NombreCompleto = txtNombre.Text.Trim();
                u.Clave = txtPass.Text.Trim();
                u.Rol = cbRol.SelectedItem?.ToString() ?? "Cajero";

                try
                {
                    _usuarioService.GuardarUsuario(u, esNuevo);

                    MessageBox.Show("Cuenta de usuario guardada con éxito.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modal.Close();
                    CargarUsuarios(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar usuario: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modal.Controls.AddRange(new Control[] { lblTitle, lblUser, txtUser, lblNombre, txtNombre, lblPass, txtPass, lblRol, cbRol, btnGuardar });
            modal.ShowDialog();
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