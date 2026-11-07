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
    public class FormDetalleVentaModal : Form
    {
        private readonly VentaService _ventaService = new VentaService();
        private readonly TVE2607 _venta;
        private List<TVD2607> _detalles = new List<TVD2607>();

        public FormDetalleVentaModal(TVE2607 venta)
        {
            _venta = venta;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            CargarDetallesDesdeServicio();
            InitializeComponent();
        }

        private void CargarDetallesDesdeServicio()
        {
            try
            {
                _detalles = _ventaService.ObtenerDetallesVenta(_venta.idTve);
            }
            catch
            {
                _detalles = new List<TVD2607>();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = $"Detalle de Venta — {_venta.Documento} Folio N° {_venta.nroDTE}";
            this.Size = new Size(880, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMainModal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // =========================================================================
            // 1. METADATOS SUPERIORES EN TARJETA REDONDEADA
            // =========================================================================
            Panel pnlMetaCard = CrearTarjetaRedondeada(0, 0, 0, 62, Color.White, Color.FromArgb(226, 232, 240));
            pnlMetaCard.Dock = DockStyle.Top;
            pnlMetaCard.Padding = new Padding(14, 8, 14, 8);

            TableLayoutPanel tlpMetadatos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            tlpMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            tlpMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            tlpMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            tlpMetadatos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));

            string clienteTexto = string.IsNullOrEmpty(_venta.RuT) ? "Consumidor Final" : $"{_venta.RazonSocial} ({_venta.RuT})";
            string vendedoraTexto = string.IsNullOrEmpty(_venta.Vendedor) ? (string.IsNullOrEmpty(_venta.UserDTE) ? "Bárbara" : _venta.UserDTE) : _venta.Vendedor;

            tlpMetadatos.Controls.Add(CrearMetaItem("📄 TIPO DE DOCUMENTO", _venta.Documento), 0, 0);
            tlpMetadatos.Controls.Add(CrearMetaItem("🏷️ FOLIO DTE", _venta.nroDTE.ToString()), 1, 0);
            tlpMetadatos.Controls.Add(CrearMetaItem("📅 FECHA EMISIÓN", _venta.FecDoc.ToString("dd/MM/yyyy HH:mm")), 2, 0);
            tlpMetadatos.Controls.Add(CrearMetaItem("👤 VENDEDORA", vendedoraTexto), 3, 0);
            tlpMetadatos.Controls.Add(CrearMetaItem("🏢 CLIENTE", clienteTexto), 4, 0);

            pnlMetaCard.Controls.Add(tlpMetadatos);

            Panel pnlSpacer1 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // =========================================================================
            // 2. BANNER DE ESTADO REDONDEADO
            // =========================================================================
            string status = _venta.status ?? "";
            bool isAnuladoNC = status.Equals("Anulado NC", StringComparison.OrdinalIgnoreCase);
            bool isAnuladoGeneral = status.Equals("Anulado", StringComparison.OrdinalIgnoreCase);
            bool isNC = _venta.iddocDTE == 61 || _venta.Documento.Contains("Crédito") || status.Equals("Emitido NC", StringComparison.OrdinalIgnoreCase);
            bool isND = _venta.iddocDTE == 56 || _venta.Documento.Contains("Débito") || status.Equals("Emitido ND", StringComparison.OrdinalIgnoreCase);
            bool isPendiente = status.Equals("Pendiente", StringComparison.OrdinalIgnoreCase) || 
                               status.Contains("Espera", StringComparison.OrdinalIgnoreCase);

            Color fondoBanner;
            Color bordeBanner;
            Color textoBanner;
            string iconoBanner;
            string textoEstado;

            if (isNC)
            {
                fondoBanner = Color.FromArgb(254, 242, 242);
                bordeBanner = Color.FromArgb(254, 202, 202);
                textoBanner = Color.FromArgb(220, 38, 38);
                iconoBanner = "↩️";
                textoEstado = "Nota de Crédito Emitida (Devolución / Descuento Tributario)";
            }
            else if (isND)
            {
                fondoBanner = Color.FromArgb(245, 243, 255);
                bordeBanner = Color.FromArgb(221, 214, 254);
                textoBanner = Color.FromArgb(124, 58, 237);
                iconoBanner = "➕";
                textoEstado = "Nota de Débito Emitida (Recargo / Ajuste de Monto)";
            }
            else if (isAnuladoNC)
            {
                fondoBanner = Color.FromArgb(254, 242, 242);
                bordeBanner = Color.FromArgb(254, 202, 202);
                textoBanner = Color.FromArgb(185, 28, 28);
                iconoBanner = "❌";
                textoEstado = "Venta Anulada mediante Nota de Crédito";
            }
            else if (isAnuladoGeneral)
            {
                fondoBanner = Color.FromArgb(254, 242, 242);
                bordeBanner = Color.FromArgb(254, 202, 202);
                textoBanner = Color.FromArgb(185, 28, 28);
                iconoBanner = "❌";
                textoEstado = "Documento Anulado";
            }
            else if (isPendiente)
            {
                fondoBanner = Color.FromArgb(254, 249, 195);
                bordeBanner = Color.FromArgb(253, 230, 138);
                textoBanner = Color.FromArgb(180, 83, 9);
                iconoBanner = "⏳";
                textoEstado = "Enviado a Caja (Pendiente de Pago)";
            }
            else
            {
                fondoBanner = Color.FromArgb(240, 253, 244);
                bordeBanner = Color.FromArgb(187, 247, 208);
                textoBanner = Color.FromArgb(22, 101, 52);
                iconoBanner = "✔️";
                textoEstado = "Emitido / Pagado Exitosamente";
            }

            Panel pnlStatusBanner = CrearTarjetaRedondeada(0, 0, 0, 42, fondoBanner, bordeBanner);
            pnlStatusBanner.Dock = DockStyle.Top;
            pnlStatusBanner.Padding = new Padding(12, 6, 12, 6);

            Label lblStatusIcon = new Label
            {
                Text = iconoBanner,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 11),
                AutoSize = true
            };

            Label lblStatusTitle = new Label
            {
                Text = "ESTADO DEL DOCUMENTO:",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(40, 6),
                AutoSize = true
            };

            Label lblStatusValue = new Label
            {
                Text = textoEstado,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = textoBanner,
                Location = new Point(40, 21),
                AutoSize = true
            };

            pnlStatusBanner.Controls.AddRange(new Control[] { lblStatusIcon, lblStatusTitle, lblStatusValue });

            // =========================================================================
            // 2.1 REFERENCIAS TRIBUTARIAS EN PANTALLA (SI ES NOTA DE CRÉDITO O DÉBITO)
            // =========================================================================
            Panel? pnlReferenciasCard = null;
            if (_venta.iddocDTE == 61 || _venta.iddocDTE == 56 || isNC || isND)
            {
                using var dbRef = new AppDbContext();
                var listaRefs = dbRef.DteReferencias
                    .Where(r => r.IdTveDTEModificador == _venta.idTve)
                    .ToList();

                if (listaRefs.Count > 0)
                {
                    int alto = Math.Min(95, 30 + (listaRefs.Count * 22));
                    pnlReferenciasCard = CrearTarjetaRedondeada(0, 0, 0, alto, Color.FromArgb(248, 250, 252), Color.FromArgb(203, 213, 225));
                    pnlReferenciasCard.Dock = DockStyle.Top;
                    pnlReferenciasCard.Padding = new Padding(12, 6, 12, 6);

                    Label lblTitRef = new Label
                    {
                        Text = "🔗 DOCUMENTO(S) DE REFERENCIA TRIBUTARIA (SII):",
                        Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(71, 85, 105),
                        Dock = DockStyle.Top,
                        Height = 18
                    };

                    string detalleReferencias = string.Join("\n", listaRefs.Select(r =>
                    {
                        string nom = !string.IsNullOrEmpty(r.NombreDocOrigen) ? r.NombreDocOrigen : (r.TipoDocOrigen == 33 ? "Factura" : "Boleta");
                        return $"• {nom} Folio N° {r.FolioOrigen}  —  Cód. {r.CodigoReferencia}: {r.RazonReferencia}  ({MonedaHelper.Formatear(r.MontoAfectado, conSigno: true)})";
                    }));

                    Label lblTextoRefs = new Label
                    {
                        Text = detalleReferencias,
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(15, 23, 42),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    pnlReferenciasCard.Controls.Add(lblTextoRefs);
                    pnlReferenciasCard.Controls.Add(lblTitRef);
                }
            }

            Panel pnlSpacer2 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // =========================================================================
            // 3. CUERPO: GRILLA A LA IZQUIERDA Y TOTALES A LA DERECHA
            // =========================================================================
            TableLayoutPanel pnlBodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            pnlBodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67F));
            pnlBodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            // Contenedor izquierdo (Grilla)
            Panel pnlLeftWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0), BackColor = Color.Transparent };
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }
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

            var estiloMoneda = new DataGridViewCellStyle
            {
                FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                Format = "$ #,##0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            };

            if (dgvItems.Columns["Producto"] != null) { dgvItems.Columns["Producto"].HeaderText = "PRODUCTO"; dgvItems.Columns["Producto"].FillWeight = 40; }
            if (dgvItems.Columns["Cantidad"] != null) { dgvItems.Columns["Cantidad"].HeaderText = "CANT."; dgvItems.Columns["Cantidad"].FillWeight = 14; dgvItems.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvItems.Columns["PrecioUnitario"] != null) { dgvItems.Columns["PrecioUnitario"].HeaderText = "P. UNITARIO"; dgvItems.Columns["PrecioUnitario"].FillWeight = 20; dgvItems.Columns["PrecioUnitario"].DefaultCellStyle = estiloMoneda; }
            if (dgvItems.Columns["Descuento"] != null) { dgvItems.Columns["Descuento"].HeaderText = "DESC."; dgvItems.Columns["Descuento"].FillWeight = 12; dgvItems.Columns["Descuento"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvItems.Columns["Subtotal"] != null) 
            { 
                dgvItems.Columns["Subtotal"].HeaderText = "SUBTOTAL"; 
                dgvItems.Columns["Subtotal"].FillWeight = 22;
                dgvItems.Columns["Subtotal"].DefaultCellStyle = new DataGridViewCellStyle
                {
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                    Format = "$ #,##0",
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
                };
            }

            pnlGridCard.Controls.Add(dgvItems);
            pnlLeftWrapper.Controls.Add(pnlGridCard);

            // Contenedor derecho (Totales y Botón Impresión)
            Panel pnlRightWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 0), BackColor = Color.Transparent };

            Panel pnlResumenCard = CrearTarjetaRedondeada(0, 0, 0, 210, Color.White, Color.FromArgb(226, 232, 240));
            pnlResumenCard.Dock = DockStyle.Top;
            pnlResumenCard.Padding = new Padding(14);

            Label lblResumenTitle = new Label 
            { 
                Text = "Resumen del Documento", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(15, 23, 42), 
                Dock = DockStyle.Top, 
                Height = 26 
            };

            Panel pnlNetoRow = CrearFilaResumen("Monto Neto:", MonedaHelper.Formatear(_venta.Neto, conSigno: true));
            pnlNetoRow.Dock = DockStyle.Top;

            Panel pnlIvaRow = CrearFilaResumen("IVA (19%):", MonedaHelper.Formatear(_venta.IvA, conSigno: true));
            pnlIvaRow.Dock = DockStyle.Top;

            Panel pnlBoxTotal = CrearTarjetaRedondeada(0, 0, 0, 72, Color.FromArgb(240, 253, 244), Color.FromArgb(187, 247, 208));
            pnlBoxTotal.Dock = DockStyle.Top;
            pnlBoxTotal.Padding = new Padding(10, 8, 10, 8);

            Label lblTotalCap = new Label 
            { 
                Text = "TOTAL DOCUMENTO", 
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(22, 163, 74), 
                Dock = DockStyle.Top, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            Label lblTotalBig = new Label 
            { 
                Text = MonedaHelper.Formatear(_venta.Total, conSigno: true), 
                Font = new Font("Segoe UI", 16F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(22, 163, 74), 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            pnlBoxTotal.Controls.AddRange(new Control[] { lblTotalBig, lblTotalCap });

            pnlResumenCard.Controls.Add(pnlBoxTotal);
            pnlResumenCard.Controls.Add(pnlIvaRow);
            pnlResumenCard.Controls.Add(pnlNetoRow);
            pnlResumenCard.Controls.Add(lblResumenTitle);

            string textoBotonImprimir = (_venta.iddocDTE > 0)
                ? "🖨️  Imprimir DTE"
                : "🖨️  Imprimir Ticket de Atención";

            Button btnImprimirModal = CrearBoton(textoBotonImprimir, Color.FromArgb(0, 102, 255), Color.White, new Size(200, 42));
            btnImprimirModal.Dock = DockStyle.Bottom;
            btnImprimirModal.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            btnImprimirModal.Click += (s, e) =>
            {
                var itemsEjemplo = _detalles.Select(d => new DetalleCarrito
                {
                    ProductoID = d.IdProducto,
                    Nombre = d.NmbProducto,
                    PrecioUnitario = d.Precio,
                    Cantidad = d.Cantidad
                }).ToList();

                decimal pagoConHistorico = _venta.Total + _venta.Vuelto;

                FormTicketModal formTicket = new FormTicketModal(_venta, itemsEjemplo, pagoConHistorico, _venta.Vuelto);
                formTicket.ShowDialog(this);
            };

            pnlRightWrapper.Controls.Add(btnImprimirModal);
            pnlRightWrapper.Controls.Add(pnlResumenCard);

            pnlBodyLayout.Controls.Add(pnlLeftWrapper, 0, 0);
            pnlBodyLayout.Controls.Add(pnlRightWrapper, 1, 0);

            // =========================================================================
            // APILAMIENTO ORDENADO (De abajo hacia arriba según DockStyle.Top / Fill)
            // =========================================================================
            pnlMainModal.Controls.Add(pnlBodyLayout);
            pnlMainModal.Controls.Add(pnlSpacer2);
            if (pnlReferenciasCard != null)
            {
                pnlMainModal.Controls.Add(pnlReferenciasCard);
                pnlMainModal.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent });
            }
            pnlMainModal.Controls.Add(pnlStatusBanner);
            pnlMainModal.Controls.Add(pnlSpacer1);
            pnlMainModal.Controls.Add(pnlMetaCard);

            this.Controls.Add(pnlMainModal);
            this.ResumeLayout(false);
        }

        private Panel CrearMetaItem(string titulo, string valor)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 4, 0), BackColor = Color.Transparent };
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 7.2F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, 2), AutoSize = true };
            Label lblV = new Label { Text = valor, Font = new Font("Segoe UI", 8.8F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, 20), AutoSize = true };
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            return pnl;
        }

        private Panel CrearFilaResumen(string titulo, string valor)
        {
            Panel pnl = new Panel { Height = 28, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 4) };
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Left, AutoSize = true };
            Label lblV = new Label { Text = valor, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Right, AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            pnl.Controls.AddRange(new Control[] { lblT, lblV });
            return pnl;
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
    }
}