using System.Collections.Immutable;

namespace DynamicUI24.Core.Companies;

public enum CompanyProfileStatus
{
    Ready,
    NotFound,
    Error,
}

public sealed record CompanyProfile
{
    public CompanyProfile(
        CompanyId companyId,
        string legalName,
        string? shortName = null,
        string? taxCode = null,
        string? address = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? representativeName = null,
        string? status = null,
        IEnumerable<KeyValuePair<string, string>>? additionalFields = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        CompanyId = companyId;
        LegalName = legalName.Trim();
        ShortName = shortName;
        TaxCode = taxCode;
        Address = address;
        Phone = phone;
        Email = email;
        Website = website;
        RepresentativeName = representativeName;
        Status = status;
        AdditionalFields = (additionalFields ?? [])
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    public CompanyId CompanyId { get; }
    public string LegalName { get; }
    public string? ShortName { get; }
    public string? TaxCode { get; }
    public string? Address { get; }
    public string? Phone { get; }
    public string? Email { get; }
    public string? Website { get; }
    public string? RepresentativeName { get; }
    public string? Status { get; }
    public IReadOnlyDictionary<string, string> AdditionalFields { get; }
}

public sealed record CompanyProfileResult(
    CompanyProfileStatus Status,
    CompanyProfile? Profile = null,
    string? DiagnosticCode = null)
{
    public static CompanyProfileResult Ready(CompanyProfile profile) =>
        new(CompanyProfileStatus.Ready, profile ?? throw new ArgumentNullException(nameof(profile)));
    public static CompanyProfileResult NotFound(string? diagnosticCode = null) =>
        new(CompanyProfileStatus.NotFound, null, diagnosticCode);
    public static CompanyProfileResult Error(string diagnosticCode) =>
        new(CompanyProfileStatus.Error, null, diagnosticCode);
}

public interface ICompanyProfileProvider
{
    Task<CompanyProfileResult> GetProfileAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default);
}
