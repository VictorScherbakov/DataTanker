namespace DataTanker.Transactions
{
    using Versioning;

    internal class DefaultTransactionProvider : ITransactionProvider
    {
        public DataTankerTransaction Current
        {
            get { return DataTankerTransaction.Current; }
            set { DataTankerTransaction.Current = value; }
        }

        public DataTankerTransaction Create(ITransactionInventory inventory)
        {
            ISnapshotData snapshotData;
            int number = inventory.CreateTransaction(out snapshotData);
            return new DataTankerTransaction(inventory, snapshotData, number);
        }
    }
}