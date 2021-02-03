using System.Collections.Generic;
using TilgangKodeverk.Model.Dto;

namespace TilgangKodeverk.Service
{
    public interface IValidKodeverkKodeCheckService
    {
        IEnumerable<IsValidCheckResponse> IsValidCheck(IEnumerable<IsValidCheckRequest> testIfValdid);
        bool  IsValidCheck(int oid,string verdi);
    }
}
