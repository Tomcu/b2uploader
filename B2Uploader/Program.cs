﻿using B2Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace B2Uploader
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Count() != 3)
            {
                Console.WriteLine("Need accountID, AppKey and Folder to upload");
                return;
            }

            if (!Directory.Exists(args[2]))
            {
                Console.WriteLine("Directory to upload MUST EXIST!");
                return;
            }

            var auth = AuthorizeUser(args[0], args[1]);
            var buckets = ListBuckets(new ListBucketsRequest() { accountId = auth.accountId }, auth.authorizationToken, auth.apiUrl);

            var bucket = buckets.buckets.First();

            foreach(string s in Directory.GetFiles(args[2]))
            {
                var uploadURL = GetUploadURL(new GetUploadURLRequest { bucketId = bucket.bucketId }, auth.apiUrl, auth.authorizationToken);
                var response = UploadFile(uploadURL.authorizationToken, "b2/x-auto", s, uploadURL.uploadUrl);
            }



        }

        static AuthorizeResponse AuthorizeUser(string accountId, string applicationKey)
        {            
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.backblaze.com/b2api/v1/b2_authorize_account");
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(accountId + ":" + applicationKey));
            webRequest.Headers.Add("Authorization", "Basic " + credentials);
            webRequest.ContentType = "application/json; charset=utf-8";
            WebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            return JsonConvert.DeserializeObject<AuthorizeResponse>(responseString);
        }

        static ListBucketsResponse ListBuckets(ListBucketsRequest request, string authToken, string apiURL)
        {            
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(apiURL + "/b2api/v1/b2_list_buckets");
            string body = JsonConvert.SerializeObject(request);
            var data = Encoding.UTF8.GetBytes(body);
            webRequest.Method = "POST";
            webRequest.Headers.Add("Authorization", authToken);
            webRequest.ContentType = "application/json; charset=utf-8";
            webRequest.ContentLength = data.Length;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            WebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();

            return JsonConvert.DeserializeObject<ListBucketsResponse>(responseString);
        }

        static GetUploadURLResponse GetUploadURL(GetUploadURLRequest request, string apiUrl, string authToken)
        {          
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(apiUrl + "/b2api/v1/b2_get_upload_url");
            string body = JsonConvert.SerializeObject(request);
            var data = Encoding.UTF8.GetBytes(body);
            webRequest.Method = "POST";
            webRequest.Headers.Add("Authorization", authToken);
            webRequest.ContentType = "application/json; charset=utf-8";
            webRequest.ContentLength = data.Length;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            WebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            return JsonConvert.DeserializeObject<GetUploadURLResponse>(responseString);
        }

        static UploadFileResponse UploadFile(string authToken, string contentType, string filePath, string uploadUrl)
        {

            byte[] bytes = File.ReadAllBytes(filePath);

            String sha1 = GetSha1(bytes);
            
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uploadUrl);
            webRequest.Method = "POST";
            webRequest.Headers.Add("Authorization", authToken);
            webRequest.Headers.Add("X-Bz-File-Name", filePath.Replace('\\','_'));
            webRequest.Headers.Add("X-Bz-Content-Sha1", sha1);
            webRequest.ContentType = contentType;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
            WebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            var resp = JsonConvert.DeserializeObject<UploadFileResponse>(responseString);

            if(resp.contentSha1 == sha1)
            {
                Console.WriteLine(responseString);
                return resp;
            }
            else
            {
                //something went wrong!
                return null;
            }
        }

        private static string GetSha1(byte[] bytes)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }

    
}
