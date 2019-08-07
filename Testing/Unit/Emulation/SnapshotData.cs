using System.Collections.Generic;
using System.Linq;

using DataTanker.Versioning;

namespace Tests.Emulation
{
    public class SnapshotData : ISnapshotData
    {
        private readonly List<int> _rolledBackTransactions;
        private readonly List<int> _commitedTransactions;

        public bool IsCommittedTransaction(int number)
        {
            return _commitedTransactions.Contains(number);
        }

        public bool IsRolledBackTransaction(int number)
        {
            return  _rolledBackTransactions.Contains(number);
        }

        public SnapshotData(IEnumerable<int> commitedTransactions)
        {
            _commitedTransactions = commitedTransactions.ToList();
            _rolledBackTransactions = new List<int>();
        }

        public SnapshotData(IEnumerable<int> commitedTransactions, IEnumerable<int> rolledBackTransactions)
        {
            _rolledBackTransactions = rolledBackTransactions.ToList();
            _commitedTransactions = commitedTransactions.ToList();
        }
    }
}