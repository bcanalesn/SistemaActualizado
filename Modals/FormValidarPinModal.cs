using System;
using System.Drawing;
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
            this.Size = new Size(320, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.KeyPreview = true;

            Label lblTitulo = new Label
            {
                Text = "🔒 Ingrese PIN de 4 Dígitos",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(10, 16),
                Size = new Size(285, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblVendedor = new Label
            {
                Text = $"Vendedor: {_nombreVendedor}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 42),
                Size = new Size(285, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblPuntosDisplay = new Label
            {
                Text = "○ ○ ○ ○",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Location = new Point(10, 68),
                Size = new Size(285, 45),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            lblPuntosDisplay.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, lblPuntosDisplay.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            TableLayoutPanel grid = new TableLayoutPanel
            {
                Location = new Point(20, 126),
                Size = new Size(265, 240),
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
                    Button btn = new Button
                    {
                        Text = texto,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TabStop = false,
                        BackColor = (texto == "C" || texto == "⌫") ? Color.FromArgb(241, 245, 249) : Color.White,
                        ForeColor = (texto == "C" || texto == "⌫") ? Color.FromArgb(100, 116, 139) : Color.FromArgb(15, 23, 42)
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

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

            this.Controls.AddRange(new Control[] { lblTitulo, lblVendedor, lblPuntosDisplay, grid });
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
            lblPuntosDisplay.Text = string.Join(" ", chars);
        }

        private void ValidarPinConBD()
        {
            using var db = new AppDbContext();
            string nombreLimpio = _nombreVendedor.Trim().ToLower();

            var usuario = db.Usuarios.AsNoTracking().FirstOrDefault(u => 
                u.Estado && (u.NombreCompleto.ToLower() == nombreLimpio || u.NombreUsuario.ToLower() == nombreLimpio));

            // Si el vendedor tiene PIN asignado, debe coincidir exactamente
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
                // Si por migración previa no tuviera PIN configurado, permite el acceso
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