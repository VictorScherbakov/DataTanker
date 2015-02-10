namespace DataTanker.Transactions
{
    /// <summary>
    /// Contains constants representing states of transaction.
    /// </summary>
    internal enum TransactionState
    {
        /// <summary>
        /// Transaction is active.
        /// </summary>
        Active,

        /// <summary>
        /// Transaction is commited.
        /// </summary>
        Commited,

        /// <summary>
        /// Transaction is rolled back.
        /// </summary>
        RolledBack,

        /// <summary>
        /// Transaction is prepared to commit or rollback in two-phase commit. 
        /// This means that all data is saved to persistent storage and there 
        /// are no update conflicts.
        /// </summary>
        Prepared
    }
}
