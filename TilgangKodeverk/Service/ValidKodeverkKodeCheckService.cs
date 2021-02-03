using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TilgangKodeverk.DataAksess;
using TilgangKodeverk.Model.Dto;

namespace TilgangKodeverk.Service
{

    public class ValidKodeverkKodeCheckService: IValidKodeverkKodeCheckService
    {
        private readonly TilgangKodeverkContext tilgangKodeverkContext;

        public ValidKodeverkKodeCheckService(TilgangKodeverkContext tilgangKodeverkContext)
        {
            this.tilgangKodeverkContext = tilgangKodeverkContext;
        }

        public IEnumerable<IsValidCheckResponse> IsValidCheck(IEnumerable<IsValidCheckRequest> testIfValdid)
        {
            var IsValidCheckResponseList = new List<IsValidCheckResponse>();
            foreach (var request in testIfValdid)
            {
                IsValidCheckResponseList.Add(new IsValidCheckResponse() { IsValid = IsValidCheck(request.OId, request.Verdi), OId = request.OId, Verdi = request.Verdi });
            }
            return IsValidCheckResponseList;
        }

        public bool IsValidCheck(int oid, string verdi)
        {
            if (tilgangKodeverkContext.KodeverkKoder.FirstOrDefault(k => k.OId == oid && k.Verdi.Equals(verdi)) != null)
            {
                return true;
            }
            else
                return false;
        }
    }
    
}
