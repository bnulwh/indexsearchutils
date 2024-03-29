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

namespace Lucene.Net.Util
{
	
	
	/// <summary> Methods for manipulating strings.
	/// 
	/// $Id: StringHelper.java 472959 2006-11-09 16:21:50Z yonik $
	/// </summary>
	public abstract class StringHelper
	{
		
		/// <summary> Compares two strings, character by character, and returns the
		/// first position where the two strings differ from one another.
		/// 
		/// </summary>
		/// <param name="s1">The first string to compare
		/// </param>
		/// <param name="s2">The second string to compare
		/// </param>
		/// <returns> The first position where the two strings differ.
		/// </returns>
		public static int StringDifference(System.String s1, System.String s2)
		{
			int len1 = s1.Length;
			int len2 = s2.Length;
			int len = len1 < len2 ? len1:len2;
			for (int i = 0; i < len; i++)
			{
				if (s1[i] != s2[i])
				{
					return i;
				}
			}
			return len;
		}
		
		
		private StringHelper()
		{
		}
	}
}