namespace EHULOG.WebApi.Shared.Multitenancy;

public class MultitenancyConstants
{
    public static class Root
    {
        public const string Id = "ehulog";
        public const string Name = "eHulog";
        public const string EmailAddress = "admin@ehulog.com";
    }

    public static class ServiceLessee
    {
        public const string Id = "service_lessee";
        public const string Name = "Service Lessee";
        public const string EmailAddress = "service_lessee@ehulog.com";
    }

    public const string DefaultPassword = "5A7A50C93FB9A4A3FC472971880AEA1D";

    public const string DefaultPasswordServiceLessee = "BD4D94272925806077FC5F5E5DECE131";

    public const string TenantIdName = "tenant";
}