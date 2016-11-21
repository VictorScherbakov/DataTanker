using System.Collections.Generic;
using System.Linq;
namespace DataTanker.Versioning
{
    using Transactions;

    internal class SnapshotDataStub : ISnapshotData
    {
        private readonly List<DataTankerTransaction> _transactions;

        public bool IsComittedTransaction(int number)
        {
            var tr = _transactions.SingleOrDefault(t => t.Id == number);
            return tr?.State == TransactionState.Commited;
        }

        public bool IsRolledBackTransaction(int number)
        {
            var tr = _transactions.SingleOrDefault(t => t.Id == number);
            if (tr != null)
                return tr.State == TransactionState.RolledBack;

            return true;
        }

        public SnapshotDataStub(IEnumerable<DataTankerTransaction> transactions)
        {
            _transactions = transactions.ToList();
        }
    }
}