using System;
using System.Linq;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class AuthResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }
    }

    public class AuthService
    {
        public AuthResult IniciarSesion(string usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            {
                return new AuthResult { Exitoso = false, Mensaje = "Por favor ingresa usuario y contraseña." };
            }

            try
            {
                using var db = new AppDbContext();
                var user = db.Usuarios.FirstOrDefault(u => 
                    u.NombreUsuario.ToLower() == usuario.Trim().ToLower() && 
                    u.Clave == clave.Trim() && 
                    u.Estado);

                if (user == null)
                {
                    return new AuthResult { Exitoso = false, Mensaje = "Credenciales incorrectas o usuario inactivo." };
                }

                if (string.IsNullOrWhiteSpace(user.Rol))
                {
                    return new AuthResult { Exitoso = false, Mensaje = "El usuario no tiene un rol asignado." };
                }

                return new AuthResult { Exitoso = true, Usuario = user };
            }
            catch (Exception ex)
            {
                return new AuthResult { Exitoso = false, Mensaje = $"Error de conexión: {ex.Message}" };
            }
        }
    }
}