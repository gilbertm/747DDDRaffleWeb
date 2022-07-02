namespace EHULOG.WebApi.Shared.Multitenancy;

public class MultitenancyConstants
{
    public static class Root
    {
        public const string Id = "root";
        public const string Name = "Root";
        public const string EmailAddress = "admin@root.com";
    }

    public static class Lessee
    {
        public const string Id = "service_lessee";
        public const string Name = "Service Lessee";
        public const string EmailAddress = "service_lessee@ehulog.com";
    }

    public const string DefaultPassword = "123Pa$$word!";

    public const string TenantIdName = "tenant";
}