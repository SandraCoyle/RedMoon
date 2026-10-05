using System;

namespace RedMoon.Core.Common
{
    /// <summary>
    /// Basisklasse for alle forventede fejl i RedMoon. Beskeden er skrevet på dansk,
    /// så brugerfladen kan vise den direkte til brugeren.
    /// </summary>
    public class RedMoonException : Exception
    {
        public RedMoonException(string message) : base(message) { }
        public RedMoonException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Kastes når brugerinput ikke overholder reglerne (fx ugyldigt brugernavn eller fremtidig dato).
    /// </summary>
    public sealed class ValidationException : RedMoonException
    {
        public ValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Kastes når den krypterede datafil ikke kan læses: forkert nøgle, manipuleret fil eller ødelagt indhold.
    /// </summary>
    public sealed class VaultCorruptedException : RedMoonException
    {
        public VaultCorruptedException(string message) : base(message) { }
        public VaultCorruptedException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Kastes når en handling kræver et aktivt login, og der ikke er et.
    /// </summary>
    public sealed class NotLoggedInException : RedMoonException
    {
        public NotLoggedInException() : base("Du er ikke logget ind.") { }
    }
}
