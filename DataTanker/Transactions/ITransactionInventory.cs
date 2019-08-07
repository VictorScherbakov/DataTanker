namespace DataTanker.Transactions
{
    using Versioning;

    internal interface ITransactionInventory
    {
        void Mark(int number, TransactionState newState);

        int CreateTransaction(out ISnapshotData snapshotData);

        void MarkActivesAsRolledBack();

        TransactionState GetState(int number);
    }
}
