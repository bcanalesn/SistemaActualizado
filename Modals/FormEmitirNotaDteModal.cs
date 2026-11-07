using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormEmitirNotaDteModal : Form
    {
        private readonly List<TVE2607> _documentosSeleccionados;
        private readonly Usuario _usuarioActual;
        private readonly ConfiguracionDteService _configService = new ConfiguracionDteService();

        private bool _esNotaCredito = true;
        private int _codigoCausaSeleccionado = 1;

        private Button btnTipoNC = null!;
        private Button btnTipoND = null!;

        private Button btnCausa1 = null!;
        private Button btnCausa2 = null!;
        private Button btnCausa3 = null!;

        private TextBox txtMotivo = null!;
        private Panel pnlMontoNDContainer = null!;
        private TextBox txtMontoND = null!;
        private Label lblTotalAfectado = null!;
        private Button btnEmitir = null!;
        private Button btnCancelar = null!;

        public bool DocumentoEmitido { get; private set; } = false;

        public FormEmitirNotaDteModal(List<TVE2607> documentos, Usuario? usuario = null)
        {
            _documentosSeleccionados = documentos ?? new List<TVE2607>();
            _usuarioActual = usuario ?? new Usuario { NombreUsuario = "admin" };

            InitializeComponent();
            ConfigurarEstadoInicial();
        }

        private void InitializeComponent()
        {
            this.Text = "⚡ Emisión de Documento Modificador DTE (SII)";
            // 👈 Tamaño ampliado a 560 x 740 para evitar que aparezca la barra de scroll
            this.ClientSize = new Size(560, 740);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 14, 22, 14),
                BackColor = Color.White,
                AutoScroll = false // 👈 Evita que fuerce recorte horizontal
            };

            // 1. TÍTULO Y RESUMEN SELECCIÓN
            Label lblTitulo = new Label
            {
                Text = "Emisión de Nota de Crédito / Débito",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 28
            };

            string foliosTexto = string.Join(", ", _documentosSeleccionados.Select(d => $"#{d.nroDTE}"));
            decimal totalDoc = _documentosSeleccionados.Sum(d => d.Total);

            Panel pnlCardResumen = CrearTarjetaRedondeada(68);
            pnlCardResumen.Dock = DockStyle.Top;
            pnlCardResumen.Margin = new Padding(0, 8, 0, 10);
            pnlCardResumen.Padding = new Padding(12, 8, 12, 8);

            Label lblResumenTit = new Label { Text = $"DOCUMENTO(S) SELECCIONADO(S): {_documentosSeleccionados.Count}", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 18 };
            Label lblResumenSub = new Label { Text = $"Folios: {foliosTexto} | Monto original: {MonedaHelper.Formatear(totalDoc, conSigno: true)}", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Fill };
            pnlCardResumen.Controls.Add(lblResumenSub);
            pnlCardResumen.Controls.Add(lblResumenTit);

            // 2. SELECTOR DE TIPO DE DOCUMENTO (NC vs ND)
            Panel pnlCardTipo = CrearTarjetaRedondeada(100);
            pnlCardTipo.Dock = DockStyle.Top;
            pnlCardTipo.Margin = new Padding(0, 8, 0, 10);
            pnlCardTipo.Padding = new Padding(12);

            Label lblTitTipo = new Label { Text = "TIPO DE DOCUMENTO A EMITIR", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 20 };

            TableLayoutPanel tlpBotonesTipo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlpBotonesTipo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpBotonesTipo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            btnTipoNC = new Button
            {
                Text = "📄 NOTA DE CRÉDITO (61)\n(Resta / Anula Venta)",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTipoNC.Click += (s, e) => SeleccionarTipoNota(esNC: true);

            btnTipoND = new Button
            {
                Text = "📑 NOTA DE DÉBITO (56)\n(Suma / Aplica Recargo)",
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 4, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTipoND.Click += (s, e) => SeleccionarTipoNota(esNC: false);

            tlpBotonesTipo.Controls.Add(btnTipoNC, 0, 0);
            tlpBotonesTipo.Controls.Add(btnTipoND, 1, 0);

            pnlCardTipo.Controls.Add(tlpBotonesTipo);
            pnlCardTipo.Controls.Add(lblTitTipo);

            // 3. SELECTOR TÁCTIL DE CAUSA SII
            Panel pnlCardCausas = CrearTarjetaRedondeada(170);
            pnlCardCausas.Dock = DockStyle.Top;
            pnlCardCausas.Margin = new Padding(0, 8, 0, 10);
            pnlCardCausas.Padding = new Padding(12);

            Label lblTitCausas = new Label 
            { 
                Text = "CÓDIGO DE REFERENCIA TRIBUTARIA (SII)", 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Dock = DockStyle.Top, 
                Height = 22 
            };

            // Contenedor vertical que auto-ajusta el ancho al 100% exacto
            Panel pnlListaCausas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnCausa1 = CrearBotonOpcionCausa("1 - Anula Documento de Referencia (Anulación / Devolución Total)", 1);
            btnCausa2 = CrearBotonOpcionCausa("2 - Corrige Texto en Documento (Glosa o Datos)", 2);
            btnCausa3 = CrearBotonOpcionCausa("3 - Corrige Montos / Recargo o Descuento Parcial", 3);

            // Al agregarlos en orden inverso con Dock = Top, quedan: 1 arriba, 2 al medio, 3 abajo
            pnlListaCausas.Controls.Add(btnCausa3);
            pnlListaCausas.Controls.Add(btnCausa2);
            pnlListaCausas.Controls.Add(btnCausa1);

            pnlCardCausas.Controls.Add(pnlListaCausas);
            pnlCardCausas.Controls.Add(lblTitCausas);

            // 4. CAMPO MONTO RECARGO (SOLO PARA NOTA DE DÉBITO)
            pnlMontoNDContainer = CrearTarjetaRedondeada(68);
            pnlMontoNDContainer.Dock = DockStyle.Top;
            pnlMontoNDContainer.Margin = new Padding(0, 8, 0, 10);
            pnlMontoNDContainer.Padding = new Padding(12, 8, 12, 8);
            pnlMontoNDContainer.Visible = false;

            Label lblTitMontoND = new Label { Text = "MONTO TOTAL A AGREGAR CON NOTA DE DÉBITO ($):", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237), Dock = DockStyle.Top, Height = 18 };
            txtMontoND = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Text = "0",
                MaxLength = 10
            };
            txtMontoND.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtMontoND.TextChanged += (s, e) =>
            {
                MonedaHelper.AplicarMascaraEnVivo(txtMontoND);
                decimal monto = MonedaHelper.Limpiar(txtMontoND.Text);
                lblTotalAfectado.Text = $"MONTO NOTA DE DÉBITO: +{MonedaHelper.Formatear(monto, conSigno: true)} (Ajuste a cobrar)";
            };

            pnlMontoNDContainer.Controls.Add(txtMontoND);
            pnlMontoNDContainer.Controls.Add(lblTitMontoND);

            // 5. MOTIVO / GLOSA TRIBUTARIA
            Panel pnlCardMotivo = CrearTarjetaRedondeada(84);
            pnlCardMotivo.Dock = DockStyle.Top;
            pnlCardMotivo.Margin = new Padding(0, 8, 0, 10);
            pnlCardMotivo.Padding = new Padding(12);

            Label lblTitMotivo = new Label { Text = "MOTIVO / GLOSA DE EMISIÓN:", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 18 };
            txtMotivo = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                Text = "Devolución de mercadería / Anulación de venta",
                Multiline = true
            };
            pnlCardMotivo.Controls.Add(txtMotivo);
            pnlCardMotivo.Controls.Add(lblTitMotivo);

            // 6. TOTAL A DECLARAR
            Panel pnlTotalCard = CrearTarjetaRedondeada(54);
            pnlTotalCard.Dock = DockStyle.Top;
            pnlTotalCard.BackColor = Color.FromArgb(248, 250, 252);
            pnlTotalCard.Padding = new Padding(12, 10, 12, 10);

            lblTotalAfectado = new Label
            {
                Text = $"MONTO NOTA DE CRÉDITO: -{MonedaHelper.Formatear(totalDoc, conSigno: true)}",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTotalCard.Controls.Add(lblTotalAfectado);

            // 7. BOTONES TÁCTILES INFERIORES
            Panel pnlBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 105,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnEmitir = new Button
            {
                Text = "⚡ EMITIR NOTA DE CRÉDITO (61)",
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEmitir.FlatAppearance.BorderSize = 0;
            btnEmitir.Click += BtnEmitir_Click;

            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 8 };

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            pnlBotones.Controls.Add(btnCancelar);
            pnlBotones.Controls.Add(pnlSpacer);
            pnlBotones.Controls.Add(btnEmitir);

            pnlMain.Controls.Add(pnlBotones);
            pnlMain.Controls.Add(pnlTotalCard);
            pnlMain.Controls.Add(pnlCardMotivo);
            pnlMain.Controls.Add(pnlMontoNDContainer);
            pnlMain.Controls.Add(pnlCardCausas);
            pnlMain.Controls.Add(pnlCardTipo);
            pnlMain.Controls.Add(pnlCardResumen);
            pnlMain.Controls.Add(lblTitulo);

            this.Controls.Add(pnlMain);
            this.CancelButton = btnCancelar;
        }

        private void ConfigurarEstadoInicial()
        {
            bool tieneFactura = _documentosSeleccionados.Any(d => d.iddocDTE == 33);
            if (!tieneFactura)
            {
                btnTipoND.Enabled = false;
                btnTipoND.BackColor = Color.FromArgb(241, 245, 249);
                btnTipoND.ForeColor = Color.FromArgb(148, 163, 184);
            }

            SeleccionarTipoNota(esNC: true);
            ActualizarEstiloCausas();
        }

        private void SeleccionarTipoNota(bool esNC)
        {
            _esNotaCredito = esNC;

            if (_esNotaCredito)
            {
                btnTipoNC.BackColor = Color.FromArgb(254, 242, 242);
                btnTipoNC.ForeColor = Color.FromArgb(220, 38, 38);
                btnTipoNC.FlatAppearance.BorderColor = Color.FromArgb(220, 38, 38);
                btnTipoNC.FlatAppearance.BorderSize = 2;

                btnTipoND.BackColor = Color.White;
                btnTipoND.ForeColor = Color.FromArgb(71, 85, 105);
                btnTipoND.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                btnTipoND.FlatAppearance.BorderSize = 1;

                pnlMontoNDContainer.Visible = false;
                decimal total = _documentosSeleccionados.Sum(d => d.Total);
                lblTotalAfectado.Text = $"MONTO NOTA DE CRÉDITO: -{MonedaHelper.Formatear(total, conSigno: true)}";
                lblTotalAfectado.ForeColor = Color.FromArgb(220, 38, 38);

                btnEmitir.Text = "⚡ EMITIR NOTA DE CRÉDITO (61)";
                btnEmitir.BackColor = Color.FromArgb(220, 38, 38);
                txtMotivo.Text = "Devolución de mercadería / Anulación de venta";

                // En Nota de Crédito se habilitan las 3 causas
                btnCausa1.Visible = true;
                btnCausa2.Visible = true;
                btnCausa3.Visible = true;
                _codigoCausaSeleccionado = 1;
            }
            else
            {
                btnTipoND.BackColor = Color.FromArgb(243, 232, 255);
                btnTipoND.ForeColor = Color.FromArgb(124, 58, 237);
                btnTipoND.FlatAppearance.BorderColor = Color.FromArgb(124, 58, 237);
                btnTipoND.FlatAppearance.BorderSize = 2;

                btnTipoNC.BackColor = Color.White;
                btnTipoNC.ForeColor = Color.FromArgb(71, 85, 105);
                btnTipoNC.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                btnTipoNC.FlatAppearance.BorderSize = 1;

                pnlMontoNDContainer.Visible = true;
                txtMontoND.Focus();
                decimal monto = MonedaHelper.Limpiar(txtMontoND.Text);
                lblTotalAfectado.Text = $"MONTO NOTA DE DÉBITO: +{MonedaHelper.Formatear(monto, conSigno: true)} (Ajuste a cobrar)";
                lblTotalAfectado.ForeColor = Color.FromArgb(124, 58, 237);

                btnEmitir.Text = "⚡ EMITIR NOTA DE DÉBITO (56)";
                btnEmitir.BackColor = Color.FromArgb(124, 58, 237);
                txtMotivo.Text = "Recargo por flete y transporte a local";

                // En Nota de Débito solo aplica Modificación de Montos (Código 3)
                btnCausa1.Visible = false;
                btnCausa2.Visible = false;
                btnCausa3.Visible = true;
                _codigoCausaSeleccionado = 3;
            }
            ActualizarEstiloCausas();
        }

        private Button CrearBotonOpcionCausa(string texto, int codigo)
        {
            Button btn = new Button
            {
                Text = texto,
                Dock = DockStyle.Top,
                Height = 38,
                Margin = new Padding(0, 0, 0, 6),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) =>
            {
                _codigoCausaSeleccionado = codigo;
                ActualizarEstiloCausas();
            };
            return btn;
        }

        private void ActualizarEstiloCausas()
        {
            Button[] btns = { btnCausa1, btnCausa2, btnCausa3 };
            for (int i = 0; i < btns.Length; i++)
            {
                bool seleccionado = (i + 1 == _codigoCausaSeleccionado);
                if (seleccionado)
                {
                    btns[i].BackColor = Color.FromArgb(239, 246, 255);
                    btns[i].ForeColor = Color.FromArgb(2, 132, 199);
                    btns[i].FlatAppearance.BorderColor = Color.FromArgb(2, 132, 199);
                    btns[i].FlatAppearance.BorderSize = 2;
                }
                else
                {
                    btns[i].BackColor = Color.White;
                    btns[i].ForeColor = Color.FromArgb(71, 85, 105);
                    btns[i].FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                    btns[i].FlatAppearance.BorderSize = 1;
                }
            }
        }

        private void BtnEmitir_Click(object? sender, EventArgs e)
        {
            string motivo = txtMotivo.Text.Trim();
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Debe ingresar un motivo o glosa para la emisión tributaria.", "Motivo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMotivo.Focus();
                return;
            }

            string operador = _usuarioActual?.NombreUsuario ?? "admin";

            if (_esNotaCredito)
            {
                var ids = _documentosSeleccionados.Select(d => d.idTve).ToList();
                var res = NotaCreditoService.EmitirNotaCredito(ids, motivo, _codigoCausaSeleccionado, operador);

                if (res.Exito)
                {
                    MessageBox.Show(res.Mensaje, "Nota de Crédito Emitida", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DocumentoEmitido = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(res.Mensaje, "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                decimal montoND = MonedaHelper.Limpiar(txtMontoND.Text);
                if (montoND <= 0)
                {
                    MessageBox.Show("Debe ingresar un monto válido a recargar mediante Nota de Débito.", "Monto Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMontoND.Focus();
                    return;
                }

                int idFactura = _documentosSeleccionados[0].idTve;
                var res = NotaCreditoService.EmitirNotaDebito(idFactura, montoND, motivo, operador);

                if (res.Exito)
                {
                    MessageBox.Show(res.Mensaje, "Nota de Débito Emitida", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DocumentoEmitido = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(res.Mensaje, "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private Panel CrearTarjetaRedondeada(int alto)
        {
            Panel pnl = new Panel 
            { 
                Height = alto, 
                BackColor = Color.White 
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath path = new GraphicsPath();
                int d = 14;
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