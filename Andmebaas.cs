using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

// SQLite kasutab EnsureCreated, mis olemasolevat andmebaasi ei uuenda. Siin lisatakse puuduvad osad
// (kirjeldus ja müügitabel), ilma et kasutajate ja toodete andmed kaoksid.
internal static class Andmebaas
{
    internal static bool UuendaSqlite(ToodedContext db)
    {
        bool muudetud = false;
        db.Database.OpenConnection();
        try
        {
            var conn = db.Database.GetDbConnection();
            if (!OnTulp(conn, "Toodetabel", "Kirjeldus"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"Toodetabel\" ADD COLUMN \"Kirjeldus\" TEXT;");
                muudetud = true;
            }
            if (!OnTabel(conn, "Muugid"))
            {
                db.Database.ExecuteSqlRaw(@"CREATE TABLE ""Muugid"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Muugid"" PRIMARY KEY AUTOINCREMENT,
    ""ToodeId"" INTEGER NOT NULL,
    ""Kogus"" INTEGER NOT NULL,
    ""Aeg"" TEXT NOT NULL,
    ""Demo"" INTEGER NOT NULL,
    CONSTRAINT ""FK_Muugid_Toodetabel_ToodeId"" FOREIGN KEY (""ToodeId"") REFERENCES ""Toodetabel"" (""Id"") ON DELETE CASCADE);");
                db.Database.ExecuteSqlRaw("CREATE INDEX \"IX_Muugid_ToodeId\" ON \"Muugid\" (\"ToodeId\");");
                muudetud = true;
            }
        }
        finally { db.Database.CloseConnection(); }
        return muudetud;
    }

    private static bool OnTulp(DbConnection conn, string tabel, string tulp)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tabel}') WHERE name = '{tulp}'";
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool OnTabel(DbConnection conn, string tabel)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{tabel}'";
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
