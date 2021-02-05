using System;

namespace Fhi.Lmr.Felles.TilgangKodeverk.Cache
{
    public class KlassifikasjonCacheKey : IEquatable<KlassifikasjonCacheKey>
    {
        public int OId { get; set; }
       
        public bool Equals(KlassifikasjonCacheKey other)
        {
            return OId == other.OId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KlassifikasjonCacheKey);
        }

        public override int GetHashCode()
        {
            return OId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{OId}";
        }

    }
}
