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

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
	[TestFixture]
	public class TestLengthFilter : LuceneTestCase
	{
		[Test]
		public virtual void  TestFilter()
		{
			TokenStream stream = new WhitespaceTokenizer(new System.IO.StringReader("short toolong evenmuchlongertext a ab toolong foo"));
			LengthFilter filter = new LengthFilter(stream, 2, 6);
			Assert.AreEqual("short", filter.Next().TermText());
			Assert.AreEqual("ab", filter.Next().TermText());
			Assert.AreEqual("foo", filter.Next().TermText());
			Assert.IsNull(filter.Next());
		}
	}
}