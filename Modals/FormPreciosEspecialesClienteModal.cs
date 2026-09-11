using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormPreciosEspecialesClienteModal : Form
    {
        private readonly Cliente _cliente;
        private readonly ProductoService _productoService = new ProductoService();

        private ComboBox cbProductos = null!;
        private Label lblCostoReferencia = null!;
        private TextBox txtPrecioEspecial = null!;
        private DateTimePicker dtpInicio = null!;
        private DateTimePicker dtpFin = null!;
        private Button btnGuardar = null!;
        private DataGridView dgvPrecios = null!;
        private Label lblContadorFooter = null!;
        private List<Producto> _productos = new List<Producto>();

        public FormPreciosEspecialesClienteModal(Cliente cliente)
        {
            _cliente = cliente;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint | 
                           ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarProductos();
            CargarPreciosEspeciales();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = $"Precios Especiales - {_cliente.RazonSocial}";
            this.ClientSize = new Size(820, 640);
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

            // 1. ENCABEZADO SUPERIOR
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 52, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 0, 0, 8) 
            };

            Label lblTitulo = new Label
            {
                Text = $"💲 Precios Especiales para {_cliente.RazonSocial}",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 24
            };

            Label lblSubtitulo = new Label
            {
                Text = "Configura tarifas y convenios con margen personalizado y vigencia por fecha",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 18
            };

            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblTitulo);

            // 2. FORMULARIO EN TARJETA BLANCA REDONDEADA
            Panel pnlFormWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 210,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            Panel pnlCardForm = CrearTarjetaRedondeada(0, 0, 0, 198, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardForm.Dock = DockStyle.Fill;
            pnlCardForm.Padding = new Padding(16, 14, 16, 14);

            Label lblSecTit = new Label
            {
                Text = "ASIGNAR NUEVO PRECIO ESPECIAL TEMPORAL",
                Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Dock = DockStyle.Top,
                Height = 22
            };

            // Fila 1: Producto, Referencia de Costo y Precio Pactado (ampliada a 76px)
            Panel pnlFila1 = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.Transparent };

            cbProductos = new ComboBox 
            { 
                Size = new Size(360, 26), 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Font = new Font("Segoe UI", 9.2F) 
            };
            cbProductos.SelectedIndexChanged += (s, e) => ActualizarReferenciaCosto();
            Panel pnlGrpProd = CrearGrupoLimpio("PRODUCTO", cbProductos, 370);
            pnlGrpProd.Location = new Point(0, 2);

            lblCostoReferencia = new Label
            {
                Text = "Costo Neto: $ 0  |  P. Venta Normal: $ 0",
                Location = new Point(2, 54),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 116, 144)
            };

            txtPrecioEspecial = new TextBox 
            { 
                Size = new Size(160, 26), 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            txtPrecioEspecial.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
            txtPrecioEspecial.TextChanged += (s, e) => MonedaHelper.AplicarMascaraEnVivo(txtPrecioEspecial);
            Panel pnlGrpPrecio = CrearGrupoConCaja("PRECIO ESPECIAL ($)", txtPrecioEspecial, 175);
            pnlGrpPrecio.Location = new Point(385, 2);

            pnlFila1.Controls.AddRange(new Control[] { pnlGrpProd, lblCostoReferencia, pnlGrpPrecio });

            // Fila 2: Fechas de Vigencia y Botón Guardar
            Panel pnlFila2 = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };

            dtpInicio = new DateTimePicker { Size = new Size(140, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 9F) };
            Panel pnlGrpIni = CrearGrupoLimpio("VIGENCIA DESDE", dtpInicio, 145);
            pnlGrpIni.Location = new Point(0, 2);

            dtpFin = new DateTimePicker { Size = new Size(140, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7), Font = new Font("Segoe UI", 9F) };
            Panel pnlGrpFin = CrearGrupoLimpio("VIGENCIA HASTA", dtpFin, 145);
            pnlGrpFin.Location = new Point(155, 2);

            btnGuardar = CrearBoton("💾 Guardar Precio", Color.FromArgb(16, 185, 129), Color.White, new Size(245, 34));
            btnGuardar.Location = new Point(385, 18);
            btnGuardar.Click += BtnGuardar_Click;

            pnlFila2.Controls.AddRange(new Control[] { pnlGrpIni, pnlGrpFin, btnGuardar });

            pnlCardForm.Controls.Add(pnlFila2);
            pnlCardForm.Controls.Add(pnlFila1);
            pnlCardForm.Controls.Add(lblSecTit);
            pnlFormWrapper.Controls.Add(pnlCardForm);

            // 3. TARJETA DE GRILLA PRINCIPAL
            Panel pnlGridCard = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Padding = new Padding(10);

            dgvPrecios = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 }
            };

            dgvPrecios.EnableHeadersVisualStyles = false;
            dgvPrecios.ColumnHeadersHeight = 36;
            dgvPrecios.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvPrecios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPrecios.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgvPrecios.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvPrecios.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            dgvPrecios.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgvPrecios.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvPrecios.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(4, 6, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 convenios registrados",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlGridCard.Controls.Add(dgvPrecios);
            pnlGridCard.Controls.Add(pnlFooter);

            pnlMainModal.Controls.Add(pnlGridCard);
            pnlMainModal.Controls.Add(pnlFormWrapper);
            pnlMainModal.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMainModal);
            this.ResumeLayout(false);
        }

        private void CargarProductos()
        {
            _productos = _productoService.ObtenerProductosActivos();
            cbProductos.DataSource = _productos;
            cbProductos.DisplayMember = "Nombre";
            cbProductos.ValueMember = "ProductoID";

            ActualizarReferenciaCosto();
        }

        private void ActualizarReferenciaCosto()
        {
            if (cbProductos.SelectedItem is Producto prod)
            {
                lblCostoReferencia.Text = $"Costo Neto: {MonedaHelper.Formatear(prod.PrecioCosto, conSigno: true)}  |  P. Venta Normal: {MonedaHelper.Formatear(prod.PrecioUnitario, conSigno: true)}";
            }
        }

        private void CargarPreciosEspeciales()
        {
            try
            {
                var lista = _productoService.ObtenerPreciosEspecialesCliente(_cliente.IdCliente);
                dgvPrecios.DataSource = lista;

                var estiloMoneda = new DataGridViewCellStyle
                {
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                    Format = "$ #,##0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                };

                if (dgvPrecios.Columns["IdEspecial"] != null) dgvPrecios.Columns["IdEspecial"].Visible = false;

                if (dgvPrecios.Columns["Producto"] != null)
                {
                    dgvPrecios.Columns["Producto"].HeaderText = "PRODUCTO";
                    dgvPrecios.Columns["Producto"].FillWeight = 34;
                    dgvPrecios.Columns["Producto"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvPrecios.Columns["Producto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                if (dgvPrecios.Columns["CostoBase"] != null)
                {
                    dgvPrecios.Columns["CostoBase"].HeaderText = "COSTO NETO";
                    dgvPrecios.Columns["CostoBase"].FillWeight = 16;
                    dgvPrecios.Columns["CostoBase"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvPrecios.Columns["CostoBase"].DefaultCellStyle = estiloMoneda;
                }

                if (dgvPrecios.Columns["PrecioEspecial"] != null)
                {
                    dgvPrecios.Columns["PrecioEspecial"].HeaderText = "PRECIO PACTADO";
                    dgvPrecios.Columns["PrecioEspecial"].FillWeight = 18;
                    dgvPrecios.Columns["PrecioEspecial"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvPrecios.Columns["PrecioEspecial"].DefaultCellStyle = new DataGridViewCellStyle
                    {
                        FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                        Format = "$ #,##0",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = Color.FromArgb(16, 185, 129),
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
                    };
                }

                if (dgvPrecios.Columns["Desde"] != null)
                {
                    dgvPrecios.Columns["Desde"].HeaderText = "DESDE";
                    dgvPrecios.Columns["Desde"].FillWeight = 14;
                    dgvPrecios.Columns["Desde"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvPrecios.Columns["Desde"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvPrecios.Columns["Desde"].DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                if (dgvPrecios.Columns["Hasta"] != null)
                {
                    dgvPrecios.Columns["Hasta"].HeaderText = "HASTA";
                    dgvPrecios.Columns["Hasta"].FillWeight = 14;
                    dgvPrecios.Columns["Hasta"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvPrecios.Columns["Hasta"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvPrecios.Columns["Hasta"].DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                if (dgvPrecios.Columns["EstadoVigencia"] != null)
                {
                    dgvPrecios.Columns["EstadoVigencia"].HeaderText = "ESTADO";
                    dgvPrecios.Columns["EstadoVigencia"].FillWeight = 14;
                    dgvPrecios.Columns["EstadoVigencia"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvPrecios.Columns["EstadoVigencia"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvPrecios.Columns["EstadoVigencia"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }

                lblContadorFooter.Text = $"Mostrando {(lista != null ? lista.Count : 0)} convenio(s) registrado(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar convenios: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            decimal precio = MonedaHelper.Limpiar(txtPrecioEspecial.Text);
            if (cbProductos.SelectedItem is not Producto prod || precio <= 0)
            {
                MessageBox.Show("Seleccione un producto y digite un precio especial válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtpFin.Value.Date < dtpInicio.Value.Date)
            {
                MessageBox.Show("La fecha de término no puede ser menor a la fecha de inicio.", "Fecha Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (precio < prod.PrecioCosto)
            {
                var confirmMargenNegativo = MessageBox.Show(
                    $"⚠️ ADVERTENCIA DE MARGEN NEGATIVO\n\nEl precio pactado ({MonedaHelper.Formatear(precio, conSigno: true)}) es MENOR que el costo neto del producto ({MonedaHelper.Formatear(prod.PrecioCosto, conSigno: true)}).\n\n¿Desea registrarlo de todas formas?",
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

                _productoService.GuardarPrecioEspecial(nuevo);
                MessageBox.Show("Precio especial registrado con éxito.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPrecioEspecial.Clear();
                CargarPreciosEspeciales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar precio especial: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
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

        private Panel CrearGrupoConCaja(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0), BackColor = Color.Transparent };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            Panel pnlInputBox = new Panel { Location = new Point(0, 18), Size = new Size(ancho, 28), BackColor = Color.FromArgb(248, 250, 252) };
            pnlInputBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlInputBox.Width - 1, pnlInputBox.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(r, 6);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            control.Location = new Point(6, 3);
            control.Width = ancho - 12;
            pnlInputBox.Controls.Add(control);

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(pnlInputBox);
            return pnl;
        }

        private Panel CrearGrupoLimpio(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0), BackColor = Color.Transparent };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            control.Location = new Point(0, 18);
            control.Width = ancho;

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }
    }
}