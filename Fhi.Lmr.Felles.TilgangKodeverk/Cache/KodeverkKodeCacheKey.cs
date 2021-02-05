using System;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Cache
{
    public class KodeverkKodeCacheKey : IEquatable<KodeverkKodeCacheKey>
    {
        public int OId { get; set; }
        public string Verdi { get; set; }
       
        public bool Equals(KodeverkKodeCacheKey other)
        {
            return OId == other.OId && Verdi.Equals(other.Verdi);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KodeverkKodeCacheKey);
        }

        public override int GetHashCode()
        {
            return OId.GetHashCode() + Verdi.GetHashCode();
        }

        public override string ToString()
        {
            return $"{OId}-{Verdi}";
        }

    }
}
