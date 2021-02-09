using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Fhi.Lmr.Felles.TilgangKodeverk.Service;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xunit;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Test.TestKlassifikasjonService
{

    [Trait("KlassifikasjonService", "Synchronize")]
    public class SynchronizeUnitTest
    {
        const int KategoriHelsepersonelloOid = 9060;
        List<Fhi.Lmr.Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode> KategoriHelsepersonellKodeListe;
        KlassifikasjonCacheKey kategoriHelsepersonellklassifikasjonCacheKey = new KlassifikasjonCacheKey() { OId = KategoriHelsepersonelloOid };

        IKlassifikasjonService sut;
        IOptions<GrunndataKodeverkOption> grunndataKodeverkOption;
        IKodeverkRepository kodeverkRepository;
        KodeverkKodeMemoryCache kodeverkKodeMemoryCache;
        Mock<ILogger<KlassifikasjonService>> logger = new Mock<ILogger<KlassifikasjonService>>();
        Mock<IGrunndataKodeverkHttpKlient> grunndataKodeverkHttpKlient;

        //     Mock<IHttpClientFactory> HttpClientFactory;

        public SynchronizeUnitTest()
        {
            KategoriHelsepersonellKodeListe = new List<Fhi.Lmr.Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode>()
           {
               new Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode() { OId=KategoriHelsepersonelloOid,Verdi="FA"},
               new Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode() { OId=KategoriHelsepersonelloOid,Verdi="SP"},
           };



            grunndataKodeverkOption = Options.Create(new GrunndataKodeverkOption() { KodeverkKodeExpiration = "1", UpdateIntervalInMinuttes = "1" });


            kodeverkRepository = new InMemoryKodeverkRepository();
            kodeverkKodeMemoryCache = new KodeverkKodeMemoryCache();
            logger = new Mock<ILogger<KlassifikasjonService>>();
            grunndataKodeverkHttpKlient = new Mock<IGrunndataKodeverkHttpKlient>();

            sut = new KlassifikasjonService(grunndataKodeverkOption, kodeverkRepository, kodeverkKodeMemoryCache, logger.Object, grunndataKodeverkHttpKlient.Object);
        }


        [Theory]
        [InlineData(9060, true)]
        [InlineData(9061, false)]
        public void TestReturnStatusOk(int oid, bool isGyldig)
        {
            KlassifikasjonCacheKey klassifikasjonCacheKey = new KlassifikasjonCacheKey() { OId = oid };
            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns((int oid, DateTime nedlastet) => CreateGetKoderByOidAndNedlastetResponse(oid, nedlastet));

            //
            Assert.Equal(0, kodeverkKodeMemoryCache.Cache.Count);
            sut.Synchronize(oid);

            //klasssifikasjon skal nå ligge i cache
            Assert.Equal(1, kodeverkKodeMemoryCache.Cache.Count);
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(klassifikasjonCacheKey, out Klassifikasjon klassifikasjon));
            //sjekk gyldighet
            Assert.Equal(isGyldig, klassifikasjon.Gyldig);

        }

        [Theory]
        [InlineData(9060, "2021.01.01")]
        public void TestUseKlassifikasjonIAlreadyInCache(int oid, string date)
        {
            DateTime nedlastet = DateTime.Parse(date);

            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
               .Returns((int oid, DateTime nedlastet) => CreateGetKoderByOidAndNedlastetResponse(oid, nedlastet));

            //første kall , 
            // skal kalle grunndataKodeverkHttpKlient
            // lagre klassifikasjon i repository
            // sette   klassifikasjon i cache

            sut.Synchronize(oid);

            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Once());

            Assert.NotNull(kodeverkRepository.GetKlassifikasjon(oid));
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kategoriHelsepersonellklassifikasjonCacheKey, out Klassifikasjon klassifikasjon));

            var lastChecked = klassifikasjon.Lastchecked;

            //nytt kall , gjenbruk Klassifikasjon i cache
            sut.Synchronize(oid);
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kategoriHelsepersonellklassifikasjonCacheKey, out Klassifikasjon klassifikasjon2));

            //skal være samme
            Assert.Equal(klassifikasjon2, klassifikasjon);
            //grunndata skal kun være kall en gang
            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Once());


        }

        [Theory]
        [InlineData(9060, "2021.01.01")]
        public void TestUseKlassifikasjonIRepositoryAfterCacheClear(int oid, string date)
        {
            DateTime nedlastet = DateTime.Parse(date);

            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
               .Returns((int oid, DateTime nedlastet) => CreateGetKoderByOidAndNedlastetResponse(oid, nedlastet));

            //første kall , 
            // skal kalle grunndataKodeverkHttpKlient
            // lagre klassifikasjon i repository
            // sette   klassifikasjon i cache

            sut.Synchronize(oid);

            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Once());

            Assert.NotNull(kodeverkRepository.GetKlassifikasjon(oid));
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kategoriHelsepersonellklassifikasjonCacheKey, out Klassifikasjon klassifikasjon));

            var lastChecked = klassifikasjon.Lastchecked;

            //clear cache
            kodeverkKodeMemoryCache.ClearCache();


            //nytt kall , gjenbruk Klassifikasjon i cache
            sut.Synchronize(oid);
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kategoriHelsepersonellklassifikasjonCacheKey, out Klassifikasjon klassifikasjon2));

            //skal være samme pga test-implementasjon av repository...
            Assert.Equal(klassifikasjon2, klassifikasjon);
            //grunndata skal kun være kall en gang
            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Once());
        }


        [Theory]
        [InlineData(9060, "2021.01.01")]
        public void Test_Lastchecked(int oid, string date)
        {
            //dersom forskjell på siste Lastchecked og nåtid  er større ennUpdateIntervalInMinuttes , skal data lastes på nytt fra grunndata

            DateTime nedlastet = DateTime.Parse(date);
            grunndataKodeverkOption = Options.Create(new GrunndataKodeverkOption() { KodeverkKodeExpiration = "1", UpdateIntervalInMinuttes = "0" });
            sut = new KlassifikasjonService(grunndataKodeverkOption, kodeverkRepository, kodeverkKodeMemoryCache, logger.Object, grunndataKodeverkHttpKlient.Object);

            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
               .Returns((int oid, DateTime nedlastet) => CreateGetKoderByOidAndNedlastetResponse(oid, nedlastet));

            //første kall , 
            // skal kalle grunndataKodeverkHttpKlient
            // lagre klassifikasjon i repository
            // sette   klassifikasjon i cache

            sut.Synchronize(oid);

            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Once());

            Assert.NotNull(kodeverkRepository.GetKlassifikasjon(oid));
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kategoriHelsepersonellklassifikasjonCacheKey, out Klassifikasjon klassifikasjon));

            var lastChecked = klassifikasjon.Lastchecked;

            //clear cache
            // kodeverkKodeMemoryCache.ClearCache();



            //nytt kall  , lastchecked er nå for gammelt, skal trigge nytt kall mot grunndata

            sut.Synchronize(oid);
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kategoriHelsepersonellklassifikasjonCacheKey, out Klassifikasjon klassifikasjon2));
            //skal ikke være samme 
            Assert.NotEqual(klassifikasjon2, klassifikasjon);

            Assert.True(klassifikasjon2.Nedlasted > DateTime.MinValue);

            //grunndata skal være kalt 2 ganger
            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Exactly(2));
        }






        [Fact]
        public void TestThrowException()
        {
            const int oid = 6090;

            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
              .Returns((int oid, DateTime nedlastet) => throw new Exception("Test"));
            sut.Synchronize(oid);

            //AktivSynkronisering skal være sått av
            Assert.False(grunndataKodeverkOption.Value.AktivSynkronisering);




        }


        [Theory]
        [InlineData(9060, "2021.01.01")]
        public void Test_OppdaterKoderL(int oid, string date)
        {
            //dersom forskjell på siste Lastchecked og nåtid  er større ennUpdateIntervalInMinuttes , skal data lastes på nytt fra grunndata

            DateTime nedlastet = DateTime.Parse(date);

            //sett UpdateIntervalInMinuttes=0 for å tvinge kall til grunndata-api
            grunndataKodeverkOption = Options.Create(new GrunndataKodeverkOption() { KodeverkKodeExpiration = "1", UpdateIntervalInMinuttes = "0" });
            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
                  .Returns((int oid, DateTime nedlastet) => CreateGetKoderByOidAndNedlastetResponse(oid, nedlastet));

            sut = new KlassifikasjonService(grunndataKodeverkOption, kodeverkRepository, kodeverkKodeMemoryCache, logger.Object, grunndataKodeverkHttpKlient.Object);
       
            sut.Synchronize(oid);

            grunndataKodeverkHttpKlient.Verify(mock => mock.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Once());

            //sjekk  koder i repository
            Assert.NotNull(kodeverkRepository.GetKodeverkKode(oid, "FA"));
    

            //simuler nye koder i grunndata..disse kal erstatte de eksistrende...
            grunndataKodeverkHttpKlient
                .Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
                .Returns((int oid, DateTime nedlastet) => new GetKoderByOidAndNedlastetResponse()
                {
                    Status = StatusEnum.OK,
                    KodeverkKoder = new List<Fhi.Lmr.Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode>()
                        {
                            new Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode() { OId=KategoriHelsepersonelloOid,Verdi="PS"},
                            new Grunndata.Kodeverk.Apiklient.Dto.KodeverkKode() { OId=KategoriHelsepersonelloOid,Verdi="PE"},
                        }

                });


            //last ned nye koder fra grunndata
            sut.Synchronize(oid);
            //de gamle kodenen skal være slettet
            Assert.Null(kodeverkRepository.GetKodeverkKode(oid, "FA"));
            //de nye skal være aktiv
            Assert.NotNull(kodeverkRepository.GetKodeverkKode(oid, "PS"));
        }








        [Fact]
        public void TestReturnStatusError()
        {
            const int oid = 6090;

            grunndataKodeverkHttpKlient.Setup(m => m.GetKoderByOidAndNedlastet(It.IsAny<int>(), It.IsAny<DateTime>()))
             .Returns((int oid, DateTime nedlastet) => new GetKoderByOidAndNedlastetResponse() { Status = StatusEnum.Error });

            sut.Synchronize(oid);
            Assert.False(grunndataKodeverkOption.Value.AktivSynkronisering);


        }


        private GetKoderByOidAndNedlastetResponse CreateGetKoderByOidAndNedlastetResponse(int oid, DateTime nedlastet)
        {
            DateTime grunndataNedlastet = DateTime.Parse("2020.12.31");

            var response = new GetKoderByOidAndNedlastetResponse() { Exception = null, KodeverkKoder = null };

            if (oid != KategoriHelsepersonelloOid)
            {
                response.Status = StatusEnum.NotFound;
                return response;
            }

            if (nedlastet == DateTime.MinValue)
            {
                response.Status = StatusEnum.OK;
                response.KodeverkKoder = KategoriHelsepersonellKodeListe;
            }

            if (nedlastet > DateTime.MinValue)
            {
                response.Status = StatusEnum.NoContent;
            }


            return response;

        }


    }
}
