using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
namespace SISTEMAACTUALIZADO.Modals
{
    public class FormDetalleCompraModal : Form
    {
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private readonly int _compraId;

        public FormDetalleCompraModal(int compraId)
        {
            _compraId = compraId;
            InitializeComponent();
            CargarDetalles();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(820, 560);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlBorde = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };
            pnlBorde.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                ControlPaint.DrawBorder(e.Graphics, pnlBorde.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            this.Controls.Add(pnlBorde);
        }

        private void CargarDetalles()
        {
            try
            {
                using var db = new AppDbContext();
                var compra = db.Compras.FirstOrDefault(c => c.CompraID == _compraId);
                if (compra == null) return;

                var detalles = db.DetalleCompras.Where(d => d.CompraID == _compraId).ToList();

                Panel pnlBorde = (Panel)this.Controls[0];
                pnlBorde.Controls.Clear();

                // Cabecera Modal
                Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.Transparent };
                pnlHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

                string iconoTipo = compra.TipoCompra == "MERCADERIA" ? "📦" : "📄";
                Label lblTitulo = new Label
                {
                    Text = $"{iconoTipo} Detalle de Compra - Folio N° {compra.NroFacturaProveedor} ({compra.TipoDocumento})",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    Dock = DockStyle.Left,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true
                };
                lblTitulo.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); };

                Button btnCerrar = new Button
                {
                    Text = "✕",
                    Size = new Size(32, 32),
                    Dock = DockStyle.Right,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Cursor = Cursors.Hand
                };
                btnCerrar.FlatAppearance.BorderSize = 0;
                btnCerrar.Click += (s, e) => this.Close();

                pnlHeader.Controls.AddRange(new Control[] { lblTitulo, btnCerrar });

                // Ficha Resumen del Proveedor y Fecha
                Panel pnlInfo = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 8, 0, 10) };
                pnlInfo.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlInfo.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

                Label lblProv = new Label { Text = $"PROVEEDOR: {compra.RazonSocialProveedor} (RUT: {compra.RutProveedor})", Location = new Point(12, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
                Label lblFch = new Label { Text = $"Fecha Emisión: {compra.FechaEmision:dd-MM-yyyy}  |  Tipo Origen: {(compra.TipoCompra == "MERCADERIA" ? "Mercadería para Venta (Afectó Stock)" : "Gasto / Insumo Interno")}", Location = new Point(12, 34), AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(100, 116, 139) };
                pnlInfo.Controls.AddRange(new Control[] { lblProv, lblFch });

                // Grilla de Ítems
                DataGridView dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowTemplate = { Height = 30 },
                    GridColor = Color.FromArgb(241, 245, 249)
                };

                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersHeight = 32;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
                dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
                dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

                dgv.Columns.Add("Item", "Descripción del Ítem / Producto");
                dgv.Columns.Add("Cant", "Cantidad");
                dgv.Columns.Add("Costo", "Costo Neto Unit.");
                dgv.Columns.Add("Subtotal", "Subtotal Neto");

                dgv.Columns["Cant"].Width = 85;
                dgv.Columns["Cant"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.Columns["Cant"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                dgv.Columns["Costo"].Width = 130;
                dgv.Columns["Costo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Columns["Costo"].DefaultCellStyle.Format = "$#,##0";

                dgv.Columns["Subtotal"].Width = 130;
                dgv.Columns["Subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Columns["Subtotal"].DefaultCellStyle.Format = "$#,##0";
                dgv.Columns["Subtotal"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                foreach (var d in detalles)
                {
                    dgv.Rows.Add(d.NombreProducto, d.Cantidad, d.PrecioCostoUnitario, d.Subtotal);
                }

                // Panel Totales Inferior
                Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 10, 0, 0) };
                Label lblTotales = new Label
                {
                    Text = $"Neto: ${compra.MontoNeto:N0}   |   IVA (19%): ${compra.MontoIva:N0}   |   TOTAL: ${compra.MontoTotal:N0}",
                    Dock = DockStyle.Right,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(16, 185, 129),
                    AutoSize = true
                };

                Button btnCerrarModal = new Button
                {
                    Text = "Cerrar",
                    Dock = DockStyle.Left,
                    Size = new Size(100, 34),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCerrarModal.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnCerrarModal.Click += (s, e) => this.Close();

                pnlBottom.Controls.AddRange(new Control[] { btnCerrarModal, lblTotales });

                pnlBorde.Controls.Add(dgv);
                pnlBorde.Controls.Add(pnlBottom);
                pnlBorde.Controls.Add(pnlInfo);
                pnlBorde.Controls.Add(pnlHeader);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}