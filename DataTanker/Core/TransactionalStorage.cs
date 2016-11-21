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

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        protected void EnterWriteLock()
        {
            _lock.EnterWriteLock();
        }

        protected void ExitWriteUnlock()
        {
            _lock.ExitWriteLock();
        }

        protected void EnterReadLock()
        {
            _lock.EnterReadLock();
        }

        protected void ExitReadLock()
        {
            _lock.ExitReadLock();
        }

        protected void EnterWriteWrap()
        {
            EnterWriteLock();
            EnterTransaction();
        }

        protected void ExitWriteWrap()
        {
            ExitTransaction();
            ExitWriteUnlock();
        }

        protected void EnterReadWrap()
        {
            EnterReadLock();
            EnterTransaction();
        }

        protected void ExitReadWrap()
        {
            ExitTransaction();
            ExitReadLock();
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


        internal TransactionalStorage(IPageManager pageManager) : base(pageManager, TimeSpan.Zero)
        {
        }

        internal TransactionalStorage(IPageManager pageManager, int autoFlushInterval, TimeSpan autoFlushTimeout)
            : base(pageManager, autoFlushTimeout, autoFlushInterval)
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
                throw new ArgumentOutOfRangeException(nameof(number));

            return _transactions[number].State;
        }
    }
}