﻿using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Documents;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ISUtils.Common
{
    [Serializable]
    public class SearchRecord : IXmlSerializable, IComparable
    {
        #region 属性
        private string name = "";
        private string caption = "";
        private string index = "";
        private float score = 0.0f;
        private string primaryKey = "";
        private List<SearchField> fieldList = new List<SearchField>();
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string Caption
        {
            get { return caption; }
            set { caption = value; }
        }
        public string IndexName
        {
            get { return index; }
            set { index = value; }
        }
        public float Score
        {
            get { return score; }
            set { score = value; }
        }
        public string PrimaryKey
        {
            get { return primaryKey; }
            set { primaryKey = value; }
        }
        public List<SearchField> Fields
        {
            get { return fieldList; }
            set { fieldList = value; }
        }
        public SearchField this[int index]
        {
            get 
            {
                if (fieldList==null)
                    return null;
                if (index < 0 || index >= fieldList.Count)
                    return null;
                return fieldList[index];
            }
            set
            {
                if (fieldList == null)
                    return;
                if (index >= 0 || index < fieldList.Count)
                    fieldList[index] = value;
            }
        }
        public SearchField this[string name]
        {
            get
            {
                if (fieldList == null)
                    return null;
                foreach (SearchField sf in fieldList)
                {
                    if (sf.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) || sf.Caption.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                        return sf;
                }
                return null;
            }
            set
            {
                if (fieldList == null)
                    return;
                foreach (SearchField sf in fieldList)
                {
                    if (sf.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) || sf.Caption.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        fieldList.Remove(sf);
                        fieldList.Add(value);
                    }
                }
            }
        }
        #endregion
        #region 构造函数
        public SearchRecord()
        {
            if (fieldList == null)
                fieldList = new List<SearchField>();
        }
        public SearchRecord(string name, string caption, string index)
        {
            this.name = name;
            this.caption = caption;
            this.index = index;
            if (this.fieldList == null)
                this.fieldList = new List<SearchField>();
        }
        public SearchRecord(string name, string caption, string index,float score)
        {
            this.name = name;
            this.caption = caption;
            this.index = index;
            this.score = score;
            if (this.fieldList == null)
                this.fieldList = new List<SearchField>();
        }
        public SearchRecord(string name, string caption, string index, List<SearchField> fields)
        {
            this.name = name;
            this.caption = caption;
            this.index = index;
            this.fieldList = fields;
            if (this.fieldList == null)
                this.fieldList = new List<SearchField>();
        }
        public SearchRecord(string name, string caption, string index,float score, List<SearchField> fields)
        {
            this.name = name;
            this.caption = caption;
            this.index = index;
            this.score = score;
            this.fieldList = fields;
            if (this.fieldList == null)
                this.fieldList = new List<SearchField>();
        }
        public SearchRecord(string index, List<SearchField> fields)
            : this(string.Empty, string.Empty, index, fields)
        { 
        }
        public SearchRecord(string name, string caption, string index, params SearchField[] fields)
        {
            this.name = name;
            this.caption = caption;
            this.index = index;
            if (this.fieldList == null)
                this.fieldList = new List<SearchField>();
            this.fieldList.AddRange(fields);
        }
        public SearchRecord(string name, string caption, string index,float score, params SearchField[] fields)
        {
            this.name = name;
            this.caption = caption;
            this.index = index;
            this.score = score;
            if (this.fieldList == null)
                this.fieldList = new List<SearchField>();
            this.fieldList.AddRange(fields);
        }
        public SearchRecord(string index, params SearchField[] fields)
            : this(string.Empty, string.Empty, index, fields)
        { 
        }
        public SearchRecord(string name, string caption, string index, Document doc)
            :this(name,caption,index)
        {
            Field[] fields = new Field[doc.GetFields().Count];
            doc.GetFields().CopyTo(fields, 0);
            foreach (Field field in fields)
            {
                fieldList.Add(new SearchField(field));
            }
        }
        public SearchRecord(Document doc)
            :this(string.Empty,string.Empty,string.Empty,doc)
        {           
        }
#if INDEXSET
        public SearchRecord(IndexSet set, List<SearchField> fields)
            : this(set.IndexName, set.Caption, set.IndexName, fields)
        { 
        }
        public SearchRecord(IndexSet set, List<SearchField> fields,string pk,float score)
            : this(set.IndexName, set.Caption, set.IndexName,score, fields)
        {
            this.primaryKey = pk;
        }
#endif
        #endregion
        #region Override
        public override string ToString()
        {
            StringBuilder title = new StringBuilder();
            StringBuilder content = new StringBuilder();
            title.Append(name+" "+caption + " ");
            foreach (SearchField sf in fieldList)
            {
                if (sf.IsTitle)
                    title.Append(sf.Name+":"+sf.Value + "\t");
                else
                    content.Append(sf.Name +":"+sf.Value + "\t");
            }
            title.Append("\n");
            content.Append("\n");
            return title.ToString() + content.ToString();
        }
        #endregion
        #region 方法
        public void Add(SearchField field)
        {
            if (fieldList == null)
                fieldList = new List<SearchField>();
            fieldList.Add(field);
        }
        public string ToWebString()
        {
            StringBuilder title = new StringBuilder();
            StringBuilder content=new StringBuilder();
            title.Append(caption + "&nbsp;");
            foreach (SearchField sf in fieldList)
            {
                if (sf.IsTitle)
                    title.Append(sf.Value + "&nbsp;");
                else
                    content.Append(sf.Value + "&nbsp;");
            }
            title.Append("<br>");
            content.Append("<br>");
            return title.ToString() + content.ToString();
        }
        public void GetWebInfo(out string szTitle, out string szContent, bool removeNullOrEmpty,bool showTitleCaption)
        {
            StringBuilder title = new StringBuilder();
            StringBuilder content = new StringBuilder();
            SearchField.Direction = ReverserInfo.Direction.DESC;
            fieldList.Sort();
            //title.Append(caption + "&nbsp;");
            foreach (SearchField sf in fieldList)
            {
                if (sf.Visible)
                {
                    if (sf.IsTitle)
                    {
                        if (!removeNullOrEmpty)
                        {
                            if (showTitleCaption)
                            {
                                title.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                            }
                            else
                            {
                                title.Append(sf.Result + "&nbsp;");
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(sf.Result))
                            {
                                if (showTitleCaption)
                                {
                                    title.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                                }
                                else
                                {
                                    title.Append(sf.Result + "&nbsp;");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!removeNullOrEmpty)
                        {
                            content.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(sf.Result))
                                content.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                        }
                    }
                }
            }
            szTitle = title.ToString();
            szContent = content.ToString();
        }
        public void GetWebInfo(out string szTitle, out string szContent, bool removeNullOrEmpty)
        {
            StringBuilder title = new StringBuilder();
            StringBuilder content = new StringBuilder();
            SearchField.Direction = ReverserInfo.Direction.DESC;
            fieldList.Sort();
            //title.Append(caption + "&nbsp;");
            foreach (SearchField sf in fieldList)
            {
                if (sf.Visible)
                {
                    if (sf.IsTitle)
                    {
                        if (!removeNullOrEmpty)
                        {
                            title.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(sf.Result))
                                title.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                        }
                    }
                    else
                    {
                        if (!removeNullOrEmpty)
                        {
                            content.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(sf.Result))
                                content.Append(sf.Caption + "：" + sf.Result + "&nbsp;");
                        }
                    }
                }
            }
            szTitle = title.ToString();
            szContent = content.ToString();
        }
        public void GetWebInfo(out string szTitle, out string szContent, bool removeNullOrEmpty, string color, bool removeHighLigt)
        {
            StringBuilder title = new StringBuilder();
            StringBuilder content = new StringBuilder();
            SearchField.Direction = ReverserInfo.Direction.DESC;
            fieldList.Sort();
            //title.Append(caption + "&nbsp;");
            foreach (SearchField sf in fieldList)
            {
                if (sf.Visible)
                {
                    if (sf.IsTitle)
                    {
                        if (!removeNullOrEmpty)
                        {
                            title.Append(sf.Caption + "：" + SupportClass.Result.GetResult(sf.Result, color, removeHighLigt) + "&nbsp;");
                        }
                        else
                        {
                            if(!string.IsNullOrEmpty(sf.Result))
                                title.Append(sf.Caption + "：" + SupportClass.Result.GetResult(sf.Result, color, removeHighLigt) + "&nbsp;");
                        }
                    }
                    else
                    {
                        if (!removeNullOrEmpty)
                        {
                            content.Append(sf.Caption + "：" + SupportClass.Result.GetResult(sf.Result, color, removeHighLigt) + "&nbsp;");
                        }
                        else
                        {
                            if(!string.IsNullOrEmpty(sf.Result))
                                content.Append(sf.Caption + "：" + SupportClass.Result.GetResult(sf.Result, color, removeHighLigt) + "&nbsp;");
                        }
                    }
                }
            }
            szTitle = title.ToString();
            szContent = content.ToString();
        }
        public string ToMinString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("{" + name + ":" + caption + "}");
            foreach (SearchField field in fieldList)
            {
                buffer.Append("["+field.Name+":"+field.Value+"]");
            }
            return buffer.ToString();
        }
        #endregion
        #region 全局方法
        public static implicit operator SearchRecord(Document doc)
        {
            return new SearchRecord(doc);
        }
        public static List<SearchRecord> ToList(List<Document> docList)
        {
            List<SearchRecord> recordList = new List<SearchRecord>();
            foreach (Document doc in docList)
            {
                recordList.Add(new SearchRecord(doc));
            }
            return recordList;
        }
        public static List<SearchRecord> ToList(params Document[] docs)
        {
            List<SearchRecord> recordList = new List<SearchRecord>();
            foreach (Document doc in docs)
            {
                recordList.Add(new SearchRecord(doc));
            }
            return recordList;
        }
        #endregion
        #region "IXmlSerializable"
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
        public void ReadXml(XmlReader reader)
        {
            string currentElementName= reader.Name;
            string currentNodeName;
            string fieldName, fieldValue, fieldCaption;
            float fieldBoost;
            bool isTitle;
            Name = SupportClass.FileUtil.GetXmlAttribute(reader, "Name", typeof(string));
            Caption = SupportClass.FileUtil.GetXmlAttribute(reader, "Caption", typeof(string));
            IndexName = SupportClass.FileUtil.GetXmlAttribute(reader, "Index", typeof(string));
            do
            {
                currentNodeName = reader.Name;
                if (currentNodeName == currentElementName && (reader.MoveToContent() == XmlNodeType.EndElement || reader.IsEmptyElement))
                {
                    break;
                }
                switch (currentNodeName)
                {
                    case "Field":
                        fieldName = SupportClass.FileUtil.GetXmlAttribute(reader, "Name", typeof(string));
                        fieldValue = SupportClass.FileUtil.GetXmlAttribute(reader, "Value", typeof(string));
                        fieldBoost = float.Parse(SupportClass.FileUtil.GetXmlAttribute(reader, "Boost", typeof(float)));
                        fieldCaption = SupportClass.FileUtil.GetXmlAttribute(reader, "Caption", typeof(string));
                        isTitle = bool.Parse(SupportClass.FileUtil.GetXmlAttribute(reader, "IsTitle", typeof(bool)));
                        Fields.Add(new SearchField(fieldName, fieldCaption, fieldValue, fieldBoost, isTitle));
                        //Fields.Add(new SearchField(fieldName, fieldName, fieldValue, 1.0f, false));
                        reader.Read();
                        break;
                    default:
                        reader.Read();
                        break;
                }
            } while (true);
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Doc");
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Caption", Caption);
            writer.WriteAttributeString("Index", IndexName);
            foreach (SearchField field in Fields)
            {
                if (!field.Name.Equals(PrimaryKey) && !field.Visible)
                    continue;
                writer.WriteStartElement("Field");
                writer.WriteAttributeString("Name", field.Name);
                writer.WriteAttributeString("Caption", field.Caption);
                writer.WriteAttributeString("Value", field.Value);
                writer.WriteAttributeString("Boost", field.Boost.ToString());
                writer.WriteAttributeString("IsTitle", field.IsTitle.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        #endregion
        #region IComparable
        private static ReverserInfo.Direction direct = ReverserInfo.Direction.ASC;
        public static ReverserInfo.Direction Direction
        {
            get { return direct; }
            set { direct = value; }
        }
        public int CompareTo(object obj)
        {
            if (!(obj is SearchRecord))
                throw new InvalidCastException("This object is not of type SearchRecord");
            SearchRecord sr = (SearchRecord)obj;
            if (Direction == ReverserInfo.Direction.ASC)
            {
                if (this.score > sr.score)
                    return 1;
                else if (this.score == sr.score)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (this.score > sr.score)
                    return -1;
                else if (this.score == sr.score)
                    return 0;
                else
                    return 1;
            }
        }
        #endregion
    }
}
