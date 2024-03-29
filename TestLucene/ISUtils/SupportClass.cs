﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Lucene.Net.Documents;

namespace ISUtils
{
    using ISUtils.Common;
    public class SupportClass
    {
        public const string TableFileNameField = "__TABLEFILECAPTION__";
        public const string TFNFieldValue = "文件";
        public const string TextFileTypes = ".txt|.ini|.sql|.log|.dat|.bat|.cmd" ;
        public const string ImageFileTypes = ".jpg|.jpeg|.png|.bmp|.dib|.gif|.jpe|.jfif|.tif|.tiff";
        public static bool WriteLogAccess = false;
        public static string LogPath=@"D:\TEMP.LOG";
        public const int RAM_FLUSH_NUM = 100000;
        public const int MAX_ROWS_WRITE = 1000;
        public const int PERCENTAGEDIVE = 10000;
        public const int FRAGMENT_SIZE = 100;
        public const string Splitor = " \t,;#，；&|";
        public sealed class Offset
        {
            public int Start=0;
            public int End=0;
            public Offset()
            { }
            public Offset(int start, int end)
            {
                Start = start;
                End = end;
            }
            public int Length
            {
                get { return End - Start + 1; }
            }
        }
        public class Formatter
        {
            public static string GetDefValue(Type type)
            {
                string tstr = type.ToString().ToLower();
                if (tstr == "system.double")
                    return "0.0";
                else if (tstr == "system.int32")
                    return "0";
                else if (tstr == "system.datetime")
                    return "1949-10-1";
                else if (tstr == "system.boolean")
                    return "False";
                else if (tstr == "system.string")
                    return "";
                else
                    return "";
            }
            public static string GetValue(Type type, object obj)
            {
                if (obj != null)
                {
                    return obj.ToString();
                }
                else
                {
                    if (type is string)
                        return string.Empty;
                    else if (type is int)
                        return "0";
                    else if (type is float)
                        return "0.0";
                    else if (type is decimal)
                        return "0.0";
                    else if (type is double)
                        return "0.0";
                    else if (type is bool)
                        return "False";
                    else if (type is long)
                        return "0";
                    else if (type is DateTime)
                        return "1949-10-1";
                    else
                        return string.Empty;
                }
            }
        }
        public class String
        {
            public const string seprator ="\t,;.，；";
            /// <summary>
            /// 去掉HTML标签
            /// </summary>
            /// <param name="strHtml"></param>
            /// <returns></returns>
            public static string DropHTML(string strHtml)
            {
                string[] aryReg ={
                                    @"<script[^>]*?>.*?</script>",

                                    @"<(\/\s*)?!?((\w+:)?\w+)(\w+(\s*=?\s*(([""''])(\\[""''tbnr]|[^\7])*?\7|\w+)|.{0})|\s)*?(\/\s*)?>",
                                    @"([\r\n])[\s]+",
                                    @"&(quot|#34);",
                                    @"&(amp|#38);",
                                    @"&(lt|#60);",
                                    @"&(gt|#62);", 
                                    @"&(nbsp|#160);", 
                                    @"&(iexcl|#161);",
                                    @"&(cent|#162);",
                                    @"&(pound|#163);",
                                    @"&(copy|#169);",
                                    @"&#(\d+);",
                                    @"-->",
                                    @"<!--.*\n"
                                 };

                string[] aryRep = {
                                    "",
                                    "",
                                    "",
                                    "\"",
                                    "&",
                                    "<",
                                    ">",
                                    " ",
                                    "\xa1",//chr(161),
                                    "\xa2",//chr(162),
                                    "\xa3",//chr(163),
                                    "\xa9",//chr(169),
                                    "",
                                    "\r\n",
                                    ""
                                  };
                string strOutput = strHtml;
                for (int i = 0; i < aryReg.Length; i++)
                {
                    string newReg = aryReg[i];
                    string newRep = aryRep[i];
                    Regex regex = new Regex(newReg, RegexOptions.IgnoreCase);
                    strOutput = regex.Replace(strOutput, newRep);
                }

                strOutput.Replace("<", "");
                strOutput.Replace(">", "");
                strOutput.Replace("\r\n", "");


                return strOutput;
            }
            public static string[] Split(string src)
            {
                return src.Split(Splitor.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            public static string[] Split(string src, string seprator)
            {
                return src.Split(seprator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            /**/
            /// <summary>
            /// 判读字符串的前缀
            /// </summary>
            /// <param name="src">源字符串</param>
            /// <param name="prefix">字符串前缀</param>
            /// <returns>返回类型</returns>
            public static bool StartsWithNoCase(string src, string prefix)
            {                
                if (src.ToUpper().StartsWith(prefix.ToUpper()))
                {
                    if (src.Length == prefix.Length)
                        return true;
                    char ch = src.ToCharArray()[prefix.Length];
                    switch (ch)
                    {
                        case ' ':
                        case '\t':
                        case '=':
                            return true;
                        default:
                            return false;
                    }
                }
                return false;
            }
            /**/
            /// <summary>
            /// 返回格式化字符串
            /// </summary>
            /// <param name="src">源字符串</param>
            /// <returns>返回类型</returns>
            public static string FormatStr(string src)
            {
                string ret = src.Replace('\t', ' ');
                return ret.Trim();
            }
            /**/
            /// <summary>
            /// 连接指定的字符串数组
            /// </summary>
            /// <param name="strs">字符串数组</param>
            /// <returns>返回类型</returns>
            public static string GetConnectStr(params string[] strs)
            {
                string result = "";
                foreach (string s in strs)
                {
                    result += s + ";";
                }
                if (string.IsNullOrEmpty(result) == false)
                    result = result.Substring(0, result.Length - 1);
                return result;
            }
            public static int GetSameLength(string srca, string srcb)
            {
                for (int i = 0; i < srca.Length && i < srcb.Length; i++)
                {
                    if (srca[i] == srcb[i])
                        continue;
                    else
                        return i;
                }
                return srca.Length < srcb.Length ? srca.Length : srcb.Length;
            }
            /**/
            /// <summary>
            /// 获取OFFSETS列表
            /// </summary>
            /// <param name="result">Token</param>
            /// <param name="result">Token</param>
            /// <returns>返回类型</returns>
            public static Offset[] GetOffsets(string src, string[] tokens)
            {//我们都是中国人； 我 我们 都 是 中国 中国人;我们 我 都 是 中国人 中国 人
                //中国人民解放军 中国 中国人民  人民 人民解放军 解放军
                int start = 0,ptr=0;
                Offset[] offsets = new Offset[tokens.Length];
                if (tokens.Length >= 1)
                {
                    offsets[ptr] = new Offset(start, tokens[0].Length-1);
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        int len = GetSameLength(tokens[ptr], tokens[i]);
                        if (len <= 0)
                        {
                            ptr = i;
                            start = src.IndexOf(tokens[i], start);
                        }
                        offsets[i] = new Offset(start, start + tokens[i].Length-1);
                        //temp.IndexOf(
                    }
                }
                return offsets;
            }
            public static string LeftOf(string src, int len)
            {
                if (string.IsNullOrEmpty(src))
                    return "";
                if (len >= src.Length)
                    return src;
                return src.Substring(0, len);
            }
            public static string RightOf(string src, int len)
            {
                if (string.IsNullOrEmpty(src))
                    return "";
                if (len >= src.Length)
                    return src;
                return src.Substring(src.Length - len, len);
            }
        }
        public class FileUtil
        {
            public static bool IsFileExists(string filename)
            {
                return System.IO.File.Exists(filename);
            }
            public static string ReadTextFile(string filename)
            {
                if (filename == null)
                    throw new ArgumentNullException("filename", "filename must no be null!");
                if (IsFileExists(filename) == false)
                    throw new ArgumentException("filename must be exists!", "filename");
                StreamReader sr = System.IO.File.OpenText(filename);
                string result=sr.ReadToEnd();
                sr.Close();
                return result;
            }
            /**/
            /// <summary>
            /// 返回文本文件内容
            /// </summary>
            /// <param name="filename">文本文件名</param>
            /// <returns>返回类型</returns>
            public static List<string> GetFileText(string filename)
            {
                if (filename == null)
                    throw new ArgumentNullException("filename", "filename must no be null!");
                if (IsFileExists(filename) == false)
                    throw new ArgumentException("filename must be exists!", "filename");
                StreamReader sr = System.IO.File.OpenText(filename);
                string line;
                List<string> results = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    results.Add(line);
#if DEBUG
                    Console.WriteLine(line);
#endif
                }
                sr.Close();
                return results;
            }
            public static void WriteLog(string detail)
            {
                if (!WriteLogAccess) return;
                if (IsFileExists(LogPath) == false)
                    return;
                try
                {
                    FileStream fs = new FileStream(LogPath, FileMode.Append);
                    StreamWriter sw = new StreamWriter(fs);
                    string str = "[" + Time.GetDateTime() + "]\t" + detail;
                    sw.WriteLine(str);
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            public static void WriteToLog(string path, string detail)
            {
                if (IsFileExists(path) == false)
                    return;
                try
                {
                    FileStream fs = new FileStream(path, FileMode.Append);
                    StreamWriter sw = new StreamWriter(fs);
                    string str = "[" + Time.GetDateTime() + "]\t" + detail;
                    sw.WriteLine(str);
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
#if INDEXSET
            public static void WriteConfigFile(string path, List<Source> sourceList, List<IndexSet> indexList,FileIndexSet fileSet, DictionarySet dictSet, IndexerSet indexerSet, SearchSet searchSet)
            {
                try
                {
                    if (IsFileExists(path) != false)
                    {
                        System.IO.File.Delete(path); 
                    }
                    FileStream fs = new FileStream(path, FileMode.CreateNew);
                    StreamWriter sw = new StreamWriter(fs,Encoding.GetEncoding("gb2312"));
                    if(sourceList !=null)
                    foreach (Source s in sourceList)
                        Source.WriteToFile(ref sw, s);
                    if (indexList!=null)
                    foreach (IndexSet i in indexList)
                        IndexSet.WriteToFile(ref sw, i);
                    if (fileSet != null)
                        FileIndexSet.WriteToFile(ref sw, fileSet);
                    if (dictSet !=null)
                        DictionarySet.WriteToFile(ref sw, dictSet);
                    if(indexerSet !=null)
                        IndexerSet.WriteToFile(ref sw, indexerSet);
                    if(searchSet !=null)
                        SearchSet.WriteToFile(ref sw, searchSet);
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
#endif
            public static object GetObjectFromXmlFile(string path, Type type)
            {
                object obj = new object();
                if (!IsFileExists(path))
                    throw new ArgumentNullException("path", "path is not valid in GetObjectFromXmlFile");
                FileStream reader=null;
                while (reader == null)
                {
                    try
                    {
                        reader = new FileStream(path, FileMode.Open);
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                XmlSerializer xsr = new XmlSerializer(type);
                obj = xsr.Deserialize(reader);
                reader.Close();
                return obj;
            }
            public static void WriteObjectToXmlFile(string path, object obj, Type type)
            {
                FileStream writer = new FileStream(path, FileMode.Create);
                XmlSerializer xsr = new XmlSerializer(type);
                xsr.Serialize(writer, obj);
                writer.Close();
            }
            public static Config GetConfigFromExcelFile(string path)
            {
                Config config = new Config();
                List<Source> sourceList = Database.DbCommon.GetExcelSettings(path);
                config.SourceList.AddRange(dict.Values);
                config.IndexList.AddRange(dict.Keys);
                return config;
            }
            public static string GetXmlAttribute(XmlReader reader, string attribute,Type type)
            {
                string value = "";
                try
                {
                    value= reader.GetAttribute(attribute);
                }
                catch (Exception)
                {
                    value = null;
                }
                if (value == null)
                {
                    if (type ==typeof(int))
                        value = "0";
                    else if (type ==typeof( float))
                        value = "1.0f";
                    else if (type ==typeof( double))
                        value = "0.0";
                    else if (type ==typeof( bool))
                        value = "false";
                    else if (type ==typeof( DateTime))
                        value = DateTime.Now.ToString();
                    else if (type ==typeof( IndexType))
                        value = "Ordinary";
                    else
                        value = string.Empty;
                }
                return value;
            }
            public static bool ConvertXlsConfigToXmlConfig(string xlsFile, string xmlFile)
            {
                try
                {
                    Config nc = GetConfigFromExcelFile(xlsFile);
                    XmlSerializer xsr = new XmlSerializer(typeof(Config));
                    FileStream writer = new FileStream(xmlFile, FileMode.Create);
                    xsr.Serialize(writer, nc);
                    writer.Close();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            public static List<string> GetDirFiles(string dirPath, string searchPatterns)
            {
                List<string> fileList = null;
                GetDirFiles(dirPath, searchPatterns, ref fileList);
                return fileList;
            }
            public static void GetDirFiles(string dirPath, string searchPatterns, ref List<string> fileList)
            {
                if (fileList == null)
                    fileList = new List<string>();
                if (string.IsNullOrEmpty(dirPath))
                    return;
                if (!Directory.Exists(dirPath))
                    return;
                DirectoryInfo dir = new DirectoryInfo(dirPath);
                DirectoryInfo[] dirs = dir.GetDirectories();
                if (string.IsNullOrEmpty(searchPatterns))
                {
                    FileInfo[] files = dir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        fileList.Add(file.FullName);
                    }
                }
                else
                {
                    string[] searchPattenArray = searchPatterns.Split(" ,;|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (string searchPattern in searchPattenArray)
                    {
                        FileInfo[] files = dir.GetFiles(searchPattern);
                        foreach (FileInfo file in files)
                        {
                            fileList.Add(file.FullName);
                        }
                    }
                }
                foreach (DirectoryInfo dirInfo in dirs)
                {
                    GetDirFiles(dirInfo.FullName, searchPatterns, ref fileList);
                }
            }
            public static void GetDirFiles(string dirPath, string searchPatterns, ref List<string> fileList, ref List<string> dirList)
            {
                if (fileList == null)
                    fileList = new List<string>();
                if (dirList == null)
                    dirList = new List<string>();
                if (string.IsNullOrEmpty(dirPath))
                    return;
                if (!Directory.Exists(dirPath))
                    return;
                DirectoryInfo dir = new DirectoryInfo(dirPath);
                DirectoryInfo[] dirs = dir.GetDirectories();
                dirList.Add(dirPath);
                if (string.IsNullOrEmpty(searchPatterns))
                {
                    FileInfo[] files = dir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        fileList.Add(file.FullName);
                    }
                }
                else
                {
                    string[] searchPattenArray = searchPatterns.Split(" ,;|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (string searchPattern in searchPattenArray)
                    {
                        FileInfo[] files = dir.GetFiles(searchPattern);
                        foreach (FileInfo file in files)
                        {
                            fileList.Add(file.FullName);
                        }
                    }
                }
                foreach (DirectoryInfo dirInfo in dirs)
                {
                    GetDirFiles(dirInfo.FullName, searchPatterns, ref fileList, ref dirList);
                }
            }
            public static bool IsTextFile(string filepath)
            {
                if (filepath == null)
                    return false;
                if (IsFileExists(filepath) == false)
                    return false;
                string[] textTypes = String.Split(TextFileTypes, "|");
                foreach (string text in textTypes)
                {
                    if (filepath.EndsWith(text, true, null))
                        return true;
                }
                return false;
            }
            public static bool IsImageFile(string filepath)
            {
                if (filepath == null)
                    return false;
                if (IsFileExists(filepath) == false)
                    return false;
                string[] imageTypes = String.Split(ImageFileTypes, "|");
                foreach (string text in imageTypes)
                {
                    if (filepath.EndsWith(text, true, null))
                        return true;
                }
                return false;
            }
            public static void DeleteFolder(string folder)
            {
                try
                {
                    foreach (string dir in Directory.GetFileSystemEntries(folder))
                    {
                        if (System.IO.File.Exists(dir))
                        {
                            FileInfo info = new FileInfo(dir);
                            if (info.Attributes.ToString().IndexOf("ReadOnly") != -1)
                                info.Attributes = FileAttributes.Normal;
                            System.IO.File.Delete(dir);
                        }
                        else
                        {
                            DeleteFolder(dir);
                        }
                    }
                    Directory.Delete(folder);
                }
                catch (Exception)
                {                    
                }
            }
            public static List<FileInfo> GetDirFilesEx(string dirPath, string searchPatterns)
            {
                List<FileInfo> fileList = null;
                GetDirFilesEx(dirPath, searchPatterns, ref fileList);
                return fileList;
            }
            public static void GetDirFilesEx(string dirPath, string searchPatterns, ref List<FileInfo> fileList)
            {
                if (fileList == null)
                    fileList = new List<FileInfo>();
                if (string.IsNullOrEmpty(dirPath))
                    return;
                if (!Directory.Exists(dirPath))
                    return;
                DirectoryInfo dir = new DirectoryInfo(dirPath);
                DirectoryInfo[] dirs = dir.GetDirectories();
                if (string.IsNullOrEmpty(searchPatterns))
                {
                    FileInfo[] files = dir.GetFiles();
                    fileList.AddRange(files);
                }
                else
                {
                    string[] searchPattenArray = searchPatterns.Split(" ,;|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (string searchPattern in searchPattenArray)
                    {
                        FileInfo[] files = dir.GetFiles(searchPattern);
                        fileList.AddRange(files);
                    }
                }
                foreach (DirectoryInfo dirInfo in dirs)
                {
                    GetDirFilesEx(dirInfo.FullName, searchPatterns, ref fileList);
                }
            }
        }
        public class Time
        {
            public static string GetDateTime()
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
            }
            public static long GetHours(TimeSpan span)
            {
                return (long)span.TotalHours;
            }
            public static long GetMinutes(TimeSpan span)
            {
                return (long)span.TotalMinutes;
            }
            public static long GetMillionSeconds(TimeSpan span)
            {
                return (long)span.TotalMilliseconds;
            }
            public static long GetSeconds(TimeSpan span)
            {
                return (long)span.TotalSeconds;
            }
            public static long GetDays(TimeSpan span)
            {
                return (long)span.TotalDays;
            }
            public static string FormatTimeSpan(TimeSpan span)
            {
                return span.Days.ToString("00") + "." +
                       span.Hours.ToString("00") + ":" +
                       span.Minutes.ToString("00") + ":" +
                       span.Seconds.ToString("00") + "." +
                       span.Milliseconds.ToString("000");
            }
            public static bool IsTimeSame(DateTime dta, DateTime dtb)
            {
                return dta.Hour == dtb.Hour && dta.Minute == dtb.Minute;
            }
            public static string GetLuceneTime(DateTime date)
            {
                return date.ToString("HHmmss");
            }
            public static string GetLuceneDate(DateTime date)
            {
                return date.ToString("yyyyMMdd");
            }
            public static string GetLuceneDateTime(DateTime dt)
            {
                return dt.ToString("yyyyMMddHHmmss");
            }
        }
        public class Result
        {
            public static void Output(QueryResult result)
            {
                if (result.docs.Count > 0)
                {
                    foreach (QueryResult.SearchInfo info in result.docs.Keys)
                    {
                        Console.WriteLine("index :" + info.IndexName);
                        foreach (QueryResult.ExDocument ed in result.docs[info])
                        {
                            Console.Write("Score=" + ed.score.ToString());
                            foreach (string s in info.Fields)
                            {
                                Console.Write("\t" + ed.doc.Get(s));
                            }
                            Console.WriteLine();
                            //Response.Write(ISUtils.SupportClass.Document.ToString(ed.doc)+ "<br>");
                            //Console.WriteLine(string.Format("title:{0} \nhistoryName:{1}", doc.Get("id"), doc.Get("historyName")));
                        }
                    }
                }
            }
            public static string GetResult(string szSrc, string color, bool removeHightLight)
            {
                szSrc = szSrc.Replace("</B><B>", "");
                if (removeHightLight)
                {
                    szSrc=szSrc.Replace("<B>", "<font color=\"" + color + "\">");
                    szSrc=szSrc.Replace("</B>", "</font>");
                }
                else
                {
                    szSrc=szSrc.Replace("<B>", "<font color=\"" + color + "\"><B>");
                    szSrc=szSrc.Replace("</B>", "</B></font>");
                }
                return szSrc;
            }
        }
        public class Numerical
        {
            public static int Min( params int[] values)
            {
                int min = int.MaxValue;
                foreach (int n in values)
                    if (n < min)
                        min = n;
                return min;
            }
            public static int Max( params int[] values)
            {
                int max = int.MinValue;
                foreach (int n in values)
                    if (n > max)
                        max = n;
                return max;
            }
            public static int UMin(params int[] values)
            {
                int min = int.MaxValue;
                foreach (int n in values)
                {
                    if (n >= 0 && n < min)
                        min = n;
                }
                return min;
            }
        }
        public class QueryParser
        {
            public static void TableFieldOf(string srcTF,out string table,out string field)
            {
                int pos=srcTF.LastIndexOf('.');
                if (pos < 0)
                {
                    table = "";
                    field = srcTF;
                }
                else
                {
                    table = srcTF.Substring(0, pos);
                    field = srcTF.Substring(pos + 1, srcTF.Length - pos - 1);
                }
            }
            public static List<string> FieldsInQuery(string query)
            {
                List<string> fieldList = new List<string>();
                int nend = query.Trim().ToLower().IndexOf("from") - 1;
                string[] fs = String.Split(query.Trim().Substring(6, nend - 5), ",");
                fieldList.AddRange(fs);
                return fieldList;
            }
            public static List<string> TablesInQuery(string query)
            {
                List<string> tableList = new List<string>();
                string lower = query.ToLower().Replace('\t', ' ');
                int nfrom = lower.IndexOf("from") + 4;
                int nwhere = lower.IndexOf("where") - 1;
                int ngroup = lower.IndexOf("group") - 1;
                int norder = lower.IndexOf("order") - 1;
                int nend = Numerical.UMin(nwhere, ngroup, norder, lower.Length - 1);
                string[] les = String.Split(lower.Substring(nfrom, nend - nfrom + 1)," ,");
                tableList.AddRange(les);
                return tableList;
            }
            public static string TopOneOfQuery(string query)
            {
                List<string> elemList = new List<string>();
                elemList.AddRange(String.Split(query.ToLower(), " \t"));
                if (!elemList.Contains("select") && !elemList.Contains("from"))
                    throw new ArgumentException("query has a bad format.", "query");
                string[] elems = String.Split(query.Replace('\t', ' '), " ");
                StringBuilder ret = new StringBuilder();
                if (elemList.Contains("top"))
                {
                    for (int i = 0; i < elems.Length; i++)
                    {
                        if (i == 2)
                        {
                            ret.Append("1 ");
                        }
                        else
                        {
                            ret.Append(elems[i] + " ");
                        }
                    }
                }
                else
                {
                    ret.Append("select top 1 ");
                    for (int i = 1; i < elems.Length; i++)
                    {
                        ret.Append(elems[i] + " ");
                    }
                }
                ret.Remove(ret.Length - 1, 1);
                return ret.ToString();
            }
            public static string TopOneOfTable(string table)
            {
                return string.Format("select top 1 * from {0}", table);
            }
        }
        //public class Doc
        //{
        //    public static string ToString(Lucene.Net.Documents.Document doc)
        //    {
        //        string ret;
        //        List<Lucene.Net.Documents.Field> fl=doc.GetFields();
        //        foreach(Lucene.Net.Documents.Field f in fl)
        //        {
        //            ret +="\t"+ f.StringValue();
        //        }
        //        return ret;
        //    }
        //}
    }
}
