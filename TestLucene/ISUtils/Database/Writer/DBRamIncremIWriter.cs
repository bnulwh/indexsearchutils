﻿using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using System.Data;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using ISUtils.Async;
namespace ISUtils.Database.Writer
{
    /**/
    /// <summary>
    /// 数据库的增量索引写入器
    /// </summary>
    public class DBRamIncremIWriter : DbWriterBase, DataBaseWriter
    {
        /**/
        /// <summary>
        /// 索引文档
        /// </summary>
        private Document document;
        /**/
        /// <summary>
        /// 索引字段
        /// </summary>
        private Dictionary<string,Field> fieldDict;
        /**/
        /// <summary>
        /// 索引写入器
        /// </summary>
        private Analyzer analyzer;
        private IndexWriter ramWriter;
        private IndexWriter fsWriter;
        private FSDirectory fsDir;
        private RAMDirectory ramDir = new RAMDirectory();
        /**/
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="analyzer">分析器</param>
        /// <param name="directory">索引存储路径</param>
        /// <param name="create">创建索引还是增量索引</param>
        public DBRamIncremIWriter(Analyzer analyzer, string directory, int maxFieldLength, double ramBufferSize, int mergeFactor, int maxBufferedDocs)
        {
            document=new Document();
            fieldDict=new Dictionary<string,Field>();
            this.analyzer = analyzer;
            try
            {
                fsDir = FSDirectory.GetDirectory(directory, false);
            }
            catch (System.IO.IOException ioe)
            {
                fsDir = FSDirectory.GetDirectory(directory, true);
#if DEBUG
                System.Console.WriteLine(ioe.StackTrace.ToString());
#endif
            }
            try
            {
                if (ramDir == null)
                    ramDir = new RAMDirectory();
                fsWriter = new IndexWriter(fsDir, analyzer, false);
                ramWriter = new IndexWriter(ramDir, analyzer, true);
                fsWriter.SetMaxFieldLength(maxFieldLength);
                fsWriter.SetRAMBufferSizeMB(ramBufferSize);
                fsWriter.SetMergeFactor(mergeFactor);
                fsWriter.SetMaxBufferedDocs(maxBufferedDocs);
            }
            catch (System.IO.IOException ie)
            {
                if (ramDir == null)
                    ramDir = new RAMDirectory();
                fsWriter = new IndexWriter(fsDir, analyzer, true);
                ramWriter = new IndexWriter(ramDir, analyzer, true);
                fsWriter.SetMaxFieldLength(maxFieldLength);
                fsWriter.SetRAMBufferSizeMB(ramBufferSize);
                fsWriter.SetMergeFactor(mergeFactor);
                fsWriter.SetMaxBufferedDocs(maxBufferedDocs);
#if DEBUG
                System.Console.WriteLine(ie.StackTrace.ToString());
#endif
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /**/
        /// <summary>
        /// 析构函数
        ~DBRamIncremIWriter()
        {
            fsWriter.Close();
            ramWriter.Close();
        }
        /**/
        /// <summary>
        /// 设定基本属性值
        /// </summary>
        /// <param name="analyzer">分析器</param>
        /// <param name="directory">索引存储路径</param>
        /// <param name="create">创建索引还是增量索引</param>
        public override void SetBasicProperties(Analyzer analyzer, string directory, bool create)
        {
            this.analyzer = analyzer;
            if (fsWriter != null && ramDir !=null)
            {
                fsWriter.AddIndexes(new Directory[] { ramDir });
                fsWriter.Flush();
                ramWriter.Close();
            }
            try
            {
                fsDir = FSDirectory.GetDirectory(directory, false);
                if (ramDir == null)
                    ramDir = new RAMDirectory();
                fsWriter = new IndexWriter(fsDir, analyzer, false);
                ramWriter = new IndexWriter(ramDir, analyzer, true);
                fsWriter.SetMaxFieldLength(int.MaxValue);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /**/
        /// <summary>
        /// 设置优化参数值
        /// </summary>
        /// <param name="mergeFactor">合并因子 (mergeFactor)</param>
        /// <param name="maxBufferedDocs">文档内存最大存储数</param>
        public override void SetOptimProperties(int mergeFactor, int maxBufferedDocs)
        {
            if (fsWriter == null)
            {
                throw new Exception("The IndexWriter does not created.");
            }
            fsWriter.SetMergeFactor(mergeFactor);
            fsWriter.SetMaxBufferedDocs(maxBufferedDocs);
        }
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public override void WriteDataTable(DataTable table)
        {
            if (fsWriter == null || ramWriter == null)
            {
                throw new Exception("The IndexWriter does not created.");
            }
            if (document == null)
                document = new Document();
            this.isBusy = true;
            RowNum = table.Rows.Count;
            Percent = RowNum / SupportClass.PERCENTAGEDIVE+1;
            DataColumnCollection columns = table.Columns;
            foreach (DataColumn column in columns)
            {
                fieldDict.Add(column.ColumnName,new Field(column.ColumnName, "value", Field.Store.COMPRESS, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            }
#if DEBUG
            DateTime start = DateTime.Now;
#endif
            WriteDataRowCollectionWithNoEvent(table.Rows);
#if DEBUG
            TimeSpan span=DateTime.Now -start;
            System.Console.WriteLine(string.Format("Speed:{0}ms/line",span.TotalMilliseconds/table.Rows.Count));
#endif
            WriteTableCompletedEventArgs args = new WriteTableCompletedEventArgs(table.TableName);
            base.OnWriteTableCompletedEvent(this, args);
            this.isBusy = false;
        }
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public override void WriteDataTableWithEvent(DataTable table)
        {
            if (fsWriter == null || ramWriter == null)
            {
                throw new Exception("The IndexWriter does not created.");
            }
            if (document == null)
                document = new Document();
            this.isBusy = true;
            RowNum = table.Rows.Count;
            Percent = RowNum / SupportClass.PERCENTAGEDIVE + 1;
            DataColumnCollection columns = table.Columns;
            foreach (DataColumn column in columns)
            {
                fieldDict.Add(column.ColumnName, new Field(column.ColumnName, "value", Field.Store.COMPRESS, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            }
#if DEBUG
            DateTime start = DateTime.Now;
#endif
            WriteDataRowCollection(table.Rows);
#if DEBUG
            TimeSpan span = DateTime.Now - start;
            System.Console.WriteLine(string.Format("Speed:{0}ms/line", span.TotalMilliseconds / table.Rows.Count));
#endif
            WriteTableCompletedEventArgs args = new WriteTableCompletedEventArgs(table.TableName);
            base.OnWriteTableCompletedEvent(this, args);
            this.isBusy = false;
        }
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public override void WriteDataTable(DataTable table,ref System.Windows.Forms.ToolStripProgressBar progressBar)
        {
            if (fsWriter == null || ramWriter == null)
            {
                throw new Exception("The IndexWriter does not created.");
            }
            if (document == null)
                document = new Document();
            DataColumnCollection columns = table.Columns;
            foreach (DataColumn column in columns)
            {
                fieldDict.Add(column.ColumnName, new Field(column.ColumnName, "value", Field.Store.COMPRESS, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            }
            WriteDataRowCollection(table.Rows,ref progressBar);
        }
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public override void WriteDataTable(DataTable table, ref System.Windows.Forms.ProgressBar progressBar)
        {
            if (fsWriter == null || ramWriter == null)
            {
                throw new Exception("The IndexWriter does not created.");
            }
            if (document == null)
                document = new Document();
            DataColumnCollection columns = table.Columns;
            foreach (DataColumn column in columns)
            {
                fieldDict.Add(column.ColumnName, new Field(column.ColumnName, "value", Field.Store.COMPRESS, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            }
            WriteDataRowCollection(table.Rows,ref progressBar);
        }
        /**/
        /// <summary>
        /// 对数据库一行进行索引
        /// </summary>
        /// <param name="row">数据库中的一行数据</param>
        /// <param name="boost">数据的权重</param>
        public override void WriteDataRow(DataRow row, float boost)
        {
            DataColumnCollection columns = row.Table.Columns;
            foreach (DataColumn column in columns)
            {
//#if DEBUG
//                Console.WriteLine("Column: name " + column.ColumnName + "\tvalue " + row[column].ToString());
//#endif
                if (column.GetType() is DateTime)
                    fieldDict[column.ColumnName].SetValue(SupportClass.Time.GetLuceneDate((DateTime)row[column]));
                else
                    fieldDict[column.ColumnName].SetValue(row[column].ToString());
                document.RemoveField(column.ColumnName);
                document.Add(fieldDict[column.ColumnName]);
//#if DEBUG
//                Console.WriteLine("Column: name " + column.ColumnName + "\tvalue " + document.Get(column.ColumnName));
//#endif
                //doc.Add(new Field(column.ColumnName, row[column].ToString(), Field.Store.COMPRESS, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            }
            ramWriter.AddDocument(document);
        }
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public override void WriteDataRowCollection(DataRowCollection collection)
        {
            int i = 0;
#if DEBUG
            System.Console.WriteLine(string.Format("i={0},time={1}", i, DateTime.Now.ToLongTimeString()));
#endif
            foreach (DataRow row in collection)
            {
                WriteDataRow(row, 1.0f);
                i++;
#if DEBUG
                if (i % SupportClass.MAX_ROWS_WRITE == 0 )
                    System.Console.WriteLine(string.Format("i={0},time={1}",i,DateTime.Now.ToLongTimeString() ));
#endif
                WriteRowCompletedEventArgs args = new WriteRowCompletedEventArgs(RowNum, i);
                base.OnWriteRowCompletedEvent(this, args);
                if (i % Percent == 0)
                {
                    WriteDbProgressChangedEventArgs pargs = new WriteDbProgressChangedEventArgs(RowNum, i);
                    base.OnProgressChangedEvent(this, pargs);
                }
                if (i / SupportClass.RAM_FLUSH_NUM >= 1 && i % SupportClass.RAM_FLUSH_NUM == 0)
                {
                    ramWriter.Flush();
                    fsWriter.AddIndexes(new Directory[] { ramDir });
                    ramWriter.Close();
                    ramWriter = new IndexWriter(ramDir, analyzer, true);
                }
            }
            ramWriter.Flush();
            fsWriter.AddIndexes(new Directory[] { ramDir });
            ramWriter.Close();
            ramWriter = new IndexWriter(ramDir, analyzer, true);
            fsWriter.Flush();
            fsWriter.Optimize();
            fsWriter.Close();
        }
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public override void WriteDataRowCollectionWithNoEvent(DataRowCollection collection)
        {
            int i = 0;
            foreach (DataRow row in collection)
            {
                WriteDataRow(row, 1.0f);
                if (i / SupportClass.RAM_FLUSH_NUM >= 1 && i % SupportClass.RAM_FLUSH_NUM == 0)
                {
                    ramWriter.Flush();
                    fsWriter.AddIndexes(new Directory[] { ramDir });
                    ramWriter.Close();
                    ramWriter = new IndexWriter(ramDir, analyzer, true);
                }
            }
            ramWriter.Flush();
            fsWriter.AddIndexes(new Directory[] { ramDir });
            ramWriter.Close();
            ramWriter = new IndexWriter(ramDir, analyzer, true);
            fsWriter.Flush();
            fsWriter.Optimize();
            fsWriter.Close();
        }
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public override void WriteDataRowCollection(DataRowCollection collection, ref System.Windows.Forms.ToolStripProgressBar progressBar)
        {
            int i = 0;
            progressBar.Maximum = collection.Count;
            progressBar.Minimum = 0;
            progressBar.Value = 0;
            foreach (DataRow row in collection)
            {
                WriteDataRow(row, 1.0f);
                i++;
                if (i % SupportClass.MAX_ROWS_WRITE == 0)
                {
                    System.Windows.Forms.Application.DoEvents();
                    progressBar.Value = i;
                }
                if (i / SupportClass.RAM_FLUSH_NUM >= 1 && i % SupportClass.RAM_FLUSH_NUM == 0)
                {
                    ramWriter.Flush();
                    fsWriter.AddIndexes(new Directory[] { ramDir });
                    ramWriter.Close();
                    ramWriter = new IndexWriter(ramDir, analyzer, true);
                }
            }
            ramWriter.Flush();
            fsWriter.AddIndexes(new Directory[] { ramDir });
            ramWriter.Close();
            ramWriter = new IndexWriter(ramDir, analyzer, true);
            fsWriter.Flush();
            fsWriter.Optimize();
            fsWriter.Close();
        }
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public override void WriteDataRowCollection(DataRowCollection collection, ref System.Windows.Forms.ProgressBar progressBar)
        {
            int i = 0;
            progressBar.Maximum = collection.Count;
            progressBar.Minimum = 0;
            progressBar.Value = 0;
            foreach (DataRow row in collection)
            {
                WriteDataRow(row, 1.0f);
                i++;
                if (i % SupportClass.MAX_ROWS_WRITE == 0)
                {
                    System.Windows.Forms.Application.DoEvents();
                    progressBar.Value = i;
                }
                if (i / SupportClass.RAM_FLUSH_NUM >= 1 && i % SupportClass.RAM_FLUSH_NUM == 0)
                {
                    ramWriter.Flush();
                    fsWriter.AddIndexes(new Directory[] { ramDir });
                    ramWriter.Close();
                    ramWriter = new IndexWriter(ramDir, analyzer, true);
                }
            }
            ramWriter.Flush();
            fsWriter.AddIndexes(new Directory[] { ramDir });
            ramWriter.Close();
            ramWriter = new IndexWriter(ramDir, analyzer, true);
            fsWriter.Flush();
            fsWriter.Optimize();
            fsWriter.Close();
        }
        /**/
        /// <summary>
        /// 合并索引
        /// </summary>
        /// <param name="directoryPaths">索引存储路径列表</param>
        public override void MergeIndexes(params string[] directoryPaths)
        {
            if (fsWriter == null)
            {
                throw new Exception("The IndexWriter does not created.");
            }
            List<Lucene.Net.Store.Directory> dictList = new List<Lucene.Net.Store.Directory>();
            foreach (string directory in directoryPaths)
            {
                dictList.Add(FSDirectory.GetDirectory(directory, false));
            }
            fsWriter.AddIndexes(dictList.ToArray());
            fsWriter.Flush();
            fsWriter.Optimize();
            fsWriter.Close();
        }
    }
}