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
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using TermQuery = Lucene.Net.Search.TermQuery;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
	
	/*
	Verify we can read the pre-2.1 file format, do searches
	against it, and add documents to it.*/
	
	[TestFixture]
	public class TestDeletionPolicy : LuceneTestCase
	{
		private void  VerifyCommitOrder(System.Collections.IList commits)
		{
			long last = SegmentInfos.GenerationFromSegmentsFileName(((IndexCommitPoint) commits[0]).GetSegmentsFileName());
			for (int i = 1; i < commits.Count; i++)
			{
				long now = SegmentInfos.GenerationFromSegmentsFileName(((IndexCommitPoint) commits[i]).GetSegmentsFileName());
				Assert.IsTrue(now > last, "SegmentInfos commits are out-of-order");
				last = now;
			}
		}
		
		internal class KeepAllDeletionPolicy : IndexDeletionPolicy
		{
			public KeepAllDeletionPolicy(TestDeletionPolicy enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestDeletionPolicy enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestDeletionPolicy enclosingInstance;
			public TestDeletionPolicy Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int numOnInit;
			internal int numOnCommit;
			public virtual void  OnInit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				numOnInit++;
			}
			public virtual void  OnCommit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				numOnCommit++;
			}
		}
		
		/// <summary> This is useful for adding to a big index w/ autoCommit
		/// false when you know readers are not using it.
		/// </summary>
		internal class KeepNoneOnInitDeletionPolicy : IndexDeletionPolicy
		{
			public KeepNoneOnInitDeletionPolicy(TestDeletionPolicy enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestDeletionPolicy enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestDeletionPolicy enclosingInstance;
			public TestDeletionPolicy Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int numOnInit;
			internal int numOnCommit;
			public virtual void  OnInit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				numOnInit++;
				// On init, delete all commit points:
				System.Collections.IEnumerator it = commits.GetEnumerator();
				while (it.MoveNext())
				{
					((IndexCommitPoint) it.Current).Delete();
				}
			}
			public virtual void  OnCommit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				int size = commits.Count;
				// Delete all but last one:
				for (int i = 0; i < size - 1; i++)
				{
					((IndexCommitPoint) commits[i]).Delete();
				}
				numOnCommit++;
			}
		}
		
		internal class KeepLastNDeletionPolicy : IndexDeletionPolicy
		{
			private void  InitBlock(TestDeletionPolicy enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestDeletionPolicy enclosingInstance;
			public TestDeletionPolicy Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int numOnInit;
			internal int numOnCommit;
			internal int numToKeep;
			internal int numDelete;
			internal System.Collections.Hashtable seen = new System.Collections.Hashtable();
			
			public KeepLastNDeletionPolicy(TestDeletionPolicy enclosingInstance, int numToKeep)
			{
				InitBlock(enclosingInstance);
				this.numToKeep = numToKeep;
			}
			
			public virtual void  OnInit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				numOnInit++;
				// do no deletions on init
				DoDeletes(commits, false);
			}
			
			public virtual void  OnCommit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				DoDeletes(commits, true);
			}
			
			private void  DoDeletes(System.Collections.IList commits, bool isCommit)
			{
				
				// Assert that we really are only called for each new
				// commit:
				if (isCommit)
				{
					System.String fileName = ((IndexCommitPoint) commits[commits.Count - 1]).GetSegmentsFileName();
					if (seen.Contains(fileName))
					{
						throw new System.SystemException("onCommit was called twice on the same commit point: " + fileName);
					}
					seen.Add(fileName, fileName);
					numOnCommit++;
				}
				int size = commits.Count;
				for (int i = 0; i < size - numToKeep; i++)
				{
					((IndexCommitPoint) commits[i]).Delete();
					numDelete++;
				}
			}
		}
		
		/*
		* Delete a commit only when it has been obsoleted by N
		* seconds.
		*/
		internal class ExpirationTimeDeletionPolicy : IndexDeletionPolicy
		{
			private void  InitBlock(TestDeletionPolicy enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestDeletionPolicy enclosingInstance;
			public TestDeletionPolicy Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			internal Directory dir;
			internal double expirationTimeSeconds;
			internal int numDelete;
			
			public ExpirationTimeDeletionPolicy(TestDeletionPolicy enclosingInstance, Directory dir, double seconds)
			{
				InitBlock(enclosingInstance);
				this.dir = dir;
				this.expirationTimeSeconds = seconds;
			}
			
			public virtual void  OnInit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				OnCommit(commits);
			}
			
			public virtual void  OnCommit(System.Collections.IList commits)
			{
				Enclosing_Instance.VerifyCommitOrder(commits);
				
				IndexCommitPoint lastCommit = (IndexCommitPoint) commits[commits.Count - 1];
				
				// Any commit older than expireTime should be deleted:
				double expireTime = dir.FileModified(lastCommit.GetSegmentsFileName()) / 1000.0 - expirationTimeSeconds;
				
				System.Collections.IEnumerator it = commits.GetEnumerator();
				
				while (it.MoveNext())
				{
					IndexCommitPoint commit = (IndexCommitPoint) it.Current;
					double modTime = dir.FileModified(commit.GetSegmentsFileName()) / 1000.0;
					if (commit != lastCommit && modTime < expireTime)
					{
						commit.Delete();
						numDelete += 1;
					}
				}
			}
		}
		
		/*
		* Test "by time expiration" deletion policy:
		*/
		[Test]
		public virtual void  TestExpirationTimeDeletionPolicy()
		{
			
			double SECONDS = 2.0;
			
			bool autoCommit = false;
			bool useCompoundFile = true;
			
			Directory dir = new RAMDirectory();
			ExpirationTimeDeletionPolicy policy = new ExpirationTimeDeletionPolicy(this, dir, SECONDS);
			IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
			writer.SetUseCompoundFile(useCompoundFile);
			writer.Close();
			
			long lastDeleteTime = 0;
			for (int i = 0; i < 7; i++)
			{
				// Record last time when writer performed deletes of
				// past commits
				lastDeleteTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
				writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false, policy);
				writer.SetUseCompoundFile(useCompoundFile);
				for (int j = 0; j < 17; j++)
				{
					AddDoc(writer);
				}
				writer.Close();
				
				// Make sure to sleep long enough so that some commit
				// points will be deleted:
				System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * (int) (1000.0 * (SECONDS / 5.0))));
			}
			
			// First, make sure the policy in fact deleted something:
			Assert.IsTrue(policy.numDelete > 0, "no commits were deleted");
			
			// Then simplistic check: just verify that the
			// segments_N's that still exist are in fact within SECONDS
			// seconds of the last one's mod time, and, that I can
			// open a reader on each:
			long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
			
			System.String fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);
			dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
			while (gen > 0)
			{
				try
				{
					IndexReader reader = IndexReader.Open(dir);
					reader.Close();
					fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);
					long modTime = dir.FileModified(fileName);
					Assert.IsTrue(lastDeleteTime - modTime <= (SECONDS * 1000), "commit point was older than " + SECONDS + " seconds (" + (lastDeleteTime - modTime) + " msec) but did not get deleted");
				}
				catch (System.IO.IOException)
				{
					// OK
					break;
				}
				
				dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
				gen--;
			}
			
			dir.Close();
		}
		
		/*
		* Test a silly deletion policy that keeps all commits around.
		*/
		[Test]
		public virtual void  TestKeepAllDeletionPolicy()
		{
			
			for (int pass = 0; pass < 4; pass++)
			{
				
				bool autoCommit = pass < 2;
				bool useCompoundFile = (pass % 2) > 0;
				
				KeepAllDeletionPolicy policy = new KeepAllDeletionPolicy(this);
				
				Directory dir = new RAMDirectory();
				
				IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
				writer.SetMaxBufferedDocs(10);
				writer.SetUseCompoundFile(useCompoundFile);
				for (int i = 0; i < 107; i++)
				{
					AddDoc(writer);
				}
				writer.Close();
				
				writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false, policy);
				writer.SetUseCompoundFile(useCompoundFile);
				writer.Optimize();
				writer.Close();
				
				Assert.AreEqual(2, policy.numOnInit);
				if (autoCommit)
				{
					Assert.IsTrue(policy.numOnCommit > 2);
				}
				else
				{
					// If we are not auto committing then there should
					// be exactly 2 commits (one per close above):
					Assert.AreEqual(2, policy.numOnCommit);
				}
				
				// Simplistic check: just verify all segments_N's still
				// exist, and, I can open a reader on each:
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
				while (gen > 0)
				{
					IndexReader reader = IndexReader.Open(dir);
					reader.Close();
					dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
					gen--;
					
					if (gen > 0)
					{
						// Now that we've removed a commit point, which
						// should have orphan'd at least one index file.
						// Open & close a writer and assert that it
						// actually removed something:
						int preCount = dir.List().Length;
						writer = new IndexWriter(dir, false, new WhitespaceAnalyzer(), false, policy);
						writer.Close();
						int postCount = dir.List().Length;
						Assert.IsTrue(postCount < preCount);
					}
				}
				
				dir.Close();
			}
		}
		
		/* Test keeping NO commit points.  This is a viable and
		* useful case eg where you want to build a big index with
		* autoCommit false and you know there are no readers.
		*/
		[Test]
		public virtual void  TestKeepNoneOnInitDeletionPolicy()
		{
			
			for (int pass = 0; pass < 4; pass++)
			{
				
				bool autoCommit = pass < 2;
				bool useCompoundFile = (pass % 2) > 0;
				
				KeepNoneOnInitDeletionPolicy policy = new KeepNoneOnInitDeletionPolicy(this);
				
				Directory dir = new RAMDirectory();
				
				IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
				writer.SetMaxBufferedDocs(10);
				writer.SetUseCompoundFile(useCompoundFile);
				for (int i = 0; i < 107; i++)
				{
					AddDoc(writer);
				}
				writer.Close();
				
				writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false, policy);
				writer.SetUseCompoundFile(useCompoundFile);
				writer.Optimize();
				writer.Close();
				
				Assert.AreEqual(2, policy.numOnInit);
				if (autoCommit)
				{
					Assert.IsTrue(policy.numOnCommit > 2);
				}
				else
				{
					// If we are not auto committing then there should
					// be exactly 2 commits (one per close above):
					Assert.AreEqual(2, policy.numOnCommit);
				}
				
				// Simplistic check: just verify the index is in fact
				// readable:
				IndexReader reader = IndexReader.Open(dir);
				reader.Close();
				
				dir.Close();
			}
		}
		
		/*
		* Test a deletion policy that keeps last N commits.
		*/
		[Test]
		public virtual void  TestKeepLastNDeletionPolicy()
		{
			
			int N = 5;
			
			for (int pass = 0; pass < 4; pass++)
			{
				
				bool autoCommit = pass < 2;
				bool useCompoundFile = (pass % 2) > 0;
				
				Directory dir = new RAMDirectory();
				
				KeepLastNDeletionPolicy policy = new KeepLastNDeletionPolicy(this, N);
				
				for (int j = 0; j < N + 1; j++)
				{
					IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
					writer.SetMaxBufferedDocs(10);
					writer.SetUseCompoundFile(useCompoundFile);
					for (int i = 0; i < 17; i++)
					{
						AddDoc(writer);
					}
					writer.Optimize();
					writer.Close();
				}
				
				Assert.IsTrue(policy.numDelete > 0);
				Assert.AreEqual(N + 1, policy.numOnInit);
				if (autoCommit)
				{
					Assert.IsTrue(policy.numOnCommit > 1);
				}
				else
				{
					Assert.AreEqual(N + 1, policy.numOnCommit);
				}
				
				// Simplistic check: just verify only the past N segments_N's still
				// exist, and, I can open a reader on each:
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
				for (int i = 0; i < N + 1; i++)
				{
					try
					{
						IndexReader reader = IndexReader.Open(dir);
						reader.Close();
						if (i == N)
						{
							Assert.Fail("should have failed on commits prior to last " + N);
						}
					}
					catch (System.IO.IOException e)
					{
						if (i != N)
						{
							throw e;
						}
					}
					if (i < N)
					{
						dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
					}
					gen--;
				}
				
				dir.Close();
			}
		}
		
		/*
		* Test a deletion policy that keeps last N commits
		* around, with reader doing deletes.
		*/
		[Test]
		public virtual void  TestKeepLastNDeletionPolicyWithReader()
		{
			
			int N = 10;
			
			for (int pass = 0; pass < 4; pass++)
			{
				
				bool autoCommit = pass < 2;
				bool useCompoundFile = (pass % 2) > 0;
				
				KeepLastNDeletionPolicy policy = new KeepLastNDeletionPolicy(this, N);
				
				Directory dir = new RAMDirectory();
				IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
				writer.SetUseCompoundFile(useCompoundFile);
				writer.Close();
				Term searchTerm = new Term("content", "aaa");
				Query query = new TermQuery(searchTerm);
				
				for (int i = 0; i < N + 1; i++)
				{
					writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false, policy);
					writer.SetUseCompoundFile(useCompoundFile);
					for (int j = 0; j < 17; j++)
					{
						AddDoc(writer);
					}
					// this is a commit when autoCommit=false:
					writer.Close();
					IndexReader reader = IndexReader.Open(dir, policy);
					reader.DeleteDocument(3 * i + 1);
					reader.SetNorm(4 * i + 1, "content", 2.0F);
					IndexSearcher searcher = new IndexSearcher(reader);
					Hits hits = searcher.Search(query);
					Assert.AreEqual(16 * (1 + i), hits.Length());
					// this is a commit when autoCommit=false:
					reader.Close();
					searcher.Close();
				}
				writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false, policy);
				writer.SetUseCompoundFile(useCompoundFile);
				writer.Optimize();
				// this is a commit when autoCommit=false:
				writer.Close();
				
				Assert.AreEqual(2 * (N + 2), policy.numOnInit);
				if (autoCommit)
				{
					Assert.IsTrue(policy.numOnCommit > 2 * (N + 2) - 1);
				}
				else
				{
					Assert.AreEqual(2 * (N + 2) - 1, policy.numOnCommit);
				}
				
				IndexSearcher searcher2 = new IndexSearcher(dir);
				Hits hits2 = searcher2.Search(query);
				Assert.AreEqual(176, hits2.Length());
				
				// Simplistic check: just verify only the past N segments_N's still
				// exist, and, I can open a reader on each:
				long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
				
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				int expectedCount = 176;
				
				for (int i = 0; i < N + 1; i++)
				{
					try
					{
						IndexReader reader = IndexReader.Open(dir);
						
						// Work backwards in commits on what the expected
						// count should be.  Only check this in the
						// autoCommit false case:
						if (!autoCommit)
						{
							searcher2 = new IndexSearcher(reader);
							hits2 = searcher2.Search(query);
							if (i > 1)
							{
								if (i % 2 == 0)
								{
									expectedCount += 1;
								}
								else
								{
									expectedCount -= 17;
								}
							}
							Assert.AreEqual(expectedCount, hits2.Length());
							searcher2.Close();
						}
						reader.Close();
						if (i == N)
						{
							Assert.Fail("should have failed on commits before last 5");
						}
					}
					catch (System.IO.IOException e)
					{
						if (i != N)
						{
							throw e;
						}
					}
					if (i < N)
					{
						dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
					}
					gen--;
				}
				
				dir.Close();
			}
		}
		
		/*
		* Test a deletion policy that keeps last N commits
		* around, through creates.
		*/
		[Test]
		public virtual void  TestKeepLastNDeletionPolicyWithCreates()
		{
			
			int N = 10;
			
			for (int pass = 0; pass < 4; pass++)
			{
				
				bool autoCommit = pass < 2;
				bool useCompoundFile = (pass % 2) > 0;
				
				KeepLastNDeletionPolicy policy = new KeepLastNDeletionPolicy(this, N);
				
				Directory dir = new RAMDirectory();
				IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
				writer.SetMaxBufferedDocs(10);
				writer.SetUseCompoundFile(useCompoundFile);
				writer.Close();
				Term searchTerm = new Term("content", "aaa");
				Query query = new TermQuery(searchTerm);
				
				for (int i = 0; i < N + 1; i++)
				{
					
					writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false, policy);
					writer.SetMaxBufferedDocs(10);
					writer.SetUseCompoundFile(useCompoundFile);
					for (int j = 0; j < 17; j++)
					{
						AddDoc(writer);
					}
					// this is a commit when autoCommit=false:
					writer.Close();
					IndexReader reader = IndexReader.Open(dir, policy);
					reader.DeleteDocument(3);
					reader.SetNorm(5, "content", 2.0F);
					IndexSearcher searcher = new IndexSearcher(reader);
					Hits hits = searcher.Search(query);
					Assert.AreEqual(16, hits.Length());
					// this is a commit when autoCommit=false:
					reader.Close();
					searcher.Close();
					
					writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true, policy);
					// This will not commit: there are no changes
					// pending because we opened for "create":
					writer.Close();
				}
				
				Assert.AreEqual(1 + 3 * (N + 1), policy.numOnInit);
				if (autoCommit)
				{
					Assert.IsTrue(policy.numOnCommit > 3 * (N + 1) - 1);
				}
				else
				{
					Assert.AreEqual(2 * (N + 1), policy.numOnCommit);
				}
				
				IndexSearcher searcher2 = new IndexSearcher(dir);
				Hits hits2 = searcher2.Search(query);
				Assert.AreEqual(0, hits2.Length());
				
				// Simplistic check: just verify only the past N segments_N's still
				// exist, and, I can open a reader on each:
				long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
				
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				int expectedCount = 0;
				
				for (int i = 0; i < N + 1; i++)
				{
					try
					{
						IndexReader reader = IndexReader.Open(dir);
						
						// Work backwards in commits on what the expected
						// count should be.  Only check this in the
						// autoCommit false case:
						if (!autoCommit)
						{
							searcher2 = new IndexSearcher(reader);
							hits2 = searcher2.Search(query);
							Assert.AreEqual(expectedCount, hits2.Length());
							searcher2.Close();
							if (expectedCount == 0)
							{
								expectedCount = 16;
							}
							else if (expectedCount == 16)
							{
								expectedCount = 17;
							}
							else if (expectedCount == 17)
							{
								expectedCount = 0;
							}
						}
						reader.Close();
						if (i == N)
						{
							Assert.Fail("should have failed on commits before last " + N);
						}
					}
					catch (System.IO.IOException e)
					{
						if (i != N)
						{
							throw e;
						}
					}
					if (i < N)
					{
						dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
					}
					gen--;
				}
				
				dir.Close();
			}
		}
		
		private void  AddDoc(IndexWriter writer)
		{
			Document doc = new Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
	}
}