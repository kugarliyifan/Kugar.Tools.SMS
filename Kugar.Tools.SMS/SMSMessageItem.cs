using System;
using System.Configuration;
using System.Text;

namespace Kugar.Tools.SMS
{
    public struct SMSMessageItem
    {
        public string Mobile { set; get; }

        public string Message { set; get; }

        public DateTime ReceiveDt { set; get; }
    }
}
