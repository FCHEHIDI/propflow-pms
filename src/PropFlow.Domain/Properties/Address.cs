using PropFlow.Domain.Common;

namespace PropFlow.Domain.Properties;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string Country { get; }
    public string? PostalCode { get; }

    public Address(string street, string city, string country, string? postalCode = null)
    {
        Street = street;
        City = city;
        Country = country;
        PostalCode = postalCode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
}
