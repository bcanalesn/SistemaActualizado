using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormHistorialPuntosModal : Form
    {
        private readonly Cliente _cliente;
        private readonly FidelizacionService _fidelizacionService = new FidelizacionService();

        private Label lblDisponibles = null!;
        private Label lblAcumulados = null!;
        private Label lblCanjeados = null!;
        private DataGridView dgvMovimientos = null!;

        public FormHistorialPuntosModal(Cliente cliente)
        {
            _cliente = cliente;
            InitializeComponent();
            CargarDatosPuntos();
        }

        private void InitializeComponent()
        {
            this.Text = $"Puntos de Fidelización - {_cliente.RazonSocial}";
            this.ClientSize = new Size(680, 470); // Altura optimizada sin footer
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblTit = new Label
            {
                Text = $"🎁 Historial de Puntos: {_cliente.RazonSocial} (RUT: {_cliente.Rut})",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 8),
                AutoSize = true
            };

            Panel card1 = CrearCardMetrica("DISPONIBLES", "0 pts", Color.FromArgb(124, 58, 237), new Point(14, 34), out lblDisponibles);
            Panel card2 = CrearCardMetrica("TOTAL ACUMULADO", "0 pts", Color.FromArgb(22, 163, 74), new Point(230, 34), out lblAcumulados);
            Panel card3 = CrearCardMetrica("TOTAL CANJEADO", "0 pts", Color.FromArgb(239, 68, 68), new Point(446, 34), out lblCanjeados);

            pnlHeader.Controls.AddRange(new Control[] { lblTit, card1, card2, card3 });

            dgvMovimientos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowTemplate = { Height = 32 }
            };

            ConfigurarColumnasGrilla();

            // Solo se agrega la grilla y el header (se elimina el pnlFooter con el botón Cerrar)
            this.Controls.Add(dgvMovimientos);
            this.Controls.Add(pnlHeader);
        }

        private void ConfigurarColumnasGrilla()
        {
            dgvMovimientos.EnableHeadersVisualStyles = false;
            
            // Colores oscuros fijos sin permitir que la selección de filas altere el encabezado
            dgvMovimientos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvMovimientos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvMovimientos.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvMovimientos.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvMovimientos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvMovimientos.ColumnHeadersHeight = 34;

            // Color de selección de fila suave (no azul invasivo)
            dgvMovimientos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            dgvMovimientos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

            dgvMovimientos.Columns.Clear();
            dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", DataPropertyName = "Fecha", HeaderText = "FECHA / HORA", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd-MM-yyyy HH:mm" } });
            dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", DataPropertyName = "Tipo", HeaderText = "TIPO", Width = 110 });
            dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Puntos", DataPropertyName = "Puntos", HeaderText = "PUNTOS", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) } });
            dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", DataPropertyName = "Descripcion", HeaderText = "DESCRIPCIÓN", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", DataPropertyName = "Usuario", HeaderText = "USUARIO", Width = 95 });

            dgvMovimientos.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvMovimientos.Rows.Count) return;
                string col = dgvMovimientos.Columns[e.ColumnIndex].Name;

                if (col == "Puntos" && e.Value is int pts)
                {
                    e.Value = pts > 0 ? $"+{pts:N0}" : $"{pts:N0}";
                    e.CellStyle.ForeColor = pts >= 0 ? Color.FromArgb(22, 163, 74) : Color.FromArgb(239, 68, 68);
                    e.FormattingApplied = true;
                }
                else if (col == "Tipo" && e.Value != null)
                {
                    string t = e.Value.ToString()!;
                    e.CellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    e.CellStyle.ForeColor = t == "ACUMULACION" ? Color.FromArgb(22, 163, 74) :
                                            t == "CANJE" ? Color.FromArgb(124, 58, 237) : Color.FromArgb(239, 68, 68);
                }
            };
        }

        private Panel CrearCardMetrica(string titulo, string valorInicial, Color colorTexto, Point ubicacion, out Label lblVal)
        {
            Panel pnl = new Panel
            {
                Location = ubicacion,
                Size = new Size(200, 56),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblT = new Label { Text = titulo, Location = new Point(8, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            lblVal = new Label { Text = valorInicial, Location = new Point(8, 24), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = colorTexto };

            pnl.Controls.AddRange(new Control[] { lblT, lblVal });
            return pnl;
        }

        private void CargarDatosPuntos()
        {
            var resumen = _fidelizacionService.ObtenerResumenPuntos(_cliente.IdCliente);
            lblDisponibles.Text = $"{resumen.PuntosDisponibles:N0} pts (${resumen.EquivalenciaPesosDisponible:N0})";
            lblAcumulados.Text = $"{resumen.PuntosHistoricosAcumulados:N0} pts";
            lblCanjeados.Text = $"{resumen.PuntosHistoricosCanjeados:N0} pts";

            var historial = _fidelizacionService.ObtenerHistorialMovimientos(_cliente.IdCliente);
            dgvMovimientos.DataSource = historial;
        }
    }
}