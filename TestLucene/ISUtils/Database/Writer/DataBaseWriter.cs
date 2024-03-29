﻿using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using System.Data;
namespace ISUtils.Database.Writer
{
    public interface DataBaseWriter
    {
        /**/
        /// <summary>
        /// 设定基本属性值
        /// </summary>
        /// <param name="analyzer">分析器</param>
        /// <param name="directory">索引存储路径</param>
        /// <param name="create">创建索引还是增量索引</param>
        void SetBasicProperties(Analyzer analyzer, string directory, bool create);
        /**/
        /// <summary>
        /// 设置优化参数值
        /// </summary>
        /// <param name="mergeFactor">合并因子 (mergeFactor)</param>
        /// <param name="maxBufferedDocs">文档内存最大存储数</param>
        void SetOptimProperties(int mergeFactor, int maxBufferedDocs);
        /**/
        /// <summary>
        /// 对数据库表进行索引
        /// </summary>
        /// <param name="table">数据库表名</param>
        void WriteDataTable(DataTable table);
        /**/
        /// <summary>
        /// 对数据库一行进行索引
        /// </summary>
        /// <param name="row">数据库中的一行数据</param>
        /// <param name="boost">数据的权重</param>
        void WriteDataRow(DataRow row);
        /**/
        /// <summary>
        /// 对数据库行进行索引
        /// </summary>
        /// <param name="collection">数据库中行数据</param>
        void WriteDataRowCollection(DataRowCollection collection);
        /**/
        /// <summary>
        /// 合并索引
        /// </summary>
        /// <param name="directoryPaths">索引存储路径列表</param>
        void MergeIndexes(params string[] directoryPaths);
    }
}