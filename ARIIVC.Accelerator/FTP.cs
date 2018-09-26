using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace ARIIVC.Accelerator
{
    public class Ftp
    {
        public static bool UploadFile(string ftpAddress, string remotePath, string filePath, string username, string password)
        {
            if (!ftpAddress.StartsWith("ftp://"))
                ftpAddress = "ftp://" + ftpAddress;
            string uri = ftpAddress.TrimEnd('/') + "//" + remotePath.Trim('/') + "/" + Path.GetFileName(filePath);

            //Create FTP request
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);

            request.Credentials = new NetworkCredential(username, password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = true;
            request.Method = WebRequestMethods.Ftp.UploadFile;
            Stream reqStream = null;

            //Upload file
            reqStream = request.GetRequestStream();
            FileStream fs = File.OpenRead(filePath);
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            reqStream.Write(buffer, 0, buffer.Length);
            reqStream.Close();
            return true;

        }

        public static List<string> FileList(string ftpServer, string remotePath, string username, string password)
        {
            string uri = "ftp://" + ftpServer + "//" + remotePath.Trim('/') + "//";
            FtpWebRequest directoryListRequest = (FtpWebRequest)WebRequest.Create(uri);
            directoryListRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            directoryListRequest.Credentials = new NetworkCredential(username, password);

            using (FtpWebResponse directoryListResponse = (FtpWebResponse)directoryListRequest.GetResponse())
            {
                using (StreamReader directoryListResponseReader = new StreamReader(directoryListResponse.GetResponseStream()))
                {
                    string responseString = directoryListResponseReader.ReadToEnd();
                    string[] results = responseString.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    return results.ToList();
                }
            }
        }

        public static void DeleteFiles(string ftpServer, string remotePath, IList<string> files, string username, string password)
        {
            foreach (var file in files)
                DeleteFile(ftpServer, remotePath, file, username, password);
        }

        public static void DeleteFile(string ftpServer, string remotePath, string fileName, string username, string password)
        {
            string uri = "ftp://" + ftpServer + "//" + remotePath.Trim('/') + "//" + fileName;
            FtpWebRequest fileDeleteRequest = (FtpWebRequest)WebRequest.Create(uri);
            fileDeleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            fileDeleteRequest.Credentials = new NetworkCredential(username, password);
            using (var response = (FtpWebResponse)fileDeleteRequest.GetResponse())
            {
                //Logger.Instance.Debug("FTP server file deletion - " + response.StatusDescription);
            }
        }
        public static void DeleteFile(string ftpServer, string fileName, string username, string password)
        {
            string uri = "ftp://" + ftpServer + "//" + fileName;
            FtpWebRequest fileDeleteRequest = (FtpWebRequest)WebRequest.Create(uri);
            fileDeleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            fileDeleteRequest.Credentials = new NetworkCredential(username, password);
            using (var response = (FtpWebResponse)fileDeleteRequest.GetResponse())
            {
                Console.WriteLine("FTP server file deletion - "+ fileName + " "+ response.StatusDescription);
            }
        }


        public static void DownloadFiles(string ftpServer, string remotePath, IList<string> files, string username,
            string password, string downloadPath = "")
        {
            using (WebClient ftpClient = new WebClient())
            {
                ftpClient.Credentials = new NetworkCredential(username, password);
                foreach (var fileName in files)
                {
                    string uri = "ftp://" + ftpServer + "//" + remotePath.Trim('/') + "//" + fileName;
                    //Logger.Instance.Debug("Download file - " + uri);
                    ftpClient.DownloadFile(uri, Path.Combine(downloadPath, fileName));
                }
            }
        }

        public static void DownloadFiles(string ftpServer, IList<string> files, string username,
        string password, string downloadPath = "")
        {
            using (WebClient ftpClient = new WebClient())
            {
                ftpClient.Credentials = new NetworkCredential(username, password);
                foreach (var fileName in files)
                {
                    if (fileName.Contains(".xml"))
                    {
                        string uri = "ftp://" + ftpServer + "//" + fileName;
                        //Logger.Instance.Debug("Download file - " + uri);
                        ftpClient.DownloadFile(uri, Path.Combine(downloadPath, fileName));
                    }
                }
            }
        }

        public static string ReadFile(string ftpServer, string remotePath, string fileName, string username,
            string password)
        {
            string uri = "ftp://" + ftpServer + "//" + remotePath.Trim('/') + "//" + fileName;
            FtpWebRequest fileReadRequest = (FtpWebRequest)WebRequest.Create(uri);
            fileReadRequest.Credentials = new NetworkCredential(username, password);
            using (Stream readerStream = fileReadRequest.GetResponse().GetResponseStream())
            {
                using (TextReader reader = new StreamReader(readerStream))
                {
                    string fileContents = reader.ReadToEnd();
                    return fileContents;
                }
            }
        }

    }
}
