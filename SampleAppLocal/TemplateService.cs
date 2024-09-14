using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SampleApp;

public class TemplateService
{
	public int Id { get; set; }
	public int TemplatePatientId { get; set; }
	public int ServiceId { get; set; }
	public int? BillToClientId { get; set; }
	public decimal Price { get; set; }
}

public class TemplateServiceConfiguration : IEntityTypeConfiguration<TemplateService>
{
	public void Configure(EntityTypeBuilder<TemplateService> builder)
	{
		builder
			.HasOne<VolumeClient>()
			.WithMany()
			.HasForeignKey(x => x.BillToClientId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}