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

using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using Lock = Lucene.Net.Store.Lock;

namespace Lucene.Net.Index
{
	
	
	/// <summary> Class for accessing a compound stream.
	/// This class implements a directory, but is limited to only read operations.
	/// Directory methods that would normally modify data throw an exception.
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: CompoundFileReader.java 564236 2007-08-09 15:21:19Z gsingers $
	/// </version>
	public class CompoundFileReader : Directory
	{
		
		private int readBufferSize;
		
		private sealed class FileEntry
		{
			internal long offset;
			internal long length;
		}
		
		
		// Base info
		private Directory directory;
		private System.String fileName;
		
		private IndexInput stream;
		private System.Collections.Hashtable entries = new System.Collections.Hashtable();
		
		
		public CompoundFileReader(Directory dir, System.String name):this(dir, name, BufferedIndexInput.BUFFER_SIZE)
		{
		}
		
		public CompoundFileReader(Directory dir, System.String name, int readBufferSize)
		{
			directory = dir;
			fileName = name;
			this.readBufferSize = readBufferSize;
			
			bool success = false;
			
			try
			{
                stream = dir.OpenInput(name, readBufferSize);
				
				// read the directory and init files
				int count = stream.ReadVInt();
				FileEntry entry = null;
				for (int i = 0; i < count; i++)
				{
					long offset = stream.ReadLong();
					System.String id = stream.ReadString();
					
					if (entry != null)
					{
						// set length of the previous entry
						entry.length = offset - entry.offset;
					}
					
					entry = new FileEntry();
					entry.offset = offset;
					entries[id] = entry;
				}
				
				// set the length of the final entry
				if (entry != null)
				{
					entry.length = stream.Length() - entry.offset;
				}
				
				success = true;
			}
			finally
			{
				if (!success && (stream != null))
				{
					try
					{
						stream.Close();
					}
					catch (System.IO.IOException)
					{
					}
				}
			}
		}
		
		public virtual Directory GetDirectory()
		{
			return directory;
		}
		
		public virtual System.String GetName()
		{
			return fileName;
		}
		
		public override void  Close()
		{
			lock (this)
			{
				if (stream == null)
					throw new System.IO.IOException("Already closed");
				
				entries.Clear();
				stream.Close();
				stream = null;
			}
		}
		
		public override IndexInput OpenInput(System.String id)
		{
			lock (this)
			{
				// Default to readBufferSize passed in when we were opened
				return OpenInput(id, readBufferSize);
			}
		}

        public override IndexInput OpenInput(System.String id, int readBufferSize)
		{
			lock (this)
			{
				if (stream == null)
					throw new System.IO.IOException("Stream closed");
				
				FileEntry entry = (FileEntry) entries[id];
				if (entry == null)
					throw new System.IO.IOException("No sub-file with id " + id + " found");

                return new CSIndexInput(stream, entry.offset, entry.length, readBufferSize);
			}
		}
		
		/// <summary>Returns an array of strings, one for each file in the directory. </summary>
		public override System.String[] List()
		{
			System.String[] res = new System.String[entries.Count];
			entries.Keys.CopyTo(res, 0);
			return res;
        }
		
		/// <summary>Returns true iff a file with the given name exists. </summary>
		public override bool FileExists(System.String name)
		{
			return entries.ContainsKey(name);
		}
		
		/// <summary>Returns the time the compound file was last modified. </summary>
		public override long FileModified(System.String name)
		{
			return directory.FileModified(fileName);
		}
		
		/// <summary>Set the modified time of the compound file to now. </summary>
		public override void  TouchFile(System.String name)
		{
			directory.TouchFile(fileName);
		}
		
		/// <summary>Not implemented</summary>
		/// <throws>  UnsupportedOperationException  </throws>
		public override void  DeleteFile(System.String name)
		{
			throw new System.NotSupportedException();
		}
		
		/// <summary>Not implemented</summary>
		/// <throws>  UnsupportedOperationException  </throws>
		public override void  RenameFile(System.String from, System.String to)
		{
			throw new System.NotSupportedException();
		}
		
		/// <summary>Returns the length of a file in the directory.</summary>
		/// <throws>  IOException if the file does not exist  </throws>
		public override long FileLength(System.String name)
		{
			FileEntry e = (FileEntry) entries[name];
			if (e == null)
				throw new System.IO.IOException("File " + name + " does not exist");
			return e.length;
		}
		
		/// <summary>Not implemented</summary>
		/// <throws>  UnsupportedOperationException  </throws>
		public override IndexOutput CreateOutput(System.String name)
		{
			throw new System.NotSupportedException();
		}
		
		/// <summary>Not implemented</summary>
		/// <throws>  UnsupportedOperationException  </throws>
		public override Lock MakeLock(System.String name)
		{
			throw new System.NotSupportedException();
		}
		
		/// <summary>Implementation of an IndexInput that reads from a portion of the
		/// compound file. The visibility is left as "package" *only* because
		/// this helps with testing since JUnit test cases in a different class
		/// can then access package fields of this class.
		/// </summary>
		public /*internal*/ sealed class CSIndexInput : BufferedIndexInput
		{
			
			public /*internal*/ IndexInput base_Renamed;
			internal long fileOffset;
			internal long length;
			
			internal CSIndexInput(IndexInput base_Renamed, long fileOffset, long length) : this(base_Renamed, fileOffset, length, BufferedIndexInput.BUFFER_SIZE)
			{
			}
			
			internal CSIndexInput(IndexInput base_Renamed, long fileOffset, long length, int readBufferSize) : base(readBufferSize)
			{
				this.base_Renamed = base_Renamed;
				this.fileOffset = fileOffset;
				this.length = length;
			}
			
			/// <summary>Expert: implements buffer refill.  Reads bytes from the current
			/// position in the input.
			/// </summary>
			/// <param name="b">the array to read bytes into
			/// </param>
			/// <param name="offset">the offset in the array to start storing bytes
			/// </param>
			/// <param name="len">the number of bytes to read
			/// </param>
			protected internal override void ReadInternal(byte[] b, int offset, int len)
			{
				lock (base_Renamed)
				{
					long start = GetFilePointer();
					if (start + len > length)
						throw new System.IO.IOException("read past EOF");
					base_Renamed.Seek(fileOffset + start);
                    base_Renamed.ReadBytes(b, offset, len, false);
				}
			}
			
			/// <summary>Expert: implements seek.  Sets current position in this file, where
			/// the next {@link #ReadInternal(byte[],int,int)} will occur.
			/// </summary>
			/// <seealso cref="ReadInternal(byte[],int,int)">
			/// </seealso>
			protected internal override void  SeekInternal(long pos)
			{
			}
			
			/// <summary>Closes the stream to further operations. </summary>
			public override void  Close()
			{
			}
			
			public override long Length()
			{
				return length;
			}
		}
	}
}