using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormTicketModal : Form
    {
        private readonly TicketPrintService _printService = new TicketPrintService();

        private TVE2607 _venta;
        private List<DetalleCarrito> _items;
        private decimal _pagoCon;
        private decimal _vuelto;

        private Button btnImprimir = null!;
        private Button btnCerrar = null!;
        private Panel pnlBoletaPapel = null!;

        // Constructor 1: Recibe objeto TVE2607 directamente
        public FormTicketModal(TVE2607 venta, List<DetalleCarrito> items, decimal pagaCon, decimal vuelto)
        {
            _venta = venta ?? new TVE2607();
            _items = items ?? new List<DetalleCarrito>();
            _pagoCon = pagaCon;
            _vuelto = vuelto;

            InitializeComponent();
        }

        // Constructor 2: Recibe parámetros individuales (Usado desde FormHistorialVentas)
        public FormTicketModal(int nroTicket, DateTime fecha, List<DetalleCarrito> items, decimal total, decimal pagaCon, decimal vuelto, string medioPago)
        {
            _venta = new TVE2607
            {
                nroDTE = nroTicket,
                FecDoc = fecha,
                Total = total,
                Neto = Math.Round(total / 1.19m, 0),
                IvA = total - Math.Round(total / 1.19m, 0),
                UserDTE = medioPago,
                Documento = medioPago.Contains("Factura") ? "Factura Electrónica" : "Boleta Electrónica"
            };
            _items = items ?? new List<DetalleCarrito>();
            _pagoCon = pagaCon;
            _vuelto = vuelto;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = $"{_venta.Documento ?? "Documento"} - N° {_venta.nroDTE:D6}";
            this.Size = new Size(420, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(241, 245, 249);

            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };

            btnImprimir = new Button
            {
                Text = "🖨️ Imprimir Ticket",
                Size = new Size(150, 35),
                Location = new Point(12, 10),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            btnImprimir.Click += (s, e) => _printService.ImprimirTicket(pnlBoletaPapel);

            btnCerrar = new Button
            {
                Text = "❌ Cerrar",
                Size = new Size(100, 35),
                Location = new Point(290, 10),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlTopBar.Controls.Add(btnImprimir);
            pnlTopBar.Controls.Add(btnCerrar);

            Panel pnlScrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            pnlBoletaPapel = new Panel
            {
                Size = new Size(360, 850),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 20)
            };
            pnlBoletaPapel.Paint += (s, e) => _printService.DibujarDocumentoTributario(e.Graphics, _venta, _items, _pagoCon, _vuelto, pnlBoletaPapel.Width);

            pnlScrollContainer.Controls.Add(pnlBoletaPapel);

            this.Controls.Add(pnlScrollContainer);
            this.Controls.Add(pnlTopBar);

            this.ResumeLayout(false);
        }
    }
}