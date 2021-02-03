using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TilgangKodeverk.Model.Dto
{
    public class IsValidCheckResponse
    {
        public int OId { get; set; }
        public string Verdi { get; set; }

        public bool  IsValid { get; set; }

    }
}
