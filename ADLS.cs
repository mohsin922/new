using System;
using System.IO;
using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;

using Azure.Storage.Blobs;


namespace ADLS
{
    public static class ADLS
    {
        //public static bool UploadFileAsync(Stream dataStream, string path, string message)
        public static bool UploadFileAsync(Stream dataStream, string path, string message)
        {
            bool flag = false;
            try
            {

                //Console.WriteLine("C# HTTP trigger function processed a request.");
                string blogStorageConnection = "DefaultEndpointsProtocol=https;AccountName=icapdiprodadls;AccountKey=bE2J8nK6RlfpUVyD3i8zGlkF0bJBpORYzaacMBBqpPfgQIsd6DsB1+v5n9f0og6972CY7rQ3tPWf+ASthnmR7A==;EndpointSuffix=core.windows.net";
                string blogStorageContainer = "icapdifs";

                message += blogStorageConnection;
                message += blogStorageContainer;

                var containerClient = new BlobContainerClient(blogStorageConnection, blogStorageContainer);
                {
                    string newPath = string.Join("/", path);

                    var fileBlobClient = containerClient.GetBlobClient(newPath);

                    dataStream.Position = 0;
                    if (fileBlobClient.Exists())
                    {
                        Console.WriteLine("The blob exists!");
                        //fileBlobClient.Delete();
                        flag = true;
                    }
                    else
                    {
                        var uploadInfo = fileBlobClient.Upload(dataStream);


                        var uploadResposne = uploadInfo.GetRawResponse();

                        if (uploadResposne.IsError == false && (uploadResposne.Status == 200 || uploadResposne.Status == 201))
                        {
                            Console.WriteLine("File uploaded with name : " + newPath);
                            message += " File Uploaded with the path : " + newPath;
                            flag = true;
                        }
                        else
                        {
                            Console.WriteLine("Failed to upload content!");
                            message += " Failed to upload file!!!";
                            flag = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to upload content!: " + ex.Message);
                message += " " + ex.Message;
                flag = false;
            }
            return flag;
        }

    }
}
