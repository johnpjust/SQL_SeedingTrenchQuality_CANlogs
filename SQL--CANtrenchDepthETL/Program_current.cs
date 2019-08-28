using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using System.Globalization;

using System.IO;

using System.IO.Compression;
using System.Diagnostics;

using System.Data;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Threading;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;

namespace Seeding2019ETL
{
    class Program
    {
        static void Main(string[] args)
        {

            //List<int> refList = new List<int>();
            //int refCount = 50;
            //int curRef = 0;
            //int runningCount = 0;
            ////var t = new can_raw_data_Seeding_2018();
            //for (refCount = 26; refCount <= 50; refCount++)
            //{
            //    curRef = refCount + 53303 - 1;
            //    refList.Add(curRef);
            //}

            Console.WriteLine("Starting Program at: {0}", DateTime.Now.ToString("h:mm:ss tt"));

            List<log_meta_data> refList;
            using (var context = new ISU_Internal_CANEntities())
            {
                context.Database.CommandTimeout = 0;

                refList = context.log_meta_data.Where(b => b.file_name.Contains("2019") & b.project_name == "Seeding").ToList();
            }

                Parallel.ForEach(refList, new ParallelOptions { MaxDegreeOfParallelism = 12 }, (a) =>
                {
                    using (var context = new ISU_Internal_CANEntities())
                    {
                        context.Database.CommandTimeout = 0;
                        Console.WriteLine("active ref = {0}", a);
                    var startTime = DateTime.Now.TimeOfDay;


                    // the query below is what accesses server bound data. changing the ref_id range (VV below VV) will effectively allow parsing of a single field
                    // this script will fail if it is pointed at a ref_id that has already been processed and inserted into the seeding database
                    var query = context.can_raw_data_Seeding_2019.Where(b => b.ref_id == a.ref_id &&
                    (b.pgn == "EFF9" || b.pgn == "FFFE" || b.pgn == "EFC5" || b.pgn == "FEE8" || b.pgn == "FEF3" || b.pgn == "FEE6")
                    ).Select(item => new can_raw_data_seeding_query
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
                    }).ToList().OrderBy(b => b.ts_sec).ThenBy(b => b.ts_usec);

                    var connstr = System.Configuration.ConfigurationManager.ConnectionStrings["SeedingTrenchDepthTable"].ConnectionString;
                    string tblstr = "dbo.SeedMapping_forTrenchQuality";
                    using (SqlConnection SQLconn = new SqlConnection(connstr))
                    {
                        //db.Database.CommandTimeout = 180;
                        SQLconn.Open();

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(SQLconn))
                        //using (var db1 = new Model1())
                        {
                            bulkCopy.BulkCopyTimeout = 0;
                            bulkCopy.DestinationTableName = tblstr;
                            //db1.Database.CommandTimeout = 0;

                            SeedMapping_forTrenchQuality myclass = new SeedMapping_forTrenchQuality();
                            DTloader<SeedMapping_forTrenchQuality> dt = new DTloader<SeedMapping_forTrenchQuality>(myclass);

                            myclass.filename = a.file_name;
                            /****************** bulkcopy columnmappings **************************/
                            foreach (DataColumn dc in dt.dt.Columns)
                            {
                                if (!(dc.ColumnName == "rowID") && !(dc.ColumnName == "HF_FileManager"))
                                {
                                    bulkCopy.ColumnMappings.Add(dc.ColumnName, dc.ColumnName);
                                }
                            }
                            /***************************************************************************/

                            //SeedingHDmapping2019 myclass = new SeedingHDmapping2019(); //data to insert to table
                            //SeedingTrenchDepthTable tdtable = new SeedingTrenchDepthTable(); // table object

                            //List<SeedingHDmapping2019> myclass_list = new List<SeedingHDmapping2019>();

                            //Object lockMe = new Object(); // locks the insert group to make thread safe
                            //Object lockSave = new Object(); // locks the insert group to make thread safe

                            myclass.centerLon = 180;
                            myclass.centerLat = 180;

                            DateTime GPStime = new DateTime(1, 1, 1);
                            returntype localDateTime = new returntype();
                            localDateTime.locationName = "";
                            localDateTime.timeoffset = new TimeSpan(0, 0, 0);
                            bool breakflag = false;


                            int RUCoffset = 127; // int offset to get SA to equal 1-24 based on row for all RUC based messages
                            int TDoffset = 207; // int offset to get SA to equal 1-24 based on row for all A2CAN messages
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
                            string PGN_gpstime = "FEE6";
                            //string PGN_vac = "EFFA";

                            // values constant for all row units at each timestamp
                            double? heading = 0;
                            double? groundSpeed = 0;
                            double? frontLat = 0;
                            double? frontLon = 0;
                            double? backLat = 0;
                            double? backLon = 0;

                            // used for gps translation
                            int Llat = 1; // modifier used in GPS transformation
                            int Llon = -1; // modifier used in GPS transformation
                            int Rlat = -1; // modifier used in GPS transformation
                            int Rlon = 1; // modifier used in GPS transformation
                            double cosHead = 0; // modifier used in GPS transformation
                            double sinHead = 0; // modifier used in GPS transformation
                            double aspectRatio = 0; // modifier used in GPS transformation
                            double[] barEnd = new double[2]; // holds the offset from center in lat and long

                            //arrays to hold the value of each metric for all 24 row units
                            //set to allow using 1 to designate row unit 1 and 24 to designate row unit 24
                            //this means my arrays start at 1...... fight me

                            // arrays that store the parameter of interest for each row
                            double?[] rideQuality = new double?[25];
                            double?[] groundContact = new double?[25];
                            double?[] marginDownforce = new double?[25];
                            double?[] appliedDownforce = new double?[25];
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
                            double?[] IRV = new double?[25];
                            byte sa_try = 0;
                            byte RUC_try = 0;

                            double?[,] aggregated = new double?[3000, 22]; //holds temp copy of database until pushed into the local portion of the database framework
                                                                           //int aggregatedCount = 0;
                                                                           //int recordCount = 0;

                            //this is the loop through each ref_ID
                            foreach (var qq in query)  //parallelized for loop running on all threads
                            {
                                if (Byte.TryParse(qq.sa, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sa_try)) // attempts to parse the SA to check if the SA is in the trench depth range
                                {
                                    thisRow = sa_try - TDoffset; // active row unit this is used for array indexing and identification of row in the output1
                                    if (thisRow < 1 || thisRow > 24)
                                    {
                                        thisRow = 0;
                                    }
                                }

                                if (Byte.TryParse(qq.sa, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out RUC_try)) // attempts to parse the SA to check if the SA is in the RUC range
                                {
                                    rucRow = RUC_try - RUCoffset; // active row unit this ios used for array indexing and identification of row in the output1
                                    if (rucRow < 1 || rucRow > 24)
                                    {
                                        rucRow = 0;
                                    }
                                }

                                if (qq.pgn == PGN_trenchDepth)
                                {
                                    if (qq.d0 == 0)
                                    {
                                        trenchDepth[thisRow] = (qq.d5 + qq.d6 * 256.0) * 0.1 - 50.0;  // trench depth in mm
                                        deltaDepth[thisRow] = (qq.d7 - 125); // wheel walk in mm
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

                                    if (qq.d0 == 175)
                                    {
                                        if (rucRow >= 13)
                                        {
                                            IRV[rucRow] = ((((qq.d3 + qq.d4 * 256) * 0.125 / 1000) - 0.5) / 4) * 27.7;
                                            if (IRV[rucRow] <= 0)
                                            {
                                                IRV[rucRow] = 0;
                                            }
                                        }
                                    }
                                }

                                if (qq.pgn == PGN_gpstime)
                                {
                                    //GPSdatetime
                                    //[Column(TypeName = "datetime2")]
                                    //public DateTime GPStime { get; set; }
                                    int GPSyear = qq.d5.GetValueOrDefault() + 1985;
                                    int GPSmonth = qq.d3.GetValueOrDefault();
                                    double GPSday = Math.Ceiling(qq.d4.GetValueOrDefault() * 0.25);
                                    int GPShour = qq.d2.GetValueOrDefault();
                                    int GPSminute = qq.d1.GetValueOrDefault();
                                    double GPSsec = Math.Floor(qq.d0.GetValueOrDefault() * 0.25);

                                    //if (gotdatetime)
                                    //{
                                    //    GPSyear = fromDateValue.Year;
                                    //    GPSmonth = fromDateValue.Month;
                                    //    GPSday = fromDateValue.Day;
                                    //}


                                    //sql query
                                    //                                            select*
                                    //FROM[Moisture].[dbo].[HF_FileManager]
                                    //  where GPSdatetime = '0001-01-01 00:00:00.0000000 +00:00' and FileName like '%2019%' and fileLenMB > 2 and SensorSN > 3

                                    if (GPSminute > 0 && GPSminute < 60 && GPShour > 0 && GPShour < 24 && GPSday > 0 && GPSday < 32
                                      && GPSsec >= 0 && GPSsec < 60 && GPSmonth > 0 && GPSmonth < 13 && GPSyear > 2015)
                                    {

                                        GPStime = new DateTime(GPSyear, GPSmonth, (int)GPSday, GPShour, GPSminute, (int)GPSsec);

                                        //adjust UTC to local time based on GPS location lat/lon
                                        if (localDateTime.timeoffset != new TimeSpan(0, 0, 0))
                                        {
                                            myclass.GPStime = GPStime + localDateTime.timeoffset;
                                            //location = localDateTime.locationName;
                                            //if (sensorSN > 0 && baleID > 0) wroteFileManager = true;

                                        }
                                        else if (Math.Abs(myclass.centerLat.GetValueOrDefault()) < 90 && Math.Abs(myclass.centerLon.GetValueOrDefault()) < 180 && localDateTime.timeoffset == new TimeSpan(0, 0, 0))
                                        {
                                            int delay = 0;
                                            localDateTime = getlocinfo.GetLocalDateTime(myclass.centerLat.GetValueOrDefault(), myclass.centerLon.GetValueOrDefault(), GPStime);
                                            while (localDateTime.locationResponse != "OK" || localDateTime.timestampResponse != "OK")
                                            {

                                                if (delay > 2000 * 5)
                                                {
                                                    Console.WriteLine("uh oh....getlocinfo prob on file, {0}\t{1}\t{2}\t{3}", a,
                                                        DateTime.Now, localDateTime.locationResponse, localDateTime.timestampResponse);

                                                    //using (StreamWriter sw = new StreamWriter(exceptionfile, true))
                                                    //{
                                                    //    sw.WriteLine("getlocinfo prob on file, {0}, {1}, {2}, {3}", GMMclass.logname, DateTime.Now,
                                                    //        localDateTime.locationResponse, localDateTime.timestampResponse);
                                                    //}
                                                    breakflag = true;
                                                    break;
                                                }

                                                delay += 2000;
                                                Thread.Sleep(delay);
                                                localDateTime = getlocinfo.GetLocalDateTime(myclass.centerLat.GetValueOrDefault(), myclass.centerLon.GetValueOrDefault(), GPStime);

                                            }

                                            if (breakflag) break;
                                            //GPStime = GPStime + localDateTime.timeoffset;
                                            //location = localDateTime.locationName;

                                        }

                                    }
                                }

                                if (qq.pgn == PGN_meter) // RUC seeding
                                {
                                    if (qq.d0 == 244 && qq.d1 == 44) // multiplexed for meter motor speed
                                    {
                                        meterSpeed[rucRow] = ((qq.d2 + qq.d3 * 256) * 0.25 - 8000); // meter speed
                                    }

                                    if (qq.d0 == 244 && qq.d1 == 47) // multiplexed for meter motor speed
                                    {
                                        if (qq.d4 != 255 && qq.d5 != 255)
                                        {
                                            instSkip[rucRow] = (qq.d4 + (qq.d5 & 15) * 256) * 0.1; // instant skips 
                                        }

                                        if (qq.d5 != 255 && qq.d6 != 255)
                                        {
                                            instDouble[rucRow] = (qq.d6 * 16 + (qq.d5 & 240) / 16) * 0.1; // instant doubles
                                        }
                                    }

                                    if (qq.d0 == 244 && qq.d1 == 71 && qq.d6 != 255) // multiplexed for seed stats
                                    {
                                        seedPop[rucRow] = (qq.d2 + qq.d3 * 256) * 10; // instant population
                                        seedCount[rucRow] = (qq.d4 + qq.d5 * 256); // instant seed count
                                                                                   // unreliable, switching over to the percentage vals 
                                                                                   //instSkip[rucRow] = qq.d6 * 0.4; // instant skips 
                                                                                   //instDouble[rucRow] = qq.d7 * 0.4; // instant doubles
                                    }

                                    if (qq.d0 == 244 && qq.d1 == 87)
                                    {
                                        appliedDownforce[rucRow] = (qq.d2 + qq.d3 * 256) - 32000;
                                    }

                                    if (qq.d0 == 244 && qq.d1 == 82)
                                    {
                                        if (rucRow <= 12)
                                        {
                                            IRV[rucRow] = ((qq.d4 + qq.d5 * 256) * 0.0078125 - 250) * 4.01865;
                                        }
                                        if (meterSpeed[rucRow] > 50 && groundSpeed > 0.5)
                                        {
                                            myclass.ts_sec = qq.ts_sec;
                                            myclass.ts_usec = qq.ts_usec;
                                            myclass.ref_id = qq.ref_id;
                                            myclass.rowUnit = rucRow;
                                            myclass.rideQuality = rideQuality[rucRow];
                                            myclass.contactTime = groundContact[rucRow];
                                            myclass.marginDownforce = marginDownforce[rucRow];
                                            myclass.appliedDownforce = marginDownforce[rucRow];
                                            myclass.trenchDepth = trenchDepth[rucRow];
                                            myclass.groundSpeed = groundSpeed;
                                            myclass.centerLat = backLat;
                                            myclass.centerLon = backLon;
                                            myclass.rowLat = rowLat[rucRow];
                                            myclass.rowLon = rowLon[rucRow];
                                            myclass.heading = heading;
                                            myclass.deltaDepth = deltaDepth[rucRow];
                                            myclass.meterSpeed = meterSpeed[rucRow];
                                            myclass.instDouble = instDouble[rucRow];
                                            myclass.instSkip = instSkip[rucRow];
                                            myclass.seedCount = seedCount[rucRow];
                                            myclass.seedPop = seedPop[rucRow];
                                            myclass.vac = IRV[rucRow];

                                            //aggregatedCount++;
                                            dt.loadTable<SeedMapping_forTrenchQuality>(myclass);
                                        }

                                        //if (aggregatedCount >= 1500)
                                        //{
                                        //    //for (int ii = 0; ii <= aggregatedCount - 1; ii++)
                                        //    //{
                                        //    //    myclass.ts_sec = aggregated[ii, 0];
                                        //    //    myclass.ts_usec = aggregated[ii, 1];
                                        //    //    myclass.ref_id = aggregated[ii, 2];
                                        //    //    myclass.rowUnit = aggregated[ii, 3];
                                        //    //    myclass.rideQuality = aggregated[ii, 4];
                                        //    //    myclass.contactTime = aggregated[ii, 5];
                                        //    //    myclass.marginDownforce = aggregated[ii, 6];
                                        //    //    myclass.appliedDownforce = aggregated[ii, 7];
                                        //    //    myclass.trenchDepth = aggregated[ii, 8];
                                        //    //    myclass.groundSpeed = aggregated[ii, 9];
                                        //    //    myclass.centerLat = aggregated[ii, 10];
                                        //    //    myclass.centerLon = aggregated[ii, 11];
                                        //    //    myclass.rowLat = aggregated[ii, 12];
                                        //    //    myclass.rowLon = aggregated[ii, 13];
                                        //    //    myclass.heading = aggregated[ii, 14];
                                        //    //    myclass.deltaDepth = aggregated[ii, 15];
                                        //    //    myclass.meterSpeed = aggregated[ii, 16];
                                        //    //    myclass.instDouble = aggregated[ii, 17];
                                        //    //    myclass.instSkip = aggregated[ii, 18];
                                        //    //    myclass.seedCount = aggregated[ii, 19];
                                        //    //    myclass.seedPop = aggregated[ii, 20];
                                        //    //    myclass.vac = aggregated[ii, 21];

                                        //    //    myclass_list.Add(myclass);
                                        //    //    recordCount++;
                                        //    //    myclass = new SeedingHDmapping2019(); // clear myclass for new values
                                        //    //}



                                        //    aggregatedCount = 0;

                                        //    if (recordCount >= 10000)
                                        //    {
                                        //        lock (lockMe) // a lock to limit the insert and save operations to a single thread at any given point in time
                                        //        {

                                        //            var myclass_list_filt = myclass_list.GroupBy(x => new { x.ref_id, x.ts_sec, x.ts_usec, x.rowUnit }).Select(y => y.First());



                                        //            tdtable.SeedingHDmapping2019.AddRange(myclass_list_filt);
                                        //            tdtable.SaveChanges(); // save table
                                        //            tdtable.Dispose(); // destroys local table to reduce memory overhead and speed up process
                                        //            tdtable = new SeedingTrenchDepthTable(); // recreate empty local table
                                        //            tdtable.Configuration.AutoDetectChangesEnabled = false; // dramatically speeds up insert and save
                                        //            tdtable.Configuration.ValidateOnSaveEnabled = false; // dramatically speeds up insert and save
                                        //            myclass_list.Clear();
                                        //            recordCount = 0;
                                        //            runningCount++;

                                        //            Console.WriteLine("time: {2} REF_ID: {0}  --{1} records Inserted", qq.ref_id, runningCount * 10000, Convert.ToString(DateTime.Now.TimeOfDay.Subtract(startTime)));

                                        //        }
                                        //    }
                                        //}

                                    }
                                }

                                if (qq.pgn == PGN_motion)
                                {
                                    groundSpeed = ((qq.d2 + qq.d3 * 256) * 0.00390625 * 0.621371); //  ground speed
                                    heading = (qq.d1 * 256 + qq.d0) * 0.0078125; // heading
                                }

                                if (qq.pgn == PGN_position)
                                {
                                    if (qq.sa == "1C") // this is the main starfire reciever sa = 1C
                                    {
                                        frontLat = (((qq.d3 * 16777216.0 + qq.d2 * 65536.0 + qq.d1 * 256.0 + qq.d0 * 1.0) * 1 / 10000000) - 210); // lat of tractor starfire
                                        frontLon = (((qq.d7 * 16777216 + qq.d6 * 65536 + qq.d5 * 256 + qq.d4 * 1.0) * 1 / 10000000) - 210); // lon of tractor starfire
                                    }
                                    if (qq.sa == "9C") // this is the planter starfire reciever sa = 9C
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
                                        cosHead = Math.Abs(Math.Cos(0.0174533 * (90 + heading.Value))); // cosine of the toolar vector
                                        sinHead = Math.Abs(Math.Sin(0.0174533 * (90 + heading.Value))); // sine of the toolbar vector
                                        aspectRatio = Math.Abs(1 / Math.Cos(0.0174533 * backLat.Value)); // aspect ratio from the elongation of a single lat/lon grid
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

                                /***************************************************************************/

                                if (dt.dt.Rows.Count > 10000)
                                {
                                    writetotable(bulkCopy, ref dt.dt);
                                }
                            }

                            if (dt.dt.Rows.Count != 0)
                            {
                                writetotable(bulkCopy, ref dt.dt);
                            }

                            //lock (lockMe) // a lock to limit the insert and save operations to a single thread at any given point in time
                            //{
                            //    for (int ii = 0; ii <= aggregatedCount - 1; ii++)
                            //    {
                            //        myclass.ts_sec = aggregated[ii, 0];
                            //        myclass.ts_usec = aggregated[ii, 1];
                            //        myclass.ref_id = aggregated[ii, 2];
                            //        myclass.rowUnit = aggregated[ii, 3];
                            //        myclass.rideQuality = aggregated[ii, 4];
                            //        myclass.contactTime = aggregated[ii, 5];
                            //        myclass.marginDownforce = aggregated[ii, 6];
                            //        myclass.appliedDownforce = aggregated[ii, 7];
                            //        myclass.trenchDepth = aggregated[ii, 8];
                            //        myclass.groundSpeed = aggregated[ii, 9];
                            //        myclass.centerLat = aggregated[ii, 10];
                            //        myclass.centerLon = aggregated[ii, 11];
                            //        myclass.rowLat = aggregated[ii, 12];
                            //        myclass.rowLon = aggregated[ii, 13];
                            //        myclass.heading = aggregated[ii, 14];
                            //        myclass.deltaDepth = aggregated[ii, 15];
                            //        myclass.meterSpeed = aggregated[ii, 16];
                            //        myclass.instDouble = aggregated[ii, 17];
                            //        myclass.instSkip = aggregated[ii, 18];
                            //        myclass.seedCount = aggregated[ii, 19];
                            //        myclass.seedPop = aggregated[ii, 20];
                            //        myclass.vac = aggregated[ii, 21];

                            //        myclass_list.Add(myclass);
                            //        recordCount++;
                            //        myclass = new SeedingHDmapping2019(); // clear myclass for new values
                            //    }

                            //    var myclass_list_filt = myclass_list.GroupBy(x => new { x.ref_id, x.ts_sec, x.ts_usec, x.rowUnit }).Select(y => y.First());

                            //    tdtable.SeedingHDmapping2019.AddRange(myclass_list_filt);
                            //    tdtable.SaveChanges(); // save table
                            //    tdtable.Dispose(); // destroys local table to reduce memory overhead and speed up process
                            //    tdtable = new SeedingTrenchDepthTable(); // recreate empty local table
                            //    tdtable.Configuration.AutoDetectChangesEnabled = false; // dramatically speeds up insert and save
                            //    tdtable.Configuration.ValidateOnSaveEnabled = false; // dramatically speeds up insert and save
                            //    myclass_list.Clear();
                            //    recordCount = 0;
                            //}

                            //context.Dispose();
                            Console.WriteLine("end of ref_id {0} elapsed time: {1}", myclass.ref_id, Convert.ToString(DateTime.Now.TimeOfDay.Subtract(startTime)));
                            //aggregatedCount = 0;
                            //Array.Clear(aggregated, 0, aggregated.Length);

                        }
                    }

                }
                });
            
        }

        public class DTloader<T>
        {
            PropertyDescriptorCollection props;
            public DataTable dt = new DataTable();
            public DTloader(T data)
            {
                props = TypeDescriptor.GetProperties(typeof(T));
                for (int i = 0; i < props.Count; i++)
                {
                    PropertyDescriptor prop = props[i];
                    //dt.Columns.Add(prop.Name, prop.PropertyType);
                    dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }

            public void loadTable<TZ>(TZ data)
            {
                object[] values = new object[props.Count];

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(data);
                }
                dt.Rows.Add(values);

            }
        }

        public static void writetotable(SqlBulkCopy bulkCopy, ref DataTable dt)
        {
            try
            {
                bulkCopy.WriteToServer(dt);

            }
            catch (Exception ex)
            {
                Console.WriteLine("uh oh...." + ex.Message);
                //using (StreamWriter sw = new StreamWriter(exceptionfile, true))
                //{
                //    sw.WriteLine(ex.Message.ToString() + "," + DateTime.Now);
                //    Console.WriteLine();
                //}
            }

            dt.Rows.Clear();
        }

    }

#pragma warning disable IDE1006 // Naming Styles

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

#pragma warning disable IDE1006 // Naming Styles
}
