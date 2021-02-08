using System;
using System.Collections.Generic;
using System.Text;

namespace Fhi.Lmr.Felles.TilgangKodeverk
{
    public class GrunndataKodeverkOption
    {
        public const string GrunndataKodeverk = "GrunndataKodeverk";

        public Boolean AktivSynkronisering { get; set; }

        public string ApiUrl { get; set; }
        public string UpdateIntervalInMinuttes { get; set; }
        public string KodeverkKodeExpiration { get; set; }
        public string KlassifikasjonExpiration { get; set; }
       
    }
}
