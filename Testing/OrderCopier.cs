using DeepCopy.Abstractions;
using Microsoft.Extensions.Logging;
using SampleAppLocal;
using System.Data;

namespace Testing;

internal class OrderCopier(ILogger<OrderCopier> logger) : LocalDeepCopy<int>(logger)
{
	const string Orders = "Orders";
	const string LineItems = "LineItems";
	const string LineItemComponents = "LineItemComponents";

	protected override async Task<int> OnExecuteAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
	{
		await RunStepAsync<OrderStep, Order>(connection, transaction, parameters);
		await RunStepAsync<LineItemStep, LineItem>(connection, transaction, parameters);
		await RunStepAsync<LineItemComponentStep, LineItemComponent>(connection, transaction, parameters);

		return KeyMap[(Orders, parameters)];
	}

	private class OrderStep : Step<Order>
	{
		protected override string Name => Orders;

		protected override Order CreateNewRow(int parameters, Order sourceRow) => new()
		{
			Customer = sourceRow.Customer,
			Status = OrderStatus.Ordered,
			Date = sourceRow.Date
		};

		protected override int GetKey(Order sourceRow) => sourceRow.Id;

		protected override Task<int> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, Order entity, int parameters)
		{
			throw new NotImplementedException();
		}

		protected override Task<IEnumerable<Order>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
		{
			throw new NotImplementedException();
		}
	}

	private class LineItemStep : Step<LineItem>
	{
		protected override string Name => LineItems;

		protected override LineItem CreateNewRow(int parameters, LineItem sourceRow) => new()
		{
			OrderId = KeyMap[("OrderStep", sourceRow.OrderId)],
			Description = sourceRow.Description,
			UnitPrice = sourceRow.UnitPrice,
			Quantity = sourceRow.Quantity
		};

		protected override int GetKey(LineItem sourceRow) => sourceRow.Id;

		protected override Task<int> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, LineItem entity, int parameters)
		{
			throw new NotImplementedException();
		}

		protected override Task<IEnumerable<LineItem>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
		{
			throw new NotImplementedException();
		}
	}

	private class LineItemComponentStep : Step<LineItemComponent>
	{
		protected override string Name => LineItemComponents;

		protected override LineItemComponent CreateNewRow(int parameters, LineItemComponent sourceRow) => new()
		{
			LineItemId = KeyMap[("LineItemStep", sourceRow.LineItemId)],
			PartNumber = sourceRow.PartNumber,
			UnitCost = sourceRow.UnitCost
		};

		protected override int GetKey(LineItemComponent sourceRow) => sourceRow.Id;

		protected override Task<int> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, LineItemComponent entity, int parameters)
		{
			throw new NotImplementedException();
		}

		protected override Task<IEnumerable<LineItemComponent>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
		{
			throw new NotImplementedException();
		}
	}
}
