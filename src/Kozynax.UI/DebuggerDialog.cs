using System;
using System.Drawing;
using System.IO;
using Kozui.Abstract;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Cpu.Tools;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI
{
    public class DebuggerDialog : ViewDescription<DebuggerDialog>
    {
        public event EventHandler Breakpoint;
        public event EventHandler UpdateState;
        public event EventHandler CpuDetailsUpdated;
        
        public delegate void OnRunningStateChange(bool isRunning);
        public event OnRunningStateChange RunningStateChanged;
        
        private IDebuggable m_spectrum;
        private DasmTool m_dasmTool;
        private TimingTool m_timingTool;
        
        public DataPanelComponent DataPanel { get; }
        public DasmPanelComponent DasmPanel { get; }
        public ListView<string> RegistersList { get; }
        public ListView<string> FlagsList { get; }
        public ListView<string> StatesList { get; }
        
        public DebuggerDialog()
        {
            DasmPanel = new DasmPanelComponent();
            DasmPanel.CheckBreakpoint += dasmPanel_CheckBreakpoint;
            DasmPanel.CheckExecuting += dasmPanel_CheckExecuting;
            DasmPanel.GetData += dasmPanel_GetData;
            DasmPanel.GetDasm += dasmPanel_GetDasm;
            DasmPanel.BreakpointClick += dasmPanel_SetBreakpoint;
            
            DataPanel = new DataPanelComponent();
            DataPanel.GetData += dasmPanel_GetData;

            RegistersList = new ListView<string>();
            FlagsList = new ListView<string>();
            StatesList = new ListView<string>();
        }
        
        public void Init(IDebuggable debugTarget)
        {
            if (debugTarget == m_spectrum)
                return;
            if (m_spectrum != null)
            {
                m_spectrum.UpdateState -= spectrum_OnUpdateState;
                m_spectrum.Breakpoint -= spectrum_OnBreakpoint;
            }
            if (debugTarget != null)
            {
                m_spectrum = debugTarget;
                m_dasmTool = new DasmTool(debugTarget.ReadMemory);
                m_timingTool = new TimingTool(m_spectrum.CPU, debugTarget.ReadMemory);
                m_spectrum.UpdateState += spectrum_OnUpdateState;
                m_spectrum.Breakpoint += spectrum_OnBreakpoint;
            }
        }
        
        public void Close()
        {
            m_spectrum.UpdateState -= spectrum_OnUpdateState;
            m_spectrum.Breakpoint -= spectrum_OnBreakpoint;
        }

        private void spectrum_OnUpdateState(object sender, EventArgs args)
            => UpdateState?.Invoke(sender, args);
        
        private void spectrum_OnBreakpoint(object sender, EventArgs args)
            => Breakpoint?.Invoke(sender, args);
        
        public void UpdateCPU(bool updatePC)
        {
            if (m_spectrum.IsRunning)
                updatePC = false;
            
            RunningStateChanged?.Invoke(m_spectrum.IsRunning);
            UpdateREGS();
            UpdateDASM(updatePC);
            UpdateDATA();
        }
        
        private void UpdateREGS()
        {
            RegistersList.List.Clear();
            RegistersList.List.Add(" PC = " + m_spectrum.CPU.regs.PC.ToString("X4"));
            RegistersList.List.Add(" IR = " + m_spectrum.CPU.regs.IR.ToString("X4"));
            RegistersList.List.Add(" SP = " + m_spectrum.CPU.regs.SP.ToString("X4"));
            RegistersList.List.Add(" AF = " + m_spectrum.CPU.regs.AF.ToString("X4"));
            RegistersList.List.Add(" HL = " + m_spectrum.CPU.regs.HL.ToString("X4"));
            RegistersList.List.Add(" DE = " + m_spectrum.CPU.regs.DE.ToString("X4"));
            RegistersList.List.Add(" BC = " + m_spectrum.CPU.regs.BC.ToString("X4"));
            RegistersList.List.Add(" IX = " + m_spectrum.CPU.regs.IX.ToString("X4"));
            RegistersList.List.Add(" IY = " + m_spectrum.CPU.regs.IY.ToString("X4"));
            RegistersList.List.Add(" AF'= " + m_spectrum.CPU.regs._AF.ToString("X4"));
            RegistersList.List.Add(" HL'= " + m_spectrum.CPU.regs._HL.ToString("X4"));
            RegistersList.List.Add(" DE'= " + m_spectrum.CPU.regs._DE.ToString("X4"));
            RegistersList.List.Add(" BC'= " + m_spectrum.CPU.regs._BC.ToString("X4"));
            RegistersList.List.Add(" MW = " + m_spectrum.CPU.regs.MW.ToString("X4"));
            
            FlagsList.List.Clear();
            FlagsList.List.Add("  S = " + (((m_spectrum.CPU.regs.F & 0x80) != 0) ? "1" : "0"));
            FlagsList.List.Add("  Z = " + (((m_spectrum.CPU.regs.F & 0x40) != 0) ? "1" : "0"));
            FlagsList.List.Add(" F5 = " + (((m_spectrum.CPU.regs.F & 0x20) != 0) ? "1" : "0"));
            FlagsList.List.Add("  H = " + (((m_spectrum.CPU.regs.F & 0x10) != 0) ? "1" : "0"));
            FlagsList.List.Add(" F3 = " + (((m_spectrum.CPU.regs.F & 0x08) != 0) ? "1" : "0"));
            FlagsList.List.Add("P/V = " + (((m_spectrum.CPU.regs.F & 0x04) != 0) ? "1" : "0"));
            FlagsList.List.Add("  N = " + (((m_spectrum.CPU.regs.F & 0x02) != 0) ? "1" : "0"));
            FlagsList.List.Add("  C = " + (((m_spectrum.CPU.regs.F & 0x01) != 0) ? "1" : "0"));

            StatesList.List.Clear();
            StatesList.List.Add("IFF1=" + (m_spectrum.CPU.IFF1 ? "1" : "0") + " IFF2=" + (m_spectrum.CPU.IFF2 ? "1" : "0"));
            StatesList.List.Add("HALT=" + (m_spectrum.CPU.HALTED ? "1" : "0"));
            StatesList.List.Add("BINT=" + (m_spectrum.CPU.BINT ? "1" : "0"));
            StatesList.List.Add("  IM=" + m_spectrum.CPU.IM.ToString());
            StatesList.List.Add("  FX=" + m_spectrum.CPU.FX.ToString());
            StatesList.List.Add(" XFX=" + m_spectrum.CPU.XFX.ToString());
            StatesList.List.Add(" LPC=#" + m_spectrum.CPU.LPC.ToString("X4"));
            StatesList.List.Add("Tact=" + m_spectrum.CPU.Tact.ToString());
            StatesList.List.Add("frmT=" + m_spectrum.GetFrameTact().ToString());
            if (m_spectrum.RzxState.IsPlayback)
            {
                StatesList.List.Add(string.Format("rzxm={0}/{1}", m_spectrum.RzxState.Fetch, m_spectrum.RzxState.FetchCount));
                StatesList.List.Add(string.Format("rzxi={0}/{1}", m_spectrum.RzxState.Input, m_spectrum.RzxState.InputCount));
                StatesList.List.Add(string.Format("rzff={0}/{1}", m_spectrum.RzxState.Frame, m_spectrum.RzxState.FrameCount));
            }
            
            CpuDetailsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateDASM(bool updatePC)
        {
            if (!m_spectrum.IsRunning && updatePC)
                DasmPanel.ActiveAddress = m_spectrum.CPU.regs.PC;
            else
            {
                DasmPanel.UpdateLines();
                DasmPanel.Update();
            }
        }

        private void UpdateDATA()
        {
            DataPanel.UpdateLines();
            DataPanel.Update();
        }
        
        private bool dasmPanel_CheckExecuting(object Sender, ushort ADDR)
        {
            if (m_spectrum.IsRunning) return false;
            if (ADDR == m_spectrum.CPU.regs.PC) return true;
            return false;
        }

        private void dasmPanel_GetDasm(object Sender, ushort ADDR, out string DASM, out int len)
        {
            var mnemonic = m_dasmTool.GetMnemonic(ADDR, out len);
            var timing = m_timingTool.GetTimingString(ADDR);

            DASM = string.Format("{0,-24} ; {1}", mnemonic, timing);
        }

        private void dasmPanel_GetData(object Sender, ushort ADDR, int len, out byte[] data)
        {
            data = new byte[len];
            for (int i = 0; i < len; i++)
            {
                data[i] = m_spectrum.ReadMemory((ushort)(ADDR + i));
            }
        }

        private bool dasmPanel_CheckBreakpoint(object sender, ushort addr)
        {
            foreach (var bp in m_spectrum.GetBreakpointList())
            {
                if (bp.Address.HasValue && bp.Address == addr)
                    return true;
            }
            return false;
        }

        private void dasmPanel_SetBreakpoint(object sender, ushort addr)
        {
            bool found = false;
            foreach (var bp in m_spectrum.GetBreakpointList())
            {
                if (bp.Address.HasValue && bp.Address == addr)
                {
                    m_spectrum.RemoveBreakpoint(bp);
                    found = true;
                }
            }
            if (!found)
            {
                var bp = new Breakpoint(addr);
                m_spectrum.AddBreakpoint(bp);
            }
        }

        public void DasmGoToPC()
        {
            DasmPanel.ActiveAddress = m_spectrum.CPU.regs.PC;
            DasmPanel.UpdateLines();
            DasmPanel.Update();
        }

        public void ClearBreakpoints()
        {
            m_spectrum.ClearBreakpoints();
            UpdateCPU(false);
        }

        public void DasmRefresh()
        {
            DasmPanel.UpdateLines();
            DasmPanel.Update();
        }

        public void ToggleFlag(int lineNumber)
        {
            if (m_spectrum.IsRunning) return;
            m_spectrum.CPU.regs.F ^= (byte)(0x80 >> lineNumber);
            UpdateREGS();
        }

        public delegate void ChangeRegFunc(ref ushort p, string reg);
        public void ChangeRegByIndex(int index, ChangeRegFunc changeReg)
        {
            switch (index)
            {
                case 0:
                    changeReg(ref m_spectrum.CPU.regs.PC, "PC");
                    break;
                case 1:
                    changeReg(ref m_spectrum.CPU.regs.IR, "IR");
                    break;
                case 2:
                    changeReg(ref m_spectrum.CPU.regs.SP, "SP");
                    break;
                case 3:
                    changeReg(ref m_spectrum.CPU.regs.AF, "AF");
                    break;
                case 4:
                    changeReg(ref m_spectrum.CPU.regs.HL, "HL");
                    break;
                case 5:
                    changeReg(ref m_spectrum.CPU.regs.DE, "DE");
                    break;
                case 6:
                    changeReg(ref m_spectrum.CPU.regs.BC, "BC");
                    break;
                case 7:
                    changeReg(ref m_spectrum.CPU.regs.IX, "IX");
                    break;
                case 8:
                    changeReg(ref m_spectrum.CPU.regs.IY, "IY");
                    break;
                case 9:
                    changeReg(ref m_spectrum.CPU.regs._AF, "AF'");
                    break;
                case 10:
                    changeReg(ref m_spectrum.CPU.regs._HL, "HL'");
                    break;
                case 11:
                    changeReg(ref m_spectrum.CPU.regs._DE, "DE'");
                    break;
                case 12:
                    changeReg(ref m_spectrum.CPU.regs._BC, "BC'");
                    break;
                case 13:
                    changeReg(ref m_spectrum.CPU.regs.MW, "MW (Memptr Word)");
                    break;
            }
        }

        public void DataRefresh()
        {
            DataPanel.UpdateLines();
            DataPanel.Update();
        }

        public void ResetCpuState(int lineNumber)
        {
            if (m_spectrum.IsRunning)
                return;
            switch (lineNumber)
            {
                case 0:     //iff
                    m_spectrum.CPU.IFF1 = m_spectrum.CPU.IFF2 = !m_spectrum.CPU.IFF1;
                    break;
                case 1:     //halt
                    m_spectrum.CPU.HALTED = !m_spectrum.CPU.HALTED;
                    break;
                case 3:     //im
                    m_spectrum.CPU.IM++;
                    if (m_spectrum.CPU.IM > 2)
                        m_spectrum.CPU.IM = 0;
                    break;
            }
            UpdateCPU(false);
        }

        public delegate string GetFilenameFunc(string dialogTitle, string filter);
        public void SaveDataToFile(GetFilenameFunc getFilename)
        {
            // TODO: store last values as default
            int s_addr = 0x4000;
            int s_len = 6912;

            var fileName = getFilename("Load Block...", "All files (*.*)|*.*");
            
            FileInfo fileInfo = new FileInfo(fileName);
            s_len = (int)fileInfo.Length;

            if (s_len < 1)
                return;
            var service = Locator.Resolve<IUserQuery>();
            if (!service.QueryValue("Load Block", "Memory Address:", "#{0:X4}", ref s_addr, 0, 0xFFFF))
                return;
            if (!service.QueryValue("Load Block", "Block Length:", "#{0:X4}", ref s_len, 0, 0x10000))
                return;

            byte[] data = new byte[s_len];
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                fs.Read(data, 0, data.Length);
            m_spectrum.WriteMemory((ushort)s_addr, data, 0, s_len);
        }

        public void ReadDataFromFile(GetFilenameFunc getFilename)
        {
            int s_addr = 0x4000;
            int s_len = 6912;
            
            var service = Locator.Resolve<IUserQuery>();
            if (!service.QueryValue("Save Block", "Memory Address:", "#{0:X4}", ref s_addr, 0, 0xFFFF))
                return;
            if (!service.QueryValue("Save Block", "Block Length:", "#{0:X4}", ref s_len, 0, 0x10000))
                return;

            var fileName = getFilename("Save Block...", "Binary Files (*.bin)|*.bin|All files (*.*)|*.*");
            byte[] data = new byte[s_len];
            m_spectrum.ReadMemory((ushort)s_addr, data, 0, s_len);

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                fs.Write(data, 0, data.Length);
        }
    }
}