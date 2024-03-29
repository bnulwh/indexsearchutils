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
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Occur = Lucene.Net.Search.BooleanClause.Occur;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> FilteredQuery JUnit tests.
	/// 
	/// <p>Created: Apr 21, 2004 1:21:46 PM
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: TestFilteredQuery.java 587050 2007-10-22 09:58:48Z doronc $
	/// </version>
	/// <since>   1.4
	/// </since>
	[TestFixture]
	public class TestFilteredQuery : LuceneTestCase
	{
		[Serializable]
		private class AnonymousClassFilter : Filter
		{
			public override System.Collections.BitArray Bits(IndexReader reader)
			{
				System.Collections.BitArray bitset = new System.Collections.BitArray((5 % 64 == 0 ? 5 / 64 : 5 / 64 + 1) * 64);
				bitset.Set(1, true);
				bitset.Set(3, true);
				return bitset;
			}
		}
		[Serializable]
		private class AnonymousClassFilter1 : Filter
		{
			public override System.Collections.BitArray Bits(IndexReader reader)
			{
				System.Collections.BitArray bitset = new System.Collections.BitArray((5 % 64 == 0 ? 5 / 64 : 5 / 64 + 1) * 64);
				for (int i = 0; i < 5; i++)
				{
					bitset.Set(i, true);
				} 
				return bitset;
			}
		}
		
		private IndexSearcher searcher;
		private RAMDirectory directory;
		private Query query;
		private Filter filter;
		
		[SetUp]
		public override void SetUp()
		{
			directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three four five", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "b", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three four", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "d", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three y", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "a", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two x", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "c", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			writer.Optimize();
			writer.Close();
			
			searcher = new IndexSearcher(directory);
			query = new TermQuery(new Term("field", "three"));
			filter = new AnonymousClassFilter();
		}
		
		// must be static for serialization tests
		private static Filter NewStaticFilterB()
		{
			return new AnonymousClassFilter();
		}
		
		[TearDown]
		public override void TearDown()
		{
			searcher.Close();
			directory.Close();
		}
		
		[Test]
		public virtual void  TestFilteredQuery_Renamed_Method()
		{
			Query filteredquery = new FilteredQuery(query, filter);
			Hits hits = searcher.Search(filteredquery);
			Assert.AreEqual(1, hits.Length());
			Assert.AreEqual(1, hits.Id(0));
			QueryUtils.Check(filteredquery, searcher);
			
			hits = searcher.Search(filteredquery, new Sort("sorter"));
			Assert.AreEqual(1, hits.Length());
			Assert.AreEqual(1, hits.Id(0));
			
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "one")), filter);
			hits = searcher.Search(filteredquery);
			Assert.AreEqual(2, hits.Length());
			QueryUtils.Check(filteredquery, searcher);
			
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "x")), filter);
			hits = searcher.Search(filteredquery);
			Assert.AreEqual(1, hits.Length());
			Assert.AreEqual(3, hits.Id(0));
			QueryUtils.Check(filteredquery, searcher);
			
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "y")), filter);
			hits = searcher.Search(filteredquery);
			Assert.AreEqual(0, hits.Length());
			QueryUtils.Check(filteredquery, searcher);
			
			// test boost
			Filter f = NewStaticFilterA();
			
			float boost = 2.5f;
			BooleanQuery bq1 = new BooleanQuery();
			TermQuery tq = new TermQuery(new Term("field", "one"));
			tq.SetBoost(boost);
			bq1.Add(tq, Occur.MUST);
			bq1.Add(new TermQuery(new Term("field", "five")), Occur.MUST);
			
			BooleanQuery bq2 = new BooleanQuery();
			tq = new TermQuery(new Term("field", "one"));
			filteredquery = new FilteredQuery(tq, f);
			filteredquery.SetBoost(boost);
			bq2.Add(filteredquery, Occur.MUST);
			bq2.Add(new TermQuery(new Term("field", "five")), Occur.MUST);
			AssertScoreEquals(bq1, bq2);
			
			Assert.AreEqual(boost, filteredquery.GetBoost(), 0);
			Assert.AreEqual(1.0f, tq.GetBoost(), 0); // the boost value of the underlying query shouldn't have changed 
		}
		
		// must be static for serialization tests 
		private static Filter NewStaticFilterA()
		{
			return new AnonymousClassFilter1();
		}
		
		/// <summary> Tests whether the scores of the two queries are the same.</summary>
		public virtual void  AssertScoreEquals(Query q1, Query q2)
		{
			Hits hits1 = searcher.Search(q1);
			Hits hits2 = searcher.Search(q2);
			
			Assert.AreEqual(hits1.Length(), hits2.Length());
			
			for (int i = 0; i < hits1.Length(); i++)
			{
				Assert.AreEqual(hits1.Score(i), hits2.Score(i), 0.0000001f);
			}
		}
		
		/// <summary> This tests FilteredQuery's rewrite correctness</summary>
		[Test]
		public virtual void  TestRangeQuery()
		{
			RangeQuery rq = new RangeQuery(new Term("sorter", "b"), new Term("sorter", "d"), true);
			
			Query filteredquery = new FilteredQuery(rq, filter);
			Hits hits = searcher.Search(filteredquery);
			Assert.AreEqual(2, hits.Length());
			QueryUtils.Check(filteredquery, searcher);
		}

		[Test]		
		public virtual void  TestBoolean()
		{
			BooleanQuery bq = new BooleanQuery();
			Query query = new FilteredQuery(new MatchAllDocsQuery(), new Lucene.Net.search.SingleDocTestFilter(0));
			bq.Add(query, BooleanClause.Occur.MUST);
			query = new FilteredQuery(new MatchAllDocsQuery(), new Lucene.Net.search.SingleDocTestFilter(1));
			bq.Add(query, BooleanClause.Occur.MUST);
			Hits hits = searcher.Search(bq);
			Assert.AreEqual(0, hits.Length());
			QueryUtils.Check(query, searcher);
		}
	}
}