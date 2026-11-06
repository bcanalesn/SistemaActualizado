using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormConfiguracionDteModal : Form
    {
        private readonly ConfiguracionDteService _configService = new ConfiguracionDteService();
        private readonly Usuario _usuarioActual;
        private int _maxBoletas = 10;
        private Label lblCantidadDisplay = null!;
        private Button btnMenos = null!;
        private Button btnMas = null!;
        private Button btnGuardar = null!;
        private Button btnCancelar = null!;

        public FormConfiguracionDteModal(Usuario? usuario = null)
        {
            _usuarioActual = usuario ?? new Usuario { NombreUsuario = "admin" };
            InitializeComponent();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.Text = "⚙️ Configuración de Emisión DTE";
            this.ClientSize = new Size(460, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            const int wCard = 420;

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                BackColor = Color.White
            };

            // 1. TÍTULO Y SUBTÍTULO
            Label lblTitulo = new Label
            {
                Text = "Configuración de Notas de Crédito",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 28
            };

            Label lblSub = new Label
            {
                Text = "Establezca las reglas para la emisión consolidada de boletas",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 22
            };

            // 2. TARJETA SELECTOR TÁCTIL (Touch Stepper)
            Panel pnlCardBoletas = CrearTarjetaRedondeada(wCard, 175);
            pnlCardBoletas.Dock = DockStyle.Top;
            pnlCardBoletas.Margin = new Padding(0, 10, 0, 12);
            pnlCardBoletas.Padding = new Padding(14);

            Label lblTitBoletas = new Label
            {
                Text = "MÁXIMO DE BOLETAS POR NOTA DE CRÉDITO",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Dock = DockStyle.Top,
                Height = 22
            };

            Label lblExplicacion = new Label
            {
                Text = "Cantidad máxima de boletas electrónicas (DTE 39) que se pueden agrupar en una sola Nota de Crédito resumen:",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 34
            };

            // Contenedor Stepper [ - ]  [ VALOR ]  [ + ]
            Panel pnlStepper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            btnMenos = new Button
            {
                Text = "➖",
                Size = new Size(65, 50),
                Location = new Point(20, 6),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMenos.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMenos.Click += (s, e) => CambiarCantidad(-1);

            lblCantidadDisplay = new Label
            {
                Text = "10 boletas",
                Size = new Size(220, 50),
                Location = new Point(95, 6),
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblCantidadDisplay.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, lblCantidadDisplay.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            btnMas = new Button
            {
                Text = "➕",
                Size = new Size(65, 50),
                Location = new Point(325, 6),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMas.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMas.Click += (s, e) => CambiarCantidad(1);

            pnlStepper.Controls.AddRange(new Control[] { btnMenos, lblCantidadDisplay, btnMas });

            pnlCardBoletas.Controls.Add(pnlStepper);
            pnlCardBoletas.Controls.Add(lblExplicacion);
            pnlCardBoletas.Controls.Add(lblTitBoletas);

            // 3. TARJETA INFORMATIVA: REGLA DE FACTURAS (1 A 1)
            Panel pnlCardFacturaInfo = CrearTarjetaRedondeada(wCard, 90);
            pnlCardFacturaInfo.Dock = DockStyle.Top;
            pnlCardFacturaInfo.Margin = new Padding(0, 10, 0, 14);
            pnlCardFacturaInfo.Padding = new Padding(12);

            Label lblInfoFacturaTit = new Label
            {
                Text = "ℹ️ POLÍTICA DE FACTURAS (DTE 33)",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };

            Label lblInfoFacturaTxt = new Label
            {
                Text = "Por normativa del SII, las Notas de Crédito y Notas de Débito sobre Facturas Electrónicas se emiten estrictamente 1 a 1 para preservar la referencia directa de RUT y folio.",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill
            };

            pnlCardFacturaInfo.Controls.Add(lblInfoFacturaTxt);
            pnlCardFacturaInfo.Controls.Add(lblInfoFacturaTit);

            // 4. BOTONES TÁCTILES INFERIORES
            Panel pnlBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnGuardar = new Button
            {
                Text = "💾 Guardar Configuración",
                Dock = DockStyle.Top,
                Height = 48, // 👈 Altura ergonómica para toque táctil
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 8 };

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            pnlBotones.Controls.Add(btnCancelar);
            pnlBotones.Controls.Add(pnlSpacer);
            pnlBotones.Controls.Add(btnGuardar);

            pnlMain.Controls.Add(pnlBotones);
            pnlMain.Controls.Add(pnlCardFacturaInfo);
            pnlMain.Controls.Add(pnlCardBoletas);
            pnlMain.Controls.Add(lblSub);
            pnlMain.Controls.Add(lblTitulo);

            this.Controls.Add(pnlMain);
            this.CancelButton = btnCancelar;
        }

        private void CargarDatos()
        {
            var config = _configService.ObtenerConfiguracion();
            _maxBoletas = config.MaxBoletasPorNC > 0 ? config.MaxBoletasPorNC : 10;
            ActualizarDisplay();
        }

        private void CambiarCantidad(int delta)
        {
            int nueva = _maxBoletas + delta;
            if (nueva >= 1 && nueva <= 50)
            {
                _maxBoletas = nueva;
                ActualizarDisplay();
            }
        }

        private void ActualizarDisplay()
        {
            lblCantidadDisplay.Text = $"{_maxBoletas} boletas";
            btnMenos.Enabled = _maxBoletas > 1;
            btnMas.Enabled = _maxBoletas < 50;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                string usuario = _usuarioActual?.NombreUsuario ?? "admin";
                _configService.GuardarConfiguracion(_maxBoletas, true, usuario);

                MessageBox.Show(
                    $"Configuración guardada correctamente.\n\n• Máximo de boletas por NC: {_maxBoletas}",
                    "Configuración DTE Actualizada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CrearTarjetaRedondeada(int ancho, int alto)
        {
            Panel pnl = new Panel { Size = new Size(ancho, alto), BackColor = Color.White };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath path = new GraphicsPath();
                int d = 16;
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1.2f);
                e.Graphics.DrawPath(pen, path);
            };
            return pnl;
        }
    }
}