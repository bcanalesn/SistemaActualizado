using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
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

        // Botones de Acción
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private Button btnEliminar = null!;
        private Button btnEstado = null!;
        private Button btnPreciosEspeciales = null!;

        // Grilla y Footer
        private DataGridView dgvClientes = null!;
        private Label lblContadorFooter = null!;
        private Cliente? _clienteSeleccionado = null;

        public FormClientes()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            CargarDatosCompletos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            // =========================================================================
            // 1. ENCABEZADO Y BOTONES DE ACCIÓN (ALINEACIÓN LIMPIA EN UNA SOLA LÍNEA)
            // =========================================================================
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 60, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 0, 0, 10) 
            };

            Panel pnlTitulos = new Panel
            {
                Dock = DockStyle.Left,
                Width = 430,
                BackColor = Color.Transparent
            };

            Label lblTitulo = new Label
            {
                Text = "👥 Clientes",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Label lblSubtitulo = new Label
            {
                Text = "Administra la información de clientes y condiciones comerciales",
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlTitulos.Controls.Add(lblSubtitulo);
            pnlTitulos.Controls.Add(lblTitulo);

            FlowLayoutPanel pnlBotonesAccion = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };

            btnNuevo = CrearBoton("➕ Nuevo Cliente", Color.FromArgb(16, 185, 129), Color.White, new Size(135, 34), 6);
            btnNuevo.Margin = new Padding(4, 0, 0, 0);
            btnNuevo.Click += BtnNuevo_Click;

            btnEditar = CrearBoton("✏️ Editar", Color.FromArgb(2, 132, 199), Color.White, new Size(95, 34), 6);
            btnEditar.Margin = new Padding(4, 0, 0, 0);
            btnEditar.Enabled = false;
            btnEditar.Click += BtnEditar_Click;

            btnPreciosEspeciales = CrearBoton("💲 Precios Especiales", Color.FromArgb(124, 58, 237), Color.White, new Size(155, 34), 6);
            btnPreciosEspeciales.Margin = new Padding(4, 0, 0, 0);
            btnPreciosEspeciales.Enabled = false;
            btnPreciosEspeciales.Click += (s, e) =>
            {
                if (_clienteSeleccionado != null)
                {
                    using var modal = new FormPreciosEspecialesClienteModal(_clienteSeleccionado);
                    modal.ShowDialog(this);
                }
            };

            btnEstado = CrearBoton("🔄 Activar / Desactivar", Color.FromArgb(245, 158, 11), Color.White, new Size(165, 34), 6);
            btnEstado.Margin = new Padding(4, 0, 0, 0);
            btnEstado.Enabled = false;
            btnEstado.Click += BtnEstado_Click;

            btnEliminar = CrearBoton("🗑️ Eliminar", Color.FromArgb(239, 68, 68), Color.White, new Size(100, 34), 6);
            btnEliminar.Margin = new Padding(4, 0, 0, 0);
            btnEliminar.Enabled = false;
            btnEliminar.Click += BtnEliminar_Click;

            pnlBotonesAccion.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnPreciosEspeciales, btnEstado, btnEliminar });
            pnlHeader.Controls.Add(pnlBotonesAccion);
            pnlHeader.Controls.Add(pnlTitulos);

            // =========================================================================
            // 2. KPIS DASHBOARD (Responsivo, 92px con tarjetas de 80px netos)
            // =========================================================================
            Panel pnlKpiWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var cardTotal = CrearCardKPI("👥", Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), "Total Clientes", "0", "Activos en el sistema", out lblTotalClientesVal);
            var cardNuevos = CrearCardKPI("👤+", Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), "Nuevos este mes", "0", "+ Registrados recientemente", out lblNuevosMesVal);

            cardTotal.Margin = new Padding(0, 0, 6, 0);
            cardNuevos.Margin = new Padding(6, 0, 0, 0);

            tlpKpis.Controls.Add(cardTotal, 0, 0);
            tlpKpis.Controls.Add(cardNuevos, 1, 0);
            tlpKpis.Resize += (s, e) => tlpKpis.Invalidate(true);
            pnlKpiWrapper.Controls.Add(tlpKpis);

            // =========================================================================
            // 3. SECCIÓN FILTROS Y BÚSQUEDA
            // =========================================================================
            Panel pnlFiltrosWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            Panel pnlFiltrosCard = CrearTarjetaRedondeada(0, 0, 0, 72, Color.White, Color.FromArgb(226, 232, 240));
            pnlFiltrosCard.Dock = DockStyle.Fill;
            pnlFiltrosCard.Padding = new Padding(16, 12, 16, 12);

            FlowLayoutPanel flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            txtBuscar = new TextBox
            {
                Size = new Size(340, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "RUT, Razón Social, Giro, Teléfono o Comuna..."
            };
            txtBuscar.TextChanged += (s, e) => AplicarFiltros();
            Panel pnlGrpBuscar = CrearGrupoConCaja("BUSCAR CLIENTE", txtBuscar, 355);

            cbFiltroEstado = new ComboBox
            {
                Size = new Size(130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbFiltroEstado.Items.AddRange(new object[] { "Todos", "Activos", "Inactivos" });
            cbFiltroEstado.SelectedIndex = 0;
            cbFiltroEstado.SelectedIndexChanged += (s, e) => AplicarFiltros();
            Panel pnlGrpEstado = CrearGrupoLimpio("ESTADO", cbFiltroEstado, 135);

            cbFiltroTipo = new ComboBox
            {
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbFiltroTipo.Items.AddRange(new object[] { "Todos", "MINORISTA", "MAYOR.(A)", "MAYOR.(B)", "RUTEROS", "PERSONAL" });
            cbFiltroTipo.SelectedIndex = 0;
            cbFiltroTipo.SelectedIndexChanged += (s, e) => AplicarFiltros();
            Panel pnlGrpTipo = CrearGrupoLimpio("TIPO DE CLIENTE", cbFiltroTipo, 165);

            btnLimpiarFiltros = CrearBoton("🧹 Limpiar", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), new Size(95, 32), 6);
            btnLimpiarFiltros.Margin = new Padding(8, 14, 0, 0);
            btnLimpiarFiltros.Click += (s, e) =>
            {
                txtBuscar.Clear();
                cbFiltroEstado.SelectedIndex = 0;
                cbFiltroTipo.SelectedIndex = 0;
                AplicarFiltros();
            };

            flpFiltros.Controls.AddRange(new Control[] { pnlGrpBuscar, pnlGrpEstado, pnlGrpTipo, btnLimpiarFiltros });
            pnlFiltrosCard.Controls.Add(flpFiltros);
            pnlFiltrosWrapper.Controls.Add(pnlFiltrosCard);

            // =========================================================================
            // 4. GRILLA PRINCIPAL
            // =========================================================================
            Panel pnlCardGrilla = CrearTarjetaRedondeada(0, 0, 0, 0, Color.White, Color.FromArgb(226, 232, 240));
            pnlCardGrilla.Dock = DockStyle.Fill;
            pnlCardGrilla.Padding = new Padding(10);

            dgvClientes = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowTemplate = { Height = 34 }
            };

            ConfigurarEstiloTabla(dgvClientes);
            dgvClientes.SelectionChanged += DgvClientes_SelectionChanged;
            dgvClientes.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    BtnEditar_Click(s, e);
                }
            };

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(4, 6, 4, 0) };
            lblContadorFooter = new Label
            {
                Text = "Mostrando 0 clientes registrados",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlFooter.Controls.Add(lblContadorFooter);

            pnlCardGrilla.Controls.Add(dgvClientes);
            pnlCardGrilla.Controls.Add(pnlFooter);

            // Orden Z estricto
            pnlPrincipal.Controls.Add(pnlCardGrilla);
            pnlPrincipal.Controls.Add(pnlFiltrosWrapper);
            pnlPrincipal.Controls.Add(pnlKpiWrapper);
            pnlPrincipal.Controls.Add(pnlHeader);

            this.Controls.Add(pnlPrincipal);
            this.ResumeLayout(false);
        }

        private Panel CrearCardKPI(string icono, Color colorIconoFondo, Color colorIconoTexto, string titulo, string valorInicial, string subtitulo, out Label lblValor)
        {
            Panel card = CrearTarjetaRedondeada(0, 0, 0, 80, Color.White, Color.FromArgb(226, 232, 240));
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(12);

            Label lblIco = new Label
            {
                Text = icono,
                Location = new Point(12, 12),
                Size = new Size(36, 36),
                BackColor = colorIconoFondo,
                ForeColor = colorIconoTexto,
                Font = new Font("Segoe UI", 12F),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblTit = new Label
            {
                Text = titulo,
                Location = new Point(56, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            lblValor = new Label
            {
                Text = valorInicial,
                Location = new Point(54, 26),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            Label lblSub = new Label
            {
                Text = subtitulo,
                Location = new Point(56, 52),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            card.Controls.AddRange(new Control[] { lblIco, lblTit, lblValor, lblSub });
            return card;
        }

        private void ConfigurarEstiloTabla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(15, 23, 42);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.RowTemplate.Height = 34;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.GridColor = Color.FromArgb(226, 232, 240);

            dgv.Columns.Clear();

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
            
            dgv.Columns.Add(new DataGridViewTextBoxColumn { 
                Name = "CupoCredito", 
                HeaderText = "Cupo Crédito", 
                DataPropertyName = "CupoCredito", 
                Width = 110, 
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    FormatProvider = new System.Globalization.CultureInfo("es-CL"),
                    Format = "$ #,##0", 
                    Alignment = DataGridViewContentAlignment.MiddleRight, 
                    ForeColor = Color.FromArgb(16, 185, 129), 
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) 
                } 
            });
            
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ListaPrecioDefecto", HeaderText = "Lista POS", DataPropertyName = "ListaPrecioDefecto", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(2, 132, 199), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) } });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Estado", HeaderText = "Activo", DataPropertyName = "Estado", Width = 65 });
        }

        private void CargarDatosCompletos()
        {
            try
            {
                _listaCompletaClientes = _clienteService.ObtenerClientes();

                int totalActivos = _listaCompletaClientes.Count(c => c.Estado);
                lblTotalClientesVal.Text = totalActivos.ToString("N0", new System.Globalization.CultureInfo("es-CL"));
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
                btnEliminar.Enabled = true;
                btnPreciosEspeciales.Enabled = true;
                btnEstado.Enabled = true;
            }
            else
            {
                _clienteSeleccionado = null;
                btnEditar.Enabled = false;
                btnEliminar.Enabled = false;
                btnPreciosEspeciales.Enabled = false;
                btnEstado.Enabled = false;
            }
        }

        private void BtnNuevo_Click(object? sender, EventArgs e) => MostrarFormularioModal(null);

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

        private void BtnEliminar_Click(object? sender, EventArgs e)
        {
            if (_clienteSeleccionado == null && dgvClientes.CurrentRow != null)
            {
                _clienteSeleccionado = dgvClientes.CurrentRow.DataBoundItem as Cliente;
            }

            if (_clienteSeleccionado == null)
            {
                MessageBox.Show("Seleccione un cliente para eliminar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirmacion = MessageBox.Show(
                $"¿Está seguro de eliminar al cliente '{_clienteSeleccionado.RazonSocial}'?\n\nRUT: {_clienteSeleccionado.Rut}\n\nEsta acción lo dará de baja y ya no estará disponible para ventas.",
                "Confirmar Eliminación de Cliente",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirmacion == DialogResult.Yes)
            {
                try
                {
                    _clienteService.EliminarCliente(_clienteSeleccionado.IdCliente);
                    MessageBox.Show("Cliente eliminado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarDatosCompletos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar cliente: {ex.Message}", "Error DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private Panel CrearTarjetaRedondeada(int x, int y, int ancho, int alto, Color colorFondo, Color colorBorde)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(ancho, alto), BackColor = colorFondo };
            pnl.Resize += (s, e) => pnl.Invalidate();
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.BackColor);
                Rectangle r = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using GraphicsPath p = CrearRutaRedondeada(r, 10);
                using Pen pen = new Pen(colorBorde, 1.2f);
                e.Graphics.DrawPath(pen, p);
            };
            return pnl;
        }

        private GraphicsPath CrearRutaRedondeada(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Panel CrearGrupoConCaja(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            Panel pnlInputBox = new Panel { Location = new Point(0, 18), Size = new Size(ancho, 28), BackColor = Color.FromArgb(248, 250, 252) };
            pnlInputBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlInputBox.Width - 1, pnlInputBox.Height - 1);
                using GraphicsPath path = CrearRutaRedondeada(r, 6);
                using Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            control.Location = new Point(6, 3);
            control.Width = ancho - 12;
            pnlInputBox.Controls.Add(control);

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(pnlInputBox);
            return pnl;
        }

        private Panel CrearGrupoLimpio(string titulo, Control control, int ancho)
        {
            Panel pnl = new Panel { Size = new Size(ancho, 48), Margin = new Padding(0, 0, 8, 0) };
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(0, 0), AutoSize = true };

            control.Location = new Point(0, 18);
            control.Width = ancho;

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private Button CrearBoton(string texto, Color back, Color fore, Size size, int radio = 0)
        {
            Button b = new Button 
            { 
                Text = texto, 
                Size = size, 
                BackColor = back, 
                ForeColor = fore, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}