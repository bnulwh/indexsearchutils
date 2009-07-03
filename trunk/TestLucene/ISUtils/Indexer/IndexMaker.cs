﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using ISUtils.Common;
using ISUtils.Analysis.Chinese;
using ISUtils.Utils;
using ISUtils.Database.Writer;

namespace ISUtils.Indexer
{
    public class IndexMaker
    {
        private List<Source> sourceList;
        private List<IndexSet> indexList;
        private IndexerSet indexer;
        private DictionarySet dictSet;
        private Dictionary<IndexSet,Source> ordinaryDict;
        private Dictionary<IndexSet,Source> incremenDict;
        public IndexMaker(string filename)
        {
            try
            {
                Parser parser = new Parser(filename);
                sourceList = parser.GetSourceList();
                indexList = parser.GetIndexList();
                indexer = parser.GetIndexer();
                dictSet = parser.GetDictionarySet();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(string.Format("Exception for open file {0},{1}", filename, ex.ToString()));
#endif
                throw;
            }
            Init();
        }
        private void Init()
        {
            ordinaryDict = new Dictionary<IndexSet,Source>();
            incremenDict = new Dictionary<IndexSet,Source>();
            foreach (IndexSet index in indexList)
            {
                foreach (Source source in sourceList)
                {
                    if (source.SourceName.ToUpper().CompareTo(index.SourceName.ToUpper()) == 0)
                    {
                        if (index.Type == IndexTypeEnum.Ordinary)
                            ordinaryDict.Add(index,source);
                        else
                            incremenDict.Add(index,source);
                        break;
                    }
                }
            }
        }
        public bool CanIndex(TimeSpan span, IndexTypeEnum type)
        {
            if (type == IndexTypeEnum.Ordinary)
            {
                if (SupportClass.Time.IsTimeSame(DateTime.Now, indexer.MainIndexReCreateTime) &&
                    SupportClass.Time.GetDays(span) % indexer.MainIndexReCreateTimeSpan == 0)
                {
                    return true;
                }
            }
            else
            {
                if (SupportClass.Time.GetSeconds(span) % indexer.IncrIndexReCreateTimeSpan == 0)
                {
                    return true;
                }
            }
            return false;
        }
        public Message ExecuteIndexer(TimeSpan span, IndexTypeEnum type)
        {
            Message msg=new Message();
            if (CanIndex(span, type) == false)
            {
                msg.Result = "ExecuteIndexer does not run.";
                msg.Success = false;
                return msg;
            }
            try
            {
                Execute(ordinaryDict, dictSet, indexer, type == IndexTypeEnum.Ordinary, ref msg);
                msg.Result = "ExecuteIndexer Success.";
                msg.Success = true;
                return msg;
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine("Execute Indexer Error.Reason:" + e.Message);

#endif
                msg.Result = "Exception:" + e.StackTrace.ToString();
                msg.Success = false;
                msg.ExceptionOccur = true;
                return msg;
            }
        }
        public static void Execute(Dictionary<IndexSet, Source> dict,DictionarySet dictSet,IndexerSet indexer,bool create,ref Message msg)
        {
            try
            {
                DateTime allStart = DateTime.Now;
                msg.AddInfo("All Start at :" + allStart.ToLocalTime());
                Utils.IndexUtil.SetIndexSettings(dict, dictSet, indexer);
                //由于中文分词结果随中文词库的变化而变化，为了使索引不需要根据中文词库的变化而变化，
                //故采用默认的Analyzer来进行分词，即StandardAnalyzer
                //Utils.IndexUtil.UseDefaultChineseAnalyzer(true);
                Utils.IndexUtil.IndexEx(create);
                msg.AddInfo("All End at :"+DateTime.Now.ToLocalTime());
                TimeSpan allSpan=DateTime.Now -allStart;
                msg.AddInfo(string.Format("All Spend {0} millionseconds.",allSpan.TotalMilliseconds));
                msg.Success =true;
                msg.Result="Execute Success.";
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine("Execute Indexer Error.Reason:"+e.Message);
#endif
                msg.AddInfo("Write Index Error.Reason:"+e.StackTrace.ToString());
                msg.Success=false;
                msg.ExceptionOccur = true;
                throw e;
            }
        }
    }
}