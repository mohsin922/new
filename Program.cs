
using Ionic.Zip;

using System.IO;

 

string zipFilePath = @"C:\Users\mohsinzahoor.peer\Videos\Test.zip";

string password = "Testing@123";

string responseMessage = ""; // Initialize the response message variable

using (ZipFile zipFile = new ZipFile(zipFilePath))

{

    zipFile.Password = password;



    foreach (ZipEntry entry in zipFile)

    {

        if (entry.UsesEncryption )

        {
            try

            {

                entry.ExtractWithPassword(@"C:\Users\mohsinzahoor.peer\Videos", password);

                responseMessage += $"Password matched for {entry.FileName}\n";

            }

            catch (Exception ex)

            {

                responseMessage += $"Password didn't match for {entry.FileName}. Extraction failed with error: {ex.Message}\n";

            }

            // Extract the entry with the password

           // entry.ExtractWithPassword(@"C:\Users\mohsinzahoor.peer\Videos", password);

        }

        else

        {
            entry.Extract(@"C:\Users\mohsinzahoor.peer\Videos", ExtractExistingFileAction.OverwriteSilently);

            responseMessage += $"No password required for {entry.FileName}\n";

            // Extract the entry without a password

           // entry.Extract(@"C:\Users\mohsinzahoor.peer\Videos", ExtractExistingFileAction.OverwriteSilently);

        }

    }
    Console.WriteLine(responseMessage);

}






