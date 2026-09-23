using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormListaProveedoresModal : Form
    {
        private readonly ProveedorService _proveedorService = new ProveedorService();

        private TextBox txtFiltro = null!;
        private DataGridView dgvProveedores = null!;
        private List<Proveedor> _listaOriginal = new List<Proveedor>();

        public Proveedor? ProveedorSeleccionado { get; private set; }

        public FormListaProveedoresModal()
        {
            InitializeComponent();
            this.Shown += (s, e) => CargarProveedores();
        }

        private void InitializeComponent()
        {
            // Ventana nativa con la barra y botón de cerrar 'X' de Windows
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Text = "Catálogo de Proveedores Registrados";
            this.Size = new Size(820, 530);
            this.BackColor = Color.White;

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.White
            };

            // 1. Buscador / Filtro superior
            Panel pnlFiltro = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 44, 
                Padding = new Padding(0, 4, 0, 8),
                BackColor = Color.Transparent
            };

            Label lblBuscar = new Label 
            { 
                Text = "🔍 Filtrar Proveedor:", 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), 
                Location = new Point(0, 10), 
                AutoSize = true,
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            txtFiltro = new TextBox
            {
                Location = new Point(140, 6),
                Size = new Size(420, 28),
                Font = new Font("Segoe UI", 9.5F)
            };
            txtFiltro.TextChanged += TxtFiltro_TextChanged;
            txtFiltro.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SeleccionarYSalir();
                }
            };

            pnlFiltro.Controls.Add(lblBuscar);
            pnlFiltro.Controls.Add(txtFiltro);

            // 2. Grilla de Proveedores
            dgvProveedores = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvProveedores);
            dgvProveedores.DoubleClick += (s, e) => SeleccionarYSalir();
            dgvProveedores.KeyDown += (s, e) => 
            { 
                if (e.KeyCode == Keys.Enter) 
                { 
                    e.Handled = true; 
                    SeleccionarYSalir(); 
                } 
            };

            // 3. Botones inferiores
            Panel pnlBotones = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 48, 
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };

            Button btnSeleccionar = new Button
            {
                Text = "✔ Seleccionar Proveedor",
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnSeleccionar.FlatAppearance.BorderSize = 0;
            btnSeleccionar.Click += (s, e) => SeleccionarYSalir();

            Button btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            // Asignación de tecla Escape para cerrar
            this.CancelButton = btnCerrar;

            pnlBotones.Controls.Add(btnCerrar);
            pnlBotones.Controls.Add(btnSeleccionar);

            pnlMain.Controls.Add(dgvProveedores);
            pnlMain.Controls.Add(pnlFiltro);
            pnlMain.Controls.Add(pnlBotones);

            this.Controls.Add(pnlMain);
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            // Evita que el encabezado cambie de color al seleccionar la celda o columna
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersHeight = 34;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 32;
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProveedores()
        {
            try
            {
                _listaOriginal = _proveedorService.ObtenerProveedoresActivos();
                ActualizarGrilla(_listaOriginal);
                txtFiltro.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarGrilla(List<Proveedor> lista)
        {
            if (dgvProveedores == null || dgvProveedores.IsDisposed) return;

            dgvProveedores.DataSource = lista.ToList();

            if (dgvProveedores.Columns.Count > 0)
            {
                if (dgvProveedores.Columns.Contains("ProveedorID")) dgvProveedores.Columns["ProveedorID"].Width = 55;
                if (dgvProveedores.Columns.Contains("Rut")) dgvProveedores.Columns["Rut"].Width = 110;
                if (dgvProveedores.Columns.Contains("RazonSocial")) dgvProveedores.Columns["RazonSocial"].HeaderText = "Razón Social";
                if (dgvProveedores.Columns.Contains("Giro")) dgvProveedores.Columns["Giro"].HeaderText = "Giro";
                if (dgvProveedores.Columns.Contains("Telefono")) dgvProveedores.Columns["Telefono"].HeaderText = "Teléfono";
                if (dgvProveedores.Columns.Contains("Email")) dgvProveedores.Columns["Email"].HeaderText = "Email";
                if (dgvProveedores.Columns.Contains("Direccion")) dgvProveedores.Columns["Direccion"].HeaderText = "Dirección";
                if (dgvProveedores.Columns.Contains("Estado")) dgvProveedores.Columns["Estado"].Visible = false;
            }
        }

        private void TxtFiltro_TextChanged(object? sender, EventArgs e)
        {
            string q = txtFiltro.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(q))
            {
                ActualizarGrilla(_listaOriginal);
                return;
            }

            var filtrados = _listaOriginal.Where(p =>
                (!string.IsNullOrEmpty(p.RazonSocial) && p.RazonSocial.ToLower().Contains(q)) ||
                (!string.IsNullOrEmpty(p.Rut) && p.Rut.ToLower().Contains(q)) ||
                (!string.IsNullOrEmpty(p.Giro) && p.Giro.ToLower().Contains(q))
            ).ToList();

            ActualizarGrilla(filtrados);
        }

        private void SeleccionarYSalir()
        {
            if (dgvProveedores.CurrentRow?.DataBoundItem is Proveedor p)
            {
                ProveedorSeleccionado = p;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}