namespace DataTanker.Recovery
{
    /// <summary>
    /// Represents a record in recovery file.
    /// </summary>
    internal class RecoveryRecord
    {
        /// <summary>
        /// Gets or sets an index of page this record corresponds to
        /// </summary>
        public long PageIndex { get; set; }

        /// <summary>
        /// Gets or sets a content of the page
        /// </summary>
        public byte[] PageContent { get; set; }

        /// <summary>
        /// Gets or sets a type of record
        /// </summary>
        public RecoveryRecordType RecordType { get; set; }
    }
}