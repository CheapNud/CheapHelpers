using CheapHelpers.Models.Dtos.Ubl;
using UblSharp.CommonAggregateComponents;
using UblSharp.UnqualifiedDataTypes;

namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Shared internal mapper for converting UBL DTO party types to UblSharp types.
/// Used by both UblService (orders) and UblInvoiceService (invoices/credit notes).
/// </summary>
internal static class UblPartyMapper
{
    // Scheme Constants (shared across order and invoice contexts)
    private const string GlnSchemeAgency = "9";
    private const string GlnScheme = "GLN";
    private const string VatScheme = "VAT";
    private const string VatSchemeId = "UN/ECE 5153";
    private const string VatSchemeAgency = "6";
    private const string OrgNrScheme = "SE:ORGNR";

    internal static PartyType ConvertToParty(UblParty party)
    {
        var partyType = new PartyType();

        // Add endpoint ID if provided
        if (!string.IsNullOrWhiteSpace(party.EndpointId))
        {
            partyType.EndpointID = new IdentifierType
            {
                schemeAgencyID = GlnSchemeAgency,
                schemeID = GlnScheme,
                Value = party.EndpointId
            };
        }

        // Add party identification
        if (!string.IsNullOrWhiteSpace(party.Id))
        {
            partyType.PartyIdentification = [new PartyIdentificationType { ID = party.Id }];
        }

        // Add party name
        if (!string.IsNullOrWhiteSpace(party.Name))
        {
            partyType.PartyName = [new PartyNameType { Name = party.Name }];
        }

        // Add address
        if (party.Address is not null)
        {
            partyType.PostalAddress = ConvertToAddress(party.Address);
        }

        // Add contact
        if (party.Contact is not null)
        {
            partyType.Contact = ConvertToContact(party.Contact);
        }

        // Add main person
        if (party.MainPerson is not null)
        {
            partyType.Person = [ConvertToPerson(party.MainPerson)];
        }

        // Add legal entity if company ID is provided
        if (!string.IsNullOrWhiteSpace(party.CompanyId))
        {
            partyType.PartyLegalEntity = [new PartyLegalEntityType
            {
                RegistrationName = party.Name,
                CompanyID = new IdentifierType
                {
                    schemeID = OrgNrScheme,
                    Value = party.CompanyId
                }
            }];
        }

        // Add tax scheme if tax ID is provided
        if (!string.IsNullOrWhiteSpace(party.TaxId))
        {
            partyType.PartyTaxScheme = [new PartyTaxSchemeType
            {
                RegistrationName = party.Name,
                CompanyID = party.TaxId,
                TaxScheme = new TaxSchemeType
                {
                    ID = new IdentifierType
                    {
                        schemeID = VatSchemeId,
                        schemeAgencyID = VatSchemeAgency,
                        Value = VatScheme
                    }
                }
            }];
        }

        return partyType;
    }

    internal static PartyType ConvertToPartyWithEndpointScheme(UblParty party, string? endpointScheme)
    {
        var partyType = ConvertToParty(party);

        // Override endpoint scheme for PEPPOL if specified
        if (!string.IsNullOrWhiteSpace(party.EndpointId) && !string.IsNullOrWhiteSpace(endpointScheme))
        {
            partyType.EndpointID = new IdentifierType
            {
                schemeID = endpointScheme,
                Value = party.EndpointId
            };
        }

        return partyType;
    }

    internal static AddressType ConvertToAddress(UblAddress address)
    {
        var addressType = new AddressType
        {
            CityName = address.CityName,
            Country = new CountryType { IdentificationCode = address.CountryCode }
        };

        if (!string.IsNullOrWhiteSpace(address.AddressId))
        {
            addressType.ID = new IdentifierType
            {
                schemeAgencyID = GlnSchemeAgency,
                schemeID = GlnScheme,
                Value = address.AddressId
            };
        }

        if (!string.IsNullOrWhiteSpace(address.PostBox)) addressType.Postbox = address.PostBox;
        if (!string.IsNullOrWhiteSpace(address.StreetName)) addressType.StreetName = address.StreetName;
        if (!string.IsNullOrWhiteSpace(address.AdditionalStreetName)) addressType.AdditionalStreetName = address.AdditionalStreetName;
        if (!string.IsNullOrWhiteSpace(address.BuildingNumber)) addressType.BuildingNumber = address.BuildingNumber;
        if (!string.IsNullOrWhiteSpace(address.Department)) addressType.Department = address.Department;
        if (!string.IsNullOrWhiteSpace(address.PostalZone)) addressType.PostalZone = address.PostalZone;
        if (!string.IsNullOrWhiteSpace(address.CountrySubentity)) addressType.CountrySubentity = address.CountrySubentity;

        return addressType;
    }

    internal static ContactType ConvertToContact(UblContact contact)
    {
        var contactType = new ContactType();

        if (!string.IsNullOrWhiteSpace(contact.Name)) contactType.Name = contact.Name;
        if (!string.IsNullOrWhiteSpace(contact.Telephone)) contactType.Telephone = contact.Telephone;
        if (!string.IsNullOrWhiteSpace(contact.Fax)) contactType.Telefax = contact.Fax;
        if (!string.IsNullOrWhiteSpace(contact.Email)) contactType.ElectronicMail = contact.Email;

        return contactType;
    }

    internal static PersonType ConvertToPerson(UblPerson person)
    {
        var personType = new PersonType
        {
            FirstName = person.FirstName,
            FamilyName = person.LastName
        };

        if (!string.IsNullOrWhiteSpace(person.MiddleName)) personType.MiddleName = person.MiddleName;
        if (!string.IsNullOrWhiteSpace(person.JobTitle)) personType.JobTitle = person.JobTitle;

        return personType;
    }
}
