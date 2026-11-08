using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
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

        private Panel pnlDetalleParcialContainer = null!;
        private DataGridView dgvItemsDevolucion = null!;
        private List<ItemDevolucionParcial> _itemsVentaOriginal = new List<ItemDevolucionParcial>();

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

            CargarDetallesVentaOriginal();
            InitializeComponent();
            ConfigurarEstadoInicial();
        }

        private void CargarDetallesVentaOriginal()
        {
            if (_documentosSeleccionados.Count == 1)
            {
                using var db = new AppDbContext();
                int idTve = _documentosSeleccionados[0].idTve;
                var detalles = db.TVD2607.Where(d => d.idTve == idTve).ToList();

                _itemsVentaOriginal = detalles.Select(d =>
                {
                    bool pesable = d.Unidad == "gr" || (d.Cantidad >= 50 && d.Precio > 0 && d.Precio < 100);
                    return new ItemDevolucionParcial
                    {
                        IdTvdOrigen = d.idTvd,
                        IdProducto = d.IdProducto,
                        NombreProducto = d.NmbProducto ?? "Producto",
                        CantidadOriginal = d.Cantidad,
                        CantidadADevolver = 0,
                        PrecioUnitario = d.Precio,
                        EsPesable = pesable,
                        Unidad = pesable ? "gr" : "UN"
                    };
                }).ToList();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "⚡ Emisión de Documento Modificador DTE (SII)";
            this.ClientSize = new Size(590, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 12, 22, 12),
                BackColor = Color.White,
                AutoScroll = true
            };

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

            Panel pnlCardResumen = CrearTarjetaRedondeada(66);
            pnlCardResumen.Dock = DockStyle.Top;
            pnlCardResumen.Margin = new Padding(0, 6, 0, 8);
            pnlCardResumen.Padding = new Padding(12, 8, 12, 8);

            Label lblResumenTit = new Label { Text = $"DOCUMENTO(S) SELECCIONADO(S): {_documentosSeleccionados.Count}", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 18 };
            Label lblResumenSub = new Label { Text = $"Folios: {foliosTexto} | Monto original: {MonedaHelper.Formatear(totalDoc, conSigno: true)}", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Fill };
            pnlCardResumen.Controls.Add(lblResumenSub);
            pnlCardResumen.Controls.Add(lblResumenTit);

            Panel pnlCardTipo = CrearTarjetaRedondeada(95);
            pnlCardTipo.Dock = DockStyle.Top;
            pnlCardTipo.Margin = new Padding(0, 6, 0, 8);
            pnlCardTipo.Padding = new Padding(12);

            Label lblTitTipo = new Label { Text = "TIPO DE DOCUMENTO A EMITIR", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 18 };

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

            Panel pnlCardCausas = CrearTarjetaRedondeada(168);
            pnlCardCausas.Dock = DockStyle.Top;
            pnlCardCausas.Margin = new Padding(0, 6, 0, 8);
            pnlCardCausas.Padding = new Padding(12);

            Label lblTitCausas = new Label 
            { 
                Text = "CÓDIGO DE REFERENCIA TRIBUTARIA (SII)", 
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Dock = DockStyle.Top, 
                Height = 20 
            };

            Panel pnlListaCausas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnCausa1 = CrearBotonOpcionCausa("1 - Anula Documento de Referencia (Anulación / Devolución Total)", 1);
            btnCausa2 = CrearBotonOpcionCausa("2 - Corrige Texto en Documento (Glosa o Datos)", 2);
            btnCausa3 = CrearBotonOpcionCausa("3 - Corrige Montos / Recargo o Descuento Parcial", 3);

            pnlListaCausas.Controls.Add(btnCausa3);
            pnlListaCausas.Controls.Add(btnCausa2);
            pnlListaCausas.Controls.Add(btnCausa1);

            pnlCardCausas.Controls.Add(pnlListaCausas);
            pnlCardCausas.Controls.Add(lblTitCausas);

            // 4. GRILLA INTERACTIVA PARA DEVOLUCIÓN PARCIAL (CAUSA 3)
            pnlDetalleParcialContainer = CrearTarjetaRedondeada(170);
            pnlDetalleParcialContainer.Dock = DockStyle.Top;
            pnlDetalleParcialContainer.Margin = new Padding(0, 6, 0, 8);
            pnlDetalleParcialContainer.Padding = new Padding(10);
            pnlDetalleParcialContainer.Visible = false;

            Label lblTitGrid = new Label
            {
                Text = "SELECCIONE LOS PRODUCTOS Y LA CANTIDAD A DEVOLVER:",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(234, 88, 12),
                Dock = DockStyle.Top,
                Height = 20
            };

            dgvItemsDevolucion = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 28 }
            };
            ConfigurarEstiloTablaParcial(dgvItemsDevolucion);
            ConfigurarColumnasGrillaParcial();

            pnlDetalleParcialContainer.Controls.Add(dgvItemsDevolucion);
            pnlDetalleParcialContainer.Controls.Add(lblTitGrid);

            // 5. CAMPO MONTO NOTA DE DÉBITO
            pnlMontoNDContainer = CrearTarjetaRedondeada(64);
            pnlMontoNDContainer.Dock = DockStyle.Top;
            pnlMontoNDContainer.Margin = new Padding(0, 6, 0, 8);
            pnlMontoNDContainer.Padding = new Padding(12, 6, 12, 6);
            pnlMontoNDContainer.Visible = false;

            Label lblTitMontoND = new Label { Text = "MONTO TOTAL A AGREGAR CON NOTA DE DÉBITO ($):", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(124, 58, 237), Dock = DockStyle.Top, Height = 18 };
            txtMontoND = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
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

            // 6. MOTIVO / GLOSA TRIBUTARIA
            Panel pnlCardMotivo = CrearTarjetaRedondeada(74);
            pnlCardMotivo.Dock = DockStyle.Top;
            pnlCardMotivo.Margin = new Padding(0, 6, 0, 8);
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

            // 7. TOTAL A DECLARAR
            Panel pnlTotalCard = CrearTarjetaRedondeada(48);
            pnlTotalCard.Dock = DockStyle.Top;
            pnlTotalCard.BackColor = Color.FromArgb(248, 250, 252);
            pnlTotalCard.Padding = new Padding(12, 8, 12, 8);

            lblTotalAfectado = new Label
            {
                Text = $"MONTO NOTA DE CRÉDITO: -{MonedaHelper.Formatear(totalDoc, conSigno: true)}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTotalCard.Controls.Add(lblTotalAfectado);

            // 8. BOTONES INFERIORES
            Panel pnlBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 95,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnEmitir = new Button
            {
                Text = "⚡ EMITIR NOTA DE CRÉDITO (61)",
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEmitir.FlatAppearance.BorderSize = 0;
            btnEmitir.Click += BtnEmitir_Click;

            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 6 };

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Dock = DockStyle.Top,
                Height = 36,
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
            pnlBotones.Controls.Add(btnEmitir);

            pnlMain.Controls.Add(pnlBotones);
            pnlMain.Controls.Add(pnlTotalCard);
            pnlMain.Controls.Add(pnlCardMotivo);
            pnlMain.Controls.Add(pnlDetalleParcialContainer);
            pnlMain.Controls.Add(pnlMontoNDContainer);
            pnlMain.Controls.Add(pnlCardCausas);
            pnlMain.Controls.Add(pnlCardTipo);
            pnlMain.Controls.Add(pnlCardResumen);
            pnlMain.Controls.Add(lblTitulo);

            this.Controls.Add(pnlMain);
            this.CancelButton = btnCancelar;
        }

        private void ConfigurarColumnasGrillaParcial()
        {
            dgvItemsDevolucion.Columns.Clear();
            dgvItemsDevolucion.AutoGenerateColumns = false;

            // Manejo preventivo del error nativo de DataGridView
            dgvItemsDevolucion.DataError += (s, e) => { e.Cancel = true; };

            // 1. PRODUCTO
            dgvItemsDevolucion.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NombreProducto",
                DataPropertyName = "NombreProducto",
                HeaderText = "PRODUCTO",
                ReadOnly = true,
                FillWeight = 42
            });

            // 2. CANTIDAD COMPRADA
            dgvItemsDevolucion.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CantOriginalStr",
                HeaderText = "COMPRADO",
                ReadOnly = true,
                FillWeight = 20,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 3. CANTIDAD A DEVOLVER (EDITABLE)
            var colDevolver = new DataGridViewTextBoxColumn
            {
                Name = "CantidadADevolver",
                DataPropertyName = "CantidadADevolver",
                HeaderText = "DEVOLVER",
                FillWeight = 18,
                ValueType = typeof(int)
            };
            colDevolver.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colDevolver.DefaultCellStyle.ForeColor = Color.FromArgb(220, 38, 38);
            colDevolver.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvItemsDevolucion.Columns.Add(colDevolver);

            // 4. TOTAL REBAJA
            var colSub = new DataGridViewTextBoxColumn
            {
                Name = "SubtotalADevolver",
                DataPropertyName = "SubtotalADevolver",
                HeaderText = "TOTAL REBAJA",
                ReadOnly = true,
                FillWeight = 20,
                ValueType = typeof(decimal)
            };
            colSub.DefaultCellStyle.FormatProvider = new System.Globalization.CultureInfo("es-CL");
            colSub.DefaultCellStyle.Format = "$ #,##0";
            colSub.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colSub.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvItemsDevolucion.Columns.Add(colSub);

            // Formato de texto para unidades / gramos
            dgvItemsDevolucion.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _itemsVentaOriginal.Count) return;
                var item = _itemsVentaOriginal[e.RowIndex];

                if (dgvItemsDevolucion.Columns[e.ColumnIndex].Name == "CantOriginalStr")
                {
                    e.Value = item.EsPesable ? $"{item.CantidadOriginal} g" : $"{item.CantidadOriginal} un";
                    e.FormattingApplied = true;
                }
            };

            // Validación al terminar de escribir la cantidad
            dgvItemsDevolucion.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _itemsVentaOriginal.Count) return;
                var item = _itemsVentaOriginal[e.RowIndex];

                string valStr = dgvItemsDevolucion.Rows[e.RowIndex].Cells["CantidadADevolver"].Value?.ToString() ?? "0";
                if (!int.TryParse(valStr, out int cantDev) || cantDev < 0)
                {
                    item.CantidadADevolver = 0;
                }
                else if (cantDev > item.CantidadOriginal)
                {
                    MessageBox.Show(
                        $"La cantidad a devolver no puede superar lo comprado ({item.CantidadOriginal} {(item.EsPesable ? "g" : "un")}).",
                        "Límite Excedido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    item.CantidadADevolver = item.CantidadOriginal;
                }
                else
                {
                    item.CantidadADevolver = cantDev;
                }

                dgvItemsDevolucion.Refresh();
                RecalcularTotalParcial();
            };
        }

        private void RecalcularTotalParcial()
        {
            decimal sumaParcial = _itemsVentaOriginal.Sum(i => i.SubtotalADevolver);
            lblTotalAfectado.Text = $"MONTO NOTA DE CRÉDITO: -{MonedaHelper.Formatear(sumaParcial, conSigno: true)} (Ajuste Parcial)";
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

                btnEmitir.Text = "⚡ EMITIR NOTA DE CRÉDITO (61)";
                btnEmitir.BackColor = Color.FromArgb(220, 38, 38);

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

                pnlDetalleParcialContainer.Visible = false;
                pnlMontoNDContainer.Visible = true;
                txtMontoND.Focus();
                decimal monto = MonedaHelper.Limpiar(txtMontoND.Text);
                lblTotalAfectado.Text = $"MONTO NOTA DE DÉBITO: +{MonedaHelper.Formatear(monto, conSigno: true)} (Ajuste a cobrar)";
                lblTotalAfectado.ForeColor = Color.FromArgb(124, 58, 237);

                btnEmitir.Text = "⚡ EMITIR NOTA DE DÉBITO (56)";
                btnEmitir.BackColor = Color.FromArgb(124, 58, 237);
                txtMotivo.Text = "Recargo por flete y transporte a local";

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
                Height = 36,
                Margin = new Padding(0, 0, 0, 4),
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

            if (_esNotaCredito)
            {
                decimal totalOriginal = _documentosSeleccionados.Sum(d => d.Total);

                switch (_codigoCausaSeleccionado)
                {
                    case 1:
                        pnlDetalleParcialContainer.Visible = false;
                        lblTotalAfectado.Text = $"MONTO NOTA DE CRÉDITO: -{MonedaHelper.Formatear(totalOriginal, conSigno: true)}";
                        lblTotalAfectado.ForeColor = Color.FromArgb(220, 38, 38);
                        txtMotivo.Text = "Devolución de mercadería / Anulación de venta";
                        break;

                    case 2:
                        pnlDetalleParcialContainer.Visible = false;
                        lblTotalAfectado.Text = "MONTO NOTA DE CRÉDITO: $ 0 (Documento Aclaratorio)";
                        lblTotalAfectado.ForeColor = Color.FromArgb(2, 132, 199);
                        txtMotivo.Text = "Corrige texto: Donde dice '...' debe decir '...'";
                        break;

                    case 3:
                        if (_documentosSeleccionados.Count > 1)
                        {
                            MessageBox.Show("La devolución parcial de ítems debe emitirse sobre un único documento.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            _codigoCausaSeleccionado = 1;
                            ActualizarEstiloCausas();
                            return;
                        }

                        pnlDetalleParcialContainer.Visible = true;
                        dgvItemsDevolucion.DataSource = null;
                        dgvItemsDevolucion.DataSource = _itemsVentaOriginal;
                        RecalcularTotalParcial();
                        lblTotalAfectado.ForeColor = Color.FromArgb(234, 88, 12);
                        txtMotivo.Text = "Devolución parcial de mercadería";
                        break;
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
                if (_codigoCausaSeleccionado == 3)
                {
                    var itemsValidos = _itemsVentaOriginal.Where(i => i.CantidadADevolver > 0).ToList();
                    if (itemsValidos.Count == 0)
                    {
                        MessageBox.Show("Debe ingresar la cantidad a devolver en al menos un producto.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int idVenta = _documentosSeleccionados[0].idTve;
                    var resParcial = NotaCreditoService.EmitirNotaCreditoParcial(idVenta, itemsValidos, motivo, operador);

                    if (resParcial.Exito)
                    {
                        MessageBox.Show(resParcial.Mensaje, "Nota de Crédito Emitida", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DocumentoEmitido = true;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show(resParcial.Mensaje, "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
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

        private void ConfigurarEstiloTablaParcial(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersHeight = 26;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 243, 199);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(146, 64, 14);
            dgv.GridColor = Color.FromArgb(226, 232, 240);
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