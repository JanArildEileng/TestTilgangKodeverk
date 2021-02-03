using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TilgangKodeverk.DataAksess;
using TilgangKodeverk.Entities;
using TilgangKodeverk.Model.Dto;
using TilgangKodeverk.Service;

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
