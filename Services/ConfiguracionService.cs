using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ConfiguracionService
    {
        public ConfiguracionEmpresa ObtenerDatosEmpresa()
        {
            using var db = new AppDbContext();
            return db.ConfiguracionEmpresa.AsNoTracking().FirstOrDefault(e => e.EmpresaID == 1) 
                   ?? new ConfiguracionEmpresa { EmpresaID = 1 };
        }

        public void GuardarDatosEmpresa(ConfiguracionEmpresa datos)
        {
            using var db = new AppDbContext();
            var emp = db.ConfiguracionEmpresa.FirstOrDefault(e => e.EmpresaID == 1);

            if (emp == null)
            {
                datos.EmpresaID = 1;
                db.ConfiguracionEmpresa.Add(datos);
            }
            else
            {
                emp.Rut = datos.Rut;
                emp.RazonSocial = datos.RazonSocial;
                emp.Giro = datos.Giro;
                emp.Direccion = datos.Direccion;
                emp.Comuna = datos.Comuna;
                emp.Ciudad = datos.Ciudad;
                emp.Telefono = datos.Telefono;
                emp.Email = datos.Email;
                emp.UnidadSII = datos.UnidadSII;
                emp.ResolucionSII = datos.ResolucionSII;
                emp.TextoPieTicket = datos.TextoPieTicket;
            }

            db.SaveChanges();
        }
    }
}