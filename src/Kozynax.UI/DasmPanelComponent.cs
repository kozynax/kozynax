using System.Collections.Generic;
using System.Drawing;
using Kozynax.UI.Base;

namespace Kozynax.UI
{
    public class DasmPanelComponent : DebugPanelComponent
    {
        public delegate bool ONCHECKCPU(object Sender, ushort ADDR);
        public delegate void ONGETDATACPU(object Sender, ushort ADDR, int len, out byte[] data);
        public delegate void ONGETDASMCPU(object Sender, ushort ADDR, out string DASM, out int len);
        public delegate void ONCLICKCPU(object Sender, ushort Addr);

        public event ONCHECKCPU CheckBreakpoint;
        public event ONCHECKCPU CheckExecuting;
        public event ONGETDATACPU GetData;
        public event ONGETDASMCPU GetDasm;
        public event ONCLICKCPU BreakpointClick;
        public event ONCLICKCPU DasmClick;
        
        private ushort _topAddress;
        private int LineCount => VisibleLineCount+ 3;
        public int VisibleLineCount { get; set; }
        public int ActiveLine { get; set; }
        ushort[] fADDRS;
        string[] fStrADDRS;
        string[] fStrDATAS;
        string[] fStrDASMS;
        bool[] fBreakpoints;

        public Color BreakColor { get; set; } = Color.Red;
        public Color BreakForeColor { get; set; } = Color.Black;
        
        public ushort TopAddress
        {
            get { return _topAddress; }
            set
            {
                _topAddress = value;
                ActiveLine = 0;
                UpdateLines();
                Update();
            }
        }

        public ushort ActiveAddress
        {
            get
            {
                if (fADDRS != null && ActiveLine >= 0 && ActiveLine < fADDRS.Length)
                    return fADDRS[ActiveLine];
                return 0;
            }
            set
            {
                // fADDRS is only allocated by UpdateLines(); on the very first call
                // (e.g. opening the debugger for the first time) it may still be null.
                if (fADDRS != null)
                {
                    for (int i = 0; i <= VisibleLineCount; i++)
                        if (fADDRS[i] == value)
                        {
                            if (ActiveLine != i)
                            {
                                if (i == VisibleLineCount)
                                {
                                    _topAddress = fADDRS[1];
                                    ActiveLine = i - 1;
                                }
                                else
                                    ActiveLine = i;
                            }
                            UpdateLines();
                            Update();
                            return;
                        }
                }
                TopAddress = value;
            }
        }

        public void EditValue(int lineNumber)
        {
            if (DasmClick != null)
                DasmClick(this, fADDRS[lineNumber]);
            UpdateLines();
        }
        
        public void ToggleBreakpoint(int lineNumber)
        {
            if (BreakpointClick != null)
                BreakpointClick(this, fADDRS[lineNumber]);
            UpdateLines();
        }
        
        public List<Line> GetLines()
        {
            const int addrWidth = 4;
            const int blockGap = 2;
            const int hexWidth = 16;

            var result = new List<Line>();
            
            for (int line = 0; line < VisibleLineCount; line++)
            {
                bool breakLine = fBreakpoints[line];
                bool execLine = false;
                if (CheckExecuting != null)
                    execLine = CheckExecuting(this, fADDRS[line]);

                result.Add(new Line(
                    new List<LineElement>()
                    {
                        new LineElement(fStrADDRS[line], 0),
                        new LineElement(fStrDATAS[line], addrWidth + blockGap),
                        new LineElement(fStrDASMS[line], addrWidth + blockGap + hexWidth)
                    }, 
                    line == ActiveLine,
                    (execLine ? Icons.Arrow : Icons.No) | (breakLine ? Icons.Breakpoint : Icons.No)));
            }

            return result;
        }
        
        public void UpdateLines()
        {
            fADDRS = new ushort[LineCount];
            fStrADDRS = new string[LineCount];
            fStrDATAS = new string[LineCount];
            fStrDASMS = new string[LineCount];
            fBreakpoints = new bool[LineCount];

            ushort CurADDR = _topAddress;
            for (int i = 0; i < LineCount; i++)
            {
                fADDRS[i] = CurADDR;

                fStrADDRS[i] = CurADDR.ToString("X4");
                string dasm;
                int len;
                byte[] data;

                if (GetDasm != null)
                    GetDasm(this, CurADDR, out dasm, out len);
                else
                {
                    dasm = "???";
                    len = 1;
                }
                if (GetData != null)
                    GetData(this, CurADDR, len, out data);
                else
                    data = new byte[] { 0x00 };

                fStrDASMS[i] = dasm;
                string sdata = "";
                int maxdata = data.Length;
                if (maxdata > 7) maxdata = 7;
                for (int j = 0; j < maxdata; j++)
                    sdata += data[j].ToString("X2");
                if (maxdata < data.Length)
                    sdata += "..";
                fStrDATAS[i] = sdata;
                if (CheckBreakpoint != null)
                {
                    if (CheckBreakpoint(this, CurADDR))
                        fBreakpoints[i] = true;
                }
                else
                    fBreakpoints[i] = false;
                CurADDR += (ushort)len;
            }
        }

        public void ControlUp()
        {
            ActiveLine--;
            if (ActiveLine < 0)
            {
                ActiveLine++;
                _topAddress = (ushort)(fADDRS[0] - 1);
                UpdateLines();
            }
        }

        public void ControlDown()
        {
            ActiveLine++;
            if (ActiveLine >= VisibleLineCount)
            {
                _topAddress = fADDRS[1];
                ActiveLine--;
                UpdateLines();
            }
        }

        public void ControlPageUp()
        {
            for (int i = 0; i < (VisibleLineCount - 1); i++)
            {
                _topAddress--;
                UpdateLines();
            }
        }

        public void ControlPageDown()
        {
            if (VisibleLineCount > 0)
            {
                _topAddress = fADDRS[VisibleLineCount - 1];
                UpdateLines();
            }
            _topAddress = fADDRS[1];
            UpdateLines();
        }
    }
}