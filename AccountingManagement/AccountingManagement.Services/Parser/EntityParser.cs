using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using ExcelDataReader;
using Serilog;

namespace AccountingManagement.Services
{
    public interface IEntityParser
    {
        bool TryParseOwner(string fileName, out List<Owner> owners);
    }

    public class EntityParser : IEntityParser
    {
        public EntityParser()
        { }

        public bool TryParseOwner(string fileName, out List<Owner> owners)
        {
            owners = new List<Owner>();

            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    if (reader.Read() == false)
                    {
                        Log.Error("Header row not found");
                        return false;
                    }

                    if (reader.FieldCount < 5)
                    {
                        Log.Error("Insufficient data");
                        return false;
                    }

                    int nameIndex = -1, sinIndex = -1, addressIndex = -1, phoneIndex = -1, emailIndex = -1, noteIndex = -1;

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader.IsDBNull(i))
                        {
                            continue;
                        }

                        switch (reader.GetString(i).ToLowerInvariant())
                        {
                            case "name":
                                nameIndex = i;
                                break;
                            case "sin":
                                sinIndex = i;
                                break;
                            case "address":
                                addressIndex = i;
                                break;
                            case "phone":
                                phoneIndex = i;
                                break;
                            case "email":
                                emailIndex = i;
                                break;
                            case "note":
                                noteIndex = i;
                                break;
                            default:
                                break;
                        }
                    }

                    if (nameIndex < 0 || sinIndex < 0 || addressIndex < 0 || phoneIndex < 0 || emailIndex < 0)
                    {
                        Log.Error("Insufficient data");
                        return false;
                    }

                    while (reader.Read())
                    {
                        if (reader.IsDBNull(nameIndex) || reader.IsDBNull(sinIndex))
                        {
                            continue;
                        }

                        var owner = new Owner()
                        {
                            Id = Guid.NewGuid(),
                            Name = reader.GetString(nameIndex),
                            SIN = reader.GetString(sinIndex),
                            Address = reader.IsDBNull(addressIndex) == false ? reader.GetString(addressIndex) : string.Empty,
                            PhoneNumber = reader.IsDBNull(phoneIndex) == false ? reader.GetString(phoneIndex) : string.Empty,
                            Email = reader.IsDBNull(emailIndex) == false ? reader.GetString(emailIndex) : string.Empty                            
                        };

                        if (noteIndex >= 0)
                        {
                            owner.Notes = reader.GetString(noteIndex);
                        }

                        owner.PersonalTaxAccounts = new List<PersonalTaxAccount>()
                        {
                            new PersonalTaxAccount
                            {
                                Id = Guid.NewGuid(),
                                OwnerId = owner.Id,
                                TaxType = PersonalTaxType.T1,
                                TaxNumber = string.Empty,
                                TaxYear = "2021",
                                Description = owner.Notes,
                                Notes = "Imported from T1 Customer List",
                                IsHighPriority = false,
                                IsActive = true
                            },
                        };

                        if (owners.All(x => x.SIN != owner.SIN))
                        {
                            owners.Add(owner);
                        }
                    }
                }

                while (reader.NextResult());
            }

            using (var dbContext = new AccountingManagementDbContext())
            {
                var existingSINs = dbContext.Owners
                        .Where(x => string.IsNullOrWhiteSpace(x.SIN) == false)
                        .Select(x => x.SIN)
                        .Distinct()
                        .ToHashSet();

                foreach (var newOwner in owners)
                {
                    if (existingSINs.Contains(newOwner.SIN) == false)
                    {
                        dbContext.Owners.Add(newOwner);
                    }
                }

                dbContext.SaveChanges();
            }

            return true;
        }
    }
}
