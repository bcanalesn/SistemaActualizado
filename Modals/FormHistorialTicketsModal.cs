using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
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

            InitializeComponent();
            CargarTickets();
        }

        private void InitializeComponent()
        {
            this.Text = "Historial de Tickets por Vendedor";
            this.Size = new Size(1060, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // 1. ENCABEZADO Y KPIS (Altura ampliada a 160 para evitar cualquier recorte)
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlHeader.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlHeader.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblTitulo = new Label
            {
                Text = "Historial de Tickets",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 10),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Consulta el estado de atención de las ventas emitidas (Modo Solo Lectura)",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 35),
                AutoSize = true
            };

            // MINI KPIS
            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Location = new Point(14, 60),
                Size = new Size(1015, 84),
                ColumnCount = 4,
                RowCount = 1
            };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            tlpKpis.Controls.Add(CrearCardKpi("TOTAL TICKETS", out lblTotalTicketsKpi, "0", Color.FromArgb(15, 23, 42), Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240), Color.FromArgb(100, 116, 139)), 0, 0);
            tlpKpis.Controls.Add(CrearCardKpi("ENVIADOS A CAJA", out lblEnCajaKpi, "0", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199), Color.FromArgb(253, 230, 138), Color.FromArgb(217, 119, 6)), 1, 0);
            tlpKpis.Controls.Add(CrearCardKpi("PAGADOS", out lblPagadosKpi, "0", Color.FromArgb(21, 128, 61), Color.FromArgb(240, 253, 244), Color.FromArgb(187, 247, 208), Color.FromArgb(22, 163, 74)), 2, 0);
            tlpKpis.Controls.Add(CrearCardKpi("ANULADOS", out lblAnuladosKpi, "0", Color.FromArgb(185, 28, 28), Color.FromArgb(254, 242, 242), Color.FromArgb(254, 202, 202), Color.FromArgb(220, 38, 38)), 3, 0);

            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblSub, tlpKpis });

            // 2. BARRA DE FILTROS (Sin botón Cerrar redundante)
            Panel pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10),
                Margin = new Padding(0, 8, 0, 8)
            };
            pnlFiltros.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlFiltros.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblVendedorTag = new Label { Text = "VENDEDOR:", Location = new Point(14, 17), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            cbVendedorModal = new ComboBox { Location = new Point(90, 13), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            foreach (var v in _listaVendedores) cbVendedorModal.Items.Add(v);
            if (cbVendedorModal.Items.Contains(_vendedorSeleccionado)) cbVendedorModal.SelectedItem = _vendedorSeleccionado;
            else if (cbVendedorModal.Items.Count > 0) cbVendedorModal.SelectedIndex = 0;
            cbVendedorModal.SelectedIndexChanged += (s, e) => { _vendedorSeleccionado = cbVendedorModal.SelectedItem?.ToString() ?? ""; CargarTickets(); };

            Label lblFechaTag = new Label { Text = "FECHA:", Location = new Point(255, 17), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            dtpFecha = new DateTimePicker { Location = new Point(305, 14), Size = new Size(115, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 9F) };
            dtpFecha.ValueChanged += (s, e) => CargarTickets();

            Label lblEstadoTag = new Label { Text = "ESTADO:", Location = new Point(435, 17), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            cbEstado = new ComboBox { Location = new Point(495, 13), Size = new Size(135, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbEstado.Items.AddRange(new object[] { "Todos", "Enviado a caja", "Pagado", "Anulado" });
            cbEstado.SelectedIndex = 0;
            cbEstado.SelectedIndexChanged += (s, e) => CargarTickets();

            txtBuscar = new TextBox { Location = new Point(645, 14), Size = new Size(240, 25), Font = new Font("Segoe UI", 9F), PlaceholderText = "🔍 Buscar ticket o cliente..." };
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CargarTickets(); e.SuppressKeyPress = true; } };

            Button btnBuscar = new Button
            {
                Text = "🔍 Filtrar",
                Location = new Point(895, 12),
                Size = new Size(90, 29),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => CargarTickets();

            pnlFiltros.Controls.AddRange(new Control[] { lblVendedorTag, cbVendedorModal, lblFechaTag, dtpFecha, lblEstadoTag, cbEstado, txtBuscar, btnBuscar });

            // 3. GRILLA READ-ONLY
            Panel pnlGridCard = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 12), BackColor = Color.Transparent };

            dgvTickets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 36 }
            };

            // FIJAR ESTILO DE CABECERA (Evita que el header se pinte de azul al seleccionar filas)
            dgvTickets.EnableHeadersVisualStyles = false;
            dgvTickets.ColumnHeadersHeight = 36;
            dgvTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvTickets.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTickets.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvTickets.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvTickets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvTickets.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvTickets.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

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

            // Columna de Estado con Renderizado de Badges de Color
            DataGridViewTextBoxColumn colEstado = new DataGridViewTextBoxColumn
            {
                Name = "Estado",
                HeaderText = "ESTADO",
                FillWeight = 18,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgvTickets.Columns.Add(colEstado);

            // DIBUJADO DE BADGES CON COLOR REAL
            dgvTickets.CellPainting += DgvTickets_CellPainting;

            // Doble clic para inspeccionar el ticket
            dgvTickets.CellDoubleClick += (s, e) =>
            {
                if (dgvTickets.CurrentRow != null && int.TryParse(dgvTickets.CurrentRow.Cells["IdTve"].Value?.ToString(), out int idTve))
                {
                    using var db = new AppDbContext();
                    var venta = db.TVE2607.Find(idTve);
                    if (venta != null)
                    {
                        var modal = new FormDetalleVentaModal(venta);
                        modal.ShowDialog(this);
                    }
                }
            };

            lblFooter = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            pnlGridCard.Controls.Add(dgvTickets);
            pnlGridCard.Controls.Add(lblFooter);

            this.Controls.Add(pnlGridCard);
            this.Controls.Add(pnlFiltros);
            this.Controls.Add(pnlHeader);
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

                // Limpiar texto para no repetir caracteres emoji
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

                // Punto circular de color
                int dotSize = 8;
                int dotX = badgeX + 10;
                int dotY = badgeY + (badgeH - dotSize) / 2;
                using (SolidBrush dotBrush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }

                // Texto con fuente tipográfica nítida
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

        private Panel CrearCardKpi(string titulo, out Label lblVal, string valIni, Color colTxt, Color colBg, Color colBorder, Color colPunto)
        {
            Panel card = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = colBg, 
                Margin = new Padding(4, 0, 4, 0), 
                Padding = new Padding(12, 10, 12, 8) 
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, colBorder, ButtonBorderStyle.Solid);

            Label lblT = new Label
            {
                Text = $"●  {titulo}",
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = colPunto
            };

            lblVal = new Label
            {
                Text = valIni,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = colTxt,
                TextAlign = ContentAlignment.MiddleLeft
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
    }
}