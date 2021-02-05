using Fhi.Lmr.Felles.TilgangKodeverk.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;

namespace TilgangKodeverk.DataAksess
{
    public class TilgangKodeverkContext : DbContext
    {
        public TilgangKodeverkContext(DbContextOptions<TilgangKodeverkContext> options) : base(options) { }


        public DbSet<KodeverkKode> KodeverkKoder { get; set; }
        public DbSet<Klassifikasjon> Klassifikasjon { get; set; }
        


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Klassifikasjon>(
          e => e.HasKey(klassifikasjon => klassifikasjon.KlassifikasjonId));


            modelBuilder.Entity<KodeverkKode>(
                e => e.HasKey(kodeverkKode => kodeverkKode.KodeverkKodeId));

            modelBuilder.Entity<KodeverkKode>().HasData(GetSeedDateFromFile());



        }

        private KodeverkKode[] GetSeedDateFromFile()
        {
            var kodeverkKodeListe = new List<KodeverkKode>();


            using (var stream = File.OpenText(".\\DataAksess\\SeedData\\KodeverkKoderSeedData.csv"))
            {
                //skip header
                var line = stream.ReadLine();
                int Id = 1;
                while ((line = stream.ReadLine()) != null)
                {
                    string[] splittet = line.Split(';');
                    kodeverkKodeListe.Add(new KodeverkKode() { KodeverkKodeId = Id++, OId = int.Parse(splittet[1]), Verdi = splittet[2], Navn = splittet[3] });
                }
            }

            return kodeverkKodeListe.ToArray();

        }
    }
}
