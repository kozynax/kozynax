using System;

namespace Kozui.Interfaces
{
    public interface IUiImplementation<T> : IDisposable
    {
        void Init(T ui);
    }
}