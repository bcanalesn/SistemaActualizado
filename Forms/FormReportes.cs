using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormReportes : Form
    {
        private readonly ReporteService _reporteService = new ReporteService();

        private DateTimePicker dtpDesde = null!;
        private DateTimePicker dtpHasta = null!;
        private Button btnFiltrar = null!;
        private Button btnHoy = null!;

        private Label lblMontoTotal = null!;
        private Label lblCantVentas = null!;
        private Label lblTicketPromedio = null!;
        private DataGridView dgvVentas = null!;

        public FormReportes()
        {
            InitializeComponent();
            CargarMetricasYVentas(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            Panel pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(15, 12, 15, 12)
            };

            Label lblDesde = new Label
            {
                Text = "📅 Desde:",
                Location = new Point(15, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            dtpDesde = new DateTimePicker
            {
                Location = new Point(85, 18),
                Size = new Size(130, 32),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F)
            };

            Label lblHasta = new Label
            {
                Text = "📅 Hasta:",
                Location = new Point(230, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            dtpHasta = new DateTimePicker
            {
                Location = new Point(300, 18),
                Size = new Size(130, 32),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F)
            };

            btnFiltrar = new Button
            {
                Text = "🔍 Filtrar",
                Location = new Point(445, 17),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFiltrar.FlatAppearance.BorderSize = 0;
            btnFiltrar.Click += (s, e) =>
            {
                DateTime inicio = dtpDesde.Value.Date;
                DateTime fin = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);
                CargarMetricasYVentas(inicio, fin);
            };

            btnHoy = new Button
            {
                Text = "📆 Ver Ventas de Hoy",
                Location = new Point(550, 17),
                Size = new Size(150, 34),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnHoy.FlatAppearance.BorderSize = 0;
            btnHoy.Click += (s, e) =>
            {
                dtpDesde.Value = DateTime.Today;
                dtpHasta.Value = DateTime.Today;
                CargarMetricasYVentas(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
            };

            pnlFiltros.Controls.Add(lblDesde);
            pnlFiltros.Controls.Add(dtpDesde);
            pnlFiltros.Controls.Add(lblHasta);
            pnlFiltros.Controls.Add(dtpHasta);
            pnlFiltros.Controls.Add(btnFiltrar);
            pnlFiltros.Controls.Add(btnHoy);

            TableLayoutPanel pnlMetricas = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 15, 0, 15)
            };
            pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            Panel card1 = CrearTarjetaMetrica("💰 Total Recaudado", "$ 0", Color.FromArgb(16, 185, 129), out lblMontoTotal);
            Panel card2 = CrearTarjetaMetrica("🧾 Transacciones", "0", Color.FromArgb(2, 132, 199), out lblCantVentas);
            Panel card3 = CrearTarjetaMetrica("📊 Ticket Promedio", "$ 0", Color.FromArgb(139, 92, 246), out lblTicketPromedio);

            pnlMetricas.Controls.Add(card1, 0, 0);
            pnlMetricas.Controls.Add(card2, 1, 0);
            pnlMetricas.Controls.Add(card3, 2, 0);

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

            ConfigurarEstiloTabla();

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            pnlGridCard.Controls.Add(dgvVentas);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlMetricas);
            pnlMain.Controls.Add(pnlFiltros);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearTarjetaMetrica(string titulo, string valorInicial, Color colorAcento, out Label lblValor)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5, 0, 5, 0),
                Padding = new Padding(15, 10, 15, 10)
            };

            Label lblTitulo = new Label
            {
                Text = titulo.ToUpper(),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 22
            };

            lblValor = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = colorAcento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblValor);
            card.Controls.Add(lblTitulo);

            return card;
        }

        private void ConfigurarEstiloTabla()
        {
            dgvVentas.EnableHeadersVisualStyles = false;
            dgvVentas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvVentas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvVentas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvVentas.ColumnHeadersHeight = 40;

            dgvVentas.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgvVentas.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgvVentas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvVentas.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvVentas.RowTemplate.Height = 36;
            dgvVentas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvVentas.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarMetricasYVentas(DateTime desde, DateTime hasta)
        {
            try
            {
                var resumen = _reporteService.ObtenerResumenVentas(desde, hasta);

                lblMontoTotal.Text = MonedaHelper.Formatear(resumen.TotalRecaudado, conSigno: true);
                lblCantVentas.Text = $"{resumen.CantidadVentas} {(resumen.CantidadVentas == 1 ? "venta" : "ventas")}";
                lblTicketPromedio.Text = MonedaHelper.Formatear(resumen.TicketPromedio, conSigno: true);

                dgvVentas.DataSource = resumen.Ventas;

                if (dgvVentas.Columns["IdTve"] != null) dgvVentas.Columns["IdTve"].Visible = false;
                if (dgvVentas.Columns["NroDTE"] != null) dgvVentas.Columns["NroDTE"].HeaderText = "N° Folio DTE";
                if (dgvVentas.Columns["FecDoc"] != null) { dgvVentas.Columns["FecDoc"].HeaderText = "Fecha y Hora"; dgvVentas.Columns["FecDoc"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; }
                if (dgvVentas.Columns["Documento"] != null) dgvVentas.Columns["Documento"].HeaderText = "Tipo Documento";
                if (dgvVentas.Columns["RazonSocial"] != null) dgvVentas.Columns["RazonSocial"].HeaderText = "Cliente";
                if (dgvVentas.Columns["RuT"] != null) dgvVentas.Columns["RuT"].HeaderText = "RUT";
                if (dgvVentas.Columns["MedioPago"] != null) dgvVentas.Columns["MedioPago"].HeaderText = "Medio Pago";
                if (dgvVentas.Columns["Vendedor"] != null) dgvVentas.Columns["Vendedor"].HeaderText = "Vendedor";
                if (dgvVentas.Columns["Total"] != null)
                {
                    dgvVentas.Columns["Total"].HeaderText = "Total Venta";
                    dgvVentas.Columns["Total"].DefaultCellStyle = new DataGridViewCellStyle
                    {
                        FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                        Format = "$ #,##0",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(16, 185, 129)
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}