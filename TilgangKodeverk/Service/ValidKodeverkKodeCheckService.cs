using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TilgangKodeverk.DataAksess;
using TilgangKodeverk.Entities;
using TilgangKodeverk.Model.Dto;

namespace TilgangKodeverk.Service
{

    public class DictionaryKey
    {
        public int OId { get; set; }
        public string Verdi { get; set; }
    }


    public class ValidKodeverkKodeCheckService : IValidKodeverkKodeCheckService
    {
        private readonly TilgangKodeverkContext tilgangKodeverkContext;
        private readonly GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient;
        private readonly ILogger<ValidKodeverkKodeCheckService> logger;
        static public Dictionary<DictionaryKey, KodeverkKode> InMemoryCache = new Dictionary<DictionaryKey, KodeverkKode>();
        static public Dictionary<int, DateTime> CheckedForUpdate = new Dictionary<int, DateTime>();


        public int UpdateIntervalInMinuttes { get; set; }

        public ValidKodeverkKodeCheckService(IConfiguration configuration,TilgangKodeverkContext tilgangKodeverkContext, GrunndataKodeverkHttpKlient grunndataKodeverkHttpKlient, ILogger<ValidKodeverkKodeCheckService> logger)
        {
            this.tilgangKodeverkContext = tilgangKodeverkContext;
            this.grunndataKodeverkHttpKlient = grunndataKodeverkHttpKlient;
            this.logger = logger;

            this.UpdateIntervalInMinuttes = int.Parse(configuration["GrunndataKodeverk:UpdateIntervalInMinuttes"]);

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
            DictionaryKey dictionaryKey = new DictionaryKey() { OId = oid, Verdi = verdi };
            KodeverkKode kodeverkKode;

            //start henting av oppdatere verdier fra grunndata
            var task = CheckForUpdate(oid);

            //sjekk InMemoryCache
            if (InMemoryCache.TryGetValue(dictionaryKey, out kodeverkKode))
            {
                return true;
            }

            //sjekk context (db altså..)
            if (IsInContext(dictionaryKey))
                return true;

            //hent fra grunndata
            task.Wait();
            if (task.IsCompletedSuccessfully)
            {
                bool isUpdated = task.Result;
                if (isUpdated)
                {
                    InMemoryCache.Clear();
                    //sjekk om verdi nå ligger i context
                    return IsInContext(dictionaryKey);
                }
            }

            return false;
        }


        private bool IsInContext(DictionaryKey dictionaryKey)
        {
            var kodeverkKode = tilgangKodeverkContext.KodeverkKoder.FirstOrDefault(k => k.OId == dictionaryKey.OId && k.Verdi.Equals(dictionaryKey.Verdi));
            if (kodeverkKode != null)
            {
                InMemoryCache.Add(dictionaryKey, kodeverkKode);
                return true;
            }
            return false;
        }

        private void UpdateContext(int oid, IEnumerable<KodeverkKode> updated)
        {
            var deleteListe = tilgangKodeverkContext.KodeverkKoder.Where(k => k.OId == oid).ToList();
            tilgangKodeverkContext.KodeverkKoder.RemoveRange(deleteListe);
            tilgangKodeverkContext.KodeverkKoder.AddRange(updated);
            tilgangKodeverkContext.SaveChanges();
        }

        public Task<bool> CheckForUpdate(int oid)
        {
            bool isUpdated = false;

            //ikke sjekk igjen mindre mindre enn <> minutter siden forrige
            if (CheckedForUpdate.TryGetValue(oid, out DateTime lastchecked))
            {
                if (DateTime.Now.Subtract(lastchecked) < TimeSpan.FromMinutes(UpdateIntervalInMinuttes))
                    return Task.FromResult(isUpdated);
            }


            var task = Task.Run(() =>
            {
                try
                {
                    logger.LogDebug($"CheckForUpdate Oid={oid}");
                    var updatedKodes = grunndataKodeverkHttpKlient.GetKoderByOid(oid);
                    if (updatedKodes != null)
                    {
                        IEnumerable<KodeverkKode>  result = updatedKodes.Select(u => new KodeverkKode() { OId = u.OId, Navn = u.Navn, Verdi = u.Verdi }).ToList();
                        UpdateContext(oid, result);
                        isUpdated = true;

                        logger.LogDebug($"CheckForUpdate updatedKodes length={result.Count()}");
                    }

                    CheckedForUpdate[oid] = DateTime.Now;
                }
                catch (Exception exp)
                {
                    logger.LogError($"CheckForUpdate Exception={exp.Message}");
                }

            });

            task.Wait();

            return Task.FromResult(isUpdated);
        }







    }

}
