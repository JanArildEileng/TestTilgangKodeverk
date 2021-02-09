using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TilgangKodeverk.DataAksess.Repository
{
    public class KodeverkRepository : IKodeverkRepository
    {
        private readonly TilgangKodeverkContext tilgangKodeverkContext;

        public KodeverkRepository(TilgangKodeverkContext tilgangKodeverkContext)
        {
            this.tilgangKodeverkContext = tilgangKodeverkContext;
        }

        public Klassifikasjon GetKlassifikasjon(int oid)
        {
            return tilgangKodeverkContext.Klassifikasjon.Where(k => k.OId == oid).AsNoTracking().FirstOrDefault();
        }

        public void AddKlassifikasjon(Klassifikasjon klassifikasjon)
        {
            tilgangKodeverkContext.Klassifikasjon.Add(klassifikasjon);
        }

        public KodeverkKode GetKodeverkKode(int oid, string verdi)
        {
            return tilgangKodeverkContext.KodeverkKoder.AsNoTracking().FirstOrDefault(k => k.OId == oid && k.Verdi.Equals(verdi));
        }

        public void UpdateKodeverkKoder(int oid, IEnumerable<KodeverkKode> updatedKodeverkKodeListe)
        {
            var deleteListe = tilgangKodeverkContext.KodeverkKoder.Where(k => k.OId == oid).ToList();
            tilgangKodeverkContext.KodeverkKoder.RemoveRange(deleteListe);
            tilgangKodeverkContext.KodeverkKoder.AddRange(updatedKodeverkKodeListe);
        }

        public int SaveChanges()
        {
            return tilgangKodeverkContext.SaveChanges();
        }
    }
}
