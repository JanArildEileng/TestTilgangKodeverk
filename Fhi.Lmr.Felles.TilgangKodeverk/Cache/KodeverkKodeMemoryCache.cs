using Microsoft.Extensions.Caching.Memory;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Cache
{
    public class KodeverkKodeMemoryCache
    {
        public MemoryCache Cache { get; set; }
        public KodeverkKodeMemoryCache()
        {      
            Cache = new MemoryCache(new MemoryCacheOptions
            {
            });
        }

        public int ClearCache()
        {
            Cache.Compact(1.0);

            return Cache.Count;
        }
    }
}
