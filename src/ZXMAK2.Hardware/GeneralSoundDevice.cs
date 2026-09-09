/*
 *  Copyright 2008, 2015 Alex Makeev
 *
 *  This file is part of ZXMAK2 (ZX Spectrum virtual machine).
 *
 *  Draft General Sound card emulator (ported to current BusDevice/SoundDevice APIs).
 *  IO + GS Z80 side exist; DAC mixing into AudioBuffer is still incomplete.
 */
using System;
using System.IO;
using System.Threading;
using ZXMAK2.Engine.Cpu.Processor;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;


namespace ZXMAK2.Hardware
{
    public class GeneralSoundDevice : SoundDeviceBase
    {
        private Thread m_thread;
        private volatile bool m_running;

        private Z80Cpu m_cpu = new Z80Cpu();
        private byte[][] m_rom;
        private byte[][] m_ram;
        private readonly AutoResetEvent m_event = new AutoResetEvent(false);

        private byte gsdata_out;
        private byte gsdata_in;
        private byte gsstat;
        private byte gscmd;

        private const int C_GRST = 0x80;
        private const int C_GNMI = 0x40;
        private const int C_GLED = 0x20;

        private const long Z80_FQ = 10000000;
        private const long FPS = 50;
        private readonly long FRAME_LEN = Z80_FQ / FPS;

        private const int PAGE = 0x4000;
        private readonly byte[] m_trash = new byte[PAGE];
        private byte[][] gsbankr;
        private byte[][] gsbankw;

        private int ngs_cfg0 = 0x30;
        private int gspage;
        private int ngs_mode_pg1;
        private const int gs_ram_size = 2048;
        private const int gs_ram_mask = (gs_ram_size - 1) >> 4;

        private readonly byte[] gsvol = new byte[4];

        private const int MPAG = 0x00;
        private const int MPAGEX = 0x10;
        private const int ZXCMD = 0x01;
        private const int ZXDATRD = 0x02;
        private const int ZXDATWR = 0x03;
        private const int ZXSTAT = 0x04;
        private const int CLRCBIT = 0x05;
        private const int VOL1 = 0x06;
        private const int VOL2 = 0x07;
        private const int VOL3 = 0x08;
        private const int VOL4 = 0x09;
        private const int DAMNPORT1 = 0x0A;
        private const int DAMNPORT2 = 0x0B;
        private const int GSCFG0 = 0x0F;
        private const int M_NOROM = 1;
        private const int M_RAMRO = 2;
        private const int M_EXPAG = 8;

        public GeneralSoundDevice()
        {
            Category = BusDeviceCategory.Music;
            Name = "GENERAL SOUND";
            Description =
                "General Sound (draft)" + Environment.NewLine +
                "IO + onboard Z80 restored; DAC/audio mix is still incomplete.";
        }

        public override void BusInit(IBusManager bmgr)
        {
            base.BusInit(bmgr);
            bmgr.Events.SubscribeWrIo(0x00FF, 0x00BB, WriteBB); // GSCOM
            bmgr.Events.SubscribeRdIo(0x00FF, 0x00BB, ReadBB);  // GSSTAT
            bmgr.Events.SubscribeWrIo(0x00FF, 0x00B3, WriteB3); // GSDAT
            bmgr.Events.SubscribeRdIo(0x00FF, 0x00B3, ReadB3);
            bmgr.Events.SubscribeWrIo(0x00FF, 0x0033, Write33); // GSCTR
            bmgr.Events.SubscribeEndFrame(OnGsFrame);
        }

        public override void BusConnect()
        {
            base.BusConnect();

            m_rom = new byte[32][];
            m_ram = new byte[256][];
            for (var i = 0; i < m_rom.Length; i++)
                m_rom[i] = new byte[PAGE];
            for (var i = 0; i < m_ram.Length; i++)
                m_ram[i] = new byte[PAGE];

            LoadBootRom();

            m_thread = new Thread(ThreadProc)
            {
                Name = "GeneralSoundDevice Thread",
                IsBackground = true,
            };
            m_thread.Start();
            while (!m_running)
                Thread.Sleep(1);
        }

        public override void BusDisconnect()
        {
            m_running = false;
            m_event.Set();
            if (m_thread != null)
            {
                m_thread.Join();
                m_thread = null;
            }
            base.BusDisconnect();
        }

        private void LoadBootRom()
        {
            try
            {
                using (var stream = RomPack.GetImageStream("DEVICES/bootgs.rom"))
                {
                    for (var i = 0; i < m_rom.Length; i++)
                    {
                        var read = stream.Read(m_rom[i], 0, m_rom[i].Length);
                        if (read != m_rom[i].Length)
                            throw new EndOfStreamException(
                                "DEVICES/bootgs.rom is shorter than 512 KiB.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private void WriteBB(ushort addr, byte value, ref bool handled)
        {
            handled = true;
            gscmd = value;
            gsstat |= 0x01;
        }

        private void ReadBB(ushort addr, ref byte value, ref bool handled)
        {
            handled = true;
            value = (byte)(gsstat | 0x7E);
        }

        private void WriteB3(ushort addr, byte value, ref bool handled)
        {
            handled = true;
            gsdata_out = value;
            gsstat |= 0x80;
        }

        private void ReadB3(ushort addr, ref byte value, ref bool handled)
        {
            handled = true;
            gsstat &= 0x7F;
            value = gsdata_in;
        }

        private void Write33(ushort addr, byte value, ref bool handled)
        {
            handled = true;
            m_cpu.RST = (value & C_GRST) != 0;
            m_cpu.NMI = (value & C_GNMI) != 0;
            if (m_cpu.RST || m_cpu.NMI)
                m_event.Set();
        }

        private void OnGsFrame()
        {
            m_event.Set();
        }

        private void ThreadProc()
        {
            m_running = true;
            Action nop = () => { };
            Action<ushort> nopAddr = _ => { };

            m_cpu.RDMEM_M1 = RDMEM;
            m_cpu.RDMEM = RDMEM;
            m_cpu.WRMEM = WRMEM;
            m_cpu.RDPORT = RDPORT;
            m_cpu.WRPORT = WRPORT;
            m_cpu.RESET = nop;
            m_cpu.INTACK_M1 = nop;
            m_cpu.NMIACK_M1 = nop;
            m_cpu.RDNOMREQ = nopAddr;
            m_cpu.WRNOMREQ = nopAddr;

            m_cpu.RST = true;
            m_cpu.ExecCycle();
            m_cpu.RST = false;
            var next = m_cpu.Tact;
            InitGsbank();

            while (m_running)
            {
                next += FRAME_LEN;
                while (m_cpu.Tact < next)
                {
                    var intTact = (int)(m_cpu.Tact % (FRAME_LEN * 50 / 44100));
                    m_cpu.INT = intTact < 32;
                    m_cpu.ExecCycle();
                }
                m_event.WaitOne();
            }
        }

        private void InitGsbank()
        {
            gsbankr = new byte[4][] { m_rom[0], m_ram[3], m_rom[0], m_rom[1] };
            gsbankw = new byte[4][] { m_trash, m_ram[3], m_trash, m_trash };
        }

        private void UpdateMemMapping()
        {
            var ramRo = (ngs_cfg0 & M_RAMRO) != 0;
            var noRom = (ngs_cfg0 & M_NOROM) != 0;
            if (noRom)
            {
                gsbankr[0] = gsbankw[0] = m_ram[0];
                gsbankr[1] = gsbankw[1] = m_ram[3];
                gsbankr[2] = gsbankw[2] = m_ram[gspage];
                gsbankr[3] = gsbankw[3] = m_ram[ngs_mode_pg1];

                if (ramRo)
                {
                    if (gspage == 0 || gspage == 1)
                        gsbankw[2] = m_trash;
                    if (ngs_mode_pg1 == 0 || ngs_mode_pg1 == 1)
                        gsbankw[3] = m_trash;
                }
            }
            else
            {
                gsbankw[0] = gsbankw[2] = gsbankw[3] = m_trash;
                gsbankr[0] = m_rom[0];
                gsbankr[1] = gsbankw[1] = m_ram[3];
                gsbankr[2] = m_rom[gspage & 0x1F];
                gsbankr[3] = m_rom[ngs_mode_pg1 & 0x1F];
            }
        }

        private byte RDMEM(ushort addr)
        {
            return gsbankr[(addr >> 14) & 3][addr & (PAGE - 1)];
        }

        private void WRMEM(ushort addr, byte value)
        {
            gsbankw[(addr >> 14) & 3][addr & (PAGE - 1)] = value;
        }

        private void WRPORT(ushort addr, byte value)
        {
            switch (addr & 0xFF)
            {
                case MPAG:
                    var extMem = (ngs_cfg0 & M_EXPAG) != 0;
                    gspage = Rol8(value, 1) & gs_ram_mask & (extMem ? 0xFF : 0xFE);
                    if (!extMem)
                        ngs_mode_pg1 = (Rol8(value, 1) & gs_ram_mask) | 1;
                    UpdateMemMapping();
                    break;

                case ZXDATRD:
                    gsstat &= 0x7F;
                    break;
                case ZXDATWR:
                    gsstat |= 0x80;
                    gsdata_in = value;
                    break;
                case CLRCBIT:
                    gsstat &= 0xFE;
                    break;
                case VOL1:
                case VOL2:
                case VOL3:
                case VOL4:
                    var chan = (addr & 0x0F) - 6;
                    gsvol[chan] = (byte)(value & 0x3F);
                    break;
                case DAMNPORT1:
                    gsstat = (byte)((gsstat & 0x7F) | (gspage << 7));
                    break;
                case DAMNPORT2:
                    gsstat = (byte)((gsstat & 0xFE) | ((gsvol[0] >> 5) & 1));
                    break;
                case GSCFG0:
                    ngs_cfg0 = value & 0x3F;
                    UpdateMemMapping();
                    break;
                case MPAGEX:
                    ngs_mode_pg1 = Rol8(value, 1) & gs_ram_mask;
                    UpdateMemMapping();
                    break;
                default:
                    Logger.Debug("SKIP GS WRIO #{0:X2},#{1:X2}", addr, value);
                    break;
            }
        }

        private byte RDPORT(ushort addr)
        {
            byte value = 0xFF;
            switch (addr & 0xFF)
            {
                case ZXCMD:
                    value = gscmd;
                    break;
                case ZXDATRD:
                    gsstat &= 0x7F;
                    value = gsdata_out;
                    break;
                case ZXDATWR:
                    gsstat |= 0x80;
                    gsdata_in = 0xFF;
                    value = 0xFF;
                    break;
                case ZXSTAT:
                    value = gsstat;
                    break;
                case CLRCBIT:
                    gsstat &= 0xFE;
                    value = 0xFF;
                    break;
                case DAMNPORT1:
                    gsstat = (byte)((gsstat & 0x7F) | (gspage << 7));
                    value = 0xFF;
                    break;
                case DAMNPORT2:
                    gsstat = (byte)((gsstat & 0xFE) | (gsvol[0] >> 5));
                    value = 0xFF;
                    break;
                case GSCFG0:
                    value = (byte)ngs_cfg0;
                    break;
            }
            return value;
        }

        private static byte Rol8(byte val, int shift)
        {
            var shifted = val << (shift & 7);
            return (byte)((shifted & 0xFF) | (shifted >> 8));
        }
    }
}
