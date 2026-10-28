using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private ComboBox cbSucursales = null!;
        private Label lblSucursalesAviso = null!;

        private Panel pnlInfoCliente = null!;
        private Label lblNombreCliente = null!;
        private Label lblEstadoCliente = null!;
        private Label lblCreditoInfo = null!;
        private Label lblPuntosFidelizacion = null!;
        private Button btnAsignar = null!;
        private Button btnNuevaSucursal = null!;
        private Button btnConsumidorFinal = null!;

        private List<Cliente> _clientesEncontrados = new List<Cliente>();
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
            this.Size = new Size(420, 580);
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
                Location = new Point(24, 55),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            txtRut = new TextBox
            {
                Location = new Point(24, 75),
                Size = new Size(356, 26),
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
                if (e.KeyCode == Keys.Enter && btnAsignar.Enabled)
                {
                    btnAsignar.PerformClick();
                    e.SuppressKeyPress = true;
                }
            };

            // Selector para locales o sucursales con el mismo RUT
            lblSucursalesAviso = new Label
            {
                Text = "🏢 Seleccione Sucursal / Razón:",
                Location = new Point(24, 106),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Visible = false
            };

            cbSucursales = new ComboBox
            {
                Location = new Point(24, 124),
                Size = new Size(356, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            cbSucursales.SelectedIndexChanged += (s, e) =>
            {
                if (cbSucursales.SelectedIndex >= 0 && cbSucursales.SelectedIndex < _clientesEncontrados.Count)
                {
                    CargarFichaCliente(_clientesEncontrados[cbSucursales.SelectedIndex]);
                }
            };
            cbSucursales.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && btnAsignar.Enabled)
                {
                    btnAsignar.PerformClick();
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            };

            pnlInfoCliente = new Panel
            {
                Location = new Point(24, 115),
                Size = new Size(356, 265),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            lblNombreCliente = new Label
            {
                Text = "Ingrese un RUT para consultar",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(10, 8),
                Size = new Size(330, 24)
            };

            lblEstadoCliente = new Label
            {
                Text = "Status: --",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 32),
                AutoSize = true
            };

            lblCreditoInfo = new Label
            {
                Text = "• Crédito: No asignado\n• Cupo Total: $0\n• Deuda Actual: $0\n• Disponible: $0",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(10, 58),
                Size = new Size(330, 140)
            };

            lblPuntosFidelizacion = new Label
            {
                Text = "🎁 Puntos Fidelización: --",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 58, 237),
                Location = new Point(10, 205),
                Size = new Size(330, 48),
                Visible = false
            };

            pnlInfoCliente.Controls.AddRange(new Control[] { lblNombreCliente, lblEstadoCliente, lblCreditoInfo, lblPuntosFidelizacion });

            // =========================================================================
            // FILA DE ACCIONES (Posiciones y anchos fijos para evitar deformaciones)
            // =========================================================================
            btnAsignar = new Button
            {
                Text = "✔ Asignar Cliente (Enter)",
                Location = new Point(24, 435),
                Size = new Size(245, 38), // Tamaño fijo predeterminado
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAsignar.FlatAppearance.BorderSize = 0;
            btnAsignar.Click += BtnAsignar_Click;

            btnNuevaSucursal = new Button
            {
                Text = "➕ Sucursal",
                Location = new Point(275, 435),
                Size = new Size(105, 38),
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(22, 101, 52),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnNuevaSucursal.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnNuevaSucursal.Click += BtnNuevaSucursal_Click;

            btnConsumidorFinal = new Button
            {
                Text = "👤 Consumidor Final (Sin RUT)",
                Location = new Point(24, 480),
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

            this.Controls.AddRange(new Control[] { pnlHeader, lblRutCap, txtRut, lblSucursalesAviso, cbSucursales, pnlInfoCliente, btnAsignar, btnNuevaSucursal, btnConsumidorFinal });
            this.AcceptButton = btnAsignar;
        }

        private void BuscarClientePorRut()
        {
            string rutLimpio = RutHelper.Limpiar(txtRut.Text);
            if (rutLimpio.Length < 7)
            {
                LimpiarVistaInfo();
                return;
            }

            try
            {
                using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
                _clientesEncontrados = db.Clientes
                    .Where(c => c.Rut == rutLimpio && c.Estado)
                    .OrderBy(c => c.RazonSocial)
                    .ToList();

                if (_clientesEncontrados.Count == 0)
                {
                    ClienteSeleccionado = null;
                    lblSucursalesAviso.Visible = false;
                    cbSucursales.Visible = false;
                    pnlInfoCliente.Location = new Point(24, 115);

                    lblNombreCliente.Text = "Cliente no encontrado";
                    lblEstadoCliente.Text = "Status: No registrado";
                    lblEstadoCliente.ForeColor = Color.FromArgb(239, 68, 68);
                    lblCreditoInfo.Text = "El RUT ingresado no pertenece a ningún cliente activo en el sistema.";
                    lblPuntosFidelizacion.Visible = false;

                    // Si el RUT tiene 8 o 9 dígitos numéricos, habilita crear el primero
                    if (rutLimpio.Length >= 8)
                    {
                        btnAsignar.Location = new Point(24, 435);
                        btnAsignar.Size = new Size(356, 38); // Ancho completo
                        btnAsignar.Enabled = true;
                        btnAsignar.Text = "➕ Registrar Nuevo Cliente";
                        btnAsignar.BackColor = Color.FromArgb(16, 185, 129); // Verde
                        btnNuevaSucursal.Visible = false;
                    }
                    else
                    {
                        btnAsignar.Location = new Point(24, 435);
                        btnAsignar.Size = new Size(356, 38);
                        btnAsignar.Enabled = false;
                        btnAsignar.Text = "✔ Asignar Cliente (Enter)";
                        btnAsignar.BackColor = Color.FromArgb(37, 99, 235);
                        btnNuevaSucursal.Visible = false;
                    }
                }
                else
                {
                    // Si ya existe al menos 1 cliente con este RUT:
                    // 1. Botón Asignar vuelve a su tamaño normal a la izquierda
                    btnAsignar.Location = new Point(24, 435);
                    btnAsignar.Size = new Size(245, 38);
                    btnAsignar.Enabled = true;
                    btnAsignar.Text = "✔ Asignar Cliente (Enter)";
                    btnAsignar.BackColor = Color.FromArgb(37, 99, 235);

                    // 2. Se activa siempre el botón de agregar otra sucursal a la derecha
                    btnNuevaSucursal.Visible = true;

                    if (_clientesEncontrados.Count == 1)
                    {
                        lblSucursalesAviso.Visible = false;
                        cbSucursales.Visible = false;
                        pnlInfoCliente.Location = new Point(24, 115);
                        pnlInfoCliente.Height = 300;

                        CargarFichaCliente(_clientesEncontrados[0]);
                    }
                    else
                    {
                        // Si hay 2 o más sucursales registradas
                        lblSucursalesAviso.Visible = true;
                        cbSucursales.Visible = true;
                        pnlInfoCliente.Location = new Point(24, 158);
                        pnlInfoCliente.Height = 260;

                        cbSucursales.Items.Clear();
                        foreach (var c in _clientesEncontrados)
                        {
                            string sucursal = !string.IsNullOrWhiteSpace(c.Comuna) ? $"{c.RazonSocial} ({c.Comuna})" : c.RazonSocial;
                            cbSucursales.Items.Add(sucursal);
                        }
                        cbSucursales.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                lblCreditoInfo.Text = $"Error: {ex.Message}";
                lblPuntosFidelizacion.Visible = false;
            }
        }

        private void CargarFichaCliente(Cliente cli)
        {
            ClienteSeleccionado = cli;

            lblNombreCliente.Text = cli.RazonSocial;
            lblEstadoCliente.Text = $"Status: [ {cli.EstadoCrediticio} ]  -  Categoría: {cli.CategoriaCliente}";
            lblEstadoCliente.ForeColor = cli.EstadoCrediticio == "MOROSO" ? Color.FromArgb(239, 68, 68) : Color.FromArgb(22, 163, 74);

            var info = _clienteService.ObtenerInfoCrediticiaPorClienteId(cli.IdCliente);

            string detalleMora = (info != null && info.FacturasVencidas > 0)
                ? $"\n⚠️ FACTURAS VENCIDAS: {info.FacturasVencidas} documento(s) impago(s)"
                : "\n✔ Al día sin documentos vencidos";

            int diasEfectivos = cli.DiasCreditoHabiles > 0 ? cli.DiasCreditoHabiles : cli.DiasCredito;
            bool tieneCreditoHabilitado = cli.PermiteCredito || diasEfectivos > 0;

            string modalidadTexto = (tieneCreditoHabilitado && diasEfectivos > 0)
                ? $"{diasEfectivos} días hábiles"
                : "Solo Contado";

            decimal deuda = info?.DeudaTotal ?? 0m;
            decimal cupoDisp = info?.CupoDisponible ?? cli.CupoCredito;

            lblCreditoInfo.Text = $"• Modalidad: {modalidadTexto}\n" +
                                  $"• Cupo Total: ${cli.CupoCredito:N0}\n" +
                                  $"• Deuda Vigente: ${deuda:N0}\n" +
                                  $"• Cupo Disponible: ${cupoDisp:N0}\n" +
                                  $"• Lista de Precios: Lista {cli.ListaPrecioDefecto}" +
                                  detalleMora;

            var fidelizacionService = new FidelizacionService();
            var resumen = fidelizacionService.ObtenerResumenPuntos(cli.IdCliente);

            lblPuntosFidelizacion.Text = $"🎁 PUNTOS DISPONIBLES: {resumen.PuntosDisponibles:N0} pts (Equiv. ${resumen.EquivalenciaPesosDisponible:N0})\n" +
                                         $"   Acumulado: {resumen.PuntosHistoricosAcumulados:N0} pts | Canjeados: {resumen.PuntosHistoricosCanjeados:N0} pts";
            lblPuntosFidelizacion.Visible = true;
        }

        private void LimpiarVistaInfo()
        {
            ClienteSeleccionado = null;
            btnAsignar.Location = new Point(24, 435);
            btnAsignar.Size = new Size(356, 38);
            btnAsignar.Enabled = false;
            btnAsignar.Text = "✔ Asignar Cliente (Enter)";
            btnAsignar.BackColor = Color.FromArgb(37, 99, 235);
            btnNuevaSucursal.Visible = false;

            lblSucursalesAviso.Visible = false;
            cbSucursales.Visible = false;
            pnlInfoCliente.Location = new Point(24, 115);
            pnlInfoCliente.Height = 300;

            lblNombreCliente.Text = "Ingrese un RUT para consultar";
            lblEstadoCliente.Text = "Status: --";
            lblCreditoInfo.Text = "• Crédito: No asignado\n• Cupo Total: $0\n• Deuda Actual: $0\n• Disponible: $0";
            lblPuntosFidelizacion.Visible = false;
        }

        private void BtnAsignar_Click(object? sender, EventArgs e)
        {
            // Caso 1: El cliente ya está seleccionado en pantalla -> Asignar al POS
            if (ClienteSeleccionado != null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // Caso 2: El cliente no existía y se presionó "➕ Registrar Nuevo Cliente"
            string rutLimpio = RutHelper.Limpiar(txtRut.Text);
            if (rutLimpio.Length >= 8)
            {
                Cliente nuevoCliente = new Cliente { Rut = rutLimpio };

                using var modalCrear = new FormNuevoClienteModal(nuevoCliente);
                if (modalCrear.ShowDialog(this) == DialogResult.OK && modalCrear.ClienteGuardado != null)
                {
                    // 👇 Se asigna automáticamente el nuevo cliente y se cierra el modal hacia el POS
                    ClienteSeleccionado = modalCrear.ClienteGuardado;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void BtnNuevaSucursal_Click(object? sender, EventArgs e)
        {
            string rutLimpio = RutHelper.Limpiar(txtRut.Text);
            if (rutLimpio.Length >= 8)
            {
                Cliente nuevaSucursal = new Cliente { Rut = rutLimpio };

                using var modalCrear = new FormNuevoClienteModal(nuevaSucursal);
                if (modalCrear.ShowDialog(this) == DialogResult.OK && modalCrear.ClienteGuardado != null)
                {
                    // 👇 Se asigna automáticamente la nueva sucursal y se cierra el modal hacia el POS
                    ClienteSeleccionado = modalCrear.ClienteGuardado;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
    }
}