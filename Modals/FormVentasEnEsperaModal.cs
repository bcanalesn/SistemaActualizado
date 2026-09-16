using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormVentasEnEsperaModal : Form
    {
        private readonly VentaService _ventaService = new VentaService();
        private readonly string _vendedorActual;
        private DataGridView dgvVentas = null!;
        public int IdTveSeleccionado { get; private set; } = 0;

        public FormVentasEnEsperaModal(string vendedorActual)
        {
            _vendedorActual = vendedorActual;
            ConfigurarFormulario();
            CargarVentas();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Ventas en Espera";
            this.Size = new Size(720, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Label lblT = new Label 
            { 
                Text = "⏸️ VENTAS PAUSADAS / EN ESPERA", 
                Location = new Point(16, 14), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(15, 23, 42) 
            };

            dgvVentas = new DataGridView
            {
                Location = new Point(16, 46),
                Size = new Size(672, 275),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvVentas.Columns.Add("idTve", "ID");
            dgvVentas.Columns["idTve"]!.Visible = false;
            dgvVentas.Columns.Add("Hora", "HORA");
            dgvVentas.Columns.Add("Cliente", "CLIENTE / REFERENCIA");
            dgvVentas.Columns.Add("Vendedor", "VENDEDOR");
            dgvVentas.Columns.Add("Total", "TOTAL");
            dgvVentas.Columns.Add("Estado", "ESTADO");

            if (dgvVentas.Columns["Hora"] != null) dgvVentas.Columns["Hora"]!.FillWeight = 16;
            if (dgvVentas.Columns["Cliente"] != null) dgvVentas.Columns["Cliente"]!.FillWeight = 38;
            if (dgvVentas.Columns["Vendedor"] != null) dgvVentas.Columns["Vendedor"]!.FillWeight = 18;
            if (dgvVentas.Columns["Total"] != null) dgvVentas.Columns["Total"]!.FillWeight = 14;
            if (dgvVentas.Columns["Estado"] != null) dgvVentas.Columns["Estado"]!.FillWeight = 14;

            dgvVentas.CellDoubleClick += (s, e) => ConfirmarRecuperacion();

            Button btnRecuperar = new Button
            {
                Text = "📂 Recuperar Venta",
                Location = new Point(548, 332),
                Size = new Size(140, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRecuperar.FlatAppearance.BorderSize = 0;
            btnRecuperar.Click += (s, e) => ConfirmarRecuperacion();

            Button btnCancelarVenta = new Button
            {
                Text = "🗑️ Descartar Venta",
                Location = new Point(16, 332),
                Size = new Size(140, 38),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(239, 68, 68),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelarVenta.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnCancelarVenta.Click += BtnCancelarVenta_Click;

            this.Controls.AddRange(new Control[] { lblT, dgvVentas, btnRecuperar, btnCancelarVenta });
        }

        private void CargarVentas()
        {
            dgvVentas.Rows.Clear();
            var lista = _ventaService.ObtenerVentasEnEspera();

            foreach (var v in lista)
            {
                string estadoVisual = v.status == "EnUso" ? $"🔒 En uso ({v.UserDTE})" : "🟡 En Espera";
                dgvVentas.Rows.Add(v.idTve, v.FecDoc.ToString("HH:mm:ss"), v.RazonSocial, v.Vendedor, MonedaHelper.Formatear(v.Total, conSigno: true), estadoVisual);
            }
        }

        private void ConfirmarRecuperacion()
        {
            if (dgvVentas.CurrentRow == null) return;

            string vendedorFila = dgvVentas.CurrentRow.Cells["Vendedor"].Value?.ToString() ?? "";

            // RESTRICCIÓN DE PROPIEDAD: Solo el vendedor dueño puede retomar su venta
            if (!vendedorFila.Equals(_vendedorActual, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Esta venta pertenece a '{vendedorFila}'.\nSolo el vendedor asignado puede reanudarla.", "Acceso Restringido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IdTveSeleccionado = Convert.ToInt32(dgvVentas.CurrentRow.Cells["idTve"].Value);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancelarVenta_Click(object? sender, EventArgs e)
        {
            if (dgvVentas.CurrentRow == null) return;

            string vendedorFila = dgvVentas.CurrentRow.Cells["Vendedor"].Value?.ToString() ?? "";
            if (!vendedorFila.Equals(_vendedorActual, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Solo el vendedor asignado ('{vendedorFila}') puede descartar esta venta.", "Acceso Restringido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idTve = Convert.ToInt32(dgvVentas.CurrentRow.Cells["idTve"].Value);

            if (MessageBox.Show("¿Desea descartar y anular esta venta en espera?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _ventaService.CancelarVentaEnEspera(idTve);
                CargarVentas();
            }
        }
    }
}