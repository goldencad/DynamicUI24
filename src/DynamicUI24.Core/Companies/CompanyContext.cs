namespace DynamicUI24.Core.Companies;

public readonly record struct CompanyId
{
    public CompanyId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record CompanyDescriptor(
    CompanyId CompanyId,
    string Code,
    string DisplayName,
    string? TaxCode = null,
    bool IsActive = true)
{
    public string Code { get; init; } = Require(Code, nameof(Code));
    public string DisplayName { get; init; } = Require(DisplayName, nameof(DisplayName));

    private static string Require(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}

public sealed class CompanyChangedEventArgs(
    CompanyDescriptor previousCompany,
    CompanyDescriptor currentCompany) : EventArgs
{
    public CompanyDescriptor PreviousCompany { get; } = previousCompany;
    public CompanyDescriptor CurrentCompany { get; } = currentCompany;
}

public enum CompanySwitchError
{
    None,
    UnknownCompany,
    InactiveCompany,
}

public sealed record CompanySwitchResult(
    bool IsSuccess,
    bool DidChange,
    CompanyDescriptor CurrentCompany,
    CompanySwitchError Error = CompanySwitchError.None)
{
    public static CompanySwitchResult Changed(CompanyDescriptor company) => new(true, true, company);
    public static CompanySwitchResult Unchanged(CompanyDescriptor company) => new(true, false, company);
    public static CompanySwitchResult Rejected(CompanyDescriptor current, CompanySwitchError error) =>
        new(false, false, current, error);
}

public interface ICompanyContextProvider
{
    CompanyDescriptor CurrentCompany { get; }
    IReadOnlyList<CompanyDescriptor> AvailableCompanies { get; }
    event EventHandler<CompanyChangedEventArgs>? CompanyChanged;
    Task<CompanySwitchResult> SwitchCompanyAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default);
}

/// <summary>A small thread-safe context implementation suitable for app composition and tests.</summary>
public sealed class CompanyContextProvider : ICompanyContextProvider
{
    private readonly IReadOnlyList<CompanyDescriptor> companies;
    private readonly object sync = new();
    private CompanyDescriptor currentCompany;

    public CompanyContextProvider(IEnumerable<CompanyDescriptor> companies, CompanyId initialCompanyId)
    {
        ArgumentNullException.ThrowIfNull(companies);
        var values = companies.ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one company is required.", nameof(companies));
        }

        if (values.Select(company => company.CompanyId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException("Company identities must be unique.", nameof(companies));
        }

        currentCompany = values.SingleOrDefault(company => company.CompanyId == initialCompanyId)
            ?? throw new ArgumentException("The initial company is not available.", nameof(initialCompanyId));
        if (!currentCompany.IsActive)
        {
            throw new ArgumentException("The initial company must be active.", nameof(initialCompanyId));
        }

        this.companies = Array.AsReadOnly(values);
    }

    public CompanyDescriptor CurrentCompany
    {
        get { lock (sync) { return currentCompany; } }
    }

    public IReadOnlyList<CompanyDescriptor> AvailableCompanies => companies;
    public event EventHandler<CompanyChangedEventArgs>? CompanyChanged;

    public Task<CompanySwitchResult> SwitchCompanyAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CompanyDescriptor? previous = null;
        CompanyDescriptor? selected = null;
        CompanySwitchResult result;

        lock (sync)
        {
            selected = companies.SingleOrDefault(company => company.CompanyId == companyId);
            if (selected is null)
            {
                return Task.FromResult(CompanySwitchResult.Rejected(currentCompany, CompanySwitchError.UnknownCompany));
            }

            if (!selected.IsActive)
            {
                return Task.FromResult(CompanySwitchResult.Rejected(currentCompany, CompanySwitchError.InactiveCompany));
            }

            if (selected == currentCompany)
            {
                return Task.FromResult(CompanySwitchResult.Unchanged(currentCompany));
            }

            previous = currentCompany;
            currentCompany = selected;
            result = CompanySwitchResult.Changed(selected);
        }

        CompanyChanged?.Invoke(this, new CompanyChangedEventArgs(previous, selected));
        return Task.FromResult(result);
    }
}
