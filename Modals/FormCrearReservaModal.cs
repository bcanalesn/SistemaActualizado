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
        private TextBox txtMontoAbono = null!;
        private ComboBox cbMedioPagoAbono = null!;
        private Label lblTotalPedidoValor = null!;
        private Label lblSaldoPendienteValor = null!;
        private Button btnConfirmar = null!;
        private Button btnCancelar = null!;

        public Reserva? ReservaCreada { get; private set; }

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
            this.Size = new Size(520, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16) };

            // 1. Tarjeta Cliente (Lectura segura de propiedades)
            Panel pnlCardCliente = CrearPanelTarjeta(456, 68);
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
                Size = new Size(430, 28),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlCardCliente.Controls.AddRange(new Control[] { lblTitCli, lblNomCli });

            // 2. Tarjeta Fecha y Hora de Entrega (Separadas para evitar mezclas de formato)
            Panel pnlCardFecha = CrearPanelTarjeta(456, 75);
            pnlCardFecha.Location = new Point(0, 76);
            Label lblTitFecha = new Label { Text = "⏰ FECHA Y HORA COMPROMISO DE ENTREGA", Location = new Point(12, 10), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };

            dtpFechaEntrega = new DateTimePicker
            {
                Location = new Point(12, 32),
                Size = new Size(270, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Now.Date,
                Value = DateTime.Now.AddDays(1).Date
            };

            dtpHoraEntrega = new DateTimePicker
            {
                Location = new Point(292, 32),
                Size = new Size(150, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm 'hrs'",
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(12)
            };

            pnlCardFecha.Controls.AddRange(new Control[] { lblTitFecha, dtpFechaEntrega, dtpHoraEntrega });

            // 3. Tarjeta Observaciones / Especificaciones
            Panel pnlCardObs = CrearPanelTarjeta(456, 95);
            pnlCardObs.Location = new Point(0, 159);
            Label lblTitObs = new Label { Text = "📝 ESPECIFICACIONES / DETALLES DEL ENCARGO", Location = new Point(12, 8), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };
            txtObservaciones = new TextBox
            {
                Location = new Point(12, 28),
                Size = new Size(430, 54),
                Multiline = true,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Ej: Torta Selva Negra con dedicatoria 'Feliz Cumpleaños', sin nueces..."
            };
            pnlCardObs.Controls.AddRange(new Control[] { lblTitObs, txtObservaciones });

            // 4. Tarjeta Abono y Montos
            Panel pnlCardMontos = CrearPanelTarjeta(456, 175);
            pnlCardMontos.Location = new Point(0, 262);

            Label lblTitAbono = new Label { Text = "💵 GESTIÓN DE ABONO Y SALDOS", Location = new Point(12, 8), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), AutoSize = true };

            Label lblTotTxt = new Label { Text = "TOTAL PEDIDO:", Location = new Point(12, 34), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), AutoSize = true };
            lblTotalPedidoValor = new Label { Text = MonedaHelper.Formatear(_totalPedido, conSigno: true), Location = new Point(130, 32), Size = new Size(140, 22), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblAboTxt = new Label { Text = "Monto Abono Inicial ($):", Location = new Point(12, 68), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            txtMontoAbono = new TextBox
            {
                Location = new Point(12, 88),
                Size = new Size(200, 26),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "0",
                MaxLength = 10
            };
            txtMontoAbono.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtMontoAbono.TextChanged += (s, e) =>
            {
                MonedaHelper.AplicarMascaraEnVivo(txtMontoAbono);
                RecalcularSaldos();
            };

            Label lblMedioTxt = new Label { Text = "Medio de Pago Abono:", Location = new Point(230, 68), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            cbMedioPagoAbono = new ComboBox
            {
                Location = new Point(230, 88),
                Size = new Size(212, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbMedioPagoAbono.Items.AddRange(new object[] { "Efectivo", "Débito", "Tarjeta Crédito", "Transferencia" });
            cbMedioPagoAbono.SelectedIndex = 0;

            Label lblSalTxt = new Label { Text = "SALDO PENDIENTE:", Location = new Point(12, 132), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
            lblSaldoPendienteValor = new Label { Text = MonedaHelper.Formatear(_totalPedido, conSigno: true), Location = new Point(155, 130), Size = new Size(180, 26), Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68) };

            pnlCardMontos.Controls.AddRange(new Control[] { lblTitAbono, lblTotTxt, lblTotalPedidoValor, lblAboTxt, txtMontoAbono, lblMedioTxt, cbMedioPagoAbono, lblSalTxt, lblSaldoPendienteValor });

            // 5. Botones de Acción
            btnConfirmar = new Button
            {
                Text = "✔ Confirmar y Generar Reserva",
                Location = new Point(0, 460),
                Size = new Size(456, 44),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += BtnConfirmar_Click;

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Location = new Point(0, 514),
                Size = new Size(456, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            pnlMain.Controls.AddRange(new Control[] { pnlCardCliente, pnlCardFecha, pnlCardObs, pnlCardMontos, btnConfirmar, btnCancelar });
            this.Controls.Add(pnlMain);
            this.CancelButton = btnCancelar;
        }

        private void RecalcularSaldos()
        {
            decimal abono = MonedaHelper.Limpiar(txtMontoAbono.Text);
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
            decimal abono = MonedaHelper.Limpiar(txtMontoAbono.Text);

            // Unión de la fecha del calendario y la hora seleccionada
            DateTime fechaCompromiso = dtpFechaEntrega.Value.Date + dtpHoraEntrega.Value.TimeOfDay;

            if (fechaCompromiso <= DateTime.Now)
            {
                MessageBox.Show("La fecha y hora de entrega debe ser posterior al momento actual.", "Fecha Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpHoraEntrega.Focus();
                return;
            }

            try
            {
                var nuevaReserva = new Reserva
                {
                    IdCliente = _cliente.IdCliente,
                    FechaEntregaPactada = fechaCompromiso,
                    TotalPedido = _totalPedido,
                    Observaciones = txtObservaciones.Text.Trim(),
                    UsuarioRegistro = _usuarioActual.NombreUsuario,
                    Detalles = _itemsCarrito.Select(i => new ReservaDetalle
                    {
                        ProductoID = i.ProductoID,
                        CodigoBarra = i.ProductoID.ToString(), // 👈 Evita que sea null
                        NombreProducto = i.Nombre ?? "Producto",
                        EsPesable = i.EsPesable,
                        Cantidad = i.Cantidad,
                        PrecioUnitario = i.PrecioUnitario,
                        Subtotal = i.Subtotal,
                        NotasItem = ""
                    }).ToList()
                };

                ReservaPago? abonoPago = null;
                if (abono > 0)
                {
                    abonoPago = new ReservaPago
                    {
                        Monto = abono,
                        MedioPago = cbMedioPagoAbono.SelectedItem?.ToString() ?? "Efectivo",
                        CajaTurnoID = _cajaTurnoId,
                        Usuario = _usuarioActual.NombreUsuario
                    };
                }

                var service = new ReservaService();
                ReservaCreada = service.RegistrarNuevaReserva(nuevaReserva, abonoPago);

                string nombreMostrado = ObtenerPropiedadCliente(_cliente, "RazonSocial", "Nombres", "Nombre");
                if (string.IsNullOrWhiteSpace(nombreMostrado)) nombreMostrado = "Cliente";

                MessageBox.Show($"¡Reserva N° {ReservaCreada.CodigoReserva} registrada con éxito!\n\n" +
                                $"• Cliente: {nombreMostrado}\n" +
                                $"• Entrega: {ReservaCreada.FechaEntregaPactada:dd/MM/yyyy HH:mm}\n" +
                                $"• Total: {MonedaHelper.Formatear(ReservaCreada.TotalPedido, conSigno: true)}\n" +
                                $"• Abono: {MonedaHelper.Formatear(ReservaCreada.TotalAbonado, conSigno: true)}\n" +
                                $"• Saldo Pendiente: {MonedaHelper.Formatear(ReservaCreada.SaldoPendiente, conSigno: true)}",
                                "Reserva Confirmada", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        return val;
                    }
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