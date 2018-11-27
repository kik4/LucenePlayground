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
using Lucene.Net.QueryParsers.Classic;

namespace ConsoleApp2
{
    class Program
    {
        const string INDEXDIR = @"c:\temp\ldn-index";　//インデックスの場所

        static void Main(string[] args)
        {
            FSDirectory dir = FSDirectory.Open(INDEXDIR);

            // テキストの解析方法（アナライザー）を定義
            JapaneseAnalyzer analyzer = new JapaneseAnalyzer(LuceneVersion.LUCENE_48);
            using (IndexReader reader = DirectoryReader.Open(dir))
            {

                IndexSearcher searcher = new IndexSearcher(reader);

                //開始時間の取得
                DateTime startDt = DateTime.Now;

                QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "contents", analyzer);

                //var docs = searcher.Search(parser.Parse("0"), 10);
                var docs = searcher.Search(new FuzzyQuery(new Term("0"), 2), 10);
                Console.WriteLine(docs.TotalHits);
                ScoreDoc[] hits = docs.ScoreDocs;

                System.Console.WriteLine("Found " + hits.Length + " hits.");
                for (int i = 0; i < hits.Length; ++i)
                {
                    int docId = hits[i].Doc;
                    Document d = searcher.Doc(docId);
                    System.Console.WriteLine((i + 1) + ". " + d.Get("path") + "\t" + d.Get("modified") + "\t" + hits[i].Score);
                }

                //終了時間の取得
                DateTime endDt = DateTime.Now;
                System.Console.WriteLine("{0}タイマ刻み数かかりました", (endDt - startDt).Ticks);
            }

            Console.ReadKey();
        }
    }
}
