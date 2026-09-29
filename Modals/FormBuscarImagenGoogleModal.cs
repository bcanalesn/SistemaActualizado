using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormBuscarImagenGoogleModal : Form
    {
        private readonly string _sku;
        private static readonly HttpClient _httpClient = new HttpClient();

        private TextBox txtBusqueda = null!;
        private Button btnBuscar = null!;
        private Label lblEstado = null!;
        private FlowLayoutPanel flowResultados = null!;
        private Button btnAnterior = null!;
        private Button btnSiguiente = null!;
        private Label lblPagina = null!;
        private Button btnCancelar = null!;

        private List<ResultadoImagen> _imagenesEncontradas = new List<ResultadoImagen>();
        private int _paginaActual = 1;
        private const int ImagenesPorPagina = 3;

        public string RutaImagenDescargada { get; private set; } = string.Empty;

        public FormBuscarImagenGoogleModal(string sugerencia, string sku)
        {
            _sku = sku;
            InitializeComponent(sugerencia);

            this.Shown += async (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtBusqueda.Text))
                {
                    await EjecutarBusquedaAsync();
                }
            };
        }

        private void InitializeComponent(string sugerencia)
        {
            this.Text = "🔍 Buscar Imagen en la Web";
            this.Size = new Size(760, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(20, 14, 20, 4) };

            Label lblTitulo = new Label
            {
                Text = "Buscar foto de producto en catálogo en línea:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Top,
                Height = 22
            };

            Panel pnlInput = new Panel { Dock = DockStyle.Top, Height = 36 };
            txtBusqueda = new TextBox
            {
                Location = new Point(0, 3),
                Size = new Size(570, 30),
                Font = new Font("Segoe UI", 10.5F),
                Text = sugerencia
            };
            txtBusqueda.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await EjecutarBusquedaAsync();
                }
            };

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(580, 2),
                Size = new Size(135, 32),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += async (s, e) => await EjecutarBusquedaAsync();

            pnlInput.Controls.Add(txtBusqueda);
            pnlInput.Controls.Add(btnBuscar);

            lblEstado = new Label
            {
                Text = "Escriba el nombre del producto y presione Buscar",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(16, 185, 129),
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTop.Controls.Add(lblEstado);
            pnlTop.Controls.Add(pnlInput);
            pnlTop.Controls.Add(lblTitulo);

            flowResultados = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 6, 15, 6),
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.White
            };

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(20, 8, 20, 8) };

            btnAnterior = new Button
            {
                Text = "◀ Anteriores",
                Size = new Size(120, 34),
                Location = new Point(20, 10),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnterior.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnAnterior.Click += (s, e) => CambiarPagina(-1);

            lblPagina = new Label
            {
                Text = "Pág. 1 de 1",
                Location = new Point(150, 10),
                Size = new Size(140, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            btnSiguiente = new Button
            {
                Text = "Siguientes ▶",
                Size = new Size(120, 34),
                Location = new Point(300, 10),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnSiguiente.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnSiguiente.Click += (s, e) => CambiarPagina(1);

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(95, 34),
                Location = new Point(625, 10),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) => this.Close();

            pnlBottom.Controls.AddRange(new Control[] { btnAnterior, lblPagina, btnSiguiente, btnCancelar });

            this.Controls.Add(flowResultados);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private async Task EjecutarBusquedaAsync()
        {
            string termino = txtBusqueda.Text.Trim();
            if (string.IsNullOrWhiteSpace(termino)) return;

            btnBuscar.Enabled = false;
            lblEstado.Text = "Buscando en catálogo en línea...";
            lblEstado.ForeColor = Color.FromArgb(2, 132, 199);
            flowResultados.Controls.Clear();

            try
            {
                _imagenesEncontradas = await BuscarEnCatalogoAsync(termino);

                if (_imagenesEncontradas.Count == 0)
                {
                    lblEstado.Text = "No se encontraron imágenes en el catálogo para esta búsqueda.";
                    lblEstado.ForeColor = Color.FromArgb(220, 38, 38);
                    btnAnterior.Enabled = false;
                    btnSiguiente.Enabled = false;
                    lblPagina.Text = "Pág. 0 de 0";
                    return;
                }

                _paginaActual = 1;
                RenderizarPaginaActual();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error: {ex.Message}";
                lblEstado.ForeColor = Color.FromArgb(220, 38, 38);
            }
            finally
            {
                btnBuscar.Enabled = true;
            }
        }

        private async Task<List<ResultadoImagen>> BuscarEnCatalogoAsync(string query)
        {
            var resultados = new List<ResultadoImagen>();
            var urlsVistas = new HashSet<string>();

            // Consultar Open Food Facts (API abierta de productos comerciales sin autenticación)
            try
            {
                string urlApi = $"https://world.openfoodfacts.org/cgi/search.pl?search_terms={Uri.EscapeDataString(query)}&search_simple=1&action=process&json=1&page_size=20";

                using var req = new HttpRequestMessage(HttpMethod.Get, urlApi);
                req.Headers.Add("User-Agent", "SistemaPOS/1.0 (contacto@localpos.cl)");

                var response = await _httpClient.SendAsync(req);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("products", out var prodArray))
                    {
                        foreach (var prod in prodArray.EnumerateArray())
                        {
                            string imgUrl = "";

                            if (prod.TryGetProperty("image_front_url", out var imgF) && !string.IsNullOrEmpty(imgF.GetString()))
                                imgUrl = imgF.GetString()!;
                            else if (prod.TryGetProperty("image_url", out var imgDef) && !string.IsNullOrEmpty(imgDef.GetString()))
                                imgUrl = imgDef.GetString()!;

                            if (!string.IsNullOrEmpty(imgUrl) && urlsVistas.Add(imgUrl))
                            {
                                string nombre = prod.TryGetProperty("product_name", out var n) ? n.GetString() ?? query : query;
                                string marca = prod.TryGetProperty("brands", out var b) ? b.GetString() ?? "" : "";
                                string tituloCompleto = !string.IsNullOrWhiteSpace(marca) ? $"{nombre} ({marca})" : nombre;

                                resultados.Add(new ResultadoImagen
                                {
                                    Titulo = tituloCompleto,
                                    Url = imgUrl
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            return resultados;
        }

        private void RenderizarPaginaActual()
        {
            flowResultados.SuspendLayout();
            flowResultados.Controls.Clear();

            int total = _imagenesEncontradas.Count;
            int totalPaginas = (int)Math.Ceiling((double)total / ImagenesPorPagina);

            int skip = (_paginaActual - 1) * ImagenesPorPagina;
            var paginaItems = _imagenesEncontradas.Skip(skip).Take(ImagenesPorPagina).ToList();

            int desde = skip + 1;
            int hasta = skip + paginaItems.Count;
            lblEstado.Text = $"Se encontraron {total} imágenes. Mostrando de la {desde} a la {hasta}:";
            lblEstado.ForeColor = Color.FromArgb(16, 185, 129);

            lblPagina.Text = $"Pág. {_paginaActual} de {totalPaginas}";
            btnAnterior.Enabled = _paginaActual > 1;
            btnSiguiente.Enabled = _paginaActual < totalPaginas;

            foreach (var item in paginaItems)
            {
                flowResultados.Controls.Add(CrearTarjetaImagen(item));
            }

            flowResultados.ResumeLayout();
        }

        private Control CrearTarjetaImagen(ResultadoImagen item)
        {
            Panel card = new Panel
            {
                Size = new Size(224, 335),
                Margin = new Padding(8, 4, 8, 4),
                BackColor = Color.White
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(203, 213, 225), ButtonBorderStyle.Solid);

            PictureBox pb = new PictureBox
            {
                Location = new Point(12, 12),
                Size = new Size(200, 190),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Carga asíncrona de miniatura
            _ = Task.Run(async () =>
            {
                try
                {
                    byte[] bytes = await _httpClient.GetByteArrayAsync(item.Url);
                    using var ms = new MemoryStream(bytes);
                    var img = Image.FromStream(ms);
                    this.BeginInvoke(new Action(() => pb.Image = img));
                }
                catch { }
            });

            Label lblDesc = new Label
            {
                Text = item.Titulo,
                Location = new Point(12, 206),
                Size = new Size(200, 56),
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.TopCenter
            };

            Button btnSelect = new Button
            {
                Text = "✔ Añadir al producto",
                Location = new Point(12, 274),
                Size = new Size(200, 46),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += async (s, e) =>
            {
                btnSelect.Enabled = false;
                btnSelect.Text = "Descargando...";
                await DescargarYAsignarImagenAsync(item.Url);
            };

            card.Controls.AddRange(new Control[] { pb, lblDesc, btnSelect });
            return card;
        }

        private async Task DescargarYAsignarImagenAsync(string url)
        {
            try
            {
                byte[] imgBytes = await _httpClient.GetByteArrayAsync(url);
                using var ms = new MemoryStream(imgBytes);
                using Image original = Image.FromStream(ms);

                string carpetaImagenes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes");
                if (!Directory.Exists(carpetaImagenes)) Directory.CreateDirectory(carpetaImagenes);

                string skuLimpio = string.IsNullOrWhiteSpace(_sku) ? "PROD" : _sku.Trim();
                string nombreArchivo = $"{skuLimpio}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                string rutaFinal = Path.Combine(carpetaImagenes, nombreArchivo);

                using Bitmap bmp = new Bitmap(original);
                bmp.Save(rutaFinal, System.Drawing.Imaging.ImageFormat.Jpeg);

                this.RutaImagenDescargada = nombreArchivo;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la imagen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CambiarPagina(int delta)
        {
            _paginaActual += delta;
            RenderizarPaginaActual();
        }

        private class ResultadoImagen
        {
            public string Titulo { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }
    }
}