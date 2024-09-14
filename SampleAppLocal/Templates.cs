namespace SampleApp;

public class AppointmentTemplate
{
	public int Id { get; set; }	
	public string Name { get; set; } = string.Empty;
	public ICollection<TemplatePatient> Patients { get; set; } = [];
}
