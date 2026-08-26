using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO
{
    public class FormLogin : Form
    {
        private TextBox txtUsuario = null!;
        private TextBox txtClave = null!;
        private Button btnMostrarClave = null!;
        private CheckBox chkRecordarme = null!;
        private Button btnIngresar = null!;
        private Label lblError = null!;
        public Usuario? UsuarioAutenticado { get; private set; }

        // Ruta local para recordar el usuario
        private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_user.cfg");

        // Variables para permitir el arrastre de la ventana sin bordes
        private bool _dragging = false;
        private Point _dragCursorPoint;
        private Point _dragFormPoint;

        public FormLogin()
        {
            InitializeComponent();
            CargarUsuarioRecordado();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "POS System - Acceso al Sistema";
            this.Size = new Size(860, 530);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;

            Font fontTituloBrand = new Font("Segoe UI", 23F, FontStyle.Bold, GraphicsUnit.Point);
            Font fontHeader = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
            Font fontMonoInput = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

            // =========================================================================
            // PANEL IZQUIERDO (BRANDING & LOGO)
            // =========================================================================
            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 350,
                BackColor = Color.FromArgb(8, 18, 38)
            };

            pnlLeft.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. Fondo Degradado Oscuro
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    pnlLeft.ClientRectangle,
                    Color.FromArgb(10, 22, 45),
                    Color.FromArgb(3, 8, 20),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bgBrush, pnlLeft.ClientRectangle);
                }

                // 2. Ondas Decorativas
                using (Pen wavePen = new Pen(Color.FromArgb(20, 0, 150, 255), 1.5f))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        GraphicsPath path = new GraphicsPath();
                        path.AddBezier(
                            new Point(-30, 390 + i * 18),
                            new Point(80, 350 + i * 14),
                            new Point(190, 470 + i * 14),
                            new Point(360, 410 + i * 18));
                        g.DrawPath(wavePen, path);
                    }
                }

                // Marca de Agua Cúbica Transparente
                using (Pen watermarkPen = new Pen(Color.FromArgb(12, 0, 122, 255), 2f))
                {
                    PointF[] bgCube = {
                        new PointF(230, 380), new PointF(300, 420), new PointF(230, 460), new PointF(160, 420),
                        new PointF(160, 500), new PointF(230, 540), new PointF(300, 500), new PointF(300, 420)
                    };
                    g.DrawPolygon(watermarkPen, bgCube);
                    g.DrawLine(watermarkPen, new PointF(230, 460), new PointF(230, 540));
                }

                // 3. Logo 3D Isométrico Cubo
                float centerX = pnlLeft.Width / 2f;
                float logoY = 95f;
                float size = 55f;

                PointF topPt = new PointF(centerX, logoY);
                PointF rightPt = new PointF(centerX + size, logoY + size * 0.55f);
                PointF leftPt = new PointF(centerX - size, logoY + size * 0.55f);
                PointF centerPt = new PointF(centerX, logoY + size * 1.1f);
                PointF btmLeftPt = new PointF(centerX - size, logoY + size * 1.65f);
                PointF btmRightPt = new PointF(centerX + size, logoY + size * 1.65f);
                PointF btmPt = new PointF(centerX, logoY + size * 2.2f);

                // Cara Superior
                PointF[] topFace = { topPt, rightPt, centerPt, leftPt };
                using (LinearGradientBrush topBrush = new LinearGradientBrush(
                    topPt, centerPt,
                    Color.FromArgb(96, 165, 250), Color.FromArgb(37, 99, 235)))
                {
                    g.FillPolygon(topBrush, topFace);
                }

                // Cara Izquierda
                PointF[] leftFace = { leftPt, centerPt, btmPt, btmLeftPt };
                using (LinearGradientBrush leftBrush = new LinearGradientBrush(
                    leftPt, btmPt,
                    Color.FromArgb(29, 78, 216), Color.FromArgb(30, 58, 138)))
                {
                    g.FillPolygon(leftBrush, leftFace);
                }

                // Cara Derecha
                PointF[] rightFace = { centerPt, rightPt, btmRightPt, btmPt };
                using (LinearGradientBrush rightBrush = new LinearGradientBrush(
                    centerPt, btmRightPt,
                    Color.FromArgb(37, 99, 235), Color.FromArgb(15, 23, 42)))
                {
                    g.FillPolygon(rightBrush, rightFace);
                }

                // 4. Texto "POS SYSTEM"
                SizeF sizePOS = g.MeasureString("POS", fontTituloBrand);
                SizeF sizeSYS = g.MeasureString("SYSTEM", fontTituloBrand);
                float totalTitleWidth = sizePOS.Width + sizeSYS.Width - 12f;
                float startTitleX = (pnlLeft.Width - totalTitleWidth) / 2f;
                float titleY = logoY + size * 2.2f + 25f;

                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("POS", fontTituloBrand, whiteBrush, startTitleX, titleY);
                }

                using (SolidBrush blueBrush = new SolidBrush(Color.FromArgb(0, 122, 255)))
                {
                    g.DrawString("SYSTEM", fontTituloBrand, blueBrush, startTitleX + sizePOS.Width - 12f, titleY);
                }

                // 5. Línea Divisora
                float lineY = titleY + sizePOS.Height + 6f;
                float lineLineWidth = totalTitleWidth + 10f;
                float lineStartX = (pnlLeft.Width - lineLineWidth) / 2f;
                using (Pen linePen = new Pen(Color.FromArgb(0, 102, 255), 2f))
                {
                    g.DrawLine(linePen, lineStartX, lineY, lineStartX + lineLineWidth, lineY);
                }

                // 6. Subtítulo
                using (Font fontSub = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    string subText = "Gestión inteligente de ventas e inventario";
                    SizeF sizeSub = g.MeasureString(subText, fontSub);
                    float subX = (pnlLeft.Width - sizeSub.Width) / 2f;
                    float subY = lineY + 14f;
                    g.DrawString(subText, fontSub, subBrush, subX, subY);
                }
            };

            pnlLeft.MouseDown += FormLogin_MouseDown;
            pnlLeft.MouseMove += FormLogin_MouseMove;
            pnlLeft.MouseUp += FormLogin_MouseUp;

            // =========================================================================
            // PANEL DERECHO (FORMULARIO)
            // =========================================================================
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(45, 20, 45, 20)
            };
            pnlRight.MouseDown += FormLogin_MouseDown;
            pnlRight.MouseMove += FormLogin_MouseMove;
            pnlRight.MouseUp += FormLogin_MouseUp;

            // Botón Cierre '✕'
            Button btnCerrar = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(460, 10),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
            btnCerrar.MouseEnter += (s, e) => btnCerrar.ForeColor = Color.FromArgb(220, 38, 38);
            btnCerrar.MouseLeave += (s, e) => btnCerrar.ForeColor = Color.FromArgb(148, 163, 184);
            btnCerrar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                Environment.Exit(0);
            };

            Label lblWelcome = new Label
            {
                Text = "Bienvenido de nuevo",
                Font = fontHeader,
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(42, 40),
                AutoSize = true
            };

            Label lblSubWelcome = new Label
            {
                Text = "Ingresa tus credenciales para acceder al panel",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(42, 80),
                AutoSize = true
            };

            // Campo Usuario
            Label lblUser = new Label
            {
                Text = "👤  USUARIO",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(42, 122),
                AutoSize = true
            };

            Panel pnlInputUser = CrearContenedorInput();
            pnlInputUser.Location = new Point(42, 145);
            pnlInputUser.Size = new Size(410, 42);

            Label lblIconUserInside = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(12, 11),
                AutoSize = true
            };

            txtUsuario = new TextBox
            {
                Location = new Point(40, 10),
                Size = new Size(355, 24),
                Font = fontMonoInput,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlInputUser.Controls.AddRange(new Control[] { lblIconUserInside, txtUsuario });

            // Campo Contraseña
            Label lblPass = new Label
            {
                Text = "🔒  CONTRASEÑA",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(42, 200),
                AutoSize = true
            };

            Panel pnlInputPass = CrearContenedorInput();
            pnlInputPass.Location = new Point(42, 223);
            pnlInputPass.Size = new Size(410, 42);

            Label lblIconPassInside = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(12, 11),
                AutoSize = true
            };

            txtClave = new TextBox
            {
                Location = new Point(40, 10),
                Size = new Size(325, 24),
                Font = fontMonoInput,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                UseSystemPasswordChar = true
            };
            txtClave.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnIngresar_Click(s, e); e.SuppressKeyPress = true; } };

            // Botón Mostrar/Ocultar Clave
            btnMostrarClave = new Button
            {
                Text = "👁️",
                Location = new Point(370, 6),
                Size = new Size(32, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F)
            };
            btnMostrarClave.FlatAppearance.BorderSize = 0;
            btnMostrarClave.Click += (s, e) =>
            {
                txtClave.UseSystemPasswordChar = !txtClave.UseSystemPasswordChar;
                btnMostrarClave.Text = txtClave.UseSystemPasswordChar ? "👁️" : "🙈";
            };

            pnlInputPass.Controls.AddRange(new Control[] { lblIconPassInside, txtClave, btnMostrarClave });

            // Checkbox Recordarme + Link Olvidaste Contraseña
            chkRecordarme = new CheckBox
            {
                Text = "Recordarme",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(44, 275),
                Size = new Size(130, 22),
                Cursor = Cursors.Hand,
                Checked = true
            };

            LinkLabel linkOlvidaste = new LinkLabel
            {
                Text = "¿Olvidaste tu contraseña?",
                Font = new Font("Segoe UI", 8.5F),
                LinkColor = Color.FromArgb(37, 99, 235),
                ActiveLinkColor = Color.FromArgb(29, 78, 216),
                Location = new Point(285, 277),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            linkOlvidaste.LinkClicked += (s, e) =>
            {
                MessageBox.Show("Para restablecer tu contraseña o desbloquear el acceso, contacta al Administrador del sistema.", "Recuperación de Acceso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(42, 308),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnIngresar = new Button
            {
                Text = "INICIAR SESIÓN  ➔",
                Location = new Point(42, 336),
                Size = new Size(410, 48),
                BackColor = Color.FromArgb(0, 102, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnIngresar.FlatAppearance.BorderSize = 0;
            btnIngresar.Click += btnIngresar_Click;

            // Footer de Seguridad
            Label lblSecurityFooter = new Label
            {
                Text = "🔒  Acceso seguro y protegido SSL",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(42, 398),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlRight.Controls.AddRange(new Control[] {
                btnCerrar,
                lblWelcome,
                lblSubWelcome,
                lblUser,
                pnlInputUser,
                lblPass,
                pnlInputPass,
                chkRecordarme,
                linkOlvidaste,
                lblError,
                btnIngresar,
                lblSecurityFooter
            });

            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlLeft);
            this.ResumeLayout(false);

            btnCerrar.BringToFront();
        }

        private void CargarUsuarioRecordado()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string savedUser = File.ReadAllText(_configPath).Trim();
                    if (!string.IsNullOrEmpty(savedUser))
                    {
                        txtUsuario.Text = savedUser;
                        chkRecordarme.Checked = true;

                        // Si el usuario ya está recordado, situar el cursor directamente en la contraseña
                        this.Shown += (s, e) =>
                        {
                            txtClave.Focus();
                        };
                    }
                }
            }
            catch { }
        }

        private void GuardarOlimpiarUsuarioRecordado(string user)
        {
            try
            {
                if (chkRecordarme.Checked && !string.IsNullOrWhiteSpace(user))
                {
                    File.WriteAllText(_configPath, user);
                }
                else
                {
                    if (File.Exists(_configPath))
                    {
                        File.Delete(_configPath);
                    }
                }
            }
            catch { }
        }

        private Panel CrearContenedorInput()
        {
            Panel p = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };
            p.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };
            return p;
        }

        private void FormLogin_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragCursorPoint = Cursor.Position;
                _dragFormPoint = this.Location;
            }
        }

        private void FormLogin_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(_dragCursorPoint));
                this.Location = Point.Add(_dragFormPoint, new Size(dif));
            }
        }

        private void FormLogin_MouseUp(object? sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private void btnIngresar_Click(object? sender, EventArgs e)
        {
            string user = txtUsuario.Text.Trim();
            string pass = txtClave.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "⚠️ Por favor ingresa usuario y contraseña";
                return;
            }

            try
            {
                using var db = new AppDbContext();

                // Validación directa contra la base de datos (Usuario, Clave y Estado Activo)
                var usuarioEncontrado = db.Usuarios
                    .FirstOrDefault(u => u.NombreUsuario.ToLower() == user.ToLower() && 
                                         u.Clave == pass && 
                                         u.Estado);

                if (usuarioEncontrado != null)
                {
                    if (string.IsNullOrWhiteSpace(usuarioEncontrado.Rol))
                    {
                        lblError.Text = "❌ El usuario no tiene un rol válido asignado.";
                        return;
                    }

                    // Guardar o limpiar el usuario según el checkbox "Recordarme"
                    GuardarOlimpiarUsuarioRecordado(user);

                    UsuarioAutenticado = usuarioEncontrado;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblError.Text = "❌ Credenciales incorrectas o usuario inactivo";
                    txtClave.Clear();
                    txtClave.Focus();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "❌ Error de conexión con la base de datos";
                MessageBox.Show($"No se pudo verificar la sesión:\n{ex.Message}", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}