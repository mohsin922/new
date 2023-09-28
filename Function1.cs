using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

public static class XmlToCsvConverter
{
    [FunctionName("ConvertXmlToCsv")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = new StreamReader(req.Body).ReadToEnd();

        // Load the XML content
        XDocument xDocument;
        using (XmlReader xmlReader = XmlReader.Create(new StringReader(requestBody)))
        {
            xDocument = XDocument.Load(xmlReader);
        }

        StringBuilder csvContent = new StringBuilder();
        ConvertToCsv(xDocument.Root, csvContent);

        // Create a CSV response
        byte[] csvBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
        return new FileContentResult(csvBytes, "text/csv")
        {
            FileDownloadName = "converted_data.csv"
        };
    }

    private static void ConvertToCsv(XElement element, StringBuilder csvContent, string parentPath = "")
    {
        if (element.HasElements)
        {
            foreach (XElement childElement in element.Elements())
            {
                string currentPath = string.IsNullOrEmpty(parentPath)
                    ? childElement.Name.LocalName
                    : parentPath + "." + childElement.Name.LocalName;

                ConvertToCsv(childElement, csvContent, currentPath);
            }
        }
        else
        {
            string header = string.IsNullOrEmpty(parentPath)
                ? element.Name.LocalName
                : parentPath + "." + element.Name.LocalName;

            AddCsvValue(csvContent, header, element.Value);
        }
    }

    private static void AddCsvValue(StringBuilder csvContent, string header, string value)
    {
        csvContent.AppendLine($"\"{header}\",\"{value.Replace("\"", "\"\"")}\"");
    }
}
