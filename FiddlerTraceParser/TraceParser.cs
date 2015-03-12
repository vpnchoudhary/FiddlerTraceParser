using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using HtmlAgilityPack;


namespace FiddlerTraceParser
{

    //This class parses a fiddler trace for multible web session's in which we are interested in determining the relative time between two urls:
    //base and second url.
    
    
    
    class TraceParser
    {
        private Uri baseUrl;
        public Uri BaseUrl { get { return baseUrl; } set { baseUrl = value; } }
        
        private Uri secondUrl;
        public Uri SecondUrl { get { return secondUrl; } set { secondUrl = value; } }

        private Uri traceDirectory;
        public Uri TraceDirectory { get { return traceDirectory; } set { traceDirectory = value; } }

        public TraceParser(string[] args)
        {
            baseUrl = new Uri(args[0], UriKind.Absolute);
            secondUrl = new Uri(args[1], UriKind.Absolute);
            traceDirectory = new Uri(args[2].Replace(".saz", ""), UriKind.RelativeOrAbsolute);
            try
            {
                if(Directory.Exists(traceDirectory.AbsolutePath))
                {
                    Directory.Delete(traceDirectory.AbsolutePath, true);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory(args[2], traceDirectory.AbsolutePath);
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //<ToDo>check that traceDirectory is valid and contain _index.html file
            LoadTrace();
        }

        private void LoadTrace()
        {
            StringBuilder csvExport = new StringBuilder();
            csvExport.AppendLine("\"CallStartTime\",\"Overall_ElapsedTime\",\"AdCallStart\",\"AdCallOverallElapsedTime\""); 
            //Debugger.Break();
            HtmlWeb web = new HtmlWeb();
            bool startofSession = false;
            string T1="", T2="", NetworkLetancy=null;
            HtmlDocument doc = web.Load(traceDirectory.AbsolutePath + @"\_index.htm");

            //determine location of "Host" & URL columns of Fiddler
            int indexHost = 0;
            int indexURL = 0;
            int i = 0;
            foreach (HtmlNode nd in doc.DocumentNode.SelectNodes("//html/body/table/thead/tr/th"))
            {
                if(nd.InnerText == "Host")
                {
                    indexHost = i;
                }
                else if(nd.InnerText == "URL")
                {
                    indexURL = i;
                }
                i++;
            }
            foreach(HtmlNode node in doc.DocumentNode.SelectNodes("//html/body/table/tbody/tr"))
            {
                //find session
                if (!startofSession)
                {
                    if (node.ChildNodes[indexHost].InnerText.Equals(baseUrl.Host, StringComparison.CurrentCultureIgnoreCase) && node.ChildNodes[indexURL].InnerText.Equals(baseUrl.PathAndQuery, StringComparison.CurrentCultureIgnoreCase))
                    {
                        startofSession = true;
                        Console.WriteLine("start of session: " + node.ChildNodes[4].InnerText);
                        //open matrics file

                        HtmlDocument met = web.Load(traceDirectory.AbsolutePath + @"\" + node.ChildNodes[0].ChildNodes[4].Attributes[0].Value);
                        HtmlNode xmlNode = met.DocumentNode.SelectSingleNode("/session/sessiontimers");
                        T1 = xmlNode.Attributes["ClientBeginRequest"].Value;
                        NetworkLetancy = Convert.ToDateTime(xmlNode.Attributes["ClientDoneResponse"].Value).Subtract(Convert.ToDateTime(T1)).TotalSeconds.ToString();
                        

                    }
                }
                else
                {
                    if (node.ChildNodes[indexHost].InnerText.Equals(secondUrl.Host, StringComparison.CurrentCultureIgnoreCase) && node.ChildNodes[indexURL].InnerText.ToLower().Contains(secondUrl.PathAndQuery.ToLower()))
                    {
                        startofSession = false;
                        Console.WriteLine("Ads url " + node.ChildNodes[4].InnerText);
                        HtmlDocument met = web.Load(traceDirectory.AbsolutePath + @"\" + node.ChildNodes[0].ChildNodes[4].Attributes[0].Value);
                        HtmlNode xmlNode = met.DocumentNode.SelectSingleNode("/session/sessiontimers");
                        T2 = xmlNode.Attributes["ClientBeginRequest"].Value;
                        //adLoadTime = node.ChildNodes[12].InnerText;
                        DateTime time1 = Convert.ToDateTime(T1);
                        DateTime time2 = Convert.ToDateTime(T2);
                        csvExport.AppendLine(
                            string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"", time1.ToLongTimeString(), NetworkLetancy,
                            time2.Subtract(time1).TotalSeconds, Convert.ToDateTime(xmlNode.Attributes["ClientDoneResponse"].Value).Subtract(time2).TotalSeconds));

                        Console.WriteLine("NetworkLetancy: {0}  AdCallStart: {1} AdCallComplete: {2}", NetworkLetancy, time2.Subtract(time1).TotalSeconds, Convert.ToDateTime(xmlNode.Attributes["ClientDoneResponse"].Value).Subtract(time2).TotalSeconds);
                    }
                    
                }
            }
            //Result is stored in trace Directory
            System.IO.File.WriteAllText(traceDirectory.AbsolutePath + @"/result.csv", csvExport.ToString());
        }

    }
}
