namespace DataTanker.Versioning
{
    internal interface ISnapshotData
    {
        bool IsComittedTransaction(int number);

        bool IsRolledBackTransaction(int number);
    }
}