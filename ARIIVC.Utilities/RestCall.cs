using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ARIIVC.Utilities
{
    public class RestCall
    {
        public string Url { get; set; }
        public string Put(string data)
        {
            string backstr = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(data);
                req.Method = "PUT";
                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0} : {1}", data, postExcep.StackTrace);
                return backstr;
            }
        }

        public string Post(Dictionary<string, object> dictionary)
        {
            string mongoposttext = JsonConvert.SerializeObject(dictionary);
            return Post(mongoposttext);
        }

        public string Post(string data)
        {
            string backstr = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                byte[] requestBytes = System.Text.Encoding.Default.GetBytes(data);
                req.Method = "POST";
                
                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                
                //req.KeepAlive = true;
                //req.AllowAutoRedirect = true;
                req.ContentLength = requestBytes.Length;                
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0} : {1}", data, postExcep.StackTrace);
                return backstr;
            }
        }

        public string Post(string data, string AuthenticationHeader)
        {
            string backstr = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                byte[] requestBytes = System.Text.Encoding.Default.GetBytes(data);
                req.Method = "POST";

                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.Headers.Add("Authentication", AuthenticationHeader);
                //req.KeepAlive = true;
                //req.AllowAutoRedirect = true;
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0} : {1}", data, postExcep.StackTrace);
                return backstr;
            }
        }

        public string Delete(string data, string AuthenticationHeader)
        {
            string backstr = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                byte[] requestBytes = System.Text.Encoding.Default.GetBytes(data);
                req.Method = "DELETE";

                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.Headers.Add("Authentication", AuthenticationHeader);
                //req.KeepAlive = true;
                //req.AllowAutoRedirect = true;
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0} : {1}", data, postExcep.StackTrace);
                return backstr;
            }
        }

        public string PostCSDT(string data)
        {
            string backstr = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url + "?data=" + data);
                byte[] requestBytes = System.Text.Encoding.Default.GetBytes(data);
                req.Method = "POST";
                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0} : {1}", data, postExcep.StackTrace);
                return backstr;
            }
        }

        public void Post()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                req.Method = "POST";
                req.ProtocolVersion = HttpVersion.Version11;
                req.Accept = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                req.GetResponse().Dispose();
                HttpWebResponse res = req.GetResponse() as HttpWebResponse;
                res.Close();
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0}, {1}", postExcep.StackTrace, postExcep.Message);
            }
        }

        public string Get()
        {
            HttpWebRequest rel_request = WebRequest.Create(Url) as HttpWebRequest;
            rel_request.Method = "GET";
            HttpWebResponse rel_response = rel_request.GetResponse() as HttpWebResponse;
            StreamReader rel_stream = new StreamReader(rel_response.GetResponseStream());
            string rel_output = rel_stream.ReadToEnd();
            rel_stream.Close();
            rel_response.Close();
            return rel_output;
        }

        public string Get(string AuthenticationHeader)
        {
            HttpWebRequest rel_request = WebRequest.Create(Url) as HttpWebRequest;
            rel_request.Method = "GET";
            rel_request.Headers.Add("Authentication", AuthenticationHeader);
            HttpWebResponse rel_response = rel_request.GetResponse() as HttpWebResponse;
            StreamReader rel_stream = new StreamReader(rel_response.GetResponseStream());
            string rel_output = rel_stream.ReadToEnd();
            rel_stream.Close();
            rel_response.Close();
            return rel_output;
        }

        public void Put()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                req.Method = "PUT";
                req.ProtocolVersion = HttpVersion.Version11;
                req.Accept = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                req.GetResponse().Dispose();
                HttpWebResponse res = req.GetResponse() as HttpWebResponse;
                res.Close();
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0}", postExcep.StackTrace);
            }
        }


    }
}
