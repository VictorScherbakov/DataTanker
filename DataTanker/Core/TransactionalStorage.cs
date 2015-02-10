namespace DataTanker
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading;

    using PageManagement;
    using Transactions;
    using Versioning;

    internal class TransactionalStorage : Storage, ITransactionInventory
    {
        private readonly List<DataTankerTransaction> _transactions = new List<DataTankerTransaction>();
        private int _lastCommited = -1;

        private readonly object _locker = new object();

        protected void Lock()
        {
            Monitor.Enter(_locker);
        }

        protected void Unlock()
        {
            Monitor.Exit(_locker);
        }

        protected void EnterWrap()
        {
            Lock();
            EnterTransaction();
        }

        protected void ExitWrap()
        {
            ExitTransaction();
            Unlock();
        }

        #region temporary disabled transactional issues

        //private readonly ITransactionProvider _transactionProvider = new DefaultTransactionProvider();
        //private bool _shouldCommitOnExit;

        private void EnterTransaction()
        {
            //// use autocommit if the transaction is not set 
            //_shouldCommitOnExit = _transactionProvider.Current == null;

            //if (_shouldCommitOnExit) // create new one for this method
            //{
            //    _transactionProvider.Current = _transactionProvider.CreateStorage(this);
            //}
        }

        private void ExitTransaction()
        {
            //if (_shouldCommitOnExit)
            //{
            //    _transactionProvider.Current.Commit();
            //    _transactionProvider.Current = null;
            //}
        }
        #endregion


        internal TransactionalStorage(IPageManager pageManager) : base(pageManager)
        {
        }

        public void Mark(int number, TransactionState newState)
        {
            if (_transactions.Count > number)
            {
                _transactions[number].State = newState;
                if (newState == TransactionState.Commited && number > _lastCommited)
                    _lastCommited = number;
            }
        }

        public int CreateTransaction(out ISnapshotData snapshotData)
        {
            snapshotData = new SnapshotDataStub(_transactions);
            var tr = new DataTankerTransaction(this, snapshotData, _transactions.Count);
            _transactions.Add(tr);

            return tr.Id;
        }

        public void MarkActivesAsRolledBack()
        {
            foreach (var tr in _transactions.Where(t => t.State == TransactionState.Active))
                tr.State = TransactionState.RolledBack;
        }

        public TransactionState GetState(int number)
        {
            if (_transactions.Count <= number)
                throw new ArgumentOutOfRangeException("number");

            return _transactions[number].State;
        }
    }
}