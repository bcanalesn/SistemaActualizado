using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Helpers;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormNuevaCategoriaModal : Form
    {
        private readonly ConfiguracionService _configService;
        private TextBox txtNom = null!;
        private Button btnMuestra = null!;
        private ComboBox cbColor = null!;
        private Button btnCrear = null!;

        public string? CategoriaCreada { get; private set; }

        public FormNuevaCategoriaModal(ConfiguracionService configService)
        {
            _configService = configService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Width = 380;
            this.Height = 230;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Text = "Nueva Categoría";
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            Label lblNom = new Label { Left = 20, Top = 15, Text = "Nombre de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtNom = new TextBox { Left = 20, Top = 38, Width = 320, Font = new Font("Segoe UI", 9.5F) };

            Label lblColor = new Label { Left = 20, Top = 72, Text = "Color de la categoría:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnMuestra = new Button { Left = 20, Top = 95, Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            btnMuestra.FlatAppearance.BorderSize = 1;

            cbColor = new ComboBox { Left = 50, Top = 95, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            foreach (var opc in CategoriaColorHelper.OpcionesPaleta) cbColor.Items.Add(opc.Nombre);

            cbColor.SelectedIndexChanged += (s, e) =>
            {
                if (cbColor.SelectedIndex >= 0)
                {
                    var pal = CategoriaColorHelper.OpcionesPaleta[cbColor.SelectedIndex];
                    btnMuestra.BackColor = pal.Texto;
                    btnMuestra.FlatAppearance.BorderColor = pal.Borde;
                }
            };
            cbColor.SelectedIndex = 0;

            btnCrear = new Button
            {
                Text = "Crear Categoría",
                Left = 190,
                Width = 150,
                Top = 140,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCrear.FlatAppearance.BorderSize = 0;
            btnCrear.Click += BtnCrear_Click;

            this.Controls.AddRange(new Control[] { lblNom, txtNom, lblColor, btnMuestra, cbColor, btnCrear });
            this.AcceptButton = btnCrear;
        }

        private void BtnCrear_Click(object? sender, EventArgs e)
        {
            string nombre = txtNom.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre)) return;

            var pal = CategoriaColorHelper.OpcionesPaleta[Math.Max(0, cbColor.SelectedIndex)];

            try
            {
                _configService.CrearOCargarCategoria(
                    nombre,
                    ColorTranslator.ToHtml(pal.Fondo),
                    ColorTranslator.ToHtml(pal.Texto),
                    ColorTranslator.ToHtml(pal.Borde)
                );

                this.CategoriaCreada = nombre;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la categoría: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}