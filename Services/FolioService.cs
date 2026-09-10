using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;

namespace SISTEMAACTUALIZADO.Services
{
    public class FolioService
    {
        public List<Folio> ObtenerFolios()
        {
            using var db = new AppDbContext();
            return db.Folios.AsNoTracking().OrderBy(f => f.TipoDocumento).ToList();
        }

        public void GuardarFolio(Folio folio, bool esNuevo)
        {
            using var db = new AppDbContext();

            if (esNuevo)
            {
                db.Folios.Add(folio);
            }
            else
            {
                var fExistente = db.Folios.Find(folio.FolioID);
                if (fExistente != null)
                {
                    fExistente.TipoDocumento = folio.TipoDocumento;
                    fExistente.FolioDesde = folio.FolioDesde;
                    fExistente.FolioHasta = folio.FolioHasta;
                    fExistente.FolioActual = folio.FolioActual;
                    fExistente.Activo = folio.Activo;
                }
            }

            db.SaveChanges();
        }
    }
}