using System;
using ditjson.Models;

namespace ditjson.Filtering
{
    internal static class ObjectFilter
    {
        internal static void CleanupComputer(Computer? computer, bool includeEmptyCollections)
        {
            if (computer == null)
            {
                return;
            }

            if (!includeEmptyCollections && computer.MemberOf?.Count == 0)
            {
                computer.MemberOf = null;
            }

            if (computer.PasswordHashes != null && string.IsNullOrEmpty(computer.PasswordHashes.NtHash)
                && string.IsNullOrEmpty(computer.PasswordHashes.LmHash))
            {
                computer.PasswordHashes = null;
            }

            if (computer.SupplementalCredentials != null && string.IsNullOrEmpty(computer.SupplementalCredentials.ClearTextPassword)
                && (computer.SupplementalCredentials.KerberosKeys == null || computer.SupplementalCredentials.KerberosKeys.Count == 0))
            {
                computer.SupplementalCredentials = null;
            }

            if (computer.Recovery != null && string.IsNullOrEmpty(computer.Recovery.RecoveryPassword)
                && computer.Recovery.RecoveryGuid == Guid.Empty && computer.Recovery.VolumeGuid == Guid.Empty)
            {
                computer.Recovery = null;
            }
        }

        internal static void CleanupGroup(Group? group, bool includeEmptyCollections)
        {
            if (group == null)
            {
                return;
            }

            if (!includeEmptyCollections && group.Members?.Count == 0)
            {
                group.Members = null;
            }

            if (!includeEmptyCollections && group.MemberOf?.Count == 0)
            {
                group.MemberOf = null;
            }
        }

        internal static void CleanupUser(User? user, bool includeEmptyCollections)
        {
            if (user == null)
            {
                return;
            }

            if (!includeEmptyCollections)
            {
                if (user.MemberOf?.Count == 0)
                {
                    user.MemberOf = null;
                }

                if (user.Ancestors?.Count == 0)
                {
                    user.Ancestors = null;
                }

                if (user.PasswordHistory?.Count == 0)
                {
                    user.PasswordHistory = null;
                }
                if (user.LmPasswordHistory?.Count == 0)
                {
                    user.LmPasswordHistory = null;
                }
                if (user.Certificates?.Count == 0)
                {
                    user.Certificates = null;
                }
            }

            if (user.PasswordHashes != null && string.IsNullOrEmpty(user.PasswordHashes.NtHash)
                && string.IsNullOrEmpty(user.PasswordHashes.LmHash))
            {
                user.PasswordHashes = null;
            }

            if (user.SupplementalCredentials != null && string.IsNullOrEmpty(user.SupplementalCredentials.ClearTextPassword)
                && (user.SupplementalCredentials.KerberosKeys == null || user.SupplementalCredentials.KerberosKeys.Count == 0))
            {
                user.SupplementalCredentials = null;
            }
        }

        internal static bool ShouldIncludeComputer(Computer? computer, FilterOptions options)
        {
            if (computer == null)
            {
                return false;
            }

            if (computer.IsDeleted && !options.IncludeDeleted)
            {
                return false;
            }

            return !options.ExcludeComputers;
        }

        internal static bool ShouldIncludeGroup(Group? group, FilterOptions options)
        {
            if (group == null)
            {
                return false;
            }

            if (group.IsDeleted && !options.IncludeDeleted)
            {
                return false;
            }

            return !options.ExcludeGroups;
        }

        internal static bool ShouldIncludeUser(User? user, FilterOptions? options)
        {
            if (user == null || options == null)
            {
                return false;
            }

            if (user.IsDeleted && !options.IncludeDeleted)
            {
                return false;
            }

            if (options.ExcludeDisabled && user.UserAccountControl?.Contains("ACCOUNTDISABLE") == true)
            {
                return false;
            }

            if (options.ExcludeLockedOut && user.UserAccountControl?.Contains("LOCKOUT") == true)
            {
                return false;
            }

            return true;
        }

        internal class FilterOptions
        {
            public bool ExcludeComputers { get; set; } = false;
            public bool ExcludeDisabled { get; set; } = false;
            public bool ExcludeGroups { get; set; } = false;
            public bool ExcludeLockedOut { get; set; } = false;
            public bool IncludeDeleted { get; set; } = false;
            public bool IncludeEmptyCollections { get; set; } = false;
        }
    }
}
