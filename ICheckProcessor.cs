using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sensu_client
{
    public interface ICheckProcessor
    {
        void ProcessCheck(JObject check);
        void PublishResult(JObject checkResult);
        void PublishCheckResult(JObject checkResult);
    }
}
