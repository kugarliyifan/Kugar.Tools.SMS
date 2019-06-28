using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Kugar.Core.ExtMethod;
using Kugar.Core.Network;

namespace Kugar.Tools.SMS
{
    public class EachweSmsSender
    {
        private string _loginName;
        private string _passWord;

        public EachweSmsSender(string loginName,string password)
        {
            _loginName = loginName;
            _passWord = password;
        }

        public string Name { get { return "Eachwe"; } }

        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="mobiles"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<(bool isSuccess,int statusCode)> Send(string[] mobiles, string message)
        {
            var url = "http://sms2.eachwe.com/api.php";

            string method = "sendsms";
            //string loginName = _config.AppSettings.GetValueByName<string>("Eachwe_LoginName");
            //string passWord = _config.AppSettings.GetValueByName<string>("Eachwe_PassWord");

            //var datas = new Dictionary<string, string>();

            //datas.Add("username", _loginName);
            //datas.Add("password", _passWord);
            //datas.Add("method", method);
            //datas.Add("mobile", mobiles.JoinToString(','));
            //datas.Add("msg", message);

            var respData =await WebHelper.Create(url)
                .Encoding(Encoding.GetEncoding("gb2312"))
                .ContentType(WebHelper.ContentTypeEnum.FormUrlencoded)
                .SetParamter("method", "sendsms")
                .SetParamter("username", _loginName)
                .SetParamter("password", _passWord)
                .SetParamter("mobile", mobiles.JoinToString(','))
                .SetParamter("msg", message)
                .Post_StringAsync();
                ;

            XmlDocument doc = new XmlDocument();
            if (!doc.SafeLoadXML(respData))
            {
                return (false,-1);
            }

            XmlElement rootElment = doc.DocumentElement;
            if (rootElment != null)
            {
                XmlNode errorXmlNode = rootElment.ChildNodes[0];
                XmlNode messageXmlNode = rootElment.ChildNodes[1];
                var statusCode = errorXmlNode.InnerText.ToInt();

                if (errorXmlNode != null && !string.IsNullOrEmpty(errorXmlNode.InnerText) && errorXmlNode.InnerText == "0")
                {
                    return (true,statusCode);
                }

                if (errorXmlNode != null && messageXmlNode != null && !string.IsNullOrEmpty(errorXmlNode.InnerText) && errorXmlNode.InnerText != "0")
                {
                    return (false,statusCode);
                }
            }


            //string para = string.Format("username={0}&password={1}&method={2}&mobile={3}&msg={4}", loginName, passWord, method, mobiles.JoinToString(','), message);
            ////byte[] bs = Encoding.ASCII.GetBytes(param);
            //byte[] bs = Encoding.UTF8.GetBytes(para);
            //HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("http://sms2.eachwe.com/api.php");
            //req.Method = "POST";
            //req.ContentType = "application/x-www-form-urlencoded";
            //req.ContentLength = bs.Length;

            //using (Stream reqStream = req.GetRequestStream())
            //{
            //    reqStream.Write(bs, 0, bs.Length);
            //}

            //using (HttpWebResponse myResponse = (HttpWebResponse)req.GetResponse())
            //{
            //    //using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.GetEncoding("GB2312")))
            //    using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8))
            //    {
            //        string content = reader.ReadToEnd();

            //    }
            //}
            //return false;

            return (false,-1);
        }

        /// <summary>
        /// 轮询获取短信
        /// </summary>
        /// <returns></returns>
        public async Task<SMSMessageItem[]> GetMessage()
        {
            var url = "http://sms2.eachwe.com/api.php";

            //string loginName = _config.AppSettings.GetValueByName<string>("Eachwe_LoginName");
            //string passWord = _config.AppSettings.GetValueByName<string>("Eachwe_PassWord");

            //var datas = new Dictionary<string, string>();
            //datas.Add("username", _loginName);
            //datas.Add("password", _passWord);
            //datas.Add("method", "getreply");

            //var respData = WebHelper.GetUriDataByPost(url, datas, Encoding.GetEncoding("gb2312"));

            var respData = await WebHelper.Create(url)
                .Encoding(Encoding.GetEncoding("gb2312"))
                .ContentType(WebHelper.ContentTypeEnum.FormUrlencoded)
                .SetParamter("method", "getreply")
                .SetParamter("username", _loginName)
                .SetParamter("password", _passWord)
                .Post_StringAsync();
            ;

            var document = new XmlDocument();
            if (!document.SafeLoadXML(respData))
            {
                return null;
            }

            var errors = document.GetElementsByTagName("error");

            if (errors == null || errors.Count <= 0 || errors[0].InnerText.ToStringEx().Trim() != "0")
            {
                return null;
            }

            var repliesItemList = document.GetElementsByTagName("reply");

            if (repliesItemList == null || repliesItemList.Count <= 0)
            {
                return null;
            }

            var lst = new List<SMSMessageItem>(repliesItemList.Count);

            foreach (XmlElement replyItem in repliesItemList)
            {
                var smsItem = new SMSMessageItem();
                smsItem.Mobile = replyItem.GetChildNodeInnerText("mobile").ToStringEx();
                smsItem.ReceiveDt = replyItem.GetChildNodeInnerText("datetime").ToDateTime("yyyy-MM-dd HH:mm").GetValueOrDefault(DateTime.MinValue);
                var msg = replyItem.GetChildNodeInnerText("msg").ToStringEx();
                //msg = msg.Substring(10,msg.Length-11);

                smsItem.Message = msg;

                lst.Add(smsItem);
            }

            return lst.ToArray();
        }

        //public bool CanGetBalance { get { return true; } }

        //public decimal GetBalance()
        //{
        //    var url = "http://sms2.eachwe.com/api.php";

        //    var datas = new Dictionary<string, string>();
        //    datas.Add("username", _loginName);
        //    datas.Add("password", _passWord);

        //    var respData = WebHelper.GetUriDataByPost(url, datas, Encoding.GetEncoding("gb2312"));

        //    var document = new XmlDocument();

        //    if (!document.SafeLoadXML(respData))
        //    {
        //        return 0;
        //    }

        //    var errors = document.GetElementsByTagName("error");

        //    if (errors == null || errors.Count <= 0 || errors[0].InnerText.ToInt(-1) != 0)
        //    {
        //        return 0;
        //    }

        //    var remainders = document.GetElementsByTagName("remainder");

        //    if (remainders == null || remainders.Count <= 0)
        //    {
        //        return 0;
        //    }

        //    return remainders[0].InnerText.ToInt();
        //}

    }
}
