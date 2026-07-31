using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormClientes : Form
    {
        private AppDbContext _db = new AppDbContext();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private Button btnRefrescar = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEstado = null!;

        private DataGridView dgvClientes = null!;
        private Cliente? _clienteSeleccionado = null;

        public FormClientes()
        {
            InitializeComponent();
            CargarClientes();
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

            // 1. BARRA SUPERIOR DE ACCIONES Y BÚSQUEDA
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
                Size = new Size(200, 32),
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarClientes(txtBuscar.Text.Trim()); e.SuppressKeyPress = true; } };

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(295, 17),
                Size = new Size(80, 34),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarClientes(txtBuscar.Text.Trim());

            btnRefrescar = new Button
            {
                Text = "🔄 Recargar",
                Location = new Point(385, 17),
                Size = new Size(100, 34),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += (s, e) => { txtBuscar.Clear(); CargarClientes(); };

            // Botones de acción alineados a la derecha
            btnNuevo = new Button
            {
                Text = "➕ Nuevo Cliente",
                Dock = DockStyle.Right,
                Width = 140,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = new Button
            {
                Text = "✏️ Editar",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += BtnEditar_Click;

            btnEstado = new Button
            {
                Text = "🔄 Activar / Desactivar",
                Dock = DockStyle.Right,
                Width = 150,
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
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

            // 2. TABLA DE CLIENTES
            dgvClientes = new DataGridView
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
            ConfigurarEstiloTabla(dgvClientes);
            dgvClientes.SelectionChanged += DgvClientes_SelectionChanged;

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 15, 0, 0)
            };
            pnlGridCard.Controls.Add(dgvClientes);

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

        private void CargarClientes(string filtro = "")
        {
            try
            {
                var query = _db.Clientes.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    query = query.Where(c => c.Nombre.Contains(filtro) || c.Rut.Contains(filtro) || c.Email.Contains(filtro));
                }

                var clientes = query.OrderBy(c => c.Nombre).ToList();
                dgvClientes.DataSource = clientes;

                if (dgvClientes.Columns["ClienteID"] != null) dgvClientes.Columns["ClienteID"].HeaderText = "ID";
                if (dgvClientes.Columns["Rut"] != null) dgvClientes.Columns["Rut"].HeaderText = "RUT / DNI";
                if (dgvClientes.Columns["Nombre"] != null) dgvClientes.Columns["Nombre"].HeaderText = "Nombre Completo";
                if (dgvClientes.Columns["Telefono"] != null) dgvClientes.Columns["Telefono"].HeaderText = "Teléfono";
                if (dgvClientes.Columns["Email"] != null) dgvClientes.Columns["Email"].HeaderText = "Correo Electrónico";
                if (dgvClientes.Columns["Direccion"] != null) dgvClientes.Columns["Direccion"].HeaderText = "Dirección";
                if (dgvClientes.Columns["Estado"] != null) dgvClientes.Columns["Estado"].HeaderText = "Estado";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvClientes_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvClientes.CurrentRow != null && dgvClientes.CurrentRow.DataBoundItem is Cliente cliente)
            {
                _clienteSeleccionado = cliente;
                btnEditar.Enabled = true;
                btnEstado.Enabled = true;
            }
            else
            {
                _clienteSeleccionado = null;
                btnEditar.Enabled = false;
                btnEstado.Enabled = false;
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            MostrarFormularioModal(null);
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            if (_clienteSeleccionado != null)
            {
                MostrarFormularioModal(_clienteSeleccionado);
            }
        }

        private void BtnEstado_Click(object? sender, EventArgs e)
        {
            if (_clienteSeleccionado == null) return;

            string accion = _clienteSeleccionado.Estado ? "desactivar" : "activar";
            var result = MessageBox.Show($"¿Desea {accion} al cliente {_clienteSeleccionado.Nombre}?", "Confirmar cambio de estado", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _clienteSeleccionado.Estado = !_clienteSeleccionado.Estado;
                    _db.SaveChanges();
                    CargarClientes(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar estado: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MostrarFormularioModal(Cliente? cliente)
        {
            bool esNuevo = (cliente == null);
            Cliente c = cliente ?? new Cliente();

            Form modal = new Form
            {
                Text = esNuevo ? "Registrar Nuevo Cliente" : "Editar Datos de Cliente",
                Size = new Size(420, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = esNuevo ? "👤 Nuevo Cliente" : "✏️ Editar Cliente", Location = new Point(25, 20), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblRut = new Label { Text = "RUT / DNI:", Location = new Point(25, 60), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtRut = new TextBox { Text = c.Rut, Location = new Point(25, 82), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Label lblNombre = new Label { Text = "Nombre Completo:", Location = new Point(25, 122), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtNombre = new TextBox { Text = c.Nombre, Location = new Point(25, 144), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Label lblTelefono = new Label { Text = "Teléfono de Contacto:", Location = new Point(25, 184), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtTelefono = new TextBox { Text = c.Telefono, Location = new Point(25, 206), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Label lblEmail = new Label { Text = "Correo Electrónico:", Location = new Point(25, 246), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtEmail = new TextBox { Text = c.Email, Location = new Point(25, 268), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Label lblDireccion = new Label { Text = "Dirección:", Location = new Point(25, 308), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtDireccion = new TextBox { Text = c.Direccion, Location = new Point(25, 330), Size = new Size(350, 30), Font = new Font("Segoe UI", 10F) };

            Button btnGuardar = new Button
            {
                Text = esNuevo ? "💾 Guardar Cliente" : "💾 Guardar Cambios",
                Location = new Point(25, 380),
                Size = new Size(350, 42),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre del cliente es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                c.Rut = txtRut.Text.Trim();
                c.Nombre = txtNombre.Text.Trim();
                c.Telefono = txtTelefono.Text.Trim();
                c.Email = txtEmail.Text.Trim();
                c.Direccion = txtDireccion.Text.Trim();

                try
                {
                    if (esNuevo)
                    {
                        _db.Clientes.Add(c);
                    }
                    _db.SaveChanges();

                    MessageBox.Show("Cliente guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modal.Close();
                    CargarClientes(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar cliente: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modal.Controls.AddRange(new Control[] { lblTitle, lblRut, txtRut, lblNombre, txtNombre, lblTelefono, txtTelefono, lblEmail, txtEmail, lblDireccion, txtDireccion, btnGuardar });
            modal.ShowDialog();
        }
    }
}