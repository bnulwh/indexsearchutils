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

using NUnit.Framework;

using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using ParseException = Lucene.Net.QueryParsers.ParseException;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	
	/// <summary>Test BooleanQuery2 against BooleanQuery by overriding the standard query parser.
	/// This also tests the scoring order of BooleanQuery.
	/// </summary>
	[TestFixture]
	public class TestBoolean2 : LuceneTestCase
	{
		[Serializable]
		private class AnonymousClassDefaultSimilarity:DefaultSimilarity
		{
			public AnonymousClassDefaultSimilarity(TestBoolean2 enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestBoolean2 enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestBoolean2 enclosingInstance;
			public TestBoolean2 Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override float Coord(int overlap, int maxOverlap)
			{
				return overlap / ((float) maxOverlap - 1);
			}
		}
		private IndexSearcher searcher;
		
		public const System.String field = "field";
		
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < docFields.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				doc.Add(new Field(field, docFields[i], Field.Store.NO, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Close();
			searcher = new IndexSearcher(directory);
		}
		
		private System.String[] docFields = new System.String[]{"w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3"};
		
		public virtual Query MakeQuery(System.String queryText)
		{
			Query q = (new Lucene.Net.QueryParsers.QueryParser(field, new WhitespaceAnalyzer())).Parse(queryText);
			return q;
		}
		
		public virtual void  QueriesTest(System.String queryText, int[] expDocNrs)
		{
			//System.out.println();
			//System.out.println("Query: " + queryText);
			try
			{
				Query query1 = MakeQuery(queryText);
				BooleanQuery.SetAllowDocsOutOfOrder(true);
				Hits hits1 = searcher.Search(query1);
				
				Query query2 = MakeQuery(queryText); // there should be no need to parse again...
				BooleanQuery.SetAllowDocsOutOfOrder(false);
				Hits hits2 = searcher.Search(query2);
				
				CheckHits.CheckHitsQuery(query2, hits1, hits2, expDocNrs);
			}
			finally
			{
				// even when a test fails.
				BooleanQuery.SetAllowDocsOutOfOrder(false);
			}
		}
		
		[Test]
		public virtual void  TestQueries01()
		{
			System.String queryText = "+w3 +xx";
			int[] expDocNrs = new int[]{2, 3};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries02()
		{
			System.String queryText = "+w3 xx";
			int[] expDocNrs = new int[]{2, 3, 1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries03()
		{
			System.String queryText = "w3 xx";
			int[] expDocNrs = new int[]{2, 3, 1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries04()
		{
			System.String queryText = "w3 -xx";
			int[] expDocNrs = new int[]{1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries05()
		{
			System.String queryText = "+w3 -xx";
			int[] expDocNrs = new int[]{1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries06()
		{
			System.String queryText = "+w3 -xx -w5";
			int[] expDocNrs = new int[]{1};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries07()
		{
			System.String queryText = "-w3 -xx -w5";
			int[] expDocNrs = new int[]{};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries08()
		{
			System.String queryText = "+w3 xx -w5";
			int[] expDocNrs = new int[]{2, 3, 1};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries09()
		{
			System.String queryText = "+w3 +xx +w2 zz";
			int[] expDocNrs = new int[]{2, 3};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries10()
		{
			System.String queryText = "+w3 +xx +w2 zz";
			int[] expDocNrs = new int[]{2, 3};
			searcher.SetSimilarity(new AnonymousClassDefaultSimilarity(this));
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestRandomQueries()
		{
			System.Random rnd = new System.Random((System.Int32) 0);
			
			System.String[] vals = new System.String[]{"w1", "w2", "w3", "w4", "w5", "xx", "yy", "zzz"};
			
			int tot = 0;
			
			try
			{
				
				// increase number of iterations for more complete testing
				for (int i = 0; i < 1000; i++)
				{
					int level = rnd.Next(3);
					BooleanQuery q1 = RandBoolQuery(new System.Random((System.Int32) i), level, field, vals, null);
					
					// Can't sort by relevance since floating point numbers may not quite
					// match up.
					Sort sort = Sort.INDEXORDER;
					
					BooleanQuery.SetAllowDocsOutOfOrder(false);
					
					QueryUtils.Check(q1, searcher);
					
					Hits hits1 = searcher.Search(q1, sort);
					if (hits1.Length() > 0)
						hits1.Id(hits1.Length() - 1);
					
					BooleanQuery.SetAllowDocsOutOfOrder(true);
					Hits hits2 = searcher.Search(q1, sort);
					if (hits2.Length() > 0)
						hits2.Id(hits1.Length() - 1);
					tot += hits2.Length();
					CheckHits.CheckEqual(q1, hits1, hits2);
				}
			}
			finally
			{
				// even when a test fails.
				BooleanQuery.SetAllowDocsOutOfOrder(false);
			}
			
			// System.out.println("Total hits:"+tot);
		}
		
		
		// used to set properties or change every BooleanQuery
		// generated from randBoolQuery.
		public interface Callback
		{
			void  PostCreate(BooleanQuery q);
		}
		
		// Random rnd is passed in so that the exact same random query may be created
		// more than once.
		public static BooleanQuery RandBoolQuery(System.Random rnd, int level, System.String field, System.String[] vals, TestBoolean2.Callback cb)
		{
			BooleanQuery current = new BooleanQuery(rnd.Next() < 0);
			for (int i = 0; i < rnd.Next(vals.Length) + 1; i++)
			{
				int qType = 0; // term query
				if (level > 0)
				{
					qType = rnd.Next(10);
				}
				Query q;
				if (qType < 7)
					q = new TermQuery(new Term(field, vals[rnd.Next(vals.Length)]));
				else
					q = RandBoolQuery(rnd, level - 1, field, vals, cb);
				
				int r = rnd.Next(10);
				BooleanClause.Occur occur;
				if (r < 2)
					occur = BooleanClause.Occur.MUST_NOT;
				else if (r < 5)
					occur = BooleanClause.Occur.MUST;
				else
					occur = BooleanClause.Occur.SHOULD;
				
				current.Add(q, occur);
			}
			if (cb != null)
				cb.PostCreate(current);
			((System.Collections.ArrayList)current.Clauses()).TrimToSize();
			return current;
		}
	}
}