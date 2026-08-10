namespace ditjson.Extractors
{
    /// <summary>
    /// Classifies security principals by their stable sAMAccountType value.
    ///
    /// Values in objectClass are schema-record references and are not portable
    /// between NTDS databases. sAMAccountType, on the other hand, is an enum
    /// whose values are defined by Active Directory.
    /// </summary>
    internal static class ObjectClassifier
    {
        private const int AliasObject = 0x20000000;
        private const int GroupObject = 0x10000000;
        private const int MachineAccount = 0x30000001;
        private const int NonSecurityAliasObject = 0x20000001;
        private const int NonSecurityGroupObject = 0x10000001;
        private const int TrustAccount = 0x30000002;
        private const int UserObject = 0x30000000;

        internal static bool IsComputerObject(int samAccountType) => samAccountType == MachineAccount;

        internal static bool IsGroupObject(int samAccountType) =>
            samAccountType == GroupObject ||
            samAccountType == NonSecurityGroupObject ||
            samAccountType == AliasObject ||
            samAccountType == NonSecurityAliasObject;

        internal static bool IsUserObject(int samAccountType) =>
                            samAccountType == UserObject || samAccountType == TrustAccount;
    }
}
