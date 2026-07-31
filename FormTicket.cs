using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public partial class FormTicket : Form
    {
        private int _nroTicket;
        private DateTime _fecha;
        private List<DetalleCarrito> _items;
        private decimal _total;
        private decimal _pagoCon;
        private decimal _vuelto;
        private string _medioPago;

        private Button btnImprimir = null!;
        private Button btnCerrar = null!;
        private Panel pnlBoletaPapel = null!;

        public FormTicket(int nroTicket, DateTime fecha, List<DetalleCarrito> items, decimal total, decimal pagaCon, decimal vuelto, string medioPago)
        {
            _nroTicket = nroTicket;
            _fecha = fecha;
            _items = items;
            _total = total;
            _pagoCon = pagaCon;
            _vuelto = vuelto;
            _medioPago = medioPago;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = $"Comprobante de Venta - Ticket #{_nroTicket}";
            this.Size = new Size(420, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(15, 23, 42);

            // Contenedor del "Papel de Boleta"
            pnlBoletaPapel = new Panel
            {
                Size = new Size(360, 520),
                Location = new Point(22, 20),
                BackColor = Color.White,
                AutoScroll = true,
                Padding = new Padding(15)
            };

            // Encabezado del Comercio
            Label lblEncabezado = new Label
            {
                Text = "⚡ SISTEMA POS DEMO\nR.U.T.: 76.543.210-K\nAv. Principal #123 - Santiago\nTel: +56 9 1234 5678",
                Font = new Font("Courier New", 9.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70
            };

            Label lblLinea1 = CrearLineaDivisora();
            lblLinea1.Dock = DockStyle.Top;

            // Datos del Ticket
            Label lblInfoTicket = new Label
            {
                Text = $"TICKET N°: #{_nroTicket:D6}\nFECHA: {_fecha:dd/MM/yyyy HH:mm:ss}\nFORMA PAGO: {_medioPago.ToUpper()}",
                Font = new Font("Courier New", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 30, 30),
                Dock = DockStyle.Top,
                Height = 50
            };

            Label lblLinea2 = CrearLineaDivisora();
            lblLinea2.Dock = DockStyle.Top;

            // Encabezado de la lista de ítems
            Label lblHeaderTabla = new Label
            {
                Text = "CANT  DESCRIPCION        PRECIO    SUBTOTAL",
                Font = new Font("Courier New", 8.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Top,
                Height = 20
            };

            Label lblLinea3 = CrearLineaDivisora();
            lblLinea3.Dock = DockStyle.Top;

            Panel pnlItems = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(330, 0)
            };

            int topPos = 0;
            foreach (var item in _items)
            {
                Label lblItem = new Label
                {
                    Text = $"{item.Cantidad,2}x {TruncarNombre(item.Nombre, 16),-16} ${item.PrecioUnitario,7:N0} ${item.Subtotal,8:N0}",
                    Font = new Font("Courier New", 8.5F, FontStyle.Regular),
                    ForeColor = Color.Black,
                    Location = new Point(0, topPos),
                    AutoSize = true
                };
                pnlItems.Controls.Add(lblItem);
                topPos += 18;
            }

            Label lblLinea4 = CrearLineaDivisora();
            lblLinea4.Dock = DockStyle.Top;

            // Sección de Totales
            string textoTotales = $"TOTAL A PAGAR:        ${_total:N0}\n";
            if (_medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
            {
                textoTotales += $"PAGO CON:             ${_pagoCon:N0}\n";
                textoTotales += $"VUELTO:               ${_vuelto:N0}\n";
            }

            Label lblTotales = new Label
            {
                Text = textoTotales,
                Font = new Font("Courier New", 10F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Top,
                Height = 65,
                TextAlign = ContentAlignment.TopRight
            };

            Label lblPie = new Label
            {
                Text = "------------------------------------------\n¡GRACIAS POR SU COMPRA!\nConserve este ticket como comprobante",
                Font = new Font("Courier New", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50
            };

            pnlBoletaPapel.Controls.Add(lblPie);
            pnlBoletaPapel.Controls.Add(lblTotales);
            pnlBoletaPapel.Controls.Add(lblLinea4);
            pnlBoletaPapel.Controls.Add(pnlItems);
            pnlBoletaPapel.Controls.Add(lblLinea3);
            pnlBoletaPapel.Controls.Add(lblHeaderTabla);
            pnlBoletaPapel.Controls.Add(lblLinea2);
            pnlBoletaPapel.Controls.Add(lblInfoTicket);
            pnlBoletaPapel.Controls.Add(lblLinea1);
            pnlBoletaPapel.Controls.Add(lblEncabezado);

            btnImprimir = new Button
            {
                Text = "🖨️ IMPRIMIR TICKET",
                Location = new Point(22, 555),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            btnImprimir.Click += BtnImprimir_Click;

            btnCerrar = new Button
            {
                Text = "❌ Cerrar",
                Location = new Point(232, 555),
                Size = new Size(150, 45),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            this.Controls.Add(btnImprimir);
            this.Controls.Add(btnCerrar);
            this.Controls.Add(pnlBoletaPapel);

            this.ResumeLayout(false);
        }

        private void BtnImprimir_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += Pd_PrintPage;

                PrintDialog printDialog = new PrintDialog
                {
                    Document = pd
                };

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    pd.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar a la impresora: {ex.Message}", "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics!;
            Font fontTitulo = new Font("Courier New", 10, FontStyle.Bold);
            Font fontTexto = new Font("Courier New", 8.5F, FontStyle.Regular);
            Font fontBold = new Font("Courier New", 8.5F, FontStyle.Bold);

            float y = 20;
            float x = 10;

            g.DrawString("⚡ POS SYSTEM DEMO", fontTitulo, Brushes.Black, x, y); y += 18;
            g.DrawString($"Ticket N°: #{_nroTicket:D6}", fontBold, Brushes.Black, x, y); y += 16;
            g.DrawString($"Fecha: {_fecha:dd/MM/yyyy HH:mm}", fontTexto, Brushes.Black, x, y); y += 16;
            g.DrawString("------------------------------------", fontTexto, Brushes.Black, x, y); y += 16;

            foreach (var item in _items)
            {
                g.DrawString($"{item.Cantidad}x {TruncarNombre(item.Nombre, 15)} - ${item.Subtotal:N0}", fontTexto, Brushes.Black, x, y);
                y += 16;
            }

            g.DrawString("------------------------------------", fontTexto, Brushes.Black, x, y); y += 16;
            g.DrawString($"TOTAL: ${_total:N0}", fontTitulo, Brushes.Black, x, y); y += 18;
            g.DrawString($"Medio Pago: {_medioPago}", fontTexto, Brushes.Black, x, y); y += 20;
            g.DrawString("¡Gracias por su compra!", fontBold, Brushes.Black, x, y);
        }

        private Label CrearLineaDivisora()
        {
            return new Label
            {
                Text = "------------------------------------------",
                Font = new Font("Courier New", 8.5F),
                ForeColor = Color.Gray,
                Height = 15,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private string TruncarNombre(string nombre, int largo)
        {
            if (string.IsNullOrEmpty(nombre)) return string.Empty;
            return nombre.Length <= largo ? nombre : nombre.Substring(0, largo - 1) + ".";
        }
    }
}