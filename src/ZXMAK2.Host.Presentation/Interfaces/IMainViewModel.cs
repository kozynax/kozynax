using System;
using System.ComponentModel;
using ZXMAK2.Mvvm;


namespace ZXMAK2.Host.Presentation.Interfaces
{
    public interface IMainViewModel : IDisposable
    {
        void Init(IMainView view, string[] args);
        void Run();
        void Attach(ISynchronizeInvoke synchronizeInvoke);
    }
}
