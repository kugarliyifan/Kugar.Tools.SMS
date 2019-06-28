using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Kugar.Tools.SMS
{
    /// <summary>
    /// 阿里云短信
    /// </summary>
    public class AliyunSmsSender
    {
        private string RegionId = "cn-hangzhou";
        private string Version = "2017-05-25";
        private string Action = "SendSms";
        private string Format = "JSON";
        private string Domain = "dysmsapi.aliyuncs.com";

        private int MaxRetryNumber = 3;//重传次数
        private bool AutoRetry = true;//是否重传
        private const string SEPARATOR = "&";
        private int TimeoutInMilliSeconds = 100000;

        private string AccessKeyId;
        private string AccessKeySecret;

        public AliyunSmsSender(string accessKeyId, string accessKeySecret)
        {
            this.AccessKeyId = accessKeyId;
            this.AccessKeySecret = accessKeySecret;
        }

        /// <summary>
        /// 发送短信
        /// </summary>
        public async Task<(bool success, string msg)> Send(string mobile,string signature,string templetKey, IDictionary<string, string> data,string outID)
        {
            var paramers = new Dictionary<string, string>
            {
                { "PhoneNumbers",mobile },
                { "SignName", signature },
                { "TemplateCode", templetKey },
                { "TemplateParam", JsonConvert.SerializeObject(data) },
                { "OutId", outID },
                { "AccessKeyId", AccessKeyId }
            };

            try
            {
                string url = GetSignUrl(paramers, AccessKeySecret);

                int retryTimes = 1;
                var reply = await HttpGetAsync(url);
                while (500 <= reply.StatusCode && AutoRetry && retryTimes < MaxRetryNumber)
                {
                    url = GetSignUrl(paramers, AccessKeySecret);
                    reply = await HttpGetAsync(url);
                    retryTimes++;
                }

                if (!string.IsNullOrEmpty(reply.response))
                {
                    var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(reply.response);
                    if (res != null && res.ContainsKey("Code") && "OK".Equals(res["Code"]))
                    {
                        return (true, msg: reply.response);
                    }
                }

                return (false, msg: reply.response);
            }
            catch (Exception ex)
            {
                return (false, msg: ex.Message);
            }
        }

        private string GetSignUrl(Dictionary<string, string> parameters, string accessSecret)
        {
            var imutableMap = new Dictionary<string, string>(parameters)
            {
                { "Timestamp", FormatIso8601Date(DateTime.Now) },
                { "SignatureMethod", "HMAC-SHA1" },
                { "SignatureVersion", "1.0" },
                { "SignatureNonce", Guid.NewGuid().ToString() },
                { "Action", Action },
                { "Version", Version },
                { "Format", Format },
                { "RegionId", RegionId }
            };

            IDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>(imutableMap, StringComparer.Ordinal);
            StringBuilder canonicalizedQueryString = new StringBuilder();
            foreach (var p in sortedDictionary)
            {
                canonicalizedQueryString.Append("&")
                .Append(PercentEncode(p.Key)).Append("=")
                .Append(PercentEncode(p.Value));
            }

            StringBuilder stringToSign = new StringBuilder();
            stringToSign.Append("GET");
            stringToSign.Append(SEPARATOR);
            stringToSign.Append(PercentEncode("/"));
            stringToSign.Append(SEPARATOR);
            stringToSign.Append(PercentEncode(canonicalizedQueryString.ToString().Substring(1)));

            string signature = SignString(stringToSign.ToString(), accessSecret + "&");

            imutableMap.Add("Signature", signature);

            return ComposeUrl(Domain, imutableMap);
        }

        private static string FormatIso8601Date(DateTime date)
        {
            return date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.CreateSpecificCulture("en-US"));
        }

        /// <summary>
        /// 签名
        /// </summary>
        public static string SignString(string source, string accessSecret)
        {
            using (var algorithm = new HMACSHA1(Encoding.UTF8.GetBytes(accessSecret.ToCharArray())))
            {
                return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(source.ToCharArray())));
            }
        }

        private static string ComposeUrl(string endpoint, Dictionary<String, String> parameters)
        {
            StringBuilder urlBuilder = new StringBuilder("");
            urlBuilder.Append("http://").Append(endpoint);
            if (-1 == urlBuilder.ToString().IndexOf("?"))
            {
                urlBuilder.Append("/?");
            }
            string query = ConcatQueryString(parameters);
            return urlBuilder.Append(query).ToString();
        }

        private static string ConcatQueryString(Dictionary<string, string> parameters)
        {
            if (null == parameters)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();

            foreach (var entry in parameters)
            {
                String key = entry.Key;
                String val = entry.Value;

                sb.Append(HttpUtility.UrlEncode(key, Encoding.UTF8));
                if (val != null)
                {
                    sb.Append("=").Append(HttpUtility.UrlEncode(val, Encoding.UTF8));
                }
                sb.Append("&");
            }

            int strIndex = sb.Length;
            if (parameters.Count > 0)
                sb.Remove(strIndex - 1, 1);

            return sb.ToString();
        }

        public static string PercentEncode(string value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
            byte[] bytes = Encoding.GetEncoding("UTF-8").GetBytes(value);
            foreach (char c in bytes)
            {
                if (text.IndexOf(c) >= 0)
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    stringBuilder.Append("%").Append(
                        string.Format(CultureInfo.InvariantCulture, "{0:X2}", (int)c));
                }
            }
            return stringBuilder.ToString();
        }

        private async Task<(int StatusCode, string response)> HttpGetAsync(string url)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = null,
                AutomaticDecompression = DecompressionMethods.GZip
            };

            using (var http = new HttpClient(handler))
            {
                http.Timeout = new TimeSpan(TimeSpan.TicksPerMillisecond * TimeoutInMilliSeconds);
                HttpResponseMessage response = await http.GetAsync(url);
                return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
