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
        private readonly int UpdateIntervalInMinuttes; 
        private readonly MemoryCacheEntryOptions cacheEntryOptions;

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
    
            //sjekk cache, eventuelt hent fra repoistory
            if (!kodeverkKodeCache.TryGetValue(klassifikasjonCacheKey,  out Klassifikasjon klassifikasjon))
            {
                klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);
                if (klassifikasjon != null)
                    kodeverkKodeCache.Set<Klassifikasjon>(klassifikasjonCacheKey, klassifikasjon, this.cacheEntryOptions);
            } else  {
                //hvis klassifikasjon allerede er funnet å være ugyldig
                if (!klassifikasjon.Gyldig)
                    return;
            }



            // hvis kodeverk mangler eller ikke synkronisert med grunndata , start synkronisering
            if (klassifikasjon == null || (!klassifikasjon.Lastchecked.HasValue) || (DateTime.Now.Subtract(klassifikasjon.Lastchecked.Value) > TimeSpan.FromMinutes(UpdateIntervalInMinuttes)))
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

                //kall grunnndata rest-api 
                var response= grunndataKodeverkHttpKlient.GetKoderByOidAndNedlastet(oid, nedlasted);

                if (response.Status==StatusEnum.OK)
                {
                     logger.LogDebug($"SynchronizeWithGrunndata oppdatertKodeverkListe:length={response.KodeverkKoder.Count()}");
                     var oppdatertKodeverkListe = response.KodeverkKoder.Select(u => new KodeverkKode() { OId = u.OId, Navn = u.Navn, Verdi = u.Verdi }).ToList();
                     OppdaterRepoistory(oid, oppdatertKodeverkListe);
         
                    isUpdated = true;
                }
                else if (response.Status == StatusEnum.NoContent)
                {
                    //sett inn null object hvis grunndata NOT_FOUND
                    logger.LogDebug($"SynchronizeWithGrunndata Not found ={oid} updating cache");
                    isUpdated = true;
                }

                else if (response.Status == StatusEnum.NotFound)
                {
                    //sett inn null object hvis grunndata NOT_FOUND
                    logger.LogDebug($"SynchronizeWithGrunndata Not found ={oid}");
                    LeggDummyKlassifikasjonTilCache(oid);

                } else  //annen ukjent error
                {
                    //sett inn null object hvis exception 
                    logger.LogError($"SynchronizeWithGrunndata Error");
                    LeggDummyKlassifikasjonTilCache(oid);
                }
            }
            catch (Exception exp)
            {
                logger.LogError($"SynchronizeWithGrunndata Exception={exp.Message}");
                throw;
            }


            if (klassifikasjon != null)
            {
                klassifikasjon.Lastchecked = DateTime.Now;
            }
               

            return isUpdated;
        }


        private Klassifikasjon LeggDummyKlassifikasjonTilCache(int oid)
        {
            KlassifikasjonCacheKey klassifikasjonCacheKey = new KlassifikasjonCacheKey() { OId = oid };
            Klassifikasjon klassifikasjon = new Klassifikasjon() { OId=oid, Lastchecked = DateTime.Now, Gyldig=false };
            kodeverkKodeCache.Set<Klassifikasjon>(new KlassifikasjonCacheKey() { OId = oid }, klassifikasjon, this.cacheEntryOptions);
            return klassifikasjon;
        }


        private void OppdaterRepoistory(int oid, IEnumerable<KodeverkKode> oppdatertKodeverkListe)
        {
            logger.LogDebug($"OppdaterRepoistory  {oid} ");
            kodeverkRepository.UpdateKodeverkKoder(oid, oppdatertKodeverkListe);

            //legg til eller oppdater Klassifikasjon
            var klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);

            if (klassifikasjon != null)
            {
                klassifikasjon.Nedlasted = DateTime.Now;
            }
            else
            {
                kodeverkRepository.AddKlassifikasjon(new Klassifikasjon() { OId = oid, Nedlasted = DateTime.Now });
            }

            kodeverkRepository.SaveChanges();
        }
    }
}
