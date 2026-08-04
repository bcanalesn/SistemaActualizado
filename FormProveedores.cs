using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormProveedores : Form
    {
        private AppDbContext _db = new AppDbContext();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private Button btnRefrescar = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEstado = null!;
        private Button btnDemo = null!;

        private DataGridView dgvProveedores = null!;
        private Proveedor? _proveedorSeleccionado = null;
        private List<Proveedor> _listaMemoria = new List<Proveedor>();

        public FormProveedores()
        {
            InitializeComponent();
            CargarProveedores();
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
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarProveedores(txtBuscar.Text.Trim()); e.SuppressKeyPress = true; } };

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
            btnBuscar.Click += (s, e) => CargarProveedores(txtBuscar.Text.Trim());

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
            btnRefrescar.Click += (s, e) => { txtBuscar.Clear(); CargarProveedores(); };

            btnNuevo = new Button
            {
                Text = "➕ Nuevo Proveedor",
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
                Text = "✏️ Editar",
                Dock = DockStyle.Right,
                Width = 90,
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

            btnDemo = new Button
            {
                Text = "✨ Cargar Demo",
                Dock = DockStyle.Right,
                Width = 125,
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDemo.FlatAppearance.BorderSize = 0;
            btnDemo.Click += BtnDemo_Click;

            pnlHeader.Controls.Add(lblBuscar);
            pnlHeader.Controls.Add(txtBuscar);
            pnlHeader.Controls.Add(btnBuscar);
            pnlHeader.Controls.Add(btnRefrescar);
            pnlHeader.Controls.Add(btnDemo);
            pnlHeader.Controls.Add(btnEstado);
            pnlHeader.Controls.Add(btnEditar);
            pnlHeader.Controls.Add(btnNuevo);

            dgvProveedores = new DataGridView
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
            ConfigurarEstiloTabla(dgvProveedores);
            dgvProveedores.SelectionChanged += DgvProveedores_SelectionChanged;

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 15, 0, 0)
            };
            pnlGridCard.Controls.Add(dgvProveedores);

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

        private void CargarProveedores(string filtro = "")
        {
            List<Proveedor> listaFinal = new List<Proveedor>();

            try
            {
                var dbSetProperty = _db.GetType().GetProperty("Proveedores");
                if (dbSetProperty != null && dbSetProperty.GetValue(_db) is IQueryable<Proveedor> query)
                {
                    if (!string.IsNullOrWhiteSpace(filtro))
                    {
                        query = query.Where(p => p.RazonSocial.Contains(filtro) || p.Rut.Contains(filtro) || p.Giro.Contains(filtro));
                    }
                    listaFinal = query.OrderBy(p => p.RazonSocial).ToList();
                }
                else
                {
                    listaFinal = FiltrarMemoria(filtro);
                }
            }
            catch
            {
                listaFinal = FiltrarMemoria(filtro);
            }

            dgvProveedores.DataSource = null;
            dgvProveedores.DataSource = listaFinal;

            if (dgvProveedores.Columns["ProveedorID"] != null) dgvProveedores.Columns["ProveedorID"].HeaderText = "ID";
            if (dgvProveedores.Columns["Rut"] != null) dgvProveedores.Columns["Rut"].HeaderText = "RUT Empresa";
            if (dgvProveedores.Columns["RazonSocial"] != null) dgvProveedores.Columns["RazonSocial"].HeaderText = "Razón Social";
            if (dgvProveedores.Columns["Giro"] != null) dgvProveedores.Columns["Giro"].HeaderText = "Giro Comercial";
            if (dgvProveedores.Columns["Telefono"] != null) dgvProveedores.Columns["Telefono"].HeaderText = "Teléfono";
            if (dgvProveedores.Columns["Email"] != null) dgvProveedores.Columns["Email"].HeaderText = "Correo Electrónico";
            if (dgvProveedores.Columns["Direccion"] != null) dgvProveedores.Columns["Direccion"].HeaderText = "Dirección";
            if (dgvProveedores.Columns["Estado"] != null) dgvProveedores.Columns["Estado"].HeaderText = "Estado";
        }

        private List<Proveedor> FiltrarMemoria(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro)) return _listaMemoria.OrderBy(p => p.RazonSocial).ToList();

            string f = filtro.ToLower();
            return _listaMemoria.Where(p => p.RazonSocial.ToLower().Contains(f) ||
                                           p.Rut.ToLower().Contains(f) ||
                                           p.Giro.ToLower().Contains(f)).OrderBy(p => p.RazonSocial).ToList();
        }

        private void DgvProveedores_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvProveedores.CurrentRow != null && dgvProveedores.CurrentRow.DataBoundItem is Proveedor proveedor)
            {
                _proveedorSeleccionado = proveedor;
                btnEditar.Enabled = true;
                btnEstado.Enabled = true;
            }
            else
            {
                _proveedorSeleccionado = null;
                btnEditar.Enabled = false;
                btnEstado.Enabled = false;
            }
        }

        private void BtnDemo_Click(object? sender, EventArgs e)
        {
            var demos = new List<Proveedor>
            {
                new Proveedor { ProveedorID = 1, Rut = "76.888.999-1", RazonSocial = "DISTRIBUIDORA LÁCTEOS SUR S.A.", Giro = "DISTRIBUCION DE PRODUCTOS LACTEOS Y PERECIBLES", Telefono = "+56 9 8888 1111", Email = "ventas@lacteossur.cl", Direccion = "Av. Industrial #450, Santiago", Estado = true },
                new Proveedor { ProveedorID = 2, Rut = "96.543.210-5", RazonSocial = "COMERCIALIZADORA DE ABARROTES CENTRAL LTA", Giro = "IMPORTADORA Y DISTRIBUIDORA DE ABARROTES", Telefono = "+56 2 2333 4444", Email = "contacto@abarrotescentral.cl", Direccion = "Calle El Roble #1200, Quilicura", Estado = true },
                new Proveedor { ProveedorID = 3, Rut = "77.111.222-3", RazonSocial = "EMBUTIDOS Y CECINAS DEL VALLE SPALTD", Giro = "ELABORACION Y DISTRIBUCION DE CECINAS Y CARNES", Telefono = "+56 9 7777 3333", Email = "pedidos@cecinasdelvalle.cl", Direccion = "Ruta 5 Sur Km 210, Talca", Estado = true }
            };

            int agregados = 0;
            foreach (var p in demos)
            {
                if (!_listaMemoria.Any(x => x.Rut == p.Rut))
                {
                    _listaMemoria.Add(p);
                    agregados++;
                }
            }

            if (agregados > 0)
            {
                MessageBox.Show($"¡Se agregaron {agregados} proveedores de prueba exitosamente!", "Éxito Demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarProveedores();
            }
            else
            {
                MessageBox.Show("Los proveedores de prueba ya se encontraban cargados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e) => MostrarModalProveedor(null);
        private void BtnEditar_Click(object? sender, EventArgs e) { if (_proveedorSeleccionado != null) MostrarModalProveedor(_proveedorSeleccionado); }

        private void BtnEstado_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null) return;

            string accion = _proveedorSeleccionado.Estado ? "desactivar" : "activar";
            var result = MessageBox.Show($"¿Desea {accion} al proveedor '{_proveedorSeleccionado.RazonSocial}'?", "Confirmar estado", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _proveedorSeleccionado.Estado = !_proveedorSeleccionado.Estado;
                CargarProveedores(txtBuscar.Text.Trim());
            }
        }

        private void MostrarModalProveedor(Proveedor? proveedor)
        {
            bool esNuevo = (proveedor == null);
            Proveedor p = proveedor ?? new Proveedor();

            Form modal = new Form
            {
                Text = esNuevo ? "Registrar Nuevo Proveedor" : "Editar Datos de Proveedor",
                Size = new Size(420, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = esNuevo ? "🚚 Nuevo Proveedor" : "✏️ Editar Proveedor", Location = new Point(25, 18), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblRut = new Label { Text = "RUT Empresa:", Location = new Point(25, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtRut = new TextBox { Text = p.Rut, Location = new Point(25, 75), Size = new Size(350, 28), Font = new Font("Segoe UI", 9.5F) };

            Label lblRazon = new Label { Text = "Razón Social / Nombre Empresa:", Location = new Point(25, 110), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtRazon = new TextBox { Text = p.RazonSocial, Location = new Point(25, 130), Size = new Size(350, 28), Font = new Font("Segoe UI", 9.5F) };

            Label lblGiro = new Label { Text = "Giro Comercial:", Location = new Point(25, 165), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtGiro = new TextBox { Text = p.Giro, Location = new Point(25, 185), Size = new Size(350, 28), Font = new Font("Segoe UI", 9.5F) };

            Label lblTel = new Label { Text = "Teléfono de Contacto:", Location = new Point(25, 220), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtTel = new TextBox { Text = p.Telefono, Location = new Point(25, 240), Size = new Size(350, 28), Font = new Font("Segoe UI", 9.5F) };

            Label lblEmail = new Label { Text = "Correo Electrónico:", Location = new Point(25, 275), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtEmail = new TextBox { Text = p.Email, Location = new Point(25, 295), Size = new Size(350, 28), Font = new Font("Segoe UI", 9.5F) };

            Label lblDir = new Label { Text = "Dirección Comercial:", Location = new Point(25, 330), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtDir = new TextBox { Text = p.Direccion, Location = new Point(25, 350), Size = new Size(350, 28), Font = new Font("Segoe UI", 9.5F) };

            Button btnGuardar = new Button
            {
                Text = esNuevo ? "💾 Guardar Proveedor" : "💾 Guardar Cambios",
                Location = new Point(25, 405),
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
                if (string.IsNullOrWhiteSpace(txtRut.Text) || string.IsNullOrWhiteSpace(txtRazon.Text))
                {
                    MessageBox.Show("El RUT y la Razón Social del proveedor son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                p.Rut = txtRut.Text.Trim();
                p.RazonSocial = txtRazon.Text.Trim();
                p.Giro = txtGiro.Text.Trim();
                p.Telefono = txtTel.Text.Trim();
                p.Email = txtEmail.Text.Trim();
                p.Direccion = txtDir.Text.Trim();

                if (esNuevo && !_listaMemoria.Contains(p))
                {
                    p.ProveedorID = _listaMemoria.Count + 1;
                    _listaMemoria.Add(p);
                }

                MessageBox.Show("Proveedor guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                modal.Close();
                CargarProveedores(txtBuscar.Text.Trim());
            };

            modal.Controls.AddRange(new Control[] { lblTitle, lblRut, txtRut, lblRazon, txtRazon, lblGiro, txtGiro, lblTel, txtTel, lblEmail, txtEmail, lblDir, txtDir, btnGuardar });
            modal.ShowDialog();
        }
    }
}