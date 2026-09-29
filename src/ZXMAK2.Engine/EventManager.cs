using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Cpu;
using ZXMAK2.Engine.Interfaces;

namespace ZXMAK2.Engine
{
    public sealed class EventManager : IEventManager
    {
        private sealed class SignalRequest
        {
            public bool IsProcessed;
            public Func<bool> Condition;
            public Action OnAck;
            public long WaitTimeOut;
        }

        private readonly CpuUnit m_cpu;
        private readonly RzxHandler m_rzx;
        private readonly object m_pendingNmiSync = new object();
        private SignalRequest m_pendingNmi;


        private BusReadProc[] m_mapReadMemoryM1;
        private BusReadProc[] m_mapReadMemory;
        private BusReadIoProc[] m_mapReadPort;
        private BusWriteProc[] m_mapWriteMemory;
        private BusWriteIoProc[] m_mapWritePort;
        private Action<ushort>[] m_mapReadNoMreq;
        private Action<ushort>[] m_mapWriteNoMreq;
        private Action m_preCycle;
        private Action m_reset;
        private Action m_nmiAck;
        private Action m_intAck;
        private Action m_beginFrame;
        private Action m_endFrame;

        public event Action ScanSig;

        public EventManager(CpuUnit cpu, RzxHandler rzx)
        {
            m_cpu = cpu;
            m_rzx = rzx;

            m_cpu.RDMEM_M1 = RDMEM_M1;
            m_cpu.INTACK_M1 = INTACK_M1;
            m_cpu.NMIACK_M1 = NMIACK_M1;
            m_cpu.RDMEM = RDMEM;
            m_cpu.WRMEM = WRMEM;
            m_cpu.RDPORT = RDPORT;
            m_cpu.WRPORT = WRPORT;
            m_cpu.RDNOMREQ = RDNOMREQ;
            m_cpu.WRNOMREQ = WRNOMREQ;
            m_cpu.RESET = RESET;
            m_cpu.SCANSIG = SCANSIG;
            
            Clear();
        }

        public void Clear()
        {
            m_preCycle = null;
            m_mapReadMemoryM1 = new BusReadProc[0x10000];
            m_mapReadMemory = new BusReadProc[0x10000];
            m_mapReadPort = new BusReadIoProc[0x10000];
            m_mapWriteMemory = new BusWriteProc[0x10000];
            m_mapWritePort = new BusWriteIoProc[0x10000];
            m_mapReadNoMreq = new Action<ushort>[0x10000];
            m_mapWriteNoMreq = new Action<ushort>[0x10000];
            m_reset = null;
            m_nmiAck = null;
            m_intAck = null;
            m_beginFrame = null;
            m_endFrame = null;
        }

        public void BeginFrame()
        {
            var handler = m_beginFrame;
            if (handler != null)
            {
                handler();
            }
        }

        public void EndFrame()
        {
            var handler = m_endFrame;
            if (handler != null)
            {
                handler();
            }
        }

        public void PreCycle()
        {
            m_cpu.NMI = m_pendingNmi != null && ProcessPendingNmi();
            var handlerPre = m_preCycle;
            if (handlerPre != null)
            {
                handlerPre();
            }
        }

        
        #region IEventManager

        public void SubscribeRdMemM1(int addrMask, int maskedValue, BusReadProc proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapReadMemoryM1[addr] += proc;
        }

        public void SubscribeRdMem(int addrMask, int maskedValue, BusReadProc proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapReadMemory[addr] += proc;
        }

        public void SubscribeWrMem(int addrMask, int maskedValue, BusWriteProc proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapWriteMemory[addr] += proc;
        }

        public void SubscribeRdIo(int addrMask, int maskedValue, BusReadIoProc proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapReadPort[addr] += proc;
        }

        public void SubscribeWrIo(int addrMask, int maskedValue, BusWriteIoProc proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapWritePort[addr] += proc;
        }

        public void SubscribeRdNoMreq(int addrMask, int maskedValue, Action<ushort> proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapReadNoMreq[addr] += proc;
        }

        public void SubscribeWrNoMreq(int addrMask, int maskedValue, Action<ushort> proc)
        {
            for (int addr = 0; addr < 0x10000; addr++)
                if ((addr & addrMask) == maskedValue)
                    m_mapWriteNoMreq[addr] += proc;
        }

        public void SubscribePreCycle(Action proc)
        {
            m_preCycle += proc;
        }

        public void SubscribeReset(Action proc)
        {
            m_reset += proc;
        }

        public void SubscribeNmiAck(Action proc)
        {
            m_nmiAck += proc;
        }

        public void SubscribeIntAck(Action proc)
        {
            m_intAck += proc;
        }

        public void SubscribeBeginFrame(Action handler)
        {
            m_beginFrame += handler;
        }

        public void SubscribeEndFrame(Action handler)
        {
            m_endFrame += handler;
        }

        public void RequestNmi(int timeOut, Action onAck, Func<bool> condition)
        {
            if (m_pendingNmi != null)
            {
                return;
            }
            lock (m_pendingNmiSync)
            {
                if (m_pendingNmi == null)
                {
                    m_pendingNmi = new SignalRequest
                    {
                        IsProcessed = false,
                        Condition = condition,
                        OnAck = onAck,
                        WaitTimeOut = m_cpu.Tact + timeOut
                    };
                }
            }
        }

        #endregion IEventManager


        #region CPU Handlers

        private byte RDMEM_M1(ushort addr)
        {
            var result = m_cpu.BUS;
            var handler = m_mapReadMemoryM1[addr];
            if (handler != null)
            {
                handler(addr, ref result);
            }
            //Logger.Info(
            //    "{0:D3}-{1:D6}: #{2:X4} = #{3:X2}",
            //    m_cpu.Tact / m_ula.FrameTactCount,
            //    m_cpu.Tact % m_ula.FrameTactCount,
            //    m_cpu.regs.PC,
            //    result);
            return result;
        }

        private byte RDMEM(ushort addr)
        {
            var result = m_cpu.BUS;
            var handler = m_mapReadMemory[addr];
            if (handler != null)
            {
                handler(addr, ref result);
            }
            return result;
        }

        private void WRMEM(ushort addr, byte value)
        {
            var handler = m_mapWriteMemory[addr];
            if (handler != null)
            {
                handler(addr, value);
            }
        }

        private byte RDPORT(ushort addr)
        {
            var result = m_cpu.BUS;
            var handler = m_mapReadPort[addr];
            if (handler != null)
            {
                var handled = false;
                handler(addr, ref result, ref handled);
            }
            //if (m_rzx == null)
            //{
            //    return result;
            //}
            if (m_rzx.IsPlayback)
            {
                return m_rzx.GetInput();
            }
            else if (m_rzx.IsRecording)
            {
                m_rzx.SetInput(result);
            }

#if PORTDEBUG
            Logger.Debug($"Port {addr:X4} -> {result:X2}");
#endif
            return result;
        }

        private void WRPORT(ushort addr, byte value)
        {
            var handler = m_mapWritePort[addr];
            if (handler != null)
            {
                var handled = false;
                handler(addr, value, ref handled);
            }
#if PORTDEBUG
            Logger.Debug($"Port {addr:X4} <- {value:X2}");
#endif
        }

        private void RDNOMREQ(ushort addr)
        {
            var handler = m_mapReadNoMreq[addr];
            if (handler != null)
            {
                handler(addr);
            }
        }

        private void WRNOMREQ(ushort addr)
        {
            var handler = m_mapWriteNoMreq[addr];
            if (handler != null)
            {
                handler(addr);
            }
        }

        private void RESET()
        {
            m_pendingNmi = null;
            if (m_rzx != null)
            {
                m_rzx.Reset();
            }
            var handler = m_reset;
            if (handler != null)
            {
                handler();
            }
        }

        private void INTACK_M1()
        {
            var handler = m_intAck;
            if (handler != null)
            {
                handler();
            }
        }

        private void NMIACK_M1()
        {
            if (m_pendingNmi != null)
            {
                SignalRequest request;
                lock (m_pendingNmiSync)
                {
                    request = m_pendingNmi;
                    if (request != null)
                    {
                        if (request.IsProcessed)
                        {
                            request = null;
                        }
                        else
                        {
                            request.IsProcessed = true;
                        }
                    }
                }
                if (request != null && request.OnAck != null)
                {
                    request.OnAck();
                }
            }
            var handler = m_nmiAck;
            if (handler != null)
            {
                handler();
            }
        }

        private void SCANSIG()
        {
            var handler = ScanSig;
            if (handler != null)
            {
                handler();
            }
        }

        #endregion


        #region Private

        private bool ProcessPendingNmi()
        {
            SignalRequest pendingNmi;
            lock (m_pendingNmiSync)
            {
                pendingNmi = m_pendingNmi;
                if (pendingNmi == null)
                {
                    return false;
                }
                if (pendingNmi.IsProcessed)
                {
                    m_pendingNmi = null;
                    return false;
                }
                if (pendingNmi.WaitTimeOut <= m_cpu.Tact)
                {
                    m_pendingNmi = null;
                    return false;
                }
            }
            return pendingNmi.Condition != null ? pendingNmi.Condition() : true;
        }

        #endregion Private
    }
}
