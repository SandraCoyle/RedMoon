using System;
using RedMoon.Core.Common;
using RedMoon.Core.Models;

namespace RedMoon.Core.Services
{
    /// <summary>
    /// Holder den indloggede brugers dekrypterede data i hukommelsen, mens appen kører.
    /// Når brugeren logger ud, slippes referencen.
    /// </summary>
    public sealed class SessionState
    {
        private UserVault? _vault;

        /// <summary>Udløses når brugeren logger ind eller ud.</summary>
        public event EventHandler? Changed;

        /// <summary>Sand hvis en bruger er logget ind.</summary>
        public bool IsLoggedIn => _vault != null;

        /// <summary>Den indloggede brugers data. Kaster NotLoggedInException hvis ingen er logget ind.</summary>
        public UserVault Vault => _vault ?? throw new NotLoggedInException();

        internal void Begin(UserVault vault)
        {
            _vault = vault ?? throw new ArgumentNullException(nameof(vault));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void End()
        {
            _vault = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
