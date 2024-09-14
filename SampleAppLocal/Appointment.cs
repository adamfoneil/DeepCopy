namespace SampleApp;

public class Appointment
{
	public int Id { get; set; }
	public int ClientId { get; set; }
	public DateTime Date { get; set; }

	public Client Client { get; set; } = new();
	public ICollection<Patient> Patients { get; set; } = [];
}

public class Patient
{
	public int Id { get; set; }
	public int AppointmentId { get; set; }
	public string Name { get; set; } = default!;
	public char Sex { get; set; }
	public SpeciesOptions Species { get; set; }

	public Appointment Appointment { get; set; } = new();
	public ICollection<PatientService> Services { get; set; } = [];
}

public class PatientService
{
	public int Id { get; set; }
	public int PatientId { get; set; }
	public int ServiceId { get; set; }
	public int? BillToClientId { get; set; } // if null then bill to owner
	public decimal Price { get; set; }

	public Service Service { get; set; } = new();
	public Patient Patient { get; set; } = new();
}