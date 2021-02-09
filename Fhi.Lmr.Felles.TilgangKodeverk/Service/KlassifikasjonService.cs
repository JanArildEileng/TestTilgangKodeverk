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
        private readonly IOptions<GrunndataKodeverkOption> grunndataKodeverkOption;
        private readonly IKodeverkRepository kodeverkRepository;
        private readonly KodeverkKodeMemoryCache kodeverkKodeMemoryCache;
        private readonly ILogger<KlassifikasjonService> logger;
        private readonly IGrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient;
        private readonly MemoryCache kodeverkKodeCache;
        private readonly int UpdateIntervalInMinuttes; 
        private readonly MemoryCacheEntryOptions cacheEntryOptions;

        public KlassifikasjonService(IOptions<GrunndataKodeverkOption> grunndataKodeverkOption,IKodeverkRepository kodeverkRepository, KodeverkKodeMemoryCache kodeverkKodeMemoryCache, ILogger<KlassifikasjonService> logger, IGrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient)
        {
            this.grunndataKodeverkOption = grunndataKodeverkOption;
            this.kodeverkRepository = kodeverkRepository;
            this.kodeverkKodeMemoryCache = kodeverkKodeMemoryCache;
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
                DateTime nedlastet = (klassifikasjon != null) ? klassifikasjon.Nedlasted : DateTime.MinValue;
                klassifikasjon = SynchronizeWithGrunndata(oid, nedlastet);
                if (klassifikasjon!=null)
                    kodeverkKodeCache.Set<Klassifikasjon>(klassifikasjonCacheKey, klassifikasjon, this.cacheEntryOptions);
                else
                {
                    //det oppstod feil...
                    logger.LogInformation("Feil i KlassifikasjonService , Synchronize mot Grunndata stoppet");
                    grunndataKodeverkOption.Value.AktivSynkronisering = false;
                }


            }
        }

        private Klassifikasjon SynchronizeWithGrunndata(int oid, DateTime nedlastet)
        {
            Klassifikasjon klassifikasjon = new Klassifikasjon() { OId = oid ,Nedlasted=nedlastet };
            try
            {
                logger.LogDebug($"SynchronizeWithGrunndata Oid={oid}   nedlasted={nedlastet}");

                //kall grunnndata rest-api 
                var response= grunndataKodeverkHttpKlient.GetKoderByOidAndNedlastet(oid, nedlastet);

                if (response.Status==StatusEnum.OK)
                {
                     logger.LogDebug($"SynchronizeWithGrunndata oppdatertKodeverkListe:length={response.KodeverkKoder.Count()}");
                     var oppdatertKodeverkListe = response.KodeverkKoder.Select(u => new KodeverkKode() { OId = u.OId, Navn = u.Navn, Verdi = u.Verdi }).ToList();
                     OppdaterRepoistory(klassifikasjon,oid, oppdatertKodeverkListe);
                }
                else if (response.Status == StatusEnum.NoContent)
                {
                    //sett inn null object hvis grunndata NOT_FOUND
                    logger.LogDebug($"SynchronizeWithGrunndata Not found ={oid} updating cache");
                }

                else if (response.Status == StatusEnum.NotFound)
                {
                    //sett inn null object hvis grunndata NOT_FOUND
                    logger.LogDebug($"SynchronizeWithGrunndata Not found ={oid}");
                    klassifikasjon.Gyldig = false;

                } else  //annen ukjent error
                {
                    //sett inn null object hvis exception 
                    logger.LogError($"SynchronizeWithGrunndata Error");
                    klassifikasjon=null;
                }
            }
            catch (Exception exp)
            {
                logger.LogError($"SynchronizeWithGrunndata Exception={exp.Message}");
                klassifikasjon = null;
            }

            if (klassifikasjon != null)
            {
                klassifikasjon.Lastchecked = DateTime.Now;
            }

            return klassifikasjon;
        }


        private Klassifikasjon LeggDummyKlassifikasjonTilCache(int oid)
        {
            KlassifikasjonCacheKey klassifikasjonCacheKey = new KlassifikasjonCacheKey() { OId = oid };
            Klassifikasjon klassifikasjon = new Klassifikasjon() { OId=oid, Lastchecked = DateTime.Now, Gyldig=false };
            kodeverkKodeCache.Set<Klassifikasjon>(new KlassifikasjonCacheKey() { OId = oid }, klassifikasjon, this.cacheEntryOptions);
            return klassifikasjon;
        }


        private void OppdaterRepoistory(Klassifikasjon klassifikasjon,int oid, IEnumerable<KodeverkKode> oppdatertKodeverkListe)
        {
            logger.LogDebug($"OppdaterRepoistory  {oid} ");
            klassifikasjon.Nedlasted = DateTime.Now;

            kodeverkRepository.UpdateKodeverkKoder(oid, oppdatertKodeverkListe);

            //legg til eller oppdater Klassifikasjon
            var eksisrendeKlassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);

            if (eksisrendeKlassifikasjon != null)
            {
                eksisrendeKlassifikasjon.Nedlasted = klassifikasjon.Nedlasted;
            }
            else
            {
                kodeverkRepository.AddKlassifikasjon(klassifikasjon);
            }

            kodeverkRepository.SaveChanges();
            //kodeverk oppdatert , må tomme cache
            logger.LogDebug($"ClearCache()");
            kodeverkKodeMemoryCache.ClearCache();

        }
    }
}
