using System;
using System.Windows.Forms;

namespace SISTEMAACTUALIZADO
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool reiniciarSesion = true;

            while (reiniciarSesion)
            {
                reiniciarSesion = false;

                // 1. Mostrar pantalla de Login
                using (FormLogin frmLogin = new FormLogin())
                {
                    if (frmLogin.ShowDialog() == DialogResult.OK)
                    {
                        // 2. Abrir Menú Principal pasando la sesión del usuario
                        FormMain frmMain = new FormMain(frmLogin.UsuarioAutenticado);
                        Application.Run(frmMain);

                        // 3. Si solicitó cerrar sesión, vuelve a mostrar el Login
                        if (frmMain.EsCerrarSesion)
                        {
                            reiniciarSesion = true;
                        }
                    }
                }
            }
        }
    }
}