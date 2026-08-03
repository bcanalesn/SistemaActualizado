using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormHistorialVentas : Form
    {
        private AppDbContext _db = new AppDbContext();

        // Controles de Filtro Superior
        private TextBox txtBuscarFolio = null!;
        private DateTimePicker dtpDesde = null!;
        private DateTimePicker dtpHasta = null!;
        private ComboBox cbTipoDocumento = null!;
        private Button btnBuscar = null!;
        private Button btnLimpiar = null!;
        private Button btnVerHoy = null!;

        // Buttons de Filtros Rápidos
        private Button btnFiltroHoy = null!;
        private Button btnFiltroAyer = null!;
        private Button btnFiltro7Dias = null!;
        private Button btnFiltroEsteMes = null!;
        private Button btnFiltroMesAnterior = null!;

        // DataGridView Principal
        private DataGridView dgvVentas = null!;

        // Controles de Resumen Lateral
        private Label lblDetTipoDoc = null!;
        private Label lblDetFolio = null!;
        private Label lblDetFecha = null!;
        private Label lblDetMedio = null!;
        private Label lblDetCliente = null!;
        private Label lblDetNeto = null!;
        private Label lblDetIva = null!;
        private Label lblDetTotal = null!;

        private Button btnReimprimir = null!;
        private Button btnNotaCredito = null!;

        // Paginador
        private Label lblPaginadorInfo = null!;

        private Venta? _ventaSeleccionada = null;
        private List<Venta> _ventasCargadas = new List<Venta>();

        public FormHistorialVentas()
        {
            InitializeComponent();
            AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 12, 24, 24),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Consulta, revisa y gestiona todas las ventas realizadas",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 26
            };

            // ------------------------------------------------------------------
            // 2. TARJETA DE FILTROS Y BÚSQUEDA AVANZADA
            // ------------------------------------------------------------------
            Panel pnlFiltrosCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.White,
                Padding = new Padding(18, 12, 18, 12),
                Margin = new Padding(0, 0, 0, 16)
            };
            pnlFiltrosCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlFiltrosCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            // Fila 1 de Filtros
            Label lblFolio = new Label { Text = "Folio / Ticket", Location = new Point(18, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            txtBuscarFolio = new TextBox
            {
                Location = new Point(18, 32),
                Size = new Size(185, 28),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            txtBuscarFolio.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { EjcutarBusqueda(); e.SuppressKeyPress = true; } };

            Label lblDesde = new Label { Text = "Fecha desde", Location = new Point(215, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            dtpDesde = new DateTimePicker { Location = new Point(215, 32), Size = new Size(120, 28), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };

            Label lblHasta = new Label { Text = "Fecha hasta", Location = new Point(345, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            dtpHasta = new DateTimePicker { Location = new Point(345, 32), Size = new Size(120, 28), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F) };

            Label lblTipoDoc = new Label { Text = "Tipo Documento", Location = new Point(475, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            cbTipoDocumento = new ComboBox
            {
                Location = new Point(475, 32),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbTipoDocumento.Items.AddRange(new string[] { "Todos", "Boleta Electrónica", "Factura Electrónica", "Guía de Despacho", "Nota de Crédito" });
            cbTipoDocumento.SelectedIndex = 0;

            btnBuscar = CrearBotonEstilizado("🔍 Buscar", Color.FromArgb(2, 132, 199), Color.White, new Point(635, 29), new Size(100, 32));
            btnBuscar.Click += (s, e) => EjcutarBusqueda();

            btnLimpiar = CrearBotonEstilizado("🔄 Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Point(743, 29), new Size(95, 32));
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            // Fila 2: Filtros Rápidos (Pills)
            Label lblFiltrosRapidos = new Label { Text = "Filtros rápidos:", Location = new Point(18, 78), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };

            btnFiltroHoy = CrearPillBoton("📅 Hoy", 115, 75, 70, (s, e) => AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroAyer = CrearPillBoton("📅 Ayer", 190, 75, 70, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-1), DateTime.Today.AddTicks(-1)));
            btnFiltro7Dias = CrearPillBoton("📅 Últimos 7 días", 265, 75, 115, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroEsteMes = CrearPillBoton("📅 Este mes", 385, 75, 90, (s, e) => AplicarFiltroFecha(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroMesAnterior = CrearPillBoton("📅 Mes anterior", 480, 75, 105, (s, e) =>
            {
                var inicioMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                var finMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddTicks(-1);
                AplicarFiltroFecha(inicioMesAnt, finMesAnt);
            });

            btnVerHoy = CrearBotonEstilizado("📅 Ver hoy", Color.FromArgb(241, 245, 249), Color.FromArgb(15, 23, 42), new Point(743, 73), new Size(95, 30));
            btnVerHoy.Click += (s, e) => AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));

            pnlFiltrosCard.Controls.AddRange(new Control[] {
                lblFolio, txtBuscarFolio, lblDesde, dtpDesde, lblHasta, dtpHasta, lblTipoDoc, cbTipoDocumento,
                btnBuscar, btnLimpiar, lblFiltrosRapidos, btnFiltroHoy, btnFiltroAyer, btnFiltro7Dias,
                btnFiltroEsteMes, btnFiltroMesAnterior, btnVerHoy
            });

            // ------------------------------------------------------------------
            // 3. SECCIÓN PRINCIPAL SPLIT (IZQ: TABLA | DER: DETALLE CARD)
            // ------------------------------------------------------------------
            TableLayoutPanel pnlGridAndDetail = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 480,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0)
            };
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));

            // A) CARD IZQUIERDA - TABLA DTE
            Panel pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 10, 0)
            };
            pnlTableCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTableCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            dgvVentas = new DataGridView
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
            ConfigurarEstiloTabla(dgvVentas);
            dgvVentas.SelectionChanged += DgvVentas_SelectionChanged;
            dgvVentas.CellFormatting += DgvVentas_CellFormatting;

            Panel pnlPaginador = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5, 5, 5, 0)
            };

            lblPaginadorInfo = new Label
            {
                Text = "Mostrando registros...",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            pnlPaginador.Controls.Add(lblPaginadorInfo);

            pnlTableCard.Controls.Add(dgvVentas);
            pnlTableCard.Controls.Add(pnlPaginador);

            // B) CARD DERECHA - RESUMEN DEL DOCUMENTO
            Panel pnlDetailCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlDetailCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlDetailCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            Label lblTituloDetalle = new Label
            {
                Text = "📄 Resumen del Documento",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 32
            };

            Panel pnlCamposDetalle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 240,
                Padding = new Padding(0, 8, 0, 0)
            };

            int yPos = 0;
            lblDetTipoDoc = CrearRenglonDetalle("Tipo Documento", "-", ref yPos, pnlCamposDetalle);
            lblDetFolio = CrearRenglonDetalle("Folio DTE N°", "-", ref yPos, pnlCamposDetalle);
            lblDetFecha = CrearRenglonDetalle("Fecha Emisión", "-", ref yPos, pnlCamposDetalle);
            lblDetMedio = CrearRenglonDetalle("Medio de Pago", "-", ref yPos, pnlCamposDetalle);
            lblDetCliente = CrearRenglonDetalle("Cliente / RUT", "-", ref yPos, pnlCamposDetalle);
            lblDetNeto = CrearRenglonDetalle("Monto Neto", "$ 0", ref yPos, pnlCamposDetalle);
            lblDetIva = CrearRenglonDetalle("IVA (19%)", "$ 0", ref yPos, pnlCamposDetalle);

            // Card Destacada para el Total
            Panel pnlTotalHighlight = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 10, 0, 10)
            };
            pnlTotalHighlight.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTotalHighlight.ClientRectangle, Color.FromArgb(186, 230, 253), ButtonBorderStyle.Solid);
            };

            Label lblTotalCaption = new Label
            {
                Text = "Monto Total",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(3, 105, 161),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 18
            };

            lblDetTotal = new Label
            {
                Text = "$ 0",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(2, 132, 199),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTotalHighlight.Controls.Add(lblDetTotal);
            pnlTotalHighlight.Controls.Add(lblTotalCaption);

            // Botones de Acción
            Panel pnlBotonesAccion = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(0, 8, 0, 0)
            };

            btnReimprimir = new Button
            {
                Text = "🖨️ Reimprimir DTE",
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnReimprimir.FlatAppearance.BorderSize = 0;
            btnReimprimir.Click += BtnReimprimir_Click;

            Panel pnlSpacerBtn = new Panel { Dock = DockStyle.Top, Height = 8 };

            btnNotaCredito = new Button
            {
                Text = "📄 Emitir Nota de Crédito",
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnNotaCredito.FlatAppearance.BorderSize = 0;
            btnNotaCredito.Click += BtnNotaCredito_Click;

            pnlBotonesAccion.Controls.Add(btnNotaCredito);
            pnlBotonesAccion.Controls.Add(pnlSpacerBtn);
            pnlBotonesAccion.Controls.Add(btnReimprimir);

            pnlDetailCard.Controls.Add(pnlBotonesAccion);
            pnlDetailCard.Controls.Add(pnlTotalHighlight);
            pnlDetailCard.Controls.Add(pnlCamposDetalle);
            pnlDetailCard.Controls.Add(lblTituloDetalle);

            pnlGridAndDetail.Controls.Add(pnlTableCard, 0, 0);
            pnlGridAndDetail.Controls.Add(pnlDetailCard, 1, 0);

            // ------------------------------------------------------------------
            // 4. FOOTER DE SEGURIDAD
            // ------------------------------------------------------------------
            Label lblFooterSecurity = new Label
            {
                Text = "🛡️ Información protegida y encriptada   |   v2.0  •  Corporación Santo Tomás",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlMain.Controls.Add(pnlGridAndDetail);
            pnlMain.Controls.Add(pnlFiltrosCard);
            pnlMain.Controls.Add(lblSubtitulo);

            this.Controls.Add(pnlMain);
            this.Controls.Add(lblFooterSecurity);

            this.ResumeLayout(false);
        }

        private Button CrearBotonEstilizado(string texto, Color back, Color fore, Point loc, Size size)
        {
            Button b = new Button
            {
                Text = texto,
                Location = loc,
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

        private Button CrearPillBoton(string texto, int x, int y, int ancho, EventHandler onClick)
        {
            Button b = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 26),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private Label CrearRenglonDetalle(string titulo, string valorInicial, ref int y, Panel container)
        {
            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, y),
                AutoSize = true
            };

            Label lblV = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, y + 15),
                AutoSize = true
            };

            container.Controls.Add(lblT);
            container.Controls.Add(lblV);

            y += 34;
            return lblV;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void DgvVentas_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = dgvVentas.Columns[e.ColumnIndex].Name;

            if (colName == "MedioPago" && e.Value != null)
            {
                string mp = e.Value.ToString() ?? "";
                if (mp.Contains("Efectivo")) e.Value = "💵 " + mp;
                else if (mp.Contains("Tarjeta")) e.Value = "💳 " + mp;
                else if (mp.Contains("Transferencia")) e.Value = "🏛️ " + mp;
                else if (mp.Contains("Nota")) e.Value = "📄 " + mp;
            }

            if (colName == "TipoDocumento" && e.Value != null)
            {
                string tipo = e.Value.ToString() ?? "";
                if (tipo.Contains("Nota de Crédito"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (tipo.Contains("Factura"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(3, 105, 161);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            }
        }

        private void AplicarFiltroFecha(DateTime desde, DateTime hasta)
        {
            dtpDesde.Value = desde.Date;
            dtpHasta.Value = hasta.Date;
            CargarVentas(desde, hasta);
        }

        private void CargarVentas(DateTime desde, DateTime hasta)
        {
            try
            {
                _ventasCargadas = _db.Ventas
                    .Where(v => v.Fecha >= desde && v.Fecha <= hasta)
                    .OrderByDescending(v => v.Fecha)
                    .ToList();

                FiltrarLocalmente();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EjcutarBusqueda()
        {
            FiltrarLocalmente();
        }

        private void LimpiarFiltros()
        {
            txtBuscarFolio.Clear();
            cbTipoDocumento.SelectedIndex = 0;
            dtpDesde.Value = DateTime.Today;
            dtpHasta.Value = DateTime.Today;
            CargarVentas(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void FiltrarLocalmente()
        {
            var resultado = _ventasCargadas.AsQueryable();

            string textoBusqueda = txtBuscarFolio.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(textoBusqueda))
            {
                resultado = resultado.Where(v => v.FolioDTE.ToString().Contains(textoBusqueda) ||
                                                 v.VentaID.ToString().Contains(textoBusqueda) ||
                                                 (!string.IsNullOrEmpty(v.RutCliente) && v.RutCliente.ToLower().Contains(textoBusqueda)));
            }

            string tipoSel = cbTipoDocumento.SelectedItem?.ToString() ?? "Todos";
            if (tipoSel != "Todos")
            {
                resultado = resultado.Where(v => v.TipoDocumento.Contains(tipoSel));
            }

            var listaFinal = resultado.ToList();
            dgvVentas.DataSource = listaFinal;

            if (dgvVentas.Columns["VentaID"] != null) { dgvVentas.Columns["VentaID"].HeaderText = "ID Venta"; dgvVentas.Columns["VentaID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; dgvVentas.Columns["VentaID"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvVentas.Columns["Fecha"] != null) { dgvVentas.Columns["Fecha"].HeaderText = "Fecha / Hora"; dgvVentas.Columns["Fecha"].DefaultCellStyle.Format = "dd-MM-yyyy HH:mm"; }
            if (dgvVentas.Columns["Total"] != null) { dgvVentas.Columns["Total"].HeaderText = "Total"; dgvVentas.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; dgvVentas.Columns["Total"].DefaultCellStyle.ForeColor = Color.FromArgb(2, 132, 199); dgvVentas.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
            if (dgvVentas.Columns["MedioPago"] != null) dgvVentas.Columns["MedioPago"].HeaderText = "Medio de Pago";
            if (dgvVentas.Columns["Usuario"] != null) dgvVentas.Columns["Usuario"].HeaderText = "Cajero";
            if (dgvVentas.Columns["TipoDocumento"] != null) dgvVentas.Columns["TipoDocumento"].HeaderText = "Documento";
            if (dgvVentas.Columns["FolioDTE"] != null) { dgvVentas.Columns["FolioDTE"].HeaderText = "Folio DTE"; dgvVentas.Columns["FolioDTE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; dgvVentas.Columns["FolioDTE"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvVentas.Columns["RutCliente"] != null) dgvVentas.Columns["RutCliente"].HeaderText = "Cliente / RUT";
            if (dgvVentas.Columns["RazonSocial"] != null) dgvVentas.Columns["RazonSocial"].HeaderText = "Razón Social";

            // Ocultar campos técnicos de base de datos
            if (dgvVentas.Columns["Neto"] != null) dgvVentas.Columns["Neto"].Visible = false;
            if (dgvVentas.Columns["IVA"] != null) dgvVentas.Columns["IVA"].Visible = false;
            if (dgvVentas.Columns["Giro"] != null) dgvVentas.Columns["Giro"].Visible = false;
            if (dgvVentas.Columns["idREF"] != null) dgvVentas.Columns["idREF"].Visible = false;
            if (dgvVentas.Columns["nroREF"] != null) dgvVentas.Columns["nroREF"].Visible = false;
            if (dgvVentas.Columns["codigoREF"] != null) dgvVentas.Columns["codigoREF"].Visible = false;
            if (dgvVentas.Columns["Direccion"] != null) dgvVentas.Columns["Direccion"].Visible = false;
            if (dgvVentas.Columns["Comuna"] != null) dgvVentas.Columns["Comuna"].Visible = false;
            if (dgvVentas.Columns["Ciudad"] != null) dgvVentas.Columns["Ciudad"].Visible = false;
            if (dgvVentas.Columns["EstadoDTE"] != null) dgvVentas.Columns["EstadoDTE"].Visible = false;
            if (dgvVentas.Columns["Glosa"] != null) dgvVentas.Columns["Glosa"].Visible = false;

            lblPaginadorInfo.Text = $"Mostrando {listaFinal.Count} de {_ventasCargadas.Count} registros";
        }

        private void DgvVentas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvVentas.CurrentRow != null && dgvVentas.CurrentRow.DataBoundItem is Venta venta)
            {
                _ventaSeleccionada = venta;
                btnReimprimir.Enabled = true;

                btnNotaCredito.Enabled = !venta.TipoDocumento.Equals("Nota de Crédito Electrónica", StringComparison.OrdinalIgnoreCase);

                lblDetTipoDoc.Text = venta.TipoDocumento;
                lblDetFolio.Text = $"#{venta.FolioDTE:D6}";
                lblDetFecha.Text = venta.Fecha.ToString("dd-MM-yyyy HH:mm");
                lblDetMedio.Text = venta.MedioPago;
                lblDetCliente.Text = string.IsNullOrEmpty(venta.RutCliente) ? "Consumidor Final" : $"{venta.RutCliente}";
                lblDetNeto.Text = $"$ {venta.Neto:N0}";
                lblDetIva.Text = $"$ {venta.IVA:N0}";
                lblDetTotal.Text = $"$ {venta.Total:N0}";
            }
            else
            {
                _ventaSeleccionada = null;
                btnReimprimir.Enabled = false;
                btnNotaCredito.Enabled = false;

                lblDetTipoDoc.Text = "-";
                lblDetFolio.Text = "-";
                lblDetFecha.Text = "-";
                lblDetMedio.Text = "-";
                lblDetCliente.Text = "-";
                lblDetNeto.Text = "$ 0";
                lblDetIva.Text = "$ 0";
                lblDetTotal.Text = "$ 0";
            }
        }

        private void BtnReimprimir_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            var itemsEjemplo = new List<DetalleCarrito>
            {
                new DetalleCarrito { ProductoID = 1, Nombre = "Glosa DTE - " + _ventaSeleccionada.TipoDocumento, PrecioUnitario = _ventaSeleccionada.Total, Cantidad = 1 }
            };

            FormTicket formTicket = new FormTicket(_ventaSeleccionada, itemsEjemplo, _ventaSeleccionada.Total, 0);
            formTicket.ShowDialog();
        }

        private void BtnNotaCredito_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            Form modalNC = new Form
            {
                Text = $"Emitir Nota de Crédito - Referencia a Folio {_ventaSeleccionada.FolioDTE}",
                Size = new Size(460, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label
            {
                Text = $"📄 Emitir Nota de Crédito Electrónica",
                Location = new Point(25, 18),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                AutoSize = true
            };

            Label lblInfo = new Label
            {
                Text = $"Documento de Origen: {_ventaSeleccionada.TipoDocumento} N° {_ventaSeleccionada.FolioDTE}\n" +
                       $"Monto Total Afecto: ${_ventaSeleccionada.Total:N0} (Neto: ${_ventaSeleccionada.Neto:N0} | IVA: ${_ventaSeleccionada.IVA:N0})",
                Location = new Point(25, 48),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true
            };

            Label lblCausa = new Label { Text = "Código de Referencia SII:", Location = new Point(25, 100), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbCausa = new ComboBox { Location = new Point(25, 122), Size = new Size(390, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbCausa.Items.AddRange(new string[] {
                "1 - Anula Documento de Referencia (Anulación / Devolución Total)",
                "2 - Corregir Texto en Documento (Glosa / Datos)",
                "3 - Corregir Montos / Descuento Parcial"
            });
            cbCausa.SelectedIndex = 0;

            Label lblMotivo = new Label { Text = "Motivo / Glosa de la Anulación:", Location = new Point(25, 165), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtMotivo = new TextBox { Text = "Devolución de mercadería / Anulación de venta", Location = new Point(25, 187), Size = new Size(390, 28), Font = new Font("Segoe UI", 9.5F) };

            CheckBox chkRestock = new CheckBox
            {
                Text = "🔄 Reintegrar automáticamente los productos al stock del inventario",
                Location = new Point(25, 230),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(3, 105, 161)
            };

            Label lblAvisoFolio = new Label
            {
                Text = "Se utilizará automáticamente el siguiente Folio de 'Nota de Crédito Electrónica' disponible en el sistema.",
                Location = new Point(25, 270),
                Size = new Size(390, 40),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            Button btnEmitirNC = new Button
            {
                Text = "⚡ GENERAR Y EMITIR NOTA DE CRÉDITO",
                Location = new Point(25, 330),
                Size = new Size(390, 48),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEmitirNC.FlatAppearance.BorderSize = 0;

            btnEmitirNC.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtMotivo.Text))
                {
                    MessageBox.Show("Debe ingresar un motivo o glosa para la emisión de la Nota de Crédito.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string codigoRefSII = (cbCausa.SelectedIndex + 1).ToString();

                try
                {
                    var rangoFolioNC = _db.Folios.FirstOrDefault(f => f.TipoDocumento.Contains("Nota de Crédito") && f.Activo);
                    int folioNC = 0;

                    if (rangoFolioNC != null)
                    {
                        folioNC = rangoFolioNC.FolioActual;
                        rangoFolioNC.FolioActual += 1;

                        if (rangoFolioNC.FolioActual > rangoFolioNC.FolioHasta)
                        {
                            rangoFolioNC.Activo = false;
                        }
                    }
                    else
                    {
                        folioNC = (int)(DateTime.Now.Ticks % 100000);
                    }

                    Venta notaCreditoVenta = new Venta
                    {
                        Fecha = DateTime.Now,
                        Total = _ventaSeleccionada.Total,
                        Neto = _ventaSeleccionada.Neto,
                        IVA = _ventaSeleccionada.IVA,
                        MedioPago = $"Nota de Crédito ({_ventaSeleccionada.MedioPago})",
                        TipoDocumento = "Nota de Crédito Electrónica",
                        FolioDTE = folioNC,
                        RutCliente = _ventaSeleccionada.RutCliente,
                        RazonSocial = _ventaSeleccionada.RazonSocial,
                        Giro = _ventaSeleccionada.Giro,
                        idREF = _ventaSeleccionada.VentaID,
                        nroREF = _ventaSeleccionada.FolioDTE,
                        codigoREF = codigoRefSII,
                        Usuario = _ventaSeleccionada.Usuario
                    };

                    _db.Ventas.Add(notaCreditoVenta);

                    if (chkRestock.Checked && codigoRefSII == "1")
                    {
                        var productosCache = _db.Productos.ToList();
                        foreach (var prod in productosCache)
                        {
                            prod.Stock += 1;
                        }
                    }

                    _db.SaveChanges();

                    MessageBox.Show($"¡Nota de Crédito Electrónica N° {folioNC} emitida exitosamente para la {_ventaSeleccionada.TipoDocumento} Ref N° {_ventaSeleccionada.FolioDTE}!", "DTE Emitido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    modalNC.Close();

                    var itemsNC = new List<DetalleCarrito>
                    {
                        new DetalleCarrito { ProductoID = 1, Nombre = $"Nota Crédito Ref {_ventaSeleccionada.TipoDocumento} N°{_ventaSeleccionada.FolioDTE}", PrecioUnitario = notaCreditoVenta.Total, Cantidad = 1 }
                    };

                    FormTicket ticketNC = new FormTicket(notaCreditoVenta, itemsNC, notaCreditoVenta.Total, 0);
                    ticketNC.ShowDialog();

                    AplicarFiltroFecha(dtpDesde.Value.Date, dtpHasta.Value.Date.AddDays(1).AddTicks(-1));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al emitir la Nota de Crédito: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modalNC.Controls.AddRange(new Control[] { lblTitle, lblInfo, lblCausa, cbCausa, lblMotivo, txtMotivo, chkRestock, lblAvisoFolio, btnEmitirNC });
            modalNC.ShowDialog();
        }
    }
}