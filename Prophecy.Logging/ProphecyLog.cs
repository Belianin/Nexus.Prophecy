﻿using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Nexus.Logging.Utils;

namespace Nexus.Logging.Prophecy
{
    public class ProphecyLog : BaseLog
    {
        private readonly HttpClient client;
        private readonly string url;
        private readonly ILog log;

        public ProphecyLog(string url, ILog log = null)
        {
            client = new HttpClient();
            
            this.url = url;
            this.log = log ?? new SilentLog();
        }

        protected override void InnerLog(LogEvent logEvent)
        {
            var request = new Request
            {
                LogLevel = logEvent.Level.ToString(),
                Message = $"{string.Join("", logEvent.Context.Select(s => $"[{s}] "))}{logEvent.Message}"
            };
            
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "text/json");
                var result = client.PostAsync($"{url}/api/v1/notify", content)
                    .GetAwaiter().GetResult();
                
                log.Info(result.StatusCode.ToString());
            }
            catch (Exception e)
            {
                log.Fatal($"Failed to send request to Prophecy: {e.Message}");
            }
        }

        public override void Dispose()
        {
            client.Dispose();
        }

        private class Request
        {
            public string LogLevel { get; set; }
            public string Message { get; set; }
        }
    }
}