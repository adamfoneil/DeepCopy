using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SampleApp;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{	
	public DbSet<Service> Services { get; set; }
	public DbSet<Client> Clients { get; set; }
	public DbSet<VolumeClient> VolumeClients { get; set; }
	public DbSet<Appointment> Appointments { get; set; }
	public DbSet<Patient> Patients { get; set; }
	public DbSet<PatientService> PatientServices { get; set; }
	public DbSet<TemplatePatient> TemplatePatients { get; set; }

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