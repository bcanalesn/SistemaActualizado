using System;
using System.Drawing;
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

        private DataGridView dgvUsuarios = null!;
        private Usuario? _usuarioSeleccionado = null;

        public FormUsuarios()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            // BARRA SUPERIOR DE BÚSQUEDA Y ACCIONES
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(15, 12, 15, 12)
            };

            Label lblBuscar = new Label
            {
                Text = "🔍 Buscar:",
                Location = new Point(15, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            txtBuscar = new TextBox
            {
                Location = new Point(85, 18),
                Size = new Size(180, 32),
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarUsuarios(txtBuscar.Text.Trim()); e.SuppressKeyPress = true; } };

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(275, 17),
                Size = new Size(75, 34),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarUsuarios(txtBuscar.Text.Trim());

            btnRefrescar = new Button
            {
                Text = "🔄 Recargar",
                Location = new Point(358, 17),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += (s, e) => { txtBuscar.Clear(); CargarUsuarios(); };

            btnNuevo = new Button
            {
                Text = "➕ Nuevo Usuario",
                Dock = DockStyle.Right,
                Width = 145,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = new Button
            {
                Text = "✏️ Editar / Clave",
                Dock = DockStyle.Right,
                Width = 130,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += BtnEditar_Click;

            btnEstado = new Button
            {
                Text = "🔄 Estado",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEstado.FlatAppearance.BorderSize = 0;
            btnEstado.Click += BtnEstado_Click;

            pnlHeader.Controls.Add(lblBuscar);
            pnlHeader.Controls.Add(txtBuscar);
            pnlHeader.Controls.Add(btnBuscar);
            pnlHeader.Controls.Add(btnRefrescar);
            pnlHeader.Controls.Add(btnEstado);
            pnlHeader.Controls.Add(btnEditar);
            pnlHeader.Controls.Add(btnNuevo);

            // TABLA DE USUARIOS
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvUsuarios);
            dgvUsuarios.SelectionChanged += DgvUsuarios_SelectionChanged;

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 15, 0, 0)
            };
            pnlGridCard.Controls.Add(dgvUsuarios);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 38;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarUsuarios(string filtro = "")
        {
            try
            {
                var lista = _usuarioService.ObtenerUsuarios(filtro);
                dgvUsuarios.DataSource = lista;

                if (dgvUsuarios.Columns["UsuarioID"] != null) dgvUsuarios.Columns["UsuarioID"].HeaderText = "ID";
                if (dgvUsuarios.Columns["NombreUsuario"] != null) dgvUsuarios.Columns["NombreUsuario"].HeaderText = "Usuario (Login)";
                if (dgvUsuarios.Columns["NombreCompleto"] != null) dgvUsuarios.Columns["NombreCompleto"].HeaderText = "Nombre Completo";
                if (dgvUsuarios.Columns["Rol"] != null) dgvUsuarios.Columns["Rol"].HeaderText = "Rol / Perfil";
                if (dgvUsuarios.Columns["Clave"] != null) dgvUsuarios.Columns["Clave"].Visible = false;
                if (dgvUsuarios.Columns["Estado"] != null) dgvUsuarios.Columns["Estado"].HeaderText = "Activo";
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

            Button btnGuardar = new Button
            {
                Text = esNuevo ? "💾 Registrar Usuario" : "💾 Guardar Cambios",
                Location = new Point(25, 330),
                Size = new Size(330, 42),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
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
    }
}