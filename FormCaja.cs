using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormCaja : Form
    {
        private AppDbContext _db = new AppDbContext();
        private static CajaTurno? _turnoActual = null;
        private Usuario? _usuarioActual;

        private Label lblEstadoCaja = null!;
        private Label lblMontoInicial = null!;
        private Label lblVentasEfectivo = null!;
        private Label lblMovimientosOtros = null!;
        private Label lblEfectivoEsperado = null!;

        private Panel pnlCajaCerrada = null!;
        private Panel pnlCajaAbierta = null!;
        private TextBox txtMontoInicial = null!;

        public FormCaja(Usuario? usuario = null)
        {
            _usuarioActual = usuario;
            InitializeComponent();
            ActualizarEstadoVista();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(244, 246, 249)
            };

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            lblEstadoCaja = new Label
            {
                Text = "ESTADO DE CAJA: CARGANDO...",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblEstadoCaja);

            pnlCajaCerrada = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                BackColor = Color.White,
                Margin = new Padding(0, 20, 0, 0),
                Padding = new Padding(30),
                Visible = false
            };

            Label lblTituloAbrir = new Label
            {
                Text = "🔓 Apertura de Caja y Turno de Trabajo",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label lblSubAbrir = new Label
            {
                Text = "Ingrese el monto inicial en efectivo (cambio/fondo de caja) disponible antes de iniciar la atención.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(30, 60),
                AutoSize = true
            };

            Label lblMontoLabel = new Label
            {
                Text = "FONDO INICIAL DE CAJA ($):",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(30, 110),
                AutoSize = true
            };

            txtMontoInicial = new TextBox
            {
                Text = "20000",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(30, 135),
                Size = new Size(250, 35),
                BorderStyle = BorderStyle.FixedSingle
            };

            Button btnAbrirCaja = new Button
            {
                Text = "🚀 ABRIR CAJA AHORA",
                Location = new Point(30, 190),
                Size = new Size(250, 45),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAbrirCaja.FlatAppearance.BorderSize = 0;
            btnAbrirCaja.Click += BtnAbrirCaja_Click;

            pnlCajaCerrada.Controls.Add(lblTituloAbrir);
            pnlCajaCerrada.Controls.Add(lblSubAbrir);
            pnlCajaCerrada.Controls.Add(lblMontoLabel);
            pnlCajaCerrada.Controls.Add(txtMontoInicial);
            pnlCajaCerrada.Controls.Add(btnAbrirCaja);

            pnlCajaAbierta = new Panel
            {
                Dock = DockStyle.Top,
                Height = 360,
                BackColor = Color.Transparent,
                Visible = false
            };

            TableLayoutPanel gridMetricas = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 120,
                ColumnCount = 4,
                RowCount = 1
            };
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            gridMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            Panel card1 = CrearCardMetrica("💵 Fondo Inicial", "$ 0", Color.FromArgb(100, 116, 139), out lblMontoInicial);
            Panel card2 = CrearCardMetrica("🛒 Ventas Efectivo", "$ 0", Color.FromArgb(16, 185, 129), out lblVentasEfectivo);
            Panel card3 = CrearCardMetrica("🔄 Retiros / Ingresos", "$ 0", Color.FromArgb(245, 158, 11), out lblMovimientosOtros);
            Panel card4 = CrearCardMetrica("📊 Efectivo Esperado", "$ 0", Color.FromArgb(2, 132, 199), out lblEfectivoEsperado);

            gridMetricas.Controls.Add(card1, 0, 0);
            gridMetricas.Controls.Add(card2, 1, 0);
            gridMetricas.Controls.Add(card3, 2, 0);
            gridMetricas.Controls.Add(card4, 3, 0);

            Panel pnlAcciones = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.White,
                Margin = new Padding(0, 20, 0, 0),
                Padding = new Padding(25)
            };

            Label lblAccionesTitulo = new Label
            {
                Text = "Acciones de Turno",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(25, 20),
                AutoSize = true
            };

            Button btnMovimiento = new Button
            {
                Text = "💸 Registrar Retiro / Ingreso",
                Location = new Point(25, 65),
                Size = new Size(240, 48),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMovimiento.FlatAppearance.BorderSize = 0;
            btnMovimiento.Click += BtnMovimiento_Click;

            Button btnArqueoCierre = new Button
            {
                Text = "🔒 REALIZAR ARQUEO Y CIERRE Z",
                Location = new Point(280, 65),
                Size = new Size(270, 48),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnArqueoCierre.FlatAppearance.BorderSize = 0;
            btnArqueoCierre.Click += BtnArqueoCierre_Click;

            pnlAcciones.Controls.Add(lblAccionesTitulo);
            pnlAcciones.Controls.Add(btnMovimiento);
            pnlAcciones.Controls.Add(btnArqueoCierre);

            pnlCajaAbierta.Controls.Add(pnlAcciones);
            pnlCajaAbierta.Controls.Add(gridMetricas);

            pnlMain.Controls.Add(pnlCajaAbierta);
            pnlMain.Controls.Add(pnlCajaCerrada);
            pnlMain.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CrearCardMetrica(string titulo, string valor, Color acento, out Label lblValor)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5),
                Padding = new Padding(15)
            };

            Label lblT = new Label
            {
                Text = titulo.ToUpper(),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };

            lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = acento,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(lblValor);
            pnl.Controls.Add(lblT);
            return pnl;
        }

        private void ActualizarEstadoVista()
        {
            if (_turnoActual != null && _turnoActual.Estado == "Abierta")
            {
                string cajero = _turnoActual.Usuario;
                lblEstadoCaja.Text = $"🟢 CAJA ABIERTA ({cajero}) - Turno iniciado el {_turnoActual.FechaApertura:dd/MM/yyyy HH:mm}";
                lblEstadoCaja.ForeColor = Color.FromArgb(16, 185, 129);

                pnlCajaCerrada.Visible = false;
                pnlCajaAbierta.Visible = true;

                decimal ventasEfectivo = 0;
                try
                {
                    // Consulta sobre TVE2607 restando las Notas de Crédito (iddocDTE == 61)
                    ventasEfectivo = _db.TVE2607
                        .Where(v => v.FecDoc >= _turnoActual.FechaApertura && v.status != "Anulado")
                        .Sum(v => (decimal?)(v.iddocDTE == 61 ? -v.Total : v.Total)) ?? 0;
                }
                catch { }

                _turnoActual.MontoEfectivoVentas = ventasEfectivo;
                decimal esperado = _turnoActual.MontoInicial + _turnoActual.MontoEfectivoVentas + _turnoActual.MontoIngresos - _turnoActual.MontoRetiros;

                lblMontoInicial.Text = $"$ {_turnoActual.MontoInicial:N0}";
                lblVentasEfectivo.Text = $"$ {_turnoActual.MontoEfectivoVentas:N0}";
                lblMovimientosOtros.Text = $"$ {(_turnoActual.MontoIngresos - _turnoActual.MontoRetiros):N0}";
                lblEfectivoEsperado.Text = $"$ {esperado:N0}";
            }
            else
            {
                lblEstadoCaja.Text = "🔴 CAJA CERRADA - Inicie un nuevo turno para poder operar en el POS";
                lblEstadoCaja.ForeColor = Color.FromArgb(220, 38, 38);

                pnlCajaAbierta.Visible = false;
                pnlCajaCerrada.Visible = true;
            }
        }

        private void BtnAbrirCaja_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMontoInicial.Text.Trim(), out decimal montoInicial) || montoInicial < 0)
            {
                MessageBox.Show("Ingrese un monto inicial válido en dinero.", "Monto inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _turnoActual = new CajaTurno
            {
                CajaTurnoID = 1,
                Usuario = _usuarioActual?.NombreUsuario ?? "admin",
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                Estado = "Abierta"
            };

            MessageBox.Show($"¡Caja abierta exitosamente por el usuario '{_turnoActual.Usuario}' con ${_turnoActual.MontoInicial:N0} de fondo!", "Apertura de Caja", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ActualizarEstadoVista();
        }

        private void BtnMovimiento_Click(object? sender, EventArgs e)
        {
            Form formMov = new Form
            {
                Text = "Registrar Movimiento de Caja",
                Size = new Size(360, 260),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTipo = new Label { Text = "Tipo de Movimiento:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cbTipo = new ComboBox { Location = new Point(20, 45), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTipo.Items.AddRange(new string[] { "Retiro / Sangría (Salida)", "Ingreso Manual (Entrada)" });
            cbTipo.SelectedIndex = 0;

            Label lblMonto = new Label { Text = "Monto ($):", Location = new Point(20, 85), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtM = new TextBox { Location = new Point(20, 110), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };

            Button btnGuardar = new Button
            {
                Text = "Guardar Movimiento",
                Location = new Point(20, 155),
                Size = new Size(300, 40),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, args) =>
            {
                if (decimal.TryParse(txtM.Text.Trim(), out decimal monto) && monto > 0 && _turnoActual != null)
                {
                    if (cbTipo.SelectedIndex == 0) _turnoActual.MontoRetiros += monto;
                    else _turnoActual.MontoIngresos += monto;

                    MessageBox.Show("Movimiento registrado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    formMov.Close();
                    ActualizarEstadoVista();
                }
                else
                {
                    MessageBox.Show("Ingrese un monto válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            formMov.Controls.AddRange(new Control[] { lblTipo, cbTipo, lblMonto, txtM, btnGuardar });
            formMov.ShowDialog();
        }

        private void BtnArqueoCierre_Click(object? sender, EventArgs e)
        {
            if (_turnoActual == null) return;

            decimal esperado = _turnoActual.MontoInicial + _turnoActual.MontoEfectivoVentas + _turnoActual.MontoIngresos - _turnoActual.MontoRetiros;

            Form formCierre = new Form
            {
                Text = "Arqueo de Caja - Cierre Z",
                Size = new Size(380, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblTitle = new Label { Text = "📊 Cierre de Turno y Arqueo", Location = new Point(20, 15), Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true };
            Label lblEsp = new Label { Text = $"EFECTIVO ESPERADO: ${esperado:N0}", Location = new Point(20, 50), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199), AutoSize = true };

            Label lblReal = new Label { Text = "¿Cuánto efectivo contaste en caja? ($):", Location = new Point(20, 95), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtReal = new TextBox { Location = new Point(20, 120), Size = new Size(320, 32), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

            Button btnCerrar = new Button
            {
                Text = "🔒 EFECTUAR CIERRE DE CAJA",
                Location = new Point(20, 175),
                Size = new Size(320, 45),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, args) =>
            {
                if (decimal.TryParse(txtReal.Text.Trim(), out decimal real))
                {
                    decimal dif = real - esperado;
                    _turnoActual.MontoEfectivoReal = real;
                    _turnoActual.Diferencia = dif;
                    _turnoActual.FechaCierre = DateTime.Now;
                    _turnoActual.Estado = "Cerrada";

                    string resMensaje = $"--- RESUMEN DE CIERRE DE CAJA (ARQUEO Z) ---\n\n" +
                                        $"• Cajero de Turno: {_turnoActual.Usuario}\n" +
                                        $"• Fondo Inicial: ${_turnoActual.MontoInicial:N0}\n" +
                                        $"• Ventas Efectivo: ${_turnoActual.MontoEfectivoVentas:N0}\n" +
                                        $"• Total Esperado: ${esperado:N0}\n" +
                                        $"• Efectivo Declarado: ${real:N0}\n\n";

                    if (dif == 0) resMensaje += "✅ CUADRATURA EXACTA ($0 de diferencia)";
                    else if (dif > 0) resMensaje += $"⚠️ SOBRANTE DE CAJA: +${dif:N0}";
                    else resMensaje += $"❌ FALTANTE DE CAJA: -${Math.Abs(dif):N0}";

                    MessageBox.Show(resMensaje, "Cierre Z Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    _turnoActual = null;
                    formCierre.Close();
                    ActualizarEstadoVista();
                }
                else
                {
                    MessageBox.Show("Ingrese el monto real contado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            formCierre.Controls.AddRange(new Control[] { lblTitle, lblEsp, lblReal, txtReal, btnCerrar });
            formCierre.ShowDialog();
        }
    }
}