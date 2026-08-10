namespace ditjson.Extractors
{
    /// <summary>
    /// Physical datatable column names for attributes used during credential extraction.
    /// Keep the syntax prefix (for example, ATTk) as well as the numeric attribute ID:
    /// ESE looks up the complete physical name.
    /// </summary>
    internal static class NtdsColumnNames
    {
        internal const string AccountExpires = "ATTq589983";
        internal const string Ancestors = "Ancestors_col";
        internal const string BadPasswordCount = "ATTj589836";
        internal const string BadPasswordTime = "ATTq589873";
        internal const string BitLockerKeyPackage = "ATTk591823";
        internal const string BitLockerRecoveryGuid = "ATTk591789";
        internal const string BitLockerRecoveryPassword = "ATTm591788";
        internal const string BitLockerVolumeGuid = "ATTk591822";
        internal const string DialInAccessPermission = "ATTi590943";
        internal const string DnsHostName = "ATTm590443";
        internal const string IsDeleted = "ATTi131120";
        internal const string LastLogon = "ATTq589876";
        internal const string LastLogonTimestamp = "ATTq591520";
        internal const string LmHash = "ATTk589879";
        internal const string LmHashHistory = "ATTk589984";
        internal const string LogonCount = "ATTj589993";
        internal const string NtHash = "ATTk589914";
        internal const string NtHashHistory = "ATTk589918";
        internal const string ObjectGuid = "ATTk589826";
        internal const string ObjectName = "ATTm589825";
        internal const string ObjectSid = "ATTr589970";
        internal const string OperatingSystem = "ATTm590187";
        internal const string OperatingSystemVersion = "ATTm590188";
        internal const string PasswordLastSet = "ATTq589920";
        internal const string PekList = "ATTk590689";
        internal const string PrimaryGroupId = "ATTj589922";
        internal const string SamAccountName = "ATTm590045";
        internal const string SamAccountType = "ATTj590126";
        internal const string SupplementalCredentials = "ATTk589949";
        internal const string UserAccountControl = "ATTj589832";
        internal const string UserCertificate = "ATTk36";
        internal const string UserPrincipalName = "ATTm590480";
        internal const string WhenChanged = "ATTl131075";
        internal const string WhenCreated = "ATTl131074";
    }
}
