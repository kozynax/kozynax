using System;

namespace ZXMAK2.Logging
{
	public static class ILogExtensions
	{
		private static void WriteFormat(ILog logger, ErrorLevel level, string fmt, object[] args)
		{
			var msg = args != null && args.Length > 0 ? string.Format(fmt, args) : fmt;
			Console.Error.WriteLine("[{0:HH:mm:ss.fff}] {1}: {2}", DateTime.Now, level, msg);
		}

		private static void WriteException(ILog logger, ErrorLevel level, string msg, Exception exception)
		{
			Console.Error.WriteLine("[{0:HH:mm:ss.fff}] {1}: {2}", DateTime.Now, level, msg);
			if (exception != null)
				Console.Error.WriteLine(exception);
		}
        
		public static void DebugFormat(this ILog logger, string fmt, object[] args)
			=> WriteFormat(logger, ErrorLevel.Debug, fmt, args);
		public static void InfoFormat(this ILog logger, string fmt, object[] args)
			=> WriteFormat(logger, ErrorLevel.Info, fmt, args);
		public static void WarnFormat(this ILog logger, string fmt, object[] args)
			=> WriteFormat(logger, ErrorLevel.Warn, fmt, args);
		public static void FatalFormat(this ILog logger, string fmt, object[] args)
			=> WriteFormat(logger, ErrorLevel.Fatal, fmt, args);
		public static void ErrorFormat(this ILog logger, string fmt, object[] args)
			=> WriteFormat(logger, ErrorLevel.Error, fmt, args);
		public static void Debug(this ILog logger, string msg, Exception exception)
			=> WriteException(logger, ErrorLevel.Debug, msg, exception);
		public static void Error(this ILog logger, string msg, Exception exception)
			=> WriteException(logger, ErrorLevel.Error, msg, exception);
		public static void Fatal(this ILog logger, string msg, Exception exception)
			=> WriteException(logger, ErrorLevel.Fatal, msg, exception);
		public static void Warn(this ILog logger, string msg, Exception exception)
			=> WriteException(logger, ErrorLevel.Warn, msg, exception);
		public static void Info(this ILog logger, string msg, Exception exception)
			=> WriteException(logger, ErrorLevel.Info, msg, exception);

	}
}