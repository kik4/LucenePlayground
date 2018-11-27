using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Lucene.Net.Analysis.Ja;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace LdNetIndex
{
    class Program
    {
        const string TARGETDIR = @"C:\temp\testdir"; //ソースとなるフォルダー
        const string INDEXDIR = @"c:\temp\ldn-index";　//インデックスの場所

        static void Main(string[] args)
        {
            DirectoryInfo sourceDirectory = new DirectoryInfo(TARGETDIR);
            FSDirectory dir = FSDirectory.Open(INDEXDIR);

            // テキストの解析方法（アナライザー）を定義
            JapaneseAnalyzer analyzer = new JapaneseAnalyzer(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
            config.OpenMode = OpenMode.CREATE_OR_APPEND;

            //開始時間の取得
            DateTime startDt = DateTime.Now;

            using (IndexWriter writer = new IndexWriter(dir, config))
            {
                IndexDocs(writer, sourceDirectory);
            }

            //終了時間の取得
            DateTime endDt = DateTime.Now;
            System.Console.WriteLine("{0}タイマ刻み数かかりました", (endDt - startDt).Ticks);

            Console.ReadKey();
        }

        internal static void IndexDocs(IndexWriter writer, DirectoryInfo directoryInfo)
        {
            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                IndexDocs(writer, dirInfo);
            }
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                IndexDocs(writer, fileInfo);
            }
        }

        internal static void IndexDocs(IndexWriter writer, FileInfo file)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                // make a new, empty document
                Document doc = new Document();

                // Add the path of the file as a field named "path".  Use a
                // field that is indexed (i.e. searchable), but don't tokenize 
                // the field into separate words and don't index term frequency
                // or positional information:
                Field pathField = new StringField("path", file.FullName, Field.Store.YES);
                doc.Add(pathField);

                // Add the last modified date of the file a field named "modified".
                // Use a LongField that is indexed (i.e. efficiently filterable with
                // NumericRangeFilter).  This indexes to milli-second resolution, which
                // is often too fine.  You could instead create a number based on
                // year/month/day/hour/minutes/seconds, down the resolution you require.
                // For example the long value 2011021714 would mean
                // February 17, 2011, 2-3 PM.
                doc.Add(new Int64Field("modified", file.LastWriteTimeUtc.Ticks, Field.Store.NO));

                // Add the contents of the file to a field named "contents".  Specify a Reader,
                // so that the text of the file is tokenized and indexed, but not stored.
                // Note that FileReader expects the file to be in UTF-8 encoding.
                // If that's not the case searching for special characters will fail.
                doc.Add(new TextField("contents", new StreamReader(fs, Encoding.UTF8)));

                if (writer.Config.OpenMode == OpenMode.CREATE)
                {
                    // New index, so we just add the document (no old document can be there):
                    Console.WriteLine("adding " + file);
                    writer.AddDocument(doc);
                }
                else
                {
                    // Existing index (an old copy of this document may have been indexed) so 
                    // we use updateDocument instead to replace the old one matching the exact 
                    // path, if present:
                    Console.WriteLine("updating " + file);
                    writer.UpdateDocument(new Term("path", file.FullName), doc);
                }
            }

        }

        /*************************
         * HTMLの解析
         *************************/
        private static void HTMLParse(ref string title, ref string content, ref string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            string text = sr.ReadToEnd();
            //正規表現パターンとオプションを指定してRegexオブジェクトを作成 
            Regex rTitle = new Regex("<title[^>]*>(.*?)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rPre = new Regex("<body[^>]*>(.*?)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            //TextBox1.Text内で正規表現と一致する対象をすべて検索 
            MatchCollection mcTitle = rTitle.Matches(text);
            MatchCollection mcPre = rPre.Matches(text);

            foreach (Match m in mcTitle)
            {
                title = m.Groups[1].Value;
            }
            foreach (Match m in mcPre)
            {
                content = m.Groups[1].Value;
            }
        }
    }
}