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
                Name = ColumnExtractor.GetString(session, table, columnDict, "ATTm589825")!,
                ObjectClass = "user",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTk589826")),
                ObjectSid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectSid)),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.SamAccountName)!,
                UserPrincipalName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590480")!,

                WhenCreated = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTl131074"))!,
                WhenChanged = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTl131075"))!,

                PasswordLastSet = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589920"))!,
                LastLogon = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589876"))!,
                LastLogonTimeStamp = TimestampDecoder.DecodeFromInt64(
                    ColumnExtractor.GetInt64(session, table, columnDict,
                        "ATTq591520"))!,
                AccountExpires = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589983"))!,
                BadPwdTime = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589873"))!,

                LogonCount = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj589993"),
                BadPwdCount = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj589836"),
                PrimaryGroupId = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj589922"),
                DialInAccessPermission = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTi590943"),

                IsDeleted = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTi131120") != 0
            };

            var uacValue = ColumnExtractor.GetInt32(session, table, columnDict,
                "ATTj589832");
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
            user.PasswordHistory = [];

            return user;
        }
    }
}
