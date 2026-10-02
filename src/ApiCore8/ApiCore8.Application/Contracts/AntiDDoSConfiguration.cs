namespace ApiCore8.Application.Contracts
{
    public class AntiDDoSConfiguration
    {
        public bool Enabled { get; set; } = true;

        // LAYER 1: Connection Limiting
        public int MaxConcurrentConnectionsPerIP { get; set; } = 10;
        public int MaxConcurrentConnectionsGlobal { get; set; } = 1000;

        // LAYER 2: Token Bucket
        public TokenBucketSettings TokenBucket { get; set; } = new();

        // LAYER 3: Sliding Window
        public List<RateLimitRule> RateLimits { get; set; } = new();

        // LAYER 4: Blocking Strategy
        public BlockStrategySettings BlockStrategy { get; set; } = new();

        // Endpoint-specific rules
        public List<EndpointRule> EndpointRules { get; set; } = new();

        // Whitelist/Blacklist
        public List<string> WhitelistedIPs { get; set; } = new();
        public List<string> BlacklistedIPs { get; set; } = new();

        // Redis settings
        public RedisSettings Redis { get; set; } = new();

        public bool EnableLogging { get; set; } = true;
    }

    public class TokenBucketSettings
    {
        public int Capacity { get; set; } = 20;           // Max tokens
        public int RefillRate { get; set; } = 5;          // Tokens per interval
        public int RefillIntervalMs { get; set; } = 1000; // Milliseconds
    }

    public class RateLimitRule
    {
        public int WindowSeconds { get; set; }
        public int MaxRequests { get; set; }
        public string Penalty { get; set; } = "Warning"; // Warning, TempBlock, PermBlock
    }

    public class BlockStrategySettings
    {
        public int FirstViolationSeconds { get; set; } = 60;      // 1 minute
        public int SecondViolationSeconds { get; set; } = 300;    // 5 minutes
        public int ThirdViolationSeconds { get; set; } = 1800;    // 30 minutes
        public int FourthViolationSeconds { get; set; } = 86400;  // 24 hours
    }

    public class EndpointRule
    {
        public string Pattern { get; set; } = string.Empty;
        public int MaxRequestsPerMinute { get; set; }
    }

    public class RedisSettings
    {
        public string ConnectionString { get; set; } = "localhost:6380,password=redis123,abortConnect=false";
        public string KeyPrefix { get; set; } = "antiddos:";
        public int DefaultExpirySeconds { get; set; } = 3600;
        public int Database { get; set; } = 0;
    }
}