using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormConfiguracion : Form
    {
        private TextBox txtRut = null!;
        private TextBox txtRazonSocial = null!;
        private TextBox txtGiro = null!;
        private TextBox txtDireccion = null!;
        private TextBox txtComuna = null!;
        private TextBox txtCiudad = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtEmail = null!;
        private TextBox txtUnidadSII = null!;
        private TextBox txtResolucionSII = null!;
        private TextBox txtTextoPie = null!;
        private Button btnGuardar = null!;

        public FormConfiguracion()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuración General de la Empresa";
            this.Size = new Size(680, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "⚙️ DATOS DEL NEGOCIO / EMISOR FISCAL",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 18)
            };
            pnlHeader.Controls.Add(lblTitulo);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                AutoScroll = true
            };

            int y = 15;
            CrearCampo(pnlBody, "RUT Emisor:", ref txtRut, ref y, "76.543.210-K");
            CrearCampo(pnlBody, "Razón Social / Nombre Comercial:", ref txtRazonSocial, ref y, "Mi Negocio SpA");
            CrearCampo(pnlBody, "Giro Comercial:", ref txtGiro, ref y, "Venta al por menor");
            CrearCampo(pnlBody, "Dirección (Casa Matriz):", ref txtDireccion, ref y, "Av. Siempre Viva 123");
            CrearCampo(pnlBody, "Comuna:", ref txtComuna, ref y, "Santiago");
            CrearCampo(pnlBody, "Ciudad:", ref txtCiudad, ref y, "Santiago");
            CrearCampo(pnlBody, "Teléfono de Contacto:", ref txtTelefono, ref y, "+56 9 1234 5678");
            CrearCampo(pnlBody, "Correo Electrónico:", ref txtEmail, ref y, "contacto@empresa.cl");
            CrearCampo(pnlBody, "Unidad S.I.I. Correspondiente:", ref txtUnidadSII, ref y, "S.I.I. - SANTIAGO CENTRO");
            CrearCampo(pnlBody, "Resolución S.I.I. (N° y Año):", ref txtResolucionSII, ref y, "Res. 99 de 2026");
            CrearCampo(pnlBody, "Mensaje al Pie del Ticket:", ref txtTextoPie, ref y, "¡Gracias por su preferencia!");

            btnGuardar = new Button
            {
                Text = "💾 Guardar Parámetros de Empresa",
                Location = new Point(220, y + 10),
                Size = new Size(380, 42),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;
            pnlBody.Controls.Add(btnGuardar);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlHeader);
        }

        private void CrearCampo(Panel pnl, string labelText, ref TextBox txt, ref int y, string placeholder)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(15, y + 3),
                Size = new Size(190, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txt = new TextBox
            {
                Location = new Point(220, y),
                Size = new Size(380, 26),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = placeholder
            };

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            y += 35;
        }

        private void CargarDatos()
        {
            using var db = new AppDbContext();
            var emp = db.ConfiguracionEmpresa.FirstOrDefault(e => e.EmpresaID == 1);
            if (emp != null)
            {
                txtRut.Text = emp.Rut;
                txtRazonSocial.Text = emp.RazonSocial;
                txtGiro.Text = emp.Giro;
                txtDireccion.Text = emp.Direccion;
                txtComuna.Text = emp.Comuna;
                txtCiudad.Text = emp.Ciudad;
                txtTelefono.Text = emp.Telefono;
                txtEmail.Text = emp.Email ?? "";
                txtUnidadSII.Text = emp.UnidadSII;
                txtResolucionSII.Text = emp.ResolucionSII;
                txtTextoPie.Text = emp.TextoPieTicket;
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var emp = db.ConfiguracionEmpresa.FirstOrDefault(e => e.EmpresaID == 1);
                if (emp == null)
                {
                    emp = new ConfiguracionEmpresa { EmpresaID = 1 };
                    db.ConfiguracionEmpresa.Add(emp);
                }

                emp.Rut = txtRut.Text.Trim();
                emp.RazonSocial = txtRazonSocial.Text.Trim();
                emp.Giro = txtGiro.Text.Trim();
                emp.Direccion = txtDireccion.Text.Trim();
                emp.Comuna = txtComuna.Text.Trim();
                emp.Ciudad = txtCiudad.Text.Trim();
                emp.Telefono = txtTelefono.Text.Trim();
                emp.Email = txtEmail.Text.Trim();
                emp.UnidadSII = txtUnidadSII.Text.Trim();
                emp.ResolucionSII = txtResolucionSII.Text.Trim();
                emp.TextoPieTicket = txtTextoPie.Text.Trim();

                db.SaveChanges();
                MessageBox.Show("Datos de la empresa guardados correctamente.\nLos nuevos tickets saldrán con esta información.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}