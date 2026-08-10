using System;
using System.Collections.Generic;

namespace ditjson.Models
{
    public class BitLockerRecovery
    {
        public Guid RecoveryGuid { get; set; }
        public string? RecoveryPassword { get; set; }
        public Guid VolumeGuid { get; set; }
    }

    public class Computer : NtdsObject
    {
        public int DialInAccessPermission { get; set; }
        public string? DnsHostName { get; set; }
        public List<GroupMembership>? MemberOf { get; set; }
        public string? OperatingSystem { get; set; }
        public string? OperatingSystemVersion { get; set; }
        public PasswordHashes? PasswordHashes { get; set; }
        public string? PasswordLastSet { get; set; }
        public BitLockerRecovery? Recovery { get; set; }
        public string? SamAccountName { get; set; }
        public SupplementalCredentials? SupplementalCredentials { get; set; }
    }

    public class Group : NtdsObject
    {
        public string? GroupType { get; set; }
        public List<GroupMember>? Members { get; set; }
        public string? SamAccountName { get; set; }
    }

    public class GroupMember
    {
        public string? DeletedTime { get; set; }
        public bool IsPrimaryGroup { get; set; }
        public string? Name { get; set; }
        public string? ObjectClass { get; set; }
        public Guid ObjectGuid { get; set; }
        public int RecordId { get; set; }
    }

    public class GroupMembership
    {
        public string? DeletedTime { get; set; }
        public bool IsPrimaryGroup { get; set; }
        public string? Name { get; set; }
        public Guid ObjectGuid { get; set; }
        public string? ObjectSid { get; set; }
        public int RecordId { get; set; }
    }

    public class KerberosKey
    {
        public string? Algorithm { get; set; }
        public string? Key { get; set; }
    }

    public class NtdsObject
    {
        public bool IsDeleted { get; set; }
        public string? Name { get; set; }
        public string? ObjectClass { get; set; }
        public Guid ObjectGuid { get; set; }
        public string? ObjectSid { get; set; }
        public int RecordId { get; set; }
        public string? WhenChanged { get; set; }
        public string? WhenCreated { get; set; }
    }

    public class PasswordHashes
    {
        public string? LmHash { get; set; }
        public string? NtHash { get; set; }
    }

    public class SupplementalCredentials
    {
        public string? ClearTextPassword { get; set; }
        public List<KerberosKey>? KerberosKeys { get; set; }
    }

    public class User : NtdsObject
    {
        public string? AccountExpires { get; set; }
        public List<NtdsObject>? Ancestors { get; set; }
        public int BadPwdCount { get; set; }
        public string? BadPwdTime { get; set; }
        public string? Certificate { get; set; }
        public int DialInAccessPermission { get; set; }
        public string? LastLogon { get; set; }
        public string? LastLogonTimeStamp { get; set; }
        public int LogonCount { get; set; }
        public List<GroupMembership>? MemberOf { get; set; }
        public PasswordHashes? PasswordHashes { get; set; }
        public List<string>? LmPasswordHistory { get; set; }
        public List<string>? PasswordHistory { get; set; }
        public string? PasswordLastSet { get; set; }
        public int PrimaryGroupId { get; set; }
        public string? SamAccountName { get; set; }
        public string? SamAccountType { get; set; }
        public SupplementalCredentials? SupplementalCredentials { get; set; }
        public List<string>? UserAccountControl { get; set; }
        public string? UserPrincipalName { get; set; }
    }
}
