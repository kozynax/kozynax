using System;
using System.Collections.Generic;

namespace Kozynax.UI
{
    public class DataPanelComponent
    {
        private ushort _topAddress;
        private int _colCount;

        public class LineElement
        {
            public LineElement(string text, int firstCol, bool selected = false)
            {
                Text = text;
                FirstCol = firstCol;
                Selected = selected;
            }

            public bool Selected { get; }
            public string Text { get; }
            public int FirstCol { get; }
        }
        public class Line
        {
            public Line(List<LineElement> lineElements, bool selected = false)
            {
                LineElements = lineElements;
                Selected = selected;
            }

            public bool Selected { get; }
            public IReadOnlyList<LineElement> LineElements { get; }
        }
        
        public delegate void ONCLICKCPU(object Sender, ushort Addr);
        public delegate void ONGETDATACPU(object Sender, ushort ADDR, int len, out byte[] data);

        public event ONCLICKCPU DataClick = null;
        public event ONGETDATACPU GetData = null;
        public event EventHandler Redraw;
        
        ushort[] fADDRS = null;
        byte[][] fBytesDATAS = null;
        
        public int VisibleLineCount {get; set; }

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

        public int ColCount
        {
            get { return _colCount; }
            set
            {
                _colCount = value;
                UpdateLines();
                Update();
            }
        }

        public void Update() => Redraw?.Invoke(this, EventArgs.Empty);

        private int fLineCount => VisibleLineCount;
        
        public int ActiveLine { get; set; }
        public int ActiveColumn {get; set; }

        public DataPanelComponent()
        {
            ColCount = 8;
        }

        public void EditValue(int line, int col)
        {
            if (DataClick != null)
                DataClick(this, (ushort)(fADDRS[line] + col));
            UpdateLines();
        }
        
        public IReadOnlyList<Line> GetLines()
        {
            const int addrWidth = 4;
            const int blockGap = 2;

            var lines = new List<Line>();

            for (int line = 0; line < VisibleLineCount; line++)
            {
                var lineElements = new List<LineElement>();
                lineElements.Add(new LineElement(fADDRS[line].ToString("X4"), 0));

                for (int col = 0; col < ColCount; col++)
                {
                    var isActive = (line == ActiveLine) && (col == ActiveColumn); 

                    lineElements.Add(new LineElement(fBytesDATAS[line][col].ToString("X2"), addrWidth + blockGap + 3 * col, isActive));
                    string sch = new String(zxencode[fBytesDATAS[line][col]], 1);
                    lineElements.Add(new LineElement(sch, addrWidth + blockGap + (3 * ColCount - 1) + blockGap + col, isActive));
                }
                lines.Add(new Line(lineElements));
            }

            return lines;
        }
        
        public void UpdateLines()
        {
            fADDRS = new ushort[fLineCount];
            fBytesDATAS = new byte[fLineCount][];

            ushort CurADDR = TopAddress;
            for (int i = 0; i < fLineCount; i++)
            {
                fADDRS[i] = CurADDR;

                if (GetData != null)
                    GetData(this, CurADDR, ColCount, out fBytesDATAS[i]);
                else
                {
                    fBytesDATAS[i] = new byte[ColCount];
                    for (int j = 0; j < ColCount; j++)
                        fBytesDATAS[i][j] = (byte)((TopAddress + i * ColCount + j) & 0xFF);
                }
                CurADDR += (ushort)ColCount;
            }
        }

        public void ControlUp()
        {
            ActiveLine--;
            if (ActiveLine < 0)
            {
                ActiveLine++;
                TopAddress -= (ushort)ColCount;
                UpdateLines();
            }
        }

        public void ControlDown()
        {
            ActiveLine++;
            if (ActiveLine >= VisibleLineCount)
            {
                TopAddress += (ushort)ColCount;
                ActiveLine--;
                UpdateLines();
            }
        }

        public void ControlLeft()
        {
            ActiveColumn--;
            if (ActiveColumn < 0)
            {
                ActiveColumn = ColCount - 1;
                ControlUp();
            }
            else
                UpdateLines();
        }

        public void ControlRight()
        {
            ActiveColumn++;
            if (ActiveColumn >= ColCount)
            {
                ActiveColumn = 0;
                ControlDown();
            }
            else
                UpdateLines();
        }

        public void ControlPageUp()
        {
            TopAddress -= (ushort)(ColCount * VisibleLineCount);
            UpdateLines();
        }

        public void ControlPageDown()
        {
            TopAddress += (ushort)(ColCount * VisibleLineCount);
            UpdateLines();
        }
        
        static char[] zxencode = new char[256]
        {
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // 00..0F
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // 10..1F
            ' ','!','"','#','$','%','&','\'','(',')','*','+',',','-','.','/', // 20..2F
            '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',  // 30..3F
            '@','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',  // 40..4F
            'P','Q','R','S','T','U','V','W','X','Y','Z','[','\\',']','↑','_', // 50..5F
            '₤','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',  // 60..6F
            'p','q','r','s','t','u','v','w','x','y','z','{','|','}','~','©',  // 70..7F

            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // 80..8F
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // 90..9F
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // A0..AF
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // B0..BF
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // C0..CF
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // D0..DF
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // E0..EF
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',  // F0..FF
        };
    }
}