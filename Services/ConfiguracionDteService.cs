using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class ConfiguracionDteService
    {
        public ConfiguracionDte ObtenerConfiguracion()
        {
            using var db = new AppDbContext();
            var config = db.ConfiguracionesDte.AsNoTracking().FirstOrDefault(c => c.IdConfig == 1);
            if (config == null)
            {
                config = new ConfiguracionDte { IdConfig = 1, MaxBoletasPorNC = 10, PermitirNCParcial = true };
            }
            return config;
        }

        public void GuardarConfiguracion(int maxBoletas, bool permitirParcial, string usuario)
        {
            using var db = new AppDbContext();
            var config = db.ConfiguracionesDte.FirstOrDefault(c => c.IdConfig == 1);
            if (config == null)
            {
                config = new ConfiguracionDte
                {
                    IdConfig = 1,
                    MaxBoletasPorNC = maxBoletas,
                    PermitirNCParcial = permitirParcial,
                    FechaModificacion = DateTime.Now,
                    UsuarioModifica = usuario
                };
                db.ConfiguracionesDte.Add(config);
            }
            else
            {
                config.MaxBoletasPorNC = maxBoletas;
                config.PermitirNCParcial = permitirParcial;
                config.FechaModificacion = DateTime.Now;
                config.UsuarioModifica = usuario;
            }

            db.SaveChanges();
        }
    }
}