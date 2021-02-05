using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using System.Linq;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Service
{
    public class KlassifikasjonService : IKlassifikasjonService
    {
        private readonly IKodeverkRepository kodeverkRepository;
        private readonly ILogger<KlassifikasjonService> logger;
        private readonly GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient;
        private readonly MemoryCache kodeverkKodeCache;

        public int UpdateIntervalInMinuttes { get; }

        public KlassifikasjonService(IConfiguration configuration, IKodeverkRepository kodeverkRepository, KodeverkKodeMemoryCache kodeverkKodeMemoryCache, ILogger<KlassifikasjonService> logger, GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient)
        {
            this.kodeverkRepository = kodeverkRepository;
            this.logger = logger;
            this.grunndataKodeverkHttpKlient = grunndataKodeverkHttpKlient;
            this.kodeverkKodeCache = kodeverkKodeMemoryCache.Cache;
            this.UpdateIntervalInMinuttes = int.Parse(configuration["GrunndataKodeverk:UpdateIntervalInMinuttes"]);
        }


        public void Synchronize(int oid)
        {
            KlassifikasjonCacheKey klassifikasjonCacheKey = new KlassifikasjonCacheKey() { OId = oid };
            Klassifikasjon klassifikasjon = null;

            if (!kodeverkKodeCache.TryGetValue(klassifikasjonCacheKey, out klassifikasjon))
            {
                // klassifikasjon = tilgangKodeverkContext.Klassifikasjon.Where(k => k.OId == oid).AsNoTracking().FirstOrDefault();
                klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);
                if (klassifikasjon != null)
                    kodeverkKodeCache.Set<Klassifikasjon>(klassifikasjonCacheKey, klassifikasjon);

                // hvis kodeverk mangler eller ikke synkronisert med grunndata , start synkroniserings-task

            };

            if (klassifikasjon == null || (!klassifikasjon.Lastchecked.HasValue) || (DateTime.Now.Subtract(klassifikasjon.Lastchecked.Value) < TimeSpan.FromMinutes(UpdateIntervalInMinuttes)))
            {
                logger.LogDebug($"før  CheckForUpdate()");
                bool isUpdated = CheckForUpdate(oid, klassifikasjon);
                logger.LogDebug($"Etter  CheckForUpdate()");
            }

        }


        public bool CheckForUpdate(int oid, Klassifikasjon klassifikasjon)
        {
            bool isUpdated = false;
            try
            {
                DateTime nedlasted = (klassifikasjon != null) ? klassifikasjon.Nedlasted : DateTime.MinValue;
                logger.LogDebug($"CheckForUpdate Oid={oid}   nedlasted={nedlasted}");
                var updatedKodes = grunndataKodeverkHttpKlient.GetKoderByOidAndNedlastet(oid, nedlasted);
                if (updatedKodes != null && updatedKodes.Count() > 0)
                {
                    IEnumerable<KodeverkKode> result = updatedKodes.Select(u => new KodeverkKode() { OId = u.OId, Navn = u.Navn, Verdi = u.Verdi }).ToList();
                    UpdateContext(oid, result);
                    isUpdated = true;
                    logger.LogDebug($"CheckForUpdate updatedKodes length={result.Count()}");
                }
                if (klassifikasjon != null)
                    klassifikasjon.Lastchecked = DateTime.Now;

            }
            catch (Exception exp)
            {
                logger.LogError($"CheckForUpdate Exception={exp.Message}");
            }

            return isUpdated;


        }

        private void UpdateContext(int oid, IEnumerable<KodeverkKode> updatedKodeverkKodeListe)
        {
            logger.LogDebug($"UpdateContext  {oid} ");

            kodeverkRepository.UpdateKodeverkKoder(oid, updatedKodeverkKodeListe);

            //adde or uppdate Klassifikasjon
            //var klassifikasjon = tilgangKodeverkContext.Klassifikasjon.Where(k => k.OId == oid).FirstOrDefault();
            var klassifikasjon = kodeverkRepository.GetKlassifikasjon(oid);

            if (klassifikasjon != null)
            {
                klassifikasjon.Nedlasted = DateTime.Now;
            }
            else
            {
                //  tilgangKodeverkContext.Klassifikasjon.Add(new Klassifikasjon() { OId = oid, Nedlasted = DateTime.Now });
                kodeverkRepository.AddKlassifikasjon(oid);
            }

            kodeverkRepository.SaveChanges();
        }
    }
}
