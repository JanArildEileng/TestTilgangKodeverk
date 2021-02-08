using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Entities
{
    public class Klassifikasjon
    {
        public int KlassifikasjonId { get; set; }
        public int OId { get; set; }
        public DateTime Nedlasted { get; set; }
        [NotMapped]
        public DateTime? Lastchecked { get; set; }
        [NotMapped]
        public bool Gyldig { get; set; } = true;
    }
}
