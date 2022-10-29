using System.IO;

namespace ZXMAK2.Host.WinForms
{
	public class Resources
	{
		public static string KeyboardMdx => File.ReadAllText("Keyboard.Mdx.config");
		public static string KeyboardWinForms => File.ReadAllText("Keyboard.WinForms.config");
	}
}