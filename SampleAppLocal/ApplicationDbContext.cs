using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SampleAppLocal;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
	public DbSet<Order> Orders { get; set; }
	public DbSet<LineItem> LineItems { get; set; }
	public DbSet<LineItemComponent> LineItemComponents { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
	}
}

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	/// <summary>
	/// I was having trouble with the connection string, so I made this method to test it from another project
	/// </summary>
	public static string GetConnectionString(string dbName) => $"Server=(localdb)\\mssqllocaldb;Database={dbName};Integrated Security=true";

	public ApplicationDbContext CreateDbContext(string[] args) =>
		new(new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseSqlServer(GetConnectionString("DeepCopyLocal")).Options);
}