using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Fhi.Lmr.Felles.TilgangKodeverk.Model.Dto;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Fhi.Lmr.Felles.TilgangKodeverk.Service
{

    public class ValidKodeverkKodeCheckService : IValidKodeverkKodeCheckService
    {
        private readonly GrunndataKodeverkOption grunndataKodeverkOption;
        private readonly ILogger<ValidKodeverkKodeCheckService> logger;
        private readonly IKodeverkRepository kodeverkRepository;
        private readonly IKlassifikasjonService klassifikasjonService;
        private readonly MemoryCache kodeverkKodeCache;
   
        MemoryCacheEntryOptions cacheEntryOptions;

   
        public ValidKodeverkKodeCheckService(IOptions<GrunndataKodeverkOption> grunndataKodeverkOption,ILogger<ValidKodeverkKodeCheckService> logger, KodeverkKodeMemoryCache kodeverkKodeMemoryCache, IKodeverkRepository kodeverkRepository, IKlassifikasjonService klassifikasjonService)
        {
            this.grunndataKodeverkOption = grunndataKodeverkOption.Value;
            this.logger = logger;
            this.kodeverkRepository = kodeverkRepository;
            this.klassifikasjonService = klassifikasjonService;
            this.kodeverkKodeCache = kodeverkKodeMemoryCache.Cache;
          
            //TODO
            this.cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTime.Now.AddMinutes(int.Parse(grunndataKodeverkOption.Value.KodeverkKodeExpiration)));  
        }

        public bool IsValidCheck(int oid, string verdi)
        {
            KodeverkKodeCacheKey kodeverkKodeCacheKey = new KodeverkKodeCacheKey() { OId = oid, Verdi = verdi };
            KodeverkKode kodeverkKode;
   
            if (grunndataKodeverkOption.AktivSynkronisering)
            klassifikasjonService.Synchronize(oid);

            //sjekk om kode finnes i cache
            if (kodeverkKodeCache.TryGetValue(kodeverkKodeCacheKey, out kodeverkKode))
            {
                logger.LogDebug($" {kodeverkKodeCacheKey}  funnet i cache");
                return kodeverkKode.Gyldig;
            }

            //sjekk om kode finnes i db-context
            if (HentFraRepository(kodeverkKodeCacheKey,out kodeverkKode))
            {
                //legg koden til cache
           
                 kodeverkKodeCache.Set<KodeverkKode>(kodeverkKodeCacheKey, kodeverkKode, this.cacheEntryOptions);
                 logger.LogDebug($" {kodeverkKodeCacheKey}  lagt til i cache");
                 return true;
            }
            else
            {
                //legg til ugyldig kode i cache
                kodeverkKodeCache.Set<KodeverkKode>(kodeverkKodeCacheKey, new KodeverkKode() {OId=oid,Verdi=verdi,Gyldig=false }, this.cacheEntryOptions);
                logger.LogDebug($" {kodeverkKodeCacheKey}  lagt til i cache med ugyldig status");
                return false;
            }
               
        }


        private bool HentFraRepository(KodeverkKodeCacheKey kodeverkKodeCacheKey,out KodeverkKode kodeverkKode)
        {
             kodeverkKode = kodeverkRepository.GetKodeverkKode(kodeverkKodeCacheKey.OId, kodeverkKodeCacheKey.Verdi);
             return kodeverkKode != null;
        }

        //bare for testing gjennom swagger
        public IEnumerable<IsValidCheckResponse> IsValidCheck(IEnumerable<IsValidCheckRequest> testIfValdid)
        {
            var IsValidCheckResponseList = new List<IsValidCheckResponse>();
            foreach (var request in testIfValdid)
            {
                IsValidCheckResponseList.Add(new IsValidCheckResponse() { IsValid = IsValidCheck(request.OId, request.Verdi), OId = request.OId, Verdi = request.Verdi });
            }
            return IsValidCheckResponseList;
        }

        public int ClearCache()
        {
            kodeverkKodeCache.Compact(1.0);

            return kodeverkKodeCache.Count;
        }
    }

}
