using System;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.Cli.Services
{
    /// <summary>
    /// Console-only <see cref="IUserMessage"/> for headless CLI (no dialogs).
    /// </summary>
    public sealed class ConsoleUserMessage : IUserMessage
    {
        public bool HadError { get; private set; }

        public void Reset() => HadError = false;

        public void ErrorDetails(Exception ex)
        {
            HadError = true;
            Console.Error.WriteLine(ex?.ToString() ?? "Unknown error");
        }

        public void Error(Exception ex)
        {
            HadError = true;
            Console.Error.WriteLine(ex?.Message ?? "Unknown error");
        }

        public void Error(string fmt, params object[] args)
        {
            HadError = true;
            Console.Error.WriteLine(args != null && args.Length > 0 ? string.Format(fmt, args) : fmt);
        }

        public void Warning(Exception ex)
            => Console.Error.WriteLine(ex?.Message ?? "Unknown warning");

        public void Warning(string fmt, params object[] args)
            => Console.Error.WriteLine(args != null && args.Length > 0 ? string.Format(fmt, args) : fmt);

        public void Info(string fmt, params object[] args)
            => Console.WriteLine(args != null && args.Length > 0 ? string.Format(fmt, args) : fmt);
    }

    /// <summary>
    /// Non-interactive <see cref="IUserQuery"/> for headless CLI.
    /// </summary>
    public sealed class ConsoleUserQuery : IUserQuery
    {
        public DlgResult Show(string message, string caption, DlgButtonSet buttonSet, DlgIcon icon)
        {
            // Prefer "No" so SCL/Hobeta never try to append to an existing image.
            Console.Error.WriteLine("{0}: {1} -> No", caption ?? "Query", message);
            return DlgResult.No;
        }

        public object ObjectSelector(object[] objArray, string caption)
            => null;

        public bool QueryText(string caption, string text, ref string value)
            => false;

        public bool QueryValue(string caption, string text, string format, ref int value, int min, int max)
            => false;
    }
}
