using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Fhi.Lmr.Felles.TilgangKodeverk.Model.Dto;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Fhi.Lmr.Felles.TilgangKodeverk.Service
{

    public class ValidKodeverkKodeCheckService : IValidKodeverkKodeCheckService
    {
        private readonly IConfiguration configuration;
        private readonly GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient;
        private readonly ILogger<ValidKodeverkKodeCheckService> logger;
        private readonly IKodeverkRepository kodeverkRepository;
        private readonly IKlassifikasjonService klassifikasjonService;
        private readonly MemoryCache kodeverkKodeCache;
     

        MemoryCacheEntryOptions cacheEntryOptions;

        public int UpdateIntervalInMinuttes { get; set; }

        public ValidKodeverkKodeCheckService(IConfiguration configuration, GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient, ILogger<ValidKodeverkKodeCheckService> logger, KodeverkKodeMemoryCache kodeverkKodeMemoryCache, IKodeverkRepository kodeverkRepository, IKlassifikasjonService klassifikasjonService)
        {
            this.configuration = configuration;
            this.grunndataKodeverkHttpKlient = grunndataKodeverkHttpKlient;
            this.logger = logger;
            this.kodeverkRepository = kodeverkRepository;
            this.klassifikasjonService = klassifikasjonService;
            this.kodeverkKodeCache = kodeverkKodeMemoryCache.Cache;
            this.UpdateIntervalInMinuttes = int.Parse(configuration["GrunndataKodeverk:UpdateIntervalInMinuttes"]);

            //TODO
            this.cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTime.Now.AddMinutes(1));  //konfigurerbart..

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


        public bool IsValidCheck(int oid, string verdi)
        {
            KodeverkKodeCacheKey kodeverkKodeCacheKey = new KodeverkKodeCacheKey() { OId = oid, Verdi = verdi };
            KodeverkKode kodeverkKode;
   
            klassifikasjonService.Synchronize(oid);


            //sjekk om kode finnes i cache

            if (kodeverkKodeCache.TryGetValue(kodeverkKodeCacheKey, out kodeverkKode))
            {
                logger.LogDebug($" {kodeverkKodeCacheKey}  funnet i cache");
                return true;
            }

            //sjekk om kode finnes i db-cache
            if (IsInContext(kodeverkKodeCacheKey))
                return true;
            else
                return false;

        }


        private bool IsInContext(KodeverkKodeCacheKey kodeverkKodeCacheKey)
        {
            //var kodeverkKode = tilgangKodeverkContext.KodeverkKoder.FirstOrDefault(k => k.OId == kodeverkKodeCacheKey.OId && k.Verdi.Equals(kodeverkKodeCacheKey.Verdi));
            var kodeverkKode = kodeverkRepository.GetKodeverkKode(kodeverkKodeCacheKey.OId, kodeverkKodeCacheKey.Verdi);

            if (kodeverkKode != null)
            {
                kodeverkKodeCache.Set<KodeverkKode>(kodeverkKodeCacheKey, kodeverkKode, this.cacheEntryOptions);
                logger.LogDebug($" {kodeverkKodeCacheKey}  lagt til i cache");
                return true;
            }
            return false;
        }

        private void UpdateContext(int oid, IEnumerable<KodeverkKode> updatedKodeverkKodeListe)
        {
            logger.LogDebug($"Update  Context  {oid} ");
            kodeverkRepository.UpdateKodeverkKoder(oid, updatedKodeverkKodeListe);

            //adde or uppdate Klassifikasjon
            var klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);

            if (klassifikasjon!=null)
            {
                klassifikasjon.Nedlasted = DateTime.Now;
            }  else
            {
                kodeverkRepository.AddKlassifikasjon(oid);
            }

            kodeverkRepository.SaveChanges();
        }

        







    }

}
