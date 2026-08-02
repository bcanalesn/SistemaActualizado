using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public partial class FormTicket : Form
    {
        private Venta _venta;
        private List<DetalleCarrito> _items;
        private decimal _pagoCon;
        private decimal _vuelto;

        private Button btnImprimir = null!;
        private Button btnCerrar = null!;
        private Panel pnlBoletaPapel = null!;

        public FormTicket(Venta venta, List<DetalleCarrito> items, decimal pagaCon, decimal vuelto)
        {
            _venta = venta;
            _items = items;
            _pagoCon = pagaCon;
            _vuelto = vuelto;

            InitializeComponent();
        }

        // Constructor sobrecargado para compatibilidad
        public FormTicket(int nroTicket, DateTime fecha, List<DetalleCarrito> items, decimal total, decimal pagaCon, decimal vuelto, string medioPago)
        {
            _venta = new Venta
            {
                FolioDTE = nroTicket,
                Fecha = fecha,
                Total = total,
                Neto = Math.Round(total / 1.19m, 0),
                IVA = total - Math.Round(total / 1.19m, 0),
                MedioPago = medioPago,
                TipoDocumento = medioPago.Contains("Factura") ? "Factura Electrónica" : "Boleta Electrónica"
            };
            _items = items;
            _pagoCon = pagaCon;
            _vuelto = vuelto;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            bool esFactura = _venta.TipoDocumento.Equals("Factura Electrónica", StringComparison.OrdinalIgnoreCase) ||
                             _venta.TipoDocumento.Equals("Guía de Despacho", StringComparison.OrdinalIgnoreCase);

            this.Text = esFactura ? $"Documento Tributario - {_venta.TipoDocumento} N° {_venta.FolioDTE}" : $"Comprobante de Venta - Boleta Electrónica N° {_venta.FolioDTE}";
            this.Size = esFactura ? new Size(480, 750) : new Size(420, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(15, 23, 42);

            // Contenedor principal del documento impreso
            pnlBoletaPapel = new Panel
            {
                Size = esFactura ? new Size(420, 590) : new Size(360, 520),
                Location = new Point(esFactura ? 22 : 22, 20),
                BackColor = Color.White,
                AutoScroll = true,
                Padding = new Padding(15)
            };

            if (esFactura)
            {
                ConstruirFormatoFactura();
            }
            else
            {
                ConstruirFormatoBoleta();
            }

            btnImprimir = new Button
            {
                Text = "🖨️ IMPRIMIR " + (esFactura ? "FACTURA" : "TICKET"),
                Location = new Point(22, esFactura ? 625 : 555),
                Size = new Size(220, 45),
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
                Location = new Point(252, esFactura ? 625 : 555),
                Size = new Size(esFactura ? 190 : 130, 45),
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

        private void ConstruirFormatoFactura()
        {
            // 1. Recuadro Tributario SII (Borde Rojo / Azul)
            Panel pnlCuadroSII = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(254, 242, 242),
                Margin = new Padding(0, 0, 0, 10)
            };

            Label lblRutsii = new Label
            {
                Text = $"R.U.T.: 76.543.210-K\n{_venta.TipoDocumento.ToUpper()}\nN° {_venta.FolioDTE:D6}\nS.I.I. - SANTIAGO CENTRO",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCuadroSII.Controls.Add(lblRutsii);

            // 2. Encabezado Emisor
            Label lblEmisor = new Label
            {
                Text = "⚡ SISTEMA POS DEMO S.A.\nGiro: Comercializadora de Abarrotes e Insumos\nAv. Principal #123 - Santiago | Tel: +56 9 1234 5678",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 55,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblLinea1 = CrearLineaDivisora();
            lblLinea1.Dock = DockStyle.Top;

            // 3. Cuadro Receptor / Datos del Cliente
            Panel pnlCliente = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(8),
                Margin = new Padding(0, 5, 0, 5)
            };

            Label lblDatosCliente = new Label
            {
                Text = $"SEÑOR(ES): {_venta.RazonSocial.ToUpper()}\n" +
                       $"R.U.T. RECEPTOR: {_venta.RutCliente}\n" +
                       $"GIRO: {(string.IsNullOrWhiteSpace(_venta.Giro) ? "SIN INFORMACIÓN" : _venta.Giro.ToUpper())}\n" +
                       $"FECHA EMISIÓN: {_venta.Fecha:dd/MM/yyyy HH:mm} | PAGO: {_venta.MedioPago.ToUpper()}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Fill
            };
            pnlCliente.Controls.Add(lblDatosCliente);

            Label lblLinea2 = CrearLineaDivisora();
            lblLinea2.Dock = DockStyle.Top;

            // 4. Encabezado de la Tabla
            Label lblHeaderTabla = new Label
            {
                Text = "CANT  DESCRIPCION            P.NETO    SUBTOTAL",
                Font = new Font("Courier New", 8.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Top,
                Height = 20
            };

            Label lblLinea3 = CrearLineaDivisora();
            lblLinea3.Dock = DockStyle.Top;

            // 5. Ítems Comprados
            Panel pnlItems = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(390, 0)
            };

            int topPos = 0;
            foreach (var item in _items)
            {
                decimal precioNetoItem = Math.Round(item.PrecioUnitario / 1.19m, 0);
                decimal subtotalNetoItem = precioNetoItem * item.Cantidad;

                Label lblItem = new Label
                {
                    Text = $"{item.Cantidad,2}x {TruncarNombre(item.Nombre, 20),-20} ${precioNetoItem,7:N0} ${subtotalNetoItem,8:N0}",
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

            // 6. Totales Tributarios (Neto + IVA + Total)
            Panel pnlTotalesDTE = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(0, 5, 0, 5)
            };

            Label lblTotales = new Label
            {
                Text = $"MONTO NETO:      ${_venta.Neto:N0}\n" +
                       $"I.V.A. (19%):    ${_venta.IVA:N0}\n" +
                       $"TOTAL FACTURA:   ${_venta.Total:N0}",
                Font = new Font("Courier New", 10F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Right,
                Width = 260,
                TextAlign = ContentAlignment.TopRight
            };
            pnlTotalesDTE.Controls.Add(lblTotales);

            Label lblPieDTE = new Label
            {
                Text = "==========================================\nTimbre Electrónico SII - Res. N° 80 de 2014\nVerifique Documento en www.sii.cl",
                Font = new Font("Courier New", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45
            };

            pnlBoletaPapel.Controls.Add(lblPieDTE);
            pnlBoletaPapel.Controls.Add(pnlTotalesDTE);
            pnlBoletaPapel.Controls.Add(lblLinea4);
            pnlBoletaPapel.Controls.Add(pnlItems);
            pnlBoletaPapel.Controls.Add(lblLinea3);
            pnlBoletaPapel.Controls.Add(lblHeaderTabla);
            pnlBoletaPapel.Controls.Add(lblLinea2);
            pnlBoletaPapel.Controls.Add(pnlCliente);
            pnlBoletaPapel.Controls.Add(lblLinea1);
            pnlBoletaPapel.Controls.Add(lblEmisor);
            pnlBoletaPapel.Controls.Add(pnlCuadroSII);
        }

        private void ConstruirFormatoBoleta()
        {
            Label lblEncabezado = new Label
            {
                Text = $"⚡ SISTEMA POS DEMO\nR.U.T.: 76.543.210-K\nBOLETA ELECTRÓNICA N° {_venta.FolioDTE:D6}\nS.I.I. - SANTIAGO CENTRO",
                Font = new Font("Courier New", 9.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 75
            };

            Label lblLinea1 = CrearLineaDivisora();
            lblLinea1.Dock = DockStyle.Top;

            Label lblInfoTicket = new Label
            {
                Text = $"FECHA: {_venta.Fecha:dd/MM/yyyy HH:mm:ss}\nFORMA PAGO: {_venta.MedioPago.ToUpper()}",
                Font = new Font("Courier New", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 30, 30),
                Dock = DockStyle.Top,
                Height = 35
            };

            Label lblLinea2 = CrearLineaDivisora();
            lblLinea2.Dock = DockStyle.Top;

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

            string textoTotales = $"TOTAL A PAGAR:        ${_venta.Total:N0}\n" +
                                  $"(Monto Neto: ${_venta.Neto:N0} | IVA 19%: ${_venta.IVA:N0})\n";
            if (_venta.MedioPago.StartsWith("Efectivo", StringComparison.OrdinalIgnoreCase))
            {
                textoTotales += $"PAGO CON:             ${_pagoCon:N0}\n" +
                                $"VUELTO:               ${_vuelto:N0}\n";
            }

            Label lblTotales = new Label
            {
                Text = textoTotales,
                Font = new Font("Courier New", 9.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Top,
                Height = 75,
                TextAlign = ContentAlignment.TopRight
            };

            Label lblPie = new Label
            {
                Text = "------------------------------------------\n¡GRACIAS POR SU COMPRA!\nEl IVA de esta boleta ha sido retenido por el SII",
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
        }

        private void BtnImprimir_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += Pd_PrintPage;

                PrintDialog printDialog = new PrintDialog { Document = pd };
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

            g.DrawString($"⚡ {_venta.TipoDocumento.ToUpper()}", fontTitulo, Brushes.Black, x, y); y += 18;
            g.DrawString($"N° Folio: {_venta.FolioDTE:D6}", fontBold, Brushes.Black, x, y); y += 16;
            g.DrawString($"Fecha: {_venta.Fecha:dd/MM/yyyy HH:mm}", fontTexto, Brushes.Black, x, y); y += 16;

            if (!string.IsNullOrWhiteSpace(_venta.RutCliente))
            {
                g.DrawString($"RUT Cliente: {_venta.RutCliente}", fontTexto, Brushes.Black, x, y); y += 16;
                g.DrawString($"Razón Social: {_venta.RazonSocial}", fontTexto, Brushes.Black, x, y); y += 16;
            }

            g.DrawString("------------------------------------", fontTexto, Brushes.Black, x, y); y += 16;

            foreach (var item in _items)
            {
                g.DrawString($"{item.Cantidad}x {TruncarNombre(item.Nombre, 15)} - ${item.Subtotal:N0}", fontTexto, Brushes.Black, x, y);
                y += 16;
            }

            g.DrawString("------------------------------------", fontTexto, Brushes.Black, x, y); y += 16;
            g.DrawString($"NETO:  ${_venta.Neto:N0}", fontTexto, Brushes.Black, x, y); y += 16;
            g.DrawString($"IVA:   ${_venta.IVA:N0}", fontTexto, Brushes.Black, x, y); y += 16;
            g.DrawString($"TOTAL: ${_venta.Total:N0}", fontTitulo, Brushes.Black, x, y); y += 20;
            g.DrawString("Verifique documento en SII", fontBold, Brushes.Black, x, y);
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