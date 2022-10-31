namespace ZXMAK2.Logging
{
	public interface ILog
	{
		void Write(ErrorLevel level, string message);
	}
}