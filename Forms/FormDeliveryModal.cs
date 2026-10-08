using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Forms
{
    public class FormDeliveryModal : Form
    {
        private readonly DeliveryService _deliveryService = new DeliveryService();
        private readonly decimal _subtotalProductos;

        // Propiedad que retorna el objeto Delivery configurado
        public Delivery? DeliveryConfigurado { get; private set; }

        // Controles de Selección de Cobertura
        private ComboBox cmbZonas = null!;
        private ComboBox cmbComunas = null!;
        private Label lblTarifaInfo = null!;
        private Label lblTiempoEstimado = null!;
        private Label lblEstadoPedidoMinimo = null!;

        // Controles de Datos del Receptor
        private TextBox txtNombre = null!;
        private TextBox txtTelefono = null!;
        private TextBox txtDireccion = null!;
        private TextBox txtNumero = null!;
        private TextBox txtDepto = null!;
        private TextBox txtReferencia = null!;
        private TextBox txtInstrucciones = null!;

        private Button btnConfirmar = null!;
        private Button btnCancelar = null!;

        private CalculoTarifaDeliveryDTO? _tarifaActual;

        public FormDeliveryModal(decimal subtotalProductos, Delivery? datosPrevios = null)
        {
            _subtotalProductos = subtotalProductos;
            InitializeComponent();
            CargarZonas();

            if (datosPrevios != null)
            {
                CargarDatosPrevios(datosPrevios);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Configurar Despacho a Domicilio (Delivery)";
            this.Size = new Size(680, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            // Header Superior
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "🚚 DATOS DE DESPACHO Y COBERTURA",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 18)
            };
            pnlHeader.Controls.Add(lblTitulo);

            // Contenedor Central
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            int y = 10;

            // Grupo 1: Ubicación y Tarifa
            GroupBox gbUbicacion = new GroupBox
            {
                Text = "Zona y Comuna de Entrega",
                Location = new Point(15, y),
                Size = new Size(630, 155),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            gbUbicacion.Controls.Add(new Label { Text = "Macro Zona:", Location = new Point(20, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) });
            cmbZonas = new ComboBox
            {
                Location = new Point(110, 25),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbZonas.SelectedIndexChanged += CmbZonas_SelectedIndexChanged;
            gbUbicacion.Controls.Add(cmbZonas);

            gbUbicacion.Controls.Add(new Label { Text = "Comuna:", Location = new Point(330, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) });
            cmbComunas = new ComboBox
            {
                Location = new Point(400, 25),
                Size = new Size(210, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbComunas.SelectedIndexChanged += CmbComunas_SelectedIndexChanged;
            gbUbicacion.Controls.Add(cmbComunas);

            lblTarifaInfo = new Label
            {
                Text = "Costo Envío: $0",
                Location = new Point(20, 65),
                Size = new Size(280, 22),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 255)
            };
            gbUbicacion.Controls.Add(lblTarifaInfo);

            lblTiempoEstimado = new Label
            {
                Text = "Tiempo Estimado: ~45 min",
                Location = new Point(330, 65),
                Size = new Size(280, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            gbUbicacion.Controls.Add(lblTiempoEstimado);

            lblEstadoPedidoMinimo = new Label
            {
                Text = "",
                Location = new Point(20, 95),
                Size = new Size(590, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38)
            };
            gbUbicacion.Controls.Add(lblEstadoPedidoMinimo);

            pnlBody.Controls.Add(gbUbicacion);
            y += 170;

            // Grupo 2: Datos de Contacto y Dirección
            GroupBox gbContacto = new GroupBox
            {
                Text = "Información del Destinatario",
                Location = new Point(15, y),
                Size = new Size(630, 270),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            int yc = 28;
            CrearCampoTexto(gbContacto, "Nombre Recibe (*):", ref txtNombre, 20, ref yc, 350);
            CrearCampoTexto(gbContacto, "Teléfono Móvil (*):", ref txtTelefono, 390, ref yc, 220, salto: true);

            CrearCampoTexto(gbContacto, "Calle / Dirección (*):", ref txtDireccion, 20, ref yc, 400);
            CrearCampoTexto(gbContacto, "Número (*):", ref txtNumero, 440, ref yc, 170, salto: true);

            CrearCampoTexto(gbContacto, "Depto / Casa / Block:", ref txtDepto, 20, ref yc, 200);
            CrearCampoTexto(gbContacto, "Punto de Referencia:", ref txtReferencia, 240, ref yc, 370, salto: true);

            CrearCampoTexto(gbContacto, "Instrucciones de Reparto:", ref txtInstrucciones, 20, ref yc, 590, salto: true);

            pnlBody.Controls.Add(gbContacto);
            y += 285;

            // Botones de Acción
            btnConfirmar = new Button
            {
                Text = "✔️ Confirmar y Asignar Delivery",
                Location = new Point(345, y),
                Size = new Size(300, 44),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += BtnConfirmar_Click;

            btnCancelar = new Button
            {
                Text = "✖ Cancelar",
                Location = new Point(185, y),
                Size = new Size(150, 44),
                BackColor = Color.FromArgb(148, 163, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlBody.Controls.Add(btnConfirmar);
            pnlBody.Controls.Add(btnCancelar);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlHeader);
        }

        private void CrearCampoTexto(GroupBox gb, string label, ref TextBox txt, int x, ref int y, int width, bool salto = false)
        {
            gb.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            });

            txt = new TextBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, 24),
                Font = new Font("Segoe UI", 9F)
            };
            gb.Controls.Add(txt);

            if (salto) y += 55;
        }

        private void CargarZonas()
        {
            var zonas = _deliveryService.ObtenerZonasActivas();
            cmbZonas.DataSource = zonas;
            cmbZonas.DisplayMember = "Nombre";
            cmbZonas.ValueMember = "ZonaDeliveryId";

            if (zonas.Count > 0)
            {
                cmbZonas.SelectedIndex = 0;
            }
        }

        private void CmbZonas_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbZonas.SelectedItem is ZonaDelivery zona)
            {
                var comunas = zona.Comunas.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
                cmbComunas.DataSource = comunas;
                cmbComunas.DisplayMember = "Nombre";
                cmbComunas.ValueMember = "ComunaDeliveryId";

                if (comunas.Count > 0)
                {
                    cmbComunas.SelectedIndex = 0;
                }
                else
                {
                    lblTarifaInfo.Text = "Costo Envío: -";
                    lblTiempoEstimado.Text = "Sin cobertura activa";
                    lblEstadoPedidoMinimo.Text = "No hay comunas habilitadas para esta zona.";
                    _tarifaActual = null;
                }
            }
        }

        private void CmbComunas_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbComunas.SelectedValue is int comunaId && comunaId > 0)
            {
                _tarifaActual = _deliveryService.EvaluarDelivery(comunaId, _subtotalProductos);

                if (!_tarifaActual.EsValido)
                {
                    lblTarifaInfo.Text = "Envío No Habilitado";
                    lblTarifaInfo.ForeColor = Color.FromArgb(220, 38, 38);
                    lblEstadoPedidoMinimo.Text = _tarifaActual.MensajeError;
                    lblEstadoPedidoMinimo.ForeColor = Color.FromArgb(220, 38, 38);
                    btnConfirmar.Enabled = false;
                }
                else
                {
                    if (_tarifaActual.AplicaEnvioGratis)
                    {
                        lblTarifaInfo.Text = "Costo Envío: ¡GRATIS! 🎉";
                        lblTarifaInfo.ForeColor = Color.FromArgb(16, 185, 129);
                        lblEstadoPedidoMinimo.Text = "Aplica despacho bonificado por superar el monto requerido.";
                        lblEstadoPedidoMinimo.ForeColor = Color.FromArgb(16, 185, 129);
                    }
                    else
                    {
                        lblTarifaInfo.Text = $"Costo Envío: ${_tarifaActual.CostoFinal:N0}";
                        lblTarifaInfo.ForeColor = Color.FromArgb(0, 102, 255);

                        if (_tarifaActual.MontoFaltanteEnvioGratis > 0)
                        {
                            lblEstadoPedidoMinimo.Text = $"💡 Agrega ${_tarifaActual.MontoFaltanteEnvioGratis:N0} más para despacho gratuito.";
                            lblEstadoPedidoMinimo.ForeColor = Color.FromArgb(100, 116, 139);
                        }
                        else
                        {
                            lblEstadoPedidoMinimo.Text = "";
                        }
                    }

                    lblTiempoEstimado.Text = $"Tiempo Estimado: ~{_tarifaActual.TiempoEstimadoMinutos} min";
                    btnConfirmar.Enabled = true;
                }
            }
        }

        private void BtnConfirmar_Click(object? sender, EventArgs e)
        {
            if (_tarifaActual == null || !_tarifaActual.EsValido)
            {
                MessageBox.Show("Debe seleccionar una zona y comuna con cobertura válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese el nombre de la persona que recibe.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("Ingrese un teléfono de contacto para el repartidor.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTelefono.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDireccion.Text) || string.IsNullOrWhiteSpace(txtNumero.Text))
            {
                MessageBox.Show("Ingrese la calle y número del domicilio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDireccion.Focus();
                return;
            }

            // Construcción del objeto Delivery con los datos del formulario
            DeliveryConfigurado = new Delivery
            {
                ZonaDeliveryId = _tarifaActual.ZonaId,
                ComunaDeliveryId = (int)cmbComunas.SelectedValue,
                NombreZona = _tarifaActual.NombreZona,
                NombreComuna = _tarifaActual.NombreComuna,
                NombreRecibe = txtNombre.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Direccion = txtDireccion.Text.Trim(),
                Numero = txtNumero.Text.Trim(),
                Departamento = string.IsNullOrWhiteSpace(txtDepto.Text) ? null : txtDepto.Text.Trim(),
                Referencia = string.IsNullOrWhiteSpace(txtReferencia.Text) ? null : txtReferencia.Text.Trim(),
                Instrucciones = string.IsNullOrWhiteSpace(txtInstrucciones.Text) ? null : txtInstrucciones.Text.Trim(),
                CostoDelivery = _tarifaActual.CostoFinal,
                TiempoEstimadoMinutos = _tarifaActual.TiempoEstimadoMinutos
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CargarDatosPrevios(Delivery d)
        {
            txtNombre.Text = d.NombreRecibe;
            txtTelefono.Text = d.Telefono;
            txtDireccion.Text = d.Direccion;
            txtNumero.Text = d.Numero;
            txtDepto.Text = d.Departamento ?? "";
            txtReferencia.Text = d.Referencia ?? "";
            txtInstrucciones.Text = d.Instrucciones ?? "";

            cmbZonas.SelectedValue = d.ZonaDeliveryId;
            cmbComunas.SelectedValue = d.ComunaDeliveryId;
        }
    }
}