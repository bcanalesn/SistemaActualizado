using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
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
        private TextBox txtCodigoPostal = null!;
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
            this.Size = new Size(680, 680);
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
                Size = new Size(630, 305),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            int yc = 28;
            CrearCampoTexto(gbContacto, "Nombre Recibe (*):", ref txtNombre, 20, ref yc, 350);
            
            // Teléfono móvil (+56 y 9 dígitos = 12 caracteres máximos)
            CrearCampoTexto(gbContacto, "Teléfono Móvil (*):", ref txtTelefono, 390, ref yc, 220, salto: true);
            txtTelefono.MaxLength = 12;
            txtTelefono.PlaceholderText = "+56912345678";

            CrearCampoTexto(gbContacto, "Calle / Dirección (*):", ref txtDireccion, 20, ref yc, 400);
            CrearCampoTexto(gbContacto, "Número (*):", ref txtNumero, 440, ref yc, 170, salto: true);

            CrearCampoTexto(gbContacto, "Depto / Casa / Block:", ref txtDepto, 20, ref yc, 200);
            
            // Código postal opcional (hasta 7 dígitos)
            CrearCampoTexto(gbContacto, "Código Postal (Opcional):", ref txtCodigoPostal, 240, ref yc, 150);
            txtCodigoPostal.MaxLength = 7;
            txtCodigoPostal.PlaceholderText = "Ej: 8320000";

            CrearCampoTexto(gbContacto, "Punto de Referencia:", ref txtReferencia, 410, ref yc, 200, salto: true);

            CrearCampoTexto(gbContacto, "Instrucciones de Reparto:", ref txtInstrucciones, 20, ref yc, 590, salto: true);

            // ========================================================
            // RESTRICCIONES DE CARACTERES EN TIEMPO REAL (KeyPress)
            // ========================================================

            // 1. Nombre: Solo letras y espacios
            txtNombre.KeyPress += (s, e) =>
            {
                if (!char.IsLetter(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            // 2. Teléfono: Solo números y '+'
            txtTelefono.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '+')
                {
                    e.Handled = true;
                }
            };

            // 3. Calle / Dirección: Solo letras y espacios
            txtDireccion.KeyPress += (s, e) =>
            {
                if (!char.IsLetter(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            // 4. Número: Letras, números, espacio o guion
            txtNumero.KeyPress += (s, e) =>
            {
                if (!char.IsLetterOrDigit(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '-')
                {
                    e.Handled = true;
                }
            };

            // 5. Depto / Casa / Block: Solo letras y espacios
            txtDepto.KeyPress += (s, e) =>
            {
                if (!char.IsLetter(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            // 6. Código Postal: Solo números (hasta 7 dígitos)
            txtCodigoPostal.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            // 7. Punto de Referencia: Letras y números (alfanumérico y espacios)
            txtReferencia.KeyPress += (s, e) =>
            {
                if (!char.IsLetterOrDigit(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            // 8. Instrucciones: Letras, números, espacios, comas y puntos
            txtInstrucciones.KeyPress += (s, e) =>
            {
                if (!char.IsLetterOrDigit(e.KeyChar) && 
                    !char.IsWhiteSpace(e.KeyChar) && 
                    !char.IsControl(e.KeyChar) && 
                    e.KeyChar != ',' && 
                    e.KeyChar != '.')
                {
                    e.Handled = true;
                }
            };

            pnlBody.Controls.Add(gbContacto);
            y += 320;

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
                
                cmbComunas.SelectedIndexChanged -= CmbComunas_SelectedIndexChanged;
                cmbComunas.DataSource = comunas;
                cmbComunas.DisplayMember = "Nombre";
                cmbComunas.ValueMember = "ComunaDeliveryId";
                cmbComunas.SelectedIndexChanged += CmbComunas_SelectedIndexChanged;

                if (comunas.Count > 0)
                {
                    cmbComunas.SelectedIndex = 0;
                    
                    int comunaId = comunas[0].ComunaDeliveryId;
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
                else
                {
                    lblTarifaInfo.Text = "Costo Envío: -";
                    lblTiempoEstimado.Text = "Sin cobertura activa";
                    lblEstadoPedidoMinimo.Text = "No hay comunas habilitadas para esta zona.";
                    _tarifaActual = null;
                    btnConfirmar.Enabled = false;
                }
            }
        }

        private void CmbComunas_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int comunaId = 0;

            if (cmbComunas.SelectedValue is int idVal)
            {
                comunaId = idVal;
            }
            else if (cmbComunas.SelectedItem is ComunaDelivery cd)
            {
                comunaId = cd.ComunaDeliveryId;
            }

            if (comunaId <= 0) return;

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

            string telefono = txtTelefono.Text.Trim();
            if (string.IsNullOrWhiteSpace(telefono))
            {
                MessageBox.Show("Ingrese un teléfono de contacto para el repartidor.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTelefono.Focus();
                return;
            }

            // Validación de longitud de teléfono (+56 con 9 dígitos = 12 caracteres, o 9 dígitos solos)
            string soloNumeros = new string(telefono.Where(char.IsDigit).ToArray());
            if (telefono.StartsWith("+56"))
            {
                if (telefono.Length != 12 || soloNumeros.Length != 11)
                {
                    MessageBox.Show("El teléfono con prefijo debe tener el formato +56 seguido de 9 dígitos (ej: +56912345678).", "Teléfono Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTelefono.Focus();
                    return;
                }
            }
            else
            {
                if (soloNumeros.Length != 9)
                {
                    MessageBox.Show("El teléfono móvil debe tener exactamente 9 dígitos (ej: 912345678 o +56912345678).", "Teléfono Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTelefono.Focus();
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(txtDireccion.Text) || string.IsNullOrWhiteSpace(txtNumero.Text))
            {
                MessageBox.Show("Ingrese la calle y número del domicilio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDireccion.Focus();
                return;
            }

            int comunaId = 0;
            if (cmbComunas.SelectedValue is int idVal)
            {
                comunaId = idVal;
            }
            else if (cmbComunas.SelectedItem is ComunaDelivery cd)
            {
                comunaId = cd.ComunaDeliveryId;
            }

            // Si se ingresó código postal opcional, se concatena a la referencia o instrucciones
            string? referenciaFinal = string.IsNullOrWhiteSpace(txtReferencia.Text) ? null : txtReferencia.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtCodigoPostal.Text))
            {
                string cpTexto = $"[CP: {txtCodigoPostal.Text.Trim()}]";
                referenciaFinal = referenciaFinal != null ? $"{referenciaFinal} {cpTexto}" : cpTexto;
            }

            // Construcción del objeto Delivery con los datos del formulario
            DeliveryConfigurado = new Delivery
            {
                ZonaDeliveryId = _tarifaActual.ZonaId,
                ComunaDeliveryId = comunaId,
                NombreZona = _tarifaActual.NombreZona,
                NombreComuna = _tarifaActual.NombreComuna,
                NombreRecibe = txtNombre.Text.Trim(),
                Telefono = telefono,
                Direccion = txtDireccion.Text.Trim(),
                Numero = txtNumero.Text.Trim(),
                Departamento = string.IsNullOrWhiteSpace(txtDepto.Text) ? null : txtDepto.Text.Trim(),
                Referencia = referenciaFinal,
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