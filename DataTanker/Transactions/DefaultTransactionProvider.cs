namespace DataTanker.Transactions
{
    using Versioning;

    internal class DefaultTransactionProvider : ITransactionProvider
    {
        public DataTankerTransaction Current
        {
            get => DataTankerTransaction.Current;
            set => DataTankerTransaction.Current = value;
        }

        public DataTankerTransaction Create(ITransactionInventory inventory)
        {
            int number = inventory.CreateTransaction(out var snapshotData);
            return new DataTankerTransaction(inventory, snapshotData, number);
        }
    }
}