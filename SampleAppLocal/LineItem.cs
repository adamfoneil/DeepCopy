namespace SampleAppLocal;

public class LineItem
{
	public int Id { get; set; }
	public int OrderId { get; set; }
	public string Description { get; set; } = default!;
	public decimal UnitPrice { get; set; }
	public int Quantity { get; set; }

	public Order? Order { get; set; }
	public ICollection<LineItemComponent> Components { get; set; } = [];
}
