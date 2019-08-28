using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seeding2019ETL
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;


    public partial class SeedMapping_forTrenchQuality
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long rowID { get; set; }

        //[Key]
        //[Column(Order = 2)]
        public double? ts_sec { get; set; }

        //[Key]
        //[Column(Order = 3)]
        public double? ts_usec { get; set; }

        //[Key]
        //[Column(Order = 1)]
        public double? ref_id { get; set; }

        //[Key]
        //[Column(Order = 4)]
        public double? rowUnit { get; set; }

        public double? rowLat { get; set; }
        public double? rowLon { get; set; }
        public double? centerLat { get; set; }
        public double? centerLon { get; set; }
        public double? heading { get; set; }
        public double? groundSpeed { get; set; }
        public double? rideQuality { get; set; }
        public double? contactTime { get; set; }
        public double? marginDownforce { get; set; }
        public double? appliedDownforce { get; set; }
        public double? trenchDepth { get; set; }
        public double? deltaDepth { get; set; }
        public double? meterSpeed { get; set; }
        public double? instDouble { get; set; }
        public double? instSkip { get; set; }
        public double? seedCount { get; set; }
        public double? seedPop { get; set; }
        public double? vac { get; set; }

        public DateTime? GPStime { get; set; }

        [StringLength(200)]
        public string filename { get; set; }

    }
}

//add-migration 'initial'
//update-database


//namespace SQL__CANtrenchDepthETL.Migrations
//{
//    using System;
//    using System.Data.Entity;
//    using System.Data.Entity.Migrations;
//    using System.Linq;

//    internal sealed class Configuration : DbMigrationsConfiguration<SQL__CANtrenchDepthETL.SeedingTrenchDepthTable>
//    {
//        public Configuration()
//        {
//            AutomaticMigrationsEnabled = false;
//        }

//        protected override void Seed(SQL__CANtrenchDepthETL.SeedingTrenchDepthTable context)
//        {
//            //  This method will be called after migrating to the latest version.

//            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
//            //  to avoid creating duplicate seed data.
//        }
//    }
//}
