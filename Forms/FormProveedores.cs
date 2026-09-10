using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormProveedores : Form
    {
        private readonly ProveedorService _proveedorService = new ProveedorService();

        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private Button btnRefrescar = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEstado = null!;
        private Button btnDemo = null!;

        private DataGridView dgvProveedores = null!;
        private Proveedor? _proveedorSeleccionado = null;

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
            try
            {
                var listaFinal = _proveedorService.ObtenerProveedores(filtro);
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            try
            {
                int agregados = _proveedorService.CargarProveedoresDemo();
                if (agregados > 0)
                {
                    MessageBox.Show($"¡Se agregaron {agregados} proveedores de prueba exitosamente!", "Éxito Demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarProveedores();
                }
                else
                {
                    MessageBox.Show("Los proveedores de prueba ya se encontraban registrados en la base de datos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos demo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            using var modal = new FormNuevoProveedorModal();
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                CargarProveedores(txtBuscar.Text.Trim());
            }
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null) return;

            using var modal = new FormNuevoProveedorModal(_proveedorSeleccionado);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                CargarProveedores(txtBuscar.Text.Trim());
            }
        }

        private void BtnEstado_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null) return;

            string accion = _proveedorSeleccionado.Estado ? "desactivar" : "activar";
            var result = MessageBox.Show($"¿Desea {accion} al proveedor '{_proveedorSeleccionado.RazonSocial}'?", "Confirmar estado", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _proveedorService.AlternarEstado(_proveedorSeleccionado.ProveedorID);
                    CargarProveedores(txtBuscar.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cambiar estado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}