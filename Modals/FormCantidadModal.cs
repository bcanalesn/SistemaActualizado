using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormCantidadModal : Form
    {
        private int _cantidad = 1;
        private decimal _precioUnitario;
        private int _stockDisponible;
        private bool _esPrimerDigito = true;

        private Label lblCantidadDisplay = null!;
        private Label lblTotalPreview = null!;
        private Button btnAgregarAccion = null!;
        private Panel pnlKeypadContainer = null!;
        private Button btnToggleKeypad = null!;

        public int CantidadSeleccionada => _cantidad;

        public FormCantidadModal(string nombreProducto, decimal precioUnitario, int cantidadInicial, int stockDisponible = 9999, string? imagenPath = null)
        {
            _cantidad = cantidadInicial > 0 ? cantidadInicial : 1;
            _precioUnitario = precioUnitario;
            _stockDisponible = stockDisponible > 0 ? stockDisponible : 1;

            if (_cantidad > _stockDisponible) _cantidad = _stockDisponible;

            InitializeComponent(nombreProducto, imagenPath);
        }

        private void InitializeComponent(string nombreProducto, string? imagenPath)
        {
            this.SuspendLayout();
            this.Text = "Agregar a la Venta";
            this.Size = new Size(390, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.BackColor = Color.White;
            this.KeyPreview = true;

            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(345, 12),
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            Control ctrlIcono;
            if (!string.IsNullOrEmpty(imagenPath) && File.Exists(imagenPath))
            {
                PictureBox pb = new PictureBox
                {
                    Location = new Point(25, 18),
                    Size = new Size(45, 45),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                try { pb.Image = Image.FromFile(imagenPath); } catch { }
                ctrlIcono = pb;
            }
            else
            {
                ctrlIcono = new Label
                {
                    Text = "📦",
                    Font = new Font("Segoe UI", 22F),
                    Location = new Point(25, 18),
                    Size = new Size(45, 45)
                };
            }

            Label lblTitle = new Label { Text = nombreProducto, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(75, 18), AutoSize = true };
            Label lblSub = new Label { Text = $"Precio: ${_precioUnitario:N0}  |  Stock disp: {_stockDisponible} un.", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199), Location = new Point(75, 40), AutoSize = true };
            Label lblDivider = new Label { Height = 1, BackColor = Color.FromArgb(226, 232, 240), Location = new Point(15, 68), Width = 355 };
            Label lblCantHeader = new Label { Text = "Cantidad (Escriba con teclado o seleccione botón)", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(15, 76), Width = 355, TextAlign = ContentAlignment.MiddleCenter };

            Button btnMinus = new Button { Text = "─", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(35, 96), Size = new Size(44, 42), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat, TabStop = false, Cursor = Cursors.Hand };
            btnMinus.FlatAppearance.BorderSize = 0;
            btnMinus.Click += (s, e) => { if (_cantidad > 1) { _cantidad--; _esPrimerDigito = false; ActualizarValores(); } btnAgregarAccion.Focus(); };

            lblCantidadDisplay = new Label { Text = _cantidad.ToString(), Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 255), Location = new Point(88, 96), Size = new Size(205, 42), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(248, 250, 252) };
            lblCantidadDisplay.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, lblCantidadDisplay.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            Button btnPlus = new Button { Text = "┼", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(302, 96), Size = new Size(44, 42), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat, TabStop = false, Cursor = Cursors.Hand };
            btnPlus.FlatAppearance.BorderSize = 0;
            btnPlus.Click += (s, e) =>
            {
                if (_cantidad < _stockDisponible)
                {
                    _cantidad++;
                    _esPrimerDigito = false;
                    ActualizarValores();
                }
                else
                {
                    MessageBox.Show($"No puede superar el stock disponible ({_stockDisponible} un.).", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                btnAgregarAccion.Focus();
            };

            Label lblRapidasHeader = new Label { Text = "CANTIDADES RÁPIDAS", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(15, 148), Width = 355, TextAlign = ContentAlignment.MiddleCenter };

            FlowLayoutPanel flowRapidas = new FlowLayoutPanel { Location = new Point(25, 168), Size = new Size(335, 38), WrapContents = false };
            int[] cantidades = new int[] { 1, 2, 3, 5, 10 };

            foreach (int c in cantidades)
            {
                Button btnQ = new Button
                {
                    Text = c.ToString(),
                    Size = new Size(58, 32),
                    BackColor = (_cantidad == c) ? Color.FromArgb(0, 102, 255) : Color.FromArgb(248, 250, 252),
                    ForeColor = (_cantidad == c) ? Color.White : Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Margin = new Padding(3, 0, 3, 0),
                    TabStop = false,
                    Cursor = Cursors.Hand
                };
                btnQ.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                int val = c;
                btnQ.Click += (s, e) =>
                {
                    if (val > _stockDisponible)
                    {
                        MessageBox.Show($"La cantidad ({val}) supera el stock disponible ({_stockDisponible} un.).", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _cantidad = _stockDisponible;
                    }
                    else
                    {
                        _cantidad = val;
                    }

                    _esPrimerDigito = false;
                    foreach (Control ctrl in flowRapidas.Controls)
                    {
                        if (ctrl is Button b)
                        {
                            b.BackColor = Color.FromArgb(248, 250, 252);
                            b.ForeColor = Color.FromArgb(15, 23, 42);
                        }
                    }
                    btnQ.BackColor = Color.FromArgb(0, 102, 255);
                    btnQ.ForeColor = Color.White;
                    ActualizarValores();
                    btnAgregarAccion.Focus();
                };
                flowRapidas.Controls.Add(btnQ);
            }

            btnToggleKeypad = new Button
            {
                Text = "⌨️ Mostrar Teclado Numérico Táctil",
                Location = new Point(25, 218),
                Size = new Size(335, 30),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TabStop = false,
                Cursor = Cursors.Hand
            };
            btnToggleKeypad.FlatAppearance.BorderSize = 0;

            pnlKeypadContainer = new Panel
            {
                Location = new Point(25, 256),
                Size = new Size(335, 130),
                Visible = false,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            ConstruirTecladoNumericoUI(pnlKeypadContainer);

            Panel pnlTotalBox = new Panel { Name = "pnlTotalBox", Location = new Point(25, 258), Size = new Size(335, 40), BackColor = Color.FromArgb(248, 250, 252) };
            pnlTotalBox.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnlTotalBox.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid); };

            Label lblTotalCap = new Label { Text = "Total", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(12, 10), AutoSize = true };
            lblTotalPreview = new Label { Text = $"${_precioUnitario * _cantidad:N0}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(200, 8), Size = new Size(120, 24), TextAlign = ContentAlignment.MiddleRight };

            pnlTotalBox.Controls.Add(lblTotalCap);
            pnlTotalBox.Controls.Add(lblTotalPreview);

            btnAgregarAccion = new Button
            {
                Name = "btnAgregarAccion",
                Text = $"🛒 Agregar {_cantidad} a la venta",
                Location = new Point(25, 310),
                Size = new Size(335, 48),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnAgregarAccion.FlatAppearance.BorderSize = 0;
            btnAgregarAccion.Click += (s, e) => { ConfirmarYAgregar(); };

            Label lblHintEnter = new Label { Name = "lblHintEnter", Text = "Presione [Enter] para agregar a la venta", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(25, 368), Width = 335, TextAlign = ContentAlignment.MiddleCenter };

            btnToggleKeypad.Click += (s, e) =>
            {
                pnlKeypadContainer.Visible = !pnlKeypadContainer.Visible;
                btnToggleKeypad.Text = pnlKeypadContainer.Visible ? "⌨️ Ocultar Teclado Numérico" : "⌨️ Mostrar Teclado Numérico Táctil";

                if (pnlKeypadContainer.Visible)
                {
                    pnlTotalBox.Location = new Point(25, 396);
                    btnAgregarAccion.Location = new Point(25, 444);
                    lblHintEnter.Location = new Point(25, 498);
                    this.Size = new Size(390, 570);
                }
                else
                {
                    pnlTotalBox.Location = new Point(25, 258);
                    btnAgregarAccion.Location = new Point(25, 310);
                    lblHintEnter.Location = new Point(25, 368);
                    this.Size = new Size(390, 480);
                }
                btnAgregarAccion.Focus();
            };

            this.KeyDown += FormCantidadModal_KeyDown;
            this.Shown += (s, e) => btnAgregarAccion.Focus();

            this.Controls.Add(btnClose);
            this.Controls.Add(ctrlIcono);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSub);
            this.Controls.Add(lblDivider);
            this.Controls.Add(lblCantHeader);
            this.Controls.Add(btnMinus);
            this.Controls.Add(lblCantidadDisplay);
            this.Controls.Add(btnPlus);
            this.Controls.Add(lblRapidasHeader);
            this.Controls.Add(flowRapidas);
            this.Controls.Add(btnToggleKeypad);
            this.Controls.Add(pnlKeypadContainer);
            this.Controls.Add(pnlTotalBox);
            this.Controls.Add(btnAgregarAccion);
            this.Controls.Add(lblHintEnter);

            this.AcceptButton = btnAgregarAccion;
            this.ResumeLayout(false);
        }

        private void ConstruirTecladoNumericoUI(Panel pnlKeypad)
        {
            pnlKeypad.Controls.Clear();

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

            string[,] btns = new string[,]
            {
                { "7", "8", "9", "C" },
                { "4", "5", "6", "⌫" },
                { "1", "2", "3", "0" }
            };

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    string label = btns[r, c];
                    Button btn = new Button
                    {
                        Text = label,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(2),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TabStop = false,
                        BackColor = (label == "C" || label == "⌫") ? Color.FromArgb(254, 226, 226) : Color.White,
                        ForeColor = (label == "C" || label == "⌫") ? Color.FromArgb(185, 28, 28) : Color.FromArgb(15, 23, 42)
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                    if (char.IsDigit(label[0]))
                    {
                        int val = int.Parse(label);
                        btn.Click += (s, e) => { ProcesarEntradaDigito(val); ActualizarValores(); btnAgregarAccion.Focus(); };
                    }
                    else if (label == "C")
                    {
                        btn.Click += (s, e) => { _cantidad = 1; _esPrimerDigito = true; ActualizarValores(); btnAgregarAccion.Focus(); };
                    }
                    else if (label == "⌫")
                    {
                        btn.Click += (s, e) =>
                        {
                            _cantidad /= 10;
                            if (_cantidad <= 0) _cantidad = 1;
                            ActualizarValores();
                            btnAgregarAccion.Focus();
                        };
                    }

                    grid.Controls.Add(btn, c, r);
                }
            }

            pnlKeypad.Controls.Add(grid);
        }

        private void FormCantidadModal_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                int digito = e.KeyCode - Keys.D0;
                ProcesarEntradaDigito(digito);
                ActualizarValores();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
            {
                int digito = e.KeyCode - Keys.NumPad0;
                ProcesarEntradaDigito(digito);
                ActualizarValores();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Back)
            {
                _cantidad /= 10;
                if (_cantidad <= 0) _cantidad = 1;
                ActualizarValores();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ConfirmarYAgregar();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                e.SuppressKeyPress = true;
            }
        }

        private void ProcesarEntradaDigito(int digito)
        {
            int nuevaCant;
            if (_esPrimerDigito || _cantidad == 0)
            {
                nuevaCant = digito;
                _esPrimerDigito = false;
            }
            else
            {
                nuevaCant = (_cantidad * 10) + digito;
            }

            if (nuevaCant > _stockDisponible)
            {
                MessageBox.Show($"La cantidad ({nuevaCant}) supera el stock disponible ({_stockDisponible} un.).", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cantidad = _stockDisponible;
            }
            else
            {
                _cantidad = nuevaCant;
            }
        }

        private void ConfirmarYAgregar()
        {
            if (_cantidad <= 0) _cantidad = 1;
            if (_cantidad > _stockDisponible)
            {
                MessageBox.Show($"La cantidad se ajustó al stock máximo disponible ({_stockDisponible} un.).", "Ajuste de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cantidad = _stockDisponible;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ActualizarValores()
        {
            lblCantidadDisplay.Text = _cantidad.ToString();
            lblTotalPreview.Text = $"${_precioUnitario * _cantidad:N0}";
            btnAgregarAccion.Text = $"🛒 Agregar {_cantidad} a la venta";
        }
    }
}