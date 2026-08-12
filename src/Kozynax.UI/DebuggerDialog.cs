using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kozui.Abstract;
using Kozynax.UI.Base;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Cpu.Tools;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    public class DebuggerDialog : ViewDescription<DebuggerDialog>
    {
        public event EventHandler Breakpoint;
        public event EventHandler UpdateState;
        public event EventHandler CpuDetailsUpdated;
        public event EventHandler CloseRequested;

        public delegate void OnRunningStateChange(bool isRunning);
        public event OnRunningStateChange RunningStateChanged;

        protected IDebuggable m_spectrum;
        private DasmTool m_dasmTool;
        private TimingTool m_timingTool;

        public Panel Root { get; private set; }
        public DataPanelComponent DataPanel { get; }
        public DasmPanelComponent DasmPanel { get; }
        public ListView<string> DasmList { get; }
        public ListView<string> DataList { get; }
        public ListView<string> RegistersList { get; }
        public ListView<string> FlagsList { get; }
        public ListView<string> StatesList { get; }
        public Button StepIntoButton { get; }
        public Button StepOverButton { get; }
        public Button RunButton { get; }
        public Button StopButton { get; }
        public Button GotoPcButton { get; }
        public Button CloseButton { get; }

        public IDebuggable Target => m_spectrum;

        public DebuggerDialog()
        {
            DasmPanel = new DasmPanelComponent { VisibleLineCount = 16 };
            DasmPanel.CheckBreakpoint += dasmPanel_CheckBreakpoint;
            DasmPanel.CheckExecuting += dasmPanel_CheckExecuting;
            DasmPanel.GetData += dasmPanel_GetData;
            DasmPanel.GetDasm += dasmPanel_GetDasm;
            DasmPanel.BreakpointClick += dasmPanel_SetBreakpoint;

            DataPanel = new DataPanelComponent { VisibleLineCount = 8, ColCount = 8 };
            DataPanel.GetData += dasmPanel_GetData;
            DataPanel.DataClick += DataPanel_DataClick;

            DasmList = new ListView<string>
            {
                ItemTextSelector = s => s ?? string.Empty,
                // Enter still toggles; mouse needs a real double-click (WinForms-style).
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                Dock = Dock.Fill,
                MinWidth = 48,
                MinHeight = 12,
            };
            DataList = new ListView<string>
            {
                ItemTextSelector = s => s ?? string.Empty,
                // Enter / real double-click poke; single click only selects the byte.
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                SuppressRowHighlight = true,
                Dock = Dock.Fill,
                MinWidth = 40,
                // 8 data rows + 2-cell list chrome (TerminalKozuiPresenter padding).
                MinHeight = 10,
            };
            DataList.GetHighlightSpans = GetDataHighlightSpans;
            DasmList.GetRowColors = GetDasmRowColors;
            RegistersList = new ListView<string>
            {
                ItemTextSelector = s => s ?? string.Empty,
                ActivateOnClick = true,
                MinWidth = 16,
                MinHeight = 14,
            };
            FlagsList = new ListView<string>
            {
                ItemTextSelector = s => s ?? string.Empty,
                // Enter still toggles; mouse needs a real double-click.
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                MinWidth = 10,
                MinHeight = 8,
            };
            StatesList = new ListView<string>
            {
                ItemTextSelector = s => s ?? string.Empty,
                // Enter still toggles; mouse needs a real double-click.
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                MinWidth = 18,
                MinHeight = 8,
            };

            StepIntoButton = new Button { Text = "F7 Into" };
            StepOverButton = new Button { Text = "F8 Over" };
            RunButton = new Button { Text = "F9 Run" };
            StopButton = new Button { Text = "F5 Stop" };
            GotoPcButton = new Button { Text = "Goto PC" };
            CloseButton = new Button { Text = "Close" };

            StepIntoButton.Clicked += (_, __) => StepInto();
            StepOverButton.Clicked += (_, __) => StepOver();
            RunButton.Clicked += (_, __) => Run();
            StopButton.Clicked += (_, __) => Stop();
            GotoPcButton.Clicked += (_, __) => DasmGoToPC();
            CloseButton.Clicked += (_, __) => CloseRequested?.Invoke(this, EventArgs.Empty);

            DasmList.ItemActivated += (_, __) => ToggleDasmBreakpoint();
            DasmList.SelectedIndexChanged += (_, index) =>
            {
                if (index >= 0 && index < DasmPanel.VisibleLineCount)
                    DasmPanel.ActiveLine = index;
            };
            DataList.ItemActivated += (_, __) => EditSelectedDataByte();
            DataList.SelectedIndexChanged += (_, index) =>
            {
                if (index >= 0 && index < DataPanel.VisibleLineCount)
                    DataPanel.ActiveLine = index;
            };
            RegistersList.ItemActivated += (_, __) => EditSelectedRegister();
            FlagsList.ItemActivated += (_, __) =>
            {
                if (FlagsList.SelectedIndex >= 0)
                    ToggleFlag(FlagsList.SelectedIndex);
            };
            StatesList.ItemActivated += (_, __) =>
            {
                if (StatesList.SelectedIndex >= 0)
                    ResetCpuState(StatesList.SelectedIndex);
            };

            Root = BuildTree();
        }

        public virtual void Init(IDebuggable debugTarget)
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
            if (m_spectrum != null)
            {
                m_spectrum.UpdateState -= spectrum_OnUpdateState;
                m_spectrum.Breakpoint -= spectrum_OnBreakpoint;
            }
        }

        private void spectrum_OnUpdateState(object sender, EventArgs args)
            => UpdateState?.Invoke(sender, args);

        private void spectrum_OnBreakpoint(object sender, EventArgs args)
            => Breakpoint?.Invoke(sender, args);

        public void UpdateCPU(bool updatePC)
        {
            if (m_spectrum == null)
                return;
            if (m_spectrum.IsRunning)
                updatePC = false;

            RunningStateChanged?.Invoke(m_spectrum.IsRunning);
            UpdateREGS();
            UpdateDASM(updatePC);
            UpdateDATA();
            SyncPanelLists();
        }

        public void SyncPanelLists()
        {
            var dasmLines = new List<string>();
            foreach (var line in DasmPanel.GetLines())
                dasmLines.Add(FormatDasmLine(line));
            DasmList.Reset(dasmLines);
            if (DasmPanel.ActiveLine >= 0 && DasmPanel.ActiveLine < DasmList.Count)
                DasmList.SelectedIndex = DasmPanel.ActiveLine;
            else if (DasmList.Count > 0)
                DasmList.SelectedIndex = 0;

            var dataLines = new List<string>();
            foreach (var line in DataPanel.GetLines())
                dataLines.Add(FormatDataLine(line));
            DataList.Reset(dataLines);
            if (DataPanel.ActiveLine >= 0 && DataPanel.ActiveLine < DataList.Count)
                DataList.SelectedIndex = DataPanel.ActiveLine;
            else if (DataList.Count > 0)
                DataList.SelectedIndex = 0;
        }

        /// <summary>
        /// Match WinForms disasm panel: visible row count follows arranged height.
        /// Hex view stays fixed at 8 rows.
        /// </summary>
        public void FitVisibleLines(int dasmRows)
        {
            if (dasmRows <= 0 || DasmPanel.VisibleLineCount == dasmRows)
                return;

            DasmPanel.VisibleLineCount = dasmRows;
            if (DasmPanel.ActiveLine >= dasmRows)
                DasmPanel.ActiveLine = dasmRows - 1;

            DasmPanel.UpdateLines();
            SyncPanelLists();
        }

        public void DasmNavigateUp()
        {
            DasmPanel.ControlUp();
            DasmPanel.Update();
            SyncPanelLists();
        }

        public void DasmNavigateDown()
        {
            DasmPanel.ControlDown();
            DasmPanel.Update();
            SyncPanelLists();
        }

        public void DasmNavigatePageUp()
        {
            DasmPanel.ControlPageUp();
            DasmPanel.Update();
            SyncPanelLists();
        }

        public void DasmNavigatePageDown()
        {
            DasmPanel.ControlPageDown();
            DasmPanel.Update();
            SyncPanelLists();
        }

        public void DataNavigateUp()
        {
            DataPanel.ControlUp();
            DataPanel.Update();
            SyncPanelLists();
        }

        public void DataNavigateDown()
        {
            DataPanel.ControlDown();
            DataPanel.Update();
            SyncPanelLists();
        }

        public void DataNavigatePageUp()
        {
            DataPanel.ControlPageUp();
            DataPanel.Update();
            SyncPanelLists();
        }

        public void DataNavigatePageDown()
        {
            DataPanel.ControlPageDown();
            DataPanel.Update();
            SyncPanelLists();
        }

        public void DataNavigateLeft()
        {
            DataPanel.ControlLeft();
            DataPanel.Update();
            SyncPanelLists();
        }

        public void DataNavigateRight()
        {
            DataPanel.ControlRight();
            DataPanel.Update();
            SyncPanelLists();
        }

        /// <summary>Select a byte cell in the hex view (row + column).</summary>
        public void DataSelectCell(int line, int column)
        {
            if (line < 0 || line >= DataPanel.VisibleLineCount)
                return;
            if (column < 0 || column >= DataPanel.ColCount)
                return;
            DataPanel.ActiveLine = line;
            DataPanel.ActiveColumn = column;
            DataPanel.Update();
            SyncPanelLists();
        }

        private static string FormatDasmLine(Line line)
        {
            var prefix = new StringBuilder(2);
            prefix.Append((line.Icons & Icons.Arrow) != 0 ? '>' : ' ');
            prefix.Append((line.Icons & Icons.Breakpoint) != 0 ? '*' : ' ');
            var addr = line.LineElements.Count > 0 ? line.LineElements[0].Text : string.Empty;
            var bytes = line.LineElements.Count > 1 ? line.LineElements[1].Text : string.Empty;
            var dasm = line.LineElements.Count > 2 ? line.LineElements[2].Text : string.Empty;
            return string.Format("{0}{1} {2,-8} {3}", prefix, addr, bytes, dasm);
        }

        private static string FormatDataLine(Line line)
        {
            if (line?.LineElements == null || line.LineElements.Count == 0)
                return string.Empty;

            var els = line.LineElements;
            var addr = els[0].Text ?? string.Empty;
            var hex = new StringBuilder(3 * 8);
            var chars = new StringBuilder(8);

            // DataPanel.GetLines stores [addr, hex0, char0, hex1, char1, ...].
            for (var i = 1; i + 1 < els.Count; i += 2)
            {
                if (hex.Length > 0)
                    hex.Append(' ');
                hex.Append(els[i].Text ?? "??");
                chars.Append(DisplayChar(els[i + 1].Text));
            }

            return string.Format("{0}  {1}  {2}", addr, hex, chars);
        }

        private IReadOnlyList<ListTextSpan> GetDataHighlightSpans(int row)
        {
            if (row != DataPanel.ActiveLine)
                return null;

            var col = DataPanel.ActiveColumn;
            if (col < 0 || col >= DataPanel.ColCount)
                return null;

            // "XXXX  HH HH HH HH HH HH HH HH  cccccccc"
            var hexStart = 6 + col * 3;
            var charStart = 6 + (DataPanel.ColCount * 3 - 1) + 2 + col;
            return new[]
            {
                new ListTextSpan(hexStart, 2),
                new ListTextSpan(charStart, 1),
            };
        }

        private ListRowColors? GetDasmRowColors(int row)
        {
            if (!DasmPanel.IsBreakpointLine(row))
                return null;

            // Match WinForms DasmPanel: BreakColor paper + BreakForeColor ink.
            var bg = DasmPanel.BreakColor;
            var fg = DasmPanel.BreakForeColor;
            return new ListRowColors(
                bg.R, bg.G, bg.B,
                fg.R, fg.G, fg.B);
        }

        private static char DisplayChar(string text)
        {
            if (string.IsNullOrEmpty(text))
                return '.';
            var ch = text[0];
            // Keep printable ASCII; map the rest so the terminal font can draw it.
            if (ch >= 0x20 && ch <= 0x7E)
                return ch;
            return '.';
        }

        protected virtual void UpdateREGS()
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

        protected void UpdateDASM(bool updatePC)
        {
            if (!m_spectrum.IsRunning && updatePC)
                DasmPanel.ActiveAddress = m_spectrum.CPU.regs.PC;
            else
            {
                DasmPanel.UpdateLines();
                DasmPanel.Update();
            }
        }

        protected void UpdateDATA()
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
            if (m_spectrum == null)
                return;
            DasmPanel.ActiveAddress = m_spectrum.CPU.regs.PC;
            DasmPanel.UpdateLines();
            DasmPanel.Update();
            SyncPanelLists();
        }

        public void DasmGoToAddress()
        {
            int addr = DasmPanel.TopAddress;
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("Disassembly Address", "New Address:", "#{0:X4}", ref addr, 0, 0xFFFF))
                return;
            DasmPanel.TopAddress = (ushort)addr;
            SyncPanelLists();
        }

        public void DataGoToAddress()
        {
            int addr = DataPanel.TopAddress;
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("Data Panel Address", "New Address:", "#{0:X4}", ref addr, 0, 0xFFFF))
                return;
            DataPanel.TopAddress = (ushort)addr;
            SyncPanelLists();
        }

        public void ClearBreakpoints()
        {
            if (m_spectrum == null)
                return;
            m_spectrum.ClearBreakpoints();
            UpdateCPU(false);
        }

        public void DasmRefresh()
        {
            DasmPanel.UpdateLines();
            DasmPanel.Update();
            SyncPanelLists();
        }

        public void ToggleFlag(int lineNumber)
        {
            if (m_spectrum == null || m_spectrum.IsRunning) return;
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
            SyncPanelLists();
        }

        public void ResetCpuState(int lineNumber)
        {
            if (m_spectrum == null || m_spectrum.IsRunning)
                return;
            switch (lineNumber)
            {
                case 0:
                    m_spectrum.CPU.IFF1 = m_spectrum.CPU.IFF2 = !m_spectrum.CPU.IFF1;
                    break;
                case 1:
                    m_spectrum.CPU.HALTED = !m_spectrum.CPU.HALTED;
                    break;
                case 3:
                    m_spectrum.CPU.IM++;
                    if (m_spectrum.CPU.IM > 2)
                        m_spectrum.CPU.IM = 0;
                    break;
                case 8:
                    EditFrameTact();
                    break;
            }
            UpdateCPU(false);
        }

        public void StepInto()
        {
            if (m_spectrum == null)
                return;
            if (m_spectrum.IsRunning)
            {
                Stop();
                return;
            }
            try
            {
                m_spectrum.DoStepInto();
            }
            catch (Exception ex)
            {
                global::ZXMAK2.Logger.Error(ex);
                Locator.TryResolve<IUserMessage>()?.ErrorDetails(ex);
            }
            UpdateCPU(true);
        }

        public void StepOver()
        {
            if (m_spectrum == null)
                return;
            if (m_spectrum.IsRunning)
            {
                Stop();
                return;
            }
            try
            {
                m_spectrum.DoStepOver();
            }
            catch (Exception ex)
            {
                global::ZXMAK2.Logger.Error(ex);
                Locator.TryResolve<IUserMessage>()?.ErrorDetails(ex);
            }
            UpdateCPU(true);
        }

        public void Run()
        {
            if (m_spectrum == null)
                return;
            m_spectrum.DoRun();
            UpdateCPU(false);
        }

        public void Stop()
        {
            if (m_spectrum == null)
                return;
            m_spectrum.DoStop();
            UpdateCPU(true);
        }

        public void ResetCpu()
        {
            if (m_spectrum == null || m_spectrum.IsRunning)
                return;
            m_spectrum.DoReset();
            UpdateCPU(true);
        }

        private void ToggleDasmBreakpoint()
        {
            if (m_spectrum == null || DasmList.SelectedIndex < 0)
                return;
            DasmPanel.ActiveLine = DasmList.SelectedIndex;
            DasmPanel.ToggleBreakpoint(DasmList.SelectedIndex);
            UpdateCPU(false);
        }

        /// <summary>Select a disassembly row (mouse click).</summary>
        public void DasmSelectLine(int line)
        {
            if (line < 0 || line >= DasmPanel.VisibleLineCount)
                return;
            DasmPanel.ActiveLine = line;
            if (line < DasmList.Count)
                DasmList.SelectedIndex = line;
        }

        /// <summary>Toggle breakpoint on the selected disassembly row.</summary>
        public void ToggleSelectedDasmBreakpoint()
            => ToggleDasmBreakpoint();

        /// <summary>Open the POKE dialog for the currently selected hex byte.</summary>
        public void EditSelectedDataByte()
        {
            if (m_spectrum == null)
                return;
            if (DataPanel.ActiveLine < 0 || DataPanel.ActiveLine >= DataPanel.VisibleLineCount)
                return;
            if (DataPanel.ActiveColumn < 0 || DataPanel.ActiveColumn >= DataPanel.ColCount)
                return;

            var addr = (ushort)(DataPanel.TopAddress
                                + DataPanel.ActiveLine * DataPanel.ColCount
                                + DataPanel.ActiveColumn);
            PokeAddress(addr);
        }

        private void DataPanel_DataClick(object sender, ushort addr)
            => PokeAddress(addr);

        /// <summary>Match WinForms: poke is allowed even while the CPU is running.</summary>
        private void PokeAddress(ushort addr)
        {
            if (m_spectrum == null)
                return;

            var poked = (int)m_spectrum.ReadMemory(addr);
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("POKE #" + addr.ToString("X4"), "Value:", "#{0:X2}", ref poked, 0, 0xFF))
                return;
            m_spectrum.WriteMemory(addr, (byte)poked);
            UpdateCPU(false);
        }

        private void EditSelectedRegister()
        {
            if (m_spectrum == null || m_spectrum.IsRunning || RegistersList.SelectedIndex < 0)
                return;
            ChangeRegByIndex(RegistersList.SelectedIndex, ChangeReg);
            UpdateCPU(false);
        }

        private void ChangeReg(ref ushort p, string reg)
        {
            int val = p;
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("Change Register " + reg, "New value:", "#{0:X4}", ref val, 0, 0xFFFF))
                return;
            p = (ushort)val;
        }

        private void EditFrameTact()
        {
            int frameTact = m_spectrum.GetFrameTact();
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("Frame Tact", "New Frame Tact:", "{0}", ref frameTact, 0, m_spectrum.FrameTactCount))
                return;
            int delta = frameTact - m_spectrum.GetFrameTact();
            if (delta < 0)
                delta += m_spectrum.FrameTactCount;
            m_spectrum.CPU.Tact += delta;
        }

        public delegate string GetFilenameFunc(string dialogTitle, string filter);

        /// <summary>Load a binary block into memory (hex view Ctrl+L).</summary>
        public void LoadMemoryBlock()
        {
            if (m_spectrum == null)
                return;
            SaveDataToFile(PickOpenFileName);
            UpdateCPU(false);
        }

        /// <summary>Save a memory block to a binary file (hex view Ctrl+S).</summary>
        public void SaveMemoryBlock()
        {
            if (m_spectrum == null)
                return;
            ReadDataFromFile(PickSaveFileName);
        }

        public void SaveDataToFile(GetFilenameFunc getFilename)
        {
            if (m_spectrum == null || getFilename == null)
                return;

            int s_addr = DataPanel.TopAddress;
            int s_len = 6912;

            var fileName = getFilename("Load Block...", "All files (*.*)|*.*");
            if (string.IsNullOrEmpty(fileName))
                return;

            FileInfo fileInfo = new FileInfo(fileName);
            s_len = (int)fileInfo.Length;

            if (s_len < 1)
                return;
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("Load Block", "Memory Address:", "#{0:X4}", ref s_addr, 0, 0xFFFF))
                return;
            if (!service.QueryValue("Load Block", "Block Length:", "#{0:X4}", ref s_len, 0, 0x10000))
                return;

            byte[] data = new byte[s_len];
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                fs.Read(data, 0, data.Length);
            m_spectrum.WriteMemory((ushort)s_addr, data, 0, s_len);
            DataPanel.TopAddress = (ushort)s_addr;
            SyncPanelLists();
        }

        public void ReadDataFromFile(GetFilenameFunc getFilename)
        {
            if (m_spectrum == null || getFilename == null)
                return;

            int s_addr = DataPanel.TopAddress;
            int s_len = 6912;

            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;
            if (!service.QueryValue("Save Block", "Memory Address:", "#{0:X4}", ref s_addr, 0, 0xFFFF))
                return;
            if (!service.QueryValue("Save Block", "Block Length:", "#{0:X4}", ref s_len, 0, 0x10000))
                return;

            var fileName = getFilename("Save Block...", "Binary Files (*.bin)|*.bin|All files (*.*)|*.*");
            if (string.IsNullOrEmpty(fileName))
                return;
            byte[] data = new byte[s_len];
            m_spectrum.ReadMemory((ushort)s_addr, data, 0, s_len);

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                fs.Write(data, 0, data.Length);
        }

        private static string PickOpenFileName(string title, string filter)
        {
            var dialog = Locator.TryResolve<IOpenFileDialog>();
            if (dialog == null)
                return null;
            using (dialog)
            {
                dialog.Title = title;
                dialog.Filter = filter;
                dialog.FileName = string.Empty;
                dialog.CheckFileExists = true;
                dialog.ShowReadOnly = false;
                if (dialog.ShowDialog(null) != DlgResult.OK)
                    return null;
                return dialog.FileName;
            }
        }

        private static string PickSaveFileName(string title, string filter)
        {
            var dialog = Locator.TryResolve<ISaveFileDialog>();
            if (dialog == null)
                return null;
            using (dialog)
            {
                dialog.Title = title;
                dialog.Filter = filter;
                dialog.DefaultExt = "bin";
                dialog.FileName = string.Empty;
                dialog.OverwritePrompt = true;
                if (dialog.ShowDialog(null) != DlgResult.OK)
                    return null;
                return dialog.FileName;
            }
        }

        private Panel BuildTree()
        {
            var title = new Label
            {
                Text = "Debugger",
                Dock = Dock.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 1),
            };

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };
            toolbar.Add(StepIntoButton);
            toolbar.Add(StepOverButton);
            toolbar.Add(RunButton);
            toolbar.Add(StopButton);
            toolbar.Add(GotoPcButton);
            toolbar.Add(CloseButton);

            RegistersList.Dock = Dock.Top;
            RegistersList.Margin = new Thickness(0, 0, 0, 1);
            FlagsList.Dock = Dock.Top;
            FlagsList.Margin = new Thickness(0, 0, 0, 1);
            StatesList.Dock = Dock.Fill;

            var side = new DockPanel
            {
                Dock = Dock.Right,
                MinWidth = 20,
                Margin = new Thickness(1, 0, 0, 0),
            };
            side.Add(RegistersList);
            side.Add(FlagsList);
            side.Add(StatesList);

            DataList.Dock = Dock.Bottom;
            DataList.Margin = new Thickness(0, 1, 0, 0);
            // Keep hex view at a fixed 8-row content height (MinHeight includes chrome).
            DataList.MinHeight = 10;
            DataList.VerticalAlignment = VerticalAlignment.Top;
            DasmList.Dock = Dock.Fill;

            var center = new DockPanel
            {
                Dock = Dock.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            center.Add(DataList);
            center.Add(DasmList);

            var body = new DockPanel
            {
                Dock = Dock.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            body.Add(side);
            body.Add(center);

            var content = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(1),
                MinWidth = 70,
                MinHeight = 24,
            };
            content.Add(title);
            content.Add(toolbar);
            content.Add(body);

            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(1),
            };

            var root = new Panel();
            root.Add(frame);
            return root;
        }
    }
}
