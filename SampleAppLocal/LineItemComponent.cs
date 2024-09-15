namespace SampleAppLocal;

public class LineItemComponent
{
	public int Id { get; set; }
	public int LineItemId { get; set; }
	public string PartNumber { get; set; } = default!;	
	public decimal UnitCost { get; set; }

	public LineItem? LineItem { get; set; }
}
