

using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware;
using ZXMAK2.Hardware.Circuits.Fdd;
using ZXMAK2.Hardware.General;

namespace ZXMAK2.Host.Presentation.Interfaces
{
    public interface IAboutView : IView
    {
    }

    public interface IKeyboardView : IView
    {
    }

    public interface IMemoryMapView : IView
    {
        void Init(MemoryBase mem);
    }

    public interface ITapeView : IView
    {
        void Init(TapeDevice tapeDevice);
    }

    public interface IFddDebugView : IView
    {
        void Init(Wd1793 debugTarget);
    }

    public interface IDebuggerBaseView : IView
    {
        void Init(IDebuggable dbg);
    }
    
    public interface IDebuggerGeneralView : IDebuggerBaseView, IViewImplementation<DebuggerDialog>
    {
    }

    public interface IDebuggerSprinterView : IDebuggerBaseView, IViewImplementation<SprinterDebuggerDialog>
    {
    }
}
