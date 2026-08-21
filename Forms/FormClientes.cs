using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Modals;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO
{
    public class FormClientes : Form
    {
        private readonly ClienteService _clienteService = new ClienteService();
        private List<Cliente> _listaCompletaClientes = new List<Cliente>();

        // KPIs Dashboard
        private Label lblTotalClientesVal = null!;
        private Label lblNuevosMesVal = null!;

        // Controles de Búsqueda y Filtros
        private TextBox txtBuscar = null!;
        private ComboBox cbFiltroEstado = null!;
        private ComboBox cbFiltroTipo = null!;
        private Button btnLimpiarFiltros = null!;

        // Botones de Acción (Estilo Productos)
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEstado = null!;

        // Grilla y Footer
        private DataGridView dgvClientes = null!;
        private Label lblContadorFooter = null!;
        private Cliente? _clienteSeleccionado = null;

        public FormClientes()
        {
            InitializeComponent();
            CargarDatosCompletos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 16),
                BackColor = Color.FromArgb(244, 246, 249),
                AutoScroll = true
            };

            // ================= 1. HEADER DE TÍTULO Y BOTONERA =================
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };

            Label lblTitulo = new Label
            {
                Text = "👥 Clientes",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(0, 0),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Administra la información de tus clientes y sus condiciones comerciales",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(2, 26),
                AutoSize = true
            };

            // Panel contenedor de botones a la derecha
            Panel pnlBotonesAccion = new Panel
            {
                Dock = DockStyle.Right,
                Width = 430,
                Height = 44,
                BackColor = Color.Transparent
            };

            // 1. + Nuevo Cliente (Verde Esmeralda #10B981)
            btnNuevo = new Button
            {
                Text = "➕ Nuevo Cliente",
                Size = new Size(135, 34),
                Location = new Point(295, 4),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            // 2. Editar (Azul Cielo #0284C7)
            btnEditar = new Button
            {
                Text = "✏️ Editar",
                Size = new Size(90, 34),
                Location = new Point(200, 4),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += BtnEditar_Click;

            // 3. Activar / Desactivar (Ámbar #F59E0B)
            btnEstado = new Button
            {
                Text = "🔄 Activar / Desactivar",
                Size = new Size(160, 34),
                Location = new Point(35, 4),
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEstado.FlatAppearance.BorderSize = 0;
            btnEstado.Click += BtnEstado_Click;

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnEstado, btnEditar, btnNuevo });
            pnlHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo, pnlBotonesAccion });

            // ================= 2. DASHBOARD KPI CARDS =================
            Panel pnlDashboard = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.Transparent, Margin = new Padding(0, 6, 0, 8) };

            Panel cardTotal = CrearCardKPI(
                icono: "👥",
                colorIconoFondo: Color.FromArgb(239, 246, 255),
                colorIconoTexto: Color.FromArgb(37, 99, 235),
                titulo: "Total Clientes",
                valorInicial: "0",
                subtitulo: "Activos en sistema",
                out lblTotalClientesVal,
                x: 0,
                ancho: 280
            );

            Panel cardNuevos = CrearCardKPI(
                icono: "👤+",
                colorIconoFondo: Color.FromArgb(240, 253, 244),
                colorIconoTexto: Color.FromArgb(22, 163, 74),
                titulo: "Nuevos este mes",
                valorInicial: "0",
                subtitulo: "+ Registrados recientemente",
                out lblNuevosMesVal,
                x: 295,
                ancho: 280
            );

            pnlDashboard.Controls.AddRange(new Control[] { cardTotal, cardNuevos });

            // ================= 3. FILTROS Y BÚSQUEDA =================
            Panel pnlFiltrosBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 6, 0, 8)
            };
            pnlFiltrosBar.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlFiltrosBar.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblIconBuscar = new Label { Text = "🔍", AutoSize = true, Location = new Point(12, 20), Font = new Font("Segoe UI", 9.5F) };
            txtBuscar = new TextBox
            {
                Location = new Point(36, 17),
                Size = new Size(340, 26),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Buscar por RUT, Razón Social, Giro, Teléfono o Comuna..."
            };
            txtBuscar.TextChanged += (s, e) => AplicarFiltros();

            Label lblFiltroEst = new Label { Text = "Estado", Location = new Point(395, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            cbFiltroEstado = new ComboBox
            {
                Location = new Point(395, 21),
                Size = new Size(125, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F)
            };
            cbFiltroEstado.Items.AddRange(new object[] { "Todos", "Activos", "Inactivos" });
            cbFiltroEstado.SelectedIndex = 0;
            cbFiltroEstado.SelectedIndexChanged += (s, e) => AplicarFiltros();

            Label lblFiltroTip = new Label { Text = "Tipo de Cliente", Location = new Point(535, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139) };
            cbFiltroTipo = new ComboBox
            {
                Location = new Point(535, 21),
                Size = new Size(160, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F)
            };
            cbFiltroTipo.Items.AddRange(new object[] { "Todos", "MINORISTA", "MAYOR.(A)", "MAYOR.(B)", "RUTEROS", "PERSONAL" });
            cbFiltroTipo.SelectedIndex = 0;
            cbFiltroTipo.SelectedIndexChanged += (s, e) => AplicarFiltros();

            btnLimpiarFiltros = new Button
            {
                Text = "🔄 Limpiar",
                Location = new Point(710, 17),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLimpiarFiltros.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnLimpiarFiltros.Click += (s, e) =>
            {
                txtBuscar.Clear();
                cbFiltroEstado.SelectedIndex = 0;
                cbFiltroTipo.SelectedIndex = 0;
                AplicarFiltros();
            };

            pnlFiltrosBar.Controls.AddRange(new Control[] { lblIconBuscar, txtBuscar, lblFiltroEst, cbFiltroEstado, lblFiltroTip, cbFiltroTipo, btnLimpiarFiltros });

            // ================= 4. GRILLA (ESTILO PRODUCTOS) =================
            Panel pnlCardGrilla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            pnlCardGrilla.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlCardGrilla.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            dgvClientes = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };

            ConfigurarEstiloTabla(dgvClientes);
            dgvClientes.SelectionChanged += DgvClientes_SelectionChanged;

            // Footer
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(248, 250, 252) };
            pnlFooter.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlFooter.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 clientes",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(12, 6),
                AutoSize = true
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlCardGrilla.Controls.Add(dgvClientes);
            pnlCardGrilla.Controls.Add(pnlFooter);

            pnlPrincipal.Controls.Add(pnlCardGrilla);
            pnlPrincipal.Controls.Add(pnlFiltrosBar);
            pnlPrincipal.Controls.Add(pnlDashboard);
            pnlPrincipal.Controls.Add(pnlHeader);

            this.Controls.Add(pnlPrincipal);
            this.ResumeLayout(false);
        }

        private Panel CrearCardKPI(string icono, Color colorIconoFondo, Color colorIconoTexto, string titulo, string valorInicial, string subtitulo, out Label lblValor, int x, int ancho)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 4),
                Size = new Size(ancho, 76),
                BackColor = Color.White
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);

            Label lblIco = new Label
            {
                Text = icono,
                Location = new Point(12, 14),
                Size = new Size(44, 44),
                BackColor = colorIconoFondo,
                ForeColor = colorIconoTexto,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblTit = new Label
            {
                Text = titulo,
                Location = new Point(64, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            lblValor = new Label
            {
                Text = valorInicial,
                Location = new Point(62, 26),
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            Label lblSub = new Label
            {
                Text = subtitulo,
                Location = new Point(64, 52),
                AutoSize = true,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = colorIconoTexto
            };

            card.Controls.AddRange(new Control[] { lblIco, lblTit, lblValor, lblSub });
            return card;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;

            // Encabezados con el estilo exacto de Productos (#0F172A / Navy Oscuro)
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;

            // Filas y Celdas
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;

            // Filas alternadas suaves
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            // Borde y separación de celdas
            dgv.GridColor = Color.FromArgb(226, 232, 240);

            dgv.Columns.Clear();

            // Columnas fijas con anchos definidos para scroll horizontal
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdCliente", HeaderText = "ID", DataPropertyName = "IdCliente", Width = 55, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rut", HeaderText = "RUT", DataPropertyName = "Rut", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RazonSocial", HeaderText = "Razón Social / Nombre", DataPropertyName = "RazonSocial", Width = 230 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Giro", HeaderText = "Giro Comercial", DataPropertyName = "Giro", Width = 180 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comuna", HeaderText = "Comuna", DataPropertyName = "Comuna", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ciudad", HeaderText = "Ciudad", DataPropertyName = "Ciudad", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefono", HeaderText = "Teléfono", DataPropertyName = "Telefono", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Correo Electrónico", DataPropertyName = "Email", Width = 180 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Direccion", HeaderText = "Dirección", DataPropertyName = "Direccion", Width = 200 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoriaCliente", HeaderText = "Tipo / Categoría", DataPropertyName = "CategoriaCliente", Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "FormaPago", HeaderText = "Forma Pago", DataPropertyName = "FormaPago", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiasCredito", HeaderText = "Días Crédito", DataPropertyName = "DiasCredito", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CupoCredito", HeaderText = "Cupo Crédito", DataPropertyName = "CupoCredito", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "$#,##0", ForeColor = Color.FromArgb(16, 185, 129), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ListaPrecioDefecto", HeaderText = "Lista POS", DataPropertyName = "ListaPrecioDefecto", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(2, 132, 199), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) } });

            // Columna CheckBox Activo
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Estado", HeaderText = "Activo", DataPropertyName = "Estado", Width = 65 });
        }

        private void CargarDatosCompletos()
        {
            try
            {
                _listaCompletaClientes = _clienteService.ObtenerClientes();

                int totalActivos = _listaCompletaClientes.Count(c => c.Estado);
                lblTotalClientesVal.Text = totalActivos.ToString("N0");
                lblNuevosMesVal.Text = _listaCompletaClientes.Count(c => c.IdCliente >= (_listaCompletaClientes.Count - 15)).ToString();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AplicarFiltros()
        {
            string q = txtBuscar.Text.Trim().ToLower();
            string estado = cbFiltroEstado.SelectedItem?.ToString() ?? "Todos";
            string tipo = cbFiltroTipo.SelectedItem?.ToString() ?? "Todos";

            var filtrados = _listaCompletaClientes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                filtrados = filtrados.Where(c =>
                    (!string.IsNullOrEmpty(c.RazonSocial) && c.RazonSocial.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Rut) && c.Rut.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Giro) && c.Giro.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Telefono) && c.Telefono.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Comuna) && c.Comuna.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Email) && c.Email.ToLower().Contains(q))
                );
            }

            if (estado == "Activos") filtrados = filtrados.Where(c => c.Estado);
            else if (estado == "Inactivos") filtrados = filtrados.Where(c => !c.Estado);

            if (tipo != "Todos") filtrados = filtrados.Where(c => !string.IsNullOrEmpty(c.CategoriaCliente) && c.CategoriaCliente.Trim().Equals(tipo, StringComparison.OrdinalIgnoreCase));

            var listaFinal = filtrados.ToList();
            dgvClientes.DataSource = listaFinal;
            lblContadorFooter.Text = $"Mostrando {listaFinal.Count:N0} de {_listaCompletaClientes.Count:N0} clientes registrados";
        }

        private void DgvClientes_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvClientes.CurrentRow != null && dgvClientes.CurrentRow.DataBoundItem is Cliente cliente)
            {
                _clienteSeleccionado = cliente;
                btnEditar.Enabled = true;
                btnEstado.Enabled = true;
            }
            else
            {
                _clienteSeleccionado = null;
                btnEditar.Enabled = false;
                btnEstado.Enabled = false;
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            MostrarFormularioModal(null);
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            if (_clienteSeleccionado != null)
            {
                MostrarFormularioModal(_clienteSeleccionado);
            }
        }

        private void BtnEstado_Click(object? sender, EventArgs e)
        {
            if (_clienteSeleccionado == null) return;

            string accion = _clienteSeleccionado.Estado ? "desactivar" : "activar";
            var result = MessageBox.Show($"¿Desea {accion} al cliente {_clienteSeleccionado.RazonSocial}?", "Confirmar cambio de estado", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _clienteService.CambiarEstado(_clienteSeleccionado.IdCliente);
                    CargarDatosCompletos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar estado: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MostrarFormularioModal(Cliente? cliente)
        {
            using (FormNuevoClienteModal modal = new FormNuevoClienteModal(cliente))
            {
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    CargarDatosCompletos();
                }
            }
        }
    }
}