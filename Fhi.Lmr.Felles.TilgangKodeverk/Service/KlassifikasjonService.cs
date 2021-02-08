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
    
            //sjekk cache, eventuelt hent fra repoistory
            if (!kodeverkKodeCache.TryGetValue(klassifikasjonCacheKey,  out Klassifikasjon klassifikasjon))
            {
                klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);
                if (klassifikasjon != null)
                    kodeverkKodeCache.Set<Klassifikasjon>(klassifikasjonCacheKey, klassifikasjon, this.cacheEntryOptions);
            };


            // hvis kodeverk mangler eller ikke synkronisert med grunndata , start synkronisering
            if (klassifikasjon == null || (!klassifikasjon.Lastchecked.HasValue) || (DateTime.Now.Subtract(klassifikasjon.Lastchecked.Value) > TimeSpan.FromMinutes(UpdateIntervalInMinuttes)))
            {
                if (klassifikasjon!=null)
                  logger.LogDebug($"SynchronizeWithGrunndata Oid={oid}   Lastchecked={klassifikasjon.Lastchecked?.ToString()}");
                else
                    logger.LogDebug($"SynchronizeWithGrunndata Oid={oid}   no klassifikasjon");


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

                //kall grunnndaat rest
                var response= grunndataKodeverkHttpKlient.GetKoderByOidAndNedlastet(oid, nedlasted);

                if (response.Status==StatusEnum.OK)
                {
                     logger.LogDebug($"SynchronizeWithGrunndata oppdatertKodeverkListe:length={response.KodeverkKoder.Count()}");
                     var oppdatertKodeverkListe = response.KodeverkKoder.Select(u => new KodeverkKode() { OId = u.OId, Navn = u.Navn, Verdi = u.Verdi }).ToList();
                     UpdateDB(oid, oppdatertKodeverkListe);
                     isUpdated = true;
                }
                else if (response.Status == StatusEnum.NoContent)
                {
                    //sett inn null object hvis grunndata NOT_FOUND
                    logger.LogDebug($"SynchronizeWithGrunndata Not found ={oid} updating cache");
                    kodeverkKodeCache.Set<Klassifikasjon>(new KlassifikasjonCacheKey() { OId = oid }, new Klassifikasjon() { Lastchecked = DateTime.Now.AddMinutes(3) }, this.cacheEntryOptions);
                }

                else if (response.Status == StatusEnum.NotFound)
                {
                    //sett inn null object hvis grunndata NOT_FOUND
                    logger.LogDebug($"SynchronizeWithGrunndata Not found ={oid}");
                    kodeverkKodeCache.Set<Klassifikasjon>(new KlassifikasjonCacheKey() { OId = oid }, new Klassifikasjon() { Lastchecked = DateTime.Now.AddMinutes(3) }, this.cacheEntryOptions);

                } else  //error
                {
                    //sett inn null object hvis exception 
                    logger.LogError($"SynchronizeWithGrunndata Exception={response.Exception.Message}");
                    kodeverkKodeCache.Set<Klassifikasjon>(new KlassifikasjonCacheKey() { OId = oid }, new Klassifikasjon() { Lastchecked = DateTime.Now.AddMinutes(3) }, this.cacheEntryOptions);
                }


              
            }
            catch (Exception exp)
            {
                logger.LogError($"SynchronizeWithGrunndata Exception={exp.Message}");
                throw;
            }

            if (klassifikasjon != null)
                klassifikasjon.Lastchecked = DateTime.Now;


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
