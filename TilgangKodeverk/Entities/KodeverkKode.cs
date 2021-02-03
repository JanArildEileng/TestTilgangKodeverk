using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TilgangKodeverk.Entities
{
  
    public class KodeverkKode
    {
        public int KodeverkKodeId { get; set; }
        public int OId { get; set; }
        public string Verdi { get; set; }
        public string Navn { get; set; }
    }
}
