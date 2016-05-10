﻿using System;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using NArctic;
using NumCIL;
using MongoDB.Driver;
using System.Diagnostics;
using System.Threading;
using NArctic.Randoms;

namespace NArctic.Tests
{
	public class MainClass
	{
		public static void TestDType(string str)
		{
			Console.WriteLine("Begin Parse<{0}>".Args(str));
			try {
				var dt = new DType (str);
				Console.WriteLine("Done Parse<{0}> ===> {1}".Args(str, dt.ToString()));
			}catch(InvalidOperationException e){
				Console.WriteLine("Failed Parse<{0}> ===> {1}".Args(str, e.Message));
			}
				
		}
		public static void TestDTypes()
		{
			//TestDType ("'f8'");
			//TestDType ("'i8'");
			//TestDType ("[('f1','f8')]");
			//TestDType ("[('f1','f8'),('f2','i8')]");
			//TestDType ("[('f1','f8'),('f2','i8')");
			//TestDType ("[('f1','f8'),('f2','i8'");
			//TestDType ("[('f1,'f8'),('f2','i8'");
			TestDType ("[('index', '<M8[ns]'), ('Open', '<f8'), ('Close', '<f8'), ('Adj Close', '<f8'), ('High', '<f8'), ('Low', '<f8'), ('Volume', '<i8')]");
		}

		public static void TestReadArctic(string dbname="arctic_bench", string host="localhost", string symbol="S1"){
			var driver = new MongoClient ("mongodb://"+host);
			var db = driver.GetDatabase (dbname);

			var arctic = new Arctic (db);
			Stopwatch sw = new Stopwatch ();
			sw.Start ();
			var df = arctic.ReadDataFrameAsync (symbol).Result;
			sw.Stop ();
			if (df != null) {
				Console.WriteLine (df);
				Console.WriteLine ("read {0} took {1}s = {2}/sec".Args (df.Rows.Count, sw.Elapsed.TotalSeconds, df.Rows.Count / sw.Elapsed.TotalSeconds));
			} else {
				Console.WriteLine ("Not found {0}".Args (symbol));
			}
		}

		public static DataFrame RandomWalk(int count, DateTime start, DateTime stop)
		{
            Console.WriteLine("RandomWalk generating {0}".Args(count));
			var df = new DataFrame { 
				Series.DateTimeRange(count, start, stop),
				Series.Random(count, new BoxMullerNormal()).Apply(v => (v*1e-5).CumSum().Exp())
			};
			Console.WriteLine (df);
			return df;
		}

		public static DataFrame SampleDataFrame(DateTime start=default(DateTime)) {
			start = start == default(DateTime) ? DateTime.Now : start;
			var df = new DataFrame();
			df.Columns.Add (new []{start,start.AddDays(1),start.AddDays(2),start.AddDays(3),start.AddDays(4)}, "index");
			//df.Columns.Add (new long[]{1, 2, 3, 4, 5}, "long");
			df.Columns.Add (new double[]{1, 2, 3, 4, 5}, "double");
			Console.WriteLine ("new dataframe:\n {0}".Args(df));
	
			var df2 = df.Clone ();
			df2 [0].AsDateTime64 += TimeSpan.FromDays (30).ToDateTime64 ();
			Console.WriteLine ("and append dataframe:\n{0}".Args (df2));
			return df;
		}
        //public const int SIZE = 24 * 60 * 60 * 365;
        public const int SIZE = 1000000;
        public const int CHUNKSIZE = 100000;
		public static DataFrame RandomDataFrame(DateTime start=default(DateTime), TimeSpan delta = default(TimeSpan), int count=SIZE) {
			start = start == default(DateTime) ? DateTime.Now : start;
            delta = delta == default(TimeSpan) ? TimeSpan.FromSeconds(1): delta;
			return RandomWalk (count, start, start+delta.Mul(count));
		}

		public static void TestWriteArctic(string dbname="arctic_net", string host="localhost", bool purge=true, string symbol="S1") {
			var driver = new MongoClient ("mongodb://"+host);
			if (purge)
				driver.DropDatabase (dbname);
			
			var db = driver.GetDatabase (dbname);

			var arctic = new Arctic (db);
			//var df = SampleDataFrame ();
			var df = RandomDataFrame();
			var df2 = SampleDataFrame (df[0].AsDateTime()[-1]);

			Stopwatch sw = new Stopwatch ();
			sw.Start ();
			var version = arctic.AppendDataFrameAsync (symbol, df, CHUNKSIZE).Result;
			var version2 = arctic.AppendDataFrameAsync (symbol, df2, CHUNKSIZE).Result;
			sw.Stop ();
			long rows = df.Rows.Count + df2.Rows.Count;
			Console.WriteLine ("write {0} took {1}s = {2}/sec -> ver:\n {3}".Args (rows, sw.Elapsed.TotalSeconds, rows/sw.Elapsed.TotalSeconds, version));
		}

		public static void Main (string[] args)
		{
//			TestDTypes ();
			TestWriteArctic("arctic_net");
			TestReadArctic("arctic_net");


			Console.WriteLine ("DONE");
		}
	}
}
