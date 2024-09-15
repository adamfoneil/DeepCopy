namespace SampleAppLocal;

public enum OrderStatus
{
	Quoted,
	Ordered,
	Shipped
}

public class Order
{
	public int Id { get; set; }
	public string Customer { get; set; } = default!;
	public DateTime Date { get; set; }
	public OrderStatus Status { get; set; }

	public ICollection<LineItem> LineItems { get; set; } = [];
}
