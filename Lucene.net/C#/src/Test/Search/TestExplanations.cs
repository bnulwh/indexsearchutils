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
using ParseException = Lucene.Net.QueryParsers.ParseException;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Lucene.Net.Search.Spans;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests primative queries (ie: that rewrite to themselves) to
	/// insure they match the expected set of docs, and that the score of each
	/// match is equal to the value of the scores explanation.
	/// 
	/// <p>
	/// The assumption is that if all of the "primative" queries work well,
	/// then anythingthat rewrites to a primative will work well also.
	/// </p>
	/// 
	/// </summary>
	/// <seealso cref=""Subclasses for actual tests"">
	/// </seealso>
	[TestFixture]
	public class TestExplanations : LuceneTestCase
	{
		protected internal IndexSearcher searcher;
		
		public const System.String FIELD = "field";
		public static readonly Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser(FIELD, new WhitespaceAnalyzer());
		
		[TearDown]
		public override void TearDown()
		{
			base.TearDown();
			if (searcher != null)
			{
				searcher.Close();
				searcher = null;
			}
		}
		
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < docFields.Length; i++)
			{
				Document doc = new Document();
				doc.Add(new Field(FIELD, docFields[i], Field.Store.NO, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Close();
			searcher = new IndexSearcher(directory);
		}
		
		protected internal System.String[] docFields = new System.String[]{"w1 w2 w3 w4 w5", "w1 w3 w2 w3 zz", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3 zz"};
		
		public virtual Query MakeQuery(System.String queryText)
		{
			return qp.Parse(queryText);
		}
		
		/// <summary>check the expDocNrs first, then check the query (and the explanations) </summary>
		public virtual void  Qtest(System.String queryText, int[] expDocNrs)
		{
			Qtest(MakeQuery(queryText), expDocNrs);
		}
		
		/// <summary>check the expDocNrs first, then check the query (and the explanations) </summary>
		public virtual void  Qtest(Query q, int[] expDocNrs)
		{
			CheckHits.CheckHitCollector(q, FIELD, searcher, expDocNrs);
		}
		
		/// <summary> Tests a query using qtest after wrapping it with both optB and reqB</summary>
		/// <seealso cref="Qtest">
		/// </seealso>
		/// <seealso cref="ReqB">
		/// </seealso>
		/// <seealso cref="OptB">
		/// </seealso>
		public virtual void  Bqtest(Query q, int[] expDocNrs)
		{
			Qtest(ReqB(q), expDocNrs);
			Qtest(OptB(q), expDocNrs);
		}
		/// <summary> Tests a query using qtest after wrapping it with both optB and reqB</summary>
		/// <seealso cref="Qtest">
		/// </seealso>
		/// <seealso cref="ReqB">
		/// </seealso>
		/// <seealso cref="OptB">
		/// </seealso>
		public virtual void  Bqtest(System.String queryText, int[] expDocNrs)
		{
			Bqtest(MakeQuery(queryText), expDocNrs);
		}
		
		/// <summary>A filter that only lets the specified document numbers pass </summary>
		[Serializable]
		public class ItemizedFilter : Filter
		{
			internal int[] docs;
			public ItemizedFilter(int[] docs)
			{
				this.docs = docs;
			}
			public override System.Collections.BitArray Bits(IndexReader r)
			{
				System.Collections.BitArray b = new System.Collections.BitArray((r.MaxDoc() % 64 == 0?r.MaxDoc() / 64:r.MaxDoc() / 64 + 1) * 64);
				for (int i = 0; i < docs.Length; i++)
				{
					b.Set(docs[i], true);
				}
				return b;
			}
		}
		
		/// <summary>helper for generating MultiPhraseQueries </summary>
		public static Term[] Ta(System.String[] s)
		{
			Term[] t = new Term[s.Length];
			for (int i = 0; i < s.Length; i++)
			{
				t[i] = new Term(FIELD, s[i]);
			}
			return t;
		}
		
		/// <summary>MACRO for SpanTermQuery </summary>
		public virtual SpanTermQuery St(System.String s)
		{
			return new SpanTermQuery(new Term(FIELD, s));
		}
		
		/// <summary>MACRO for SpanNotQuery </summary>
		public virtual SpanNotQuery Snot(SpanQuery i, SpanQuery e)
		{
			return new SpanNotQuery(i, e);
		}
		
		/// <summary>MACRO for SpanOrQuery containing two SpanTerm queries </summary>
		public virtual SpanOrQuery Sor(System.String s, System.String e)
		{
			return Sor(St(s), St(e));
		}
		/// <summary>MACRO for SpanOrQuery containing two SpanQueries </summary>
		public virtual SpanOrQuery Sor(SpanQuery s, SpanQuery e)
		{
			return new SpanOrQuery(new SpanQuery[]{s, e});
		}
		
		/// <summary>MACRO for SpanOrQuery containing three SpanTerm queries </summary>
		public virtual SpanOrQuery Sor(System.String s, System.String m, System.String e)
		{
			return Sor(St(s), St(m), St(e));
		}
		/// <summary>MACRO for SpanOrQuery containing two SpanQueries </summary>
		public virtual SpanOrQuery Sor(SpanQuery s, SpanQuery m, SpanQuery e)
		{
			return new SpanOrQuery(new SpanQuery[]{s, m, e});
		}
		
		/// <summary>MACRO for SpanNearQuery containing two SpanTerm queries </summary>
		public virtual SpanNearQuery Snear(System.String s, System.String e, int slop, bool inOrder)
		{
			return Snear(St(s), St(e), slop, inOrder);
		}
		/// <summary>MACRO for SpanNearQuery containing two SpanQueries </summary>
		public virtual SpanNearQuery Snear(SpanQuery s, SpanQuery e, int slop, bool inOrder)
		{
			return new SpanNearQuery(new SpanQuery[]{s, e}, slop, inOrder);
		}
		
		
		/// <summary>MACRO for SpanNearQuery containing three SpanTerm queries </summary>
		public virtual SpanNearQuery Snear(System.String s, System.String m, System.String e, int slop, bool inOrder)
		{
			return Snear(St(s), St(m), St(e), slop, inOrder);
		}
		/// <summary>MACRO for SpanNearQuery containing three SpanQueries </summary>
		public virtual SpanNearQuery Snear(SpanQuery s, SpanQuery m, SpanQuery e, int slop, bool inOrder)
		{
			return new SpanNearQuery(new SpanQuery[]{s, m, e}, slop, inOrder);
		}
		
		/// <summary>MACRO for SpanFirst(SpanTermQuery) </summary>
		public virtual SpanFirstQuery Sf(System.String s, int b)
		{
			return new SpanFirstQuery(St(s), b);
		}
		
		/// <summary> MACRO: Wraps a Query in a BooleanQuery so that it is optional, along
		/// with a second prohibited clause which will never match anything
		/// </summary>
		public virtual Query OptB(System.String q)
		{
			return OptB(MakeQuery(q));
		}
		/// <summary> MACRO: Wraps a Query in a BooleanQuery so that it is optional, along
		/// with a second prohibited clause which will never match anything
		/// </summary>
		public virtual Query OptB(Query q)
		{
			BooleanQuery bq = new BooleanQuery(true);
			bq.Add(q, BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("NEVER", "MATCH")), BooleanClause.Occur.MUST_NOT);
			return bq;
		}
		
		/// <summary> MACRO: Wraps a Query in a BooleanQuery so that it is required, along
		/// with a second optional clause which will match everything
		/// </summary>
		public virtual Query ReqB(System.String q)
		{
			return ReqB(MakeQuery(q));
		}
		/// <summary> MACRO: Wraps a Query in a BooleanQuery so that it is required, along
		/// with a second optional clause which will match everything
		/// </summary>
		public virtual Query ReqB(Query q)
		{
			BooleanQuery bq = new BooleanQuery(true);
			bq.Add(q, BooleanClause.Occur.MUST);
			bq.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			return bq;
		}
		
		/// <summary> Placeholder: JUnit freaks if you don't have one test ... making
		/// class abstract doesn't help
		/// </summary>
		[Test]
		public virtual void  TestNoop()
		{
			/* NOOP */
		}
	}
}