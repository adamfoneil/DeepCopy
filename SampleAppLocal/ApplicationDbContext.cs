using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SampleApp;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
	public DbSet<AppointmentTemplate> AppointmentTemplates { get; set; }
	public DbSet<Service> Services { get; set; }
	public DbSet<Client> Clients { get; set; }
	public DbSet<VolumeClient> VolumeClients { get; set; }
	public DbSet<Appointment> Appointments { get; set; }
	public DbSet<Patient> Patients { get; set; }
	public DbSet<PatientService> PatientServices { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
	}
}

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args) =>
		new(new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseSqlServer("Server=(localdb)\\msssqllocaldb;Database=DeepCopyLocal;Integrated Security=true").Options);
}