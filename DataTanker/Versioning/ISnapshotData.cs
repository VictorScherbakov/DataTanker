namespace DataTanker.Versioning
{
    public interface ISnapshotData
    {
        bool IsComittedTransaction(int number);

        bool IsRolledBackTransaction(int number);
    }
}