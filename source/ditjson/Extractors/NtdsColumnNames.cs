namespace ditjson.Extractors
{
    /// <summary>
    /// Physical datatable column names for attributes used during credential extraction.
    /// Keep the syntax prefix (for example, ATTk) as well as the numeric attribute ID:
    /// ESE looks up the complete physical name.
    /// </summary>
    internal static class NtdsColumnNames
    {
        internal const string SamAccountName = "ATTm590045";
        internal const string SamAccountType = "ATTj590126";
        internal const string ObjectSid = "ATTr589970";

        internal const string LmHash = "ATTk589879";
        internal const string NtHash = "ATTk589914";
        internal const string PekList = "ATTk590689";
        internal const string LmHashHistory = "ATTk589984";
        internal const string NtHashHistory = "ATTk589918";
        internal const string SupplementalCredentials = "ATTk589949";

        internal const string BitLockerKeyPackage = "ATTk591823";
        internal const string BitLockerRecoveryGuid = "ATTk591789";
        internal const string BitLockerRecoveryPassword = "ATTm591788";
        internal const string BitLockerVolumeGuid = "ATTk591822";
    }
}
