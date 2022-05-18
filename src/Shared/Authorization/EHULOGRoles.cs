using System.Collections.ObjectModel;

namespace EHULOG.WebApi.Shared.Authorization;

public static class EHULOGRoles
{
    public static string Admin = nameof(Admin);
    public static string Basic = nameof(Basic);

    public static string Lender = nameof(Lender);
    public static string Lessee = nameof(Lessee);

    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Lender,
        Lessee,
        Basic
    });

    public static bool IsDefault(string roleName) => DefaultRoles.Any(r => r == roleName);
}