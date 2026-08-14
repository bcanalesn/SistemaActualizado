using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormDetalleVentaModal : Form
    {
        private readonly TVE2607 _venta;
        private List<TVD2607> _detalles = new List<TVD2607>();

        public FormDetalleVentaModal(TVE2607 venta)
        {
            _venta = venta;
            CargarDetallesDesdeBD();
            InitializeComponent();
        }

        private void CargarDetallesDesdeBD()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    _detalles = db.TVD2607.Where(d => d.idTve == _venta.idTve).ToList();
                }
            }
            catch
            {
                _detalles = new List<TVD2607>();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Ventana modal estándar, compacta y movible
            this.Text = $"Detalle de Venta — {_venta.Documento} Folio N° {_venta.nroDTE}";
            this.Size = new Size(840, 530); // Altura compactada
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMainModal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 18),
                BackColor = Color.White
            };

            // 1. FILA DE METADATOS (5 Columnas)
            TableLayoutPanel pnlMetadatos = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent
            };
            pnlMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            pnlMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            pnlMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            pnlMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            pnlMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));

            string clienteTexto = string.IsNullOrEmpty(_venta.RuT) ? "Consumidor Final" : $"{_venta.RazonSocial} ({_venta.RuT})";
            string vendedoraTexto = string.IsNullOrEmpty(_venta.Vendedor) ? (string.IsNullOrEmpty(_venta.UserDTE) ? "Bárbara" : _venta.UserDTE) : _venta.Vendedor;

            pnlMetadatos.Controls.Add(CrearMetaItem("📄 Tipo de Documento", _venta.Documento), 0, 0);
            pnlMetadatos.Controls.Add(CrearMetaItem("🏷️ Folio DTE", _venta.nroDTE.ToString()), 1, 0);
            pnlMetadatos.Controls.Add(CrearMetaItem("📅 Fecha de Emisión", _venta.FecDoc.ToString("dd/MM/yyyy HH:mm")), 2, 0);
            pnlMetadatos.Controls.Add(CrearMetaItem("👤 Vendedora", vendedoraTexto), 3, 0);
            pnlMetadatos.Controls.Add(CrearMetaItem("🏢 Cliente", clienteTexto), 4, 0);

            // 2. BANNER DE ESTADO
            bool isAnulado = _venta.status.Contains("Anulado");
            Panel pnlStatusBanner = CrearTarjetaRedondeada(0, 0, 0, 44,
                isAnulado ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244),
                isAnulado ? Color.FromArgb(254, 202, 202) : Color.FromArgb(187, 247, 208));
            pnlStatusBanner.Dock = DockStyle.Top;
            pnlStatusBanner.Padding = new Padding(12, 6, 12, 6);
            pnlStatusBanner.Margin = new Padding(0, 6, 0, 14);

            Label lblStatusIcon = new Label
            {
                Text = isAnulado ? "❌" : "✔️",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(12, 12),
                AutoSize = true
            };
            Label lblStatusTitle = new Label
            {
                Text = "Estado",
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(38, 5),
                AutoSize = true
            };
            Label lblStatusValue = new Label
            {
                Text = isAnulado ? "Anulado con Nota de Crédito" : "Emitido / Pagado",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = isAnulado ? Color.FromArgb(185, 28, 28) : Color.FromArgb(22, 101, 52),
                Location = new Point(38, 20),
                AutoSize = true
            };
            pnlStatusBanner.Controls.AddRange(new Control[] { lblStatusIcon, lblStatusTitle, lblStatusValue });

            // 3. CUERPO: TABLA (IZQUIERDA) + RESUMEN / IMPRIMIR (DERECHA)
            TableLayoutPanel pnlBodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            pnlBodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            pnlBodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

            // Columna Izquierda: DataGridView (Ocupa todo el alto sin caja de observaciones)
            Panel pnlLeftContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 12, 0) };

            DataGridView dgvItems = new DataGridView
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
            ConfigurarEstiloTabla(dgvItems);

            dgvItems.DataSource = _detalles.Select(d => new
            {
                Producto = d.NmbProducto,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.Precio,
                Descuento = "$ 0",
                Subtotal = d.SubTotal
            }).ToList();

            if (dgvItems.Columns["Producto"] != null) dgvItems.Columns["Producto"].HeaderText = "Producto";
            if (dgvItems.Columns["Cantidad"] != null) { dgvItems.Columns["Cantidad"].HeaderText = "Cantidad"; dgvItems.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvItems.Columns["PrecioUnitario"] != null) { dgvItems.Columns["PrecioUnitario"].HeaderText = "Precio Unitario"; dgvItems.Columns["PrecioUnitario"].DefaultCellStyle.Format = "$#,##0"; }
            if (dgvItems.Columns["Descuento"] != null) { dgvItems.Columns["Descuento"].HeaderText = "Descuento"; dgvItems.Columns["Descuento"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvItems.Columns["Subtotal"] != null) { dgvItems.Columns["Subtotal"].HeaderText = "Subtotal"; dgvItems.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0"; dgvItems.Columns["Subtotal"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }

            pnlLeftContainer.Controls.Add(dgvItems);

            // Columna Derecha: Tarjeta de Resumen + Botón Imprimir inmediatamente abajo
            Panel pnlRightContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 0) };

            Panel pnlResumenCard = CrearTarjetaRedondeada(0, 0, 0, 205, Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240));
            pnlResumenCard.Dock = DockStyle.Top;
            pnlResumenCard.Padding = new Padding(14);

            Label lblResumenTitle = new Label { Text = "Resumen del Documento", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 26 };

            int yOffset = 30;
            CrearFilaResumenModal("Monto Neto", $"$ {_venta.Neto:N0}", ref yOffset, pnlResumenCard);
            CrearFilaResumenModal("IVA (19%)", $"$ {_venta.IvA:N0}", ref yOffset, pnlResumenCard);

            Label lblTotalCap = new Label { Text = "Total Documento", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199), Location = new Point(14, yOffset + 12), AutoSize = true };
            Label lblTotalBig = new Label { Text = $"$ {_venta.Total:N0}", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Location = new Point(12, yOffset + 28), AutoSize = true };
            Label lblDesgloseSmall = new Label { Text = $"(Neto: ${_venta.Neto:N0} | IVA 19%: ${_venta.IvA:N0})", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(14, yOffset + 60), AutoSize = true };

            pnlResumenCard.Controls.AddRange(new Control[] { lblResumenTitle, lblTotalCap, lblTotalBig, lblDesgloseSmall });

            // Botón Imprimir DTE posicionado directamente debajo de la tarjeta de resumen
            Button btnImprimirModal = CrearBotonRedondeado("🖨️  Imprimir DTE", Color.FromArgb(0, 102, 255), Color.White, new Size(200, 42), 8);
            btnImprimirModal.Dock = DockStyle.Top;
            btnImprimirModal.Margin = new Padding(0, 12, 0, 0);
            btnImprimirModal.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            // Espaciador entre tarjeta y botón
            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Color.Transparent };

            btnImprimirModal.Click += (s, e) =>
            {
                var itemsEjemplo = _detalles.Select(d => new DetalleCarrito
                {
                    ProductoID = d.IdProducto,
                    Nombre = d.NmbProducto,
                    PrecioUnitario = d.Precio,
                    Cantidad = d.Cantidad
                }).ToList();

                // Reconstruye pagoCon sumando el Total + Vuelto guardado en la BD
                decimal pagoConHistorico = _venta.Total + _venta.Vuelto;

                // Llamada directa con el objeto TVE2607 completo
                FormTicketModal formTicket = new FormTicketModal(_venta, itemsEjemplo, pagoConHistorico, _venta.Vuelto);
                formTicket.ShowDialog(this);
            };

            pnlRightContainer.Controls.Add(btnImprimirModal);
            pnlRightContainer.Controls.Add(pnlSpacer);
            pnlRightContainer.Controls.Add(pnlResumenCard);

            pnlBodyLayout.Controls.Add(pnlLeftContainer, 0, 0);
            pnlBodyLayout.Controls.Add(pnlRightContainer, 1, 0);

            // Ensamble
            pnlMainModal.Controls.Add(pnlBodyLayout);
            pnlMainModal.Controls.Add(pnlStatusBanner);
            pnlMainModal.Controls.Add(pnlMetadatos);

            this.Controls.Add(pnlMainModal);
            this.ResumeLayout(false);
        }

        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 8);
                using Pen pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
        }

        private Button CrearBotonRedondeado(string texto, Color back, Color fore, Size size, int radio)
        {
            Button b = new Button { Text = texto, Size = size, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, radio);
                b.Region = new Region(p);
            };
            return b;
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

        private Panel CrearMetaItem(string titulo, string valor)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 4, 0), BackColor = Color.Transparent };
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 7.8F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, 2), AutoSize = true };
            Label lblV = new Label { Text = valor, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, 18), AutoSize = true };
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            return pnl;
        }

        private void CrearFilaResumenModal(string titulo, string valor, ref int y, Panel container)
        {
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(14, y), AutoSize = true };
            Label lblV = new Label { Text = valor, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(container.Width - 85, y), AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            container.Controls.Add(lblT);
            container.Controls.Add(lblV);
            y += 24;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersHeight = 34;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 32;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }
    }
}