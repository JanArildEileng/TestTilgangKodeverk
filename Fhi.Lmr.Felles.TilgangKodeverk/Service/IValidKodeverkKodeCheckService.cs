using Fhi.Lmr.Felles.TilgangKodeverk.Model.Dto;
using System.Collections.Generic;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Service
{
    public interface IValidKodeverkKodeCheckService
    {
        IEnumerable<IsValidCheckResponse> IsValidCheck(IEnumerable<IsValidCheckRequest> testIfValdid);
        bool  IsValidCheck(int oid,string verdi);
       
    }
}
