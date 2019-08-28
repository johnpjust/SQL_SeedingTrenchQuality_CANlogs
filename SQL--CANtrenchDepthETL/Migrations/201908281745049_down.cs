namespace Seeding2019ETL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class down : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SeedMapping_forTrenchQuality",
                c => new
                    {
                        rowID = c.Long(nullable: false, identity: true),
                        ts_sec = c.Double(),
                        ts_usec = c.Double(),
                        ref_id = c.Double(),
                        rowUnit = c.Double(),
                        rowLat = c.Double(),
                        rowLon = c.Double(),
                        centerLat = c.Double(),
                        centerLon = c.Double(),
                        heading = c.Double(),
                        groundSpeed = c.Double(),
                        rideQuality = c.Double(),
                        contactTime = c.Double(),
                        marginDownforce = c.Double(),
                        appliedDownforce = c.Double(),
                        trenchDepth = c.Double(),
                        deltaDepth = c.Double(),
                        meterSpeed = c.Double(),
                        instDouble = c.Double(),
                        instSkip = c.Double(),
                        seedCount = c.Double(),
                        seedPop = c.Double(),
                        vac = c.Double(),
                        GPStime = c.DateTime(),
                        filename = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.rowID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SeedMapping_forTrenchQuality");
        }
    }
}
