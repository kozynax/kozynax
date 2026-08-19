using System;
using Kozui.Abstract;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware.Sprinter;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    [KozuiDialog(CaptureBackdrop = false)]
    public class SprinterDebuggerDialog : DebuggerDialog
    {
        private SprinterMMU sprint_mmu;
        private SprinterULA sprint_ula;

        public ListView<string> ExtendedVariables { get; } = new ListView<string>
        {
            ItemTextSelector = s => s ?? string.Empty,
            ActivateOnClick = false,
            ActivateOnSecondClick = false,
            MinWidth = 22,
            MinHeight = 12,
        };

        public SprinterDebuggerDialog()
        {
            ExtendedVariables.ItemActivated += (_, __) =>
            {
                if (ExtendedVariables.SelectedIndex >= 0)
                    ResetExtendedVariable(ExtendedVariables.SelectedIndex);
            };
        }

        protected override string TitleText => "Sprinter Debugger";

        protected override int SidePanelMinWidth => 24;

        protected override void ConfigureSidePanel(DockPanel side)
        {
            RegistersList.Dock = Dock.Top;
            RegistersList.Margin = new Thickness(0, 0, 0, 1);
            FlagsList.Dock = Dock.Top;
            FlagsList.Margin = new Thickness(0, 0, 0, 1);

            ExtendedVariables.Dock = Dock.Bottom;
            ExtendedVariables.Margin = new Thickness(0, 1, 0, 0);
            StatesList.Dock = Dock.Fill;

            side.Add(RegistersList);
            side.Add(FlagsList);
            side.Add(ExtendedVariables);
            side.Add(StatesList);
        }

        public override void Init(IDebuggable debugTarget)
        {
            base.Init(debugTarget);

            if (debugTarget != null)
            {
                sprint_mmu = m_spectrum.Bus.FindDevice<SprinterMMU>();
                sprint_ula = m_spectrum.Bus.FindDevice<SprinterULA>();
            }
        }

        protected override void UpdateREGS()
        {
            base.UpdateREGS();
            UpdateExtendedVariables();
        }

        public void UpdateExtendedVariables()
        {
            if (sprint_mmu == null)
            {
                ExtendedVariables.Reset();
                return;
            }

            var lines = new System.Collections.Generic.List<string>();
            void OutInfo(string msg) => lines.Add(msg);
            void OutBit(string name, byte val, byte bit)
                => OutInfo("  " + name + "=" + (((val & (1 << bit)) != 0) ? "1" : "0"));

            OutInfo("Variables----------");
            OutInfo("  SYS:         " + (sprint_mmu.SYS ? " On" : "Off"));
            OutInfo("  RA16:        " + (sprint_mmu.RA16 ? " On" : "Off"));

            OutInfo("");

            OutInfo("Port XX7C/XX3C-----");
            OutBit("D0(ON/OFF)", sprint_mmu.SYSPORT, 0);
            OutBit("D1(TRB/ExpROM)", sprint_mmu.SYSPORT, 1);
            OutBit("D2(DCP0)", sprint_mmu.SYSPORT, 2);
            OutBit("D3(DCP1)", sprint_mmu.SYSPORT, 3);
            OutBit("D4(DCP2)", sprint_mmu.SYSPORT, 4);
            OutBit("D7(P128 ON)", sprint_mmu.SYSPORT, 7);

            OutInfo("");

            OutInfo("Port 89(RGADR): " + string.Format("#{0:X2}", sprint_mmu.RGADR));
            OutInfo("Port C9(RGMOD): " + string.Format("#{0:X2}", sprint_mmu.RGMOD));
            OutInfo("");
            OutInfo("Port 82(PAGE0): " + string.Format("#{0:X2}", sprint_mmu.PAGE0));
            OutInfo("Port A2(PAGE1): " + string.Format("#{0:X2}", sprint_mmu.PAGE1));
            OutInfo("Port C2(PAGE2): " + string.Format("#{0:X2}", sprint_mmu.PAGE2));
            OutInfo("Port E2(PAGE3): " + string.Format("#{0:X2}", sprint_mmu.PAGE3));

            OutInfo("Port 7FFD:      " + string.Format("#{0:X2}", sprint_mmu.CMR0));
            OutInfo("Port 1FFD:      " + string.Format("#{0:X2}", sprint_mmu.CMR1));

            OutInfo("");

            var selected = ExtendedVariables.SelectedIndex;
            ExtendedVariables.Reset(lines);
            if (selected >= 0 && selected < ExtendedVariables.Count)
                ExtendedVariables.SelectedIndex = selected;
            else if (ExtendedVariables.Count > 0)
                ExtendedVariables.SelectedIndex = 0;
        }

        private void Update1FFD()
        {
            int num = sprint_mmu.CMR1;
            var service = Locator.Resolve<IUserQuery>();
            if (service.QueryValue("Value of 1FFD port", "New value:", "#{0:X2}", ref num, 0, 0xff))
                sprint_mmu.CMR1 = (byte)num;
        }

        private void Update7FFD()
        {
            int num = sprint_mmu.CMR0;
            var service = Locator.Resolve<IUserQuery>();
            if (service.QueryValue("Value of 7FFD port", "New value:", "#{0:X2}", ref num, 0, 0xff))
                sprint_mmu.CMR0 = (byte)num;
        }

        public void ResetExtendedVariable(int lineNumber)
        {
            if (sprint_mmu == null)
                return;

            switch (lineNumber)
            {
                case 1: sprint_mmu.SYS = !sprint_mmu.SYS; break;
                case 2: sprint_mmu.RA16 = !sprint_mmu.RA16; break;
                case 19: Update7FFD(); break;
                case 20: Update1FFD(); break;
            }
            UpdateREGS();
            UpdateDASM(false);
            UpdateDATA();
            SyncPanelLists();
        }
    }
}
