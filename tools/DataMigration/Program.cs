using AtelieBebe.Backoffice.Core.Infrastructure.Persistence;
using AtelieBebe.Catalog.Core.Infrastructure.Persistence;
using AtelieBebe.Identity.Core.Infrastructure.Persistence;
using AtelieBebe.Orders.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// One-shot data migration: SQLite (old) -> SQL Server (new), one service at a time, preserving
// every GUID primary key exactly as it was (cross-service references, e.g. OrderItem.ProductId
// pointing at Catalog, are plain GUIDs with no real FK constraint across service boundaries — if
// the values don't match exactly, those references silently break).
//
// Usage: dotnet run -- <sqliteDir> <sqlServerHost> <saPassword>
//   sqliteDir    directory containing identity.db, catalog.db, orders.db, backoffice.db
//   sqlServerHost e.g. "127.0.0.1,1433"
//   saPassword   the SQL Server "sa" password (MSSQL_SA_PASSWORD)

if (args.Length < 3)
{
    Console.WriteLine("Usage: dotnet run -- <sqliteDir> <sqlServerHost> <saPassword>");
    return 1;
}

var sqliteDir = args[0];
var sqlServerHost = args[1];
var saPassword = args[2];

string SqlServerConnStr(string db) => $"Server={sqlServerHost};Database={db};User Id=sa;Password={saPassword};TrustServerCertificate=True";
string SqliteConnStr(string file) => $"Data Source={Path.Combine(sqliteDir, file)}";

await MigrateIdentityAsync();
await MigrateCatalogAsync();
await MigrateOrdersAsync();
await MigrateBackofficeAsync();

Console.WriteLine("Done.");
return 0;

async Task MigrateIdentityAsync()
{
    Console.WriteLine("=== identity ===");
    var srcOptions = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(SqliteConnStr("identity.db")).Options;
    var dstOptions = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(SqlServerConnStr("IdentityDb")).Options;
    await using var src = new IdentityDbContext(srcOptions);
    await using var dst = new IdentityDbContext(dstOptions);

    var admins = await src.Admins.AsNoTracking().ToListAsync();
    var customers = await src.Customers.AsNoTracking().ToListAsync();
    var addresses = await src.CustomerAddresses.AsNoTracking().ToListAsync();
    var pwTokens = await src.PasswordResetTokens.AsNoTracking().ToListAsync();
    var emailTokens = await src.EmailVerificationTokens.AsNoTracking().ToListAsync();
    var outbox = await src.OutboxMessages.AsNoTracking().ToListAsync();

    dst.Admins.AddRange(admins);
    dst.Customers.AddRange(customers);
    await dst.SaveChangesAsync();
    dst.CustomerAddresses.AddRange(addresses);
    dst.PasswordResetTokens.AddRange(pwTokens);
    dst.EmailVerificationTokens.AddRange(emailTokens);
    dst.OutboxMessages.AddRange(outbox);
    await dst.SaveChangesAsync();

    Console.WriteLine($"  admins={admins.Count} customers={customers.Count} addresses={addresses.Count} pwTokens={pwTokens.Count} emailTokens={emailTokens.Count} outbox={outbox.Count}");
}

async Task MigrateCatalogAsync()
{
    Console.WriteLine("=== catalog ===");
    var srcOptions = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(SqliteConnStr("catalog.db")).Options;
    var dstOptions = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(SqlServerConnStr("CatalogDb")).Options;
    await using var src = new CatalogDbContext(srcOptions);
    await using var dst = new CatalogDbContext(dstOptions);

    // String-based Include reaches the private backing fields for Product's owned collections
    // (images, per-customer access) — they have no public DbSet of their own.
    var products = await src.Products.AsNoTracking().Include("_images").Include("_allowedCustomerAccess").ToListAsync();
    var reviews = await src.ProductReviews.AsNoTracking().ToListAsync();
    var wishlist = await src.WishlistItems.AsNoTracking().ToListAsync();
    var gallery = await src.GalleryImages.AsNoTracking().ToListAsync();
    var siteImages = await src.SiteImages.AsNoTracking().ToListAsync();
    var outbox = await src.OutboxMessages.AsNoTracking().ToListAsync();

    dst.Products.AddRange(products);
    await dst.SaveChangesAsync();
    dst.ProductReviews.AddRange(reviews);
    dst.WishlistItems.AddRange(wishlist);
    dst.GalleryImages.AddRange(gallery);
    dst.SiteImages.AddRange(siteImages);
    dst.OutboxMessages.AddRange(outbox);
    await dst.SaveChangesAsync();

    Console.WriteLine($"  products={products.Count} reviews={reviews.Count} wishlist={wishlist.Count} gallery={gallery.Count} siteImages={siteImages.Count} outbox={outbox.Count}");
}

async Task MigrateOrdersAsync()
{
    Console.WriteLine("=== orders ===");
    var srcOptions = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(SqliteConnStr("orders.db")).Options;
    var dstOptions = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlServer(SqlServerConnStr("OrdersDb")).Options;
    await using var src = new OrdersDbContext(srcOptions);
    await using var dst = new OrdersDbContext(dstOptions);

    var orders = await src.Orders.AsNoTracking().Include(o => o.Items).ToListAsync();
    var coupons = await src.Coupons.AsNoTracking().ToListAsync();
    var cartSnapshots = await src.CartSnapshots.AsNoTracking().ToListAsync();
    var outbox = await src.OutboxMessages.AsNoTracking().ToListAsync();

    dst.Orders.AddRange(orders);
    dst.Coupons.AddRange(coupons);
    dst.CartSnapshots.AddRange(cartSnapshots);
    dst.OutboxMessages.AddRange(outbox);
    await dst.SaveChangesAsync();

    Console.WriteLine($"  orders={orders.Count} items={orders.Sum(o => o.Items.Count)} coupons={coupons.Count} cartSnapshots={cartSnapshots.Count} outbox={outbox.Count}");
}

async Task MigrateBackofficeAsync()
{
    Console.WriteLine("=== backoffice ===");
    var srcOptions = new DbContextOptionsBuilder<BackofficeDbContext>().UseSqlite(SqliteConnStr("backoffice.db")).Options;
    var dstOptions = new DbContextOptionsBuilder<BackofficeDbContext>().UseSqlServer(SqlServerConnStr("BackofficeDb")).Options;
    await using var src = new BackofficeDbContext(srcOptions);
    await using var dst = new BackofficeDbContext(dstOptions);

    var contactMessages = await src.ContactMessages.AsNoTracking().ToListAsync();
    var newsletter = await src.NewsletterSubscribers.AsNoTracking().ToListAsync();
    var auditLogs = await src.AuditLogs.AsNoTracking().ToListAsync();
    var outbox = await src.OutboxMessages.AsNoTracking().ToListAsync();

    dst.ContactMessages.AddRange(contactMessages);
    dst.NewsletterSubscribers.AddRange(newsletter);
    dst.AuditLogs.AddRange(auditLogs);
    dst.OutboxMessages.AddRange(outbox);
    await dst.SaveChangesAsync();

    Console.WriteLine($"  contactMessages={contactMessages.Count} newsletter={newsletter.Count} auditLogs={auditLogs.Count} outbox={outbox.Count}");
}
