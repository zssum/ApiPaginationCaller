using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ApiPaginationCaller
{
    class Program
    {
        static void Main(string[] args)
        {
            string data = string.Empty;
            string listLocation = ConfigurationManager.AppSettings["listLocation"];
            string[] listLocationLevels = listLocation.Split('.');
            string baseApi = ConfigurationManager.AppSettings["baseApi"];
            string pageQueryStringKey = ConfigurationManager.AppSettings["pageQueryStringKey"];
            int delayMilliSeconds = Int32.Parse(ConfigurationManager.AppSettings["delayMilliSeconds"]);

            JArray finalDataResult = new JArray();
            DataTable csvTable = new DataTable();

            int pageNumber = 0;
            string url = baseApi + "&"+ pageQueryStringKey + "=" + pageNumber;
            data = GetJsonString(url);
            JObject obj = JObject.Parse(data);
            int pages = (int)obj["meta"]["totalPages"];


            for (int p = 0; p < pages; p++)
            {
                Thread.Sleep(delayMilliSeconds);
                if (p != 0) //Skip calling api for the first round
                {
                    url = baseApi + "&" + pageQueryStringKey + "=" + p;
                    data = GetJsonString(url);
                    obj = JObject.Parse(data);
                }
                
                //Get Array Object with flexible access from configuration
                for (int i = 0; i < listLocationLevels.Length; i++)
                {
                    if (i == listLocationLevels.Length - 1)
                        finalDataResult.Merge((JArray)obj[listLocationLevels[i]]);
                    else
                        obj = (JObject)obj[listLocationLevels[i]];
                }

                if (p == 0) //Initialize dataTable
                {
                    JObject objectForHeader = (JObject)finalDataResult[0];
                    foreach (var s in objectForHeader.Properties().Select(prop => prop.Name).ToList())
                    {
                        csvTable.Columns.Add(s);
                    }
                }
            }

            //Add into DataTable data from finalDataResult
            for (int i = 0; i < finalDataResult.Count; i++)
            {
                DataRow row = csvTable.NewRow();
                foreach (var line in finalDataResult[i].Children())
                {
                    //row[line.it];
                    row[((JProperty)line).Name] = line.First().ToString();
                }
                csvTable.Rows.Add(row);
            }

            //Write CSV
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = csvTable.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in csvTable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText($"{DateTime.Now.Ticks}.csv", sb.ToString());
            Console.WriteLine("Successfully completed.");
            Console.ReadLine();
        }

        public static string GetJsonString(string url)
        {
            Console.WriteLine($"Calling {url}");
            string result = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
        
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

 
    }
}
