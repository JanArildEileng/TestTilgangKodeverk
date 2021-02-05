using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Fhi.Lmr.Felles.TilgangKodeverk.Model.Dto;
using Fhi.Lmr.Felles.TilgangKodeverk.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using TilgangKodeverk.DataAksess;

namespace TilgangKodeverk.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KodeverkKodeController : ControllerBase
    {

        private readonly ILogger<KodeverkKodeController> _logger;
        private readonly TilgangKodeverkContext tilgangKodeverkContext;

        public KodeverkKodeController(ILogger<KodeverkKodeController> logger, TilgangKodeverkContext tilgangKodeverkContext)
        {
            _logger = logger;
            this.tilgangKodeverkContext = tilgangKodeverkContext;
        }

        [HttpGet]
        public IEnumerable<KodeverkKode> Get(int take=10,int skip=0)
        {
            return tilgangKodeverkContext.KodeverkKoder.Skip(skip).Take(take);
        }

        [HttpPost("IsValidCheck")]
        public IEnumerable<IsValidCheckResponse> IsValidCheck([FromServices] IValidKodeverkKodeCheckService validKodeverkKodeCheck, [FromBody]IEnumerable<IsValidCheckRequest> testIfValdid )
        {
             return validKodeverkKodeCheck.IsValidCheck(testIfValdid);
        }

    }
}
