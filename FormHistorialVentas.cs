using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormHistorialVentas : Form
    {
        private AppDbContext _db = new AppDbContext();

        private TextBox txtBuscarFolio = null!;
        private DateTimePicker dtpDesde = null!;
        private DateTimePicker dtpHasta = null!;
        private Button btnBuscar = null!;
        private Button btnHoy = null!;

        private DataGridView dgvVentas = null!;
        private DataGridView dgvDetalle = null!;
        private Button btnReimprimir = null!;
        private Button btnAnular = null!;

        private Venta? _ventaSeleccionada = null;

        public FormHistorialVentas()
        {
            InitializeComponent();
            CargarVentas(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            // 1. BARRA SUPERIOR DE FILTROS Y BÚSQUEDA
            Panel pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.White,
                Padding = new Padding(15, 12, 15, 12)
            };

            Label lblFolio = new Label
            {
                Text = "🔍 Folio / Ticket:",
                Location = new Point(15, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            txtBuscarFolio = new TextBox
            {
                Location = new Point(125, 18),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBuscarFolio.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BuscarPorFolio(); e.SuppressKeyPress = true; } };

            Label lblDesde = new Label
            {
                Text = "📅 Desde:",
                Location = new Point(265, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            dtpDesde = new DateTimePicker
            {
                Location = new Point(330, 18),
                Size = new Size(120, 32),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F)
            };

            Label lblHasta = new Label
            {
                Text = "📅 Hasta:",
                Location = new Point(465, 23),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            dtpHasta = new DateTimePicker
            {
                Location = new Point(530, 18),
                Size = new Size(120, 32),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F)
            };

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(665, 17),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtBuscarFolio.Text))
                {
                    BuscarPorFolio();
                }
                else
                {
                    DateTime inicio = dtpDesde.Value.Date;
                    DateTime fin = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);
                    CargarVentas(inicio, fin);
                }
            };

            btnHoy = new Button
            {
                Text = "📆 Ver Hoy",
                Location = new Point(770, 17),
                Size = new Size(100, 34),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnHoy.FlatAppearance.BorderSize = 0;
            btnHoy.Click += (s, e) =>
            {
                txtBuscarFolio.Clear();
                dtpDesde.Value = DateTime.Today;
                dtpHasta.Value = DateTime.Today;
                CargarVentas(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1));
            };

            pnlFiltros.Controls.Add(lblFolio);
            pnlFiltros.Controls.Add(txtBuscarFolio);
            pnlFiltros.Controls.Add(lblDesde);
            pnlFiltros.Controls.Add(dtpDesde);
            pnlFiltros.Controls.Add(lblHasta);
            pnlFiltros.Controls.Add(dtpHasta);
            pnlFiltros.Controls.Add(btnBuscar);
            pnlFiltros.Controls.Add(btnHoy);

            // 2. PANEL CONTENEDOR PRINCIPAL (LISTA DE VENTAS + DETALLE)
            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 600,
                Padding = new Padding(0, 15, 0, 0)
            };

            // PANEL IZQUIERDO: Tabla de Ventas
            Panel pnlVentasCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
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

            pnlVentasCard.Controls.Add(dgvVentas);
            splitMain.Panel1.Controls.Add(pnlVentasCard);

            // PANEL DERECHO: Detalle del Ticket + Acciones (Reimprimir / Anular)
            Panel pnlDetalleCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            Label lblTituloDetalle = new Label
            {
                Text = "🧾 DETALLE DEL TICKET",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 35
            };

            dgvDetalle = new DataGridView
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
            ConfigurarEstiloTabla(dgvDetalle);

            // Botones de Acción para el ticket seleccionado
            Panel pnlAccionesTicket = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                Padding = new Padding(0, 10, 0, 0)
            };

            btnReimprimir = new Button
            {
                Text = "🖨️ Reimprimir Ticket",
                Location = new Point(0, 12),
                Size = new Size(180, 42),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnReimprimir.FlatAppearance.BorderSize = 0;
            btnReimprimir.Click += BtnReimprimir_Click;

            btnAnular = new Button
            {
                Text = "❌ Anular / Devolución",
                Location = new Point(190, 12),
                Size = new Size(180, 42),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnular.FlatAppearance.BorderSize = 0;
            btnAnular.Click += BtnAnular_Click;

            pnlAccionesTicket.Controls.Add(btnReimprimir);
            pnlAccionesTicket.Controls.Add(btnAnular);

            pnlDetalleCard.Controls.Add(dgvDetalle);
            pnlDetalleCard.Controls.Add(pnlAccionesTicket);
            pnlDetalleCard.Controls.Add(lblTituloDetalle);

            splitMain.Panel2.Controls.Add(pnlDetalleCard);

            pnlMain.Controls.Add(splitMain);
            pnlMain.Controls.Add(pnlFiltros);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 38;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void CargarVentas(DateTime desde, DateTime hasta)
        {
            try
            {
                var ventas = _db.Ventas
                    .Where(v => v.Fecha >= desde && v.Fecha <= hasta)
                    .OrderByDescending(v => v.Fecha)
                    .ToList();

                dgvVentas.DataSource = ventas;

                if (dgvVentas.Columns["VentaID"] != null) dgvVentas.Columns["VentaID"].HeaderText = "N° Ticket";
                if (dgvVentas.Columns["Fecha"] != null) { dgvVentas.Columns["Fecha"].HeaderText = "Fecha / Hora"; dgvVentas.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; }
                if (dgvVentas.Columns["TipoDocumento"] != null) dgvVentas.Columns["TipoDocumento"].HeaderText = "Tipo DTE";
                if (dgvVentas.Columns["FolioDTE"] != null) dgvVentas.Columns["FolioDTE"].HeaderText = "Folio DTE";
                if (dgvVentas.Columns["MedioPago"] != null) dgvVentas.Columns["MedioPago"].HeaderText = "Medio Pago";
                if (dgvVentas.Columns["Total"] != null) { dgvVentas.Columns["Total"].HeaderText = "Total"; dgvVentas.Columns["Total"].DefaultCellStyle.Format = "$#,##0"; }
                if (dgvVentas.Columns["EstadoDTE"] != null) dgvVentas.Columns["EstadoDTE"].HeaderText = "Estado DTE";
                if (dgvVentas.Columns["nroREF"] != null) dgvVentas.Columns["nroREF"].HeaderText = "Ref. Folio";

                AplicarIndicadoresEstado();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuscarPorFolio()
        {
            if (int.TryParse(txtBuscarFolio.Text.Trim(), out int nroFolio))
            {
                var venta = _db.Ventas.FirstOrDefault(v => v.VentaID == nroFolio);
                if (venta != null)
                {
                    dgvVentas.DataSource = new List<Venta> { venta };
                }
                else
                {
                    MessageBox.Show($"No se encontró ningún ticket con el N° #{nroFolio:D6}.", "Ticket No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Ingrese un número de folio válido.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DgvVentas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvVentas.CurrentRow != null && dgvVentas.CurrentRow.DataBoundItem is Venta venta)
            {
                _ventaSeleccionada = venta;
                btnReimprimir.Enabled = true;
                btnAnular.Enabled = true;

                // Cargar ítems ficticios/representativos de la venta para vista preliminar
                CargarDetalleVenta(venta);
            }
            else
            {
                _ventaSeleccionada = null;
                btnReimprimir.Enabled = false;
                btnAnular.Enabled = false;
                dgvDetalle.DataSource = null;
            }
        }

        private void CargarDetalleVenta(Venta venta)
        {
            var detalles = _db.VentaDetalles
                .Where(d => d.VentaID == venta.VentaID)
                .Select(d => new
                {
                    Producto = d.NombreProducto,
                    Cantidad = d.Cantidad,
                    Precio = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                })
                .ToList();

            if (detalles.Count > 0)
            {
                dgvDetalle.DataSource = detalles;

                if (dgvDetalle.Columns["Producto"] != null) dgvDetalle.Columns["Producto"].HeaderText = "Producto";
                if (dgvDetalle.Columns["Cantidad"] != null) dgvDetalle.Columns["Cantidad"].HeaderText = "Cant.";
                if (dgvDetalle.Columns["Precio"] != null) { dgvDetalle.Columns["Precio"].HeaderText = "Precio"; dgvDetalle.Columns["Precio"].DefaultCellStyle.Format = "$#,##0"; }
                if (dgvDetalle.Columns["Subtotal"] != null) { dgvDetalle.Columns["Subtotal"].HeaderText = "Subtotal"; dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0"; }
            }
            else
            {
                var resumen = new List<object>
                {
                    new { Item = "Ticket #", Detalle = $"#{venta.VentaID:D6}" },
                    new { Item = "Fecha Emisión", Detalle = venta.Fecha.ToString("dd/MM/yyyy HH:mm:ss") },
                    new { Item = "Medio de Pago", Detalle = venta.MedioPago },
                    new { Item = "Total Pagado", Detalle = $"$ {venta.Total:N0}" }
                };

                dgvDetalle.DataSource = resumen;
            }
        }

        private void BtnReimprimir_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            var detalles = _db.VentaDetalles.Where(d => d.VentaID == _ventaSeleccionada.VentaID).ToList();
            var itemsEjemplo = detalles
                .Select(d => new DetalleCarrito
                {
                    ProductoID = d.ProductoID,
                    Nombre = d.NombreProducto,
                    PrecioUnitario = d.PrecioUnitario,
                    Cantidad = d.Cantidad
                })
                .ToList();

            if (itemsEjemplo.Count == 0)
            {
                itemsEjemplo.Add(new DetalleCarrito { ProductoID = 1, Nombre = "Venta Ticket #" + _ventaSeleccionada.VentaID, PrecioUnitario = _ventaSeleccionada.Total, Cantidad = 1 });
            }

            FormTicket formTicket = new FormTicket(_ventaSeleccionada, itemsEjemplo, _ventaSeleccionada.Total, 0);

            formTicket.ShowDialog();
        }

        private void BtnAnular_Click(object? sender, EventArgs e)
        {
            if (_ventaSeleccionada == null) return;

            MostrarModalNotaCredito(_ventaSeleccionada);
        }

        private void MostrarModalNotaCredito(Venta ventaOrigen)
        {
            Form modal = new Form
            {
                Text = $"Emitir Nota de Crédito - Ticket #{ventaOrigen.VentaID:D6}",
                Size = new Size(420, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblCausa = new Label
            {
                Text = "Causa SII:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            ComboBox cbCausa = new ComboBox
            {
                Location = new Point(20, 42),
                Size = new Size(360, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbCausa.Items.AddRange(new object[]
            {
                "1 - Anula Documento de Referencia",
                "2 - Corregir Texto en Documento",
                "3 - Corregir Montos / Descuento Parcial"
            });
            cbCausa.SelectedIndex = 0;

            Label lblGlosa = new Label
            {
                Text = "Motivo / Glosa:",
                Location = new Point(20, 80),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            TextBox txtGlosa = new TextBox
            {
                Location = new Point(20, 102),
                Size = new Size(360, 50),
                Multiline = true,
                Font = new Font("Segoe UI", 9F)
            };

            CheckBox chkReintegrar = new CheckBox
            {
                Text = "Reintegrar productos al stock",
                Location = new Point(20, 160),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };

            Button btnEmitir = new Button
            {
                Text = "Emitir Nota de Crédito",
                Location = new Point(20, 200),
                Size = new Size(175, 36),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnEmitir.FlatAppearance.BorderSize = 0;

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(205, 200),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            btnEmitir.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtGlosa.Text))
                {
                    MessageBox.Show("Debe ingresar un motivo o glosa para la Nota de Crédito.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string codigoCausa = cbCausa.SelectedItem?.ToString()?.Split(' ')[0] ?? "1";

                try
                {
                    NotaCreditoService.ProcesarNotaCredito(
                        _db,
                        ventaOrigen.VentaID,
                        codigoCausa,
                        txtGlosa.Text.Trim(),
                        chkReintegrar.Checked,
                        ventaOrigen.Usuario);

                    MessageBox.Show("La Nota de Crédito fue emitida correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modal.Close();
                    CargarVentas(dtpDesde.Value.Date, dtpHasta.Value.Date.AddDays(1).AddTicks(-1));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo emitir la Nota de Crédito: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancelar.Click += (s, e) => modal.Close();

            modal.Controls.Add(lblCausa);
            modal.Controls.Add(cbCausa);
            modal.Controls.Add(lblGlosa);
            modal.Controls.Add(txtGlosa);
            modal.Controls.Add(chkReintegrar);
            modal.Controls.Add(btnEmitir);
            modal.Controls.Add(btnCancelar);
            modal.ShowDialog();
        }

        private void AplicarIndicadoresEstado()
        {
            foreach (DataGridViewRow row in dgvVentas.Rows)
            {
                if (row.DataBoundItem is Venta venta)
                {
                    if (string.Equals(venta.EstadoDTE, "Anulado_NC", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    }
                    else if (string.Equals(venta.TipoDocumento, NotaCreditoService.TipoDocumentoNotaCredito, StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 249, 255);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(2, 132, 199);
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
                    }
                }
            }
        }
    }
}