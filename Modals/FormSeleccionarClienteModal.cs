using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormSeleccionarClienteModal : Form
    {
        private readonly ClienteService _clienteService = new ClienteService();

        public string NombreCliente { get; private set; } = "Consumidor Final";
        public string RutCliente { get; private set; } = "";

        private TextBox txtRutBuscar = null!;
        private ListBox lstSugerencias = null!;
        private Panel pnlInfoCliente = null!;
        private Label lblNombreCliente = null!;
        private Label lblEstadoCliente = null!;
        private FlowLayoutPanel flpUltimasCompras = null!;
        private Button btnConfirmar = null!;

        public FormSeleccionarClienteModal(string rutInicial = "")
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(rutInicial))
            {
                txtRutBuscar.Text = RutHelper.Formatear(rutInicial);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Asignar Cliente a la Venta";
            this.ClientSize = new Size(410, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = true;
            this.BackColor = Color.White;
            this.KeyPreview = true;

            const int margenX = 18;
            const int anchoControles = 374;

            Label lblT = new Label 
            { 
                Text = "👤 Buscar Cliente", 
                Location = new Point(margenX, 15), 
                Font = new Font("Segoe UI", 12F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true 
            };

            Label lblRut = new Label 
            { 
                Text = "RUT / DNI Cliente:", 
                Location = new Point(margenX, 50), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            
            txtRutBuscar = new TextBox 
            { 
                Location = new Point(margenX, 72), 
                Size = new Size(anchoControles, 26), 
                Font = new Font("Segoe UI", 10.5F), 
                MaxLength = 12 
            };
            
            txtRutBuscar.TextChanged += (s, e) =>
            {
                string rutLimpio = RutHelper.Limpiar(txtRutBuscar.Text);
                if (rutLimpio.Length > 9) rutLimpio = rutLimpio.Substring(0, 9);

                string formateado = RutHelper.Formatear(rutLimpio);
                if (txtRutBuscar.Text != formateado)
                {
                    txtRutBuscar.Text = formateado;
                    txtRutBuscar.SelectionStart = txtRutBuscar.Text.Length;
                }

                EjecutarBusquedaInteligente();
            };

            txtRutBuscar.KeyDown += (s, e) => 
            { 
                if (e.KeyCode == Keys.Down && lstSugerencias.Visible && lstSugerencias.Items.Count > 0)
                {
                    lstSugerencias.Focus();
                    lstSugerencias.SelectedIndex = lstSugerencias.Items.Count > 1 ? 1 : 0;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Enter) 
                { 
                    ProcesarSeleccionEnter(); 
                    e.SuppressKeyPress = true; 
                } 
                else if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            lstSugerencias = new ListBox
            {
                Location = new Point(margenX, 102),
                Size = new Size(anchoControles, 95),
                Font = new Font("Segoe UI", 9.5F),
                Visible = false
            };

            lstSugerencias.Click += (s, e) => SeleccionarDesdeSugerencias();
            lstSugerencias.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    SeleccionarDesdeSugerencias();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Up && lstSugerencias.SelectedIndex == 0)
                {
                    txtRutBuscar.Focus();
                    txtRutBuscar.SelectionStart = txtRutBuscar.Text.Length;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    lstSugerencias.Visible = false;
                    txtRutBuscar.Focus();
                    e.SuppressKeyPress = true;
                }
            };

            pnlInfoCliente = new Panel
            {
                Location = new Point(margenX, 105),
                Size = new Size(anchoControles, 265),
                BackColor = Color.FromArgb(248, 250, 252),
                Visible = false
            };
            pnlInfoCliente.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlInfoCliente.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            lblNombreCliente = new Label { Location = new Point(12, 10), Size = new Size(350, 22), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
            lblEstadoCliente = new Label { Location = new Point(12, 34), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };

            Label lblHistorialHeader = new Label { Text = "🕒 ÚLTIMAS COMPRAS", Location = new Point(12, 60), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };

            flpUltimasCompras = new FlowLayoutPanel
            {
                Location = new Point(12, 80),
                Size = new Size(350, 175),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            pnlInfoCliente.Controls.Add(lblNombreCliente);
            pnlInfoCliente.Controls.Add(lblEstadoCliente);
            pnlInfoCliente.Controls.Add(lblHistorialHeader);
            pnlInfoCliente.Controls.Add(flpUltimasCompras);

            btnConfirmar = new Button
            {
                Text = "🔍 Ingrese RUT para buscar",
                Location = new Point(margenX, 385),
                Size = new Size(anchoControles, 42),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += (s, e) => ConfirmarYSalir();

            Button btnAnonimo = new Button
            {
                Text = "👤 Consumidor Final (Sin RUT)",
                Location = new Point(margenX, 435),
                Size = new Size(anchoControles, 34),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAnonimo.FlatAppearance.BorderSize = 0;
            btnAnonimo.Click += (s, e) =>
            {
                NombreCliente = "Consumidor Final";
                RutCliente = "";
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(lblT);
            this.Controls.Add(lblRut);
            this.Controls.Add(txtRutBuscar);
            this.Controls.Add(lstSugerencias);
            this.Controls.Add(pnlInfoCliente);
            this.Controls.Add(btnConfirmar);
            this.Controls.Add(btnAnonimo);
        }

        private void EjecutarBusquedaInteligente()
        {
            string queryLimpia = RutHelper.Limpiar(txtRutBuscar.Text);

            if (queryLimpia.Length < 2)
            {
                lstSugerencias.Visible = false;
                pnlInfoCliente.Visible = false;
                btnConfirmar.Text = "🔍 Ingrese RUT para buscar";
                btnConfirmar.Enabled = false;
                return;
            }

            var resultados = _clienteService.BuscarClientesPredictivo(queryLimpia);
            var directo = _clienteService.BuscarPorRut(queryLimpia);

            if (directo != null)
            {
                MostrarInformacionCliente(directo);
                lstSugerencias.Visible = false;
            }
            else if (resultados.Count > 0)
            {
                lstSugerencias.DataSource = resultados;
                lstSugerencias.DisplayMember = "RazonSocial";
                lstSugerencias.ValueMember = "Rut";
                lstSugerencias.Visible = true;
                lstSugerencias.BringToFront();
                pnlInfoCliente.Visible = false;
                btnConfirmar.Text = "✔ Asignar Cliente";
                btnConfirmar.Enabled = true;
            }
            else
            {
                lstSugerencias.Visible = false;
                pnlInfoCliente.Visible = false;
                btnConfirmar.Text = "➕ Registrar Nuevo Cliente";
                btnConfirmar.Enabled = true;
            }
        }

        private void SeleccionarDesdeSugerencias()
        {
            if (lstSugerencias.SelectedItem is Cliente clienteSel)
            {
                txtRutBuscar.Text = RutHelper.Formatear(clienteSel.Rut);
                MostrarInformacionCliente(clienteSel);
                lstSugerencias.Visible = false;
            }
        }

        private void ProcesarSeleccionEnter()
        {
            if (lstSugerencias.Visible && lstSugerencias.Items.Count > 0)
            {
                var seleccionado = lstSugerencias.SelectedItem as Cliente ?? lstSugerencias.Items[0] as Cliente;
                if (seleccionado != null)
                {
                    txtRutBuscar.Text = RutHelper.Formatear(seleccionado.Rut);
                    MostrarInformacionCliente(seleccionado);
                    lstSugerencias.Visible = false;
                    return;
                }
            }

            if (pnlInfoCliente.Visible && !string.IsNullOrEmpty(RutCliente))
            {
                ConfirmarYSalir();
                return;
            }

            string rutLimpio = RutHelper.Limpiar(txtRutBuscar.Text);
            var clienteBD = _clienteService.BuscarPorRut(rutLimpio);

            if (clienteBD != null)
            {
                MostrarInformacionCliente(clienteBD);
            }
            else
            {
                if (rutLimpio.Length < 8)
                {
                    MessageBox.Show("Para registrar un nuevo cliente, debe ingresar un RUT válido (mínimo 8 dígitos).", "RUT Incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AbrirModalRegistrarNuevoCliente(txtRutBuscar.Text);
            }
        }

        private void ConfirmarYSalir()
        {
            if (btnConfirmar.Text.Contains("Registrar"))
            {
                string rutLimpio = RutHelper.Limpiar(txtRutBuscar.Text);
                if (rutLimpio.Length < 8)
                {
                    MessageBox.Show("Para registrar un nuevo cliente, debe ingresar un RUT válido (mínimo 8 dígitos).", "RUT Incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AbrirModalRegistrarNuevoCliente(txtRutBuscar.Text);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void MostrarInformacionCliente(Cliente cliente)
        {
            NombreCliente = !string.IsNullOrEmpty(cliente.RazonSocial) ? cliente.RazonSocial : "Cliente Sin Nombre";
            RutCliente = RutHelper.Formatear(cliente.Rut ?? txtRutBuscar.Text);

            lblNombreCliente.Text = NombreCliente;
            lblEstadoCliente.Text = "Status: " + (cliente.Estado ? "✅ Activo" : "❌ Inactivo");
            lblEstadoCliente.ForeColor = cliente.Estado ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

            CargarUltimasDosCompras(cliente.Rut);

            pnlInfoCliente.Visible = true;
            btnConfirmar.Text = "✔ Asignar Cliente (Enter)";
            btnConfirmar.Enabled = true;
            btnConfirmar.Focus();
        }

        private void CargarUltimasDosCompras(string rut)
        {
            flpUltimasCompras.Controls.Clear();

            try
            {
                var ultimasVentas = _clienteService.ObtenerUltimasComprasPorRut(rut, 2);

                if (ultimasVentas == null || ultimasVentas.Count == 0)
                {
                    Label lblSinCompras = new Label
                    {
                        Text = "Este cliente no registra compras previas.",
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                        ForeColor = Color.Gray,
                        Size = new Size(330, 30)
                    };
                    flpUltimasCompras.Controls.Add(lblSinCompras);
                    return;
                }

                foreach (var venta in ultimasVentas)
                {
                    Panel cardVenta = new Panel
                    {
                        Size = new Size(330, 58),
                        BackColor = Color.White,
                        Margin = new Padding(0, 0, 0, 6)
                    };
                    cardVenta.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, cardVenta.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

                    Label lblFecha = new Label
                    {
                        Text = $"📅 {venta.FecDoc:dd/MM/yyyy HH:mm} - Ticket N° {venta.nroDTE}",
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(30, 41, 59),
                        Location = new Point(8, 6),
                        AutoSize = true
                    };

                    Label lblMonto = new Label
                    {
                        Text = $"Total: ${venta.Total:N0}",
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(16, 185, 129),
                        Location = new Point(8, 30),
                        AutoSize = true
                    };

                    cardVenta.Controls.Add(lblFecha);
                    cardVenta.Controls.Add(lblMonto);

                    flpUltimasCompras.Controls.Add(cardVenta);
                }
            }
            catch
            {
                Label lblError = new Label { Text = "No se pudo cargar el historial de compras.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 8F, FontStyle.Italic), AutoSize = true };
                flpUltimasCompras.Controls.Add(lblError);
            }
        }

        private void AbrirModalRegistrarNuevoCliente(string rutInicial)
        {
            Form modalNuevo = new Form
            {
                Text = "Registrar Nuevo Cliente",
                ClientSize = new Size(420, 630),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            const int mX = 20;
            const int aW = 380;
            int yPos = 12;

            Label lblTitle = new Label { Text = "👤 Registrar Nuevo Cliente", Location = new Point(mX, yPos), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };
            yPos += 38;

            // RUT
            Label lblRut = new Label { Text = "RUT / DNI Cliente:", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtRut = new TextBox { Text = RutHelper.Formatear(rutInicial), Location = new Point(mX, yPos), Size = new Size(aW, 25), Font = new Font("Segoe UI", 9.5F), MaxLength = 12 };
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
            yPos += 34;

            // Razón Social
            Label lblNom = new Label { Text = "Razón Social / Nombre Completo:", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtNom = new TextBox { Location = new Point(mX, yPos), Size = new Size(aW, 25), Font = new Font("Segoe UI", 9.5F) };
            txtNom.KeyPress += (s, e) =>
            {
                if (!char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '.' && e.KeyChar != '&')
                {
                    e.Handled = true;
                }
            };
            yPos += 34;

            // Giro Comercial
            Label lblGiro = new Label { Text = "Giro Comercial / Actividad Económica:", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtGiro = new TextBox { Location = new Point(mX, yPos), Size = new Size(aW, 25), Font = new Font("Segoe UI", 9.5F), Text = "PARTICULAR" };
            yPos += 34;

            // Teléfono (9 dígitos, empezando con 9)
            Label lblTel = new Label { Text = "Teléfono de Contacto (9 dígitos):", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtTel = new TextBox { Text = "9", Location = new Point(mX, yPos), Size = new Size(aW, 25), Font = new Font("Segoe UI", 9.5F), MaxLength = 9 };
            txtTel.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            yPos += 34;

            // Correo Electrónico
            Label lblMail = new Label { Text = "Correo Electrónico:", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtMail = new TextBox { Location = new Point(mX, yPos), Size = new Size(aW, 25), Font = new Font("Segoe UI", 9.5F) };
            yPos += 34;

            // Dirección
            Label lblDir = new Label { Text = "Dirección:", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtDir = new TextBox { Location = new Point(mX, yPos), Size = new Size(aW, 25), Font = new Font("Segoe UI", 9.5F) };
            yPos += 34;

            // Comuna y Ciudad (Doble columna)
            int anchoMitad = (aW - 10) / 2;
            Label lblComuna = new Label { Text = "Comuna:", Location = new Point(mX, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            Label lblCiudad = new Label { Text = "Ciudad:", Location = new Point(mX + anchoMitad + 10, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            yPos += 18;
            TextBox txtComuna = new TextBox { Location = new Point(mX, yPos), Size = new Size(anchoMitad, 25), Font = new Font("Segoe UI", 9.5F), Text = "SANTIAGO" };
            TextBox txtCiudad = new TextBox { Location = new Point(mX + anchoMitad + 10, yPos), Size = new Size(anchoMitad, 25), Font = new Font("Segoe UI", 9.5F), Text = "SANTIAGO" };
            yPos += 46;

            // Botón Guardar
            Button btnGuardar = new Button
            {
                Text = "💾 Guardar y Asignar Cliente",
                Location = new Point(mX, yPos),
                Size = new Size(aW, 42),
                BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

            btnGuardar.Click += (s, e) =>
            {
                // Validación de campos obligatorios
                if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtRut.Text) || 
                    string.IsNullOrWhiteSpace(txtTel.Text) || string.IsNullOrWhiteSpace(txtMail.Text) || 
                    string.IsNullOrWhiteSpace(txtDir.Text) || string.IsNullOrWhiteSpace(txtComuna.Text) ||
                    string.IsNullOrWhiteSpace(txtCiudad.Text))
                {
                    MessageBox.Show("Todos los campos son obligatorios para registrar al cliente.", "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validación RUT mínimo 8 dígitos y formato chileno
                string rutSinPuntos = RutHelper.Limpiar(txtRut.Text);
                if (rutSinPuntos.Length < 8 || !RutHelper.EsValidoFormato(txtRut.Text))
                {
                    MessageBox.Show("El RUT ingresado no cumple con el formato requerido en Chile (mínimo 8 caracteres).", "RUT Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validación Teléfono 9 dígitos
                if (txtTel.Text.Trim().Length != 9)
                {
                    MessageBox.Show("El teléfono debe contener exactamente 9 dígitos numéricos (ej. 912345678).", "Teléfono Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validación Email con '@' y '.'
                if (!txtMail.Text.Contains("@") || !txtMail.Text.Contains("."))
                {
                    MessageBox.Show("Ingrese un correo electrónico válido (debe contener '@' y un dominio válido).", "Correo Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    Cliente nuevoCliente = new Cliente
                    {
                        Rut = rutSinPuntos,
                        RazonSocial = txtNom.Text.Trim(),
                        Giro = txtGiro.Text.Trim(),
                        Telefono = txtTel.Text.Trim(),
                        Email = txtMail.Text.Trim(),
                        Direccion = txtDir.Text.Trim(),
                        Comuna = txtComuna.Text.Trim(),
                        Ciudad = txtCiudad.Text.Trim(),
                        FormaPago = "CONTADO",
                        ListaPrecioDefecto = 1,
                        CategoriaCliente = "MINORISTA",
                        Estado = true
                    };

                    _clienteService.GuardarCliente(nuevoCliente, esNuevo: true);

                    MessageBox.Show("¡Cliente registrado y guardado con éxito!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    NombreCliente = nuevoCliente.RazonSocial;
                    RutCliente = RutHelper.Formatear(nuevoCliente.Rut);

                    modalNuevo.DialogResult = DialogResult.OK;
                    modalNuevo.Close();

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al registrar el cliente: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modalNuevo.Controls.AddRange(new Control[] 
            { 
                lblTitle, lblRut, txtRut, lblNom, txtNom, lblGiro, txtGiro, 
                lblTel, txtTel, lblMail, txtMail, lblDir, txtDir, 
                lblComuna, lblCiudad, txtComuna, txtCiudad, btnGuardar 
            });
            
            modalNuevo.ShowDialog(this);
        }
    }
}