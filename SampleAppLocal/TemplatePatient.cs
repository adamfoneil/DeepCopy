namespace SampleApp;

public enum Species
{
	Cat = 1,
	Dog = 2
}

public class TemplatePatient
{
	public int Id { get; set; }
	public string Name { get; set; } = default!;
	public char Sex { get; set; }
	public Species Species { get; set; }
	public ICollection<TemplateService> Services { get; set; } = [];
}
