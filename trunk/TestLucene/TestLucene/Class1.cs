using System;
using System.Runtime.InteropServices;
using seg.result;
using Lucene.Net.Analysis;

namespace ConsoleApplication1
{
	/// <summary>
	/// �����Դ http://www.lietu.com
	/// </summary>
	class Class1
	{
		/// <summary>
		/// Ӧ�ó��������ڵ㡣
		/// </summary>
		[DllImport("Kernel32.DLL", SetLastError=true)] 
		public static extern bool SetEnvironmentVariable(string lpName, string lpValue); 

		[STAThread]
        //static void Main(string[] args)
        //{
        //    SetEnvironmentVariable( "dic.dir", "F:/lwh/TestLucene/TestLucene/dic");
        //    //
        //    // TODO: �ڴ˴���Ӵ���������Ӧ�ó���
        //    //
        //    testCnAnalyzer();
        //    System.Console.Read();
        //}
		
		public static void testCnAnalyzer() 
		{
			System.IO.TextReader input;

            try
            {
                CnTokenizer.makeTag = true;
            }
            //catch()
            //{
            //}
            finally
            {
                string sentence = "�������������9�²μ�����ɼ���е�30�������ΰ��ɾʹ󽱻�";

                input = new System.IO.StringReader(sentence);
                TokenStream tokenizer = new seg.result.CnTokenizer(input);

                for (Token t = tokenizer.Next(); t != null; t = tokenizer.Next())
                {
                    System.Console.WriteLine(t.TermText() + " " + t.StartOffset() + " "
                        + t.EndOffset() + " " + t.Type());
                }
            }
		}
	}
}
