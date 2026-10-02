using System;
using System.Collections.Generic;

namespace ApiCore8.Application.Contracts
{
    public class RateLimitInfo
    {
        public string ClientIP { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public DateTime? BlockedUntil { get; set; }
        public List<DateTime> RequestTimestamps { get; set; } = new();
        public int SpamCount { get; set; } = 0;
        
        public bool IsBlocked => BlockedUntil.HasValue && BlockedUntil.Value > DateTime.Now;
        
        public void AddRequest()
        {
            RequestTimestamps.Add(DateTime.Now);
        }
        
        public void CleanupOldRequests(TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now - timeWindow;
            RequestTimestamps.RemoveAll(t => t < cutoffTime);
        }
        
        public void Block(TimeSpan duration)
        {
            BlockedUntil = DateTime.Now.Add(duration);
            SpamCount++;
        }
    }
}