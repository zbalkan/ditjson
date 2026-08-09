using System;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class ObjectClassifier
    {
        private const int UserObjectClass = 0x7d;
        private const int GroupObjectClass = 0x73;
        private const int ComputerObjectClass = 0x6c;
        private const int DeletedObjectClass = 0x87;
        private const int InetOrgPersonClass = 0x75;

        internal static string GetObjectClass(Session session, JET_TABLEID table,
            ColumnInfo objectClassColumn)
        {
            try
            {
                var objectClassValue = GetColumnAsInt32(session, table, objectClassColumn);

                return objectClassValue switch
                {
                    UserObjectClass => "user",
                    GroupObjectClass => "group",
                    ComputerObjectClass => "computer",
                    DeletedObjectClass => "deletedObject",
                    InetOrgPersonClass => "inetOrgPerson",
                    _ => "unknown"
                };
            }
            catch
            {
                return "unknown";
            }
        }

        internal static bool IsUserObject(int objectClassId) => objectClassId == UserObjectClass || objectClassId == InetOrgPersonClass;

        internal static bool IsGroupObject(int objectClassId) => objectClassId == GroupObjectClass;

        internal static bool IsComputerObject(int objectClassId) => objectClassId == ComputerObjectClass;

        internal static bool IsDeletedObject(int objectClassId) => objectClassId == DeletedObjectClass;

        private static int GetColumnAsInt32(Session session, JET_TABLEID table,
            ColumnInfo column)
        {
            try
            {
                var value = Api.RetrieveColumnAsInt32(session, table, column.Columnid);
                return value ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
