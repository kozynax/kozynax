using System;
using ZXMAK2.Host.Entities;

namespace Kozui.Interfaces
{
	public interface IViewImplementation<T> : IDisposable
	{
		void Init(T ui);
		DlgResult ShowDialog(object owner);
	}
}