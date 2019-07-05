using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;

namespace Kugar.Tools.SMS
{
    /// <summary>
    /// 移动企信通接口
    /// </summary>
    public class MPower100SmsSender
    {
        private string _u = "";
        private string _p = "";
        private string _s = "";
        private string _f = "";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u">用户ID</param>
        /// <param name="p">密码</param>
        /// <param name="s">企业ID</param>
        /// <param name="f">通讯口令</param>
        public MPower100SmsSender(string u,string p,string s,string f)
        {
            _u = u;
            _p = p;
            _s = s;
            _f = f;
        }


        public string Name
        {
            get { return "企信通"; }
        }

        public async Task<(bool isSuccess,int statusCode)> SendAsync(string[] mobiles, string message)
        {
            var result = string.Empty;

            //string U = _u;//蓝凌公司提供
            //string S = _s;//企业ID(或账号)
            string P = Uri.EscapeUriString(_u); //密码 (需编码)
            string M = Uri.EscapeUriString(message);//要发送的短信内容。(需编码)
            string T = mobiles.JoinToString();//发送目标号码。
            string F = Uri.EscapeUriString(_f);//双方约定的接口通讯口令。(需编码)            

            string para = String.Format("U={0}&P={1}&S={2}&M={3}&T={4}&F={5}", _u, P, _s, M, T, F);

            string fullUrl = String.Format("{0}?{1}", "http://qxt.mpower100.com:8001/SendSM.aspx", para);

            //HttpWebRequest request = WebRequest.Create(fullUrl) as HttpWebRequest;
            //request.Method = "GET";
            //request.UserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
            //request.KeepAlive = true;
            //request.Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/vnd.ms-excel, application/msword, */*";
            //request.Headers.Add("Accept-Encoding", "gzip, deflate");
            //request.Headers.Add("Accept-Language", "zh-cn");
            //HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            //byte[] content = new byte[1024];
            //Stream rspStream = response.GetResponseStream();
            //result = Helper.GZipDecompress(rspStream);
            //MessageManager.AddSendMessages(productId, MessageUser.S, mobiles, msgContent, result, MsgAccountType.Mpower100, sendUser, msgType);

            result = await Kugar.Core.Network.WebHelper.Create(fullUrl).Get_StringAsync();

            var statusCode = 0;

            #region
            if (!string.IsNullOrEmpty(result))
            {
                string value = result.Split(',')[0];

                statusCode = value.ToInt();

                return (statusCode > 0,statusCode);

                //if (int.Parse(value) > 0)
                //{
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}
            }
            else
            {
                statusCode = 0;
                return (false, statusCode);
            }

            #endregion
        }

        public async Task<SMSMessageItem[]> GetMessage()
        {

            string result = string.Empty;
            try
            {
                string para = String.Format("U={0}&P={1}&S={2}&F={3}", _u, _p, _s, _f);
                string fullUrl = String.Format("{0}?{1}", "http://qxt.mpower100.com:8001/GetSM.aspx", para);
                //HttpWebRequest request = WebRequest.Create(fullUrl) as HttpWebRequest;
                //request.Method = "GET";
                //request.Timeout = 30000;
                //request.UserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
                //request.KeepAlive = true;
                //request.Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/vnd.ms-excel, application/msword, */*";
                //request.Headers.Add("Accept-Encoding", "gzip, deflate");
                //request.Headers.Add("Accept-Language", "zh-cn");

                //HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //byte[] content = new byte[1024];
                //Stream rspStream = response.GetResponseStream();
                result =await Kugar.Core.Network.WebHelper.Create(fullUrl).Get_StringAsync();

                //result = "<?xml version=\"1.0\" encoding=\"GB2312\" ?><RecvMsg><Return>OK</Return><Msg><SendNum>13682867081</SendNum><RecvNum>10657020801264</RecvNum><RecvTime>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</RecvTime><Content>C#145417#1656#15</Content></Msg></RecvMsg>";
                //判断是否含有有效回复格式关键字
                if (!result.Contains("<RecvMsg>"))
                {
                    return null;
                }


                XmlDocument doc = new XmlDocument();
                doc.LoadXml(result);

                var node = doc.SelectNodes("//Msg");
                if (node.Count <= 0)
                {
                    return null;
                }

                var query = from n in doc.SelectNodes("//Msg").Cast<XmlNode>() select n;

                var lst = new List<SMSMessageItem>(query.Count());

                foreach (var q in query)
                {
                    var item = new SMSMessageItem();
                    item.Mobile = q["SendNum"].InnerText;
                    item.ReceiveDt = q["RecvTime"].InnerText.ToDateTime();
                    item.Message = q["Content"].InnerText.ToStringEx();

                    lst.Add(item);
                }

                return lst.ToArray();
            }
            catch (Exception ex)
            {
                LoggerManager.GetLogger("SMSMessageLog").Error(ex.Message);

                return null;
            }

        }

        public bool CanGetBalance { get { return false; } }

        public decimal GetBalance()
        {
            return 0;
        }

        public void Dispose()
        {

        }
    }
}