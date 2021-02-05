using System;
using System.Collections.Generic;
using System.Text;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Entities
{
    public class Klassifikasjon
    {
        public int KlassifikasjonId { get; set; }
        public int OId { get; set; }
        public DateTime Nedlasted { get; set; }
        public DateTime? Lastchecked { get; set; }
    }
}
