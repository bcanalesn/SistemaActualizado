using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormPreciosEspecialesClienteModal : Form
    {
        private readonly Cliente _cliente;
        private ComboBox cbProductos = null!;
        private Label lblCostoReferencia = null!;
        private TextBox txtPrecioEspecial = null!;
        private DateTimePicker dtpInicio = null!;
        private DateTimePicker dtpFin = null!;
        private Button btnGuardar = null!;
        private DataGridView dgvPrecios = null!;
        private List<Producto> _productos = new List<Producto>();

        public FormPreciosEspecialesClienteModal(Cliente cliente)
        {
            _cliente = cliente;
            InitializeComponent();
            CargarProductos();
            CargarPreciosEspeciales();
        }

        private void InitializeComponent()
        {
            this.Text = $"Precios Especiales - {_cliente.RazonSocial}";
            this.ClientSize = new Size(700, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Label lblTitulo = new Label
            {
                Text = $"💲 Precios Especiales para {_cliente.RazonSocial}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 12),
                AutoSize = true
            };

            // Formulario superior para nuevo precio
            GroupBox gbNuevo = new GroupBox
            {
                Text = " Asignar Nuevo Precio Especial Temporal ",
                Location = new Point(16, 42),
                Size = new Size(668, 160),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            Label lblProd = new Label { Text = "Producto:", Location = new Point(14, 22), AutoSize = true };
            cbProductos = new ComboBox 
            { 
                Location = new Point(14, 42), 
                Size = new Size(290, 26), 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Font = new Font("Segoe UI", 9F) 
            };
            cbProductos.SelectedIndexChanged += (s, e) => ActualizarReferenciaCosto();

            // Etiqueta para mostrar Costo Neto y Precio Normal de referencia
            lblCostoReferencia = new Label
            {
                Text = "Costo Neto: $0  |  P. Venta Normal: $0",
                Location = new Point(14, 72),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 116, 144)
            };

            Label lblPrecio = new Label { Text = "Precio Especial ($):", Location = new Point(320, 22), AutoSize = true };
            txtPrecioEspecial = new TextBox { Location = new Point(320, 42), Size = new Size(130, 26), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtPrecioEspecial.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };

            Label lblIni = new Label { Text = "Vigencia Desde:", Location = new Point(14, 100), AutoSize = true };
            dtpInicio = new DateTimePicker { Location = new Point(14, 120), Size = new Size(135, 24), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            Label lblFin = new Label { Text = "Vigencia Hasta:", Location = new Point(160, 100), AutoSize = true };
            dtpFin = new DateTimePicker { Location = new Point(160, 120), Size = new Size(135, 24), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7) };

            btnGuardar = new Button
            {
                Text = "💾 Guardar Precio",
                Location = new Point(470, 42),
                Size = new Size(180, 102),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            gbNuevo.Controls.AddRange(new Control[] { lblProd, cbProductos, lblCostoReferencia, lblPrecio, txtPrecioEspecial, lblIni, dtpInicio, lblFin, dtpFin, btnGuardar });

            // Grilla de precios vigentes/históricos
            dgvPrecios = new DataGridView
            {
                Location = new Point(16, 212),
                Size = new Size(668, 305),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvPrecios.EnableHeadersVisualStyles = false;
            dgvPrecios.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvPrecios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPrecios.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvPrecios.RowTemplate.Height = 30;

            this.Controls.AddRange(new Control[] { lblTitulo, gbNuevo, dgvPrecios });
        }

        private void CargarProductos()
        {
            using var db = new AppDbContext();
            _productos = db.Productos.Where(p => p.Estado).OrderBy(p => p.Nombre).ToList();
            cbProductos.DataSource = _productos;
            cbProductos.DisplayMember = "Nombre";
            cbProductos.ValueMember = "ProductoID";

            ActualizarReferenciaCosto();
        }

        private void ActualizarReferenciaCosto()
        {
            if (cbProductos.SelectedItem is Producto prod)
            {
                lblCostoReferencia.Text = $"🏷️ Costo Neto: ${prod.PrecioCosto:N0}   |   P. Normal (L1): ${prod.PrecioUnitario:N0}";
            }
        }

        private void CargarPreciosEspeciales()
        {
            DateTime hoy = DateTime.Today;
            using var db = new AppDbContext();

            var datos = (from pe in db.PreciosEspecialesClientes
                         join pr in db.Productos on pe.ProductoId equals pr.ProductoID
                         where pe.ClienteId == _cliente.IdCliente && pe.Estado
                         orderby pe.FechaFin descending
                         select new
                         {
                             pe.IdEspecial,
                             Producto = pr.Nombre,
                             CostoBase = pr.PrecioCosto,
                             PrecioEspecial = pe.PrecioEspecial,
                             Desde = pe.FechaInicio,
                             Hasta = pe.FechaFin,
                             EstadoVigencia = (pe.FechaInicio <= hoy && pe.FechaFin >= hoy) ? "🟢 VIGENTE" : (pe.FechaFin < hoy ? "🔴 EXPIRADO" : "🟡 PROGRAMADO")
                         }).Take(10) // <-- Limita a los 10 registros más recientes
                         .ToList();

            dgvPrecios.DataSource = datos;

            if (dgvPrecios.Columns["IdEspecial"] != null) dgvPrecios.Columns["IdEspecial"].Visible = false;
            if (dgvPrecios.Columns["CostoBase"] != null)
            {
                dgvPrecios.Columns["CostoBase"].HeaderText = "COSTO NETO";
                dgvPrecios.Columns["CostoBase"].DefaultCellStyle.Format = "$#,##0";
            }
            if (dgvPrecios.Columns["PrecioEspecial"] != null)
            {
                dgvPrecios.Columns["PrecioEspecial"].HeaderText = "PRECIO PACTADO";
                dgvPrecios.Columns["PrecioEspecial"].DefaultCellStyle.Format = "$#,##0";
            }
            if (dgvPrecios.Columns["Desde"] != null) dgvPrecios.Columns["Desde"].DefaultCellStyle.Format = "dd/MM/yyyy";
            if (dgvPrecios.Columns["Hasta"] != null) dgvPrecios.Columns["Hasta"].DefaultCellStyle.Format = "dd/MM/yyyy";
            if (dgvPrecios.Columns["EstadoVigencia"] != null) dgvPrecios.Columns["EstadoVigencia"].HeaderText = "ESTADO";
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (cbProductos.SelectedItem is not Producto prod || !decimal.TryParse(txtPrecioEspecial.Text.Trim(), out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Seleccione un producto y digite un precio especial válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtpFin.Value.Date < dtpInicio.Value.Date)
            {
                MessageBox.Show("La fecha de término no puede ser menor a la fecha de inicio.", "Fecha Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Advertencia si el precio pactado es menor al costo neto
            if (precio < prod.PrecioCosto)
            {
                var confirmMargenNegativo = MessageBox.Show(
                    $"⚠️ ADVERTENCIA DE MARGEN NEGATIVO\n\nEl precio pactado (${precio:N0}) es MENOR que el costo neto del producto (${prod.PrecioCosto:N0}).\n\n¿Desea registrarlo de todas formas?",
                    "Confirmar Precio Bajo Costo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (confirmMargenNegativo != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                using var db = new AppDbContext();
                var nuevo = new PrecioEspecialCliente
                {
                    ClienteId = _cliente.IdCliente,
                    ProductoId = prod.ProductoID,
                    PrecioEspecial = precio,
                    FechaInicio = dtpInicio.Value.Date,
                    FechaFin = dtpFin.Value.Date,
                    Estado = true,
                    FechaRegistro = DateTime.Now
                };

                db.PreciosEspecialesClientes.Add(nuevo);
                db.SaveChanges();

                MessageBox.Show("Precio especial registrado con éxito.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPrecioEspecial.Clear();
                CargarPreciosEspeciales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar precio especial: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}