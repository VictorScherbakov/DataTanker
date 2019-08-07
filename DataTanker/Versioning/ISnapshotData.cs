namespace DataTanker.Versioning
{
    public interface ISnapshotData
    {
        bool IsCommittedTransaction(int number);

        bool IsRolledBackTransaction(int number);
    }
}