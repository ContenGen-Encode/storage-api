using Congen.Storage.Data.Data_Objects.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Data.Data_Objects
{
    public class User
    {
        public string Id { get; set; }

        public bool PasswordEnabled { get; set; }

        public bool TotpEnabled { get; set; }

        public bool BackupCodeEnabled { get; set; }

        public bool TwoFactorEnabled { get; set; }

        public bool Banned { get; set; }

        public bool Locked { get; set; }

        public long CreatedAt { get; set; }

        public long UpdatedAt { get; set; }

        public string ImageUrl { get; set; }

        public bool HasImage { get; set; }

        public string PrimaryEmailAddressId { get; set; }

        public string PrimaryPhoneNumberId { get; set; }

        public string PrimaryWeb3WalletId { get; set; }

        public long LastSignInAt { get; set; }

        public string ExternalId { get; set; }

        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Dictionary<string, object> PublicMetadata { get; set; }

        public Dictionary<string, object> PrivateMetadata { get; set; }

        public Dictionary<string, object> UnsafeMetadata { get; set; }

        public List<EmailAddress> EmailAddresses { get; set; }

        public List<object> PhoneNumbers { get; set; }

        public List<object> Web3Wallets { get; set; }

        public List<object> ExternalAccounts { get; set; }

        public List<object> SamlAccounts { get; set; }

        public long LastActiveAt { get; set; }

        public bool CreateOrganizationEnabled { get; set; }

        public int? CreateOrganizationsLimit { get; set; }

        public bool DeleteSelfEnabled { get; set; }

        public long LegalAcceptedAt { get; set; }
    }

}
