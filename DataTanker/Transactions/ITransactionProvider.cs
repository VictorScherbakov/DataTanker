namespace DataTanker.Transactions
{
    internal interface ITransactionProvider
    {
        DataTankerTransaction Current { get; set; }

        DataTankerTransaction Create(ITransactionInventory inventory);
    }
}