﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using System.Data;
using ISUtils.Async;
namespace ISUtils.Database.Writer
{
    public abstract class DbWriterBase : DataBaseWriter
    {
        public event WriteTableCompletedEventHandler OnWriteTableCompleted;
        public event WriteRowCompletedEventHandler OnWriteRowCompleted;
        public event WriteDbProgressChangedEventHandler OnProgressChanged;
        protected int RowNum = 0;
        protected int Percent = 1;
        protected bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
        }
        protected virtual void OnWriteTableCompletedEvent(object sender, WriteTableCompletedEventArgs e)
        {
            if (OnWriteTableCompleted != null)
                OnWriteTableCompleted(sender, e);
        }
        protected virtual void OnWriteRowCompletedEvent(object sender, WriteRowCompletedEventArgs e)
        {
            if (OnWriteRowCompleted != null)
                OnWriteRowCompleted(sender, e);
        }
        protected virtual void OnProgressChangedEvent(object sender, WriteDbProgressChangedEventArgs e)
        {
            if (OnProgressChanged != null)
                OnProgressChanged(sender, e);
        }
        /**/
        /// <summary>
        /// 设定基本属性值
        /// </summary>
        /// <param name="analyzer">分析器</param>
        /// <param name="directory">索引存储路径</param>
        /// <param name="create">创建索引还是增量索引</param>
        public abstract void SetBasicProperties(Analyzer analyzer, string directory, bool create);
        /**/
        /// <summary>
        /// 设置优化参数值
        /// </summary>
        /// <param name="mergeFactor">合并因子 (mergeFactor)</param>
        /// <param name="maxBufferedDocs">文档内存最大存储数</param>
        public abstract void SetOptimProperties(int mergeFactor, int maxBufferedDocs);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public abstract void WriteDataTable(DataTable table);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public abstract void WriteDataTable(DataTable table,Dictionary<string,float> fieldsBoostDict);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public abstract void WriteDataTableWithEvent(DataTable table);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public abstract void WriteDataTableWithEvent(DataTable table, Dictionary<string, float> fieldsBoostDict);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public abstract void WriteDataTable(DataTable table,ref System.Windows.Forms.ProgressBar progressBar);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        public abstract void WriteDataTable(DataTable table, ref System.Windows.Forms.ToolStripProgressBar progressBar);
        /**/
        /// <summary>
        /// 对数据库一行进行索引
        /// </summary>
        /// <param name="row">数据库中的一行数据</param>
        /// <param name="boost">数据的权重</param>
        public abstract void WriteDataRow(DataRow row);
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public abstract void WriteDataRowCollection(DataRowCollection collection);
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public abstract void WriteDataRowCollectionWithNoEvent(DataRowCollection collection);
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public abstract void WriteDataRowCollection(DataRowCollection collection,ref System.Windows.Forms.ToolStripProgressBar progressBar);
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        public abstract void WriteDataRowCollection(DataRowCollection collection, ref System.Windows.Forms.ProgressBar progressBar);
        /**/
        /// <summary>
        /// 合并索引
        /// </summary>
        /// <param name="directoryPaths">索引存储路径列表</param>
        public abstract void MergeIndexes(params string[] directoryPaths);
    }
}