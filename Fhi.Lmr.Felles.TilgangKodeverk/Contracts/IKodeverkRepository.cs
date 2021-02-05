using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Contracts
{
    public interface IKodeverkRepository
    {
        Klassifikasjon GetKlassifikasjon(int oid);
        void AddKlassifikasjon(int oid);
        KodeverkKode GetKodeverkKode(int oid, string verdi);
        void UpdateKodeverkKoder(int oid, IEnumerable<KodeverkKode> updatedKodeverkKodeListe);

        int SaveChanges();
    }
}
