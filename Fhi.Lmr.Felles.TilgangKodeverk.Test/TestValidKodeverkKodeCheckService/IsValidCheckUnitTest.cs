using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Fhi.Lmr.Felles.TilgangKodeverk.Service;
using Fhi.Lmr.Felles.TilgangKodeverk.Test.TestKlassifikasjonService;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Test.TestValidKodeverkKodeCheckService
{

    [Trait( "ValidKodeverkKodeCheckService", "IsValidCheck")]
    public class IsValidCheckUnitTest
    {
        const int KategoriHelsepersonelloOid = 9060;
        IValidKodeverkKodeCheckService sut;
        IOptions<GrunndataKodeverkOption> grunndataKodeverkOption;
        IKodeverkRepository kodeverkRepository;
        KodeverkKodeMemoryCache kodeverkKodeMemoryCache;
        Mock<ILogger<ValidKodeverkKodeCheckService>> logger = new Mock<ILogger<ValidKodeverkKodeCheckService>>();
        Mock<IKlassifikasjonService> klassifikasjonService = new Mock<IKlassifikasjonService>();
      
            public IsValidCheckUnitTest()
        {
            grunndataKodeverkOption = Options.Create(new GrunndataKodeverkOption() { KodeverkKodeExpiration = "1", UpdateIntervalInMinuttes = "1" });
            kodeverkRepository = new InMemoryKodeverkRepository();
            kodeverkKodeMemoryCache = new KodeverkKodeMemoryCache();

            klassifikasjonService.Setup(m => m.Synchronize(It.IsAny<int>()));

            //legg inn gyldige koder
            kodeverkRepository.UpdateKodeverkKoder(KategoriHelsepersonelloOid, new List<KodeverkKode>()
            {
                new KodeverkKode() { OId=KategoriHelsepersonelloOid,Verdi="FA"},
                new KodeverkKode() { OId=KategoriHelsepersonelloOid,Verdi="SP"},

            });



            sut = new ValidKodeverkKodeCheckService(grunndataKodeverkOption, logger.Object, kodeverkKodeMemoryCache, kodeverkRepository, klassifikasjonService.Object);
        }

        [Theory]
        [InlineData(9060,"FA", true)]
        [InlineData(9060, "JAN", false)]
        [InlineData(9061,"FA", false)]
        public void IsValidTest(int oid,string verdi,bool gyldig)
        {

            Assert.Equal(gyldig, sut.IsValidCheck(oid, verdi));

            //kode skal ligge i cache, med riktig gyldighet....
            KodeverkKodeCacheKey kodeverkKodeCacheKey = new KodeverkKodeCacheKey() { OId = oid, Verdi = verdi };
            Assert.True(kodeverkKodeMemoryCache.Cache.TryGetValue(kodeverkKodeCacheKey, out KodeverkKode kodeverkKode));

            Assert.Equal(gyldig, kodeverkKode.Gyldig);
        }

        [Theory]
        [InlineData(9060, "FA", true)]
        [InlineData(9060, "JAN", false)]
        [InlineData(9061, "FA", false)]
        public void ClearCacheTest(int oid, string verdi, bool gyldig)
        {

            Assert.Equal(gyldig, sut.IsValidCheck(oid, verdi));

            //kode skal ligge i cache, med riktig gyldighet....
            kodeverkKodeMemoryCache.ClearCache();
            //koder skal være fjernetfra cache...
            KodeverkKodeCacheKey kodeverkKodeCacheKey = new KodeverkKodeCacheKey() { OId = oid, Verdi = verdi };
            Assert.False(kodeverkKodeMemoryCache.Cache.TryGetValue(kodeverkKodeCacheKey, out KodeverkKode kodeverkKode));

        }




    }
}
