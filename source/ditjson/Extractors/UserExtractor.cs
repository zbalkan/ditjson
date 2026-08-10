using System;
using System.Collections.Generic;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;
using ditjson.Decoders;

namespace ditjson.Extractors
{
    internal static class UserExtractor
    {
        internal static User ExtractUser(Session session, JET_TABLEID table, int recordId,
            IDictionary<string, JET_COLUMNID> columnDict)
        {
            var user = new User
            {
                RecordId = recordId,
                Name = ColumnExtractor.GetString(session, table, columnDict, "ATTm131220")!,
                ObjectClass = "user",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTb131353")),
                ObjectSid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTr589970")),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590045")!,
                UserPrincipalName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590480")!,

                WhenCreated = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591520"))!,
                WhenChanged = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591521"))!,

                PasswordLastSet = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589926"))!,
                LastLogon = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589876"))!,
                LastLogonTimeStamp = TimestampDecoder.DecodeFromInt64(
                    ColumnExtractor.GetInt64(session, table, columnDict,
                        "ATTq591983"))!,
                AccountExpires = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589960"))!,
                BadPwdTime = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591923"))!,

                LogonCount = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj589875"),
                BadPwdCount = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj589874"),
                PrimaryGroupId = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj590077"),
                DialInAccessPermission = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj590093"),

                IsDeleted = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTb589825") != null
            };

            var uacValue = ColumnExtractor.GetInt32(session, table, columnDict,
                "ATTj590084");
            if (uacValue != 0)
            {
                user.UserAccountControl = FlagsDecoder.DecodeUAC(uacValue);
            }
            else
            {
                user.UserAccountControl = [];
            }

            var samAccountTypeValue = ColumnExtractor.GetInt32(session, table, columnDict,
                "ATTj590046");
            if (samAccountTypeValue != 0)
            {
                user.SamAccountType = FlagsDecoder.DecodeSAMAccountType(samAccountTypeValue);
            }

            user.Ancestors = [];
            user.MemberOf = [];
            user.PasswordHistory = [];

            return user;
        }
    }
}
