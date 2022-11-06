/// Description: CPU Debug Window
/// Author: Alex Makeev
/// Date: 18.03.2008
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Cpu.Tools;
using ZXMAK2.Dependency;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.WinForms.Views;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;
using ZXMAK2.Resources;


namespace ZXMAK2.Hardware.WinForms.General
{
    public partial class FormCpu : FormView, IDebuggerGeneralView
    {
        private IDebuggable m_spectrum;
        private DebuggerDialog _dialog;
        private IViewImplementation<DebuggerDialog> _viewImplementationImplementation;

        public FormCpu()
        {
            InitializeComponent();

            // remove gap from the sizing-grip
            statusStrip.Padding = new Padding(
                statusStrip.Padding.Left,
                statusStrip.Padding.Top,
                statusStrip.Padding.Left,
                statusStrip.Padding.Bottom);
            LoadImages();

            toolStripContinue.ToolTipText += " (F9)";
            toolStripBreak.ToolTipText += " (F5)";
            toolStripStepInto.ToolTipText += " (F7)";
            toolStripStepOver.ToolTipText += " (F8)";
        }

        private void LoadImages()
        {
            toolStripContinue.Image = ResourceImages.DebuggerContinue;
            toolStripBreak.Image = ResourceImages.DebuggerBreak;
            toolStripStepInto.Image = ResourceImages.DebuggerStepInto;
            toolStripStepOver.Image = ResourceImages.DebuggerStepOver;
            toolStripStepOut.Image = ResourceImages.DebuggerStepOut;
            toolStripShowNext.Image = ResourceImages.DebuggerShowNext;
            toolStripBreakpoints.Image = ResourceImages.DebuggerShowBreakpoints;

            menuFileClose.Image = ResourceImages.DebuggerClose;
            menuDebugContinue.Image = ResourceImages.DebuggerContinue;
            menuDebugBreak.Image = ResourceImages.DebuggerBreak;
            menuDebugStepInto.Image = ResourceImages.DebuggerStepInto;
            menuDebugStepOver.Image = ResourceImages.DebuggerStepOver;
            menuDebugStepOut.Image = ResourceImages.DebuggerStepOut;
            menuDebugShowNext.Image = ResourceImages.DebuggerShowNext;
        }

        protected virtual void Dialog_CpuDetailsUpdated(object sender, EventArgs e)
        {
            listREGS.Items.Clear();
            foreach (var line in _dialog.RegistersList.List)
                listREGS.Items.Add(line);

            listF.Items.Clear();
            foreach (var line in _dialog.FlagsList.List)
                listF.Items.Add(line);
            
            listState.Items.Clear();
            foreach (var line in _dialog.StatesList.List)
                listState.Items.Add(line);
        }

        public void Init(DebuggerDialog ui)
        {
            _dialog = ui;
            
            dataPanel.Init(_dialog.DataPanel);
            dasmPanel.Init(_dialog.DasmPanel);

            _dialog.Breakpoint += spectrum_OnBreakpoint;
            _dialog.UpdateState += spectrum_OnUpdateState;
            _dialog.DataPanel.DataClick += dataPanel_DataClick;
            _dialog.RunningStateChanged += isRunning =>
            {
                dasmPanel.ForeColor = isRunning ? SystemColors.ControlDarkDark : SystemColors.ControlText;
                statusStrip.BackColor = isRunning ? ColorTranslator.FromHtml("#cc6600") : ColorTranslator.FromHtml("#0077cc");
                statusStrip.ForeColor = ColorTranslator.FromHtml("#ffffff");
                toolStripStatus.Text = isRunning ? "Running" : "Ready";
                toolStripStatusTact.Text = isRunning
                    ? string.Format("T: - / {0}", m_spectrum.FrameTactCount)
                    : string.Format("T: {0} / {1}", m_spectrum.GetFrameTact(), m_spectrum.FrameTactCount);
                toolStripStatusTact.Enabled = !isRunning;
                toolStripContinue.Enabled = !isRunning;
                toolStripBreak.Enabled = isRunning;
                toolStripStepInto.Enabled = !isRunning;
                toolStripStepOver.Enabled = !isRunning;
                toolStripStepOut.Enabled = false;
                toolStripShowNext.Enabled = !isRunning;
                toolStripBreakpoints.Enabled = false;
            };
            _dialog.CpuDetailsUpdated += Dialog_CpuDetailsUpdated;
        }
        
        public void Init(IDebuggable debugTarget)
        {
            if (debugTarget != null)
                m_spectrum = debugTarget;
            
            _dialog.Init(debugTarget);
        }
        
        protected void FormCPU_FormClosed(object sender, FormClosedEventArgs e)
            => _dialog.Close();

        protected void FormCPU_Load(object sender, EventArgs e)
        {
            _dialog.UpdateCPU(true);
        }

        protected void FormCPU_Shown(object sender, EventArgs e)
        {
            Show();
            _dialog.UpdateCPU(false);
            dasmPanel.Focus();
            Select();
        }

        protected void spectrum_OnUpdateState(object sender, EventArgs args)
        {
            if (!Created)
                return;
            BeginInvoke(new Action(() => _dialog.UpdateCPU(true)), null);
        }

        protected void spectrum_OnBreakpoint(object sender, EventArgs args)
        {
            //LogAgent.Info("spectrum_OnBreakpoint {0}", sender);
            if (!Created)
                return;
            BeginInvoke(new Action(() =>
            {
                Show();
                _dialog.UpdateCPU(true);
                dasmPanel.Focus();
                Select();
            }), null);
        }

        protected void FormCPU_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F3:              // reset
                    if (m_spectrum.IsRunning)
                        break;
                    m_spectrum.DoReset();
                    _dialog.UpdateCPU(true);
                    break;
                case Keys.F7:              // StepInto
                    if (m_spectrum.IsRunning)
                        break;
                    try
                    {
                        m_spectrum.DoStepInto();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        Locator.Resolve<IUserMessage>().ErrorDetails(ex);
                    }
                    _dialog.UpdateCPU(true);
                    break;
                case Keys.F8:              // StepOver
                    if (m_spectrum.IsRunning)
                        break;
                    try
                    {
                        m_spectrum.DoStepOver();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        Locator.Resolve<IUserMessage>().ErrorDetails(ex);
                    }
                    _dialog.UpdateCPU(true);
                    break;
                case Keys.F9:              // Run
                    m_spectrum.DoRun();
                    _dialog.UpdateCPU(false);
                    break;
                case Keys.F5:              // Stop
                    m_spectrum.DoStop();
                    _dialog.UpdateCPU(true);
                    break;
            }
        }

        protected void menuItemDasmGotoADDR_Click(object sender, EventArgs e)
        {
            int ToAddr = 0;
            var service = Locator.Resolve<IUserQuery>();
            if (service == null)
            {
                return;
            }
            if (!service.QueryValue("Disassembly Address", "New Address:", "#{0:X4}", ref ToAddr, 0, 0xFFFF))
            {
                return;
            }
            _dialog.DasmPanel.TopAddress = (ushort)ToAddr;
        }

        protected void menuItemDasmGotoPC_Click(object sender, EventArgs e)
            => _dialog.DasmGoToPC();

        protected void menuItemDasmClearBP_Click(object sender, EventArgs e)
            => _dialog.ClearBreakpoints();

        protected void menuItemDasmRefresh_Click(object sender, EventArgs e)
            => _dialog.DasmRefresh();

        protected void listF_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listF.SelectedIndex < 0) return;
            _dialog.ToggleFlag(listF.SelectedIndex);
        }

        protected void listREGS_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listREGS.SelectedIndex < 0) return;
            if (m_spectrum.IsRunning) return;
            _dialog.ChangeRegByIndex(listREGS.SelectedIndex, ChangeReg);
        }

        protected void ChangeReg(ref ushort p, string reg)
        {
            int val = p;
            var service = Locator.Resolve<IUserQuery>();
            if (service == null)
            {
                return;
            }
            if (!service.QueryValue("Change Register " + reg, "New value:", "#{0:X4}", ref val, 0, 0xFFFF)) return;
            p = (ushort)val;
            _dialog.UpdateCPU(false);
        }

        // dbg funs
        protected void dataPanel_DataClick(object Sender, ushort Addr)
        {
            int poked;
            poked = m_spectrum.ReadMemory((ushort)Addr);
            var service = Locator.Resolve<IUserQuery>();
            if (service == null)
            {
                return;
            }
            if (!service.QueryValue("POKE #" + Addr.ToString("X4"), "Value:", "#{0:X2}", ref poked, 0, 0xFF)) return;
            m_spectrum.WriteMemory((ushort)Addr, (byte)poked);
            _dialog.UpdateCPU(false);
        }

        protected void menuItemDataGotoADDR_Click(object sender, EventArgs e)
        {
            int adr = _dialog.DataPanel.TopAddress;
            var service = Locator.Resolve<IUserQuery>();
            if (service == null)
            {
                return;
            }
            if (!service.QueryValue("Data Panel Address", "New Address:", "#{0:X4}", ref adr, 0, 0xFFFF)) return;
            _dialog.DataPanel.TopAddress = (ushort)adr;
        }

        protected void menuItemDataRefresh_Click(object sender, EventArgs e)
        {
            _dialog.DataRefresh();
            Refresh();
        }

        protected void menuItemDataSetColumnCount_Click(object sender, EventArgs e)
        {
            int cols = _dialog.DataPanel.ColCount;
            var service = Locator.Resolve<IUserQuery>();
            if (service == null)
            {
                return;
            }
            if (!service.QueryValue("Data Panel Columns", "Column Count:", "{0}", ref cols, 1, 32)) return;
            _dialog.DataPanel.ColCount = cols;
        }

        protected void dasmPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuDasm.Show(dasmPanel, e.Location);
        }

        protected void dataPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuData.Show(dataPanel, e.Location);
        }

        protected void listState_DoubleClick(object sender, EventArgs e)
        {
            if (listState.SelectedIndex < 0) return;
            if (m_spectrum.IsRunning)
                return;
            if (listState.SelectedIndex == 8) //frmT
            {
                int frameTact = m_spectrum.GetFrameTact();
                var service = Locator.Resolve<IUserQuery>();
                if (service.QueryValue("Frame Tact", "New Frame Tact:", "{0}", ref frameTact, 0,
                        m_spectrum.FrameTactCount))
                {
                    int delta = frameTact - m_spectrum.GetFrameTact();
                    if (delta < 0)
                        delta += m_spectrum.FrameTactCount;
                    m_spectrum.CPU.Tact += delta;
                }
            }
            
            _dialog.ResetCpuState(listState.SelectedIndex);
        }

        protected void menuLoadBlock_Click(object sender, EventArgs e)
        {
            _dialog.SaveDataToFile((title, filter) =>
            {
                using (var loadDialog = new OpenFileDialog())
                {
                    loadDialog.SupportMultiDottedExtensions = true;
                    loadDialog.Title = title;
                    loadDialog.Filter = filter;
                    loadDialog.DefaultExt = string.Empty;
                    loadDialog.FileName = null;
                    loadDialog.ShowReadOnly = false;
                    loadDialog.CheckFileExists = true;
                    if (loadDialog.ShowDialog() != DialogResult.OK)
                        return null;

                    return loadDialog.FileName;
                }
            });
        }

        protected void menuSaveBlock_Click(object sender, EventArgs e)
            => _dialog.ReadDataFromFile((title, filter) =>
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.SupportMultiDottedExtensions = true;
                    saveDialog.Title = title;
                    saveDialog.Filter = filter;
                    saveDialog.DefaultExt = "";
                    saveDialog.FileName = "";
                    saveDialog.OverwritePrompt = true;
                    if (saveDialog.ShowDialog() != DialogResult.OK)
                        return null;
                    return saveDialog.FileName;
                }
            });


        #region Toolstrip handlers

        private void menuFileClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuFileLoad_Click(object sender, EventArgs e)
        {
            menuLoadBlock_Click(sender, e);
        }

        private void menuFileSave_Click(object sender, EventArgs e)
        {
            menuSaveBlock_Click(sender, e);
        }

        private void toolStripStatusTact_DoubleClick(object sender, EventArgs e)
        {
            if (m_spectrum == null || m_spectrum.IsRunning) return;
            var frameTact = m_spectrum.GetFrameTact();
            var service = Locator.Resolve<IUserQuery>();
            if (service.QueryValue("Frame Tact", "New Frame Tact:", "{0}", ref frameTact, 0, m_spectrum.FrameTactCount))
            {
                var delta = frameTact - m_spectrum.GetFrameTact();
                if (delta < 0)
                    delta += m_spectrum.FrameTactCount;
                m_spectrum.CPU.Tact += delta;
            }
            _dialog.UpdateCPU(false);
        }

        private void toolStripContinue_Click(object sender, EventArgs e)
        {
            m_spectrum.DoRun();
            _dialog.UpdateCPU(false);
        }

        private void toolStripBreak_Click(object sender, EventArgs e)
        {
            m_spectrum.DoStop();
            _dialog.UpdateCPU(true);
        }

        private void toolStripStepInto_Click(object sender, EventArgs e)
        {
            if (m_spectrum.IsRunning)
                return;
            try
            {
                m_spectrum.DoStepInto();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Locator.Resolve<IUserMessage>().ErrorDetails(ex);
            }
            _dialog.UpdateCPU(true);
        }

        private void toolStripStepOver_Click(object sender, EventArgs e)
        {
            if (m_spectrum.IsRunning)
                return;
            try
            {
                m_spectrum.DoStepOver();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Locator.Resolve<IUserMessage>().ErrorDetails(ex);
            }
            _dialog.UpdateCPU(true);
        }

        private void toolStripStepOut_Click(object sender, EventArgs e)
        {
            Locator.Resolve<IUserMessage>().Error("Not implemented");
        }

        private void toolStripShowNext_Click(object sender, EventArgs e)
        {
            _dialog.DasmPanel.ActiveAddress = m_spectrum.CPU.regs.PC;
            _dialog.DasmGoToPC();
        }

        #endregion Toolstrip handlers

        DlgResult IViewImplementation<DebuggerDialog>.ShowDialog(object owner)
        {
            if (ShowDialog((IWin32Window)owner) == DialogResult.OK)
                return DlgResult.OK;
            return DlgResult.Cancel;
        }
    }
}