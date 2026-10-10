using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormPedidosReservados : Form
    {
        private readonly ReservaService _reservaService = new ReservaService();
        private readonly CajaService _cajaService = new CajaService();
        private readonly TicketPrintService _ticketPrintService = new TicketPrintService();
        private readonly Usuario _usuarioActual;

        private DataGridView dgvReservas = null!;
        private DataGridView dgvDetalle = null!;
        private DataGridView dgvAbonos = null!;
        private TextBox txtBuscar = null!;
        private ComboBox cbFiltroEstado = null!;
        private Label lblContadorFooter = null!;

        private Button btnMarcarListo = null!;
        private Button btnRegistrarAbono = null!;
        private Button btnEntregarLiquidar = null!;
        private Button btnReimprimir = null!;
        private Button btnAnular = null!;

        private Reserva? _reservaSeleccionada;
        private List<Reserva> _listaReservas = new List<Reserva>();

        public FormPedidosReservados(Usuario? usuario = null)
        {
            _usuarioActual = usuario ?? new Usuario { NombreUsuario = "admin" };

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            this.Shown += (s, e) => CargarReservas();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // =========================================================================
            // 1. ENCABEZADO
            // =========================================================================
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 56, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 0, 0, 10) 
            };

            Label lblTitulo = new Label
            {
                Text = "📅 Pedidos por Encargo y Reservas",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Label lblSubtitulo = new Label
            {
                Text = "Seguimiento de pedidos futuros, control de abonos, producción y entrega con boleta",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblTitulo);

            // =========================================================================
            // 2. SECCIÓN FILTROS Y BÚSQUEDA
            // =========================================================================
            Panel pnlFiltrosWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 70, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 10, 16, 10);

            FlowLayoutPanel flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            cbFiltroEstado = new ComboBox
            {
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbFiltroEstado.Items.AddRange(new object[] { "Todos", "Reservado", "Listo para Retiro", "Entregado", "Anulado" });
            cbFiltroEstado.SelectedIndex = 0;
            cbFiltroEstado.SelectedIndexChanged += (s, e) => CargarReservas();
            Panel pnlGrpEstado = CrearGrupoLimpio("ESTADO", cbFiltroEstado, 165);

            txtBuscar = new TextBox
            {
                Size = new Size(320, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "Código, Cliente, RUT..."
            };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarReservas(); e.SuppressKeyPress = true; } };
            Panel pnlGrpBuscar = CrearGrupoConCaja("BUSCAR RESERVA", txtBuscar, 335);

            Button btnBuscar = CrearBoton("🔍 Buscar", Color.FromArgb(37, 99, 235), Color.White, new Size(90, 32));
            btnBuscar.Margin = new Padding(6, 14, 0, 0);
            btnBuscar.Click += (s, e) => CargarReservas();

            Button btnLimpiarFiltros = CrearBoton("🔄 Recargar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(100, 32));
            btnLimpiarFiltros.Margin = new Padding(6, 14, 0, 0);
            btnLimpiarFiltros.Click += (s, e) => { txtBuscar.Clear(); cbFiltroEstado.SelectedIndex = 0; CargarReservas(); };

            flpFiltros.Controls.AddRange(new Control[] { pnlGrpEstado, pnlGrpBuscar, btnBuscar, btnLimpiarFiltros });
            pnlFiltrosCard.Controls.Add(flpFiltros);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // =========================================================================
            // 3. LAYOUT CENTRAL Y PANELES RESPONSIVOS
            // =========================================================================
            TableLayoutPanel gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));

            // A) TARJETA GRILLA SUPERIOR
            Panel pnlCardReservas = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardReservas.Dock = DockStyle.Fill;
            pnlCardReservas.Padding = new Padding(10);
            pnlCardReservas.Margin = new Padding(0, 0, 0, 8);

            dgvReservas = CrearGrillaBase();
            ConfigurarColumnasReservas();
            dgvReservas.SelectionChanged += DgvReservas_SelectionChanged;

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(4, 4, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 reservas registradas",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlCardReservas.Controls.Add(dgvReservas);
            pnlCardReservas.Controls.Add(pnlFooter);

            // B) TARJETA SPLIT INFERIOR (3 Columnas con anchos responsivos y relleno total)
            TableLayoutPanel pnlBottomSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            pnlBottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F)); // Detalle Ítems
            pnlBottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // Historial Abonos
            pnlBottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F)); // Acciones

            // 1. Detalle de Ítems
            Panel pnlCardDet = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardDet.Dock = DockStyle.Fill;
            pnlCardDet.Padding = new Padding(10);
            pnlCardDet.Margin = new Padding(0, 0, 6, 0);

            Label lblTitDet = new Label 
            { 
                Text = "PRODUCTOS DEL PEDIDO", 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Dock = DockStyle.Top, 
                Height = 22 
            };

            dgvDetalle = CrearGrillaBase();
            ConfigurarColumnasDetalle();
            pnlCardDet.Controls.Add(dgvDetalle);
            pnlCardDet.Controls.Add(lblTitDet);

            // 2. Historial de Abonos
            Panel pnlCardAbonos = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardAbonos.Dock = DockStyle.Fill;
            pnlCardAbonos.Padding = new Padding(10);
            pnlCardAbonos.Margin = new Padding(6, 0, 6, 0);

            Label lblTitAbo = new Label 
            { 
                Text = "HISTORIAL DE PAGOS / ABONOS", 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Dock = DockStyle.Top, 
                Height = 22 
            };

            dgvAbonos = CrearGrillaBase();
            ConfigurarColumnasAbonos();
            pnlCardAbonos.Controls.Add(dgvAbonos);
            pnlCardAbonos.Controls.Add(lblTitAbo);
            // 3. Botones de Acción (Separación vertical limpia)
            Panel pnlCardAcciones = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardAcciones.Dock = DockStyle.Fill;
            pnlCardAcciones.Padding = new Padding(12);
            pnlCardAcciones.Margin = new Padding(6, 0, 0, 0);

            // Se agregan de abajo hacia arriba con DockStyle.Top usando un espaciador entre cada uno
            btnAnular = CrearBotonAccionDock("🚫 Anular Reserva", Color.FromArgb(254, 242, 242), Color.FromArgb(239, 68, 68));
            btnAnular.Click += BtnAnular_Click;

            btnReimprimir = CrearBotonAccionDock("🖨️ Reimprimir Ticket", Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59));
            btnReimprimir.Click += BtnReimprimir_Click;

            btnMarcarListo = CrearBotonAccionDock("📦 Marcar Listo para Retiro", Color.FromArgb(124, 58, 237), Color.White);
            btnMarcarListo.Click += BtnMarcarListo_Click;

            btnRegistrarAbono = CrearBotonAccionDock("💵 Registrar Abono", Color.FromArgb(37, 99, 235), Color.White);
            btnRegistrarAbono.Click += BtnRegistrarAbono_Click;

            btnEntregarLiquidar = CrearBotonAccionDock("🚀 ENTREGAR Y COBRAR", Color.FromArgb(16, 185, 129), Color.White);
            btnEntregarLiquidar.Click += BtnEntregarLiquidar_Click;

            pnlCardAcciones.Controls.Add(btnAnular);
            pnlCardAcciones.Controls.Add(CrearEspaciador(8));
            pnlCardAcciones.Controls.Add(btnReimprimir);
            pnlCardAcciones.Controls.Add(CrearEspaciador(8));
            pnlCardAcciones.Controls.Add(btnMarcarListo);
            pnlCardAcciones.Controls.Add(CrearEspaciador(8));
            pnlCardAcciones.Controls.Add(btnRegistrarAbono);
            pnlCardAcciones.Controls.Add(CrearEspaciador(8));
            pnlCardAcciones.Controls.Add(btnEntregarLiquidar);

            pnlBottomSplit.Controls.Add(pnlCardDet, 0, 0);
            pnlBottomSplit.Controls.Add(pnlCardAbonos, 1, 0);
            pnlBottomSplit.Controls.Add(pnlCardAcciones, 2, 0);

            gridLayout.Controls.Add(pnlCardReservas, 0, 0);
            gridLayout.Controls.Add(pnlBottomSplit, 0, 1);

            pnlPrincipal.Controls.Add(gridLayout);
            pnlPrincipal.Controls.Add(pnlFiltrosWrapper);
            pnlPrincipal.Controls.Add(pnlHeader);

            this.Controls.Add(pnlPrincipal);
            this.ResumeLayout(false);
        }

        private DataGridView CrearGrillaBase()
        {
            var dgv = new DataGridView
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
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false
            };

            // Estilo unificado con FormClientes: SelectionBackColor fijo para evitar cabeceras celestes
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);

            return dgv;
        }

        private void ConfigurarColumnasReservas()
        {
            dgvReservas.Columns.Clear();
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodigoReserva", DataPropertyName = "CodigoReserva", HeaderText = "CÓDIGO", FillWeight = 20 });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreCliente", DataPropertyName = "NombreCliente", HeaderText = "CLIENTE", FillWeight = 32 });
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "TelefonoCliente", DataPropertyName = "TelefonoCliente", HeaderText = "TELÉFONO", FillWeight = 18 });

            var colEntrega = new DataGridViewTextBoxColumn { Name = "FechaEntregaPactada", DataPropertyName = "FechaEntregaPactada", HeaderText = "FECHA COMPROMISO", FillWeight = 24 };
            colEntrega.DefaultCellStyle.Format = "dd-MM-yyyy HH:mm";
            dgvReservas.Columns.Add(colEntrega);

            var estiloMoneda = new DataGridViewCellStyle 
            { 
                FormatProvider = new System.Globalization.CultureInfo("es-CL"), 
                Format = "$ #,##0", 
                Alignment = DataGridViewContentAlignment.MiddleRight, 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) 
            };

            // TOTAL
            var colTotal = new DataGridViewTextBoxColumn { Name = "TotalPedido", DataPropertyName = "TotalPedido", HeaderText = "TOTAL", FillWeight = 18, DefaultCellStyle = estiloMoneda };
            colTotal.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvReservas.Columns.Add(colTotal);

            // ABONADO
            var colAbonado = new DataGridViewTextBoxColumn { Name = "TotalAbonado", DataPropertyName = "TotalAbonado", HeaderText = "ABONADO", FillWeight = 18, DefaultCellStyle = estiloMoneda };
            colAbonado.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvReservas.Columns.Add(colAbonado);

            // SALDO
            var colSaldo = new DataGridViewTextBoxColumn { Name = "SaldoPendiente", DataPropertyName = "SaldoPendiente", HeaderText = "SALDO", FillWeight = 18, DefaultCellStyle = estiloMoneda };
            colSaldo.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvReservas.Columns.Add(colSaldo);

            // ESTADO
            dgvReservas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", DataPropertyName = "Estado", HeaderText = "ESTADO", FillWeight = 22 });

            dgvReservas.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvReservas.Rows.Count) return;
                string col = dgvReservas.Columns[e.ColumnIndex].Name;

                if (col == "Estado" && e.Value != null)
                {
                    string est = e.Value.ToString()!;
                    if (est == "Reservado") { e.CellStyle.ForeColor = Color.FromArgb(234, 88, 12); e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
                    else if (est == "Listo para Retiro") { e.CellStyle.ForeColor = Color.FromArgb(37, 99, 235); e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
                    else if (est == "Entregado") { e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74); e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
                    else if (est == "Anulado") { e.CellStyle.ForeColor = Color.FromArgb(239, 68, 68); }
                }
                else if (col == "SaldoPendiente" && e.Value is decimal saldo)
                {
                    e.CellStyle.ForeColor = saldo > 0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(22, 163, 74);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            };
        }

        private void ConfigurarColumnasDetalle()
        {
            dgvDetalle.Columns.Clear();
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreProducto", DataPropertyName = "NombreProducto", HeaderText = "PRODUCTO", FillWeight = 50 });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", DataPropertyName = "Cantidad", HeaderText = "CANT.", FillWeight = 20 });
            
            var estiloM = new DataGridViewCellStyle { FormatProvider = new System.Globalization.CultureInfo("es-CL"), Format = "$ #,##0", Alignment = DataGridViewContentAlignment.MiddleRight };
            var colSubtotal = new DataGridViewTextBoxColumn { Name = "Subtotal", DataPropertyName = "Subtotal", HeaderText = "SUBTOTAL", FillWeight = 30, DefaultCellStyle = estiloM };
            colSubtotal.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDetalle.Columns.Add(colSubtotal);

            dgvDetalle.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvDetalle.Rows.Count) return;
                if (dgvDetalle.Columns[e.ColumnIndex].Name == "Cantidad" && dgvDetalle.Rows[e.RowIndex].DataBoundItem is ReservaDetalle item)
                {
                    e.Value = item.EsPesable ? $"{item.Cantidad}g" : $"{item.Cantidad}x";
                    e.FormattingApplied = true;
                }
            };
        }

        private void ConfigurarColumnasAbonos()
        {
            dgvAbonos.Columns.Clear();
            var colF = new DataGridViewTextBoxColumn { Name = "FechaPago", DataPropertyName = "FechaPago", HeaderText = "FECHA", FillWeight = 35 };
            colF.DefaultCellStyle.Format = "dd-MM HH:mm";
            dgvAbonos.Columns.Add(colF);

            var estiloM = new DataGridViewCellStyle { FormatProvider = new System.Globalization.CultureInfo("es-CL"), Format = "$ #,##0", Alignment = DataGridViewContentAlignment.MiddleRight };
            var colMonto = new DataGridViewTextBoxColumn { Name = "Monto", DataPropertyName = "Monto", HeaderText = "MONTO", FillWeight = 35, DefaultCellStyle = estiloM };
            colMonto.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvAbonos.Columns.Add(colMonto);

            dgvAbonos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedioPago", DataPropertyName = "MedioPago", HeaderText = "MEDIO", FillWeight = 30 });
        }

        private void CargarReservas()
        {
            try
            {
                string estado = cbFiltroEstado.SelectedItem?.ToString() ?? "Todos";
                string query = txtBuscar.Text.Trim();

                _listaReservas = _reservaService.ObtenerReservas(estado, query);
                dgvReservas.DataSource = null;
                dgvReservas.DataSource = _listaReservas;

                lblContadorFooter.Text = $"Mostrando {_listaReservas.Count} reservas registradas";

                if (_listaReservas.Count > 0)
                {
                    dgvReservas.Rows[0].Selected = true;
                    CargarSubDetalles(_listaReservas[0]);
                }
                else
                {
                    _reservaSeleccionada = null;
                    dgvDetalle.DataSource = null;
                    dgvAbonos.DataSource = null;
                    ActualizarBotonesAccion();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reservas: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvReservas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvReservas.CurrentRow?.DataBoundItem is Reserva res)
            {
                _reservaSeleccionada = res;
                CargarSubDetalles(res);
            }
        }

        private void CargarSubDetalles(Reserva res)
        {
            try
            {
                var detalles = _reservaService.ObtenerDetallesReserva(res.IdReserva);
                dgvDetalle.DataSource = detalles;

                var pagos = _reservaService.ObtenerPagosReserva(res.IdReserva);
                dgvAbonos.DataSource = pagos;

                ActualizarBotonesAccion();
            }
            catch { }
        }

        private void ActualizarBotonesAccion()
        {
            if (_reservaSeleccionada == null)
            {
                btnEntregarLiquidar.Enabled = false;
                btnRegistrarAbono.Enabled = false;
                btnMarcarListo.Enabled = false;
                btnReimprimir.Enabled = false;
                btnAnular.Enabled = false;
                return;
            }

            bool esActiva = _reservaSeleccionada.Estado == "Reservado" || _reservaSeleccionada.Estado == "Listo para Retiro";
            btnEntregarLiquidar.Enabled = esActiva;
            btnRegistrarAbono.Enabled = esActiva && _reservaSeleccionada.SaldoPendiente > 0;
            btnMarcarListo.Enabled = _reservaSeleccionada.Estado == "Reservado";
            btnReimprimir.Enabled = true;
            btnAnular.Enabled = esActiva;
        }

        private void BtnMarcarListo_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var r = MessageBox.Show($"¿Marcar el pedido N° {_reservaSeleccionada.CodigoReserva} como 'Listo para Retiro'?", "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                _reservaService.MarcarComoListoParaRetiro(_reservaSeleccionada.IdReserva);
                CargarReservas();
            }
        }

        private void BtnRegistrarAbono_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null || _reservaSeleccionada.SaldoPendiente <= 0) return;

            var turno = _cajaService.ObtenerTurnoAbierto(_usuarioActual.NombreUsuario);
            if (turno == null || turno.Estado != "Abierta")
            {
                MessageBox.Show("Debe tener una caja abierta para registrar ingresos de dinero por abonos.", "Caja Cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using Form modalAbono = new Form
            {
                Text = $"Registrar Abono - {_reservaSeleccionada.CodigoReserva}",
                Size = new Size(360, 260),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblInf = new Label { Text = $"Saldo actual: {MonedaHelper.Formatear(_reservaSeleccionada.SaldoPendiente, conSigno: true)}", Location = new Point(20, 15), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
            Label lblM = new Label { Text = "Monto a abonar ($):", Location = new Point(20, 48), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            TextBox txtM = new TextBox { Location = new Point(20, 70), Size = new Size(300, 26), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), MaxLength = 10 };
            txtM.TextChanged += (s, ev) => MonedaHelper.AplicarMascaraEnVivo(txtM);

            Label lblMp = new Label { Text = "Medio de Pago:", Location = new Point(20, 106), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            ComboBox cbMp = new ComboBox { Location = new Point(20, 128), Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbMp.Items.AddRange(new object[] { "Efectivo", "Débito", "Tarjeta Crédito", "Transferencia" });
            cbMp.SelectedIndex = 0;

            Button btnGuardar = new Button { Text = "✔ Guardar Abono", Location = new Point(20, 168), Size = new Size(300, 38), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnGuardar.FlatAppearance.BorderSize = 0;

            btnGuardar.Click += (s, ev) =>
            {
                decimal monto = MonedaHelper.Limpiar(txtM.Text);
                if (monto <= 0)
                {
                    MessageBox.Show("Ingrese un monto válido.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (monto > _reservaSeleccionada.SaldoPendiente)
                {
                    MessageBox.Show("El abono no puede superar el saldo pendiente.", "Monto Excedido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _reservaService.RegistrarAbono(_reservaSeleccionada.IdReserva, monto, cbMp.SelectedItem?.ToString() ?? "Efectivo", turno.CajaTurnoID, _usuarioActual.NombreUsuario);
                MessageBox.Show("Abono registrado e ingresado al turno de caja exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                modalAbono.DialogResult = DialogResult.OK;
                modalAbono.Close();
            };

            modalAbono.AcceptButton = btnGuardar;
            modalAbono.Controls.AddRange(new Control[] { lblInf, lblM, txtM, lblMp, cbMp, btnGuardar });

            if (modalAbono.ShowDialog(this) == DialogResult.OK)
            {
                CargarReservas();
            }
        }

        private void BtnEntregarLiquidar_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            decimal saldo = _reservaSeleccionada.SaldoPendiente;

            var r = MessageBox.Show(
                $"¿Desea generar el ticket de liquidación para el pedido N° {_reservaSeleccionada.CodigoReserva}?\n\n" +
                $"• Total Pedido: {MonedaHelper.Formatear(_reservaSeleccionada.TotalPedido, conSigno: true)}\n" +
                $"• Total Abonado: {MonedaHelper.Formatear(_reservaSeleccionada.TotalAbonado, conSigno: true)}\n" +
                $"• Saldo a Cobrar en Caja: {MonedaHelper.Formatear(saldo, conSigno: true)}\n\n" +
                "El ticket se enviará a Control de Caja para que el cliente pague el saldo y reciba su boleta.",
                "Entregar y Cobrar Saldo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (r != DialogResult.Yes) return;

            try
            {
                int nroTicket = _reservaService.GenerarTicketLiquidacionCaja(
                    _reservaSeleccionada.IdReserva,
                    _usuarioActual.NombreUsuario
                );

                MessageBox.Show(
                    $"¡TICKET N° {nroTicket} GENERADO CON ÉXITO!\n\n" +
                    $"Monto por pagar en Caja: {MonedaHelper.Formatear(saldo, conSigno: true)}\n" +
                    "Pase al cliente a Caja para procesar el cobro y entrega.",
                    "Ticket Enviado a Caja",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                CargarReservas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar ticket: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReimprimir_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var detalles = _reservaService.ObtenerDetallesReserva(_reservaSeleccionada.IdReserva);
            _reservaSeleccionada.Detalles = detalles;

            _ticketPrintService.ImprimirComprobanteReserva(_reservaSeleccionada, null, _usuarioActual.NombreUsuario);
        }

        private void BtnAnular_Click(object? sender, EventArgs e)
        {
            if (_reservaSeleccionada == null) return;

            var r = MessageBox.Show($"¿Está seguro de anular la Reserva N° {_reservaSeleccionada.CodigoReserva}?\n\n⚠️ Los abonos quedarán registrados en el historial de la caja.", "Anulación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                _reservaService.AnularReserva(_reservaSeleccionada.IdReserva);
                CargarReservas();
            }
        }

        // =========================================================================
        // MÉTODOS DE DIBUJO Y DISEÑO (ESTÁNDAR FORMCLIENTES)
        // =========================================================================
        private Button CrearBotonAccionDock(string texto, Color bg, Color fore)
        {
            Button btn = new Button
            {
                Text = texto,
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = bg,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Panel CrearEspaciador(int alto)
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = alto,
                BackColor = Color.Transparent
            };
        }

        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Resize += (s, e) => pnl.Invalidate();
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.BackColor);
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 10);
                using Pen pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
        }

        private GraphicsPath CrearRutaRedondeada(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Panel CrearGrupoConCaja(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            Panel pnlInputBox = new Panel { Location = new Point(0, 18), Size = new Size(ancho, 28), BackColor = Color.FromArgb(248, 250, 252) };
            pnlInputBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlInputBox.Width - 1, pnlInputBox.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(r, 6);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            control.Location = new Point(6, 3);
            control.Width = ancho - 12;
            pnlInputBox.Controls.Add(control);

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(pnlInputBox);
            return pnl;
        }

        private Panel CrearGrupoLimpio(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            control.Location = new Point(0, 18);
            control.Width = ancho;

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private Button CrearBoton(string texto, Color back, Color fore, Size size)
        {
            Button b = new Button 
            { 
                Text = texto, 
                Size = size, 
                BackColor = back, 
                ForeColor = fore, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}