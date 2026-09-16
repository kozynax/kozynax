/* 
 *  Copyright 2007, 2015 Alex Makeev
 * 
 *  This file is part of ZXMAK2 (ZX Spectrum virtual machine).
 *
 *  ZXMAK2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ZXMAK2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ZXMAK2.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  Description: Z80 CPU Emulator [ED prefixed opcode part]
 *  Date: 13.04.2007
 * 
 */


using System;
namespace ZXMAK2.Engine.Cpu.Processor
{
    public partial class Z80Cpu
    {
        private void ED_LDI(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var val = RDMEM(regs.HL); Tact += 3;

            WRMEM(regs.DE, val); Tact += 3;
            WRNOMREQ(regs.DE); Tact++;
            WRNOMREQ(regs.DE); Tact++;

            regs.HL++;
            regs.DE++;
            regs.BC--;
            val += regs.A;

            regs.F = (byte)((regs.F & CpuFlags.NotHPNF3F5) |
                (val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            if (regs.BC != 0) regs.F |= CpuFlags.P;
        }

        private void ED_CPI(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var cf = regs.F & CpuFlags.C;
            var val = RDMEM(regs.HL); Tact += 3;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;

            regs.HL++;
            regs.F = (byte)(CpuTables.Cpf8b[regs.A * 0x100 + val] + cf);
            if (--regs.BC != 0) regs.F |= CpuFlags.P;
            regs.MW++;
        }

        private void ED_INI(byte cmd)   // INI [16T]
        {
            // 16T (4, 5, 3, 4)

            RDNOMREQ(regs.IR); Tact++;

            var val = RDPORT(regs.BC); Tact += 3;

            WRMEM(regs.HL, val); Tact += 3;

            regs.MW = (ushort)(regs.BC + 1);
            regs.HL++;
            regs.B--;

            //FUSE
            byte flgtmp = (byte)(val + regs.C + 1);
            regs.F = (byte)(CpuTables.Logf[regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ regs.B] & CpuFlags.P) != 0) regs.F |= CpuFlags.P;
            if (flgtmp < val) regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) regs.F |= CpuFlags.N;

            Tact++; //?? really?
        }

        private void ED_OUTI(byte cmd)  // OUTI [16T]
        {
            // 16 (4, 5, 3, 4)

            RDNOMREQ(regs.IR); Tact++;
            regs.B--;

            var val = RDMEM(regs.HL); Tact += 3;

            WRPORT(regs.BC, val); Tact += 3;

            regs.MW = (ushort)(regs.BC + 1);
            regs.HL++;

            //FUSE
            byte flgtmp = (byte)(val + regs.L);
            regs.F = (byte)(CpuTables.Logf[regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ regs.B] & CpuFlags.P) != 0) regs.F |= CpuFlags.P;
            if (flgtmp < val) regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) regs.F |= CpuFlags.N;

            Tact++; //?? really?
        }

        private void ED_LDD(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var val = RDMEM(regs.HL); Tact += 3;

            WRMEM(regs.DE, val); Tact += 3;
            WRNOMREQ(regs.DE); Tact++;
            WRNOMREQ(regs.DE); Tact++;

            regs.HL--;
            regs.DE--;
            regs.BC--;
            val += regs.A;

            regs.F = (byte)((regs.F & CpuFlags.NotHPNF3F5) |
                (val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            if (regs.BC != 0) regs.F |= CpuFlags.P;
        }

        private void ED_CPD(byte cmd)
        {
            // 16T (4, 4, 3, 5)

            var cf = regs.F & CpuFlags.C;
            var val = RDMEM(regs.HL); Tact += 3;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;

            regs.HL--;
            regs.BC--;
            regs.MW--;
            regs.F = (byte)(CpuTables.Cpf8b[regs.A * 0x100 + val] + cf);
            if (regs.BC != 0) regs.F |= CpuFlags.P;
        }

        private void ED_IND(byte cmd)   // IND [16T]
        {
            // 16T (4, 5, 3, 4)

            RDNOMREQ(regs.IR); Tact++;

            var val = RDPORT(regs.BC); Tact += 3;

            WRMEM(regs.HL, val); Tact += 3;

            regs.MW = (ushort)(regs.BC - 1);
            regs.HL--;
            regs.B--;

            //FUSE
            byte flgtmp = (byte)(val + regs.C - 1);
            regs.F = (byte)(CpuTables.Logf[regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ regs.B] & CpuFlags.P) != 0) regs.F |= CpuFlags.P;
            if (flgtmp < val) regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) regs.F |= CpuFlags.N;

            Tact++; // ?? really?
        }

        private void ED_OUTD(byte cmd)  // OUTD [16T]
        {
            // 16T (4, 5, 3, 4)

            RDNOMREQ(regs.IR); Tact++;

            regs.B--;
            var val = RDMEM(regs.HL); Tact += 3;

            WRPORT(regs.BC, val); Tact += 3;

            regs.MW = (ushort)(regs.BC - 1);
            regs.HL--;

            //FUSE
            byte flgtmp = (byte)(val + regs.L);
            regs.F = (byte)(CpuTables.Logf[regs.B] & CpuFlags.NotP);
            if ((CpuTables.Logf[(flgtmp & 0x07) ^ regs.B] & CpuFlags.P) != 0) regs.F |= CpuFlags.P;
            if (flgtmp < val) regs.F |= CpuFlags.HC;
            if ((val & 0x80) != 0) regs.F |= CpuFlags.N;
            
            Tact++; // ?? really?
        }

        private void ED_LDIR(byte cmd)
        {
            //BC==0 => 16T (4, 4, 3, 5)
            //BC!=0 => 21T (4, 4, 3, 5, 5)

            var val = RDMEM(regs.HL); Tact += 3;

            WRMEM(regs.DE, val); Tact += 3;
            WRNOMREQ(regs.DE); Tact++;
            WRNOMREQ(regs.DE); Tact++;

            regs.BC--;
            val += regs.A;

            regs.F &= CpuFlags.NotHPNF3F5;
            if (regs.BC != 0)
            {
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                regs.PC--;
                regs.MW = regs.PC;
                regs.PC--;

                // YF = PCi.13; XF = PCi.11
                regs.F |= (byte)(regs.PCH & CpuFlags.F3F5);
                regs.F |= CpuFlags.P;
            }
            else
            {
                // YF = PCi.13; XF = PCi.11
                regs.F |= (byte)((val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            }
            regs.HL++;
            regs.DE++;
        }

        private void ED_CPIR(byte cmd)
        {
            //BC==0 => 16T (4, 4, 3, 5)
            //BC!=0 => 21T (4, 4, 3, 5, 5)

            regs.MW++;
            var cf = regs.F & CpuFlags.C;
            var val = RDMEM(regs.HL); Tact += 3;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;

            regs.BC--;
            regs.F = (byte)(CpuTables.Cpf8b[regs.A * 0x100 + val] + cf);

            if (regs.BC != 0)
            {
                regs.F |= CpuFlags.P;
                if ((regs.F & CpuFlags.Z) == 0)
                {
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    regs.PC--;
                    regs.MW = regs.PC;
                    regs.PC--;

                    // YF = PCi.13; XF = PCi.11
                    regs.F &= CpuFlags.NotF3F5;
                    regs.F |= (byte)(regs.PCH & CpuFlags.F3F5);
                }
            }
            regs.HL++;
        }

        private void ED_INIR(byte cmd)      // INIR [16T/21T]
        {
            // B==0 => 16T (4, 5, 3, 4)
            // B!=0 => 21T (4, 5, 3, 4, 5)

            RDNOMREQ(regs.IR); Tact++;

            var io = RDPORT(regs.BC); Tact += 3;
            regs.MW = (ushort)(regs.BC + 1);
            var word = regs.MW & 0xff;
            regs.B--;
            WRMEM(regs.HL, io); Tact += 4;

            if (regs.B != 0)
            {
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                regs.PC -= 2;
                regs.MW = (ushort)(regs.PC + 1);
            }
            regs.HL++;
            FLAGS_INxR_OTxR(io, word);
        }

        private void ED_OTIR(byte cmd)  // OTIR [16T/21T]
        {
            // B==0 => 16T (4, 5, 3, 4)
            // B!=0 => 21T (4, 5, 3, 4, 5)

            RDNOMREQ(regs.IR); Tact++;

            var io = RDMEM(regs.HL); Tact += 3;
            regs.HL++;
            var l = regs.L;
            regs.B--;
            WRPORT(regs.BC, io); Tact += 4;
            regs.MW = (ushort)(regs.BC + 1);

            if (regs.B != 0)
            {
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                regs.PC -= 2;
                regs.MW = (ushort)(regs.PC + 1);
            }
            FLAGS_INxR_OTxR(io, l);
        }

        private void ED_LDDR(byte cmd)
        {
            //BC==0 => 16T (4, 4, 3, 5)
            //BC!=0 => 21T (4, 4, 3, 5, 5)

            var val = RDMEM(regs.HL); Tact += 3;

            WRMEM(regs.DE, val); Tact += 3;
            WRNOMREQ(regs.DE); Tact++;
            WRNOMREQ(regs.DE); Tact++;

            regs.BC--;
            val += regs.A;

            regs.F &= CpuFlags.NotHPNF3F5;
            if (regs.BC != 0)
            {
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                WRNOMREQ(regs.DE); Tact++;
                regs.PC--;
                regs.MW = regs.PC;
                regs.PC--;

                // YF = PCi.13; XF = PCi.11
                regs.F |= (byte)(regs.PCH & CpuFlags.F3F5);
                regs.F |= CpuFlags.P;
            }
            else
            {
                // YF = PCi.13; XF = PCi.11
                regs.F |= (byte)((val & CpuFlags.F3) | ((val << 4) & CpuFlags.F5));
            }
            regs.HL--;
            regs.DE--;
        }

        private void ED_CPDR(byte cmd)
        {
            // BC==0 => 16T (4, 4, 3, 5)
            // BC!=0 => 21T (4, 4, 3, 5, 5)

            regs.MW--;
            var cf = regs.F & CpuFlags.C;
            var val = RDMEM(regs.HL); Tact += 3;

            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            regs.BC--;
            regs.F = (byte)(CpuTables.Cpf8b[regs.A * 0x100 + val] + cf);

            if (regs.BC != 0)
            {
                regs.F |= CpuFlags.P;
                if ((regs.F & CpuFlags.Z) == 0)
                {
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    RDNOMREQ(regs.HL); Tact++;
                    regs.PC--;
                    regs.MW = regs.PC;
                    regs.PC--;

                    // YF = PCi.13; XF = PCi.11
                    regs.F &= CpuFlags.NotF3F5;
                    regs.F |= (byte)(regs.PCH & CpuFlags.F3F5);
                }
            }
            regs.HL--;
        }

        private void ED_INDR(byte cmd)      // INDR [16T/21T]
        {
            // B==0 => 16 (4, 5, 3, 4)
            // B!=0 => 21 (4, 5, 3, 4, 5)

            RDNOMREQ(regs.IR); Tact++;

            var io = RDPORT(regs.BC); Tact += 3;
            regs.MW = (ushort)(regs.BC - 1);
            var word = regs.MW & 0xff;
            regs.B--;
            WRMEM(regs.HL, io); Tact += 4;

            if (regs.B != 0)
            {
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                WRNOMREQ(regs.HL); Tact++;
                regs.PC -= 2;
                regs.MW = (ushort)(regs.PC + 1);
            }
            regs.HL--;
            FLAGS_INxR_OTxR(io, word);
        }

        private void ED_OTDR(byte cmd)  //OTDR [16T/21T]
        {
            // B==0 => 16T (4, 5, 3, 4)
            // B!=0 => 21T (4, 5, 3, 4, 5)

            RDNOMREQ(regs.IR); Tact++;

            var io = RDMEM(regs.HL); Tact += 3;
            regs.HL--;
            var l = regs.L;
            regs.B--;
            WRPORT(regs.BC, io); Tact += 4;
            regs.MW = (ushort)(regs.BC - 1);

            if (regs.B != 0)
            {
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                RDNOMREQ(regs.BC); Tact++;
                regs.PC -= 2;
                regs.MW = (ushort)(regs.PC + 1);
            }
            FLAGS_INxR_OTxR(io, l);
        }

        /// <summary>
        /// Undocumented flag calculation for repeating block I/O (INIR/OTIR/INDR/OTDR).
        /// On repeat, MEMPTR (MW) is already set to PC+1 by the caller.
        /// </summary>
        private void FLAGS_INxR_OTxR(byte io, int word)
        {
            var t = io + word;
            var nf = (io >> 6) & CpuFlags.N;
            var pIndex = (t & 7) ^ regs.B;
            int f;

            if (regs.B != 0)
            {
                // SF = B.7; YF/XF from PCH; ZF = 0
                f = (regs.B & CpuFlags.S) | (regs.PCH & CpuFlags.F3F5);
                if (t > 255)
                {
                    f |= CpuFlags.C;
                    if (nf != 0)
                    {
                        pIndex ^= (regs.B - 1) & 7;
                        if ((regs.B & 0x0f) == 0x00)
                            f |= CpuFlags.H;
                    }
                    else
                    {
                        pIndex ^= (regs.B + 1) & 7;
                        if ((regs.B & 0x0f) == 0x0f)
                            f |= CpuFlags.H;
                    }
                }
                else
                {
                    pIndex ^= regs.B & 7;
                }
            }
            else
            {
                // ZF = 1; SF, YF, XF = 0
                f = CpuFlags.Z;
                if (t > 255)
                    f |= CpuFlags.HC;
            }

            regs.F = (byte)(f | nf | CpuTables.Parity[pIndex]);
        }

        private void ED_INRC(byte cmd)      // in R,(c)  [12T] 
        {
            // 12T (4, 4, 4)
            var r = (cmd & 0x38) >> 3;

            regs.MW = regs.BC;
            var pval = RDPORT(regs.BC);
            regs.MW++;
            if (r != CpuRegId.F)
                _regSetters[r](pval);
            regs.F = (byte)(CpuTables.Logf[pval] | (regs.F & CpuFlags.C));
            Tact += 4;
        }

        private void ED_OUTCR_NMOS(byte cmd)     // out (c),R [12T]
        {
            // 12T (4, 4, 4)
            var r = (cmd & 0x38) >> 3;

            regs.MW = regs.BC;
            if (r != CpuRegId.F)
                WRPORT(regs.BC, _regGetters[r]());
            else
                WRPORT(regs.BC, 0x00);	// 0 for Z80 and 0xFF for Z84
            regs.MW++;
            Tact += 4;
        }

        private void ED_OUTCR_CMOS(byte cmd)     // out (c),R [12T]
        {
            // 12T (4, 4, 4)
            var r = (cmd & 0x38) >> 3;

            regs.MW = regs.BC;
            if (r != CpuRegId.F)
                WRPORT(regs.BC, _regGetters[r]());
            else
                WRPORT(regs.BC, 0xFF);	// 0 for Z80 and 0xFF for Z84
            regs.MW++;
            Tact += 4;
        }

        private void ED_ADCHLRR(byte cmd)   // adc hl,RR
        {
            var rr = (cmd & 0x30) >> 4;

            RDNOMREQ(regs.IR); Tact++;
            RDNOMREQ(regs.IR); Tact++;
            RDNOMREQ(regs.IR); Tact++;
            RDNOMREQ(regs.IR); Tact++;
            RDNOMREQ(regs.IR); Tact++;
            RDNOMREQ(regs.IR); Tact++;
            RDNOMREQ(regs.IR); Tact++;

            regs.MW = (ushort)(regs.HL + 1);
            byte fl = (byte)((((regs.HL & 0x0FFF) + (_pairGetters[rr]() & 0x0FFF) + (regs.F & CpuFlags.C)) >> 8) & CpuFlags.H);
            uint tmp = (uint)((regs.HL & 0xFFFF) + (_pairGetters[rr]() & 0xFFFF) + (regs.F & CpuFlags.C));  // AF???
            if ((tmp & 0x10000) != 0) fl |= CpuFlags.C;
            if ((tmp & 0xFFFF) == 0) fl |= CpuFlags.Z;
            int ri = (int)(short)regs.HL + (int)(short)_pairGetters[rr]() + (int)(regs.F & CpuFlags.C);
            if (ri < -0x8000 || ri >= 0x8000) fl |= CpuFlags.P;
            regs.HL = (ushort)tmp;
            regs.F = (byte)(fl | (regs.H & CpuFlags.SF3F5));
        }

        private void ED_SBCHLRR(byte cmd)   // sbc hl,RR
        {
            var rr = (cmd & 0x30) >> 4;

            RDNOMREQ(regs.IR); Tact += 1;
            RDNOMREQ(regs.IR); Tact += 1;
            RDNOMREQ(regs.IR); Tact += 1;
            RDNOMREQ(regs.IR); Tact += 1;
            RDNOMREQ(regs.IR); Tact += 1;
            RDNOMREQ(regs.IR); Tact += 1;
            RDNOMREQ(regs.IR); Tact += 1;

            regs.MW = (ushort)(regs.HL + 1);
            byte fl = CpuFlags.N;
            fl |= (byte)((((regs.HL & 0x0FFF) - (_pairGetters[rr]() & 0x0FFF) - (regs.F & CpuFlags.C)) >> 8) & CpuFlags.H);
            uint tmp = (uint)((regs.HL & 0xFFFF) - (_pairGetters[rr]() & 0xFFFF) - (regs.F & CpuFlags.C));  // AF???
            if ((tmp & 0x10000) != 0) fl |= CpuFlags.C;
            if ((tmp & 0xFFFF) == 0) fl |= CpuFlags.Z;
            int ri = (int)(short)regs.HL - (int)(short)_pairGetters[rr]() - (int)(regs.F & CpuFlags.C);
            if (ri < -0x8000 || ri >= 0x8000) fl |= CpuFlags.P;
            regs.HL = (ushort)tmp;
            regs.F = (byte)(fl | (regs.H & CpuFlags.SF3F5));
        }

        private void ED_LDRR_NN_(byte cmd)  // ld RR,(NN)
        {
            // 20T (4, 4, 3, 3, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            var adr = (ushort)RDMEM(regs.PC);
            regs.PC++;
            Tact += 3;

            adr += (ushort)(RDMEM(regs.PC) * 0x100);
            regs.PC++;
            regs.MW = (ushort)(adr + 1);
            Tact += 3;

            var val = (ushort)RDMEM(adr);
            Tact += 3;

            val += (ushort)(RDMEM(regs.MW) * 0x100);
            _pairSetters[rr](val);
            Tact += 3;
        }

        private void ED_LD_NN_RR(byte cmd)  // ld (NN),RR
        {
            // 20 (4, 4, 3, 3, 3, 3)
            var rr = (cmd & 0x30) >> 4;

            var adr = (ushort)RDMEM(regs.PC);
            regs.PC++;
            Tact += 3;

            adr += (ushort)(RDMEM(regs.PC) * 0x100);
            regs.PC++;
            regs.MW = (ushort)(adr + 1);
            var val = _pairGetters[rr]();
            Tact += 3;

            WRMEM(adr, (byte)val);
            Tact += 3;

            WRMEM(regs.MW, (byte)(val >> 8));
            Tact += 3;
        }

        private void ED_RETN(byte cmd)      // reti/retn
        {
            // 14T (4, 4, 3, 3)

            IFF1 = IFF2;
            var adr = (ushort)RDMEM(regs.SP);
            Tact += 3;

            adr += (ushort)(RDMEM(++regs.SP) * 0x100);
            ++regs.SP;
            regs.PC = adr;
            regs.MW = adr;
            Tact += 3;
        }

        private void ED_IM(byte cmd)        // im X
        {
            var mode = (byte)((cmd & 0x18) >> 3);
            if (mode < 2) mode = 1;
            mode--;

            IM = mode;
        }

        private void ED_LDXRA(byte cmd)     // ld I/R,a
        {
            var ir = (cmd & 0x08) == 0;
            
            RDNOMREQ(regs.IR); Tact++;

            if (ir)   // I
                regs.I = regs.A;
            else
                regs.R = regs.A;
        }

        private void ED_LDAXR(byte cmd)     // ld a,I/R
        {
            var ir = (cmd & 0x08) == 0;

            RDNOMREQ(regs.IR); Tact++;

            if (ir)   // I
                regs.A = regs.I;
            else
                regs.A = regs.R;

            regs.F = (byte)(((regs.F & CpuFlags.C) | CpuTables.Logf[regs.A]) & CpuFlags.NotP);
            switch (_type)
            {
                case CpuType.ZILOG_NMOS:
                case CpuType.NEC_NMOS:
                case CpuType.ST_CMOS:
                    var handler = SCANSIG;
                    if (handler != null)
                    {
                        handler();
                    }
                    if (!(INT && IFF1) && IFF2)
                    {
                        regs.F |= CpuFlags.P;
                    }
                    break;
                case CpuType.ZILOG_CMOS:
                    if (IFF2)
                    {
                        regs.F |= CpuFlags.P;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void ED_RRD(byte cmd)       // RRD
        {
            // 18T (4, 4, 3, 4, 3)

            var tmp = RDMEM(regs.HL); Tact += 3;

            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;

            regs.MW = (ushort)(regs.HL + 1);
            var val = (byte)((regs.A << 4) | (tmp >> 4));

            WRMEM(regs.HL, val); Tact += 3;
            regs.A = (byte)((regs.A & 0xF0) | (tmp & 0x0F));
            regs.F = (byte)(CpuTables.Logf[regs.A] | (regs.F & CpuFlags.C));
        }

        private void ED_RLD(byte cmd)       // RLD
        {
            // 18T (4, 4, 3, 4, 3)

            var tmp = RDMEM(regs.HL); Tact += 3;

            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;
            RDNOMREQ(regs.HL); Tact++;

            regs.MW = (ushort)(regs.HL + 1);
            var val = (byte)((regs.A & 0x0F) | (tmp << 4));

            WRMEM(regs.HL, val); Tact += 3;
            regs.A = (byte)((regs.A & 0xF0) | (tmp >> 4));
            regs.F = (byte)(CpuTables.Logf[regs.A] | (regs.F & CpuFlags.C));
        }

        private void ED_NEG(byte cmd)       // NEG
        {
            regs.F = CpuTables.Sbcf[regs.A];
            regs.A = (byte)-regs.A;
        }
    }
}
