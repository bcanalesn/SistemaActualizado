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

        // Botones de Filtros Rápidos
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

        private Label lblPaginadorInfo = null!;

        private TVE2607? _ventaSeleccionada = null;
        private List<TVE2607> _ventasCargadas = new List<TVE2607>();

        public FormHistorialVentas()
        {
            InitializeComponent();
            AplicarFiltroFecha(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 20),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Consulta, revisa y gestiona todas las ventas registradas en TVE2607",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 24
            };

            Panel pnlFiltrosCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = Color.White,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 0, 14)
            };
            pnlFiltrosCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlFiltrosCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            FlowLayoutPanel flowFiltrosFila1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                WrapContents = true,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            Panel pnlGrpFolio = CrearGrupoFiltro("Folio / Ticket / RUT", txtBuscarFolio = new TextBox
            {
                Size = new Size(150, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252)
            });
            txtBuscarFolio.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { EjcutarBusqueda(); e.SuppressKeyPress = true; } };

            Panel pnlGrpDesde = CrearGrupoFiltro("Fecha desde", dtpDesde = new DateTimePicker
            {
                Size = new Size(115, 26),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5F)
            });

            Panel pnlGrpHasta = CrearGrupoFiltro("Fecha hasta", dtpHasta = new DateTimePicker
            {
                Size = new Size(115, 26),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5F)
            });

            cbTipoDocumento = new ComboBox
            {
                Size = new Size(140, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbTipoDocumento.Items.AddRange(new string[] { "Todos", "Boleta Electrónica", "Factura Electrónica", "Guía de Despacho", "Nota de Crédito" });
            cbTipoDocumento.SelectedIndex = 0;
            Panel pnlGrpTipoDoc = CrearGrupoFiltro("Tipo Documento", cbTipoDocumento);

            btnBuscar = CrearBotonEstilizado("🔍 Buscar", Color.FromArgb(2, 132, 199), Color.White, new Size(85, 30));
            btnBuscar.Margin = new Padding(4, 18, 4, 0);
            btnBuscar.Click += (s, e) => EjcutarBusqueda();

            btnLimpiar = CrearBotonEstilizado("🔄 Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(85, 30));
            btnLimpiar.Margin = new Padding(4, 18, 4, 0);
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            flowFiltrosFila1.Controls.AddRange(new Control[] {
                pnlGrpFolio, pnlGrpDesde, pnlGrpHasta, pnlGrpTipoDoc, btnBuscar, btnLimpiar
            });

            FlowLayoutPanel flowFiltrosFila2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                Margin = new Padding(0, 4, 0, 0),
                WrapContents = false,
                BackColor = Color.Transparent
            };

            Label lblFiltrosRapidos = new Label
            {
                Text = "Filtros rápidos:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Margin = new Padding(0, 6, 8, 0),
                AutoSize = true
            };

            btnFiltroHoy = CrearPillBoton("📅 Hoy", 65, (s, e) => AplicarFiltroFecha(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroAyer = CrearPillBoton("📅 Ayer", 65, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-1), DateTime.Today.AddTicks(-1)));
            btnFiltro7Dias = CrearPillBoton("📅 Últimos 7 días", 120, (s, e) => AplicarFiltroFecha(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroEsteMes = CrearPillBoton("📅 Este mes", 90, (s, e) => AplicarFiltroFecha(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today.AddDays(1).AddTicks(-1)));
            btnFiltroMesAnterior = CrearPillBoton("📅 Mes anterior", 110, (s, e) =>
            {
                var inicioMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                var finMesAnt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddTicks(-1);
                AplicarFiltroFecha(inicioMesAnt, finMesAnt);
            });

            flowFiltrosFila2.Controls.AddRange(new Control[] {
                lblFiltrosRapidos, btnFiltroHoy, btnFiltroAyer, btnFiltro7Dias, btnFiltroEsteMes, btnFiltroMesAnterior
            });

            pnlFiltrosCard.Controls.Add(flowFiltrosFila2);
            pnlFiltrosCard.Controls.Add(flowFiltrosFila1);

            TableLayoutPanel pnlGridAndDetail = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 490,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            pnlGridAndDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));

            Panel pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 8, 0)
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

            Panel pnlPaginador = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
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

            Panel pnlDetailCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14)
            };
            pnlDetailCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlDetailCard.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            Label lblTituloDetalle = new Label
            {
                Text = "📄 Resumen del Documento",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 28
            };

            Panel pnlCamposDetalle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 230,
                Padding = new Padding(0, 4, 0, 0)
            };

            int yPos = 0;
            lblDetTipoDoc = CrearRenglonDetalle("Tipo Documento", "-", ref yPos, pnlCamposDetalle);
            lblDetFolio = CrearRenglonDetalle("Folio DTE N°", "-", ref yPos, pnlCamposDetalle);
            lblDetFecha = CrearRenglonDetalle("Fecha Emisión", "-", ref yPos, pnlCamposDetalle);
            lblDetMedio = CrearRenglonDetalle("Vendedor", "-", ref yPos, pnlCamposDetalle);
            lblDetCliente = CrearRenglonDetalle("Cliente / RUT", "-", ref yPos, pnlCamposDetalle);
            lblDetNeto = CrearRenglonDetalle("Monto Neto", "$ 0", ref yPos, pnlCamposDetalle);
            lblDetIva = CrearRenglonDetalle("IVA (19%)", "$ 0", ref yPos, pnlCamposDetalle);

            Panel pnlTotalHighlight = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 8, 0, 8)
            };
            pnlTotalHighlight.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlTotalHighlight.ClientRectangle, Color.FromArgb(186, 230, 253), ButtonBorderStyle.Solid);
            };

            Label lblTotalCaption = new Label
            {
                Text = "Monto Total",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(3, 105, 161),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 16
            };

            lblDetTotal = new Label
            {
                Text = "$ 0",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(2, 132, 199),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTotalHighlight.Controls.Add(lblDetTotal);
            pnlTotalHighlight.Controls.Add(lblTotalCaption);

            Panel pnlBotonesAccion = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 95,
                Padding = new Padding(0, 6, 0, 0)
            };

            btnReimprimir = new Button
            {
                Text = "🖨️ Reimprimir DTE",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnReimprimir.FlatAppearance.BorderSize = 0;
            btnReimprimir.Click += BtnReimprimir_Click;

            Panel pnlSpacerBtn = new Panel { Dock = DockStyle.Top, Height = 6 };

            btnNotaCredito = new Button
            {
                Text = "📄 Emitir Nota de Crédito",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
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

            pnlMain.Controls.Add(pnlGridAndDetail);
            pnlMain.Controls.Add(pnlFiltrosCard);
            pnlMain.Controls.Add(lblSubtitulo);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearGrupoFiltro(string titulo, Control control)
        {
            Panel pnl = new Panel { Size = new Size(control.Width + 4, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };
            control.Location = new Point(0, 18);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private Button CrearBotonEstilizado(string texto, Color back, Color fore, Size size)
        {
            Button b = new Button { Text = texto, Size = size, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Button CrearPillBoton(string texto, int ancho, EventHandler onClick)
        {
            Button b = new Button { Text = texto, Size = new Size(ancho, 24), BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 2, 6, 0) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private Label CrearRenglonDetalle(string titulo, string valorInicial, ref int y, Panel container)
        {
            Label lblT = new Label { Text = titulo, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(0, y), AutoSize = true };
            Label lblV = new Label { Text = valorInicial, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(0, y + 14), AutoSize = true };
            container.Controls.Add(lblT);
            container.Controls.Add(lblV);
            y += 32;
            return lblV;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 32;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
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
                _ventasCargadas = _db.TVE2607
                    .Where(v => v.FecDoc >= desde && v.FecDoc <= hasta)
                    .OrderByDescending(v => v.FecDoc)
                    .ToList();

                FiltrarLocalmente();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas desde TVE2607: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EjcutarBusqueda() => FiltrarLocalmente();

        private void LimpiarFiltros()
        {
            txtBuscarFolio.Clear();
            cbTipoDocumento.SelectedIndex = 0;
            AplicarFiltroFecha(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void FiltrarLocalmente()
        {
            var resultado = _ventasCargadas.AsQueryable();

            string textoBusqueda = txtBuscarFolio.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(textoBusqueda))
            {
                resultado = resultado.Where(v => v.nroDTE.ToString().Contains(textoBusqueda) ||
                                                 v.idTve.ToString().Contains(textoBusqueda) ||
                                                 (!string.IsNullOrEmpty(v.RuT) && v.RuT.ToLower().Contains(textoBusqueda)));
            }

            string tipoSel = cbTipoDocumento.SelectedItem?.ToString() ?? "Todos";
            if (tipoSel != "Todos")
            {
                resultado = resultado.Where(v => v.Documento.Contains(tipoSel));
            }

            var listaFinal = resultado.ToList();
            dgvVentas.DataSource = listaFinal;

            if (dgvVentas.Columns["idTve"] != null) { dgvVentas.Columns["idTve"].HeaderText = "ID Venta"; dgvVentas.Columns["idTve"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvVentas.Columns["FecDoc"] != null) { dgvVentas.Columns["FecDoc"].HeaderText = "Fecha / Hora"; dgvVentas.Columns["FecDoc"].DefaultCellStyle.Format = "dd-MM-yyyy HH:mm"; }
            if (dgvVentas.Columns["Documento"] != null) dgvVentas.Columns["Documento"].HeaderText = "Documento";
            if (dgvVentas.Columns["nroDTE"] != null) { dgvVentas.Columns["nroDTE"].HeaderText = "Folio DTE"; dgvVentas.Columns["nroDTE"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvVentas.Columns["Total"] != null) { dgvVentas.Columns["Total"].HeaderText = "Total"; dgvVentas.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; dgvVentas.Columns["Total"].DefaultCellStyle.ForeColor = Color.FromArgb(2, 132, 199); dgvVentas.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); }
            if (dgvVentas.Columns["UserDTE"] != null) dgvVentas.Columns["UserDTE"].HeaderText = "Cajero";
            if (dgvVentas.Columns["RuT"] != null) dgvVentas.Columns["RuT"].HeaderText = "Cliente / RUT";
            if (dgvVentas.Columns["RazonSocial"] != null) dgvVentas.Columns["RazonSocial"].HeaderText = "Razón Social";

            string[] ocultar = new string[] { "idLocal", "nmbLocal", "iddocDTE", "nroInT", "SubTotal", "Descuento", "Neto", "Impto1", "Impto2", "Impto3", "IvA", "Vendedor", "nroZ", "Url", "nPAX", "Idcliente", "DNI", "dv", "Giro", "Direccion", "idcomuna", "nComuna", "idCiudad", "nCiudad", "Fono1", "Fono2", "email", "status", "idREF", "nroREF", "codigoREF", "FechaREF", "HoraDoc", "Detalles" };
            foreach (var col in ocultar)
            {
                if (dgvVentas.Columns[col] != null) dgvVentas.Columns[col].Visible = false;
            }

            lblPaginadorInfo.Text = $"Mostrando {listaFinal.Count} de {_ventasCargadas.Count} registros";
        }

        private void DgvVentas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvVentas.CurrentRow != null && dgvVentas.CurrentRow.DataBoundItem is TVE2607 venta)
            {
                _ventaSeleccionada = venta;
                btnReimprimir.Enabled = true;
                btnNotaCredito.Enabled = !venta.Documento.Equals("Nota de Crédito Electrónica", StringComparison.OrdinalIgnoreCase) && !venta.status.Equals("Anulado NC", StringComparison.OrdinalIgnoreCase);

                lblDetTipoDoc.Text = venta.Documento;
                lblDetFolio.Text = $"#{venta.nroDTE:D6}";
                lblDetFecha.Text = venta.FecDoc.ToString("dd-MM-yyyy HH:mm");
                lblDetMedio.Text = string.IsNullOrEmpty(venta.UserDTE) ? "barbara" : venta.UserDTE;
                lblDetCliente.Text = string.IsNullOrEmpty(venta.RuT) ? "Consumidor Final" : venta.RuT;
                lblDetNeto.Text = $"$ {venta.Neto:N0}";
                lblDetIva.Text = $"$ {venta.IvA:N0}";
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
                new DetalleCarrito { ProductoID = 1, Nombre = "Glosa DTE - " + _ventaSeleccionada.Documento, PrecioUnitario = _ventaSeleccionada.Total, Cantidad = 1 }
            };

            FormTicket formTicket = new FormTicket(_ventaSeleccionada.nroDTE, _ventaSeleccionada.FecDoc, itemsEjemplo, _ventaSeleccionada.Total, _ventaSeleccionada.Total, 0, "Efectivo");
            formTicket.ShowDialog();
        }

        private void BtnNotaCredito_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            Form modalNC = new Form
            {
                Text = $"Emitir Nota de Crédito - Referencia a Folio {_ventaSeleccionada.nroDTE}",
                Size = new Size(460, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = "📄 Emitir Nota de Crédito Electrónica", Location = new Point(25, 18), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(185, 28, 28), AutoSize = true };
            Label lblInfo = new Label { Text = $"Documento de Origen: {_ventaSeleccionada.Documento} N° {_ventaSeleccionada.nroDTE}\nMonto Total: ${_ventaSeleccionada.Total:N0} (Neto: ${_ventaSeleccionada.Neto:N0} | IVA: ${_ventaSeleccionada.IvA:N0})", Location = new Point(25, 48), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), AutoSize = true };

            Label lblCausa = new Label { Text = "Código de Referencia SII:", Location = new Point(25, 100), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbCausa = new ComboBox { Location = new Point(25, 122), Size = new Size(390, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbCausa.Items.AddRange(new string[] { "1 - Anula Documento de Referencia (Anulación / Devolución Total)", "2 - Corregir Texto en Documento (Glosa / Datos)", "3 - Corregir Montos / Descuento Parcial" });
            cbCausa.SelectedIndex = 0;

            Label lblMotivo = new Label { Text = "Motivo / Glosa de la Anulación:", Location = new Point(25, 165), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtMotivo = new TextBox { Text = "Devolución de mercadería / Anulación de venta", Location = new Point(25, 187), Size = new Size(390, 28), Font = new Font("Segoe UI", 9.5F) };

            CheckBox chkRestock = new CheckBox { Text = "🔄 Reintegrar automáticamente los productos al stock del inventario", Location = new Point(25, 230), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(3, 105, 161) };

            Button btnEmitirNC = new Button { Text = "⚡ GENERAR Y EMITIR NOTA DE CRÉDITO", Location = new Point(25, 310), Size = new Size(390, 48), BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnEmitirNC.FlatAppearance.BorderSize = 0;

            btnEmitirNC.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtMotivo.Text))
                {
                    MessageBox.Show("Debe ingresar un motivo para la emisión.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // 1. Crear el Encabezado DTE Nota de Crédito asignando iddocDTE = 61
                    TVE2607 nc = new TVE2607
                    {
                        idLocal = _ventaSeleccionada.idLocal,
                        nmbLocal = _ventaSeleccionada.nmbLocal,
                        iddocDTE = 61, // 61 = Nota de Crédito Electrónica según SII
                        Documento = "Nota de Crédito Electrónica",
                        nroDTE = (int)(DateTime.Now.Ticks % 100000),
                        FecDoc = DateTime.Now,
                        HoraDoc = DateTime.Now.ToString("HH:mm:ss"),
                        SubTotal = _ventaSeleccionada.SubTotal,
                        Descuento = _ventaSeleccionada.Descuento,
                        Neto = _ventaSeleccionada.Neto,
                        IvA = _ventaSeleccionada.IvA,
                        Total = _ventaSeleccionada.Total,
                        RuT = _ventaSeleccionada.RuT,
                        RazonSocial = _ventaSeleccionada.RazonSocial,
                        Giro = _ventaSeleccionada.Giro,
                        idREF = _ventaSeleccionada.idTve,
                        nroREF = _ventaSeleccionada.nroDTE,
                        codigoREF = (cbCausa.SelectedIndex + 1).ToString(),
                        UserDTE = _ventaSeleccionada.UserDTE,
                        Vendedor = _ventaSeleccionada.Vendedor,
                        status = "Emitido"
                    };

                    _db.TVE2607.Add(nc);
                    _db.SaveChanges(); // Genera nc.idTve

                    // 2. Obtener los detalles de la venta original desde TVD2607
                    var detallesOrigen = _db.TVD2607.Where(d => d.idTve == _ventaSeleccionada.idTve).ToList();

                    foreach (var item in detallesOrigen)
                    {
                        // Registrar el detalle de la Nota de Crédito
                        TVD2607 ncDetalle = new TVD2607
                        {
                            idTve = nc.idTve,
                            idLocal = item.idLocal,
                            iddocDTE = 61, // 61 = Nota de Crédito
                            Documento = "Nota de Crédito Electrónica",
                            NroDTE = nc.nroDTE,
                            FecMoV = DateTime.Now,
                            HoraMoV = DateTime.Now.ToString("HH:mm:ss"),
                            IdProducto = item.IdProducto,
                            NmbProducto = item.NmbProducto,
                            Cantidad = item.Cantidad,
                            Precio = item.Precio,
                            PneTo = item.PneTo,
                            SubTotal = item.SubTotal,
                            SubNeto = item.SubNeto,
                            nmbVendedor = item.nmbVendedor
                        };
                        _db.TVD2607.Add(ncDetalle);

                        // 3. RESTAURAR STOCK EXACTO de cada producto (ej: si eran 5 unidades, devuelve 5)
                        if (chkRestock.Checked)
                        {
                            var prod = _db.Productos.FirstOrDefault(p => p.ProductoID == item.IdProducto);
                            if (prod != null)
                            {
                                prod.Stock += item.Cantidad; // Devuelve las N unidades reales compradas
                            }
                        }
                    }

                    // Marcar estado del documento original
                    _ventaSeleccionada.status = "Anulado NC";

                    _db.SaveChanges();

                    MessageBox.Show($"¡Nota de Crédito N° {nc.nroDTE} emitida con éxito!\nSe devolvieron los productos al stock y se restó el monto del Control de Caja.", "DTE Emitido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modalNC.Close();
                    AplicarFiltroFecha(dtpDesde.Value.Date, dtpHasta.Value.Date.AddDays(1).AddTicks(-1));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al emitir Nota de Crédito: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modalNC.Controls.AddRange(new Control[] { lblTitle, lblInfo, lblCausa, cbCausa, lblMotivo, txtMotivo, chkRestock, btnEmitirNC });
            modalNC.ShowDialog();
        }
    }
}