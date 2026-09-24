using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormBuscarImagenModal : Form
    {
        private readonly JumboScraperService _scraperService = new JumboScraperService();
        private readonly HttpClient _http = new HttpClient();

        private TextBox txtBusqueda = null!;
        private Button btnBuscar = null!;
        private Button btnMas = null!;
        private Label lblEstado = null!;
        private FlowLayoutPanel pnlTarjetas = null!;

        private List<ProductoScrapDTO> _resultadosTotales = new List<ProductoScrapDTO>();
        private int _indiceActual = 0;
        private const int CANTIDAD_POR_PAGINA = 3;

        public string? RutaImagenDescargada { get; private set; }
        private readonly string? _codigoBarra;

        public FormBuscarImagenModal(string terminoInicial, string? codigoBarra = null)
        {
            _codigoBarra = codigoBarra;
            InitializeComponent();
            txtBusqueda.Text = terminoInicial;
            this.Shown += async (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtBusqueda.Text))
                {
                    await EjecutarBusquedaAsync();
                }
            };
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Size = new Size(680, 520);
            this.BackColor = Color.White;
            this.Text = "🌐 Buscar Imagen en Línea";
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(15, 12, 15, 10), BackColor = Color.FromArgb(248, 250, 252) };
            Label lblTit = new Label { Text = "Buscar producto o marca en el catálogo web:", Location = new Point(15, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            
            txtBusqueda = new TextBox { Location = new Point(15, 30), Width = 470, Font = new Font("Segoe UI", 10F) };
            txtBusqueda.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await EjecutarBusquedaAsync(); } };

            btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(495, 29),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += async (s, e) => await EjecutarBusquedaAsync();

            pnlTop.Controls.AddRange(new Control[] { lblTit, txtBusqueda, btnBuscar });

            lblEstado = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Text = "Listo para buscar.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };

            pnlTarjetas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(15, 10, 15, 10),
                AutoScroll = true
            };

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(15, 8, 15, 10) };
            btnMas = new Button
            {
                Text = "➕ Ver 3 opciones más",
                Size = new Size(180, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Location = new Point(15, 10)
            };
            btnMas.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMas.Click += (s, e) => MostrarSiguienteGrupo();

            Button btnCerrar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(243, 244, 246),
                ForeColor = Color.FromArgb(55, 65, 81),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Location = new Point(545, 10)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            pnlBottom.Controls.AddRange(new Control[] { btnMas, btnCerrar });

            this.Controls.AddRange(new Control[] { pnlTarjetas, lblEstado, pnlBottom, pnlTop });
        }

        private async Task EjecutarBusquedaAsync()
        {
            string query = txtBusqueda.Text.Trim();
            if (string.IsNullOrWhiteSpace(query)) return;

            btnBuscar.Enabled = false;
            btnMas.Enabled = false;
            lblEstado.Text = "Buscando productos e imágenes...";
            lblEstado.ForeColor = Color.FromArgb(37, 99, 235);
            LimpiarTarjetas();

            _resultadosTotales = await _scraperService.BuscarProductosAsync(query);
            _indiceActual = 0;

            btnBuscar.Enabled = true;

            if (_resultadosTotales.Count == 0)
            {
                lblEstado.Text = "No se encontraron imágenes para el producto buscado.";
                lblEstado.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }

            lblEstado.Text = $"Se encontraron {_resultadosTotales.Count} imágenes. Mostrando las 3 primeras:";
            lblEstado.ForeColor = Color.FromArgb(22, 101, 52);
            MostrarSiguienteGrupo();
        }

        private void LimpiarTarjetas()
        {
            foreach (Control c in pnlTarjetas.Controls)
            {
                if (c is Panel p)
                {
                    var pb = p.Controls.OfType<PictureBox>().FirstOrDefault();
                    pb?.Image?.Dispose();
                }
            }
            pnlTarjetas.Controls.Clear();
        }

        private async void MostrarSiguienteGrupo()
        {
            LimpiarTarjetas();

            var grupo = _resultadosTotales.Skip(_indiceActual).Take(CANTIDAD_POR_PAGINA).ToList();
            _indiceActual += grupo.Count;

            btnMas.Enabled = _indiceActual < _resultadosTotales.Count;

            foreach (var prod in grupo)
            {
                Panel card = new Panel
                {
                    Size = new Size(200, 270),
                    Margin = new Padding(8, 5, 8, 5),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White
                };

                PictureBox pb = new PictureBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(178, 140),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(248, 250, 252)
                };

                Label lblNombre = new Label
                {
                    Location = new Point(10, 155),
                    Size = new Size(178, 55),
                    Text = prod.Titulo,
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.FromArgb(30, 41, 59)
                };

                Button btnSeleccionar = new Button
                {
                    Location = new Point(10, 220),
                    Size = new Size(178, 36),
                    Text = "✔ Añadir al producto",
                    BackColor = Color.FromArgb(22, 163, 74),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSeleccionar.FlatAppearance.BorderSize = 0;

                btnSeleccionar.Click += async (s, e) =>
                {
                    btnSeleccionar.Enabled = false;
                    btnSeleccionar.Text = "Descargando...";
                    string? path = await _scraperService.DescargarYGuardarImagenAsync(prod.UrlImagen, prod.Titulo, _codigoBarra);
                    if (!string.IsNullOrEmpty(path))
                    {
                        RutaImagenDescargada = path;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo guardar la imagen en disco.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnSeleccionar.Enabled = true;
                        btnSeleccionar.Text = "✔ Añadir al producto";
                    }
                };

                card.Controls.AddRange(new Control[] { pb, lblNombre, btnSeleccionar });
                pnlTarjetas.Controls.Add(card);

                // Carga asíncrona de miniatura
                _ = Task.Run(async () =>
                {
                    try
                    {
                        byte[] bytes = await _http.GetByteArrayAsync(prod.UrlImagen);
                        using var ms = new MemoryStream(bytes);
                        var img = new Bitmap(ms);
                        if (!this.IsDisposed && pb.IsHandleCreated)
                        {
                            this.Invoke(new Action(() => pb.Image = img));
                        }
                    }
                    catch { }
                });
            }
        }
    }
}