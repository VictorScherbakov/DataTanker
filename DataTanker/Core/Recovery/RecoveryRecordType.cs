namespace DataTanker.Recovery
{
    /// <summary>
    /// Represents possible types of recovery record.
    /// </summary>
    internal enum RecoveryRecordType
    {
        /// <summary>
        /// The record represents the deletion of page
        /// </summary>
        Delete = 1,

        /// <summary>
        /// The record represents update of page
        /// </summary>
        Update = 2,

        /// <summary>
        /// The record represents final marker of recovery file
        /// </summary>
        Final = 3
    }
}