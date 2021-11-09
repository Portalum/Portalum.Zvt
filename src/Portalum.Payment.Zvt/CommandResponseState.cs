namespace Portalum.Payment.Zvt
{
    /// <summary>
    /// CommandResponseState
    /// </summary>
    public enum CommandResponseState
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// Successful (Information from the payment terminal)
        /// </summary>
        Successful,
        /// <summary>
        /// Abort (Information from the payment terminal)
        /// </summary>
        Abort,
        /// <summary>
        /// Timeout controlled by the zvt client and not by the payment terminal
        /// </summary>
        Timeout,
        /// <summary>
        /// NotSupported (Information from the payment terminal)
        /// </summary>
        NotSupported,
        /// <summary>
        /// Error (Information from the payment terminal)
        /// </summary>
        Error
    }
}
