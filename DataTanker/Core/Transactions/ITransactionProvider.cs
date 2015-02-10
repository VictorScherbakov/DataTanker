namespace DataTanker.Transactions
{
    using Versioning;

    internal interface ITransactionProvider
    {
        DataTankerTransaction Current { get; set; }

        DataTankerTransaction Create(ITransactionInventory inventory);
    }
}