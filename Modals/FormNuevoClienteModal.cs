using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormNuevoClienteModal : Form
    {
        private readonly ClienteService _clienteService = new ClienteService();
        private readonly Cliente? _clienteAEditar;

        private TextBox txtRut = null!;
        private TextBox txtRazon = null!;
        private TextBox txtGiro = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtEmail = null!;
        private TextBox txtDireccion = null!;
        private TextBox txtComuna = null!;
        private TextBox txtCiudad = null!;

        // Campos Comerciales y de Crédito
        private ComboBox cbCategoriaTipo = null!;
        private TextBox txtCupoCredito = null!;
        private ComboBox cbDiasCredito = null!;
        private ComboBox cbListaPrecio = null!;

        private Button btnGuardar = null!;
        private Button btnCancelar = null!;

        public FormNuevoClienteModal(Cliente? cliente = null)
        {
            _clienteAEditar = cliente;
            InitializeComponent();
            CargarDatosSiEsEdicion();
        }

        private void InitializeComponent()
        {
            bool esEdicion = _clienteAEditar != null;
            this.Text = esEdicion ? "Editar Datos del Cliente" : "Registrar Nuevo Cliente";
            this.ClientSize = new Size(540, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = true;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            const int mX = 28;
            const int aW = 484;           // Ancho completo disponible
            const int colW = 236;         // Ancho para campos en doble columna
            const int col2X = mX + colW + 12; // Posición X de la segunda columna
            int yPos = 18;

            // Encabezado
            Label lblTitle = new Label
            {
                Text = esEdicion ? "✏️ Editar Cliente" : "👤 Registrar Nuevo Cliente",
                Location = new Point(mX, yPos),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            yPos += 42;

            // 1. RUT (Columna 1) y Giro Comercial (Columna 2)
            Label lblRut = CrearLabel("RUT / DNI Cliente:", mX, yPos);
            Label lblGiro = CrearLabel("Giro Comercial / Actividad:", col2X, yPos);
            yPos += 20;

            txtRut = new TextBox { Location = new Point(mX, yPos), Size = new Size(colW, 27), Font = new Font("Segoe UI", 9.5F), MaxLength = 12 };
            txtRut.TextChanged += (s, e) =>
            {
                string limpio = RutHelper.Limpiar(txtRut.Text);
                if (limpio.Length > 9) limpio = limpio.Substring(0, 9);
                string fmt = RutHelper.Formatear(limpio);
                if (txtRut.Text != fmt)
                {
                    txtRut.Text = fmt;
                    txtRut.SelectionStart = txtRut.Text.Length;
                }
            };

            txtGiro = new TextBox { Location = new Point(col2X, yPos), Size = new Size(colW, 27), Font = new Font("Segoe UI", 9.5F), Text = "PARTICULAR" };
            yPos += 38;

            // 2. Razón Social / Nombre Completo (Ancho completo)
            Label lblRazon = CrearLabel("Razón Social / Nombre Completo:", mX, yPos);
            yPos += 20;
            txtRazon = new TextBox { Location = new Point(mX, yPos), Size = new Size(aW, 27), Font = new Font("Segoe UI", 9.5F) };
            txtRazon.KeyPress += (s, e) =>
            {
                if (!char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '.' && e.KeyChar != '&')
                {
                    e.Handled = true;
                }
            };
            yPos += 38;

            // 3. Teléfono y Correo Electrónico (Doble Columna)
            Label lblTel = CrearLabel("Teléfono (9 dígitos):", mX, yPos);
            Label lblMail = CrearLabel("Correo Electrónico:", col2X, yPos);
            yPos += 20;

            txtTelefono = new TextBox { Text = "9", Location = new Point(mX, yPos), Size = new Size(colW, 27), Font = new Font("Segoe UI", 9.5F), MaxLength = 9 };
            txtTelefono.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            txtEmail = new TextBox { Location = new Point(col2X, yPos), Size = new Size(colW, 27), Font = new Font("Segoe UI", 9.5F) };
            yPos += 38;

            // 4. Dirección (Ancho completo)
            Label lblDir = CrearLabel("Dirección Particular / Comercial:", mX, yPos);
            yPos += 20;
            txtDireccion = new TextBox { Location = new Point(mX, yPos), Size = new Size(aW, 27), Font = new Font("Segoe UI", 9.5F) };
            yPos += 38;

            // 5. Comuna y Ciudad (Doble Columna)
            Label lblComuna = CrearLabel("Comuna:", mX, yPos);
            Label lblCiudad = CrearLabel("Ciudad:", col2X, yPos);
            yPos += 20;

            txtComuna = new TextBox { Location = new Point(mX, yPos), Size = new Size(colW, 27), Font = new Font("Segoe UI", 9.5F), Text = "SANTIAGO" };
            txtCiudad = new TextBox { Location = new Point(col2X, yPos), Size = new Size(colW, 27), Font = new Font("Segoe UI", 9.5F), Text = "SANTIAGO" };
            yPos += 38;

            // 6. Tipo/Categoría y Cupo de Crédito (Doble Columna)
            Label lblTipo = CrearLabel("Tipo de Cliente (Categoría):", mX, yPos);
            Label lblCupo = CrearLabel("Cupo de Crédito ($):", col2X, yPos);
            yPos += 20;

            cbCategoriaTipo = new ComboBox
            {
                Location = new Point(mX, yPos),
                Size = new Size(colW, 27),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbCategoriaTipo.Items.AddRange(new object[] { "MINORISTA", "MAYOR.(A)", "MAYOR.(B)", "RUTEROS", "PERSONAL" });
            cbCategoriaTipo.SelectedIndex = 0;

            txtCupoCredito = new TextBox
            {
                Location = new Point(col2X, yPos),
                Size = new Size(colW, 27),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Text = "0"
            };
            txtCupoCredito.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            yPos += 38;

            // 7. Días de Crédito y Lista de Precios Asignada (Doble Columna)
            Label lblDias = CrearLabel("Días de Crédito (Plazo):", mX, yPos);
            Label lblLista = CrearLabel("Lista de Precios Asignada:", col2X, yPos);
            yPos += 20;

            cbDiasCredito = new ComboBox
            {
                Location = new Point(mX, yPos),
                Size = new Size(colW, 27),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbDiasCredito.Items.AddRange(new object[]
            {
                "0 días (Contado)",
                "7 días",
                "15 días",
                "30 días",
                "45 días",
                "60 días"
            });
            cbDiasCredito.SelectedIndex = 0;

            cbListaPrecio = new ComboBox
            {
                Location = new Point(col2X, yPos),
                Size = new Size(colW, 27),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbListaPrecio.Items.AddRange(new object[]
            {
                "Lista 1 (Público General)",
                "Lista 2 (Clientes Frecuentes)",
                "Lista 3 (Mayorista 2)",
                "Lista 4 (Distribuidor)",
                "Lista 5 (Liquidación / Especial)",
                "Lista 6", "Lista 7", "Lista 8", "Lista 9", "Lista 10"
            });
            cbListaPrecio.SelectedIndex = 0;
            yPos += 54;

            // Botonera Inferior
            btnGuardar = new Button
            {
                Text = esEdicion ? "💾 Guardar Cambios" : "💾 Guardar Cliente",
                Location = new Point(col2X, yPos),
                Size = new Size(colW, 42),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Location = new Point(mX, yPos),
                Size = new Size(colW, 42),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblRut, txtRut, lblGiro, txtGiro,
                lblRazon, txtRazon,
                lblTel, txtTelefono, lblMail, txtEmail,
                lblDir, txtDireccion,
                lblComuna, txtComuna, lblCiudad, txtCiudad,
                lblTipo, cbCategoriaTipo, lblCupo, txtCupoCredito,
                lblDias, cbDiasCredito, lblLista, cbListaPrecio,
                btnCancelar, btnGuardar
            });
        }

        private Label CrearLabel(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
        }

        private void CargarDatosSiEsEdicion()
        {
            if (_clienteAEditar != null)
            {
                txtRut.Text = RutHelper.Formatear(_clienteAEditar.Rut);
                txtRazon.Text = _clienteAEditar.RazonSocial;
                txtGiro.Text = !string.IsNullOrWhiteSpace(_clienteAEditar.Giro) ? _clienteAEditar.Giro : "PARTICULAR";
                txtTelefono.Text = _clienteAEditar.Telefono;
                txtEmail.Text = _clienteAEditar.Email;
                txtDireccion.Text = _clienteAEditar.Direccion;
                txtComuna.Text = !string.IsNullOrWhiteSpace(_clienteAEditar.Comuna) ? _clienteAEditar.Comuna : "SANTIAGO";
                txtCiudad.Text = !string.IsNullOrWhiteSpace(_clienteAEditar.Ciudad) ? _clienteAEditar.Ciudad : "SANTIAGO";

                // Categoría / Tipo
                string cat = string.IsNullOrWhiteSpace(_clienteAEditar.CategoriaCliente) ? "MINORISTA" : _clienteAEditar.CategoriaCliente.Trim();
                int idxCat = cbCategoriaTipo.Items.IndexOf(cat);
                cbCategoriaTipo.SelectedIndex = idxCat >= 0 ? idxCat : 0;

                // Cupo de Crédito
                txtCupoCredito.Text = _clienteAEditar.CupoCredito.ToString("0");

                // Días de Crédito
                int dias = _clienteAEditar.DiasCredito;
                cbDiasCredito.SelectedIndex = dias switch
                {
                    7 => 1,
                    15 => 2,
                    30 => 3,
                    45 => 4,
                    60 => 5,
                    _ => 0
                };

                // Lista de Precios (1 a 10)
                int listaIdx = Math.Max(1, Math.Min(10, _clienteAEditar.ListaPrecioDefecto)) - 1;
                cbListaPrecio.SelectedIndex = listaIdx;
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRazon.Text) || string.IsNullOrWhiteSpace(txtRut.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtDireccion.Text) || string.IsNullOrWhiteSpace(txtComuna.Text) ||
                string.IsNullOrWhiteSpace(txtCiudad.Text))
            {
                MessageBox.Show("Todos los campos principales son obligatorios.", "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rutLimpio = RutHelper.Limpiar(txtRut.Text);
            if (rutLimpio.Length < 8 || !RutHelper.EsValidoFormato(txtRut.Text))
            {
                MessageBox.Show("El RUT ingresado no cumple con el formato requerido (mínimo 8 caracteres).", "RUT Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtTelefono.Text.Trim().Length != 9)
            {
                MessageBox.Show("El teléfono debe contener exactamente 9 dígitos numéricos.", "Teléfono Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
            {
                MessageBox.Show("Ingrese un correo electrónico válido (debe contener '@' y un dominio).", "Correo Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal.TryParse(txtCupoCredito.Text.Trim(), out decimal cupo);

            int diasCredito = cbDiasCredito.SelectedIndex switch
            {
                1 => 7,
                2 => 15,
                3 => 30,
                4 => 45,
                5 => 60,
                _ => 0
            };

            string formaPago = diasCredito > 0 ? $"CREDITO{diasCredito}" : "CONTADO";

            try
            {
                bool esNuevo = (_clienteAEditar == null);
                Cliente clienteGuardar = _clienteAEditar ?? new Cliente();

                clienteGuardar.Rut = rutLimpio;
                clienteGuardar.RazonSocial = txtRazon.Text.Trim();
                clienteGuardar.Giro = txtGiro.Text.Trim();
                clienteGuardar.Telefono = txtTelefono.Text.Trim();
                clienteGuardar.Email = txtEmail.Text.Trim();
                clienteGuardar.Direccion = txtDireccion.Text.Trim();
                clienteGuardar.Comuna = txtComuna.Text.Trim();
                clienteGuardar.Ciudad = txtCiudad.Text.Trim();
                
                // Parámetros comerciales
                clienteGuardar.CategoriaCliente = cbCategoriaTipo.SelectedItem?.ToString() ?? "MINORISTA";
                clienteGuardar.CupoCredito = cupo;
                clienteGuardar.DiasCredito = diasCredito;
                clienteGuardar.FormaPago = formaPago;
                clienteGuardar.ListaPrecioDefecto = cbListaPrecio.SelectedIndex + 1;
                clienteGuardar.Estado = true;

                _clienteService.GuardarCliente(clienteGuardar, esNuevo);

                MessageBox.Show("Cliente guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cliente: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}