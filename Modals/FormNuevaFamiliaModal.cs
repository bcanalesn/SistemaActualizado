using System;
using System.Drawing;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Services;

namespace SISTEMAACTUALIZADO.Modals
{
    public class FormNuevaFamiliaModal : Form
    {
        private readonly int _categoriaId;
        private readonly ConfiguracionService _configService;
        private TextBox txtNom = null!;
        private Button btnCrear = null!;

        public string? FamiliaCreada { get; private set; }

        public FormNuevaFamiliaModal(int categoriaId, string categoriaNombre, ConfiguracionService configService)
        {
            _categoriaId = categoriaId;
            _configService = configService;
            InitializeComponent(categoriaNombre);
        }

        private void InitializeComponent(string categoriaNombre)
        {
            this.Width = 350;
            this.Height = 160;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Text = $"Nueva Familia en {categoriaNombre}";
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            Label textLabel = new Label { Left = 20, Top = 15, Text = "Nombre de la subfamilia:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtNom = new TextBox { Left = 20, Top = 40, Width = 295, Font = new Font("Segoe UI", 10F) };

            btnCrear = new Button
            {
                Text = "Crear",
                Left = 215,
                Width = 100,
                Top = 75,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCrear.FlatAppearance.BorderSize = 0;
            btnCrear.Click += BtnCrear_Click;

            this.Controls.AddRange(new Control[] { textLabel, txtNom, btnCrear });
            this.AcceptButton = btnCrear;
        }

        private void BtnCrear_Click(object? sender, EventArgs e)
        {
            string nombre = txtNom.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre)) return;

            try
            {
                _configService.CrearFamilia(_categoriaId, nombre);
                this.FamiliaCreada = nombre;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear la subfamilia: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}