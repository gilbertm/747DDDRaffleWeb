using System.Collections.ObjectModel;

namespace EHULOG.WebApi.Shared.Authorization;

public static class EHULOGAction
{
    public const string View = nameof(View);
    public const string Search = nameof(Search);
    public const string Create = nameof(Create);
    public const string Update = nameof(Update);
    public const string Delete = nameof(Delete);
    public const string Export = nameof(Export);
    public const string Generate = nameof(Generate);
    public const string Clean = nameof(Clean);
    public const string UpgradeSubscription = nameof(UpgradeSubscription);
}

public static class EHULOGResource
{
    public const string Tenants = nameof(Tenants);
    public const string Dashboard = nameof(Dashboard);
    public const string Hangfire = nameof(Hangfire);
    public const string Users = nameof(Users);
    public const string UserRoles = nameof(UserRoles);
    public const string Roles = nameof(Roles);
    public const string RoleClaims = nameof(RoleClaims);
    public const string Products = nameof(Products);
    public const string Brands = nameof(Brands);

    public const string AppUsers = nameof(AppUsers);
    public const string AppUserProducts = nameof(AppUserProducts);
    public const string Categories = nameof(Categories);
    public const string Ledgers = nameof(Ledgers);
    public const string Loans = nameof(Loans);
    public const string LoanApplicants = nameof(LoanApplicants);
    public const string LoanLenders = nameof(LoanLenders);
    public const string LoanLessees = nameof(LoanLessees);
    public const string LoanLocations = nameof(LoanLocations);
    public const string LoanProducts = nameof(LoanProducts);
    public const string Packages = nameof(Packages);
    public const string Ratings = nameof(Ratings);
    public const string InputOutputResources = nameof(InputOutputResources);
}

public static class EHULOGPermissions
{
    private static readonly EHULOGPermission[] _all = new EHULOGPermission[]
    {
        new("View Dashboard", EHULOGAction.View, EHULOGResource.Dashboard),
        new("View Hangfire", EHULOGAction.View, EHULOGResource.Hangfire),
        new("View Users", EHULOGAction.View, EHULOGResource.Users, IsBasic: true, IsLender: true, IsLessee: true),
        new("Search Users", EHULOGAction.Search, EHULOGResource.Users, IsBasic: true, IsLender: true, IsLessee: true),
        new("Create Users", EHULOGAction.Create, EHULOGResource.Users),
        new("Update Users", EHULOGAction.Update, EHULOGResource.Users),
        new("Delete Users", EHULOGAction.Delete, EHULOGResource.Users),
        new("Export Users", EHULOGAction.Export, EHULOGResource.Users),
        new("View UserRoles", EHULOGAction.View, EHULOGResource.UserRoles, IsBasic: true, IsLender: true, IsLessee: true),
        new("Update UserRoles", EHULOGAction.Update, EHULOGResource.UserRoles, IsBasic: true, IsLender: true, IsLessee: true),
        new("View Roles", EHULOGAction.View, EHULOGResource.Roles, IsBasic: true, IsLender: true, IsLessee: true),
        new("Create Roles", EHULOGAction.Create, EHULOGResource.Roles),
        new("Update Roles", EHULOGAction.Update, EHULOGResource.Roles),
        new("Delete Roles", EHULOGAction.Delete, EHULOGResource.Roles),
        new("View RoleClaims", EHULOGAction.View, EHULOGResource.RoleClaims),
        new("Update RoleClaims", EHULOGAction.Update, EHULOGResource.RoleClaims),
        new("View Products", EHULOGAction.View, EHULOGResource.Products, IsLender: true, IsLessee: true),
        new("Search Products", EHULOGAction.Search, EHULOGResource.Products, IsLender: true),
        new("Create Products", EHULOGAction.Create, EHULOGResource.Products, IsLender: true),
        new("Update Products", EHULOGAction.Update, EHULOGResource.Products, IsLender: true),
        new("Delete Products", EHULOGAction.Delete, EHULOGResource.Products, IsLender: true),
        new("Export Products", EHULOGAction.Export, EHULOGResource.Products),
        new("View Brands", EHULOGAction.View, EHULOGResource.Brands, IsLender: true, IsLessee: true),
        new("Search Brands", EHULOGAction.Search, EHULOGResource.Brands, IsLender: true, IsLessee: true),
        new("Create Brands", EHULOGAction.Create, EHULOGResource.Brands),
        new("Update Brands", EHULOGAction.Update, EHULOGResource.Brands),
        new("Delete Brands", EHULOGAction.Delete, EHULOGResource.Brands),
        new("Generate Brands", EHULOGAction.Generate, EHULOGResource.Brands),
        new("Clean Brands", EHULOGAction.Clean, EHULOGResource.Brands),
        new("View Tenants", EHULOGAction.View, EHULOGResource.Tenants, IsRoot: true),
        new("Create Tenants", EHULOGAction.Create, EHULOGResource.Tenants, IsRoot: true),
        new("Update Tenants", EHULOGAction.Update, EHULOGResource.Tenants, IsRoot: true),
        new("Upgrade Tenant Subscription", EHULOGAction.UpgradeSubscription, EHULOGResource.Tenants, IsRoot: true),

        new("View Applicants", EHULOGAction.View, EHULOGResource.LoanApplicants, IsLender: true, IsLessee: true),
        new("Search Applicants", EHULOGAction.Search, EHULOGResource.LoanApplicants, IsLender: true, IsLessee: true),
        new("Create Applicants", EHULOGAction.Create, EHULOGResource.LoanApplicants),
        new("Update Applicants", EHULOGAction.Update, EHULOGResource.LoanApplicants),
        new("Delete Applicants", EHULOGAction.Delete, EHULOGResource.LoanApplicants),
        new("Export Applicants", EHULOGAction.Export, EHULOGResource.LoanApplicants),

        new("View AppUsers", EHULOGAction.View, EHULOGResource.AppUsers, IsBasic: true, IsLender: true, IsLessee: true),
        new("Search AppUsers", EHULOGAction.Search, EHULOGResource.AppUsers),
        new("Create AppUsers", EHULOGAction.Create, EHULOGResource.AppUsers, IsBasic: true, IsLender: true, IsLessee: true),
        new("Update AppUsers", EHULOGAction.Update, EHULOGResource.AppUsers, IsBasic: true, IsLender: true, IsLessee: true),
        new("Delete AppUsers", EHULOGAction.Delete, EHULOGResource.AppUsers),
        new("Export AppUsers", EHULOGAction.Export, EHULOGResource.AppUsers),

        new("View AppUserProducts", EHULOGAction.View, EHULOGResource.AppUserProducts, IsLender: true, IsLessee: true),
        new("Search AppUserProducts", EHULOGAction.Search, EHULOGResource.AppUserProducts, IsLender: true, IsLessee: true),
        new("Create AppUserProducts", EHULOGAction.Create, EHULOGResource.AppUserProducts, IsLender: true),
        new("Update AppUserProducts", EHULOGAction.Update, EHULOGResource.AppUserProducts, IsLender: true),
        new("Delete AppUserProducts", EHULOGAction.Delete, EHULOGResource.AppUserProducts, IsLender: true),
        new("Export AppUserProducts", EHULOGAction.Export, EHULOGResource.AppUserProducts, IsLender: true),

        new("View Categories", EHULOGAction.View, EHULOGResource.Categories, IsBasic: true, IsLender: true, IsLessee: true),
        new("Search Categories", EHULOGAction.Search, EHULOGResource.Categories, IsBasic: true, IsLender: true, IsLessee: true),
        new("Create Categories", EHULOGAction.Create, EHULOGResource.Categories),
        new("Update Categories", EHULOGAction.Update, EHULOGResource.Categories),
        new("Delete Categories", EHULOGAction.Delete, EHULOGResource.Categories),
        new("Export Categories", EHULOGAction.Export, EHULOGResource.Categories),

        new("View Ledgers", EHULOGAction.View, EHULOGResource.Ledgers, IsLender: true, IsLessee: true),
        new("Search Ledgers", EHULOGAction.Search, EHULOGResource.Ledgers, IsLender: true, IsLessee: true),
        new("Create Ledgers", EHULOGAction.Create, EHULOGResource.Ledgers),
        new("Update Ledgers", EHULOGAction.Update, EHULOGResource.Ledgers),
        new("Delete Ledgers", EHULOGAction.Delete, EHULOGResource.Ledgers),
        new("Export Ledgers", EHULOGAction.Export, EHULOGResource.Ledgers),

        new("View Loans", EHULOGAction.View, EHULOGResource.Loans, IsBasic: true),
        new("Search Loans", EHULOGAction.Search, EHULOGResource.Loans, IsBasic: true),
        new("Create Loans", EHULOGAction.Create, EHULOGResource.Loans),
        new("Update Loans", EHULOGAction.Update, EHULOGResource.Loans),
        new("Delete Loans", EHULOGAction.Delete, EHULOGResource.Loans),
        new("Export Loans", EHULOGAction.Export, EHULOGResource.Loans),

        new("View LoanLenders", EHULOGAction.View, EHULOGResource.LoanLenders, IsLender: true, IsLessee: true),
        new("Search LoanLenders", EHULOGAction.Search, EHULOGResource.LoanLenders, IsLender: true, IsLessee: true),
        new("Create LoanLenders", EHULOGAction.Create, EHULOGResource.LoanLenders, IsLender: true),
        new("Update LoanLenders", EHULOGAction.Update, EHULOGResource.LoanLenders),
        new("Delete LoanLenders", EHULOGAction.Delete, EHULOGResource.LoanLenders),
        new("Export LoanLenders", EHULOGAction.Export, EHULOGResource.LoanLenders),

        new("View LoanLessees", EHULOGAction.View, EHULOGResource.LoanLessees, IsLender: true),
        new("Search LoanLessees", EHULOGAction.Search, EHULOGResource.LoanLessees, IsLender: true),
        new("Create LoanLessees", EHULOGAction.Create, EHULOGResource.LoanLessees),
        new("Update LoanLessees", EHULOGAction.Update, EHULOGResource.LoanLessees),
        new("Delete LoanLessees", EHULOGAction.Delete, EHULOGResource.LoanLessees),
        new("Export LoanLessees", EHULOGAction.Export, EHULOGResource.LoanLessees),

        new("View LoanLocations", EHULOGAction.View, EHULOGResource.LoanLocations, IsBasic: true),
        new("Search LoanLocations", EHULOGAction.Search, EHULOGResource.LoanLocations, IsBasic: true),
        new("Create LoanLocations", EHULOGAction.Create, EHULOGResource.LoanLocations),
        new("Update LoanLocations", EHULOGAction.Update, EHULOGResource.LoanLocations),
        new("Delete LoanLocations", EHULOGAction.Delete, EHULOGResource.LoanLocations),
        new("Export LoanLocations", EHULOGAction.Export, EHULOGResource.LoanLocations),

        new("View LoanProducts", EHULOGAction.View, EHULOGResource.LoanProducts, IsBasic: true),
        new("Search LoanProducts", EHULOGAction.Search, EHULOGResource.LoanProducts, IsBasic: true),
        new("Create LoanProducts", EHULOGAction.Create, EHULOGResource.LoanProducts),
        new("Update LoanProducts", EHULOGAction.Update, EHULOGResource.LoanProducts),
        new("Delete LoanProducts", EHULOGAction.Delete, EHULOGResource.LoanProducts),
        new("Export LoanProducts", EHULOGAction.Export, EHULOGResource.LoanProducts),

        new("View Packages", EHULOGAction.View, EHULOGResource.Packages, IsBasic: true, IsLender: true, IsLessee: true),
        new("Search Packages", EHULOGAction.Search, EHULOGResource.Packages, IsBasic: true, IsLender: true, IsLessee: true),
        new("Create Packages", EHULOGAction.Create, EHULOGResource.Packages),
        new("Update Packages", EHULOGAction.Update, EHULOGResource.Packages),
        new("Delete Packages", EHULOGAction.Delete, EHULOGResource.Packages),
        new("Export Packages", EHULOGAction.Export, EHULOGResource.Packages),

        new("View Ratings", EHULOGAction.View, EHULOGResource.Ratings, IsLender: true, IsLessee: true),
        new("Search Ratings", EHULOGAction.Search, EHULOGResource.Ratings, IsLender: true, IsLessee: true),
        new("Create Ratings", EHULOGAction.Create, EHULOGResource.Ratings),
        new("Update Ratings", EHULOGAction.Update, EHULOGResource.Ratings),
        new("Delete Ratings", EHULOGAction.Delete, EHULOGResource.Ratings),
        new("Export Ratings", EHULOGAction.Export, EHULOGResource.Ratings),

        new("View InputOutputResources", EHULOGAction.View, EHULOGResource.InputOutputResources, IsBasic: true, IsLender: true, IsLessee: true),
        new("Search InputOutputResources", EHULOGAction.Search, EHULOGResource.InputOutputResources, IsBasic: true, IsLender: true, IsLessee: true),
        new("Create InputOutputResources", EHULOGAction.Create, EHULOGResource.InputOutputResources, IsBasic: true, IsLender: true, IsLessee: true),
        new("Update InputOutputResources", EHULOGAction.Update, EHULOGResource.InputOutputResources, IsBasic: true, IsLender: true, IsLessee: true),
        new("Delete InputOutputResources", EHULOGAction.Delete, EHULOGResource.InputOutputResources, IsBasic: true, IsLender: true, IsLessee: true),
        new("Export InputOutputResources", EHULOGAction.Export, EHULOGResource.InputOutputResources, IsBasic: true, IsLender: true, IsLessee: true)

    };

    public static IReadOnlyList<EHULOGPermission> All { get; } = new ReadOnlyCollection<EHULOGPermission>(_all);
    public static IReadOnlyList<EHULOGPermission> Root { get; } = new ReadOnlyCollection<EHULOGPermission>(_all.Where(p => p.IsRoot).ToArray());
    public static IReadOnlyList<EHULOGPermission> Admin { get; } = new ReadOnlyCollection<EHULOGPermission>(_all.Where(p => !p.IsRoot).ToArray());
    public static IReadOnlyList<EHULOGPermission> Basic { get; } = new ReadOnlyCollection<EHULOGPermission>(_all.Where(p => p.IsBasic).ToArray());

    public static IReadOnlyList<EHULOGPermission> Lessee { get; } = new ReadOnlyCollection<EHULOGPermission>(_all.Where(p => p.IsLessee).ToArray());
    public static IReadOnlyList<EHULOGPermission> Lender { get; } = new ReadOnlyCollection<EHULOGPermission>(_all.Where(p => p.IsLender).ToArray());

}

public record EHULOGPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false, bool IsLessee = false, bool IsLender = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource) => $"Permissions.{resource}.{action}";
}