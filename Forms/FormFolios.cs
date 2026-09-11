using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormFolios : Form
    {
        private readonly FolioService _folioService = new FolioService();

        private DataGridView dgvFolios = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnRefrescar = null!;
        private Label lblContadorFooter = null!;
        private Folio? _folioSeleccionado = null;

        public FormFolios()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarFolios();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
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
            // 1. ENCABEZADO Y BOTONES DE ACCIÓN
            // =========================================================================
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 60, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 0, 0, 10) 
            };

            Panel pnlTitulos = new Panel
            {
                Dock = DockStyle.Left,
                Width = 460,
                BackColor = Color.Transparent
            };

            Label lblTitulo = new Label
            {
                Text = "📄 Rangos de Folios Autorizados (SII)",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Label lblSubtitulo = new Label
            {
                Text = "Administración de folios timbrados para documentos tributarios electrónicos",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlTitulos.Controls.Add(lblSubtitulo);
            pnlTitulos.Controls.Add(lblTitulo);

            FlowLayoutPanel pnlBotonesAccion = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };

            btnNuevo = CrearBoton("➕ Cargar Nuevo Rango", Color.FromArgb(16, 185, 129), Color.White, new Size(185, 34), 6);
            btnNuevo.Margin = new Padding(4, 0, 0, 0);
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = CrearBoton("✏️ Editar Rango", Color.FromArgb(2, 132, 199), Color.White, new Size(130, 34), 6);
            btnEditar.Margin = new Padding(4, 0, 0, 0);
            btnEditar.Enabled = false;
            btnEditar.Click += BtnEditar_Click;

            btnRefrescar = CrearBoton("🔄 Recargar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(110, 34), 6);
            btnRefrescar.Margin = new Padding(4, 0, 0, 0);
            btnRefrescar.Click += (s, e) => CargarFolios();

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnRefrescar });
            pnlHeader.Controls.Add(pnlBotonesAccion);
            pnlHeader.Controls.Add(pnlTitulos);

            // =========================================================================
            // 2. TARJETA DE GRILLA PRINCIPAL REDONDEADA
            // =========================================================================
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

            dgvFolios = new DataGridView
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
                AutoGenerateColumns = false,
                RowTemplate = { Height = 34 }
            };

            ConfigurarEstiloYColumnasTabla();
            dgvFolios.SelectionChanged += DgvFolios_SelectionChanged;

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 rangos de folios autorizados",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlGridCard.Controls.Add(dgvFolios);
            pnlGridCard.Controls.Add(pnlFooter);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void ConfigurarEstiloYColumnasTabla()
        {
            dgvFolios.EnableHeadersVisualStyles = false;
            dgvFolios.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvFolios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvFolios.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvFolios.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvFolios.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvFolios.ColumnHeadersHeight = 36;

            dgvFolios.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgvFolios.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgvFolios.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvFolios.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvFolios.RowTemplate.Height = 34;
            dgvFolios.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvFolios.GridColor = Color.FromArgb(226, 232, 240);

            dgvFolios.Columns.Clear();

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "FolioID", 
                HeaderText = "ID", 
                DataPropertyName = "FolioID", 
                FillWeight = 8, 
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TipoDocumento", 
                HeaderText = "TIPO DOCUMENTO (DTE)", 
                DataPropertyName = "TipoDocumento", 
                FillWeight = 24, 
                DefaultCellStyle = 
                { 
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(37, 99, 235) 
                } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "FolioDesde", 
                HeaderText = "FOLIO DESDE", 
                DataPropertyName = "FolioDesde", 
                FillWeight = 14, 
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "FolioHasta", 
                HeaderText = "FOLIO HASTA", 
                DataPropertyName = "FolioHasta", 
                FillWeight = 14, 
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "FolioActual", 
                HeaderText = "SIGUIENTE FOLIO", 
                DataPropertyName = "FolioActual", 
                FillWeight = 16, 
                DefaultCellStyle = 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(16, 185, 129) 
                } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "FoliosUsados", 
                HeaderText = "FOLIOS USADOS", 
                DataPropertyName = "FoliosUsados", 
                FillWeight = 12, 
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "FoliosDisponibles", 
                HeaderText = "FOLIOS DISPONIBLES", 
                DataPropertyName = "FoliosDisponibles", 
                FillWeight = 14, 
                DefaultCellStyle = 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) 
                } 
            });

            dgvFolios.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Activo", 
                HeaderText = "ESTADO", 
                DataPropertyName = "Activo", 
                FillWeight = 10, 
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } 
            });
        }

        private void CargarFolios()
        {
            try
            {
                var lista = _folioService.ObtenerFolios() ?? new System.Collections.Generic.List<Folio>();
                dgvFolios.DataSource = null;
                dgvFolios.DataSource = lista;

                lblContadorFooter.Text = $"Mostrando {lista.Count:N0} rango(s) de folios autorizados";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar folios: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvFolios_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvFolios.CurrentRow != null && dgvFolios.CurrentRow.DataBoundItem is Folio folio)
            {
                _folioSeleccionado = folio;
                btnEditar.Enabled = true;
            }
            else
            {
                _folioSeleccionado = null;
                btnEditar.Enabled = false;
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e) => MostrarModalFolio(null);
        private void BtnEditar_Click(object? sender, EventArgs e) { if (_folioSeleccionado != null) MostrarModalFolio(_folioSeleccionado); }

        private void MostrarModalFolio(Folio? folio)
        {
            bool esNuevo = (folio == null);
            Folio f = folio ?? new Folio();

            Form modal = new Form
            {
                Text = esNuevo ? "Configurar Rango de Folios" : "Editar Rango de Folios",
                Size = new Size(380, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = esNuevo ? "📄 Nuevo Rango de Folios" : "✏️ Editar Rango", Location = new Point(25, 20), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(15, 23, 42) };

            Label lblTipo = new Label { Text = "Tipo de Documento Tributario:", Location = new Point(25, 60), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbTipo = new ComboBox { Location = new Point(25, 82), Size = new Size(310, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbTipo.Items.AddRange(new string[] { "Boleta Electrónica", "Factura Electrónica", "Guía de Despacho", "Nota de Crédito Electrónica" });
            cbTipo.SelectedItem = string.IsNullOrEmpty(f.TipoDocumento) ? "Boleta Electrónica" : f.TipoDocumento;

            Label lblDesde = new Label { Text = "Folio Desde:", Location = new Point(25, 122), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numDesde = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = f.FolioDesde == 0 ? 1 : f.FolioDesde, Location = new Point(25, 144), Size = new Size(145, 30), Font = new Font("Segoe UI", 9.5F) };

            Label lblHasta = new Label { Text = "Folio Hasta:", Location = new Point(190, 122), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numHasta = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = f.FolioHasta == 0 ? 1000 : f.FolioHasta, Location = new Point(190, 144), Size = new Size(145, 30), Font = new Font("Segoe UI", 9.5F) };

            Label lblActual = new Label { Text = "Siguiente Folio a Emitir:", Location = new Point(25, 184), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numActual = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = f.FolioActual == 0 ? 1 : f.FolioActual, Location = new Point(25, 206), Size = new Size(310, 30), Font = new Font("Segoe UI", 9.5F) };

            Button btnGuardar = CrearBoton(esNuevo ? "💾 Guardar Rango" : "💾 Guardar Cambios", Color.FromArgb(0, 102, 255), Color.White, new Size(310, 42), 8);
            btnGuardar.Location = new Point(25, 255);

            btnGuardar.Click += (s, e) =>
            {
                if (numDesde.Value > numHasta.Value)
                {
                    MessageBox.Show("El 'Folio Desde' no puede ser mayor que el 'Folio Hasta'.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (numActual.Value < numDesde.Value || numActual.Value > numHasta.Value)
                {
                    MessageBox.Show("El 'Siguiente Folio' debe estar dentro del rango comprendido entre Desde y Hasta.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                f.TipoDocumento = cbTipo.SelectedItem?.ToString() ?? "Boleta Electrónica";
                f.FolioDesde = (int)numDesde.Value;
                f.FolioHasta = (int)numHasta.Value;
                f.FolioActual = (int)numActual.Value;

                try
                {
                    _folioService.GuardarFolio(f, esNuevo);

                    MessageBox.Show("Rango de folios guardado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    modal.Close();
                    CargarFolios();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar folios: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            modal.Controls.AddRange(new Control[] { lblTitle, lblTipo, cbTipo, lblDesde, numDesde, lblHasta, numHasta, lblActual, numActual, btnGuardar });
            modal.ShowDialog();
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

        private Button CrearBoton(string texto, Color back, Color fore, Size size, int radio = 0)
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