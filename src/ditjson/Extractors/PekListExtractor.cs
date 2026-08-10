using System;
using System.Collections.Generic;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class PekListExtractor
    {
        internal static List<byte[]> Extract(Session session, JET_DBID dbid, byte[] bootkey)
        {
            using var table = new Table(session, dbid, "datatable", OpenTableGrbit.ReadOnly);
            var columns = Api.GetColumnDictionary(session, table);
            if (!columns.ContainsKey(NtdsColumnNames.PekList))
            {
                throw new InvalidOperationException("NTDS datatable has no pekList column");
            }

            Api.MoveBeforeFirst(session, table);
            while (Api.TryMoveNext(session, table))
            {
                var blob = ColumnExtractor.GetBinary(session, table, columns, NtdsColumnNames.PekList);
                if (blob != null && blob.Length >= 24)
                {
                    return CredentialCrypto.DecryptPekList(blob, bootkey);
                }
            }
            throw new InvalidOperationException("No PEK list was found in the NTDS datatable");
        }
    }
}
