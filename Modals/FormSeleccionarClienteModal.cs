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
            this.ClientSize = new Size(410, 520); // ClientSize asegura centrado exacto
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
                    // Si hay más de 1, baja directo al segundo; si hay 1 solo, selecciona el primero
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
                lstSugerencias.DisplayMember = "Nombre";
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
            // Paso 1: Si hay sugerencias desplegadas, selecciona y muestra la ficha SIN cerrar
            if (lstSugerencias.Visible && lstSugerencias.Items.Count > 0)
            {
                var seleccionado = lstSugerencias.SelectedItem as Cliente ?? lstSugerencias.Items[0] as Cliente;
                if (seleccionado != null)
                {
                    txtRutBuscar.Text = RutHelper.Formatear(seleccionado.Rut);
                    MostrarInformacionCliente(seleccionado);
                    lstSugerencias.Visible = false;
                    return; // No se cierra: muestra la info y pasa el foco al botón azul
                }
            }

            // Paso 2: Si ya está la ficha del cliente visible, el Enter confirma la venta
            if (pnlInfoCliente.Visible && !string.IsNullOrEmpty(RutCliente))
            {
                ConfirmarYSalir();
                return;
            }

            // Paso 3: Si no estaba seleccionando sugerencias, busca directo
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
                AbrirModalRegistrarNuevoCliente(txtRutBuscar.Text);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void MostrarInformacionCliente(Cliente cliente)
        {
            NombreCliente = cliente.Nombre ?? "Cliente Sin Nombre";
            RutCliente = RutHelper.Formatear(cliente.Rut ?? txtRutBuscar.Text);

            lblNombreCliente.Text = NombreCliente;
            lblEstadoCliente.Text = "Status: " + (cliente.Estado ? "✅ Activo" : "❌ Inactivo");
            lblEstadoCliente.ForeColor = cliente.Estado ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

            CargarUltimasDosCompras(cliente.Rut);

            pnlInfoCliente.Visible = true;
            btnConfirmar.Text = "✔ Asignar Cliente (Enter)";
            btnConfirmar.Enabled = true;
            btnConfirmar.Focus(); // Pasa el foco al botón para que el siguiente Enter confirme
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
                ClientSize = new Size(370, 470),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            const int mX = 20;
            const int aW = 330;

            Label lblTitle = new Label { Text = "👤 Nuevo Cliente", Location = new Point(mX, 15), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblRut = new Label { Text = "RUT / DNI:", Location = new Point(mX, 55), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtRut = new TextBox { Text = RutHelper.Formatear(rutInicial), Location = new Point(mX, 75), Size = new Size(aW, 26), Font = new Font("Segoe UI", 9.5F), MaxLength = 12 };

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

            Label lblNom = new Label { Text = "Nombre Completo:", Location = new Point(mX, 115), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtNom = new TextBox { Location = new Point(mX, 135), Size = new Size(aW, 26), Font = new Font("Segoe UI", 9.5F) };

            txtNom.KeyPress += (s, e) =>
            {
                if (!char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != ' ')
                {
                    e.Handled = true;
                }
            };

            Label lblTel = new Label { Text = "Teléfono de Contacto (9 dígitos):", Location = new Point(mX, 175), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtTel = new TextBox { Text = "9", Location = new Point(mX, 195), Size = new Size(aW, 26), Font = new Font("Segoe UI", 9.5F), MaxLength = 9 };

            txtTel.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            Label lblMail = new Label { Text = "Correo Electrónico:", Location = new Point(mX, 235), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtMail = new TextBox { Location = new Point(mX, 255), Size = new Size(aW, 26), Font = new Font("Segoe UI", 9.5F) };

            Label lblDir = new Label { Text = "Dirección:", Location = new Point(mX, 295), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtDir = new TextBox { Location = new Point(mX, 315), Size = new Size(aW, 26), Font = new Font("Segoe UI", 9.5F) };

            Button btnGuardar = new Button
            {
                Text = "💾 Guardar Cliente",
                Location = new Point(mX, 370),
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
                if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtRut.Text) || 
                    string.IsNullOrWhiteSpace(txtTel.Text) || string.IsNullOrWhiteSpace(txtMail.Text) || 
                    string.IsNullOrWhiteSpace(txtDir.Text))
                {
                    MessageBox.Show("Todos los campos son obligatorios para registrar al cliente.", "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!RutHelper.EsValidoFormato(txtRut.Text))
                {
                    MessageBox.Show("El RUT ingresado no cumple con el formato requerido en Chile.", "RUT Incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (txtTel.Text.Trim().Length != 9)
                {
                    MessageBox.Show("El teléfono debe contener exactamente 9 dígitos (ej. 912345678).", "Teléfono Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!txtMail.Text.Contains("@") || !txtMail.Text.Contains("."))
                {
                    MessageBox.Show("Ingrese un correo electrónico válido (debe contener '@' y un dominio).", "Correo Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    Cliente nuevoCliente = new Cliente
                    {
                        Rut = RutHelper.Limpiar(txtRut.Text),
                        Nombre = txtNom.Text.Trim(),
                        Telefono = txtTel.Text.Trim(),
                        Email = txtMail.Text.Trim(),
                        Direccion = txtDir.Text.Trim(),
                        Estado = true
                    };

                    _clienteService.GuardarCliente(nuevoCliente, esNuevo: true);

                    MessageBox.Show("¡Cliente guardado con éxito!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    NombreCliente = nuevoCliente.Nombre;
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

            modalNuevo.Controls.AddRange(new Control[] { lblTitle, lblRut, txtRut, lblNom, txtNom, lblTel, txtTel, lblMail, txtMail, lblDir, txtDir, btnGuardar });
            modalNuevo.ShowDialog(this);
        }
    }
}