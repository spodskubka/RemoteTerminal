using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Common;

namespace Renci.SshNet
{
    /// <summary>
    /// Provides connection information when keyboard interactive authentication method is used
    /// </summary>
    public class KeyboardInteractiveConnectionInfo : ConnectionInfo, IDisposable
    {
        /// <summary>
        /// Occurs when server prompts for more authentication information.
        /// </summary>
        public event EventHandler<AuthenticationPromptEventArgs> AuthenticationPrompt;

        //  TODO: DOCS Add exception documentation for this class.

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardInteractiveConnectionInfo"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="username">The username.</param>
        public KeyboardInteractiveConnectionInfo(string host, string username)
            : this(host, 22, username)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardInteractiveConnectionInfo"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="username">The username.</param>
        public KeyboardInteractiveConnectionInfo(string host, int port, string username)
            : base(host, port, username, new KeyboardInteractiveAuthenticationMethod(username))
        {
            foreach (var authenticationMethod in this.AuthenticationMethods.OfType<KeyboardInteractiveAuthenticationMethod>())
            {
                authenticationMethod.AuthenticationPrompt += AuthenticationMethod_AuthenticationPrompt;
            }

        }

        private void AuthenticationMethod_AuthenticationPrompt(object sender, AuthenticationPromptEventArgs e)
        {
            if (this.AuthenticationPrompt != null)
            {
                this.AuthenticationPrompt(sender, e);
            }
        }


        #region IDisposable Members

        private bool isDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.isDisposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this.AuthenticationMethods != null)
                    {
                        foreach (var authenticationMethods in this.AuthenticationMethods.OfType<IDisposable>())
                        {
                            authenticationMethods.Dispose();
                        }
                    }
                }

                // Note disposing has been done.
                isDisposed = true;
            }
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="PasswordConnectionInfo"/> is reclaimed by garbage collection.
        /// </summary>
        ~KeyboardInteractiveConnectionInfo()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }
}
