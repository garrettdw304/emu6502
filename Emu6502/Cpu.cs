namespace Emu6502
{
    public partial class Cpu
    {
        private const byte N_MASK = 0b1000_0000;
        private const byte V_MASK = 0b0100_0000;
        private const byte B_MASK = 0b0001_0000;
        private const byte D_MASK = 0b0000_1000;
        private const byte I_MASK = 0b0000_0100;
        private const byte Z_MASK = 0b0000_0010;
        private const byte C_MASK = 0b0000_0001;

        /// <summary>
        /// Fetch the opcode for the next instruction during the next cycle.
        /// </summary>
        private const int NEXT_INSTR_STEP = -1;
        /// <summary>
        /// We are waiting for an interrupt to occur because of WAI.
        /// </summary>
        private const int WAI_STEP = -2;
        /// <summary>
        /// We are waiting for reset to occur because of STP.
        /// </summary>
        private const int STP_STEP = -3;
        /// <summary>
        /// An instruction that fetches the opcode of the next instruction to be executed during its last cycle (happens
        /// for instructions that dont perform a read or write on their last cycle) will set step to this to indicate
        /// that the fetch cycle of the next instruction is to be skipped and that it has already loaded opcode with the
        /// opcode byte of the next instruction to be executed.
        /// </summary>
        private const int PIPELINED_FETCH_STEP = -4;
        private const int NMI_OPCODE = 256;
        private const int RST_OPCODE = 257;
        private const int IRQ_OPCODE = 258;

        public readonly InterruptLine nmi;
        public readonly InterruptLine irq;
        public readonly InterruptLine rst;

        public readonly BusController bc;

        public byte a;
        public byte x;
        public byte y;
        /// <summary>
        /// Processor status (flags) register.
        /// </summary>
        public byte p;
        /// <summary>
        /// Program counter.
        /// </summary>
        public ushort pc;
        /// <summary>
        /// Stack pointer. Push -> post decrement. Pop -> pre increment.
        /// </summary>
        public byte s;

        /// <summary>
        /// The opcode of the instruction currently being executed.
        /// See constants postfixed with _OPCODE.
        /// </summary>
        private int opcode;
        /// <summary>
        /// The step that we are at in processing the current instruction.
        /// <para/>See:
        /// <br/><see cref="NEXT_INSTR_STEP"/>
        /// <br/><see cref="WAI_STEP"/>
        /// <br/><see cref="STP_STEP"/>
        /// </summary>
        private int step;
        /// <summary>
        /// Used to store the zero page address byte that will be used in indirect addressing modes.
        /// </summary>
        private byte indAddr;
        /// <summary>
        /// Used to store absolute addresses or addresses loaded during indirect addressing modes.
        /// </summary>
        private ushort effectiveAddress;
        /// <summary>
        /// Temporary ALU input for operations on values that are not stored in the A register.
        /// </summary>
        private byte aluTmp;

        private readonly Instruction[] instructions;

        /// <summary>
        /// 0x100 OR s
        /// </summary>
        public ushort ExpandedS => (ushort)(0x100 | s);
        public byte PcL
        {
            set => pc = (ushort)((pc & 0xFF00) | value);
            get => (byte)pc;
        }
        public byte PcH
        {
            set => pc = (ushort)((pc & 0x00FF) | (value << 8));
            get => (byte)(pc >> 8);
        }
        public byte EffectiveAddressL
        {
            set => effectiveAddress = (ushort)((effectiveAddress & 0xFF00) | value);
            get => (byte)effectiveAddress;
        }
        public byte EffectiveAddressH
        {
            set => effectiveAddress = (ushort)((effectiveAddress & 0x00FF) | (value << 8));
            get => (byte)(effectiveAddress >> 8);
        }

        public bool I
        {
            get
            {
                return (p & I_MASK) != 0;
            }
            set
            {
                if (value)
                    p |= I_MASK;
                else
                    p &= unchecked((byte)~I_MASK);
            }
        }
        public bool N
        {
            get
            {
                return (p & N_MASK) != 0;
            }
            set
            {
                if (value)
                    p |= N_MASK;
                else
                    p &= unchecked((byte)~N_MASK);
            }
        }
        public bool Z
        {
            get
            {
                return (p & Z_MASK) != 0;
            }
            set
            {
                if (value)
                    p |= Z_MASK;
                else
                    p &= unchecked((byte)~Z_MASK);
            }
        }
        public bool C
        {
            get
            {
                return (p & C_MASK) != 0;
            }
            set
            {
                if (value)
                    p |= C_MASK;
                else
                    p &= unchecked((byte)~C_MASK);
            }
        }
        public bool V
        {
            get
            {
                return (p & V_MASK) != 0;
            }
            set
            {
                if (value)
                    p |= V_MASK;
                else
                    p &= unchecked((byte)~V_MASK);
            }
        }
        public bool D
        {
            get
            {
                return (p & D_MASK) != 0;
            }
            set
            {
                if (value)
                    p |= D_MASK;
                else
                    p &= unchecked((byte)~D_MASK);
            }
        }

        public Cpu()
        {
            nmi = new InterruptLine(true);
            irq = new InterruptLine(false);
            rst = new InterruptLine(false);
            bc = new BusController();

            a = 0;
            x = 0;
            y = 0;
            s = 0;
            p = 0;
            pc = 0;

            opcode = RST_OPCODE; // TODO: What to init to?
            step = 0; // TODO: What to init to?
            indAddr = 0;
            effectiveAddress = 0;
            aluTmp = 0;

            instructions = new Instruction[259]
            {
                BRK_IMPL_00, ORA_XIND_01, None, None, TSB_ZPG_04, ORA_ZPG_05, ASL_ZPG_06, RMB0_ZPG_07,
                PHP_IMPL_08, ORA_IMM_09, ASL_A_0A, None, TSB_ABS_0C, ORA_ABS_0D, ASL_ABS_0E, BBR0_REL_0F,
                BPL_REL_10, ORA_INDY_11, ORA_ZPGIND_12, None, TRB_ZPG_14, ORA_ZPGX_15, ASL_ZPGX_16, RMB1_ZPG_17,
                CLC_IMPL_18, ORA_ABSY_19, INC_A_1A, None, TRB_ABS_1C, ORA_ABSX_1D, ASL_ABSX_1E, BBR1_REL_1F,
                JSR_ABS_20, AND_XIND_21, None, None, BIT_ZPG_24, AND_ZPG_25, ROL_ZPG_26, RMB2_ZPG_27,
                PLP_IMPL_28, AND_IMM_29, ROL_A_2A, None, BIT_ABS_2C, AND_ABS_2D, ROL_ABS_2E, BBR2_REL_2F,
                BMI_REL_30, AND_INDY_31, AND_ZPGIND_32, None, BIT_ZPGX_34, AND_ZPGX_35, ROL_ZPGX_36, RMB3_ZPG_37,
                SEC_IMPL_38, AND_ABSY_39, DEC_A_3A, None, BIT_ABSX_3C, AND_ABSX_3D, ROL_ABSX_3E, BBR3_REL_3F,
                RTI_IMPL_40, EOR_XIND_41, None, None, None, None, LSR_ZPG_46, RMB4_ZPG_47,
                PHA_IMPL_48, EOR_IMM_49, LSR_A_4A, None, JMP_ABS_4C, EOR_ABS_4D, LSR_ABS_4E, BBR4_REL_4F,
                BVC_REL_50, EOR_INDY_51, EOR_ZPGIND_52, None, None, EOR_ZPGX_55, LSR_ZPGX_56, RMB5_ZPG_57,
                CLI_IMPL_58, EOR_ABSY_59, PHY_IMPL_5A, None, None, EOR_ABSX_5D, LSR_ABSX_5E, BBR5_REL_5F,
                RTS_IMPL_60, ADC_XIND_61, None, None, STZ_ZPG_64, ADC_ZPG_65, ROR_ZPG_66, RMB6_ZPG_67,
                PLA_IMPL_68, ADC_IMM_69, ROR_A_6A, None, JMP_IND_6C, ADC_ABS_6D, ROR_ABS_6E, BBR6_REL_6F,
                BVS_REL_70, ADC_INDY_71, ADC_ZPGIND_72, None, STZ_ZPGX_74, ADC_ZPGX_75, ROR_ZPGX_76, RMB7_ZPG_77,
                SEI_IMPL_78, ADC_ABSY_79, PLY_IMPL_7A, None, JMP_ABSXIND_7C, ADC_ABSX_7D, ROR_ABSX_7E, BBR7_REL_7F,
                BRA_REL_80, STA_XIND_81, None, None, STY_ZPG_84, STA_ZPG_85, STX_ZPG_86, SMB0_ZPG_87,
                DEY_IMPL_88, BIT_IMM_89, TXA_IMPL_8A, None, STY_ABS_8C, STA_ABS_8D, STX_ABS_8E, BBS0_REL_8F,
                BCC_REL_90, STA_INDY_91, STA_ZPGIND_92, None, STY_ZPGX_94, STA_ZPGX_95, STX_ZPGY_96, SMB1_ZPG_97,
                TYA_IMPL_98, STA_ABSY_99, TXS_IMPL_9A, None, STZ_ABS_9C, STA_ABSX_9D, STZ_ABSX_9E, BBS1_REL_9F,
                LDY_IMM_A0, LDA_XIND_A1, LDX_IMM_A2, None, LDY_ZPG_A4, LDA_ZPG_A5, LDX_ZPG_A6, SMB2_ZPG_A7,
                TAY_IMPL_A8, LDA_IMM_A9, TAX_IMPL_AA, None, LDY_ABS_AC, LDA_ABS_AD, LDX_ABS_AE, BBS2_REL_AF,
                BCS_REL_B0, LDA_INDY_B1, LDA_ZPGIND_B2, None, LDY_ZPGX_B4, LDA_ZPGX_B5, LDX_ZPGY_B6, None,
                CLV_IMPL_B8, LDA_ABSY_B9, TSX_IMPL_BA, None, LDY_ABSX_BC, LDA_ABSX_BD, LDX_ABSY_BE, BBS3_REL_BF,
                CPY_IMM_C0, CMP_XIND_C1, None, None, CPY_ZPG_C4, CMP_ZPG_C5, DEC_ZPG_C6, SMB4_ZPG_C7,
                INY_IMPL_C8, CMP_IMM_C9, DEX_IMPL_CA, WAI_IMPL_CB, CPY_ABS_CC, CMP_ABS_CD, DEC_ABS_CE, BBS4_REL_CF,
                BNE_REL_D0, CMP_INDY_D1, CMP_ZPGIND_D2, None, None, CMP_ZPGX_D5, DEC_ZPGX_D6, SMB5_ZPG_D7,
                CLD_IMPL_D8, CMP_ABSY_D9, PHX_IMPL_DA, STP_IMPL_DB, None, CMP_ABSX_DD, DEC_ABSX_DE, BBS5_REL_DF,
                CPX_IMM_E0, SBC_XIND_E1, None, None, CPX_ZPG_E4, SBC_ZPG_E5, INC_ZPG_E6, SMB6_ZPG_E7,
                INX_IMPL_E8, SBC_IMM_E9, NOP_IMPL_EA, None, CPX_ABS_EC, SBC_ABS_ED,INC_ABS_EE, BBS6_REL_EF,
                BEQ_REL_F0, SBC_INDY_F1, SBC_ZPGIND_F2, None, None, SBC_ZPGX_F5, INC_ZPGX_F6, SMB7_ZPG_F7,
                SED_IMPL_F8, SBC_ABSY_F9, PLX_IMPL_FA, None, None, SBC_ABSX_FD, INC_ABSX_FE, BBS7_REL_FF,
                NMI, RST, IRQ
            };
        }

        public void Cycle(int hz)
        {
            if (rst.ShouldInterrupt)
            {
                opcode = RST_OPCODE;
                step = 0;
            }

            if (step == STP_STEP)
                return;

            // Don't service interrupts until we finish the current instruction.
            // TODO: Sometimes we should (like inbetween an NMI_OPCODE, IRQ_OPCODE etc)
            else if (step >= 0)
                instructions[opcode]();

            else if (nmi.ShouldInterrupt)
            {
                opcode = NMI_OPCODE;
                step = 0;
                instructions[opcode]();
            }
            else if (irq.ShouldInterrupt && (!I || step == WAI_STEP))
            {
                if (!I)
                {
                    opcode = IRQ_OPCODE;
                    step = 0;
                    instructions[opcode]();
                }
                else if (step == WAI_STEP)
                {
                    opcode = bc.SyncCycle(pc);
                    step = 0;
                    instructions[opcode]();
                }
            }
            else if (step == WAI_STEP)
                return;
            else if (step == NEXT_INSTR_STEP)
            {
                opcode = bc.SyncCycle(pc);
                step = 0;
                instructions[opcode]();
            }
            else if (step == PIPELINED_FETCH_STEP)
            {
                // Opcode was set by the previous instruction.
                step = 1;
                instructions[opcode]();
            }
        }

        public override string ToString()
        {
            return $"""
                A: {a}
                X: {x}
                Y: {y}
                S: {s}
                P: NV-BDIZC
                P: {Convert.ToString(p, 2).PadLeft(8, '0')}
                PC: {pc:X4}
                Step: {StepToString(step)}
                Instr: {instructions[opcode].Method.Name}
                """;
        }
        
        private void None()
        {
            throw new InvalidOperationException();
            // TODO: Should probably be split up into the individiual NOPs for each reserved opcode or into as few as can
            // implement appropriate cycle and pc incrementing behavior.
        }

        #region Instruction Helpers
        private void SetNZ(byte value)
        {
            N = (value & 0b1000_0000) != 0;
            Z = value == 0;
        }

        private void SetV(byte a, byte b, byte result)
        {
            // If both signs are the same...
            if (((a ^ b) & 0b1000_0000) == 0)
                // See if the result sign has changed
                V = (a ^ result & 0b1000_0000) != 0;
            else
                V = false;
        }

        /// <summary>
        /// Adds the value to A and calculates flag bits.
        /// </summary>
        private void ADC(byte value)
        {
            int result = a + value + (C ? 1 : 0);

            C = result > byte.MaxValue;
            SetNZ((byte)result);
            SetV(a, value, (byte)result);

            a = (byte)result;
        }

        /// <summary>
        /// Subtracts the value from A and calculates flag bits.
        /// </summary>
        private void SBC(byte value)
        {
            int result = a - value - (C ? 0 : 1);

            C = result < 0;
            SetNZ((byte)result);
            SetV(a, value, (byte)result);

            a = (byte)result;
        }

        private void CMP(byte reg, byte value)
        {
            int result = reg - value;

            C = result < 0;
            SetNZ((byte)result);
            SetV(reg, value, (byte)result);
        }

        private void BIT(byte value)
        {
            N = (value & N_MASK) != 0;
            V = (value & V_MASK) != 0;
            Z = (a & value) == 0;
        }

        /// <summary>
        /// Shifts the value in <see cref="aluTmp"/> left by 1 and sets N, Z and C flags.
        /// </summary>
        private void ASL()
        {
            int result = aluTmp << 1;
            SetNZ((byte)result);
            C = result > byte.MaxValue;
            aluTmp = (byte)result;
        }

        private void LSR()
        {
            int result = aluTmp >> 1;
            SetNZ((byte)result);
            C = (aluTmp & 1) != 0;
            aluTmp = (byte)result;
        }

        private void ROL()
        {
            uint result = (uint)((aluTmp << 1) | (C ? 1 : 0));
            SetNZ((byte)result);
            C = result > byte.MaxValue;
            aluTmp = (byte)result;
        }

        private void ROR()
        {
            uint result = (uint)((aluTmp >> 1) | (C ? 0b1000_0000 : 0b0000_0000));
            SetNZ((byte)result);
            C = (aluTmp & 1) != 0;
            aluTmp = (byte)result;
        }

        /// <summary>
        /// Resets selected bits. Bits to reset are selected with a 0, bits to leave unchanged are selected with a 1.
        /// </summary>
        private void RMB(byte bitMask)
        {
            aluTmp = (byte)(aluTmp & bitMask);
        }

        /// <summary>
        /// Sets selected bits. Bits to set are selected with a 1, bits to leave unchanged are selected with a 0.
        /// </summary>
        private void SMB(byte bitMask)
        {
            aluTmp = (byte)(aluTmp | bitMask);
        }

        /// <summary>
        /// Performs steps of a read-modify-write instruction taking 3 cycles.
        /// </summary>
        /// <param name="beginningStep">The step number that the read step should happen on.</param>
        /// <param name="modifyAction">The action that will modify the data on the modify step.</param>
        private void RMW(Action modifyAction, int beginningStep)
        {
            if (step == beginningStep)
            {
                aluTmp = bc.ReadCycle(effectiveAddress);

                step++;
            }
            else if (step == beginningStep + 1)
            {
                _ = bc.ReadCycle(effectiveAddress);
                modifyAction();

                step++;
            }
            else if (step == beginningStep + 2)
            {
                bc.WriteCycle(effectiveAddress, aluTmp);

                step = NEXT_INSTR_STEP;
            }
        }

        private void PH(byte value)
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                step++;
            }
            else if (step == 2)
            {
                bc.WriteCycle(ExpandedS, value);
                s--;

                step = NEXT_INSTR_STEP;
            }
        }

        private void PL(ref byte destination)
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                step++;
            }
            else if (step == 2)
            {
                _ = bc.ReadCycle(ExpandedS);
                s++;

                step++;
            }
            else if (step == 3)
            {
                destination = bc.ReadCycle(ExpandedS);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Reads an absolute address and stores it into <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns>true if the step was handled in this function (steps 0-2).</returns>
        private bool ABS()
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                EffectiveAddressL = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }
            else if (step == 2)
            {
                EffectiveAddressH = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }

            return false;
        }

        private void BR(bool taken)
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                effectiveAddress = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 2)
            {
                if (taken)
                {
                    _ = bc.ReadCycle(pc);

                    int result = PcL + (sbyte)effectiveAddress;
                    PcL = (byte)result;

                    if (result > byte.MaxValue)
                        step++;
                    else
                        step = NEXT_INSTR_STEP;
                }
                else
                {
                    opcode = bc.ReadCycle(pc);
                    pc++;

                    step = PIPELINED_FETCH_STEP;
                }
            }
            else if (step == 3)
            {
                _ = bc.ReadCycle(pc);
                PcH++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Branch on bit.
        /// </summary>
        /// <param name="bitMask"></param>
        /// <param name="onSet"></param>
        private void BB(byte bitMask, bool onSet)
        {

            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                effectiveAddress = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 2)
            {
                aluTmp = bc.ReadCycle(effectiveAddress);

                step++;
            }
            else if (step == 3)
            {
                effectiveAddress = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 4)
            {
                if (onSet ? ((aluTmp & bitMask) != 0) : ((aluTmp & bitMask) == 0))
                {
                    _ = bc.ReadCycle(pc);
                    int result = PcL + (sbyte)effectiveAddress;
                    PcL = (byte)result;

                    if (result > byte.MaxValue)
                        step++;
                    else
                        step = NEXT_INSTR_STEP;
                }
                else
                {
                    opcode = bc.ReadCycle(pc);
                    pc++;

                    step = PIPELINED_FETCH_STEP;
                }
            }
            else if (step == 5) // Skipped if branch was taken and page boundry was not crossed or branch was not taken.
            {
                _ = bc.ReadCycle(pc);
                PcH++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Reads a zero page address and stores it into <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns>true if the step was handled in this function (steps 0-1).</returns>
        private bool ZPG()
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                effectiveAddress = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a zero page address and then adds index and stores the result into <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns>true if the step was handled in this function (steps 0-2).</returns>
        private bool ZPGI(byte index)
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                effectiveAddress = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }
            else if (step == 2)
            {
                _ = bc.ReadCycle(effectiveAddress);
                effectiveAddress = (byte)(effectiveAddress + index);

                step++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads an absolute address and then adds index and stores the result into <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns>true if the step was handled in this function (steps 0-3).</returns>
        private bool ABSI(byte index)
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                EffectiveAddressL = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }
            else if (step == 2)
            {
                EffectiveAddressH = bc.ReadCycle(pc);

                int result = EffectiveAddressL + index;
                EffectiveAddressL = (byte)result;

                pc++;

                if (result > byte.MaxValue)
                    step++;
                else
                    step += 2;
                return true;
            }
            else if (step == 3) // This step is skipped if the addition of EffectiveAddressL and index did not overflow.
            {
                _ = bc.ReadCycle(effectiveAddress);
                EffectiveAddressH++;

                step++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a zero page address, adds x to it, then reads the absolute address stored there and then stores it into
        /// <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns>true if the step was handled in this function (steps 0-4).</returns>
        private bool XIND()
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                indAddr = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }
            else if (step == 2)
            {
                _ = bc.ReadCycle(indAddr);
                indAddr += x;

                step++;
                return true;
            }
            else if (step == 3)
            {
                EffectiveAddressL = bc.ReadCycle(indAddr);
                indAddr++;

                step++;
                return true;
            }
            else if (step == 4)
            {
                EffectiveAddressH = bc.ReadCycle(indAddr);

                step++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads a zero page address, reads an address from that, and then adds y to it and then stores it into
        /// <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns>true if the step was handled in this function (steps 0-4).</returns>
        private bool INDY()
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                indAddr = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }
            else if (step == 2)
            {
                EffectiveAddressL = bc.ReadCycle(indAddr);
                indAddr++;

                step++;
                return true;
            }
            else if (step == 3)
            {
                EffectiveAddressH = bc.ReadCycle(indAddr);

                int result = EffectiveAddressL + y;
                EffectiveAddressL = (byte)result;

                if (result > byte.MaxValue)
                    step++;
                else
                    step += 2;
                return true;
            }
            else if (step == 4) // This step is skipped if the addition of EffectiveAddressL and index did not overflow.
            {
                _ = bc.ReadCycle(effectiveAddress);
                EffectiveAddressH++;

                step++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Takes 4 steps and gets the effective address using indirect addressing and stores it in
        /// <see cref="effectiveAddress"/>.
        /// </summary>
        /// <returns></returns>
        private bool ZPGIND()
        {
            if (step == 0)
            {
                pc++;

                step++;
                return true;
            }
            else if (step == 1)
            {
                indAddr = bc.ReadCycle(pc);
                pc++;

                step++;
                return true;
            }
            else if (step == 2)
            {
                EffectiveAddressL = bc.ReadCycle(indAddr);
                indAddr++;

                step++;
                return true;
            }
            else if (step == 3)
            {
                EffectiveAddressH = bc.ReadCycle(indAddr);

                step++;
                return true;
            }

            return false;
        }
        #endregion Instruction Helpers

        private string StepToString(int step)
        {
            if (step >= 0)
                return step.ToString();

            if (step == NEXT_INSTR_STEP)
                return nameof(NEXT_INSTR_STEP);
            else if (step == WAI_STEP)
                return nameof(WAI_STEP);
            else if (step == STP_STEP)
                return nameof(STP_STEP);

            return step.ToString();
        }

        private delegate void Instruction();
    }
}
