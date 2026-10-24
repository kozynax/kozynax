/// Description: Tape emulator
/// Author: Alex Makeev
/// Date: 16.04.2007
using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Dependency;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Cpu;
using ZXMAK2.Model.Tape.Entities;
using ZXMAK2.Model.Tape.Interfaces;
using ZXMAK2.Serializers.TapeSerializers;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Resources;


namespace ZXMAK2.Hardware.General
{
    public class TapeDevice : SoundDeviceBase, ITapeDevice
    {
        #region Fields

        private readonly IconDescriptor m_iconTape = new IconDescriptor("TAPE", ResourceImages.OsdTapeRd);
        private CpuUnit m_cpu;
        private IMemoryDevice m_memory;
        private bool m_trapsAllowed = true;
        private bool m_autoPlay = true;

        // sound related
        private ushort m_dacValue0 = 0;
        private ushort m_dacValue1 = 0x1FFF;

        private readonly object m_syncBlocks = new object();
        private int m_index = 0;
        private int m_playPosition = 0;
        private readonly Stack<KeyValuePair<int, int>> m_loopStack = new Stack<KeyValuePair<int, int>>();
        private readonly Stack<int> m_sequenceStack = new Stack<int>();

        private bool m_isPlay = false;

        private int m_waitEdge = 0;
        private TapeFlags m_edgeFlags;
        private long m_edgeTick;
        private bool m_state = false;

        private IViewHolder m_viewHolder;

        private bool m_noDos;
        private int m_mask;
        private int m_port;
        private int m_bit;
        private int m_bitMask;


        #endregion Fields

        
        public TapeDevice()
        {
            Category = BusDeviceCategory.Tape;
            Name = "TAPE PLAYER";

            m_noDos = true;
            m_mask = 0x01;
            m_port = 0xFE;
            m_bit = 6;
            m_bitMask = 1 << m_bit;
            //OnProcessConfigChange();

            Blocks = new List<ITapeBlock>();
            Volume = 5;
            CreateViewHolder();
        }


        #region Properties

        public bool NoDos
        {
            get { return m_noDos; }
            set
            {
                m_noDos = value;
                OnConfigChanged();
            }
        }

        public int Mask
        {
            get { return m_mask; }
            set
            {
                m_mask = value;
                OnConfigChanged();
            }
        }

        public int Port
        {
            get { return m_port; }
            set
            {
                m_port = value;
                OnConfigChanged();
            }
        }

        public int Bit
        {
            get { return m_bit; }
            set
            {
                value = value < 0 ? 0 : value;
                value = value > 7 ? 7 : value;
                m_bit = value;
                m_bitMask = 1 << m_bit;
                OnConfigChanged();
            }
        }

        protected override void OnProcessConfigChange()
        {
            base.OnProcessConfigChange();

            // process Volume change...
            m_dacValue0 = ushort.MinValue;
            m_dacValue1 = (ushort)((ushort.MaxValue * Volume) / 100);

            // update description...
            var builder = new StringBuilder();
            builder.Append("Common Tape Device");
            builder.Append(Environment.NewLine);
            builder.Append(Environment.NewLine);
            builder.Append(string.Format("NoDos: {0}", NoDos));
            builder.Append(Environment.NewLine);
            builder.Append(string.Format("Mask:  #{0:X4}", Mask));
            builder.Append(Environment.NewLine);
            builder.Append(string.Format("Port:  #{0:X4}", Port));
            builder.Append(Environment.NewLine);
            builder.Append(string.Format("Bit:   D{0}", Bit));
            builder.Append(Environment.NewLine);
            Description = builder.ToString();
        }

        #endregion Properties


        #region IBusDevice

        public override void BusInit(IBusManager bmgr)
        {
            base.BusInit(bmgr);
            m_cpu = bmgr.CPU;
            m_memory = bmgr.FindDevice<IMemoryDevice>();

            bmgr.Events.SubscribeRdIo(Mask, Port & Mask, ReadPortFe);
            bmgr.Events.SubscribeWrIo(Mask, Port & Mask, WritePortFe);

            bmgr.Events.SubscribePreCycle(busPreCycle);

            bmgr.AddSerializer(new TapSerializer(this));
            bmgr.AddSerializer(new TzxSerializer(this));
            bmgr.AddSerializer(new CswSerializer(this));
            bmgr.AddSerializer(new WavSerializer(this));
            bmgr.RegisterIcon(m_iconTape);
            if (m_viewHolder != null)
            {
                bmgr.AddCommandUi(m_viewHolder.CommandOpen);
            }
        }

        public override void BusDisconnect()
        {
            base.BusDisconnect();
            if (m_viewHolder != null)
            {
                m_viewHolder.Close();
            }
        }

        protected override void OnConfigLoad(XmlNode node)
        {
            base.OnConfigLoad(node);
            UseTraps = Utils.GetXmlAttributeAsBool(node, "useTraps", UseTraps);
            UseAutoPlay = Utils.GetXmlAttributeAsBool(node, "useAutoPlay", UseAutoPlay);
            NoDos = Utils.GetXmlAttributeAsBool(node, "noDos", NoDos);
            Mask = Utils.GetXmlAttributeAsInt32(node, "mask", Mask);
            Port = Utils.GetXmlAttributeAsInt32(node, "port", Port);
            Bit = Utils.GetXmlAttributeAsInt32(node, "bit", Bit);
        }

        protected override void OnConfigSave(XmlNode node)
        {
            base.OnConfigSave(node);
            Utils.SetXmlAttribute(node, "useTraps", UseTraps);
            Utils.SetXmlAttribute(node, "useAutoPlay", UseAutoPlay);
            Utils.SetXmlAttribute(node, "noDos", NoDos);
            Utils.SetXmlAttribute(node, "mask", Mask);
            Utils.SetXmlAttribute(node, "port", Port);
            Utils.SetXmlAttribute(node, "bit", Bit);
        }

        #endregion


        #region ITapeDevice

        public bool UseTraps
        {
            get { return m_trapsAllowed; }
            set { m_trapsAllowed = value; OnConfigChanged(); }
        }

        public bool UseAutoPlay
        {
            get { return m_autoPlay; }
            set { m_autoPlay = value; OnConfigChanged(); detectorReset(); }
        }

        #endregion


        #region Bus Handlers

        private void WritePortFe(ushort addr, byte value, ref bool handled)
        {
            if (handled || (m_noDos && m_memory.DOSEN))
                return;
            //handled = true;

            if (!m_isPlay)
            {
                // http://www.worldofspectrum.org/faq/reference/48kreference.htm
                // issue 2: Ear
                // issue 3: Ear | Mic
                m_state = (value & 0x10) != 0;
            }
        }

        private void ReadPortFe(ushort addr, ref byte value, ref bool handled)
        {
            if (handled || (m_noDos && m_memory.DOSEN))
                return;
            //handled = true;

            if (m_isPlay)
            {
                tape_bit(m_cpu.Tact);
            }
            if (m_state)
            {
                value |= (byte)m_bitMask;
            }
            else
            {
                value &= (byte)~m_bitMask;
            }
            detectorRead();
        }

        private void busPreCycle()
        {
            if (!m_isPlay)
                return;
            //bmgr.SubscribeRDMEM_M1(0xFFFF, 0x056B, tapeTrap);
            //bmgr.SubscribeRDMEM_M1(0xFFFF, 0x059E, tapeTrap);

            var val = tape_bit(m_cpu.Tact) ? m_dacValue1 : m_dacValue0;
            UpdateDac(val, val);

            ushort addr = m_cpu.regs.PC;
            if (!UseTraps || !m_memory.IsRom48 ||
                !(addr == 0x056B || addr == 0x059E) ||
                (m_cpu.regs._AF & CpuFlags.C) == 0) // verify?
            {
                return;
            }

            var current = CurrentBlock;
            if (current < 0 || current >= Blocks.Count)
                return;
            var tb = Blocks[current];
            var tapData = tb.GetData();
            if (tapData == null || tapData.Length < 2)
            {
                return;
            }

            var length = tapData.Length;
            var read = length - 1;
            read = read < m_cpu.regs.DE ? read : m_cpu.regs.DE;
            if (read <= 0)
            {
                return;
            }
            var parity = tapData[0];
            if (parity != m_cpu.regs._AF >> 8)
            {
                return;
            }
            m_cpu.regs._AF = 0x0145;

            /* Loading or verifying determined by the carry flag of F' */
            for (var i = 0; i < read; i++)
            {
                var value = tapData[i + 1];
                m_cpu.regs.L = value;
                parity ^= value;
                m_cpu.WRMEM((ushort)(m_cpu.regs.IX + i), value);
            }
            var pc = (ushort)0x05DF;
            /* If |DE| bytes have been read and there's more data, do the parity check */
            if (m_cpu.regs.DE == read && read + 1 < length)
            {
                var value = tapData[read + 1];
                m_cpu.regs.L = value;
                parity ^= value;
                m_cpu.regs.B = 0xB0;
            }
            else
            {
                /* Failure to read first bit of the next byte (ref. 48K ROM, 0x5EC) */
                m_cpu.regs.L = 1;
                m_cpu.regs.A = parity;
                m_cpu.regs.F = 0x50;
                m_cpu.regs.B = 0;
                pc = 0x05EE;
            }

            m_cpu.regs.H = parity;
            m_cpu.regs.DE -= (ushort)read;
            m_cpu.regs.IX += (ushort)read;
            m_cpu.regs.C = 0x01;

            m_cpu.regs.PC = pc;//0x05DF;//0x05DF - 1;

            int newBlock = CurrentBlock + 1;
            if (newBlock < Blocks.Count)
            {
                CurrentBlock = newBlock;
            }
            else
            {
                Stop();
                Rewind();
            }
        }

        protected override void OnBeginFrame()
        {
            base.OnBeginFrame();
            m_iconTape.Visible = false;
        }

        protected override void OnEndFrame()
        {
            //ushort val = tape_bit(m_cpu.Tact) ? m_dacValue1 : m_dacValue0;
            //UpdateDAC(val, val);
            base.OnEndFrame();
            detectorFrame();
        }

        #endregion


        #region Events

        public event EventHandler TapeStateChanged;

        protected virtual void OnTapeStateChanged()
        {
            if (TapeStateChanged != null)
                TapeStateChanged(this, EventArgs.Empty);
        }

        #endregion


        #region Properties

        public List<ITapeBlock> Blocks { get; private set; }

        public int CurrentBlock
        {
            get
            {
                lock (m_syncBlocks)
                {
                    if (Blocks.Count == 0 || m_index < 0 || m_index >= Blocks.Count)
                        return -1;
                    return m_index;
                }
            }
            set
            {
                lock (m_syncBlocks)
                {
                    if (value == m_index)
                        return;
                    if (value < 0 || value >= Blocks.Count)
                        return;
                    m_index = value;
                    m_playPosition = 0;
                    m_edgeTick = m_cpu != null ? m_cpu.Tact : 0;
                    m_edgeFlags = (TapeFlags)0;
                }
                OnTapeStateChanged();
            }
        }

        public int Position
        {
            get
            {
                lock (m_syncBlocks)
                {
                    if (m_index < 0 || m_index >= Blocks.Count ||
                        m_playPosition < 0 || m_playPosition >= Blocks[m_index].Count)
                        return 0;
                    return m_playPosition;
                }
            }
        }

        public bool IsPlay
        {
            get { return m_isPlay; }
        }

        public int TactsPerSecond
        {
            get { return FrameTactCount * 50; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called on load new image
        /// </summary>
        public void Reset()
        {
            Stop();
            lock (m_syncBlocks)
            {
                m_loopStack.Clear();
                m_sequenceStack.Clear();
                m_index = 0;
                m_playPosition = 0;
                m_edgeTick = m_cpu != null ? m_cpu.Tact : 0;
                m_state = true;
            }
            OnTapeStateChanged();
        }

        public void Rewind()
        {
            Reset();
        }

        public void Play()
        {
            lock (m_syncBlocks)
            {
                if (!m_isPlay)
                {
                    m_isPlay = true;
                    if (m_index == 0)
                        m_state = true;
                    m_playPosition = 0;
                    m_edgeTick = m_cpu != null ? m_cpu.Tact : 0;
                    m_edgeFlags = (TapeFlags)0;
                }
            }
            if (m_cpu != null)
                tape_bit(m_cpu.Tact);
            OnTapeStateChanged();
        }

        public void Stop()
        {
            lock (m_syncBlocks)
            {
                if (m_isPlay)
                {
                    m_isPlay = false;
                    m_playPosition = 0;
                    if (m_index < 0 && Blocks.Count > 0)
                        m_index = 0;
                }
            }
            OnTapeStateChanged();
        }

        #endregion

        #region private methods

        private int PauseTacts(int pauseMs)
        {
            var tps = TactsPerSecond;
            if (tps <= 0)
                tps = 3500000;
            return pauseMs * (tps / 1000);
        }

        private bool IsInvalidBlock(int index)
        {
            return index == m_index || index < 0 || index >= Blocks.Count;
        }

        private void get_next_edge(out int tstates, out TapeFlags flags)
        {
            var notify = false;
            lock (m_syncBlocks)
            {
                var advanceBlock = false;
                var jumped = false;
                flags = (TapeFlags)0;
                ITapeBlock block = null;
                if (Blocks.Count > 0 && m_index >= 0 && m_index < Blocks.Count)
                    block = Blocks[m_index];

                if (block == null)
                {
                    tstates = 0;
                    advanceBlock = true;
                }
                else
                {
                    switch (block.Type)
                    {
                        case TapeType.ROM:
                        case TapeType.TURBO:
                        case TapeType.PURE_DATA:
                        case TapeType.RAW_DATA:
                        case TapeType.CSW_RECORDING:
                        case TapeType.GENERALIZED_DATA:
                        {
                            var i = m_playPosition++;
                            if (i >= block.Count)
                            {
                                advanceBlock = true;
                                tstates = PauseTacts(block.Pause);
                                if (tstates == 0)
                                    flags |= TapeFlags.NO_EDGE;
                            }
                            else
                            {
                                block.GetEdge(i, out tstates, out flags);
                            }
                            break;
                        }
                        case TapeType.PURE_TONE:
                        case TapeType.PULSES:
                        {
                            var i = m_playPosition++;
                            if (i >= block.Count)
                            {
                                advanceBlock = true;
                                tstates = PauseTacts(block.Pause);
                                if (tstates == 0)
                                    flags |= TapeFlags.NO_EDGE;
                            }
                            else
                            {
                                block.GetEdge(i, out tstates, out flags);
                                if (i + 1 >= block.Count)
                                    advanceBlock = true;
                            }
                            break;
                        }
                        case TapeType.PAUSE:
                            tstates = PauseTacts(block.Pause);
                            advanceBlock = true;
                            if (tstates == 0)
                                flags |= TapeFlags.STOP;
                            break;
                        case TapeType.JUMP:
                        {
                            var dest = m_index + block.Tag;
                            if (IsInvalidBlock(dest))
                            {
                                Logger.Warn("TZX: #23 Jump to invalid block {0} of {1}", dest, Blocks.Count);
                                jumped = true;
                                tstates = 0;
                                advanceBlock = true;
                                flags |= TapeFlags.ERROR | TapeFlags.STOP | TapeFlags.NO_EDGE;
                            }
                            else
                            {
                                m_index = dest;
                                jumped = true;
                                tstates = 0;
                                advanceBlock = true;
                                flags |= TapeFlags.NO_EDGE;
                            }
                            break;
                        }
                        case TapeType.CALL_SEQUENCE:
                        {
                            var offsets = block.TagArray;
                            var invalid = -1;
                            if (offsets == null || offsets.Length == 0)
                                invalid = m_index;
                            else
                            {
                                for (var n = 0; n < offsets.Length; n++)
                                {
                                    var dest = m_index + offsets[n];
                                    if (IsInvalidBlock(dest))
                                    {
                                        invalid = dest;
                                        break;
                                    }
                                }
                            }
                            if (invalid != -1)
                            {
                                Logger.Warn("TZX: #26 Call sequence to invalid block {0} of {1}", invalid, Blocks.Count);
                                jumped = true;
                                tstates = 0;
                                advanceBlock = true;
                                flags |= TapeFlags.ERROR | TapeFlags.STOP | TapeFlags.NO_EDGE;
                                break;
                            }
                            m_sequenceStack.Push(m_index + 1);
                            for (var n = offsets.Length - 1; n >= 0; n--)
                                m_sequenceStack.Push(m_index + offsets[n]);
                            m_index = m_sequenceStack.Pop();
                            jumped = true;
                            tstates = 0;
                            advanceBlock = true;
                            flags |= TapeFlags.NO_EDGE;
                            break;
                        }
                        case TapeType.RET_FROM_SEQUENCE:
                            if (m_sequenceStack.Count == 0)
                            {
                                Logger.Warn("TZX: unexpected #27 Return from sequence");
                                jumped = true;
                                tstates = 0;
                                advanceBlock = true;
                                flags |= TapeFlags.ERROR | TapeFlags.STOP | TapeFlags.NO_EDGE;
                            }
                            else
                            {
                                m_index = m_sequenceStack.Pop();
                                jumped = true;
                                tstates = 0;
                                advanceBlock = true;
                                flags |= TapeFlags.NO_EDGE;
                            }
                            break;
                        case TapeType.LOOP_START:
                            m_loopStack.Push(new KeyValuePair<int, int>(m_index + 1, block.Tag));
                            tstates = 0;
                            advanceBlock = true;
                            flags |= TapeFlags.NO_EDGE;
                            break;
                        case TapeType.LOOP_END:
                        {
                            if (m_loopStack.Count == 0)
                            {
                                Logger.Warn("TZX: unexpected #25 End of loop");
                                jumped = true;
                                tstates = 0;
                                advanceBlock = true;
                                flags |= TapeFlags.ERROR | TapeFlags.STOP | TapeFlags.NO_EDGE;
                                break;
                            }
                            var frame = m_loopStack.Pop();
                            var start = frame.Key;
                            var remain = frame.Value;
                            if (--remain > 0)
                            {
                                m_loopStack.Push(new KeyValuePair<int, int>(start, remain));
                                m_index = start;
                                jumped = true;
                            }
                            tstates = 0;
                            advanceBlock = true;
                            flags |= TapeFlags.NO_EDGE;
                            break;
                        }
                        case TapeType.STOP48:
                            tstates = 0;
                            advanceBlock = true;
                            flags |= TapeFlags.STOP48 | TapeFlags.NO_EDGE;
                            break;
                        case TapeType.SET_SIGNAL_LEVEL:
                            tstates = 0;
                            advanceBlock = true;
                            // Match unpublished player: Tag!=0 → LOW, Tag==0 → HIGH.
                            flags |= block.Tag != 0 ? TapeFlags.LEVEL_LOW : TapeFlags.LEVEL_HIGH;
                            break;
                        case TapeType.GROUP_START:
                        case TapeType.GROUP_END:
                        case TapeType.SELECT:
                        case TapeType.COMMENT:
                        case TapeType.MESSAGE:
                        case TapeType.ARCHIVE_INFO:
                        case TapeType.HARDWARE:
                        case TapeType.CUSTOM:
                            tstates = 0;
                            advanceBlock = true;
                            flags |= TapeFlags.NO_EDGE;
                            break;
                        case TapeType.RLE_PULSE:
                        case TapeType.PULSE_SEQUENCE:
                        case TapeType.DATA_BLOCK:
                        {
                            block.GetEdge(m_playPosition, out tstates, out flags);
                            if (++m_playPosition >= block.Count)
                                advanceBlock = true;
                            break;
                        }
                        default:
                            Logger.Warn("get_next_edge: switch case #{0:x2} ({1}) not implemented", (int)block.Type, block.Type);
                            tstates = 0;
                            advanceBlock = true;
                            break;
                    }
                }

                if (advanceBlock)
                {
                    flags |= TapeFlags.BLOCK;
                    if (!jumped)
                    {
                        m_index++;
                        if (m_index < 0 || m_index >= Blocks.Count)
                        {
                            flags |= TapeFlags.STOP | TapeFlags.TAPE;
                            flags &= ~TapeFlags.NO_EDGE;
                            m_loopStack.Clear();
                            m_sequenceStack.Clear();
                            // Stay on the last block so UI/tests can see completion
                            // before Stop() (rewinding to 0 hid empty trailing blocks).
                            m_index = Blocks.Count > 0 ? Blocks.Count - 1 : 0;
                            m_playPosition = 0;
                        }
                    }
                    m_playPosition = 0;
                    notify = true;
                }
            }
            if (notify)
                OnTapeStateChanged();
        }

        private bool tape_bit(long globalTact)
        {
            if (!m_isPlay)
                return m_state;

            while (m_isPlay && globalTact >= m_edgeTick)
            {
                if ((m_edgeFlags & TapeFlags.STOP) != 0)
                {
                    Stop();
                    return m_state;
                }
                if ((m_edgeFlags & TapeFlags.STOP48) != 0 && m_memory != null && m_memory.IsRom48)
                {
                    Stop();
                    return m_state;
                }

                get_next_edge(out m_waitEdge, out m_edgeFlags);
                m_edgeTick += m_waitEdge;

                var apply =
                    (m_waitEdge != 0 ||
                     (m_edgeFlags & TapeFlags.NO_EDGE) == 0 ||
                     (m_edgeFlags & (TapeFlags.STOP | TapeFlags.LEVEL_LOW | TapeFlags.LEVEL_HIGH)) != 0)
                    && (m_edgeFlags & TapeFlags.NO_EDGE) == 0;
                if (apply)
                {
                    if ((m_edgeFlags & TapeFlags.LEVEL_LOW) != 0)
                        m_state = false;
                    else if ((m_edgeFlags & TapeFlags.LEVEL_HIGH) != 0)
                        m_state = true;
                    else
                        m_state = !m_state;
                }

                if (UseAutoPlay && (m_edgeFlags & TapeFlags.ERROR) != 0)
                    UseAutoPlay = false;
            }
            return m_state;
        }

        #endregion


        #region AutoPlay

        private long m_lastInTact = 0;
        private int m_detectCounter;
        private int m_detectTimeOut;
        private ushort m_lastPC;
        private byte[] m_lastRegs;

        private void detectorReset()
        {
            m_lastInTact = 0;
            m_detectCounter = 0;
            m_lastPC = 0;
            m_lastRegs = null;
        }

        private void detectorRead()
        {
            long cpuTact = m_cpu.Tact;
            int delta = (int)(cpuTact - m_lastInTact);
            m_lastInTact = cpuTact;

            byte[] newRegs = new byte[] {
				m_cpu.regs.A,
				m_cpu.regs.B, m_cpu.regs.C,
				m_cpu.regs.D, m_cpu.regs.E,
				m_cpu.regs.H, m_cpu.regs.L,
			};
            if (delta > 0 && delta < 96 && m_cpu.regs.PC == m_lastPC && m_lastRegs != null)
            {
                int diffCount = 0;
                int diffValue = 0;
                for (int i = 0; i < newRegs.Length; i++)
                {
                    if (m_lastRegs[i] != newRegs[i])
                    {
                        diffValue = m_lastRegs[i] - newRegs[i];
                        diffCount++;
                    }
                }
                if (diffCount == 1 && (diffValue == 1 || diffValue == -1))
                {
                    m_iconTape.Visible = true;
                    m_detectCounter++;
                    if (m_detectCounter >= 8 && m_autoPlay)
                    {
                        if (!m_isPlay)
                            Play();
                        m_detectTimeOut = 50;
                    }
                }
                else
                {
                    m_detectCounter = 0;
                }
            }
            m_lastRegs = newRegs;
            m_lastPC = m_cpu.regs.PC;
        }

        private void detectorFrame()
        {
            if (m_isPlay && m_autoPlay && m_detectTimeOut >= 0)
                m_detectTimeOut--;
            // Autoplay only starts playback. Pause-0 / end-of-tape still stop
            // via get_next_edge; EAR-idle stop breaks turbo loaders that pause
            // polling between ROM-load and custom data.
        }

        #endregion


        #region IGuiExtension Members

        private void CreateViewHolder()
        {
            try
            {
                m_viewHolder = new ViewHolder<ITapeView>("Tape", t => t.Init(new TapeSettings(this)));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        #endregion
    }
}
