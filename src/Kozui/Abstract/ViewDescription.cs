using System;
using Kozui.Interfaces;
using ZXMAK2.Dependency;
using ZXMAK2.Host.Entities;

namespace Kozui.Abstract
{
	public class ViewDescription<T> : IViewDescription<T> where T : ViewDescription<T>
	{
		public object ViewHandle { get; private set; }
		public DlgResult ShowDialog(object owner)
			=> ShowDialog<IViewImplementation<T>>(owner, null);
		public DlgResult ShowDialog<T1>(object owner, Action<T1> additionalInit) where T1 : IViewImplementation<T>
		{
			using (var form = Locator.Resolve<T1>())
			{
				ViewHandle = form;
				form.Init((T)this);
				additionalInit?.Invoke(form);
				var result = form.ShowDialog(owner);
				ViewHandle = null;
				return result;
			}
		}
	}
}