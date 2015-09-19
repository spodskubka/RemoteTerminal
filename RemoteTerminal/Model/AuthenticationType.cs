namespace RemoteTerminal.Model
{
    /// <summary>
    /// The authentication type (used for SSH connections).
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// Password authentication.
        /// </summary>
        Password,

        /// <summary>
        /// Keyboard-Interactive authentication.
        /// </summary>
        KeyboardInteractive,

        /// <summary>
        /// Private Key authentication.
        /// </summary>
        PrivateKey,

        /// <summary>
        /// Private Key Agent authentication.
        /// </summary>
        PrivateKeyAgent,

        ///// <summary>
        ///// Host-based authentication.
        ///// </summary>
        //HostBased,
    }
}
