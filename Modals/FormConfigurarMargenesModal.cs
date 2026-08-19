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
        // Memoria estática que persiste durante toda la ejecución
        private static decimal[] _ultimosMargenes = new decimal[] { 35m, 25m, 18m, 12m, 5m, 0m, 0m, 0m, 0m, 0m };
        private static bool _margenesInicializadosDesdeBD = false;

        private RadioButton rbTodo = null!;
        private RadioButton rbCategoria = null!;
        private ComboBox cbCategorias = null!;
        private CheckBox chkRedondear = null!;
        private NumericUpDown[] numMargenes = new NumericUpDown[10];
        private List<string> _categoriasDisponibles = new List<string>();

        public FormConfigurarMargenesModal(List<string>? categorias = null)
        {
            _categoriasDisponibles = categorias ?? new List<string>();
            
            // Cargar los márgenes reales que existen en la BD si es la primera apertura
            if (!_margenesInicializadosDesdeBD)
            {
                CargarMargenesRealesDesdeBD();
                _margenesInicializadosDesdeBD = true;
            }

            InitializeComponent();
            CargarValoresEnControles();
        }

        private void CargarMargenesRealesDesdeBD()
        {
            try
            {
                using var db = new AppDbContext();
                // Tomar el primer producto activo que tenga costo y precio calculados
                var prod = db.Productos.FirstOrDefault(p => p.Estado && p.PrecioCosto > 0 && p.PrecioUnitario > 0);
                if (prod != null)
                {
                    decimal costo = prod.PrecioCosto;
                    decimal[] precios = new decimal[]
                    {
                        prod.PrecioUnitario,
                        prod.Precio2,
                        prod.Precio3,
                        prod.Precio4,
                        prod.Precio5,
                        prod.Precio6,
                        prod.Precio7,
                        prod.Precio8,
                        prod.Precio9,
                        prod.Precio10
                    };

                    for (int i = 0; i < 10; i++)
                    {
                        if (precios[i] > 0)
                        {
                            // Fórmula Inversa: Margen % = (((PrecioBruto / 1.19) / CostoNeto) - 1) * 100
                            decimal neto = precios[i] / 1.19m;
                            decimal margenCalc = ((neto / costo) - 1m) * 100m;
                            _ultimosMargenes[i] = Math.Round(margenCalc, 1);
                        }
                    }
                }
            }
            catch { }
        }

        private void CargarValoresEnControles()
        {
            for (int i = 0; i < 10; i++)
            {
                if (numMargenes[i] != null)
                {
                    decimal val = Math.Max(numMargenes[i].Minimum, Math.Min(numMargenes[i].Maximum, _ultimosMargenes[i]));
                    numMargenes[i].Value = val;
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Estrategia Global de Listas de Precios (% Margen)";
            this.Size = new Size(560, 580);
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
                Text = "⚙️ Asignación de Márgenes a Listas de Precios",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };
            Label lblSub = new Label
            {
                Text = "Define los márgenes sobre el costo: (Costo Neto + % Margen) + 19% IVA",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(16, 34),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblSub });

            // Ámbito
            GroupBox gbAmbito = new GroupBox
            {
                Text = "Aplicar a:",
                Location = new Point(16, 68),
                Size = new Size(512, 65),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            rbTodo = new RadioButton { Text = "Todo el Catálogo", Location = new Point(15, 24), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5F) };
            rbCategoria = new RadioButton { Text = "Por Categoría:", Location = new Point(160, 24), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };

            cbCategorias = new ComboBox
            {
                Location = new Point(270, 22),
                Size = new Size(180, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Font = new Font("Segoe UI", 8.5F)
            };
            cbCategorias.Items.AddRange(_categoriasDisponibles.ToArray());
            if (cbCategorias.Items.Count > 0) cbCategorias.SelectedIndex = 0;

            rbCategoria.CheckedChanged += (s, e) => cbCategorias.Enabled = rbCategoria.Checked;
            gbAmbito.Controls.AddRange(new Control[] { rbTodo, rbCategoria, cbCategorias });

            // Panel de Listas
            Panel pnlListas = new Panel
            {
                Location = new Point(16, 142),
                Size = new Size(512, 310),
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
                "Lista 5 (Liquidación / Por Vencer)",
                "Lista 6", "Lista 7", "Lista 8", "Lista 9", "Lista 10"
            };

            int yPos = 8;
            for (int i = 0; i < 10; i++)
            {
                Label lbl = new Label
                {
                    Text = titulosListas[i],
                    Location = new Point(10, yPos + 4),
                    Size = new Size(300, 20),
                    Font = new Font("Segoe UI", 8.5F, i == 0 ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = i == 0 ? Color.FromArgb(16, 185, 129) : (i == 4 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(30, 41, 59))
                };

                numMargenes[i] = new NumericUpDown
                {
                    Location = new Point(320, yPos),
                    Size = new Size(90, 24),
                    DecimalPlaces = 1,
                    Minimum = -50,
                    Maximum = 500,
                    Value = _ultimosMargenes[i], // Toma el valor guardado
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    TextAlign = HorizontalAlignment.Right
                };

                Label lblPorc = new Label { Text = "%", Location = new Point(415, yPos + 4), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

                pnlListas.Controls.AddRange(new Control[] { lbl, numMargenes[i], lblPorc });
                yPos += 30;
            }

            // Opciones y Botones
            chkRedondear = new CheckBox
            {
                Text = "Redondear precios calculados a la decena más cercana (ej: $1.426 ➔ $1.430)",
                Location = new Point(16, 460),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            Button btnGuardar = new Button
            {
                Text = "💾 Recalcular y Guardar",
                Location = new Point(210, 495),
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
                Location = new Point(400, 495),
                Size = new Size(125, 36),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { pnlHeader, gbAmbito, pnlListas, chkRedondear, btnGuardar, btnCancelar });
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show("¿Está seguro de recalcular las listas de precios con los porcentajes definidos?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                // Guardar los nuevos valores en la memoria estática
                for (int i = 0; i < 10; i++)
                {
                    _ultimosMargenes[i] = numMargenes[i].Value;
                }

                using var db = new AppDbContext();
                IQueryable<Producto> query = db.Productos.Where(p => p.Estado);

                if (rbCategoria.Checked && cbCategorias.SelectedItem != null)
                {
                    string cat = cbCategorias.SelectedItem.ToString()!;
                    query = query.Where(p => p.Categoria == cat);
                }

                var productos = query.ToList();
                bool redondear = chkRedondear.Checked;

                foreach (var prod in productos)
                {
                    if (prod.PrecioCosto <= 0) continue;
                    decimal costo = prod.PrecioCosto;

                    prod.MargenGanancia = numMargenes[0].Value;
                    prod.PrecioUnitario = CalcularPrecio(costo, numMargenes[0].Value, redondear); // Lista 1
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
                MessageBox.Show($"Precios recalculados con éxito para {productos.Count} producto(s).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private decimal CalcularPrecio(decimal costo, decimal margen, bool redondear)
        {
            if (costo <= 0 || margen == 0) return 0;
            decimal neto = costo * (1 + (margen / 100m));
            decimal bruto = neto * 1.19m;
            return redondear ? Math.Round(bruto / 10m, MidpointRounding.AwayFromZero) * 10m : Math.Round(bruto, 0);
        }
    }
}