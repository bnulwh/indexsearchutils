/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Lucene.Net.Util;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Searchable = Lucene.Net.Search.Searchable;

namespace Lucene.Net
{
	
	class ThreadSafetyTest
	{
		private static readonly Analyzer ANALYZER = new SimpleAnalyzer();
		private static readonly System.Random RANDOM = new System.Random();
		private static Searcher SEARCHER;
		
		private static int ITERATIONS = 1;
		
		private static int Random(int i)
		{
			// for JDK 1.1 compatibility
			int r = RANDOM.Next();
			if (r < 0)
				r = - r;
			return r % i;
		}
		
		private class IndexerThread:SupportClass.ThreadClass
		{
			private void  InitBlock()
			{
				reopenInterval = 30 + Lucene.Net.ThreadSafetyTest.Random(60);
			}
			private int reopenInterval;
			internal IndexWriter writer;
			
			public IndexerThread(IndexWriter writer)
			{
				this.writer = writer;
			}
			
			override public void  Run()
			{
				try
				{
					bool useCompoundFiles = false;
					
					for (int i = 0; i < 1024 * Lucene.Net.ThreadSafetyTest.ITERATIONS; i++)
					{
						Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
						int n = Lucene.Net.ThreadSafetyTest.RANDOM.Next();
						d.Add(new Field("id", System.Convert.ToString(n), Field.Store.YES, Field.Index.UN_TOKENIZED));
						d.Add(new Field("contents", Lucene.Net.Util.English.IntToEnglish(n), Field.Store.NO, Field.Index.TOKENIZED));
						System.Console.Out.WriteLine("Adding " + n);
						
						// Switch between single and multiple file segments
						useCompoundFiles = (new System.Random()).NextDouble() < 0.5;
						writer.SetUseCompoundFile(useCompoundFiles);
						
						writer.AddDocument(d);
						
						if (i % reopenInterval == 0)
						{
							writer.Close();
							writer = new IndexWriter("index", Lucene.Net.ThreadSafetyTest.ANALYZER, false);
						}
					}
					
					writer.Close();
				}
				catch (System.Exception e)
				{
					System.Console.Out.WriteLine(e.ToString());
					System.Console.Error.WriteLine(e.StackTrace);
					System.Environment.Exit(0);
				}
			}
		}
		
		private class SearcherThread : SupportClass.ThreadClass
		{
			private void  InitBlock()
			{
				reopenInterval = 10 + Lucene.Net.ThreadSafetyTest.Random(20);
			}
			private IndexSearcher searcher;
			private int reopenInterval;
			
			public SearcherThread(bool useGlobal)
			{
				if (!useGlobal)
					this.searcher = new IndexSearcher("index");
			}
			
			override public void  Run()
			{
				try
				{
					for (int i = 0; i < 512 * Lucene.Net.ThreadSafetyTest.ITERATIONS; i++)
					{
						searchFor(Lucene.Net.ThreadSafetyTest.RANDOM.Next(), (searcher == null)?Lucene.Net.ThreadSafetyTest.SEARCHER:searcher);
						if (i % reopenInterval == 0)
						{
							if (searcher == null)
							{
								Lucene.Net.ThreadSafetyTest.SEARCHER = new IndexSearcher("index");
							}
							else
							{
								searcher.Close();
								searcher = new IndexSearcher("index");
							}
						}
					}
				}
				catch (System.Exception e)
				{
					System.Console.Out.WriteLine(e.ToString());
					System.Console.Error.WriteLine(e.StackTrace);
					System.Environment.Exit(0);
				}
			}
			
			private void  searchFor(int n, Searcher searcher)
			{
				System.Console.Out.WriteLine("Searching for " + n);
				Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser("contents", Lucene.Net.ThreadSafetyTest.ANALYZER);
				Lucene.Net.Search.Hits hits = searcher.Search(parser.Parse(Lucene.Net.Util.English.IntToEnglish(n)));
				System.Console.Out.WriteLine("Search for " + n + ": total=" + hits.Length());
				for (int j = 0; j < System.Math.Min(3, hits.Length()); j++)
				{
					System.Console.Out.WriteLine("Hit for " + n + ": " + hits.Doc(j).Get("id"));
				}
			}
		}
		
		[STAThread]
		public static void  Main(System.String[] args)
		{
			
			bool readOnly = false;
			bool add = false;
			
			for (int i = 0; i < args.Length; i++)
			{
				if ("-ro".Equals(args[i]))
					readOnly = true;
				if ("-add".Equals(args[i]))
					add = true;
			}
			
			System.IO.FileInfo indexDir = new System.IO.FileInfo("index");
			bool tmpBool;
			if (System.IO.File.Exists(indexDir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(indexDir.FullName);
			if (!tmpBool)
			{
				System.IO.Directory.CreateDirectory(indexDir.FullName);
			}
			
			IndexReader.Unlock(FSDirectory.GetDirectory(indexDir));
			
			if (!readOnly)
			{
				IndexWriter writer = new IndexWriter(indexDir, ANALYZER, !add);
				
				SupportClass.ThreadClass indexerThread = new IndexerThread(writer);
				indexerThread.Start();
				
				System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1000));
			}
			
			SearcherThread searcherThread1 = new SearcherThread(false);
			searcherThread1.Start();
			
			SEARCHER = new IndexSearcher(indexDir.ToString());
			
			SearcherThread searcherThread2 = new SearcherThread(true);
			searcherThread2.Start();
			
			SearcherThread searcherThread3 = new SearcherThread(true);
			searcherThread3.Start();
		}
	}
}