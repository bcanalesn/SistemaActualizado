using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormValidarPinModal : Form
    {
        private readonly string _nombreVendedor;
        private string _pinIngresado = "";
        private Label lblPuntosDisplay = null!;
        public bool PinValidado { get; private set; } = false;

        public FormValidarPinModal(string nombreVendedor)
        {
            _nombreVendedor = nombreVendedor;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Autenticación de Vendedor";
            // 👈 Tamaño ampliado para formato Touch (430 x 570)
            this.ClientSize = new Size(430, 570);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.KeyPreview = true;

            const int formW = 430;

            Label lblTitulo = new Label
            {
                Text = "🔒 Ingrese PIN de 4 Dígitos",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(15, 18),
                Size = new Size(formW - 30, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblVendedor = new Label
            {
                Text = $"Vendedor: {_nombreVendedor}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(15, 48),
                Size = new Size(formW - 30, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Display con altura táctil y círculos grandes
            lblPuntosDisplay = new Label
            {
                Text = "○  ○  ○  ○",
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Location = new Point(24, 76),
                Size = new Size(formW - 48, 56),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            lblPuntosDisplay.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, lblPuntosDisplay.Width - 1, lblPuntosDisplay.Height - 1);
                using Pen pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
                e.Graphics.DrawRectangle(pen, r);
            };

            // Botonera táctil: 382 px de ancho x 360 px de alto (cada botón queda aprox. de 120 x 82 px)
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Location = new Point(24, 148),
                Size = new Size(formW - 48, 360),
                ColumnCount = 3,
                RowCount = 4
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            for (int i = 0; i < 4; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            string[,] botones = {
                { "1", "2", "3" },
                { "4", "5", "6" },
                { "7", "8", "9" },
                { "C", "0", "⌫" }
            };

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string texto = botones[r, c];
                    bool esAccion = (texto == "C" || texto == "⌫");

                    Button btn = new Button
                    {
                        Text = texto,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(5), // 👈 Margen entre botones para evitar pulsar números contiguos
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", esAccion ? 16F : 19F, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TabStop = false,
                        BackColor = esAccion 
                            ? (texto == "C" ? Color.FromArgb(254, 242, 242) : Color.FromArgb(241, 245, 249)) 
                            : Color.White,
                        ForeColor = esAccion 
                            ? (texto == "C" ? Color.FromArgb(239, 68, 68) : Color.FromArgb(71, 85, 105)) 
                            : Color.FromArgb(15, 23, 42)
                    };

                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = esAccion && texto == "C" 
                        ? Color.FromArgb(254, 202, 202) 
                        : Color.FromArgb(203, 213, 225);

                    if (char.IsDigit(texto[0]))
                    {
                        btn.Click += (s, e) => AgregarDigito(texto);
                    }
                    else if (texto == "C")
                    {
                        btn.Click += (s, e) => { _pinIngresado = ""; ActualizarDisplay(); };
                    }
                    else if (texto == "⌫")
                    {
                        btn.Click += (s, e) => BorrarUltimo();
                    }

                    grid.Controls.Add(btn, c, r);
                }
            }

            // Botón inferior para cancelar con el dedo
            Button btnCancelarModal = new Button
            {
                Text = "✕ Cancelar",
                Location = new Point(24, 518),
                Size = new Size(formW - 48, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelarModal.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelarModal.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                {
                    AgregarDigito((e.KeyCode - Keys.D0).ToString());
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
                {
                    AgregarDigito((e.KeyCode - Keys.NumPad0).ToString());
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    BorrarUltimo();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            this.Controls.AddRange(new Control[] { lblTitulo, lblVendedor, lblPuntosDisplay, grid, btnCancelarModal });
            this.CancelButton = btnCancelarModal;
        }

        private void AgregarDigito(string d)
        {
            if (_pinIngresado.Length < 4)
            {
                _pinIngresado += d;
                ActualizarDisplay();

                if (_pinIngresado.Length == 4)
                {
                    ValidarPinConBD();
                }
            }
        }

        private void BorrarUltimo()
        {
            if (_pinIngresado.Length > 0)
            {
                _pinIngresado = _pinIngresado.Substring(0, _pinIngresado.Length - 1);
                ActualizarDisplay();
            }
        }

        private void ActualizarDisplay()
        {
            char[] chars = new char[] { '○', '○', '○', '○' };
            for (int i = 0; i < _pinIngresado.Length; i++)
            {
                chars[i] = '●';
            }
            lblPuntosDisplay.Text = string.Join("  ", chars);
        }

        private void ValidarPinConBD()
        {
            using var db = new AppDbContext();
            string nombreLimpio = _nombreVendedor.Trim().ToLower();

            var usuario = db.Usuarios.AsNoTracking().FirstOrDefault(u => 
                u.Estado && (u.NombreCompleto.ToLower() == nombreLimpio || u.NombreUsuario.ToLower() == nombreLimpio));

            if (usuario != null && !string.IsNullOrEmpty(usuario.Pin))
            {
                if (usuario.Pin == _pinIngresado)
                {
                    PinValidado = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
            }
            else
            {
                PinValidado = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            MessageBox.Show("El PIN ingresado es incorrecto.", "Acceso Denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _pinIngresado = "";
            ActualizarDisplay();
        }
    }
}