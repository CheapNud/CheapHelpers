﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UblSharp;
using UblSharp.CommonAggregateComponents;
using UblSharp.UnqualifiedDataTypes;


namespace CheapHelpers.Services
{
    public class UblService
    {
        public async Task Create(dynamic order)
        {
            try
            {
                UblSharp.OrderType doc = new UblSharp.OrderType
                {
                    UBLVersionID = "2.1",
                    CustomizationID = "urn:www.cenbii.eu:transaction:biicoretrdm001:ver1.0",
                    ProfileID = new IdentifierType
                    {
                        schemeAgencyID = "BII",
                        schemeID = "Profile",
                        Value = "urn:www.cenbii.eu:profile:BII01:ver1.0"
                    },
                    ID = "34",
                    IssueDate = "2010-01-20",
                    IssueTime = "12:30:00",
                    Note = new List<TextType>()
                    {
                        new TextType
                        {
                            Value = "Information text for the whole order"
                        }
                    },
                    DocumentCurrencyCode = "SEK",
                    AccountingCostCode = "Project123",
                    ValidityPeriod = new List<PeriodType>()
                {
                    new PeriodType
                    {
                        EndDate = "2010-01-31"
                    }
                },
                    QuotationDocumentReference = new DocumentReferenceType
                    {
                        ID = "QuoteID123"
                    },
                    OrderDocumentReference = new List<DocumentReferenceType>()
                {
                    new DocumentReferenceType
                    {
                        ID = "RjectedOrderID123"
                    }
                },
                    OriginatorDocumentReference = new DocumentReferenceType
                    {
                        ID = "MAFO"
                    },
                    AdditionalDocumentReference = new List<DocumentReferenceType>()
                {
                    new DocumentReferenceType
                    {
                        ID = "Doc1",
                        DocumentType = "Timesheet",
                        Attachment = new AttachmentType
                        {
                            ExternalReference = new ExternalReferenceType
                            {
                                URI = "http://www.suppliersite.eu/sheet001.html"
                            }
                        }
                    },
                    new DocumentReferenceType
                    {
                        ID = "Doc2",
                        DocumentType = "Drawing",
                        Attachment = new AttachmentType
                        {
                            EmbeddedDocumentBinaryObject = new BinaryObjectType
                            {
                                mimeCode = "application/pdf",
                                Value = Convert.FromBase64String("UjBsR09EbGhjZ0dTQUxNQUFBUUNBRU1tQ1p0dU1GUXhEUzhi")
                            }
                        }
                    }
                },
                    Contract = new List<ContractType>()
                {
                    new ContractType
                    {
                        ID = "34322",
                        ContractType1 = "FrameworkAgreementID123"
                    }
                },
                    BuyerCustomerParty = new CustomerPartyType
                    {
                        Party = new PartyType
                        {
                            EndpointID = new IdentifierType
                            {
                                schemeAgencyID = "9",
                                schemeID = "GLN",
                                Value = "7300072311115"
                            },
                            PartyIdentification = new List<PartyIdentificationType>()
                            {
                                new PartyIdentificationType
                                {
                                    ID = new IdentifierType
                                    {
                                        schemeAgencyID = "9",
                                        schemeID = "GLN",
                                        Value = "7300070011115"
                                    }
                                },
                            new PartyIdentificationType
                            {
                                ID = "PartyID123"
                            }
                        },
                            PartyName = new List<PartyNameType>()
                        {
                            new PartyNameType
                            {
                                Name = "Johnssons byggvaror"
                            }
                        },
                            PostalAddress = new AddressType
                            {
                                ID = new IdentifierType
                                {
                                    schemeAgencyID = "9",
                                    schemeID = "GLN",
                                    Value = "1234567890123"
                                },
                                Postbox = "PoBox123",
                                StreetName = "Rådhusgatan",
                                AdditionalStreetName = "2nd floor",
                                BuildingNumber = "5",
                                Department = "Purchasing department",
                                CityName = "Stockholm",
                                PostalZone = "11000",
                                CountrySubentity = "RegionX",
                                Country = new CountryType
                                {
                                    IdentificationCode = "SE"
                                }
                            },
                            PartyTaxScheme = new List<PartyTaxSchemeType>()
                        {
                            new PartyTaxSchemeType
                            {
                                RegistrationName = "Herra Johnssons byggvaror AS",
                                CompanyID = "SE1234567801",
                                RegistrationAddress = new AddressType
                                {
                                    CityName = "Stockholm",
                                    Country = new CountryType
                                    {
                                        IdentificationCode = "SE"
                                    }
                                },
                                TaxScheme = new TaxSchemeType
                                {
                                    ID = new IdentifierType
                                    {
                                        schemeID = "UN/ECE 5153",
                                        schemeAgencyID = "6",
                                        Value = "VAT"
                                    }
                                }
                            }
                        },
                            PartyLegalEntity = new List<PartyLegalEntityType>()
                        {
                            new PartyLegalEntityType
                            {
                                RegistrationName = "Johnssons Byggvaror AB",
                                CompanyID = new IdentifierType
                                {
                                    schemeID = "SE:ORGNR",
                                    Value = "5532331183"
                                },
                                RegistrationAddress = new AddressType
                                {
                                    CityName = "Stockholm",
                                    CountrySubentity = "RegionX",
                                    Country = new CountryType
                                    {
                                        IdentificationCode = "SE"
                                    }
                                }
                            }
                        },
                            Contact = new ContactType
                            {
                                Telephone = "123456",
                                Telefax = "123456",
                                ElectronicMail = "pelle@johnsson.se"
                            },
                            Person = new List<PersonType>()
                            {
                                new PersonType
                                {
                                    FirstName = "Pelle",
                                    FamilyName = "Svensson",
                                    MiddleName = "X",
                                    JobTitle = "Boss"
                                }
                            },
                        },
                        DeliveryContact = new ContactType
                        {
                            Name = "Eva Johnsson",
                            Telephone = "1234356",
                            Telefax = "123455",
                            ElectronicMail = "eva@johnsson.se"
                        }
                    },
                    SellerSupplierParty = new SupplierPartyType
                    {
                        Party = new PartyType
                        {
                            EndpointID = new IdentifierType
                            {
                                schemeAgencyID = "9",
                                schemeID = "GLN",
                                Value = "7302347231111"
                            },
                            PartyIdentification = new List<PartyIdentificationType>()
                        {
                            new PartyIdentificationType
                            {
                                ID = "SellerPartyID123"
                            }
                        },
                            PartyName = new List<PartyNameType>()
                        {
                            new PartyNameType
                            {
                                Name = "Moderna Produkter AB"
                            }
                        },
                            PostalAddress = new AddressType
                            {
                                ID = new IdentifierType
                                {
                                    schemeAgencyID = "9",
                                    schemeID = "GLN",
                                    Value = "0987654321123"
                                },
                                Postbox = "321",
                                StreetName = "Kungsgatan",
                                AdditionalStreetName = "suite12",
                                BuildingNumber = "22",
                                Department = "Sales department",
                                CityName = "Stockholm",
                                PostalZone = "11000",
                                CountrySubentity = "RegionX",
                                Country = new CountryType
                                {
                                    IdentificationCode = "SE"
                                }
                            },
                            PartyLegalEntity = new List<PartyLegalEntityType>()
                        {
                            new PartyLegalEntityType
                            {
                                RegistrationName = "Moderna Produkter AB",
                                CompanyID = new IdentifierType
                                {
                                    schemeID = "SE:ORGNR",
                                    Value = "5532332283"
                                },
                                RegistrationAddress = new AddressType
                                {
                                    CityName = "Stockholm",
                                    CountrySubentity = "RegionX",
                                    Country = new CountryType
                                    {
                                        IdentificationCode = "SE"
                                    }
                                }
                            }
                        },
                            Contact = new ContactType
                            {
                                Telephone = "34557",
                                Telefax = "3456767",
                                ElectronicMail = "lars@moderna.se"
                            },
                            Person = new List<PersonType>()
                        {
                            new PersonType
                            {
                                FirstName = "Lars",
                                FamilyName = "Petersen",
                                MiddleName = "M",
                                JobTitle = "Sales manager"
                            }
                        },
                        }
                    },
                    OriginatorCustomerParty = new CustomerPartyType
                    {
                        Party = new PartyType
                        {
                            PartyIdentification = new List<PartyIdentificationType>()
                        {
                            new PartyIdentificationType
                            {
                                ID = new IdentifierType
                                {
                                    schemeAgencyID = "9",
                                    schemeID = "GLN",
                                    Value = "0987678321123"
                                }
                            }
                        },
                            PartyName = new List<PartyNameType>()
                        {
                            new PartyNameType
                            {
                                Name = "Moderna Produkter AB"
                            }
                        },
                            Contact = new ContactType
                            {
                                Telephone = "346788",
                                Telefax = "8567443",
                                ElectronicMail = "sven@moderna.se"
                            },
                            Person = new List<PersonType>()
                        {
                            new PersonType
                            {
                                FirstName = "Sven",
                                FamilyName = "Pereson",
                                MiddleName = "N",
                                JobTitle = "Stuffuser"
                            }
                        },
                        }
                    },
                    Delivery = new List<DeliveryType>()
                {
                    new DeliveryType
                    {
                        DeliveryLocation = new LocationType
                        {
                            Address = new AddressType
                            {
                                ID = new IdentifierType
                                {
                                    schemeAgencyID = "9",
                                    schemeID = "GLN",
                                    Value = "1234567890123"
                                },
                                Postbox = "123",
                                StreetName = "Rådhusgatan",
                                AdditionalStreetName = "2nd floor",
                                BuildingNumber = "5",
                                Department = "Purchasing department",
                                CityName = "Stockholm",
                                PostalZone = "11000",
                                CountrySubentity = "RegionX",
                                Country = new CountryType
                                {
                                    IdentificationCode = "SE"
                                }
                            }
                        },
                        RequestedDeliveryPeriod = new PeriodType
                        {
                            StartDate = "2010-02-10",
                            EndDate = "2010-02-25"
                        },
                        DeliveryParty = new PartyType
                        {
                            PartyIdentification = new List<PartyIdentificationType>()
                            {
                                new PartyIdentificationType
                                {
                                    ID = new IdentifierType
                                    {
                                        schemeAgencyID = "9",
                                        schemeID = "GLN",
                                        Value = "67654328394567"
                                    }
                                }
                            },
                            PartyName = new List<PartyNameType>()
                            {
                                new PartyNameType
                                {
                                    Name = "Swedish trucking"
                                }
                            },
                            Contact = new ContactType
                            {
                                Name = "Per",
                                Telephone = "987098709",
                                Telefax = "34673435",
                                ElectronicMail = "bill@svetruck.se"
                            }
                        }
                    }
                },
                    DeliveryTerms = new List<DeliveryTermsType>()
                {
                    new DeliveryTermsType
                    {
                        ID = new IdentifierType
                        {
                            schemeAgencyID = "6",
                            schemeID = "IMCOTERM",
                            Value = "FOT"
                        },
                        SpecialTerms = new List<TextType>()
                        {
                            new TextType
                            {
                                Value = "CAD"
                            }
                        },
                        DeliveryLocation = new LocationType
                        {
                            ID = "STO"
                        }
                    }
                },
                    AllowanceCharge = new List<AllowanceChargeType>()
                {
                    new AllowanceChargeType
                    {
                        ChargeIndicator = true,
                        AllowanceChargeReason = new List<TextType>()
                        {
                            new TextType
                            {
                                Value = "Transport documents"
                            }
                        },
                        Amount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 100M
                        }
                    },
                    new AllowanceChargeType
                    {
                        ChargeIndicator = false,
                        AllowanceChargeReason = new List<TextType>()
                        {
                            new TextType
                            {
                                Value = "Total order value discount"
                            }
                        },
                        Amount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 100M
                        }
                    }
                },
                    TaxTotal = new List<TaxTotalType>()
                {
                    new TaxTotalType
                    {
                        TaxAmount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 100M
                        }
                    }
                },
                    AnticipatedMonetaryTotal = new MonetaryTotalType
                    {
                        LineExtensionAmount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 6225M
                        },
                        AllowanceTotalAmount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 100M
                        },
                        ChargeTotalAmount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 100M
                        },
                        PayableAmount = new AmountType
                        {
                            currencyID = "SEK",
                            Value = 6225M
                        }
                    },
                    OrderLine = new List<OrderLineType>()
                {
                    new OrderLineType
                    {
                        Note = new List<TextType>()
                        {
                            new TextType
                            {
                                Value = "Freetext note on line 1"
                            }
                        },
                        LineItem = new LineItemType
                        {
                            ID = "1",
                            Quantity = new QuantityType
                            {
                                unitCode = "LTR",
                                Value = 120M
                            },
                            LineExtensionAmount = new AmountType
                            {
                                currencyID = "SEK",
                                Value = 6000M
                            },
                            TotalTaxAmount = new AmountType
                            {
                                currencyID = "SEK",
                                Value = 10M
                            },
                            PartialDeliveryIndicator = false,
                            AccountingCostCode = "ProjectID123",
                            Delivery = new List<DeliveryType>()
                            {
                                new DeliveryType
                                {
                                    RequestedDeliveryPeriod = new PeriodType
                                    {
                                        StartDate = "2010-02-10",
                                        EndDate = "2010-02-25"
                                    }
                                }
                            },
                            OriginatorParty = new PartyType
                            {
                                PartyIdentification = new List<PartyIdentificationType>()
                                {
                                    new PartyIdentificationType
                                    {
                                        ID = new IdentifierType
                                        {
                                            schemeAgencyID = "ZZZ",
                                            schemeID = "ZZZ",
                                            Value = "EmployeeXXX"
                                        }
                                    }
                                },
                                PartyName = new List<PartyNameType>()
                                {
                                    new PartyNameType
                                    {
                                        Name = "Josef K."
                                    }
                                },
                            },
                            Price = new PriceType
                            {
                                PriceAmount = new AmountType
                                {
                                    currencyID = "SEK",
                                    Value = 50M
                                },
                                BaseQuantity = new QuantityType
                                {
                                    unitCode = "LTR",
                                    Value = 1M
                                }
                            },
                            Item = new ItemType
                            {
                                Description = new List<TextType>()
                                {
                                    new TextType
                                    {
                                        Value = "Red paint"
                                    }
                                },
                                Name = "Falu Rödfärg",
                                SellersItemIdentification = new ItemIdentificationType
                                {
                                    ID = "SItemNo001"
                                },
                                StandardItemIdentification = new ItemIdentificationType
                                {
                                    ID = new IdentifierType
                                    {
                                        schemeAgencyID = "6",
                                        schemeID = "GTIN",
                                        Value = "1234567890123"
                                    }
                                },
                                AdditionalItemProperty = new List<ItemPropertyType>()
                                {
                                    new ItemPropertyType
                                    {
                                        Name = "Paint type",
                                        Value = "Acrylic"
                                    },
                                    new ItemPropertyType
                                    {
                                        Name = "Solvant",
                                        Value = "Water"
                                    }
                                },
                            }
                        }
                    },
                    new OrderLineType
                    {
                        Note = new List<TextType>()
                        {
                            new TextType
                            {
                                Value = "Freetext note on line 2"
                            }
                        },
                        LineItem = new LineItemType
                        {
                            ID = "2",
                            Quantity = new QuantityType
                            {
                                unitCode = "C62",
                                Value = 15M
                            },
                            LineExtensionAmount = new AmountType
                            {
                                currencyID = "SEK",
                                Value = 225M
                            },
                            TotalTaxAmount = new AmountType
                            {
                                currencyID = "SEK",
                                Value = 10M
                            },
                            PartialDeliveryIndicator = false,
                            AccountingCostCode = "ProjectID123",
                            Delivery = new List<DeliveryType>()
                            {
                                new DeliveryType
                                {
                                    RequestedDeliveryPeriod = new PeriodType
                                    {
                                        StartDate = "2010-02-10",
                                        EndDate = "2010-02-25"
                                    }
                                }
                            },
                            OriginatorParty = new PartyType
                            {
                                PartyIdentification = new List<PartyIdentificationType>()
                                {
                                    new PartyIdentificationType
                                    {
                                        ID = new IdentifierType
                                        {
                                            schemeAgencyID = "ZZZ",
                                            schemeID = "ZZZ",
                                            Value = "EmployeeXXX"
                                        }
                                    }
                                },
                                PartyName = new List<PartyNameType>()
                                {
                                    new PartyNameType
                                    {
                                        Name = "Josef K."
                                    }
                                },
                            },
                            Price = new PriceType
                            {
                                PriceAmount = new AmountType
                                {
                                    currencyID = "SEK",
                                    Value = 15M
                                },
                                BaseQuantity = new QuantityType
                                {
                                    unitCode = "C62",
                                    Value = 1M
                                }
                            },
                            Item = new ItemType
                            {
                                Description = new List<TextType>()
                                {
                                    new TextType
                                    {
                                        Value = "Very good pencils for red paint."
                                    }
                                },
                                Name = "Pensel 20 mm",
                                SellersItemIdentification = new ItemIdentificationType
                                {
                                    ID = "SItemNo011"
                                },
                                StandardItemIdentification = new ItemIdentificationType
                                {
                                    ID = new IdentifierType
                                    {
                                        schemeAgencyID = "6",
                                        schemeID = "GTIN",
                                        Value = "123452340123"
                                    }
                                },
                                AdditionalItemProperty = new List<ItemPropertyType>()
                                {
                                    new ItemPropertyType
                                    {
                                        Name = "Hair color",
                                        Value = "Black"
                                    },
                                    new ItemPropertyType
                                    {
                                        Name = "Width",
                                        Value = "20mm"
                                    }
                                },
                            }
                        }
                    }
                }
                };

                doc.Xmlns = new System.Xml.Serialization.XmlSerializerNamespaces(new[]
                {
                    new XmlQualifiedName("cac","urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"),
                    new XmlQualifiedName("cbc","urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"),
                });

                //return doc;
                doc.Save(@$"C:\Users\BrechtVandeninden\Downloads\ubl.xml");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }



    }
}
