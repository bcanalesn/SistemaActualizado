using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormCaja : Form
    {
        private AppDbContext _db = new AppDbContext();
        private static CajaTurno? _turnoActual = null;
        private Usuario? _usuarioActual;

        private Label lblEstadoCaja = null!;
        private Label lblMontoInicial = null!;
        private Label lblVentasEfectivo = null!;
        private Label lblEfectivoEsperado = null!;

        private Panel pnlCajaCerrada = null!;
        private Panel pnlCajaAbierta = null!;
        private TextBox txtMontoInicial = null!;

        // Controles de Cobro de Ticket en Caja
        private TextBox txtBuscarTicket = null!;
        private DataGridView dgvTicketsPendientes = null!;
        private DataGridView dgvDetalleTicket = null!;

        private ComboBox cbTipoDocumentoCobro = null!;
        private ComboBox cbMedioPago = null!;
        private TextBox txtRutCliente = null!;
        private TextBox txtRazonSocial = null!;
        private TextBox txtGiro = null!;
        private Panel pnlDatosFactura = null!;

        private TextBox txtPagaCon = null!;
        private Label lblVuelto = null!;
        private Label lblTotalCobrar = null!;
        private Button btnCobrarTicket = null!;

        private TVE2607? _ticketSeleccionado = null;

        public FormCaja(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            ActualizarEstadoVista();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            TabControl tabsCaja = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            TabPage tabCobro = new TabPage { Text = "💳 Cobro de Tickets en Caja", BackColor = Color.FromArgb(244, 246, 249) };
            TabPage tabApertura = new TabPage { Text = "💵 Estado y Apertura de Caja", BackColor = Color.FromArgb(244, 246, 249) };

            // ==========================================
            // PESTAÑA 1: COBRO DE TICKETS EN CAJA
            // ==========================================
            TableLayoutPanel layoutCobro = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12)
            };
            layoutCobro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            layoutCobro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            // Columna Izquierda: Lista de Tickets Pendientes y Detalle
            Panel pnlColIzql = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };

            Panel pnlBusquedaTicket = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            Label lblBuscarT = new Label { Text = "🎟️ N° Ticket / Folio:", Location = new Point(10, 15), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtBuscarTicket = new TextBox { Location = new Point(130, 11), Size = new Size(160, 26), Font = new Font("Segoe UI", 10F) };
            txtBuscarTicket.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarTicketsPendientes(txtBuscarTicket.Text.Trim()); e.SuppressKeyPress = true; } };

            Button btnBuscarT = new Button { Text = "Buscar", Location = new Point(298, 10), Size = new Size(75, 28), BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBuscarT.FlatAppearance.BorderSize = 0;
            btnBuscarT.Click += (s, e) => CargarTicketsPendientes(txtBuscarTicket.Text.Trim());

            Button btnRefrescarT = new Button { Text = "🔄 Recargar", Location = new Point(378, 10), Size = new Size(90, 28), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(51, 65, 85), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRefrescarT.FlatAppearance.BorderSize = 0;
            btnRefrescarT.Click += (s, e) => CargarTicketsPendientes();

            pnlBusquedaTicket.Controls.AddRange(new Control[] { lblBuscarT, txtBuscarTicket, btnBuscarT, btnRefrescarT });

            Label lblTitPend = new Label { Text = "TICKETS PENDIENTES DE PAGO", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 22 };

            dgvTicketsPendientes = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvTicketsPendientes);
            dgvTicketsPendientes.SelectionChanged += DgvTicketsPendientes_SelectionChanged;

            Label lblTitDet = new Label { Text = "DETALLE DEL TICKET SELECCIONADO", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 22 };

            dgvDetalleTicket = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvDetalleTicket);

            pnlColIzql.Controls.Add(dgvDetalleTicket);
            pnlColIzql.Controls.Add(lblTitDet);
            pnlColIzql.Controls.Add(dgvTicketsPendientes);
            pnlColIzql.Controls.Add(lblTitPend);
            pnlColIzql.Controls.Add(pnlBusquedaTicket);

            // Columna Derecha: Opciones de Pago, Selección de Boleta/Factura y Cobro
            Panel pnlColDer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };

            Label lblTitCobro = new Label { Text = "⚡ OPCIONES DE COBRO Y EMISIÓN DTE", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 28 };

            Label lblTipoDocCap = new Label { Text = "Tipo de Documento:", Location = new Point(15, 38), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            cbTipoDocumentoCobro = new ComboBox { Location = new Point(15, 58), Size = new Size(330, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbTipoDocumentoCobro.Items.AddRange(new string[] { "Boleta Electrónica", "Factura Electrónica" });
            cbTipoDocumentoCobro.SelectedIndex = 0;
            cbTipoDocumentoCobro.SelectedIndexChanged += CbTipoDocumentoCobro_SelectedIndexChanged;

            // Panel Factura
            pnlDatosFactura = new Panel { Location = new Point(15, 92), Size = new Size(330, 115), BackColor = Color.FromArgb(248, 250, 252), Visible = false };
            Label lblRut = new Label { Text = "RUT Receptor:", Location = new Point(8, 6), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            txtRutCliente = new TextBox { Text = "76.543.210-K", Location = new Point(8, 24), Size = new Size(314, 24), Font = new Font("Segoe UI", 9F) };

            Label lblRazon = new Label { Text = "Razón Social:", Location = new Point(8, 52), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            txtRazonSocial = new TextBox { Text = "EMPRESA DE PRUEBA SPALD", Location = new Point(8, 70), Size = new Size(314, 24), Font = new Font("Segoe UI", 9F) };

            Label lblGiroCap = new Label { Text = "Giro:", Location = new Point(8, 98), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            txtGiro = new TextBox { Text = "COMERCIALIZADORA GENERAL", Location = new Point(8, 116), Size = new Size(314, 24), Font = new Font("Segoe UI", 9F) };

            pnlDatosFactura.Controls.AddRange(new Control[] { lblRut, txtRutCliente, lblRazon, txtRazonSocial, lblGiroCap, txtGiro });

            Label lblMedioCap = new Label { Text = "Medio de Pago:", Location = new Point(15, 215), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            cbMedioPago = new ComboBox { Location = new Point(15, 235), Size = new Size(330, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbMedioPago.Items.AddRange(new string[] { "Efectivo", "Tarjeta Débito", "Tarjeta Crédito", "Transferencia" });
            cbMedioPago.SelectedIndex = 0;

            Label lblPagaCap = new Label { Text = "Paga Con ($):", Location = new Point(15, 275), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            txtPagaCon = new TextBox { Location = new Point(15, 295), Size = new Size(150, 28), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            txtPagaCon.TextChanged += TxtPagaCon_TextChanged;

            lblVuelto = new Label { Text = "Vuelto: $ 0", Location = new Point(180, 298), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) };

            Panel pnlTotalCobroCard = new Panel { Location = new Point(15, 335), Size = new Size(330, 50), BackColor = Color.FromArgb(240, 249, 255) };
            Label lblTotCap = new Label { Text = "TOTAL A COBRAR:", Location = new Point(10, 16), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161) };
            lblTotalCobrar = new Label { Text = "$ 0", Location = new Point(140, 10), AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199) };
            pnlTotalCobroCard.Controls.Add(lblTotCap);
            pnlTotalCobroCard.Controls.Add(lblTotalCobrar);

            btnCobrarTicket = new Button
            {
                Text = "⚡ COBRAR Y EMITIR DTE",
                Location = new Point(15, 395),
                Size = new Size(330, 48),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnCobrarTicket.FlatAppearance.BorderSize = 0;
            btnCobrarTicket.Click += BtnCobrarTicket_Click;

            pnlColDer.Controls.AddRange(new Control[] {
                lblTitCobro, lblTipoDocCap, cbTipoDocumentoCobro, pnlDatosFactura,
                lblMedioCap, cbMedioPago, lblPagaCap, txtPagaCon, lblVuelto,
                pnlTotalCobroCard, btnCobrarTicket
            });

            layoutCobro.Controls.Add(pnlColIzql, 0, 0);
            layoutCobro.Controls.Add(pnlColDer, 1, 0);
            tabCobro.Controls.Add(layoutCobro);

            // ==========================================
            // PESTAÑA 2: ESTADO Y APERTURA DE CAJA
            // ==========================================
            Panel pnlMainApertura = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25) };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(15) };
            lblEstadoCaja = new Label { Text = "ESTADO DE CAJA...", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            pnlHeader.Controls.Add(lblEstadoCaja);

            pnlCajaCerrada = new Panel { Dock = DockStyle.Top, Height = 250, BackColor = Color.White, Margin = new Padding(0, 15, 0, 0), Padding = new Padding(25), Visible = false };
            Label lblTituloAbrir = new Label { Text = "🔓 Apertura de Caja y Turno de Trabajo", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(25, 20), AutoSize = true };
            Label lblMontoLabel = new Label { Text = "FONDO INICIAL DE CAJA ($):", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(25, 65), AutoSize = true };
            txtMontoInicial = new TextBox { Text = "20000", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(25, 90), Size = new Size(220, 32) };
            Button btnAbrirCaja = new Button { Text = "🚀 ABRIR CAJA AHORA", Location = new Point(25, 135), Size = new Size(220, 42), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAbrirCaja.FlatAppearance.BorderSize = 0;
            btnAbrirCaja.Click += BtnAbrirCaja_Click;

            pnlCajaCerrada.Controls.AddRange(new Control[] { lblTituloAbrir, lblMontoLabel, txtMontoInicial, btnAbrirCaja });

            pnlCajaAbierta = new Panel { Dock = DockStyle.Top, Height = 250, BackColor = Color.Transparent, Visible = false };
            TableLayoutPanel gridMetricas = new TableLayoutPanel { Dock = DockStyle.Top, Height = 100, ColumnCount = 3, RowCount = 1 };
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            Panel card1 = CrearCardMetrica("💵 Fondo Inicial", "$ 0", Color.FromArgb(100, 116, 139), out lblMontoInicial);
            Panel card2 = CrearCardMetrica("🛒 Ventas Efectivo", "$ 0", Color.FromArgb(16, 185, 129), out lblVentasEfectivo);
            Panel card3 = CrearCardMetrica("📊 Efectivo Esperado", "$ 0", Color.FromArgb(2, 132, 199), out lblEfectivoEsperado);

            gridMetricas.Controls.Add(card1, 0, 0);
            gridMetricas.Controls.Add(card2, 1, 0);
            gridMetricas.Controls.Add(card3, 2, 0);

            Button btnCerrarTurno = new Button { Text = "🔒 REALIZAR CIERRE DE CAJA Z", Location = new Point(5, 120), Size = new Size(260, 45), BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCerrarTurno.FlatAppearance.BorderSize = 0;
            btnCerrarTurno.Click += BtnArqueoCierre_Click;

            pnlCajaAbierta.Controls.Add(btnCerrarTurno);
            pnlCajaAbierta.Controls.Add(gridMetricas);

            pnlMainApertura.Controls.Add(pnlCajaAbierta);
            pnlMainApertura.Controls.Add(pnlCajaCerrada);
            pnlMainApertura.Controls.Add(pnlHeader);
            tabApertura.Controls.Add(pnlMainApertura);

            tabsCaja.TabPages.Add(tabCobro);
            tabsCaja.TabPages.Add(tabApertura);

            this.Controls.Add(tabsCaja);
            this.ResumeLayout(false);

            CargarTicketsPendientes();
        }

        private Panel CrearCardMetrica(string titulo, string valor, Color acento, out Label lblValor)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(4), Padding = new Padding(12) };
            Label lblT = new Label { Text = titulo.ToUpper(), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 18 };
            lblValor = new Label { Text = valor, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = acento, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            pnl.Controls.Add(lblValor);
            pnl.Controls.Add(lblT);
            return pnl;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 30;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 28;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarTicketsPendientes(string filtro = "")
        {
            try
            {
                var query = _db.TVE2607.Where(v => v.status == "Pendiente");

                if (!string.IsNullOrEmpty(filtro))
                {
                    query = query.Where(v => v.nroDTE.ToString().Contains(filtro));
                }

                var pendientes = query.OrderByDescending(v => v.FecDoc).ToList();
                dgvTicketsPendientes.DataSource = pendientes;

                if (dgvTicketsPendientes.Columns["nroDTE"] != null) dgvTicketsPendientes.Columns["nroDTE"].HeaderText = "N° Ticket";
                if (dgvTicketsPendientes.Columns["FecDoc"] != null) { dgvTicketsPendientes.Columns["FecDoc"].HeaderText = "Hora"; dgvTicketsPendientes.Columns["FecDoc"].DefaultCellStyle.Format = "HH:mm:ss"; }
                if (dgvTicketsPendientes.Columns["UserDTE"] != null) dgvTicketsPendientes.Columns["UserDTE"].HeaderText = "Vendedor";
                if (dgvTicketsPendientes.Columns["Total"] != null) { dgvTicketsPendientes.Columns["Total"].HeaderText = "Total"; dgvTicketsPendientes.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; }

                string[] ocultar = new string[] { "idTve", "idLocal", "nmbLocal", "iddocDTE", "Documento", "nroInT", "SubTotal", "Descuento", "Neto", "Impto1", "Impto2", "Impto3", "IvA", "Vendedor", "nroZ", "Url", "nPAX", "Idcliente", "DNI", "RuT", "dv", "RazonSocial", "Giro", "Direccion", "idcomuna", "nComuna", "idCiudad", "nCiudad", "Fono1", "Fono2", "email", "status", "idREF", "nroREF", "codigoREF", "FechaREF", "HoraDoc", "Detalles" };
                foreach (var col in ocultar)
                {
                    if (dgvTicketsPendientes.Columns[col] != null) dgvTicketsPendientes.Columns[col].Visible = false;
                }
            }
            catch { }
        }

        private void DgvTicketsPendientes_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvTicketsPendientes.CurrentRow?.DataBoundItem is TVE2607 ticket)
            {
                _ticketSeleccionado = ticket;
                btnCobrarTicket.Enabled = true;

                lblTotalCobrar.Text = $"$ {ticket.Total:N0}";

                var detalles = _db.TVD2607.Where(d => d.idTve == ticket.idTve).ToList();
                dgvDetalleTicket.DataSource = detalles;

                if (dgvDetalleTicket.Columns["NmbProducto"] != null) dgvDetalleTicket.Columns["NmbProducto"].HeaderText = "Producto";
                if (dgvDetalleTicket.Columns["Cantidad"] != null) dgvDetalleTicket.Columns["Cantidad"].HeaderText = "Cant.";
                if (dgvDetalleTicket.Columns["Precio"] != null) { dgvDetalleTicket.Columns["Precio"].HeaderText = "Precio"; dgvDetalleTicket.Columns["Precio"].DefaultCellStyle.Format = "$#,##0"; }
                if (dgvDetalleTicket.Columns["SubTotal"] != null) { dgvDetalleTicket.Columns["SubTotal"].HeaderText = "Subtotal"; dgvDetalleTicket.Columns["SubTotal"].DefaultCellStyle.Format = "$#,##0"; }

                string[] ocultarDet = new string[] { "idTvd", "idTve", "idLocal", "iddocDTE", "Documento", "NroDTE", "NroInT", "FecMoV", "HoraMoV", "IdProducto", "nmbCorT", "PneTo", "SubNeto", "Costo", "Tcosto", "IdVendedor", "nmbVendedor", "idFamilia", "nFamilia", "idGrupo", "nGrupo", "Unidad", "VentaEncabezado" };
                foreach (var col in ocultarDet)
                {
                    if (dgvDetalleTicket.Columns[col] != null) dgvDetalleTicket.Columns[col].Visible = false;
                }
            }
            else
            {
                _ticketSeleccionado = null;
                btnCobrarTicket.Enabled = false;
                lblTotalCobrar.Text = "$ 0";
                dgvDetalleTicket.DataSource = null;
            }
        }

        private void CbTipoDocumentoCobro_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool esFactura = cbTipoDocumentoCobro.SelectedIndex == 1;
            pnlDatosFactura.Visible = esFactura;
        }

        private void TxtPagaCon_TextChanged(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            if (decimal.TryParse(txtPagaCon.Text.Trim(), out decimal pagaCon))
            {
                decimal vuelto = pagaCon - _ticketSeleccionado.Total;
                lblVuelto.Text = vuelto >= 0 ? $"Vuelto: $ {vuelto:N0}" : "Efectivo insuficiente";
                lblVuelto.ForeColor = vuelto >= 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(220, 38, 38);
            }
            else
            {
                lblVuelto.Text = "Vuelto: $ 0";
            }
        }

        private void BtnCobrarTicket_Click(object? sender, EventArgs e)
        {
            if (_ticketSeleccionado == null) return;

            string tipoDoc = cbTipoDocumentoCobro.SelectedItem?.ToString() ?? "Boleta Electrónica";
            string medioPago = cbMedioPago.SelectedItem?.ToString() ?? "Efectivo";

            decimal pagaCon = 0;
            decimal vuelto = 0;

            if (medioPago == "Efectivo")
            {
                if (!decimal.TryParse(txtPagaCon.Text.Trim(), out pagaCon) || pagaCon < _ticketSeleccionado.Total)
                {
                    MessageBox.Show("Ingrese un monto en efectivo suficiente para realizar el cobro.", "Cobro denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                vuelto = pagaCon - _ticketSeleccionado.Total;
            }

            try
            {
                // Asignar Folio DTE Oficial
                int folioOficial = (int)(DateTime.Now.Ticks % 1000000);
                int iddoc = tipoDoc.Contains("Factura") ? 33 : 39;

                _ticketSeleccionado.iddocDTE = iddoc;
                _ticketSeleccionado.Documento = tipoDoc;
                _ticketSeleccionado.nroDTE = folioOficial;
                _ticketSeleccionado.UserDTE = _usuarioActual?.NombreUsuario ?? "barbara";
                _ticketSeleccionado.status = "Emitido";

                if (tipoDoc.Contains("Factura"))
                {
                    _ticketSeleccionado.RuT = txtRutCliente.Text.Trim();
                    _ticketSeleccionado.RazonSocial = txtRazonSocial.Text.Trim();
                    _ticketSeleccionado.Giro = txtGiro.Text.Trim();
                }

                // Cargar detalles para construir carrito de previsualización
                // (Nota: El stock ya fue descontado al generar el Ticket de Atención en FormVenta.cs)
                var detalles = _db.TVD2607.Where(d => d.idTve == _ticketSeleccionado.idTve).ToList();
                List<DetalleCarrito> itemsCarrito = new List<DetalleCarrito>();

                foreach (var item in detalles)
                {
                    item.iddocDTE = iddoc;
                    item.Documento = tipoDoc;
                    item.NroDTE = folioOficial;

                    itemsCarrito.Add(new DetalleCarrito
                    {
                        ProductoID = item.IdProducto,
                        Nombre = item.NmbProducto,
                        PrecioUnitario = item.Precio,
                        Cantidad = item.Cantidad
                    });
                }

                _db.SaveChanges();

                MessageBox.Show($"¡{tipoDoc.ToUpper()} N° {folioOficial} COBRADA CON ÉXITO!\n\n" +
                                $"• Medio de Pago: {medioPago}\n" +
                                $"• Total Cobrado: ${_ticketSeleccionado.Total:N0}\n" +
                                (medioPago == "Efectivo" ? $"• Vuelto al Cliente: ${vuelto:N0}\n" : ""),
                                "Venta Finalizada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Abrir previsualización e impresión de Boleta/Factura
                FormTicket formTicket = new FormTicket(_ticketSeleccionado, itemsCarrito, pagaCon, vuelto);
                formTicket.ShowDialog();

                CargarTicketsPendientes();
                ActualizarEstadoVista();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar el cobro: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarEstadoVista()
        {
            if (_turnoActual != null && _turnoActual.Estado == "Abierta")
            {
                string cajero = _turnoActual.Usuario;
                lblEstadoCaja.Text = $"🟢 CAJA ABIERTA ({cajero}) - Turno iniciado el {_turnoActual.FechaApertura:dd/MM/yyyy HH:mm}";
                lblEstadoCaja.ForeColor = Color.FromArgb(16, 185, 129);

                pnlCajaCerrada.Visible = false;
                pnlCajaAbierta.Visible = true;

                decimal ventasEfectivo = 0;
                try
                {
                    ventasEfectivo = _db.TVE2607
                        .Where(v => v.FecDoc >= _turnoActual.FechaApertura && v.status == "Emitido")
                        .Sum(v => (decimal?)(v.iddocDTE == 61 ? -v.Total : v.Total)) ?? 0;
                }
                catch { }

                _turnoActual.MontoEfectivoVentas = ventasEfectivo;
                decimal esperado = _turnoActual.MontoInicial + _turnoActual.MontoEfectivoVentas;

                lblMontoInicial.Text = $"$ {_turnoActual.MontoInicial:N0}";
                lblVentasEfectivo.Text = $"$ {_turnoActual.MontoEfectivoVentas:N0}";
                lblEfectivoEsperado.Text = $"$ {esperado:N0}";
            }
            else
            {
                lblEstadoCaja.Text = "🔴 CAJA CERRADA - Inicie un nuevo turno para poder operar en el POS";
                lblEstadoCaja.ForeColor = Color.FromArgb(220, 38, 38);

                pnlCajaAbierta.Visible = false;
                pnlCajaCerrada.Visible = true;
            }
        }

        private void BtnAbrirCaja_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMontoInicial.Text.Trim(), out decimal montoInicial) || montoInicial < 0)
            {
                MessageBox.Show("Ingrese un monto inicial válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _turnoActual = new CajaTurno
            {
                CajaTurnoID = 1,
                Usuario = _usuarioActual?.NombreUsuario ?? "barbara",
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                Estado = "Abierta"
            };

            MessageBox.Show($"¡Caja abierta exitosamente con fondo de ${_turnoActual.MontoInicial:N0}!", "Apertura de Caja", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ActualizarEstadoVista();
        }

        private void BtnArqueoCierre_Click(object? sender, EventArgs e)
        {
            if (_turnoActual == null) return;

            decimal esperado = _turnoActual.MontoInicial + _turnoActual.MontoEfectivoVentas;
            var res = MessageBox.Show($"¿Desea cerrar el turno de caja?\n\nEfectivo Esperado: ${esperado:N0}", "Cierre Z de Caja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                _turnoActual = null;
                ActualizarEstadoVista();
                MessageBox.Show("Turno de caja cerrado exitosamente.", "Cierre Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}