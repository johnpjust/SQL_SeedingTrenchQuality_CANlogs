namespace Seeding2019ETL.Migrations
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class SeedingTrenchDepthTable : DbContext
    {
        public SeedingTrenchDepthTable()
            : base("name=SeedingTrenchDepthTable")
        {
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public virtual DbSet<SeedMapping_forTrenchQuality> SeedMapping_forTrenchQuality { get; set; }
    }
}
