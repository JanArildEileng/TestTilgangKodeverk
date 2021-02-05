using System;
using System.Collections.Generic;
using System.Text;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Service
{
    public interface IKlassifikasjonService
    {
        void Synchronize(int oid);
    }
}
