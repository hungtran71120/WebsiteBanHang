using Microsoft.Data.SqlClient;
using Npgsql;

const string SqlServerConnection =
    "Server=.;Database=HungStoreDb;Trusted_Connection=True;TrustServerCertificate=True";

const string PostgreSqlConnection =
    "Host=aws-0-ap-southeast-2.pooler.supabase.com;" +
    "Port=5432;" +
    "Database=postgres;" +
    "Username=postgres.wweyclunhbplusmlokrk;" +
    "Password=Hu01629206440;" +
    "SSL Mode=Require";

var tables = new[]
{
    // Identity
    "AspNetRoles",
    "AspNetUsers",
    "AspNetRoleClaims",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserRoles",
    "AspNetUserTokens",

    // Product
    "Categories",
    "Products",
    "ProductVariantOptions",
    "ProductVariantOptionValues",
    "ProductVariants",

    // Other product-related
    "Banners",

    // Cart
    "Carts",
    "CartItems",

    // Wishlist
    "Wishlists",
    "WishlistItems",

    // Chat
    "Conversations",
    "ChatMessages",

    // Notification
    "Notifications",

    // Flash Sale
    "FlashSales",
    "FlashSaleItems",

    // Voucher
    "Vouchers",
    "VoucherRedemptions",

    // Orders
    "Orders",
    "OrderItems",
    "OrderStatusHistories",

    // Reviews
    "Reviews",

    // Refresh token
    "RefreshTokens"
};

await using var sqlConnection =
    new SqlConnection(SqlServerConnection);

await sqlConnection.OpenAsync();

await using var pgConnection =
    new NpgsqlConnection(PostgreSqlConnection);

await pgConnection.OpenAsync();

Console.WriteLine("======================================");
Console.WriteLine(" HungStore Data Migration");
Console.WriteLine(" SQL Server -> Supabase PostgreSQL");
Console.WriteLine("======================================");
Console.WriteLine();


// =====================================================
// 1. Disable Foreign Keys
// =====================================================

// =====================================================
// 2. Migrate data
// =====================================================

foreach (var table in tables)
{
    await MigrateTable(
        sqlConnection,
        pgConnection,
        table);
}

// =====================================================
// 4. Reset sequences
// =====================================================

Console.WriteLine();
Console.WriteLine("Resetting sequences...");

await ResetSequences(pgConnection, tables);

Console.WriteLine();
Console.WriteLine("======================================");
Console.WriteLine(" Migration completed!");
Console.WriteLine("======================================");


static async Task MigrateTable(
    SqlConnection sqlConnection,
    NpgsqlConnection pgConnection,
    string tableName)
{
    Console.WriteLine($"Migrating: {tableName}");

    // -------------------------------------------------
    // Get PostgreSQL columns
    // -------------------------------------------------

    var columns = new List<string>();

    await using (var columnCommand = new NpgsqlCommand(
        """
        SELECT column_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = @table
        ORDER BY ordinal_position
        """,
        pgConnection))
    {
        columnCommand.Parameters.AddWithValue(
            "table",
            tableName);

        await using var reader =
            await columnCommand.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
    }

    if (columns.Count == 0)
    {
        Console.WriteLine(
            $"  WARNING: {tableName} not found.");

        return;
    }


    // -------------------------------------------------
    // Read SQL Server
    // -------------------------------------------------

    var sqlColumns = string.Join(
        ", ",
        columns.Select(c => $"[{c}]"));

    var selectSql =
        $"SELECT {sqlColumns} FROM [{tableName}]";


    await using var sqlCommand =
        new SqlCommand(
            selectSql,
            sqlConnection);

    await using var sqlReader =
        await sqlCommand.ExecuteReaderAsync();


    // -------------------------------------------------
    // PostgreSQL INSERT
    // -------------------------------------------------

    var pgColumns = string.Join(
        ", ",
        columns.Select(c => $"\"{c}\""));

    var parameters = string.Join(
        ", ",
        columns.Select((_, i) => $"@p{i}"));

    var insertSql =
        $"INSERT INTO \"{tableName}\" " +
        $"({pgColumns}) VALUES ({parameters})";


    var rowCount = 0;

    while (await sqlReader.ReadAsync())
    {
        await using var insertCommand =
            new NpgsqlCommand(
                insertSql,
                pgConnection);

        for (var i = 0; i < columns.Count; i++)
        {
            var value =
                sqlReader.IsDBNull(i)
                    ? DBNull.Value
                    : sqlReader.GetValue(i);

            insertCommand.Parameters.AddWithValue(
                $"p{i}",
                value);
        }

        await insertCommand.ExecuteNonQueryAsync();

        rowCount++;
    }

    Console.WriteLine(
        $"  OK: {rowCount} rows");
}


static async Task ResetSequences(
    NpgsqlConnection connection,
    string[] tables)
{
    foreach (var table in tables)
    {
        // Find identity/serial columns
        var sql = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @table
              AND (
                  is_identity = 'YES'
                  OR column_default LIKE 'nextval%'
              )
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "table",
            table);

        var identityColumns = new List<string>();

        await using (var reader =
            await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                identityColumns.Add(
                    reader.GetString(0));
            }
        }


        foreach (var column in identityColumns)
        {
            var sequenceSql = $"""
                SELECT pg_get_serial_sequence(
                    '"{table}"',
                    '{column}'
                )
                """;

            await using var sequenceCommand =
                new NpgsqlCommand(
                    sequenceSql,
                    connection);

            var sequence =
                await sequenceCommand
                    .ExecuteScalarAsync();

            if (sequence == null ||
                sequence == DBNull.Value)
            {
                continue;
            }

            var resetSql = $"""
                SELECT setval(
                    '{sequence}',
                    COALESCE(
                        (SELECT MAX("{column}")
                         FROM "{table}"),
                        1
                    ),
                    true
                )
                """;

            await using var resetCommand =
                new NpgsqlCommand(
                    resetSql,
                    connection);

            await resetCommand.ExecuteScalarAsync();

            Console.WriteLine(
                $"  Sequence reset: {table}.{column}");
        }
    }
}