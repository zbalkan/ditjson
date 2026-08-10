using System.Collections.Generic;

namespace ditjson.Decoders
{
    internal static class FlagsDecoder
    {
        internal static string DecodeGroupType(int groupType)
        {
            if ((groupType & 0x80000000) != 0)
            {
                return "SAM_DOMAIN_GROUP";
            }

            if ((groupType & 0x00000002) != 0)
            {
                return "SAM_LOCAL_GROUP";
            }

            if ((groupType & 0x00000001) != 0)
            {
                return "SAM_SECURITY_GROUP";
            }

            return "SAM_GLOBAL_GROUP";
        }

        internal static string DecodeSAMAccountType(int samType) => samType switch {
            0x30000000 => "User",
            0x30000001 => "Computer",
            0x30000002 => "Trust",
            0x10000000 => "SAM_GROUP_OBJECT",
            0x10000001 => "SAM_NON_SECURITY_GROUP_OBJECT",
            0x20000000 => "SAM_ALIAS_OBJECT",
            0x20000001 => "SAM_NON_SECURITY_ALIAS_OBJECT",
            0x40000000 => "SAM_APP_BASIC_GROUP",
            0x40000001 => "SAM_APP_QUERY_GROUP",
            _ => "Unknown"
        };

        internal static List<string> DecodeUAC(int uacValue)
        {
            var flags = new List<string>();

            if ((uacValue & 0x0001) != 0)
            {
                flags.Add("SCRIPT");
            }

            if ((uacValue & 0x0002) != 0)
            {
                flags.Add("ACCOUNTDISABLE");
            }

            if ((uacValue & 0x0008) != 0)
            {
                flags.Add("HOMEDIR_REQUIRED");
            }

            if ((uacValue & 0x0010) != 0)
            {
                flags.Add("LOCKOUT");
            }

            if ((uacValue & 0x0020) != 0)
            {
                flags.Add("PWD_NOTREQD");
            }

            if ((uacValue & 0x0040) != 0)
            {
                flags.Add("PASSWD_CANT_CHANGE");
            }

            if ((uacValue & 0x0080) != 0)
            {
                flags.Add("ENCRYPTED_TEXT_PWD_ALLOWED");
            }

            if ((uacValue & 0x0200) != 0)
            {
                flags.Add("NORMAL_ACCOUNT");
            }

            if ((uacValue & 0x0800) != 0)
            {
                flags.Add("INTERDOMAIN_TRUST_ACCOUNT");
            }

            if ((uacValue & 0x1000) != 0)
            {
                flags.Add("WORKSTATION_TRUST_ACCOUNT");
            }

            if ((uacValue & 0x2000) != 0)
            {
                flags.Add("SERVER_TRUST_ACCOUNT");
            }

            if ((uacValue & 0x10000) != 0)
            {
                flags.Add("DONT_EXPIRE_PASSWORD");
            }

            if ((uacValue & 0x40000) != 0)
            {
                flags.Add("SMARTCARD_REQUIRED");
            }

            if ((uacValue & 0x80000) != 0)
            {
                flags.Add("TRUSTED_FOR_DELEGATION");
            }

            if ((uacValue & 0x100000) != 0)
            {
                flags.Add("NOT_DELEGATED");
            }

            if ((uacValue & 0x200000) != 0)
            {
                flags.Add("USE_DES_KEY_ONLY");
            }

            if ((uacValue & 0x400000) != 0)
            {
                flags.Add("DONT_REQ_PREAUTH");
            }

            if ((uacValue & 0x800000) != 0)
            {
                flags.Add("PASSWORD_EXPIRED");
            }

            if ((uacValue & 0x1000000) != 0)
            {
                flags.Add("TRUSTED_TO_AUTH_FOR_DELEGATION");
            }

            return flags;
        }
    }
}
