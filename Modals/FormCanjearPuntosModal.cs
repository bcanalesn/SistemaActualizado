using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormCanjearPuntosModal : Form
    {
        private readonly int _puntosDisponibles;
        private readonly decimal _totalVenta;

        private NumericUpDown numPuntos = null!;
        private Label lblDescuentoPesos = null!;
        private Label lblNuevoTotal = null!;
        private Button btnAplicar = null!;
        private Button btnQuitarCanje = null!;

        public int PuntosSeleccionados { get; private set; } = 0;
        public decimal DescuentoEnPesos => PuntosSeleccionados * FidelizacionConfig.ValorPesoPorPunto;

        public FormCanjearPuntosModal(int puntosDisponibles, decimal totalVenta, int puntosYaAplicados = 0)
        {
            _puntosDisponibles = Math.Max(0, puntosDisponibles);
            _totalVenta = Math.Max(0m, totalVenta);
            PuntosSeleccionados = Math.Min(puntosYaAplicados, _puntosDisponibles);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🎁 Canjear Puntos de Fidelización";
            this.ClientSize = new Size(380, 310);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // Máximo de puntos que puede usar sin exceder el total de la venta ni el saldo del cliente
            int maxPuntosPorTotal = (int)Math.Floor(_totalVenta / FidelizacionConfig.ValorPesoPorPunto);
            int maximoPermitido = Math.Min(_puntosDisponibles, maxPuntosPorTotal);

            Label lblTit = new Label
            {
                Text = "🎁 CANJE DE BENEFICIO",
                Location = new Point(20, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 58, 237)
            };

            decimal equivPesos = _puntosDisponibles * FidelizacionConfig.ValorPesoPorPunto;
            Label lblDisp = new Label
            {
                Text = $"Puntos disponibles: {_puntosDisponibles:N0} pts  (Equiv. {MonedaHelper.Formatear(equivPesos, conSigno: true)})",
                Location = new Point(20, 38),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            Label lblPrompt = new Label
            {
                Text = "Indique cuántos puntos desea utilizar:",
                Location = new Point(20, 68),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            numPuntos = new NumericUpDown
            {
                Location = new Point(20, 90),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Minimum = 0,
                Maximum = Math.Max(0, maximoPermitido),
                Value = Math.Min(PuntosSeleccionados, Math.Max(0, maximoPermitido)),
                TextAlign = HorizontalAlignment.Center
            };

            Button btnMax = new Button
            {
                Text = "Usar Todo",
                Location = new Point(248, 89),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(245, 243, 255),
                ForeColor = Color.FromArgb(124, 58, 237),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMax.FlatAppearance.BorderColor = Color.FromArgb(221, 214, 254);
            btnMax.Click += (s, e) => numPuntos.Value = maximoPermitido;

            Panel pnlInfoDescuento = new Panel
            {
                Location = new Point(20, 130),
                Size = new Size(338, 65),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblDescuentoPesos = new Label
            {
                Text = "Descuento a aplicar: -$ 0",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74)
            };

            lblNuevoTotal = new Label
            {
                Text = $"Nuevo Total a Pagar: {MonedaHelper.Formatear(_totalVenta, conSigno: true)}",
                Location = new Point(10, 34),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            pnlInfoDescuento.Controls.AddRange(new Control[] { lblDescuentoPesos, lblNuevoTotal });

            Action recalcular = () =>
            {
                int pts = (int)numPuntos.Value;
                decimal desc = pts * FidelizacionConfig.ValorPesoPorPunto;
                decimal totFinal = Math.Max(0, _totalVenta - desc);

                lblDescuentoPesos.Text = $"Descuento a aplicar: -{MonedaHelper.Formatear(desc, conSigno: true)}";
                lblNuevoTotal.Text = $"Nuevo Total a Pagar: {MonedaHelper.Formatear(totFinal, conSigno: true)}";
            };

            numPuntos.ValueChanged += (s, e) => recalcular();
            recalcular();

            btnAplicar = new Button
            {
                Text = "✔ Confirmar Descuento",
                Location = new Point(20, 208),
                Size = new Size(210, 42),
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAplicar.FlatAppearance.BorderSize = 0;
            btnAplicar.Click += (s, e) =>
            {
                int seleccion = (int)numPuntos.Value;
                if (seleccion > _puntosDisponibles)
                {
                    MessageBox.Show($"No puede utilizar más puntos de los que el cliente posee ({_puntosDisponibles} pts).", "Saldo Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numPuntos.Value = _puntosDisponibles;
                    return;
                }

                PuntosSeleccionados = seleccion;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnQuitarCanje = new Button
            {
                Text = "✕ Quitar",
                Location = new Point(238, 208),
                Size = new Size(120, 42),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuitarCanje.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnQuitarCanje.Click += (s, e) =>
            {
                PuntosSeleccionados = 0;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.AcceptButton = btnAplicar;
            this.CancelButton = btnQuitarCanje;

            this.Controls.AddRange(new Control[]
            {
                lblTit, lblDisp, lblPrompt, numPuntos, btnMax,
                pnlInfoDescuento, btnAplicar, btnQuitarCanje
            });
        }
    }
}