using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Test.TestKlassifikasjonService
{
    public class InMemoryKodeverkRepository : IKodeverkRepository
    {

        List<Klassifikasjon> KlassifikasjonListe = new List<Klassifikasjon>();
        List<KodeverkKode> KodeverkKodeListe = new List<KodeverkKode>();

        public InMemoryKodeverkRepository()
        {
        }

        public Klassifikasjon GetKlassifikasjon(int oid)
        {
            return KlassifikasjonListe.Where(k => k.OId == oid).FirstOrDefault();
        }

        public void AddKlassifikasjon(Klassifikasjon klassifikasjon)
        {
            KlassifikasjonListe.Add(klassifikasjon);
        }

        public KodeverkKode GetKodeverkKode(int oid, string verdi)
        {
            return KodeverkKodeListe.FirstOrDefault(k => k.OId == oid && k.Verdi.Equals(verdi));
        }

        public void UpdateKodeverkKoder(int oid, IEnumerable<KodeverkKode> updatedKodeverkKodeListe)
        {
            var deleteListe = KodeverkKodeListe.Where(k => k.OId == oid).ToList();
            KodeverkKodeListe = KodeverkKodeListe.Where(k => k.OId != oid).ToList();
            KodeverkKodeListe.AddRange(updatedKodeverkKodeListe);
        }

        public int SaveChanges()
        {
            return KodeverkKodeListe.Count;
        }
    }
}
