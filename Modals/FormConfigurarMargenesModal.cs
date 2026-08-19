using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormConfigurarMargenesModal : Form
    {
        private RadioButton rbTodo = null!;
        private RadioButton rbCategoria = null!;
        private RadioButton rbSeleccion = null!;
        private ComboBox cbCategorias = null!;
        private CheckBox chkRedondear = null!;
        private NumericUpDown[] numMargenes = new NumericUpDown[10];
        private Label lblCostoInfo = null!;
        private Producto? _productoSeleccionado;
        private List<string> _categoriasDisponibles = new List<string>();

        public FormConfigurarMargenesModal(Producto? prodSeleccionado = null, List<string>? categorias = null)
        {
            _productoSeleccionado = prodSeleccionado;
            _categoriasDisponibles = categorias ?? new List<string>();
            InitializeComponent();
            CargarValoresSegunContexto();
        }

        private void InitializeComponent()
        {
            this.Text = "Configurar Márgenes y Listas de Precios";
            this.Size = new Size(580, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblTitulo = new Label
            {
                Text = "⚙️ Asignación de Márgenes de Ganancia",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };
            Label lblSub = new Label
            {
                Text = "Calcula los precios automáticamente: (Costo Neto + % Margen) + 19% IVA",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(16, 34),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblSub });

            // Panel de Ámbito
            GroupBox gbAmbito = new GroupBox
            {
                Text = "Aplicar cambios a:",
                Location = new Point(16, 68),
                Size = new Size(530, 95),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            rbSeleccion = new RadioButton 
            { 
                Text = _productoSeleccionado != null ? $"Solo: {_productoSeleccionado.Nombre}" : "Solo producto seleccionado (Ninguno)", 
                Location = new Point(15, 22), 
                AutoSize = true, 
                Checked = _productoSeleccionado != null,
                Enabled = _productoSeleccionado != null,
                Font = new Font("Segoe UI", 8.5F, _productoSeleccionado != null ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = _productoSeleccionado != null ? Color.FromArgb(2, 132, 199) : Color.Gray
            };

            rbCategoria = new RadioButton { Text = "Por Categoría:", Location = new Point(15, 52), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            
            cbCategorias = new ComboBox
            {
                Location = new Point(125, 50),
                Size = new Size(160, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Font = new Font("Segoe UI", 8.5F)
            };
            cbCategorias.Items.AddRange(_categoriasDisponibles.ToArray());
            if (cbCategorias.Items.Count > 0) cbCategorias.SelectedIndex = 0;

            rbTodo = new RadioButton 
            { 
                Text = "Todo el Catálogo", 
                Location = new Point(310, 52), 
                AutoSize = true, 
                Checked = _productoSeleccionado == null, 
                Font = new Font("Segoe UI", 8.5F) 
            };

            rbCategoria.CheckedChanged += (s, e) => { cbCategorias.Enabled = rbCategoria.Checked; };
            rbSeleccion.CheckedChanged += (s, e) => { if (rbSeleccion.Checked) CargarValoresDesdeProducto(); };
            rbTodo.CheckedChanged += (s, e) => { if (rbTodo.Checked) CargarValoresPorDefecto(); };

            gbAmbito.Controls.AddRange(new Control[] { rbSeleccion, rbCategoria, cbCategorias, rbTodo });

            // Etiqueta informativa del costo base
            lblCostoInfo = new Label
            {
                Location = new Point(16, 168),
                Size = new Size(530, 20),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Panel con la lista de márgenes (Scrollable)
            Panel pnlListas = new Panel
            {
                Location = new Point(16, 190),
                Size = new Size(530, 315),
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            string[] titulosListas = new string[]
            {
                "Lista 1 (Precio Venta Público General)",
                "Lista 2 (Clientes Frecuentes / Mayorista 1)",
                "Lista 3 (Mayorista 2)",
                "Lista 4 (Distribuidor)",
                "Lista 5 (Liquidación / Oferta)",
                "Lista 6", "Lista 7", "Lista 8", "Lista 9", "Lista 10"
            };

            int yPos = 8;
            for (int i = 0; i < 10; i++)
            {
                Label lbl = new Label
                {
                    Text = titulosListas[i],
                    Location = new Point(10, yPos + 4),
                    Size = new Size(320, 20),
                    Font = new Font("Segoe UI", 8.5F, i == 0 ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = i == 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(30, 41, 59)
                };

                numMargenes[i] = new NumericUpDown
                {
                    Location = new Point(340, yPos),
                    Size = new Size(90, 24),
                    DecimalPlaces = 1,
                    Minimum = -50,
                    Maximum = 500,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    TextAlign = HorizontalAlignment.Right
                };

                Label lblPorc = new Label { Text = "%", Location = new Point(435, yPos + 4), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

                pnlListas.Controls.AddRange(new Control[] { lbl, numMargenes[i], lblPorc });
                yPos += 32;
            }

            // Opciones y Botones Footer
            chkRedondear = new CheckBox
            {
                Text = "Redondear precios calculados a la decena más cercana (ej: $1.426 ➔ $1.430)",
                Location = new Point(16, 515),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            Button btnGuardar = new Button
            {
                Text = "💾 Recalcular y Guardar",
                Location = new Point(230, 550),
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(420, 550),
                Size = new Size(125, 36),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { pnlHeader, gbAmbito, lblCostoInfo, pnlListas, chkRedondear, btnGuardar, btnCancelar });
        }

        private void CargarValoresSegunContexto()
        {
            if (_productoSeleccionado != null)
            {
                CargarValoresDesdeProducto();
            }
            else
            {
                CargarValoresPorDefecto();
            }
        }

        private void CargarValoresDesdeProducto()
        {
            if (_productoSeleccionado == null) return;

            decimal costo = _productoSeleccionado.PrecioCosto;
            lblCostoInfo.Text = $"Costo Neto Base del producto: ${costo:N0}";

            // Array de precios guardados en la BD
            decimal[] preciosGuardados = new decimal[]
            {
                _productoSeleccionado.PrecioUnitario,
                _productoSeleccionado.Precio2,
                _productoSeleccionado.Precio3,
                _productoSeleccionado.Precio4,
                _productoSeleccionado.Precio5,
                _productoSeleccionado.Precio6,
                _productoSeleccionado.Precio7,
                _productoSeleccionado.Precio8,
                _productoSeleccionado.Precio9,
                _productoSeleccionado.Precio10
            };

            for (int i = 0; i < 10; i++)
            {
                decimal precio = preciosGuardados[i];

                if (costo > 0 && precio > 0)
                {
                    // Fórmula Inversa: Margen % = (((PrecioBruto / 1.19) / CostoNeto) - 1) * 100
                    decimal netoCalculado = precio / 1.19m;
                    decimal margenReal = ((netoCalculado / costo) - 1.00m) * 100.0m;

                    // Ajustar a los límites permitidos del NumericUpDown
                    margenReal = Math.Max(numMargenes[i].Minimum, Math.Min(numMargenes[i].Maximum, Math.Round(margenReal, 1)));
                    numMargenes[i].Value = margenReal;
                }
                else if (i == 0 && _productoSeleccionado.MargenGanancia > 0)
                {
                    numMargenes[0].Value = _productoSeleccionado.MargenGanancia;
                }
                else
                {
                    numMargenes[i].Value = 0m;
                }
            }
        }

        private void CargarValoresPorDefecto()
        {
            lblCostoInfo.Text = "Valores estándar de referencia para aplicar";
            decimal[] margenesDefecto = new decimal[] { 35m, 25m, 18m, 12m, 5m, 0m, 0m, 0m, 0m, 0m };

            for (int i = 0; i < 10; i++)
            {
                numMargenes[i].Value = margenesDefecto[i];
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show("¿Está seguro de recalcular los precios según los porcentajes definidos?", "Confirmar Operación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                IQueryable<Producto> query = db.Productos.Where(p => p.Estado);

                if (rbSeleccion.Checked && _productoSeleccionado != null)
                {
                    query = query.Where(p => p.ProductoID == _productoSeleccionado.ProductoID);
                }
                else if (rbCategoria.Checked && cbCategorias.SelectedItem != null)
                {
                    string cat = cbCategorias.SelectedItem.ToString()!;
                    query = query.Where(p => p.Categoria == cat);
                }

                var productosAfectados = query.ToList();
                bool redondear = chkRedondear.Checked;

                foreach (var prod in productosAfectados)
                {
                    if (prod.PrecioCosto <= 0) continue;

                    decimal costo = prod.PrecioCosto;

                    // Margen de ganancia principal
                    prod.MargenGanancia = numMargenes[0].Value;
                    prod.PrecioUnitario = CalcularPrecio(costo, numMargenes[0].Value, redondear);

                    // Precios del 2 al 10
                    prod.Precio2 = CalcularPrecio(costo, numMargenes[1].Value, redondear);
                    prod.Precio3 = CalcularPrecio(costo, numMargenes[2].Value, redondear);
                    prod.Precio4 = CalcularPrecio(costo, numMargenes[3].Value, redondear);
                    prod.Precio5 = CalcularPrecio(costo, numMargenes[4].Value, redondear);
                    prod.Precio6 = CalcularPrecio(costo, numMargenes[5].Value, redondear);
                    prod.Precio7 = CalcularPrecio(costo, numMargenes[6].Value, redondear);
                    prod.Precio8 = CalcularPrecio(costo, numMargenes[7].Value, redondear);
                    prod.Precio9 = CalcularPrecio(costo, numMargenes[8].Value, redondear);
                    prod.Precio10 = CalcularPrecio(costo, numMargenes[9].Value, redondear);

                    prod.FchUpd = DateTime.Now;
                    prod.Sincro = 0;
                }

                db.SaveChanges();
                MessageBox.Show($"Se actualizaron con éxito los precios de {productosAfectados.Count} producto(s).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar los márgenes: {ex.Message}", "Error BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private decimal CalcularPrecio(decimal costoNeto, decimal margenPorc, bool redondear)
        {
            if (costoNeto <= 0 || margenPorc == 0) return 0;

            decimal netoConMargen = costoNeto * (1 + (margenPorc / 100m));
            decimal precioBruto = netoConMargen * 1.19m; // 19% IVA

            if (redondear)
            {
                return Math.Round(precioBruto / 10m, MidpointRounding.AwayFromZero) * 10m;
            }
            return Math.Round(precioBruto, 0);
        }
    }
}