using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormNuevoProveedorModal : Form
    {
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private TextBox txtRut = null!;
        private TextBox txtRazonSocial = null!;
        private TextBox txtGiro = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtEmail = null!;
        private TextBox txtDireccion = null!;
        private Button btnGuardar = null!;
        private Button btnCancelar = null!;
        private Label lblTitulo = null!;

        private readonly Proveedor? _proveedorAEditar;
        public Proveedor? ProveedorResultado { get; private set; }

        public FormNuevoProveedorModal(Proveedor? proveedor = null)
        {
            _proveedorAEditar = proveedor;
            InitializeComponent();
            CargarDatosSiEsEdicion();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(460, 490);
            this.BackColor = Color.White;

            Panel pnlBorde = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 16, 22, 20),
                BackColor = Color.White
            };
            pnlBorde.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlBorde.ClientRectangle, Color.FromArgb(203, 213, 225), ButtonBorderStyle.Solid);
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };
            pnlHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            lblTitulo = new Label
            {
                Text = _proveedorAEditar == null ? "🚚 Registrar Nuevo Proveedor" : "✏️ Editar Proveedor",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            lblTitulo.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

            Button btnCerrar = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(btnCerrar);

            FlowLayoutPanel flowCampos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                AutoScroll = true
            };

            txtRut = new TextBox { Width = 405, Font = new Font("Segoe UI", 9.5F) };
            txtRazonSocial = new TextBox { Width = 405, Font = new Font("Segoe UI", 9.5F) };
            txtGiro = new TextBox { Width = 405, Font = new Font("Segoe UI", 9.5F), Text = "Distribución y Comercialización" };
            txtTelefono = new TextBox { Width = 405, Font = new Font("Segoe UI", 9.5F) };
            txtEmail = new TextBox { Width = 405, Font = new Font("Segoe UI", 9.5F) };
            txtDireccion = new TextBox { Width = 405, Font = new Font("Segoe UI", 9.5F) };

            flowCampos.Controls.Add(CrearRenglonCampo("RUT Proveedor (ej: 76.123.456-7):", txtRut));
            flowCampos.Controls.Add(CrearRenglonCampo("Razón Social / Empresa:", txtRazonSocial));
            flowCampos.Controls.Add(CrearRenglonCampo("Giro Comercial:", txtGiro));
            flowCampos.Controls.Add(CrearRenglonCampo("Teléfono:", txtTelefono));
            flowCampos.Controls.Add(CrearRenglonCampo("Email:", txtEmail));
            flowCampos.Controls.Add(CrearRenglonCampo("Dirección:", txtDireccion));

            Panel pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 45 };

            btnGuardar = new Button
            {
                Text = _proveedorAEditar == null ? "💾 Guardar Proveedor" : "💾 Guardar Cambios",
                Size = new Size(180, 38),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(225, 4)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(115, 4)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();

            pnlBotones.Controls.Add(btnGuardar);
            pnlBotones.Controls.Add(btnCancelar);

            pnlBorde.Controls.Add(flowCampos);
            pnlBorde.Controls.Add(pnlBotones);
            pnlBorde.Controls.Add(pnlHeader);

            this.Controls.Add(pnlBorde);
        }

        private void CargarDatosSiEsEdicion()
        {
            if (_proveedorAEditar != null)
            {
                txtRut.Text = _proveedorAEditar.Rut;
                txtRazonSocial.Text = _proveedorAEditar.RazonSocial;
                txtGiro.Text = _proveedorAEditar.Giro;
                txtTelefono.Text = _proveedorAEditar.Telefono;
                txtEmail.Text = _proveedorAEditar.Email;
                txtDireccion.Text = _proveedorAEditar.Direccion;
            }
        }

        private Panel CrearRenglonCampo(string titulo, Control ctrl)
        {
            Panel p = new Panel { Size = new Size(415, 52), Margin = new Padding(0, 0, 0, 4) };
            Label l = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            ctrl.Location = new Point(0, 18);
            p.Controls.Add(l);
            p.Controls.Add(ctrl);
            return p;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            string rut = txtRut.Text.Trim();
            string razonSocial = txtRazonSocial.Text.Trim();

            if (string.IsNullOrWhiteSpace(rut) || string.IsNullOrWhiteSpace(razonSocial))
            {
                MessageBox.Show("El RUT y la Razón Social son campos obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();

                if (_proveedorAEditar == null)
                {
                    // Crear Nuevo
                    var nuevo = new Proveedor
                    {
                        Rut = rut,
                        RazonSocial = razonSocial,
                        Giro = txtGiro.Text.Trim(),
                        Telefono = txtTelefono.Text.Trim(),
                        Email = txtEmail.Text.Trim(),
                        Direccion = txtDireccion.Text.Trim(),
                        Estado = true
                    };

                    db.Proveedores.Add(nuevo);
                    db.SaveChanges();
                    ProveedorResultado = nuevo;
                }
                else
                {
                    // Editar Existente
                    var provBd = db.Proveedores.Find(_proveedorAEditar.ProveedorID);
                    if (provBd != null)
                    {
                        provBd.Rut = rut;
                        provBd.RazonSocial = razonSocial;
                        provBd.Giro = txtGiro.Text.Trim();
                        provBd.Telefono = txtTelefono.Text.Trim();
                        provBd.Email = txtEmail.Text.Trim();
                        provBd.Direccion = txtDireccion.Text.Trim();
                        db.SaveChanges();
                        ProveedorResultado = provBd;
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el proveedor:\n{ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}