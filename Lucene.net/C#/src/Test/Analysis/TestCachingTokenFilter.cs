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
using TermVector = Lucene.Net.Documents.Field.TermVector;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using TermPositions = Lucene.Net.Index.TermPositions;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
	public class TestCachingTokenFilter : LuceneTestCase
	{
		private class AnonymousClassTokenStream : TokenStream
		{
			public AnonymousClassTokenStream(TestCachingTokenFilter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestCachingTokenFilter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestCachingTokenFilter enclosingInstance;
			public TestCachingTokenFilter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private int index = 0;
			
			public override Token Next()
			{
				if (index == Enclosing_Instance.tokens.Length)
				{
					return null;
				}
				else
				{
					return new Token(Enclosing_Instance.tokens[index++], 0, 0);
				}
			}
		}
		private System.String[] tokens = new System.String[]{"term1", "term2", "term3", "term2"};
		
		[Test]
		public virtual void  TestCaching()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new SimpleAnalyzer());
			Document doc = new Document();
			TokenStream stream = new AnonymousClassTokenStream(this);
			
			stream = new CachingTokenFilter(stream);
			
			doc.Add(new Field("preanalyzed", stream, TermVector.NO));
			
			// 1) we consume all tokens twice before we add the doc to the index
			CheckTokens(stream);
			stream.Reset();
			CheckTokens(stream);
			
			// 2) now add the document to the index and verify if all tokens are indexed
			//    don't reset the stream here, the DocumentWriter should do that implicitly
			writer.AddDocument(doc);
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			TermPositions termPositions = reader.TermPositions(new Term("preanalyzed", "term1"));
			Assert.IsTrue(termPositions.Next());
			Assert.AreEqual(1, termPositions.Freq());
			Assert.AreEqual(0, termPositions.NextPosition());
			
			termPositions.Seek(new Term("preanalyzed", "term2"));
			Assert.IsTrue(termPositions.Next());
			Assert.AreEqual(2, termPositions.Freq());
			Assert.AreEqual(1, termPositions.NextPosition());
			Assert.AreEqual(3, termPositions.NextPosition());
			
			termPositions.Seek(new Term("preanalyzed", "term3"));
			Assert.IsTrue(termPositions.Next());
			Assert.AreEqual(1, termPositions.Freq());
			Assert.AreEqual(2, termPositions.NextPosition());
			reader.Close();
			
			// 3) reset stream and consume tokens again
			stream.Reset();
			CheckTokens(stream);
		}
		
		private void  CheckTokens(TokenStream stream)
		{
			int count = 0;
			Token token;
			while ((token = stream.Next()) != null)
			{
				Assert.IsTrue(count < tokens.Length);
				Assert.AreEqual(tokens[count], token.TermText());
				count++;
			}
			
			Assert.AreEqual(tokens.Length, count);
		}
	}
}