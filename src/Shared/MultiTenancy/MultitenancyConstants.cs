namespace RAFFLE.WebApi.Shared.Multitenancy;

public class MultitenancyConstants
{
    public static class Root
    {
        public const string Id = "raffle";
        public const string Name = "Main Raffle";
        public const string EmailAddress = "admin@raffle.com";
    }

    public const string DefaultPassword = "5A7A50C93FB9A4A3FC472971880AEA1D";

    public const string TenantIdName = "tenant";
}