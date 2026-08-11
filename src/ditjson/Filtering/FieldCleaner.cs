using ditjson.Models;

namespace ditjson.Filtering
{
    internal static class FieldCleaner
    {
        internal static void CleanComputer(Computer? computer)
        {
            if (computer == null)
            {
                return;
            }

            computer.Name = CleanString(computer.Name)!;
            computer.SamAccountName = CleanString(computer.SamAccountName)!;
            computer.DnsHostName = CleanString(computer.DnsHostName)!;
            computer.OperatingSystem = CleanString(computer.OperatingSystem)!;
            computer.OperatingSystemVersion = CleanString(computer.OperatingSystemVersion)!;

            if (computer.DialInAccessPermission < 0)
            {
                computer.DialInAccessPermission = 0;
            }

            if (string.IsNullOrEmpty(computer.ObjectSid))
            {
                computer.ObjectSid = null;
            }
        }

        internal static void CleanGroup(Group? group)
        {
            if (group == null)
            {
                return;
            }

            group.Name = CleanString(group.Name);
            group.SamAccountName = CleanString(group.SamAccountName);
            group.GroupType = CleanString(group.GroupType);

            if (string.IsNullOrEmpty(group.ObjectSid))
            {
                group.ObjectSid = null;
            }
        }

        internal static void CleanUser(User? user)
        {
            if (user == null)
            {
                return;
            }

            user.Name = CleanString(user.Name);
            user.SamAccountName = CleanString(user.SamAccountName);
            user.UserPrincipalName = CleanString(user.UserPrincipalName);
            user.SamAccountType = CleanString(user.SamAccountType);
            if (user.PrimaryGroupId <= 0)
            {
                user.PrimaryGroupId = 0;
            }

            if (user.LogonCount < 0)
            {
                user.LogonCount = 0;
            }

            if (user.BadPwdCount < 0)
            {
                user.BadPwdCount = 0;
            }

            if (user.DialInAccessPermission < 0)
            {
                user.DialInAccessPermission = 0;
            }

            if (string.IsNullOrEmpty(user.ObjectSid))
            {
                user.ObjectSid = null;
            }

            user.UserAccountControl?.RemoveAll(string.IsNullOrWhiteSpace);
            if (user.UserAccountControl?.Count == 0)
            {
                user.UserAccountControl = null;
            }
        }

        private static string? CleanString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }
    }
}
