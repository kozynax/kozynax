using System;
using System.Windows.Forms;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Hardware.WinForms.General;
using ZXMAK2.Host.Entities;

namespace ZXMAK2.Hardware.WinForms.Sprinter
{
    public class DebugForm : FormCpu, IDebuggerSprinterView
    {
        private Panel panel1;
        private ListBox PentEvoRegs;

        private SprinterDebuggerDialog _dialog;
        
        #region Initialize
        
        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            this.panel1 = new System.Windows.Forms.Panel();
            this.PentEvoRegs = new System.Windows.Forms.ListBox();
            this.panel1.SuspendLayout();
            
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.PentEvoRegs);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(193, 416);
            this.panel1.TabIndex = 1;
            // 
            // PentEvoRegs
            // 
            this.PentEvoRegs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PentEvoRegs.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.PentEvoRegs.FormattingEnabled = true;
            this.PentEvoRegs.ItemHeight = 14;
            this.PentEvoRegs.Location = new System.Drawing.Point(0, 0);
            this.PentEvoRegs.Name = "PentEvoRegs";
            this.PentEvoRegs.Size = new System.Drawing.Size(193, 416);
            this.PentEvoRegs.TabIndex = 0;
            this.PentEvoRegs.DoubleClick += new EventHandler(PentEvoRegs_OnDBLClick);
            
            this.Controls.Add(this.panel1);
            this.panel1.ResumeLayout(false);
            
            this.panelStatus.Location = new System.Drawing.Point(726, 0);
            this.panelStatus.Size = new System.Drawing.Size(168, 416);
            this.panelState.Location = new System.Drawing.Point(0, 224);
            this.panelState.Size = new System.Drawing.Size(168, 192);
            this.listState.Location = new System.Drawing.Point(0, 0);
            this.listState.Size = new System.Drawing.Size(164, 188);
            this.splitter3.Location = new System.Drawing.Point(0, 221);
            this.splitter3.Size = new System.Drawing.Size(168, 3);
            this.panelRegs.Location = new System.Drawing.Point(0, 0);
            this.panelRegs.Size = new System.Drawing.Size(168, 221);
            this.listF.Location = new System.Drawing.Point(101, 0);
            this.listF.Size = new System.Drawing.Size(63, 217);
            this.splitter4.Location = new System.Drawing.Point(98, 0);
            this.splitter4.Size = new System.Drawing.Size(3, 217);
            this.listREGS.Location = new System.Drawing.Point(0, 0);
            this.listREGS.Size = new System.Drawing.Size(98, 217);
            this.splitter1.Location = new System.Drawing.Point(723, 0);
            this.splitter1.Size = new System.Drawing.Size(3, 416);
            this.panelMem.Location = new System.Drawing.Point(193, 294);
            this.panelMem.Size = new System.Drawing.Size(530, 122);
            this.dataPanel.Location = new System.Drawing.Point(0, 0);
            this.dataPanel.Size = new System.Drawing.Size(526, 118);
            this.splitter2.Location = new System.Drawing.Point(193, 291);
            this.splitter2.Size = new System.Drawing.Size(530, 3);
            this.panelDasm.Location = new System.Drawing.Point(193, 0);
            this.panelDasm.Size = new System.Drawing.Size(530, 291);
            this.dasmPanel.Location = new System.Drawing.Point(0, 0);
            this.dasmPanel.Size = new System.Drawing.Size(526, 287);
            this.ClientSize = new System.Drawing.Size(894, 416);
        }
        #endregion

        protected override void Dialog_CpuDetailsUpdated(object sender, EventArgs e)
        {
            base.Dialog_CpuDetailsUpdated(sender, e);
            
            PentEvoRegs.Items.Clear();
            foreach (var line in _dialog.ExtendedVariables.List)
                PentEvoRegs.Items.Add(line);
        }

        private void PentEvoRegs_OnDBLClick(object sender, EventArgs args)
            => _dialog.ResetExtendedVariable(PentEvoRegs.SelectedIndex);

        public void Init(SprinterDebuggerDialog ui)
        {
            _dialog = ui;
            (this as IViewImplementation<DebuggerDialog>).Init(ui);
        }

        public DlgResult ShowDialog(object owner)
            => (this as IViewImplementation<DebuggerDialog>).ShowDialog(owner);
    }
}
