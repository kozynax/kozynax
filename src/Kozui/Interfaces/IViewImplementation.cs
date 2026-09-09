using System;
using ZXMAK2.Host.Entities;

namespace Kozui.Interfaces
{
	public interface IViewImplementation<T> : IUiImplementation<T>
	{
		DlgResult ShowDialog(object owner);
	}
}