using System;

namespace DataTanker
{
    public class StorageFlushExceptionEventArgs : EventArgs
    {
      public Exception FlushException { get; set; }
    }
}
