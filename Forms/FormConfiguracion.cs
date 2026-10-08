using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormConfiguracion : Form
    {
        private readonly ConfiguracionService _configService = new ConfiguracionService();
        private readonly DeliveryService _deliveryService = new DeliveryService();

        // --- Controles Pestaña Empresa ---
        private TextBox txtRut = null!;
        private TextBox txtRazonSocial = null!;
        private TextBox txtGiro = null!;
        private TextBox txtDireccion = null!;
        private TextBox txtComuna = null!;
        private TextBox txtCiudad = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtEmail = null!;
        private TextBox txtUnidadSII = null!;
        private TextBox txtResolucionSII = null!;
        private TextBox txtTextoPie = null!;
        private Button btnGuardarEmpresa = null!;

        // --- Controles Pestaña Delivery ---
        private DataGridView dgvZonas = null!;
        private DataGridView dgvComunas = null!;
        private TextBox txtZonaNombre = null!;
        private NumericUpDown numCostoEnvio = null!;
        private NumericUpDown numPedidoMinimo = null!;
        private NumericUpDown numEnvioGratis = null!;
        private CheckBox chkZonaActiva = null!;
        private Button btnGuardarZona = null!;
        private Button btnNuevaZona = null!;

        private TextBox txtComunaNombre = null!;
        private NumericUpDown numTiempoMinutos = null!;
        private CheckBox chkComunaActiva = null!;
        private Button btnGuardarComuna = null!;
        private Button btnNuevaComuna = null!;

        private int _zonaSeleccionadaId = 0;
        private int _comunaSeleccionadaId = 0;

        public FormConfiguracion()
        {
            InitializeComponent();
            CargarDatosEmpresa();
            CargarZonasDelivery();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuración General del Negocio";
            this.Size = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            // Header Principal
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "⚙️ PANEL DE CONFIGURACIÓN DEL SISTEMA",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 16)
            };
            pnlHeader.Controls.Add(lblTitulo);

            // TabControl Principal
            TabControl tabPrincipal = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Point(14, 8)
            };

            TabPage tabEmpresa = new TabPage("🏢 Datos de Empresa");
            tabEmpresa.BackColor = Color.FromArgb(248, 250, 252);
            ConstruirPestañaEmpresa(tabEmpresa);

            TabPage tabDelivery = new TabPage("🚚 Zonas y Cobertura Delivery");
            tabDelivery.BackColor = Color.FromArgb(248, 250, 252);
            ConstruirPestañaDelivery(tabDelivery);

            tabPrincipal.TabPages.Add(tabEmpresa);
            tabPrincipal.TabPages.Add(tabDelivery);

            this.Controls.Add(tabPrincipal);
            this.Controls.Add(pnlHeader);
        }

        // ==========================================
        // 1. PESTAÑA: DATOS DE EMPRESA
        // ==========================================
        private void ConstruirPestañaEmpresa(TabPage tab)
        {
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                AutoScroll = true
            };

            int y = 15;
            CrearCampoEmpresa(pnlBody, "RUT Emisor:", ref txtRut, ref y, "76.543.210-K");
            CrearCampoEmpresa(pnlBody, "Razón Social / Comercial:", ref txtRazonSocial, ref y, "Mi Negocio SpA");
            CrearCampoEmpresa(pnlBody, "Giro Comercial:", ref txtGiro, ref y, "Venta al por menor");
            CrearCampoEmpresa(pnlBody, "Dirección (Casa Matriz):", ref txtDireccion, ref y, "Av. Siempre Viva 123");
            CrearCampoEmpresa(pnlBody, "Comuna:", ref txtComuna, ref y, "Santiago");
            CrearCampoEmpresa(pnlBody, "Ciudad:", ref txtCiudad, ref y, "Santiago");
            CrearCampoEmpresa(pnlBody, "Teléfono de Contacto:", ref txtTelefono, ref y, "+56 9 1234 5678");
            CrearCampoEmpresa(pnlBody, "Correo Electrónico:", ref txtEmail, ref y, "contacto@empresa.cl");
            CrearCampoEmpresa(pnlBody, "Unidad S.I.I.:", ref txtUnidadSII, ref y, "S.I.I. - SANTIAGO CENTRO");
            CrearCampoEmpresa(pnlBody, "Resolución S.I.I.:", ref txtResolucionSII, ref y, "Res. 99 de 2026");
            CrearCampoEmpresa(pnlBody, "Mensaje al Pie:", ref txtTextoPie, ref y, "¡Gracias por su preferencia!");

            btnGuardarEmpresa = new Button
            {
                Text = "💾 Guardar Parámetros de Empresa",
                Location = new Point(220, y + 10),
                Size = new Size(380, 40),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardarEmpresa.FlatAppearance.BorderSize = 0;
            btnGuardarEmpresa.Click += BtnGuardarEmpresa_Click;
            pnlBody.Controls.Add(btnGuardarEmpresa);

            tab.Controls.Add(pnlBody);
        }

        private void CrearCampoEmpresa(Panel pnl, string labelText, ref TextBox txt, ref int y, string placeholder)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(15, y + 3),
                Size = new Size(190, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txt = new TextBox
            {
                Location = new Point(220, y),
                Size = new Size(380, 26),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = placeholder
            };

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            y += 35;
        }

        // ==========================================
        // 2. PESTAÑA: GESTIÓN DE DELIVERY
        // ==========================================
        private void ConstruirPestañaDelivery(TabPage tab)
        {
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 480,
                Padding = new Padding(10)
            };

            // PANEL IZQUIERDO: ZONAS
            GroupBox gbZonas = new GroupBox
            {
                Text = "📍 Macro Zonas de Reparto",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10)
            };

            dgvZonas = CrearGridBasico();
            dgvZonas.Dock = DockStyle.Top;
            dgvZonas.Height = 220;
            dgvZonas.Columns.Add("Id", "ID");
            dgvZonas.Columns["Id"]!.Visible = false;
            dgvZonas.Columns.Add("Nombre", "Zona");
            dgvZonas.Columns.Add("Costo", "Flete ($)");
            dgvZonas.Columns.Add("Minimo", "Mínimo ($)");
            dgvZonas.Columns.Add("Gratis", "Gratis Desde ($)");
            dgvZonas.Columns.Add("Activo", "Activo");
            dgvZonas.SelectionChanged += DgvZonas_SelectionChanged;

            Panel pnlEditorZona = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yz = 10;

            pnlEditorZona.Controls.Add(new Label { Text = "Nombre Zona:", Location = new Point(5, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
            txtZonaNombre = new TextBox { Location = new Point(130, yz), Size = new Size(200, 24), Font = new Font("Segoe UI", 9F) };
            pnlEditorZona.Controls.Add(txtZonaNombre);
            yz += 32;

            pnlEditorZona.Controls.Add(new Label { Text = "Costo Envío ($):", Location = new Point(5, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
            numCostoEnvio = new NumericUpDown { Location = new Point(130, yz), Size = new Size(130, 24), Maximum = 100000, Increment = 500, ThousandsSeparator = true };
            pnlEditorZona.Controls.Add(numCostoEnvio);
            yz += 32;

            pnlEditorZona.Controls.Add(new Label { Text = "Pedido Mínimo ($):", Location = new Point(5, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
            numPedidoMinimo = new NumericUpDown { Location = new Point(130, yz), Size = new Size(130, 24), Maximum = 500000, Increment = 1000, ThousandsSeparator = true };
            pnlEditorZona.Controls.Add(numPedidoMinimo);
            yz += 32;

            pnlEditorZona.Controls.Add(new Label { Text = "Envío Gratis Desde:", Location = new Point(5, yz + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
            numEnvioGratis = new NumericUpDown { Location = new Point(130, yz), Size = new Size(130, 24), Maximum = 500000, Increment = 1000, ThousandsSeparator = true };
            pnlEditorZona.Controls.Add(numEnvioGratis);
            yz += 32;

            chkZonaActiva = new CheckBox { Text = "Zona Habilitada", Location = new Point(130, yz), AutoSize = true, Checked = true };
            pnlEditorZona.Controls.Add(chkZonaActiva);
            yz += 35;

            btnGuardarZona = new Button
            {
                Text = "💾 Guardar Zona",
                Location = new Point(130, yz),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardarZona.FlatAppearance.BorderSize = 0;
            btnGuardarZona.Click += BtnGuardarZona_Click;

            btnNuevaZona = new Button
            {
                Text = "➕ Nueva Zona",
                Location = new Point(270, yz),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNuevaZona.FlatAppearance.BorderSize = 0;
            btnNuevaZona.Click += (s, e) => LimpiarFormularioZona();

            pnlEditorZona.Controls.Add(btnGuardarZona);
            pnlEditorZona.Controls.Add(btnNuevaZona);

            gbZonas.Controls.Add(pnlEditorZona);
            gbZonas.Controls.Add(dgvZonas);
            split.Panel1.Controls.Add(gbZonas);

            // PANEL DERECHO: COMUNAS
            GroupBox gbComunas = new GroupBox
            {
                Text = "🏘️ Comunas Asignadas a la Zona",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10)
            };

            dgvComunas = CrearGridBasico();
            dgvComunas.Dock = DockStyle.Top;
            dgvComunas.Height = 220;
            dgvComunas.Columns.Add("Id", "ID");
            dgvComunas.Columns["Id"]!.Visible = false;
            dgvComunas.Columns.Add("Nombre", "Comuna");
            dgvComunas.Columns.Add("Minutos", "T. Estimado (Min)");
            dgvComunas.Columns.Add("Activo", "Activo");
            dgvComunas.SelectionChanged += DgvComunas_SelectionChanged;

            Panel pnlEditorComuna = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yc = 15;

            pnlEditorComuna.Controls.Add(new Label { Text = "Nombre Comuna:", Location = new Point(5, yc + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
            txtComunaNombre = new TextBox { Location = new Point(140, yc), Size = new Size(220, 24), Font = new Font("Segoe UI", 9F) };
            pnlEditorComuna.Controls.Add(txtComunaNombre);
            yc += 35;

            pnlEditorComuna.Controls.Add(new Label { Text = "Tiempo Est. (min):", Location = new Point(5, yc + 3), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
            numTiempoMinutos = new NumericUpDown { Location = new Point(140, yc), Size = new Size(90, 24), Minimum = 10, Maximum = 180, Value = 45 };
            pnlEditorComuna.Controls.Add(numTiempoMinutos);
            yc += 35;

            chkComunaActiva = new CheckBox { Text = "Comuna Habilitada", Location = new Point(140, yc), AutoSize = true, Checked = true };
            pnlEditorComuna.Controls.Add(chkComunaActiva);
            yc += 38;

            btnGuardarComuna = new Button
            {
                Text = "💾 Guardar Comuna",
                Location = new Point(140, yc),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardarComuna.FlatAppearance.BorderSize = 0;
            btnGuardarComuna.Click += BtnGuardarComuna_Click;

            btnNuevaComuna = new Button
            {
                Text = "➕ Nueva Comuna",
                Location = new Point(280, yc),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNuevaComuna.FlatAppearance.BorderSize = 0;
            btnNuevaComuna.Click += (s, e) => LimpiarFormularioComuna();

            pnlEditorComuna.Controls.Add(btnGuardarComuna);
            pnlEditorComuna.Controls.Add(btnNuevaComuna);

            gbComunas.Controls.Add(pnlEditorComuna);
            gbComunas.Controls.Add(dgvComunas);
            split.Panel2.Controls.Add(gbComunas);

            tab.Controls.Add(split);
        }

        private DataGridView CrearGridBasico()
        {
            var grid = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        // ==========================================
        // 3. LÓGICA DE NEGOCIO Y PERSISTENCIA
        // ==========================================
        private void CargarDatosEmpresa()
        {
            try
            {
                var emp = _configService.ObtenerDatosEmpresa();
                txtRut.Text = emp.Rut;
                txtRazonSocial.Text = emp.RazonSocial;
                txtGiro.Text = emp.Giro;
                txtDireccion.Text = emp.Direccion;
                txtComuna.Text = emp.Comuna;
                txtCiudad.Text = emp.Ciudad;
                txtTelefono.Text = emp.Telefono;
                txtEmail.Text = emp.Email ?? "";
                txtUnidadSII.Text = emp.UnidadSII;
                txtResolucionSII.Text = emp.ResolucionSII;
                txtTextoPie.Text = emp.TextoPieTicket;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos de empresa: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardarEmpresa_Click(object? sender, EventArgs e)
        {
            try
            {
                var datos = new ConfiguracionEmpresa
                {
                    EmpresaID = 1,
                    Rut = txtRut.Text.Trim(),
                    RazonSocial = txtRazonSocial.Text.Trim(),
                    Giro = txtGiro.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    Comuna = txtComuna.Text.Trim(),
                    Ciudad = txtCiudad.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    UnidadSII = txtUnidadSII.Text.Trim(),
                    ResolucionSII = txtResolucionSII.Text.Trim(),
                    TextoPieTicket = txtTextoPie.Text.Trim()
                };

                _configService.GuardarDatosEmpresa(datos);
                MessageBox.Show("Datos fiscales guardados con éxito.", "Empresa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatosEmpresa();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar empresa: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarZonasDelivery()
        {
            try
            {
                dgvZonas.SelectionChanged -= DgvZonas_SelectionChanged;
                dgvZonas.Rows.Clear();
                var zonas = _deliveryService.ObtenerZonasActivas();

                foreach (var z in zonas)
                {
                    dgvZonas.Rows.Add(z.ZonaDeliveryId, z.Nombre, z.CostoEnvio, z.PedidoMinimo, z.MontoEnvioGratis, z.Activo ? "Sí" : "No");
                }

                dgvZonas.SelectionChanged += DgvZonas_SelectionChanged;

                if (dgvZonas.Rows.Count > 0)
                {
                    dgvZonas.ClearSelection();
                    dgvZonas.Rows[0].Selected = true;
                    dgvZonas.CurrentCell = dgvZonas.Rows[0].Cells["Nombre"];
                    CargarDatosFilaZonaSeleccionada(dgvZonas.Rows[0]);
                }
                else
                {
                    LimpiarFormularioZona();
                    dgvComunas.Rows.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar zonas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvZonas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvZonas.CurrentRow == null) return;
            CargarDatosFilaZonaSeleccionada(dgvZonas.CurrentRow);
        }

        private void CargarDatosFilaZonaSeleccionada(DataGridViewRow fila)
        {
            if (fila.Cells["Id"].Value == null) return;

            _zonaSeleccionadaId = Convert.ToInt32(fila.Cells["Id"].Value);
            txtZonaNombre.Text = fila.Cells["Nombre"].Value?.ToString() ?? "";

            decimal costo = Convert.ToDecimal(fila.Cells["Costo"].Value ?? 0);
            decimal minimo = Convert.ToDecimal(fila.Cells["Minimo"].Value ?? 0);
            decimal gratis = Convert.ToDecimal(fila.Cells["Gratis"].Value ?? 0);

            numCostoEnvio.Value = Math.Min(numCostoEnvio.Maximum, Math.Max(numCostoEnvio.Minimum, costo));
            numPedidoMinimo.Value = Math.Min(numPedidoMinimo.Maximum, Math.Max(numPedidoMinimo.Minimum, minimo));
            numEnvioGratis.Value = Math.Min(numEnvioGratis.Maximum, Math.Max(numEnvioGratis.Minimum, gratis));
            chkZonaActiva.Checked = (fila.Cells["Activo"].Value?.ToString() == "Sí");

            CargarComunasDeZona(_zonaSeleccionadaId);
        }

        private void CargarComunasDeZona(int zonaId)
        {
            try
            {
                dgvComunas.SelectionChanged -= DgvComunas_SelectionChanged;
                dgvComunas.Rows.Clear();
                LimpiarFormularioComuna();

                using var db = new SISTEMAACTUALIZADO.Data.AppDbContext();
                var comunas = db.ComunasDelivery
                    .Where(c => c.ZonaDeliveryId == zonaId)
                    .OrderBy(c => c.Nombre)
                    .ToList();

                foreach (var c in comunas)
                {
                    dgvComunas.Rows.Add(c.ComunaDeliveryId, c.Nombre, c.TiempoEstimadoMinutos, c.Activo ? "Sí" : "No");
                }

                dgvComunas.SelectionChanged += DgvComunas_SelectionChanged;

                if (dgvComunas.Rows.Count > 0)
                {
                    dgvComunas.ClearSelection();
                    dgvComunas.Rows[0].Selected = true;
                    dgvComunas.CurrentCell = dgvComunas.Rows[0].Cells["Nombre"];
                    CargarDatosFilaComunaSeleccionada(dgvComunas.Rows[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar comunas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvComunas_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvComunas.CurrentRow == null) return;
            CargarDatosFilaComunaSeleccionada(dgvComunas.CurrentRow);
        }

        private void CargarDatosFilaComunaSeleccionada(DataGridViewRow fila)
        {
            if (fila.Cells["Id"].Value == null) return;

            _comunaSeleccionadaId = Convert.ToInt32(fila.Cells["Id"].Value);
            txtComunaNombre.Text = fila.Cells["Nombre"].Value?.ToString() ?? "";
            
            int minutos = Convert.ToInt32(fila.Cells["Minutos"].Value ?? 45);
            numTiempoMinutos.Value = Math.Min(numTiempoMinutos.Maximum, Math.Max(numTiempoMinutos.Minimum, minutos));
            chkComunaActiva.Checked = (fila.Cells["Activo"].Value?.ToString() == "Sí");
        }

        private void BtnGuardarZona_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtZonaNombre.Text))
            {
                MessageBox.Show("Ingrese el nombre de la zona.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var zona = new ZonaDelivery
                {
                    ZonaDeliveryId = _zonaSeleccionadaId,
                    Nombre = txtZonaNombre.Text.Trim(),
                    CostoEnvio = numCostoEnvio.Value,
                    PedidoMinimo = numPedidoMinimo.Value,
                    MontoEnvioGratis = numEnvioGratis.Value,
                    Activo = chkZonaActiva.Checked
                };

                _deliveryService.GuardarZona(zona);
                MessageBox.Show("Zona guardada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarZonasDelivery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar zona: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardarComuna_Click(object? sender, EventArgs e)
        {
            if (_zonaSeleccionadaId == 0)
            {
                MessageBox.Show("Debe seleccionar una macro zona antes de agregar o editar una comuna.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtComunaNombre.Text))
            {
                MessageBox.Show("Ingrese el nombre de la comuna.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var comuna = new ComunaDelivery
                {
                    ComunaDeliveryId = _comunaSeleccionadaId,
                    ZonaDeliveryId = _zonaSeleccionadaId,
                    Nombre = txtComunaNombre.Text.Trim(),
                    TiempoEstimadoMinutos = (int)numTiempoMinutos.Value,
                    Activo = chkComunaActiva.Checked
                };

                _deliveryService.GuardarComuna(comuna);
                MessageBox.Show("Comuna guardada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarComunasDeZona(_zonaSeleccionadaId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar comuna: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarFormularioZona()
        {
            _zonaSeleccionadaId = 0;
            txtZonaNombre.Clear();
            numCostoEnvio.Value = 0;
            numPedidoMinimo.Value = 0;
            numEnvioGratis.Value = 0;
            chkZonaActiva.Checked = true;
            txtZonaNombre.Focus();
        }

        private void LimpiarFormularioComuna()
        {
            _comunaSeleccionadaId = 0;
            txtComunaNombre.Clear();
            numTiempoMinutos.Value = 45;
            chkComunaActiva.Checked = true;
            txtComunaNombre.Focus();
        }
    }
}