using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Service
{
    public class KlassifikasjonService : IKlassifikasjonService
    {
        private readonly IKodeverkRepository kodeverkRepository;
        private readonly ILogger<KlassifikasjonService> logger;
        private readonly GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient;
        private readonly MemoryCache kodeverkKodeCache;

        public int UpdateIntervalInMinuttes { get; }

        private MemoryCacheEntryOptions cacheEntryOptions;

        public KlassifikasjonService(IOptions<GrunndataKodeverkOption> grunndataKodeverkOption,IKodeverkRepository kodeverkRepository, KodeverkKodeMemoryCache kodeverkKodeMemoryCache, ILogger<KlassifikasjonService> logger, GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient)
        {
            this.kodeverkRepository = kodeverkRepository;
            this.logger = logger;
            this.grunndataKodeverkHttpKlient = grunndataKodeverkHttpKlient;
            this.kodeverkKodeCache = kodeverkKodeMemoryCache.Cache;
            this.UpdateIntervalInMinuttes = int.Parse(grunndataKodeverkOption.Value.UpdateIntervalInMinuttes);
            this.cacheEntryOptions = new MemoryCacheEntryOptions()
              .SetAbsoluteExpiration(DateTime.Now.AddMinutes(int.Parse(grunndataKodeverkOption.Value.KodeverkKodeExpiration)));
        }


        public void Synchronize(int oid)
        {
            KlassifikasjonCacheKey klassifikasjonCacheKey = new KlassifikasjonCacheKey() { OId = oid };
    
            if (!kodeverkKodeCache.TryGetValue(klassifikasjonCacheKey,  out Klassifikasjon klassifikasjon))
            {
                // klassifikasjon = tilgangKodeverkContext.Klassifikasjon.Where(k => k.OId == oid).AsNoTracking().FirstOrDefault();
                klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);
                if (klassifikasjon != null)
                    kodeverkKodeCache.Set<Klassifikasjon>(klassifikasjonCacheKey, klassifikasjon, this.cacheEntryOptions);
            };

            // hvis kodeverk mangler eller ikke synkronisert med grunndata , start synkronisering
            if (klassifikasjon == null || (!klassifikasjon.Lastchecked.HasValue) || (DateTime.Now.Subtract(klassifikasjon.Lastchecked.Value) < TimeSpan.FromMinutes(UpdateIntervalInMinuttes)))
            {
                bool isUpdated = SynchronizeWithGrunndata(oid, klassifikasjon);
            }
        }

        private bool SynchronizeWithGrunndata(int oid, Klassifikasjon klassifikasjon)
        {
            bool isUpdated = false;
            try
            {
                DateTime nedlasted = (klassifikasjon != null) ? klassifikasjon.Nedlasted : DateTime.MinValue;
                logger.LogDebug($"SynchronizeWithGrunndata Oid={oid}   nedlasted={nedlasted}");

                //kall grunnndat rest
                var grunndataKodeListe = grunndataKodeverkHttpKlient.GetKoderByOidAndNedlastet(oid, nedlasted);
                
                if (grunndataKodeListe != null && grunndataKodeListe.Count() > 0)
                {
                    var oppdatertKodeverkListe = grunndataKodeListe.Select(u => new KodeverkKode() { OId = u.OId, Navn = u.Navn, Verdi = u.Verdi }).ToList();
                    UpdateDB(oid, oppdatertKodeverkListe);
                    isUpdated = true;
                    logger.LogDebug($"SynchronizeWithGrunndata oppdatertKodeverkListe:length={oppdatertKodeverkListe.Count()}");
                }
                if (klassifikasjon != null)
                    klassifikasjon.Lastchecked = DateTime.Now;

            }
            catch (Exception exp)
            {
                logger.LogError($"SynchronizeWithGrunndata Exception={exp.Message}");
            }

            return isUpdated;
        }

        private void UpdateDB(int oid, IEnumerable<KodeverkKode> oppdatertKodeverkListe)
        {
            logger.LogDebug($"UpdateDB  {oid} ");
            kodeverkRepository.UpdateKodeverkKoder(oid, oppdatertKodeverkListe);

            //adde or uppdate Klassifikasjon
            var klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);

            if (klassifikasjon != null)
            {
                klassifikasjon.Nedlasted = DateTime.Now;
            }
            else
            {
                kodeverkRepository.AddKlassifikasjon(oid);
            }

            kodeverkRepository.SaveChanges();
        }
    }
}
