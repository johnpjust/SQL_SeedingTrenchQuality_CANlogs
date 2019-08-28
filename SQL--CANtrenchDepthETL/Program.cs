using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using System.Globalization;

namespace Seeding2019ETL
{
    class Program
    {
        static void Main(string[] args)
        {

            ////////////////// scan seeding table for list of existing ref_ids


            ///////////////////////////////////



            using (var context = new ISU_Internal_CANEntities())
            {
                int[] refList;
                //var t = new can_raw_data_Seeding_2019();
                for(refCount = 1;refCount<=50;refCount++)
                {
                    refList.Add(refCount + 53302);
                }


                //foreach(int existingRef in refList)
                //{
                //    var returnedRefs = context.seedingHDmapping2019.Where(c => c.ref_id == existingRef).Select(item => new existing_data_query
                //    {
                //        ref_id = item.existing_id,
                //    }).ToList();

                //}



                Parallel.ForEach(refList, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (a) =>
                {
                    Console.WriteLine("active ref = {0}", a);

                        //this is the query
                        //var query = context.can_raw_data_Seeding_2019.Where(b => b.ref_id > 41865 && b.ref_id < 41865+2 ).OrderBy(b=> b.ts_sec).ThenBy(b=>b.ts_usec).GroupBy(b => b.ref_id);
                    var query = context.can_raw_data_Seeding_2019.Where(b => b.ref_id == a).Select(item => new can_raw_data_seeding_query // && b.ref_id <= 41884
                    {
                        ts_sec = item.ts_sec,
                        ts_usec = item.ts_usec,
                        channel = item.channel,
                        d0 = item.d0,
                        d1 = item.d1,
                        d2 = item.d2,
                        d3 = item.d3,
                        d4 = item.d4,
                        d5 = item.d5,
                        d6 = item.d6,
                        d7 = item.d7,
                        pgn = item.pgn,
                        ref_id = item.ref_id,
                        sa = item.sa
                        // }).ToList().GroupBy(b => b.ref_id).OrderBy(b => b.Key);//.Select(s => s.Key);// || b.ref_id == 41866 || b.ref_id == 41867
                    }).ToList().OrderBy(b => b.ts_sec).ThenBy(b => b.ts_usec);//.Select(s => s.Key);// || b.ref_id == 41866 || b.ref_id == 41867

                    seedingTrenchDepth myclass = new seedingTrenchDepth(); //data to insert to table
                    SeedingTrenchDepthTable tdtable = new SeedingTrenchDepthTable(); // table object
                    Object lockMe = new Object(); // locks the insert group to make thread safe
                    Object lockSave = new Object(); // locks the insert group to make thread safe

                    int commitCount = 0;

                    //this is the loop through each ref_ID
                    foreach (var q in query)
                    {
                        int RUCoffset = 127;
                        int TDoffset = 207;
                        int thisRow = 0;
                        int rucRow = 0;
                        int i = 0;
                        double[] gpsOffset = new double[25] { 0, 8.763, 8.001, 7.239, 6.477, 5.715, 4.959, 4.191, 3.429, 2.667, 1.905, 1.143, 0.381, 0.381, 1.143, 1.905, 2.667, 3.429, 4.191, 4.959, 5.715, 6.477, 7.239, 8.001, 8.763 };

                        // string values of PGNs
                        string PGN_trenchDepth = "EFF9";
                        string PGN_ride = "FFFE";
                        string PGN_meter = "EFC5";
                        string PGN_motion = "FEE8";
                        string PGN_position = "FEF3";

                        // values constant for all row units at each timestamp
                        double? heading = 0;
                        double? groundSpeed = 0;
                        double? frontLat = 0;
                        double? frontLon = 0;
                        double? backLat = 0;
                        double? backLon = 0;

                        // used for gps translation
                        int Llat = 1;
                        int Llon = -1;
                        int Rlat = -1;
                        int Rlon = 1;
                        double cosHead = 0;
                        double sinHead = 0;
                        double aspectRatio = 0;
                        double[] barEnd = new double[2];

                        //arrays to hold the value of each metric for all 24 row units
                        //set to allow using 1 to designate row unit 1 and 24 to designate row unit 24
                        //this means my arrays start at 1...... fight me

                        // unique values for each row unit
                        double?[] rideQuality = new double?[25];
                        double?[] groundContact = new double?[25];
                        double?[] marginDownforce = new double?[25];
                        double?[] trenchDepth = new double?[25];
                        double?[] deltaDepth = new double?[25];
                        double?[] refID = new double?[25];
                        double?[] meterSpeed = new double?[25];
                        double?[] instDouble = new double?[25];
                        double?[] instSkip = new double?[25];
                        double?[] seedCount = new double?[25];
                        double?[] seedPop = new double?[25];
                        double?[] rowLat = new double?[25];
                        double?[] rowLon = new double?[25];
                        byte sa_try = 0;
                        byte RUC_try = 0;
                        int nullCheck = 0;


                        //this is the loop through each line/row
                        foreach (var qq in q)
                        {
                            if (Byte.TryParse(qq.sa, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sa_try))
                            {
                                thisRow = sa_try - TDoffset; // active row unit this ios used for array indexing and identification of row in the output1
                                if (thisRow < 1 || thisRow > 24)
                                {
                                    thisRow = 0;
                                }
                            }

                            if (Byte.TryParse(qq.sa, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out RUC_try))
                            {
                                rucRow = RUC_try - RUCoffset; // active row unit this ios used for array indexing and identification of row in the output1
                                if (rucRow < 1 || rucRow > 24)
                                {
                                    rucRow = 0;
                                }
                            }

                            if (qq.pgn == PGN_trenchDepth)//& Convert.ToInt32(qq.pgn, 16) == PGN_trenchDepth)
                            {
                                if (qq.d0 == 0)// && (qq.channel == 3 || qq.channel == 4)) //correct cmd byte and on a subnet
                                {
                                    //Console.WriteLine(qq.pgn);

                                    trenchDepth[thisRow] = (qq.d5 + qq.d6 * 256.0) * 0.1 - 50.0;  // trench depth in mm
                                    deltaDepth[thisRow] = (qq.d7 - 125); // wheel walk in mm

                                    // check to see if lat and long are populated
                                    // should burst insert to table here
                                    myclass.ts_sec = qq.ts_sec;
                                    myclass.ts_usec = qq.ts_usec;
                                    myclass.ref_id = qq.ref_id;
                                    myclass.rowUnit = thisRow;
                                    myclass.rideQuality = rideQuality[thisRow];
                                    myclass.contactTime = groundContact[thisRow];
                                    myclass.marginDownforce = marginDownforce[thisRow];
                                    myclass.trenchDepth = trenchDepth[thisRow];
                                    myclass.groundSpeed = groundSpeed;
                                    myclass.centerLat = backLat;
                                    myclass.centerLon = backLon;
                                    myclass.rowLat = rowLat[thisRow];
                                    myclass.rowLon = rowLon[thisRow];
                                    myclass.heading = heading;
                                    myclass.deltaDepth = deltaDepth[thisRow];
                                    myclass.meterSpeed = meterSpeed[thisRow];
                                    myclass.instDouble = instDouble[thisRow];
                                    myclass.instSkip = instSkip[thisRow];
                                    myclass.seedCount = seedCount[thisRow];
                                    myclass.seedPop = seedPop[thisRow];

                                    lock (lockMe)
                                    {
                                        commitCount++;
                                        if (myclass.centerLat != null && myclass.centerLon != null && myclass.contactTime != null && myclass.deltaDepth != null && myclass.groundSpeed != null && myclass.heading != null
                                            && myclass.instDouble != null && myclass.instSkip != null && myclass.marginDownforce != null && myclass.meterSpeed != null && myclass.ref_id != null && myclass.rideQuality != null
                                            && myclass.rowLat != null && myclass.rowLon != null && myclass.rowUnit != null && myclass.seedCount != null && myclass.seedPop != null && myclass.trenchDepth != null
                                            && myclass.ts_sec != null && myclass.ts_usec != null)
                                        {
                                            nullCheck = 1;
                                        }
                                        else
                                        {
                                            nullCheck = 0;
                                        }
                                        if ((myclass.meterSpeed > 0 && myclass.groundSpeed > 0.5) && (nullCheck != 0))
                                        {
                                            try
                                            {
                                                tdtable.seedingTrenchDepth.Add(myclass);
                                                tdtable.SaveChanges();
                                                tdtable.Dispose();
                                                tdtable = new SeedingTrenchDepthTable(); // table object
                                                tdtable.Configuration.AutoDetectChangesEnabled = false;
                                                tdtable.Configuration.ValidateOnSaveEnabled = false;
                                                //Console.WriteLine("{0}, {1}, {2}, {3}", myclass.ref_id, myclass.rowUnit, myclass.ts_sec, myclass.ts_usec);
                                                myclass = new seedingTrenchDepth();
                                            }
                                            catch
                                            {
                                                Console.WriteLine("Add Failed");
                                            }
                                            commitCount = 0;
                                        }
                                        //else
                                        //{
                                        //    tdtable.seedingTrenchDepth.Add(myclass);
                                        //}
                                    }
                                }
                            }

                            if (qq.pgn == PGN_ride) // RUC ride
                            {
                                if (qq.d0 == 174)
                                {
                                    rideQuality[rucRow] = qq.d2 * 0.4; // ride quality
                                    groundContact[rucRow] = qq.d3 * 0.4; // contact time
                                    marginDownforce[rucRow] = (qq.d4 + qq.d5 * 256) * 5; // downforce
                                }
                            }

                            if (qq.pgn == PGN_meter) // RUC seeding
                            {
                                //Console.WriteLine(qq.pgn);
                                if (qq.d0 == 244 && qq.d1 == 44) // multiplexed for meter motor speed
                                {
                                    meterSpeed[rucRow] = ((qq.d2 + qq.d3 * 256) * 0.25 - 8000); // meter speed
                                }

                                if (qq.d0 == 244 && qq.d1 == 71 && qq.d6 != 255) // multiplexed for seed stats
                                {
                                    seedPop[rucRow] = (qq.d2 + qq.d3 * 256) * 10; // instant population
                                    seedCount[rucRow] = (qq.d4 + qq.d5 * 256); // instant seed count
                                    instSkip[rucRow] = qq.d6 * 0.4; // instant skips 
                                    instDouble[rucRow] = qq.d7 * 0.4; // instant doubles
                                }

                            }

                            if (qq.pgn == PGN_motion)
                            {
                                //Console.WriteLine(qq.pgn);

                                groundSpeed = ((qq.d2 + qq.d3 * 256) * 0.00390625 * 0.621371); //  ground speed
                                heading = (qq.d1 * 256 + qq.d0) * 0.0078125; // heading
                            }

                            if (qq.pgn == PGN_position)
                            {
                                //Console.WriteLine(qq.pgn);

                                if (qq.sa == "1C") // this is the main starfire reciever sa = 1C
                                {
                                    frontLat = (((qq.d3 * 16777216.0 + qq.d2 * 65536.0 + qq.d1 * 256.0 + qq.d0 * 1.0) * 1 / 10000000) - 210); // lat of tractor starfire
                                    frontLon = (((qq.d7 * 16777216 + qq.d6 * 65536 + qq.d5 * 256 + qq.d4 * 1.0) * 1 / 10000000) - 210); // lon of tractor starfire
                                }
                                if (qq.sa == "9C") // this is the main starfire reciever sa = 9C
                                {
                                    backLat = (((qq.d3 * 16777216.0 + qq.d2 * 65536.0 + qq.d1 * 256.0 + qq.d0 * 1.0) * 1 / 10000000) - 210); // lat of implement starfire
                                    backLon = (((qq.d7 * 16777216 + qq.d6 * 65536 + qq.d5 * 256 + qq.d4 * 1.0) * 1 / 10000000) - 210); // lon of implement starfire
                                }
                                if ((heading < 90) && (heading > 0)) // northeast
                                {
                                    Llat = 1;
                                    Llon = -1;
                                    Rlat = -1;
                                    Rlon = 1;
                                }
                                if ((heading < 180) && (heading > 90)) // southeast
                                {
                                    Llat = 1;
                                    Llon = 1;
                                    Rlat = -1;
                                    Rlon = -1;
                                }
                                if ((heading < 270) && (heading > 180)) // southwest
                                {
                                    Llat = -1;
                                    Llon = 1;
                                    Rlat = 1;
                                    Rlon = -1;
                                }
                                if ((heading < 360) && (heading > 270)) // northwest
                                {
                                    Llat = -1;
                                    Llon = -1;
                                    Rlat = 1;
                                    Rlon = 1;
                                }
                                if (heading == 0 || heading == 360) // north
                                {
                                    Llat = 1;
                                    Llon = -1;
                                    Rlat = 1;
                                    Rlon = 1;
                                }
                                if (heading == 90) // east
                                {
                                    Llat = 1;
                                    Llon = 1;
                                    Rlat = -1;
                                    Rlon = 1;
                                }
                                if (heading == 180) // south
                                {
                                    Llat = 1;
                                    Llon = 1;
                                    Rlat = 1;
                                    Rlon = -1;
                                }
                                if (heading == 270) // west
                                {
                                    Llat = -1;
                                    Llon = 1;
                                    Rlat = 1;
                                    Rlon = 1;
                                }
                                for (i = 1; i <= 24; i++) //loops through all 24 rows calculating position of each
                                {
                                    cosHead = Math.Abs(Math.Cos(0.0174533 * (90 + heading.Value)));
                                    sinHead = Math.Abs(Math.Sin(0.0174533 * (90 + heading.Value)));
                                    aspectRatio = Math.Abs(1 / Math.Cos(0.0174533 * backLat.Value));
                                    if (i >= 13 && i <= 24) //row units on the right side of the machine are transposed
                                    {
                                        barEnd[0] = Math.Abs(gpsOffset[i] * cosHead) * (10 / 1.1) * (10E-7);
                                        barEnd[1] = Math.Abs(gpsOffset[i] * sinHead) * (10 / 1.1) * (10E-7);
                                        rowLat[i] = Math.Round(backLat.Value + Rlat * Math.Abs(barEnd[0]), 7); //lat 0f row unit
                                        rowLon[i] = Math.Round(backLon.Value + Rlon * Math.Abs(aspectRatio * barEnd[1]), 7); // lon of row unit
                                    }
                                    if (i >= 1 && i <= 12) //row units on the left side of the machine are transposed
                                    {
                                        barEnd[0] = Math.Abs(gpsOffset[i] * cosHead) * (10 / 1.1) * (10E-7);
                                        barEnd[1] = Math.Abs(gpsOffset[i] * sinHead) * (10 / 1.1) * (10E-7);
                                        rowLat[i] = Math.Round(backLat.Value + Llat * Math.Abs((barEnd[0])), 7);
                                        rowLon[i] = Math.Round(backLon.Value + Llon * Math.Abs(aspectRatio * (barEnd[1])), 7);
                                    }
                                }
                            }
                            //tdtable.seedingTrenchDepth.Add(myclass);
                        }

                        Console.WriteLine("end of ref_id {0}", myclass.ref_id);

                        //lock (lockSave)
                       // {
                       //     try
                       //     {
                       //         tdtable.SaveChanges();
                       //         tdtable.Dispose();
                       //         tdtable = new SeedingTrenchDepthTable(); // table object
                       //         tdtable.Configuration.AutoDetectChangesEnabled = false;
                       //         tdtable.Configuration.ValidateOnSaveEnabled = false;
                       //     }
                        //    catch
                        //    {
                        //        Console.WriteLine("Save Failed");
                        //    }
                       // }
                        ///write to seeding database
                        //myclass.ID = 1;
                    }
                });
            }
        }
    }

    public class can_raw_data_seeding_query
    {
        public int? ts_sec { get; set; }
        public int? ts_usec { get; set; }
        public Nullable<byte> channel { get; set; }
        public string mid { get; set; }
        public string pgn { get; set; }
        public string sa { get; set; }
        public Nullable<byte> dlc { get; set; }
        public Nullable<byte> d0 { get; set; }
        public Nullable<byte> d1 { get; set; }
        public Nullable<byte> d2 { get; set; }
        public Nullable<byte> d3 { get; set; }
        public Nullable<byte> d4 { get; set; }
        public Nullable<byte> d5 { get; set; }
        public Nullable<byte> d6 { get; set; }
        public Nullable<byte> d7 { get; set; }
        public int ref_id { get; set; }
    }

    public class existing_data_query
    {
        public int existing_id { get; set; }
    }
}
