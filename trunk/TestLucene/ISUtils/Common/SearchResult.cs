﻿using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Documents;

namespace ISUtils.Common
{
    [Serializable]
    public class SearchResult
    {
        private int pageNum = 0;
        public int PageNum
        {
            get { return pageNum; }
            set { pageNum = value; }
        }
        private Lucene.Net.Search.Query query;
        public Lucene.Net.Search.Query Query
        {
            get { return query; }
            set { query = value; }
        }
        private int totalPages = 1;
        public int TotalPages
        {
            get { return totalPages; }
            set { totalPages = value; }
        }
        private List<Document> docList = new List<Document>();
        public List<Document> Docs
        {
            get 
            {
                if (docList == null)
                    docList = new List<Document>();
                return docList;
            }
            set
            {
                docList = value;
            }
        }
        public SearchResult()
        { 
        }
        public SearchResult(List<Document> docList)
        {
            this.docList = docList;
        }
        public void AddResult(List<Document> docList)
        {
            if (this.docList == null)
                this.docList = new List<Document>();
            this.docList.AddRange(docList);
        }
        public void AddResult(Document[] docs)
        {
            if (this.docList == null)
                this.docList = new List<Document>();
            this.docList.AddRange(docs); 
        }
        public void AddResult(Document doc)
        {
            if (this.docList == null)
                this.docList = new List<Document>();
            this.docList.Add(doc);
        }
    }
}
