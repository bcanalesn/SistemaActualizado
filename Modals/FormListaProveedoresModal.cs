using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormListaProveedoresModal : Form
    {
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private TextBox txtFiltro = null!;
        private DataGridView dgvProveedores = null!;
        private List<Proveedor> _listaOriginal = new List<Proveedor>();

        public Proveedor? ProveedorSeleccionado { get; private set; }

        public FormListaProveedoresModal()
        {
            InitializeComponent();
            
            // 🟢 La carga se ejecuta cuando el formulario ya está completamente creado y visible
            this.Shown += (s, e) => CargarProveedores();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(800, 520);
            this.BackColor = Color.White;

            Panel pnlBorde = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 20),
                BackColor = Color.White
            };
            pnlBorde.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlBorde.ClientRectangle, Color.FromArgb(203, 213, 225), ButtonBorderStyle.Solid);
            };

            // Cabecera arrastrable
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            pnlHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            Label lblTitulo = new Label
            {
                Text = "🚚 Catálogo de Proveedores Registrados",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            lblTitulo.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            Button btnX = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand
            };
            btnX.FlatAppearance.BorderSize = 0;
            btnX.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(btnX);

            // Barra de Búsqueda
            Panel pnlFiltro = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            Label lblBuscar = new Label { Text = "🔍 Filtrar Proveedor:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(0, 14), AutoSize = true };

            txtFiltro = new TextBox
            {
                Location = new Point(140, 10),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 9.5F)
            };
            txtFiltro.TextChanged += TxtFiltro_TextChanged;

            pnlFiltro.Controls.Add(lblBuscar);
            pnlFiltro.Controls.Add(txtFiltro);

            // DataGridView de Proveedores
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
            dgvProveedores.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; SeleccionarYSalir(); } };

            // Panel Inferior de Botones
            Panel pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 0) };

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

            pnlBotones.Controls.Add(btnCerrar);
            pnlBotones.Controls.Add(btnSeleccionar);

            pnlBorde.Controls.Add(dgvProveedores);
            pnlBorde.Controls.Add(pnlFiltro);
            pnlBorde.Controls.Add(pnlBotones);
            pnlBorde.Controls.Add(pnlHeader);

            this.Controls.Add(pnlBorde);
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 30;
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarProveedores()
        {
            try
            {
                using var db = new AppDbContext();
                _listaOriginal = db.Proveedores.Where(p => p.Estado).OrderBy(p => p.RazonSocial).ToList();
                ActualizarGrilla(_listaOriginal);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarGrilla(List<Proveedor> lista)
        {
            if (dgvProveedores == null || dgvProveedores.IsDisposed) return;

            // Uso de lista tipada directa sin setear a null primero para evitar NullReference en eventos internos
            dgvProveedores.DataSource = lista.ToList();

            if (dgvProveedores.Columns.Count > 0)
            {
                if (dgvProveedores.Columns.Contains("ProveedorID")) dgvProveedores.Columns["ProveedorID"].Width = 50;
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