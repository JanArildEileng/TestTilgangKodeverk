﻿using System.ComponentModel.DataAnnotations.Schema;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Entities
{

    public class KodeverkKode 
    {
        public int KodeverkKodeId { get; set; }
        public int OId { get; set; }
        public string Verdi { get; set; }
        public string Navn { get; set; }

        [NotMapped]
        public bool Gyldig { get; set; } = true;

    }
}
