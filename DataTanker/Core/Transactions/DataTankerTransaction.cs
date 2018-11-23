namespace DataTanker.Transactions
{
    using System;
    using System.Transactions;

    using Versioning;

    internal class DataTankerTransaction : IEnlistmentNotification 
    {
        [ThreadStatic] 
        private static DataTankerTransaction _current;

        private readonly ITransactionInventory _inventory;

        public ISnapshotData SnapshotData { get; }

        public int Id { get; }

        public TransactionState State { get; set; }

        public void Commit()
        {
            _inventory.Mark(Id, TransactionState.Commited);
        }

        public void Rollback()
        {
            _inventory.Mark(Id, TransactionState.RolledBack);
        }

        public void Prepare()
        {
            _inventory.Mark(Id, TransactionState.Prepared);
        }

        public static DataTankerTransaction Current
        {
            get { return _current; }
            set { _current = value; }
        }

        public DataTankerTransaction(ITransactionInventory inventory, ISnapshotData snapshotData, int id)
        {
            if(id == int.MaxValue)
                throw new DataTankerException("Transaction number overflow");

            _inventory = inventory;
            SnapshotData = snapshotData ?? throw new ArgumentNullException(nameof(snapshotData));
            Id = id;

            State = TransactionState.Active;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                Prepare();
                preparingEnlistment.Prepared();
            }
            catch (Exception)
            {
                Rollback();
                preparingEnlistment.ForceRollback();
                throw;
            }
        }

        public void Commit(Enlistment enlistment)
        {
            Commit();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            Rollback();
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}
