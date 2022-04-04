namespace Portalum.Zvt.Models
{
    /// <summary>
    /// ProcessData State
    /// </summary>
    public enum ProcessDataState
    {
        /// <summary>
        /// Cannot process data
        /// </summary>
        CannotProcess,
        /// <summary>
        /// Failure on processing
        /// </summary>
        ParseFailure,
        /// <summary>
        /// Successfully processed
        /// </summary>
        Processed,
        /// <summary>
        /// Wait for more data
        /// </summary>
        WaitForMoreData
    }
}
