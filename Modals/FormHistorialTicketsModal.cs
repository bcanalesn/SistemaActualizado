using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormHistorialTicketsModal : Form
    {
        private readonly VentaService _ventaService = new VentaService();
        private readonly List<string> _listaVendedores;
        private string _vendedorSeleccionado;

        private ComboBox cbVendedorModal = null!;
        private ComboBox cbEstado = null!;
        private DateTimePicker dtpFecha = null!;
        private TextBox txtBuscar = null!;
        private Button btnBuscar = null!;
        private DataGridView dgvTickets = null!;
        private Label lblTotalTicketsKpi = null!;
        private Label lblEnCajaKpi = null!;
        private Label lblPagadosKpi = null!;
        private Label lblAnuladosKpi = null!;
        private Label lblFooter = null!;

        public FormHistorialTicketsModal(string vendedorActual, List<string> listaVendedores)
        {
            _vendedorSeleccionado = vendedorActual;
            _listaVendedores = listaVendedores;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarTickets();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Historial de Tickets por Vendedor";
            this.Size = new Size(1080, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            // =========================================================================
            // 1. ENCABEZADO (TÍTULO Y SUBTÍTULO)
            // =========================================================================
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 52, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 0, 0, 8) 
            };

            Label lblTitulo = new Label
            {
                Text = "🎟️ Historial de Tickets",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 24
            };

            Label lblSub = new Label
            {
                Text = "Consulta el estado de atención de las ventas emitidas (Modo Solo Lectura)",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 18
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitulo);

            // =========================================================================
            // 2. MINI KPIS RESPONSIVOS (Tarjeta redondeada de 80px)
            // =========================================================================
            Panel pnlKpisWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var card1 = CrearCardKpi("TOTAL TICKETS", out lblTotalTicketsKpi, "0", Color.FromArgb(15, 23, 42), Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240), Color.FromArgb(100, 116, 139));
            var card2 = CrearCardKpi("ENVIADOS A CAJA", out lblEnCajaKpi, "0", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199), Color.FromArgb(253, 230, 138), Color.FromArgb(217, 119, 6));
            var card3 = CrearCardKpi("PAGADOS", out lblPagadosKpi, "0", Color.FromArgb(21, 128, 61), Color.FromArgb(240, 253, 244), Color.FromArgb(187, 247, 208), Color.FromArgb(22, 163, 74));
            var card4 = CrearCardKpi("ANULADOS", out lblAnuladosKpi, "0", Color.FromArgb(185, 28, 28), Color.FromArgb(254, 242, 242), Color.FromArgb(254, 202, 202), Color.FromArgb(220, 38, 38));

            card1.Margin = new Padding(0, 0, 6, 0);
            card2.Margin = new Padding(6, 0, 6, 0);
            card3.Margin = new Padding(6, 0, 6, 0);
            card4.Margin = new Padding(6, 0, 0, 0);

            tlpKpis.Controls.Add(card1, 0, 0);
            tlpKpis.Controls.Add(card2, 1, 0);
            tlpKpis.Controls.Add(card3, 2, 0);
            tlpKpis.Controls.Add(card4, 3, 0);

            tlpKpis.Resize += (s, e) => tlpKpis.Invalidate(true);
            pnlKpisWrapper.Controls.Add(tlpKpis);

            // =========================================================================
            // 3. BARRA DE FILTROS EN TARJETA REDONDEADA
            // =========================================================================
            Panel pnlFiltrosWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 72, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 12, 16, 12);

            FlowLayoutPanel flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            cbVendedorModal = new ComboBox { Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            foreach (var v in _listaVendedores) cbVendedorModal.Items.Add(v);
            if (cbVendedorModal.Items.Contains(_vendedorSeleccionado)) cbVendedorModal.SelectedItem = _vendedorSeleccionado;
            else if (cbVendedorModal.Items.Count > 0) cbVendedorModal.SelectedIndex = 0;
            cbVendedorModal.SelectedIndexChanged += (s, e) => { _vendedorSeleccionado = cbVendedorModal.SelectedItem?.ToString() ?? ""; CargarTickets(); };
            Panel pnlGrpVendedor = CrearGrupoLimpio("VENDEDOR", cbVendedorModal, 145);

            dtpFecha = new DateTimePicker { Size = new Size(115, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 9F) };
            dtpFecha.ValueChanged += (s, e) => CargarTickets();
            Panel pnlGrpFecha = CrearGrupoLimpio("FECHA", dtpFecha, 120);

            cbEstado = new ComboBox { Size = new Size(130, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbEstado.Items.AddRange(new object[] { "Todos", "Enviado a caja", "Pagado", "Anulado" });
            cbEstado.SelectedIndex = 0;
            cbEstado.SelectedIndexChanged += (s, e) => CargarTickets();
            Panel pnlGrpEstado = CrearGrupoLimpio("ESTADO", cbEstado, 135);

            txtBuscar = new TextBox { Size = new Size(240, 26), Font = new Font("Segoe UI", 9.5F), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(248, 250, 252), PlaceholderText = "Buscar ticket o cliente..." };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarTickets(); e.SuppressKeyPress = true; } };
            Panel pnlGrpBuscar = CrearGrupoConCaja("BUSCAR", txtBuscar, 255);

            btnBuscar = CrearBoton("🔍 Filtrar", Color.FromArgb(37, 99, 235), Color.White, new Size(95, 32));
            btnBuscar.Margin = new Padding(8, 14, 0, 0);
            btnBuscar.Click += (s, e) => CargarTickets();

            flpFiltros.Controls.AddRange(new Control[] { pnlGrpVendedor, pnlGrpFecha, pnlGrpEstado, pnlGrpBuscar, btnBuscar });
            pnlFiltrosCard.Controls.Add(flpFiltros);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // =========================================================================
            // 4. TARJETA DE GRILLA PRINCIPAL REDONDEADA
            // =========================================================================
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

            dgvTickets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 36 }
            };

            dgvTickets.EnableHeadersVisualStyles = false;
            dgvTickets.ColumnHeadersHeight = 36;
            dgvTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvTickets.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTickets.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvTickets.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvTickets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvTickets.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvTickets.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvTickets.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

            dgvTickets.Columns.Clear();
            dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdTve", Visible = false });
            dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ticket", HeaderText = "N° TICKET", FillWeight = 14, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235) } });
            dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "FECHA", FillWeight = 12 });
            dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hora", HeaderText = "HORA", FillWeight = 10 });
            dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "CLIENTE", FillWeight = 36, DefaultCellStyle = { Font = new Font("Segoe UI", 9F, FontStyle.Bold) } });
            dgvTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Total",
                HeaderText = "TOTAL",
                FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                    Format = "$ #,##0",
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                }
            });

            DataGridViewTextBoxColumn colEstado = new DataGridViewTextBoxColumn
            {
                Name = "Estado",
                HeaderText = "ESTADO",
                FillWeight = 18,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgvTickets.Columns.Add(colEstado);

            dgvTickets.CellPainting += DgvTickets_CellPainting;
            dgvTickets.CellDoubleClick += (s, e) =>
            {
                if (dgvTickets.CurrentRow != null && int.TryParse(dgvTickets.CurrentRow.Cells["IdTve"].Value?.ToString(), out int idTve))
                {
                    var venta = _ventaService.ObtenerVentaPorId(idTve);
                    if (venta != null)
                    {
                        var modal = new FormDetalleVentaModal(venta);
                        modal.ShowDialog(this);
                    }
                }
            };

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblFooter = new Label
            {
                Text = "Mostrando 0 tickets",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblFooter);

            pnlGridCard.Controls.Add(dgvTickets);
            pnlGridCard.Controls.Add(pnlFooter);

            // Orden Z estricto
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlFiltrosWrapper);
            pnlMain.Controls.Add(pnlKpisWrapper);
            pnlMain.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void DgvTickets_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvTickets.Columns["Estado"].Index)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

                string textoEstado = e.Value?.ToString() ?? "";
                Color bgPill, borderPill, dotColor, textColor;

                if (textoEstado.Contains("Pagado"))
                {
                    bgPill = Color.FromArgb(240, 253, 244);
                    borderPill = Color.FromArgb(187, 247, 208);
                    dotColor = Color.FromArgb(22, 163, 74);
                    textColor = Color.FromArgb(21, 128, 61);
                }
                else if (textoEstado.Contains("Enviado"))
                {
                    bgPill = Color.FromArgb(254, 243, 199);
                    borderPill = Color.FromArgb(253, 230, 138);
                    dotColor = Color.FromArgb(217, 119, 6);
                    textColor = Color.FromArgb(180, 83, 9);
                }
                else
                {
                    bgPill = Color.FromArgb(254, 242, 242);
                    borderPill = Color.FromArgb(254, 202, 202);
                    dotColor = Color.FromArgb(220, 38, 38);
                    textColor = Color.FromArgb(185, 28, 28);
                }

                string textoLimpio = textoEstado.Replace("🟢", "").Replace("🟡", "").Replace("🔴", "").Trim();

                Graphics g = e.Graphics!;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int badgeW = Math.Min(e.CellBounds.Width - 16, 130);
                int badgeH = 24;
                int badgeX = e.CellBounds.X + (e.CellBounds.Width - badgeW) / 2;
                int badgeY = e.CellBounds.Y + (e.CellBounds.Height - badgeH) / 2;

                Rectangle rBadge = new Rectangle(badgeX, badgeY, badgeW, badgeH);

                using (GraphicsPath path = CrearRutaRedondeada(rBadge, 12))
                {
                    using (SolidBrush sb = new SolidBrush(bgPill)) g.FillPath(sb, path);
                    using (Pen pen = new Pen(borderPill, 1f)) g.DrawPath(pen, path);
                }

                int dotSize = 8;
                int dotX = badgeX + 10;
                int dotY = badgeY + (badgeH - dotSize) / 2;
                using (SolidBrush dotBrush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }

                using (Font f = new Font("Segoe UI", 8.5F, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(textColor))
                {
                    Rectangle textRect = new Rectangle(badgeX + 22, badgeY, badgeW - 24, badgeH);
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    g.DrawString(textoLimpio, f, textBrush, textRect, sf);
                }

                e.Handled = true;
            }
        }

        private Panel CrearCardKpi(string titulo, out Label lblVal, string valIni, Color colTxt, Color colBg, Color colBorder, Color colPunto)
        {
            Panel card = CrearTarjetaRedondeada(0, 0, 0, 80, colBg, colBorder);
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(12, 10, 12, 8);

            Label lblT = new Label
            {
                Text = $"●  {titulo}",
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = colPunto,
                BackColor = Color.Transparent
            };

            lblVal = new Label
            {
                Text = valIni,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = colTxt,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { lblVal, lblT });
            return card;
        }

        private void CargarTickets()
        {
            string vendedor = cbVendedorModal.SelectedItem?.ToString() ?? _vendedorSeleccionado;
            DateTime fecha = dtpFecha.Value;
            string estado = cbEstado.SelectedItem?.ToString() ?? "Todos";
            string busqueda = txtBuscar.Text.Trim();

            var tickets = _ventaService.ObtenerMisTickets(vendedor, fecha, estado, busqueda);

            dgvTickets.Rows.Clear();
            foreach (var t in tickets)
            {
                dgvTickets.Rows.Add(
                    t.IdTve,
                    $"#{t.NroTicket:D6}",
                    t.FechaHora.ToString("dd/MM/yyyy"),
                    t.FechaHora.ToString("HH:mm:ss"),
                    t.Cliente,
                    t.Total,
                    t.EstadoVisual
                );
            }

            lblTotalTicketsKpi.Text = tickets.Count.ToString();
            lblEnCajaKpi.Text = tickets.Count(t => t.EstadoBD == "Pendiente").ToString();
            lblPagadosKpi.Text = tickets.Count(t => t.EstadoBD == "Emitido").ToString();
            lblAnuladosKpi.Text = tickets.Count(t => t.EstadoBD == "Anulado").ToString();

            lblFooter.Text = $"Mostrando {tickets.Count} tickets de {vendedor}  |  Doble clic en un ticket para ver sus productos (modo lectura)";
        }

        // =========================================================================
        // MÉTODOS AUXILIARES: BOTONES FLAT NATIVOS Y TARJETAS SUAVES
        // =========================================================================
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
    }
}