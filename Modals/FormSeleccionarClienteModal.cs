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

        private TextBox txtRut = null!;
        private Panel pnlInfoCliente = null!;
        private Label lblNombreCliente = null!;
        private Label lblEstadoCliente = null!;
        private Label lblCreditoInfo = null!;
        private Label lblPuntosFidelizacion = null!;
        private Button btnAsignar = null!;
        private Button btnConsumidorFinal = null!;

        public Cliente? ClienteSeleccionado { get; private set; }

        public FormSeleccionarClienteModal(string rutInicial = "")
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(rutInicial))
            {
                txtRut.Text = RutHelper.Formatear(rutInicial);
                BuscarClientePorRut();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Asignar Cliente a la Venta";
            this.Size = new Size(420, 545);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(190, 24, 93),
                Padding = new Padding(12, 10, 12, 10)
            };
            Label lblTitHeader = new Label
            {
                Text = "👤 Asignar Cliente a la Venta",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Dock = DockStyle.Fill
            };
            pnlHeader.Controls.Add(lblTitHeader);

            Label lblRutCap = new Label
            {
                Text = "RUT / DNI Cliente:",
                Location = new Point(24, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            txtRut = new TextBox
            {
                Location = new Point(24, 82),
                Size = new Size(356, 28),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Ej: 15.799.295-3"
            };
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
                BuscarClientePorRut();
            };
            txtRut.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (btnAsignar.Enabled)
                    {
                        btnAsignar.PerformClick();
                        e.SuppressKeyPress = true;
                    }
                }
            };

            pnlInfoCliente = new Panel
            {
                Location = new Point(24, 122),
                Size = new Size(356, 280),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            lblNombreCliente = new Label
            {
                Text = "Ingrese un RUT para consultar",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(10, 10),
                Size = new Size(330, 24)
            };

            lblEstadoCliente = new Label
            {
                Text = "Status: --",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 36),
                AutoSize = true
            };

            lblCreditoInfo = new Label
            {
                Text = "• Crédito: No asignado\n• Cupo Total: $0\n• Deuda Actual: $0\n• Disponible: $0",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(10, 64),
                Size = new Size(330, 170)
            };

            lblPuntosFidelizacion = new Label
            {
                Text = "🎁 Puntos Fidelización: --",
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 58, 237),
                Location = new Point(10, 238),
                Size = new Size(330, 36),
                Visible = false
            };

            pnlInfoCliente.Controls.AddRange(new Control[] { lblNombreCliente, lblEstadoCliente, lblCreditoInfo, lblPuntosFidelizacion });

            btnAsignar = new Button
            {
                Text = "✔ Asignar Cliente (Enter)",
                Location = new Point(24, 412),
                Size = new Size(356, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAsignar.FlatAppearance.BorderSize = 0;
            btnAsignar.Click += BtnAsignar_Click;

            btnConsumidorFinal = new Button
            {
                Text = "👤 Consumidor Final (Sin RUT)",
                Location = new Point(24, 456),
                Size = new Size(356, 32),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConsumidorFinal.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnConsumidorFinal.Click += (s, e) =>
            {
                ClienteSeleccionado = null;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { pnlHeader, lblRutCap, txtRut, pnlInfoCliente, btnAsignar, btnConsumidorFinal });
        }

        private void BuscarClientePorRut()
        {
            string rutLimpio = RutHelper.Limpiar(txtRut.Text);
            if (rutLimpio.Length < 7)
            {
                ClienteSeleccionado = null;
                btnAsignar.Enabled = false;
                btnAsignar.Text = "✔ Asignar Cliente (Enter)";
                btnAsignar.BackColor = Color.FromArgb(37, 99, 235);
                lblNombreCliente.Text = "Ingrese un RUT para consultar";
                lblEstadoCliente.Text = "Status: --";
                lblCreditoInfo.Text = "• Crédito: No asignado\n• Cupo Total: $0\n• Deuda Actual: $0\n• Disponible: $0";
                
                // 1. Ocultar puntos si no hay RUT completo
                if (lblPuntosFidelizacion != null) lblPuntosFidelizacion.Visible = false;
                return;
            }

            try
            {
                var info = _clienteService.ObtenerInfoCrediticiaPorRut(rutLimpio);

                if (info != null)
                {
                    var cli = info.Cliente;
                    ClienteSeleccionado = cli;
                    
                    // Estado: Cliente encontrado -> Botón azul de asignación
                    btnAsignar.Enabled = true;
                    btnAsignar.Text = "✔ Asignar Cliente (Enter)";
                    btnAsignar.BackColor = Color.FromArgb(37, 99, 235);

                    lblNombreCliente.Text = cli.RazonSocial;
                    lblEstadoCliente.Text = $"Status: [ {cli.EstadoCrediticio} ]  -  Categoría: {cli.CategoriaCliente}";
                    lblEstadoCliente.ForeColor = cli.EstadoCrediticio == "MOROSO" ? Color.FromArgb(239, 68, 68) : Color.FromArgb(22, 163, 74);

                    string detalleMora = info.FacturasVencidas > 0
                        ? $"\n⚠️ FACTURAS VENCIDAS: {info.FacturasVencidas} documento(s) impago(s)"
                        : "\n✔ Al día sin documentos vencidos";

                    int diasEfectivos = cli.DiasCreditoHabiles > 0 ? cli.DiasCreditoHabiles : cli.DiasCredito;
                    bool tieneCreditoHabilitado = cli.PermiteCredito || diasEfectivos > 0;

                    string modalidadTexto = (tieneCreditoHabilitado && diasEfectivos > 0)
                        ? $"{diasEfectivos} días hábiles"
                        : "Solo Contado";

                    lblCreditoInfo.Text = $"• Modalidad: {modalidadTexto}\n" +
                                          $"• Cupo Total: ${cli.CupoCredito:N0}\n" +
                                          $"• Deuda Vigente: ${info.DeudaTotal:N0}\n" +
                                          $"• Cupo Disponible: ${info.CupoDisponible:N0}\n" +
                                          $"• Lista de Precios: Lista {cli.ListaPrecioDefecto}" +
                                          detalleMora;

                    // 2. AQUÍ: Consultar y mostrar los puntos reales del cliente
                    if (lblPuntosFidelizacion != null)
                    {
                        var fidelizacionService = new FidelizacionService();
                        var resumen = fidelizacionService.ObtenerResumenPuntos(cli.IdCliente);

                        lblPuntosFidelizacion.Text = $"🎁 PUNTOS DISPONIBLES: {resumen.PuntosDisponibles:N0} pts  (Equiv. ${resumen.EquivalenciaPesosDisponible:N0})\n" +
                                                     $"   Acumulado histórico: {resumen.PuntosHistoricosAcumulados:N0} pts | Canjeados: {resumen.PuntosHistoricosCanjeados:N0} pts";
                        lblPuntosFidelizacion.Visible = true;
                    }
                }
                else
                {
                    ClienteSeleccionado = null;
                    lblNombreCliente.Text = "Cliente no encontrado";
                    lblEstadoCliente.Text = "Status: No registrado";
                    lblEstadoCliente.ForeColor = Color.FromArgb(239, 68, 68);
                    lblCreditoInfo.Text = "El RUT ingresado no pertenece a ningún cliente activo en el sistema.";

                    // 3. AQUÍ: Ocultar si el cliente no existe
                    if (lblPuntosFidelizacion != null) lblPuntosFidelizacion.Visible = false;

                    // Si el RUT ya tiene 8 o 9 dígitos numéricos limpios, activar opción de crear nuevo
                    if (rutLimpio.Length >= 8)
                    {
                        btnAsignar.Enabled = true;
                        btnAsignar.Text = "➕ Registrar Nuevo Cliente";
                        btnAsignar.BackColor = Color.FromArgb(16, 185, 129); // Verde esmeralda
                    }
                    else
                    {
                        btnAsignar.Enabled = false;
                        btnAsignar.Text = "✔ Asignar Cliente (Enter)";
                        btnAsignar.BackColor = Color.FromArgb(37, 99, 235);
                    }
                }
            }
            catch (Exception ex)
            {
                lblCreditoInfo.Text = $"Error: {ex.Message}";
                if (lblPuntosFidelizacion != null) lblPuntosFidelizacion.Visible = false;
            }
        }
        private void BtnAsignar_Click(object? sender, EventArgs e)
        {
            // Caso 1: El cliente ya existe -> Asignar a la venta
            if (ClienteSeleccionado != null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // Caso 2: El cliente no existe -> Abrir modal para crearlo
            string rutLimpio = RutHelper.Limpiar(txtRut.Text);
            if (rutLimpio.Length >= 8)
            {
                Cliente nuevoCliente = new Cliente
                {
                    Rut = rutLimpio
                };

                using (var modalCrear = new FormNuevoClienteModal(nuevoCliente))
                {
                    if (modalCrear.ShowDialog(this) == DialogResult.OK)
                    {
                        // Se consulta el cliente recién creado para asignarlo de inmediato
                        var info = _clienteService.ObtenerInfoCrediticiaPorRut(rutLimpio);
                        if (info != null)
                        {
                            ClienteSeleccionado = info.Cliente;
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                    }
                }
            }
        }
    }
}