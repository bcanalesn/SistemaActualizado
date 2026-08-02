using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormFolios : Form
    {
        private AppDbContext _db = new AppDbContext();

        private DataGridView dgvFolios = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnRefrescar = null!;

        private Folio? _folioSeleccionado = null;

        public FormFolios()
        {
            InitializeComponent();
            CargarFolios();
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

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(15, 12, 15, 12)
            };

            Label lblTitulo = new Label
            {
                Text = "📄 Rangos de Folios Autorizados (SII)",
                Location = new Point(15, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            btnNuevo = new Button
            {
                Text = "➕ Cargar Nuevo Rango",
                Dock = DockStyle.Right,
                Width = 170,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = new Button
            {
                Text = "✏️ Editar Rango",
                Dock = DockStyle.Right,
                Width = 120,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += BtnEditar_Click;

            btnRefrescar = new Button
            {
                Text = "🔄 Recargar",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;
            btnRefrescar.Click += (s, e) => CargarFolios();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(btnRefrescar);
            pnlHeader.Controls.Add(btnEditar);
            pnlHeader.Controls.Add(btnNuevo);

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ConfigurarEstiloTabla(dgvFolios);
            dgvFolios.SelectionChanged += DgvFolios_SelectionChanged;

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 15, 0, 0)
            };
            pnlGridCard.Controls.Add(dgvFolios);

            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlHeader);

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

        private void CargarFolios()
        {
            try
            {
                var lista = _db.Folios.OrderBy(f => f.TipoDocumento).ToList();
                dgvFolios.DataSource = lista;

                if (dgvFolios.Columns["FolioID"] != null) dgvFolios.Columns["FolioID"].HeaderText = "ID";
                if (dgvFolios.Columns["TipoDocumento"] != null) dgvFolios.Columns["TipoDocumento"].HeaderText = "Tipo Documento (DTE)";
                if (dgvFolios.Columns["FolioDesde"] != null) dgvFolios.Columns["FolioDesde"].HeaderText = "Folio Desde";
                if (dgvFolios.Columns["FolioHasta"] != null) dgvFolios.Columns["FolioHasta"].HeaderText = "Folio Hasta";
                if (dgvFolios.Columns["FolioActual"] != null) dgvFolios.Columns["FolioActual"].HeaderText = "Siguiente Folio a Emitir";
                if (dgvFolios.Columns["FoliosUsados"] != null) dgvFolios.Columns["FoliosUsados"].HeaderText = "Folios Usados";
                if (dgvFolios.Columns["FoliosDisponibles"] != null) dgvFolios.Columns["FoliosDisponibles"].HeaderText = "Folios Disponibles";
                if (dgvFolios.Columns["Activo"] != null) dgvFolios.Columns["Activo"].HeaderText = "Estado Rango";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar folios: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            ComboBox cbTipo = new ComboBox { Location = new Point(25, 82), Size = new Size(310, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cbTipo.Items.AddRange(new string[] { "Boleta Electrónica", "Factura Electrónica", "Guía de Despacho" });
            cbTipo.SelectedItem = string.IsNullOrEmpty(f.TipoDocumento) ? "Boleta Electrónica" : f.TipoDocumento;

            Label lblDesde = new Label { Text = "Folio Desde:", Location = new Point(25, 122), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numDesde = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = f.FolioDesde == 0 ? 1 : f.FolioDesde, Location = new Point(25, 144), Size = new Size(145, 30), Font = new Font("Segoe UI", 10F) };

            Label lblHasta = new Label { Text = "Folio Hasta:", Location = new Point(190, 122), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numHasta = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = f.FolioHasta == 0 ? 1000 : f.FolioHasta, Location = new Point(190, 144), Size = new Size(145, 30), Font = new Font("Segoe UI", 10F) };

            Label lblActual = new Label { Text = "Siguiente Folio a Emitir:", Location = new Point(25, 184), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            NumericUpDown numActual = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = f.FolioActual == 0 ? 1 : f.FolioActual, Location = new Point(25, 206), Size = new Size(310, 30), Font = new Font("Segoe UI", 10F) };

            Button btnGuardar = new Button
            {
                Text = esNuevo ? "💾 Guardar Rango" : "💾 Guardar Cambios",
                Location = new Point(25, 260),
                Size = new Size(310, 42),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

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
                    if (esNuevo) _db.Folios.Add(f);
                    _db.SaveChanges();

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
    }
}