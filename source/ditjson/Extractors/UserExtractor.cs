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
                Name = ColumnExtractor.GetString(session, table, columnDict, NtdsColumnNames.ObjectName)!,
                ObjectClass = "user",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectGuid)),
                ObjectSid = SidDecoder.DecodeNtds(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectSid)),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.SamAccountName)!,
                UserPrincipalName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.UserPrincipalName)!,

                WhenCreated = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.WhenCreated))!,
                WhenChanged = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.WhenChanged))!,

                PasswordLastSet = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.PasswordLastSet))!,
                LastLogon = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.LastLogon))!,
                LastLogonTimeStamp = TimestampDecoder.DecodeFromInt64(
                    ColumnExtractor.GetInt64(session, table, columnDict,
                        NtdsColumnNames.LastLogonTimestamp))!,
                AccountExpires = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.AccountExpires))!,
                BadPwdTime = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.BadPasswordTime))!,

                LogonCount = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.LogonCount),
                BadPwdCount = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.BadPasswordCount),
                PrimaryGroupId = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.PrimaryGroupId),
                DialInAccessPermission = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.DialInAccessPermission),

                IsDeleted = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.IsDeleted) != 0
            };

            var uacValue = ColumnExtractor.GetInt32(session, table, columnDict,
                NtdsColumnNames.UserAccountControl);
            if (uacValue != 0)
            {
                user.UserAccountControl = FlagsDecoder.DecodeUAC(uacValue);
            }
            else
            {
                user.UserAccountControl = [];
            }

            var samAccountTypeValue = ColumnExtractor.GetInt32(session, table, columnDict,
                NtdsColumnNames.SamAccountType);
            if (samAccountTypeValue != 0)
            {
                user.SamAccountType = FlagsDecoder.DecodeSAMAccountType(samAccountTypeValue);
            }

            user.Ancestors = [];
            user.MemberOf = [];

            return user;
        }
    }
}
