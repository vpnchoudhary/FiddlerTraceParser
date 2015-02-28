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
            System.IO.Compression.ZipFile.ExtractToDirectory(args[2], traceDirectory.AbsolutePath);
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
            string T1="", T2="", NetworkLetancy=null, adLoadTime=null;
            HtmlDocument doc = web.Load(traceDirectory.AbsolutePath + @"\_index.htm");

            //determine location of "Host" column
            int indexHost = 0;
            foreach (HtmlNode nd in doc.DocumentNode.SelectNodes("//html/body/table/thead/tr/th"))
            {
                if(nd.InnerText == "Host")
                {
                    break;
                }
                indexHost++;
            }
            foreach(HtmlNode node in doc.DocumentNode.SelectNodes("//html/body/table/tbody/tr"))
            {
                //find session
                if (!startofSession)
                {
                    if (node.ChildNodes[indexHost].InnerText == baseUrl.Host)
                    {
                        startofSession = true;
                        Console.WriteLine("start of session: " + node.ChildNodes[4].InnerText);
                        //open matrics file

                        HtmlDocument met = web.Load(traceDirectory.AbsolutePath + @"\" + node.ChildNodes[0].ChildNodes[4].Attributes[0].Value);
                        HtmlNode xmlNode = met.DocumentNode.SelectSingleNode("/session/sessiontimers");
                        T1 = xmlNode.Attributes["ClientBeginRequest"].Value;
                        NetworkLetancy = Convert.ToDateTime(xmlNode.Attributes["ClientDoneResponse"].Value).Subtract(Convert.ToDateTime(T1)).Milliseconds.ToString();
                        

                    }
                }
                else
                {
                    //ALERT: (specific condition):-> node.ChildNodes[indexHost + 1].InnerText.Contains("ADPT12")
                    // the second url with contain ADPT12 for MSN.com ad url testing
                    if (node.ChildNodes[indexHost].InnerText == secondUrl.Host && node.ChildNodes[indexHost + 1].InnerText.Contains("ADPT12"))
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
                            string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"", T1, NetworkLetancy,
                            time2.Subtract(time1).Milliseconds, Convert.ToDateTime(xmlNode.Attributes["ClientDoneResponse"].Value).Subtract(time2).Milliseconds));

                        Console.WriteLine("NetworkLetancy: {0}  AdCallStart: {1} AdCallComplete: {2}", NetworkLetancy, time2.Subtract(time1).Milliseconds, Convert.ToDateTime(xmlNode.Attributes["ClientDoneResponse"].Value).Subtract(time2).Milliseconds);
                    }
                    
                }
            }
            //Result is stored in trace Directory
            System.IO.File.WriteAllText(traceDirectory.AbsolutePath + @"/result.csv", csvExport.ToString());
        }

    }
}
