using AspNetCoreRateLimit;

namespace NineChronicles.Headless.Properties
{
    public class CustomIpRateLimitProperties : IpRateLimitOptions
    {
        public int IpBanThresholdCount { get; set; } = 10;

        public int IpBanMinute { get; set; } = 60;
    }
}
