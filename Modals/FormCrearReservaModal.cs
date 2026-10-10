using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormCrearReservaModal : Form
    {
        private readonly Cliente _cliente;
        private readonly List<DetalleCarrito> _itemsCarrito;
        private readonly decimal _totalPedido;
        private readonly Usuario _usuarioActual;
        private readonly int _cajaTurnoId;

        private DateTimePicker dtpFechaEntrega = null!;
        private DateTimePicker dtpHoraEntrega = null!;
        private TextBox txtObservaciones = null!;
        
        // Controles de Abono
        private CheckBox chkAbonar = null!;
        private TextBox txtMontoAbono = null!;
        private Label lblTotalPedidoValor = null!;
        private Label lblSaldoPendienteValor = null!;
        private Button btnConfirmar = null!;
        private Button btnCancelar = null!;

        public Reserva? ReservaCreada { get; private set; }
        public int NroTicketAbonoGenerado { get; private set; } = 0;

        public FormCrearReservaModal(Cliente cliente, List<DetalleCarrito> items, decimal total, Usuario usuario, int cajaTurnoId)
        {
            _cliente = cliente;
            _itemsCarrito = items;
            _totalPedido = total;
            _usuarioActual = usuario;
            _cajaTurnoId = cajaTurnoId;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "📅 Registrar Pedido por Encargo / Reserva";
            this.ClientSize = new Size(490, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel 
            { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(18, 14, 18, 14),
                BackColor = Color.White,
                AutoScroll = true
            };

            const int wCard = 454;

            // 1. Tarjeta Cliente
            Panel pnlCardCliente = CrearPanelTarjeta(wCard, 68);
            pnlCardCliente.Dock = DockStyle.Top;
            pnlCardCliente.Margin = new Padding(0, 0, 0, 10);

            Label lblTitCli = new Label { Text = "👤 CLIENTE ASOCIADO", Location = new Point(12, 10), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };

            string nombreCli = ObtenerPropiedadCliente(_cliente, "RazonSocial", "Nombres", "Nombre", "Razon_Social");
            if (string.IsNullOrWhiteSpace(nombreCli)) nombreCli = "Cliente General";

            string rutCli = ObtenerPropiedadCliente(_cliente, "Rut", "RuT", "RUT", "Dni");
            string telCli = ObtenerPropiedadCliente(_cliente, "Telefono", "Fono1", "Fono", "Celular");

            string rutTexto = !string.IsNullOrWhiteSpace(rutCli) ? $" ({rutCli})" : "";
            string telTexto = !string.IsNullOrWhiteSpace(telCli) ? $" | Tel: {telCli}" : "";

            Label lblNomCli = new Label
            {
                Text = $"{nombreCli}{rutTexto}{telTexto}",
                Location = new Point(12, 28),
                Size = new Size(wCard - 24, 28),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlCardCliente.Controls.AddRange(new Control[] { lblTitCli, lblNomCli });

            // 2. Tarjeta Fecha y Hora de Entrega
            Panel pnlCardFecha = CrearPanelTarjeta(wCard, 74);
            pnlCardFecha.Dock = DockStyle.Top;
            pnlCardFecha.Margin = new Padding(0, 10, 0, 10);

            Label lblTitFecha = new Label { Text = "⏰ FECHA Y HORA COMPROMISO DE ENTREGA", Location = new Point(12, 10), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };

            dtpFechaEntrega = new DateTimePicker
            {
                Location = new Point(12, 32),
                Size = new Size(265, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Now.Date,
                Value = DateTime.Now.AddDays(1).Date
            };

            dtpHoraEntrega = new DateTimePicker
            {
                Location = new Point(287, 32),
                Size = new Size(155, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm 'hrs'",
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(12)
            };
            pnlCardFecha.Controls.AddRange(new Control[] { lblTitFecha, dtpFechaEntrega, dtpHoraEntrega });

            // 3. Tarjeta Observaciones / Especificaciones
            Panel pnlCardObs = CrearPanelTarjeta(wCard, 96);
            pnlCardObs.Dock = DockStyle.Top;
            pnlCardObs.Margin = new Padding(0, 10, 0, 10);

            Label lblTitObs = new Label { Text = "📝 ESPECIFICACIONES / DETALLES DEL ENCARGO", Location = new Point(12, 8), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };
            txtObservaciones = new TextBox
            {
                Location = new Point(12, 28),
                Size = new Size(wCard - 24, 56),
                Multiline = true,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Ej: Torta Selva Negra con dedicatoria 'Feliz Cumpleaños', sin nueces..."
            };
            pnlCardObs.Controls.AddRange(new Control[] { lblTitObs, txtObservaciones });

            // 4. Tarjeta Abono y Montos (SIN MEDIO DE PAGO)
            Panel pnlCardMontos = CrearPanelTarjeta(wCard, 175);
            pnlCardMontos.Dock = DockStyle.Top;
            pnlCardMontos.Margin = new Padding(0, 10, 0, 12);

            Label lblTitAbono = new Label { Text = "💵 GESTIÓN DE ABONO Y SALDOS", Location = new Point(12, 8), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), AutoSize = true };

            Label lblTotTxt = new Label { Text = "TOTAL PEDIDO:", Location = new Point(12, 34), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };
            lblTotalPedidoValor = new Label { Text = MonedaHelper.Formatear(_totalPedido, conSigno: true), Location = new Point(130, 32), Size = new Size(180, 22), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

            // CheckBox para registrar intención de abono inicial
            chkAbonar = new CheckBox
            {
                Text = "Cliente pagará Abono Inicial en Caja",
                Location = new Point(12, 66),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Cursor = Cursors.Hand
            };

            Label lblAboTxt = new Label { Text = "Monto Abono Inicial ($):", Location = new Point(12, 94), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            txtMontoAbono = new TextBox
            {
                Location = new Point(12, 114),
                Size = new Size(205, 26),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "0",
                MaxLength = 10,
                Enabled = false // Inactivo hasta marcar casilla
            };
            txtMontoAbono.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtMontoAbono.TextChanged += (s, e) =>
            {
                MonedaHelper.AplicarMascaraEnVivo(txtMontoAbono);
                RecalcularSaldos();
            };

            chkAbonar.CheckedChanged += (s, e) =>
            {
                txtMontoAbono.Enabled = chkAbonar.Checked;
                if (!chkAbonar.Checked)
                {
                    txtMontoAbono.Text = "0";
                }
                else
                {
                    txtMontoAbono.Focus();
                    txtMontoAbono.SelectAll();
                }
                RecalcularSaldos();
            };

            Label lblAvisoCaja = new Label
            {
                Text = "ℹ️ El medio de pago se ingresará en Caja al cobrar el ticket de abono.",
                Location = new Point(230, 94),
                Size = new Size(215, 45),
                Font = new Font("Segoe UI", 7.8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            Label lblSalTxt = new Label { Text = "SALDO ESTIMADO:", Location = new Point(12, 146), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
            lblSaldoPendienteValor = new Label { Text = MonedaHelper.Formatear(_totalPedido, conSigno: true), Location = new Point(155, 144), Size = new Size(220, 26), Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68) };

            pnlCardMontos.Controls.AddRange(new Control[] { lblTitAbono, lblTotTxt, lblTotalPedidoValor, chkAbonar, lblAboTxt, txtMontoAbono, lblAvisoCaja, lblSalTxt, lblSaldoPendienteValor });

            // Espaciador entre tarjetas y botones
            Panel pnlEspaciador = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Color.Transparent };

            // 5. Botones de Acción
            btnConfirmar = new Button
            {
                Text = "✔ Confirmar y Generar Reserva",
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += BtnConfirmar_Click;

            Panel pnlEspaciadorBtn = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            pnlMain.Controls.Add(btnCancelar);
            pnlMain.Controls.Add(pnlEspaciadorBtn);
            pnlMain.Controls.Add(btnConfirmar);
            pnlMain.Controls.Add(pnlEspaciador);
            pnlMain.Controls.Add(pnlCardMontos);
            pnlMain.Controls.Add(pnlCardObs);
            pnlMain.Controls.Add(pnlCardFecha);
            pnlMain.Controls.Add(pnlCardCliente);

            this.Controls.Add(pnlMain);
            this.CancelButton = btnCancelar;
        }

        private void RecalcularSaldos()
        {
            decimal abono = chkAbonar.Checked ? MonedaHelper.Limpiar(txtMontoAbono.Text) : 0m;
            if (abono > _totalPedido)
            {
                abono = _totalPedido;
                txtMontoAbono.Text = MonedaHelper.Formatear(abono);
            }

            decimal saldo = _totalPedido - abono;
            lblSaldoPendienteValor.Text = MonedaHelper.Formatear(saldo, conSigno: true);
            lblSaldoPendienteValor.ForeColor = saldo > 0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(16, 185, 129);
        }

        private void BtnConfirmar_Click(object? sender, EventArgs e)
        {
            decimal abono = chkAbonar.Checked ? MonedaHelper.Limpiar(txtMontoAbono.Text) : 0m;
            DateTime fechaCompromiso = dtpFechaEntrega.Value.Date + dtpHoraEntrega.Value.TimeOfDay;

            if (fechaCompromiso <= DateTime.Now)
            {
                MessageBox.Show("La fecha y hora de entrega debe ser posterior al momento actual.", "Fecha Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpHoraEntrega.Focus();
                return;
            }

            if (chkAbonar.Checked && abono <= 0)
            {
                MessageBox.Show("Indicó que se ingresará un abono, pero el monto es $0. Ingrese el monto o desmarque la casilla.", "Monto de Abono", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMontoAbono.Focus();
                return;
            }

            try
            {
                var nuevaReserva = new Reserva
                {
                    IdCliente = _cliente.IdCliente,
                    FechaEntregaPactada = fechaCompromiso,
                    TotalPedido = _totalPedido,
                    TotalAbonado = 0m,               // Se mantiene en $0 hasta que Caja cobre el ticket
                    SaldoPendiente = _totalPedido,   // Saldo total pendiente hasta que se cobre en caja
                    Observaciones = txtObservaciones.Text.Trim(),
                    UsuarioRegistro = _usuarioActual.NombreUsuario,
                    Detalles = _itemsCarrito.Select(i => new ReservaDetalle
                    {
                        ProductoID = i.ProductoID,
                        CodigoBarra = i.ProductoID.ToString(),
                        NombreProducto = i.Nombre ?? "Producto",
                        EsPesable = i.EsPesable,
                        Cantidad = i.Cantidad,
                        PrecioUnitario = i.PrecioUnitario,
                        Subtotal = i.Subtotal,
                        NotasItem = ""
                    }).ToList()
                };

                var service = new ReservaService();
                
                // Si el cliente va a abonar, se genera la reserva Y el ticket pendiente para Caja
                var resultado = service.RegistrarReservaConTicketAbono(nuevaReserva, abono, _usuarioActual.NombreUsuario);
                ReservaCreada = resultado.Reserva;
                NroTicketAbonoGenerado = resultado.NroTicketAbono;

                string nombreMostrado = ObtenerPropiedadCliente(_cliente, "RazonSocial", "Nombres", "Nombre");
                if (string.IsNullOrWhiteSpace(nombreMostrado)) nombreMostrado = "Cliente";

                string mensajeExito = $"¡Reserva N° {ReservaCreada.CodigoReserva} registrada con éxito!\n\n" +
                                      $"• Cliente: {nombreMostrado}\n" +
                                      $"• Entrega: {ReservaCreada.FechaEntregaPactada:dd/MM/yyyy HH:mm}\n" +
                                      $"• Total: {MonedaHelper.Formatear(ReservaCreada.TotalPedido, conSigno: true)}";

                if (NroTicketAbonoGenerado > 0)
                {
                    mensajeExito += $"\n\n🎟️ TICKET DE ABONO GENERADO: N° {NroTicketAbonoGenerado}\n" +
                                    $"Monto a cobrar en Caja: {MonedaHelper.Formatear(abono, conSigno: true)}\n" +
                                    $"(El ticket ya está disponible en Control de Caja para ser cobrado).";
                }

                MessageBox.Show(mensajeExito, "Reserva Registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar reserva: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ObtenerPropiedadCliente(Cliente cliente, params string[] nombresPosibles)
        {
            if (cliente == null) return string.Empty;
            var tipo = cliente.GetType();
            foreach (var nombre in nombresPosibles)
            {
                var prop = tipo.GetProperty(nombre, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var val = prop.GetValue(cliente)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
            return string.Empty;
        }

        private Panel CrearPanelTarjeta(int ancho, int alto)
        {
            Panel p = new Panel { Size = new Size(ancho, alto), BackColor = Color.White };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1.2f);
                e.Graphics.DrawRectangle(pen, r);
            };
            return p;
        }
    }
}