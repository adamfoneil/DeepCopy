namespace SampleApp;

public enum SpeciesOptions
{
	Cat = 1,
	Dog = 2
}

public class TemplatePatient
{
	public int Id { get; set; }
	public int AppointmentTemplateId { get; set; }
	public char Sex { get; set; }
	public SpeciesOptions Species { get; set; }
	public ICollection<TemplateService> Services { get; set; } = [];
}
