using System;

namespace RedMoon.Core.Services
{
    /// <summary>Udfaldet af et login-forsøg.</summary>
    public enum LoginOutcome
    {
        Success,
        /// <summary>Forkert brugernavn eller mønster (bevidst uspecifikt).</summary>
        WrongCredentials,
        /// <summary>For mange forkerte forsøg – login er midlertidigt spærret.</summary>
        LockedOut,
        /// <summary>Der findes ingen konto på telefonen.</summary>
        NoAccount,
    }

    /// <summary>
    /// Resultat af LoginAsync. Indeholder den besked der skal vises til brugeren.
    /// </summary>
    public sealed class LoginResult
    {
        private LoginResult(LoginOutcome outcome, string message, DateTime? lockedUntilUtc, int attemptsLeft)
        {
            Outcome = outcome;
            Message = message;
            LockedUntilUtc = lockedUntilUtc;
            AttemptsLeftBeforeLockout = attemptsLeft;
        }

        public LoginOutcome Outcome { get; }
        public bool IsSuccess => Outcome == LoginOutcome.Success;

        /// <summary>Dansk besked til brugeren (tom ved succes).</summary>
        public string Message { get; }

        /// <summary>Hvornår spærringen ophører (kun ved LockedOut).</summary>
        public DateTime? LockedUntilUtc { get; }

        /// <summary>Antal forsøg tilbage før spærring (kun ved WrongCredentials).</summary>
        public int AttemptsLeftBeforeLockout { get; }

        internal static LoginResult Success() => new LoginResult(LoginOutcome.Success, string.Empty, null, 0);

        internal static LoginResult NoAccount() =>
            new LoginResult(LoginOutcome.NoAccount, "Der findes ingen bruger på denne telefon. Opret en ny.", null, 0);

        internal static LoginResult Wrong(int attemptsLeft) =>
            new LoginResult(LoginOutcome.WrongCredentials,
                attemptsLeft > 0
                    ? $"Forkert brugernavn eller mønster. {attemptsLeft} forsøg tilbage før en pause."
                    : "Forkert brugernavn eller mønster.",
                null, attemptsLeft);

        internal static LoginResult Locked(DateTime untilUtc, DateTime nowUtc)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((untilUtc - nowUtc).TotalSeconds));
            var text = seconds >= 60
                ? $"For mange forkerte forsøg. Prøv igen om {(seconds + 59) / 60} min."
                : $"For mange forkerte forsøg. Prøv igen om {seconds} sek.";
            return new LoginResult(LoginOutcome.LockedOut, text, untilUtc, 0);
        }
    }
}
