using WinSCP;

// Setup session options
SessionOptions sessionOptions = new SessionOptions
{
    Protocol = Protocol.Sftp,
    HostName = "sftp.provana.com",
    UserName = "RGS",
    Password = "Lk31JpQZ",
    GiveUpSecurityAndAcceptAnySshHostKey = true
};

using (Session session = new Session())
{
    // Connect to SFTP server
    session.Open(sessionOptions);

    // Download files
    TransferOptions transferOptions = new TransferOptions();
    transferOptions.TransferMode = TransferMode.Binary;

    RemoteDirectoryInfo directoryInfo = session.ListDirectory("/");
    foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
    {
        if (!fileInfo.IsDirectory)
        {
            TransferOperationResult transferResult;
            transferResult = session.GetFiles(fileInfo.FullName, "C:\\Users\\mohsinzahoor.peer\\Videos\\SftpTesting\\", false, transferOptions);

            // Throw on any error
            transferResult.Check();

            // Print results
            foreach (TransferEventArgs transfer in transferResult.Transfers)
            {
                Console.WriteLine("Download of {0} succeeded", transfer.FileName);
            }
        }
    }
}