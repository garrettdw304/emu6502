﻿namespace Emu6502
{
    public partial class Cpu
    {
        private void BRK_IMPL_00()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 2)
            {
                bc.WriteCycle(ExpandedS, PcH);
                s--;

                step++;
            }
            else if (step == 3)
            {
                bc.WriteCycle(ExpandedS, PcL);
                s--;

                step++;
            }
            else if (step == 4)
            {
                bc.WriteCycle(ExpandedS, (byte)(p | B_MASK | R_MASK));
                s--;
                // Not sure when to do this. Definitely after we push P.
                I = true;
                D = false;

                step++;
            }
            else if (step == 5)
            {
                EffectiveAddressL = bc.VecCycle(0xFFFE);

                step++;
            }
            else if (step == 6)
            {
                EffectiveAddressH = bc.VecCycle(0xFFFF);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_XIND_01()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Test and set bits.
        /// </summary>
        private void TSB_ZPG_04()
        {
            if (ZPG())
                return;

            RMW(
                () => 
                {
                    Z = (aluTmp & a) == 0;
                    aluTmp |= a;
                }, 2);
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_ZPG_05()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Arithmetic shift left.
        /// </summary>
        private void ASL_ZPG_06()
        {
            if (ZPG())
                return;

            RMW(ASL, 2);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB0_ZPG_07()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1111_1110), 2);
        }

        /// <summary>
        /// Push P.
        /// </summary>
        private void PHP_IMPL_08()
        {
            PH((byte)(p | B_MASK | R_MASK));
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_IMM_09()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SetNZ(a |= bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        private void ASL_A_0A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                int result = a << 1;
                SetNZ((byte)result);
                C = result > byte.MaxValue;
                a = (byte)result;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Test and set bits.
        /// </summary>
        private void TSB_ABS_0C()
        {
            if (ABS())
                return;

            RMW(
                () =>
                {
                    Z = (aluTmp & a) == 0;
                    aluTmp |= a;
                }, 2);
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_ABS_0D()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                SetNZ(a = (byte)(a | bc.ReadCycle(effectiveAddress)));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Arithmetic shift left.
        /// </summary>
        private void ASL_ABS_0E()
        {
            if (ABS())
                return;

            RMW(ASL, 3);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR0_REL_0F()
        {
            BB(0b0000_0001, false);
        }

        /// <summary>
        /// Branch on plus (branch when N = 0).
        /// </summary>
        private void BPL_REL_10()
        {
            BR(!N);
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_INDY_11()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void ORA_ZPGIND_12()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Test and reset bits.
        /// </summary>
        private void TRB_ZPG_14()
        {
            if (ZPG())
                return;

            RMW(
                () =>
                {
                    Z = (aluTmp & a) == 0;
                    aluTmp &= (byte)~a;
                }, 2);
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_ZPGX_15()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Arithmetic shift left.
        /// </summary>
        private void ASL_ZPGX_16()
        {
            if (ZPGI(x))
                return;

            RMW(ASL, 3);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB1_ZPG_17()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1111_1101), 2);
        }

        /// <summary>
        /// Clear C.
        /// </summary>
        private void CLC_IMPL_18()
        {
            if (step == 0)
            {
                step++;

                pc++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                C = false;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_ABSY_19()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void INC_A_1A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(++a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Test and reset bits.
        /// </summary>
        private void TRB_ABS_1C()
        {
            if (ABS())
                return;

            RMW(
                () =>
                {
                    Z = (aluTmp & a) == 0;
                    aluTmp &= (byte)~a;
                }, 2);
        }

        /// <summary>
        /// OR.
        /// </summary>
        private void ORA_ABSX_1D()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                SetNZ(a |= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Arithmetic shift left.
        /// </summary>
        private void ASL_ABSX_1E()
        {
            if (ABSI(x))
                return;

            RMW(ASL, 4);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR1_REL_1F()
        {
            BB(0b0000_0010, false);
        }

        /// <summary>
        /// Jump to subroutine.
        /// </summary>
        private void JSR_ABS_20()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                EffectiveAddressL = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 2)
            {
                _ = bc.ReadCycle(ExpandedS);

                step++;
            }
            else if (step == 3)
            {
                bc.WriteCycle(ExpandedS, PcH);
                s--;

                step++;
            }
            else if (step == 4)
            {
                bc.WriteCycle(ExpandedS, PcL);
                s--;

                step++;
            }
            else if (step == 5)
            {
                EffectiveAddressH = bc.ReadCycle(pc);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_XIND_21()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// BIT test.
        /// </summary>
        private void BIT_ZPG_24()
        {
            if (ZPG())
                return;

            else if (step == 2)
            {
                BIT(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_ZPG_25()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void ROL_ZPG_26()
        {
            if (ZPG())
                return;

            RMW(ROL, 2);
        }

        /// <summary>
        /// Reset memory bit 2.
        /// </summary>
        private void RMB2_ZPG_27()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1111_1011), 2);
        }

        /// <summary>
        /// Pull P.
        /// </summary>
        private void PLP_IMPL_28()
        {
            PL(ref p);
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_IMM_29()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SetNZ(a &= bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        private void ROL_A_2A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                uint result = (uint)((a << 1) | (C ? 1 : 0));
                SetNZ((byte)result);
                C = result > byte.MaxValue;
                a = (byte)result;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Bit test.
        /// </summary>
        private void BIT_ABS_2C()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                BIT(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_ABS_2D()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                SetNZ(a = (byte)(a & bc.ReadCycle(effectiveAddress)));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate left.
        /// </summary>
        private void ROL_ABS_2E()
        {
            if (ABS())
                return;

            RMW(ROL, 3);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR2_REL_2F()
        {
            BB(0b0000_0100, false);
        }

        /// <summary>
        /// Branch on minus. (N = 1)
        /// </summary>
        private void BMI_REL_30()
        {
            BR(N);
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_INDY_31()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void AND_ZPGIND_32()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Bit test.
        /// </summary>
        private void BIT_ZPGX_34()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                BIT(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_ZPGX_35()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate left.
        /// </summary>
        private void ROL_ZPGX_36()
        {
            if (ZPGI(x))
                return;

            RMW(ROL, 3);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB3_ZPG_37()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1111_0111), 2);
        }

        /// <summary>
        /// Set C.
        /// </summary>
        private void SEC_IMPL_38()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                C = true;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_ABSY_39()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void DEC_A_3A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(--a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Bit test.
        /// </summary>
        private void BIT_ABSX_3C()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                BIT(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// AND.
        /// </summary>
        private void AND_ABSX_3D()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                SetNZ(a &= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate left.
        /// </summary>
        private void ROL_ABSX_3E()
        {
            if (ABSI(x))
                return;

            RMW(ROL, 4);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR3_REL_3F()
        {
            BB(0b0000_1000, false);
        }

        /// <summary>
        /// Return from interrupt.
        /// </summary>
        private void RTI_IMPL_40()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                step++;
            } else if (step == 2)
            {
                _ = bc.ReadCycle(ExpandedS);
                s++;

                step++;
            } else if (step == 3)
            {
                p = bc.ReadCycle(ExpandedS);
                s++;

                step++;
            } else if (step == 4)
            {
                EffectiveAddressL = bc.ReadCycle(ExpandedS);
                s++;

                step++;
            } else if (step == 5)
            {
                EffectiveAddressH = bc.ReadCycle(ExpandedS);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_XIND_41()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_ZPG_45()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                // TODO: The actual EOR operation likely happens along side a pipelined fetch. Should we emulate this?
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Logical shift right.
        /// </summary>
        private void LSR_ZPG_46()
        {
            if (ZPG())
                return;

            RMW(LSR, 2);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB4_ZPG_47()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1110_1111), 2);
        }

        /// <summary>
        /// Push A.
        /// </summary>
        private void PHA_IMPL_48()
        {
            PH(a);
        }

        private void EOR_IMM_49()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SetNZ(a ^= bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        private void LSR_A_4A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                int result = a >> 1;
                SetNZ((byte)result);
                C = (a & 1) != 0;
                a = (byte)result;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Jump.
        /// </summary>
        private void JMP_ABS_4C()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                EffectiveAddressL = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 2)
            {
                EffectiveAddressH = bc.ReadCycle(pc);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_ABS_4D()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                SetNZ(a = (byte)(a ^ bc.ReadCycle(effectiveAddress)));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Logical shift right.
        /// </summary>
        private void LSR_ABS_4E()
        {
            if (ABS())
                return;

            RMW(LSR, 3);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR4_REL_4F()
        {
            BB(0b0001_0000, false);
        }

        /// <summary>
        /// Branch on overflow clear.
        /// </summary>
        private void BVC_REL_50()
        {
            BR(!V);
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_INDY_51()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void EOR_ZPGIND_52()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_ZPGX_55()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Logical shift right.
        /// </summary>
        private void LSR_ZPGX_56()
        {
            if (ZPGI(x))
                return;

            RMW(LSR, 3);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB5_ZPG_57()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1101_1111), 2);
        }

        /// <summary>
        /// Clear interrupt bit (enable interrupts).
        /// </summary>
        private void CLI_IMPL_58()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                I = false;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_ABSY_59()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Push Y.
        /// </summary>
        private void PHY_IMPL_5A()
        {
            PH(y);
        }

        /// <summary>
        /// Exclusive OR.
        /// </summary>
        private void EOR_ABSX_5D()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                SetNZ(a ^= bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Logical shift right.
        /// </summary>
        private void LSR_ABSX_5E()
        {
            if (ABSI(x))
                return;

            RMW(LSR, 4);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR5_REL_5F()
        {
            BB(0b0010_0000, false);
        }

        /// <summary>
        /// Return from subroutine.
        /// </summary>
        private void RTS_IMPL_60()
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
                EffectiveAddressL = bc.ReadCycle(ExpandedS);
                s++;

                step++;
            }
            else if (step == 4)
            {
                EffectiveAddressH = bc.ReadCycle(ExpandedS);
                pc = effectiveAddress;

                step++;
            }
            else if (step == 5)
            {
                _ = bc.ReadCycle(pc);
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_XIND_61()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void STZ_ZPG_64()
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
                bc.WriteCycle(effectiveAddress, 0);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_ZPG_65()
        {
            if (ZPG())
                return;

            else if (step == 2)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate right.
        /// </summary>
        private void ROR_ZPG_66()
        {
            if (ZPG())
                return;

            RMW(ROR, 2);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB6_ZPG_67()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b1011_1111), 2);
        }

        /// <summary>
        /// Pull A.
        /// </summary>
        private void PLA_IMPL_68()
        {
            PL(ref a);
            SetNZ(a);
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_IMM_69()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                ADC(bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        private void ROR_A_6A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                uint result = (uint)((a >> 1) | (C ? 0b1000_0000 : 0b0000_0000));
                SetNZ((byte)result);
                C = (a & 1) != 0;
                a = (byte)result;

                step = NEXT_INSTR_STEP;
            }
        }

        private void JMP_IND_6C()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                EffectiveAddressL = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 2)
            {
                EffectiveAddressH = bc.ReadCycle(pc);
                pc++;

                step++;
            }
            else if (step == 3)
            {
                PcL = bc.ReadCycle(effectiveAddress);
                int result = EffectiveAddressL + 1;
                EffectiveAddressL = (byte)result;

                if (result > byte.MaxValue)
                    step++;
                else
                    step += 2;
            }
            else if (step == 4)
            {
                _ = bc.ReadCycle(effectiveAddress);
                PcH++;

                step++;
            }
            else if (step == 5)
            {
                PcH = bc.ReadCycle(effectiveAddress);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_ABS_6D()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                // TODO: Does the math happen this cycle or does it do a pipelining thing where it does the math next cycle while reading the next opcode?
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate right.
        /// </summary>
        private void ROR_ABS_6E()
        {
            if (ABS())
                return;

            RMW(ROR, 3);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR6_REL_6F()
        {
            BB(0b0100_0000, false);
        }

        /// <summary>
        /// Branch on overflow set.
        /// </summary>
        private void BVS_REL_70()
        {
            BR(V);
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_INDY_71()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void ADC_ZPGIND_72()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void STZ_ZPGX_74()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, 0);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_ZPGX_75()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate right.
        /// </summary>
        private void ROR_ZPGX_76()
        {
            if (ZPGI(x))
                return;

            RMW(ROR, 3);
        }

        /// <summary>
        /// Reset memory bit.
        /// </summary>
        private void RMB7_ZPG_77()
        {
            if (ZPG())
                return;

            RMW(() => RMB(0b0111_1111), 2);
        }

        /// <summary>
        /// Set interrupt.
        /// </summary>
        private void SEI_IMPL_78()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                I = true;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_ABSY_79()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Pull Y.
        /// </summary>
        private void PLY_IMPL_7A()
        {
            PL(ref y);
        }

        private void JMP_ABSXIND_7C()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add with carry.
        /// </summary>
        private void ADC_ABSX_7D()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                ADC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Rotate right.
        /// </summary>
        private void ROR_ABSX_7E()
        {
            if (ABSI(x))
                return;

            RMW(ROR, 4);
        }

        /// <summary>
        /// Branch on bit reset.
        /// </summary>
        private void BBR7_REL_7F()
        {
            BB(0b1000_0000, false);
        }

        /// <summary>
        /// Branch always.
        /// </summary>
        private void BRA_REL_80()
        {
            BR(true);
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_XIND_81()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store y.
        /// </summary>
        private void STY_ZPG_84()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                bc.WriteCycle(effectiveAddress, y);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_ZPG_85()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store x.
        /// </summary>
        private void STX_ZPG_86()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                bc.WriteCycle(effectiveAddress, x);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB0_ZPG_87()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0000_0001), 2);
        }

        /// <summary>
        /// Decrement y.
        /// </summary>
        private void DEY_IMPL_88()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(--y);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Bit.
        /// </summary>
        private void BIT_IMM_89()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                BIT(bc.ReadCycle(pc)); // TODO: All immediate instructions are likely pipelined with next instr fetch...
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Transfer x to a.
        /// </summary>
        private void TXA_IMPL_8A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(a = x);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store y.
        /// </summary>
        private void STY_ABS_8C()
        {
            if (ABS())
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, y);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_ABS_8D()
        {
            if (ABS())
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store x.
        /// </summary>
        private void STX_ABS_8E()
        {
            if (ABS())
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, x);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS0_REL_8F()
        {
            BB(0b0000_0001, true);
        }

        /// <summary>
        /// Branch on carry clear.
        /// </summary>
        private void BCC_REL_90()
        {
            BR(!C);
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_INDY_91()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        private void STA_ZPGIND_92()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store y.
        /// </summary>
        private void STY_ZPGX_94()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, y);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_ZPGX_95()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store x.
        /// </summary>
        private void STX_ZPGY_96()
        {
            if (ZPGI(y))
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, x);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB1_ZPG_97()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0000_0010), 2);
        }

        private void TYA_IMPL_98()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(a = y);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_ABSY_99()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Transfer x to stack.
        /// </summary>
        private void TXS_IMPL_9A()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                s = x;

                step = NEXT_INSTR_STEP;
            }
        }

        private void STZ_ABS_9C()
        {
            if (ABS())
                return;

            if (step == 3)
            {
                bc.WriteCycle(effectiveAddress, 0);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Store a.
        /// </summary>
        private void STA_ABSX_9D()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                bc.WriteCycle(effectiveAddress, a);

                step = NEXT_INSTR_STEP;
            }
        }

        private void STZ_ABSX_9E()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                bc.WriteCycle(effectiveAddress, 0);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS1_REL_9F()
        {
            BB(0b0000_0010, true);
        }

        /// <summary>
        /// Load y.
        /// </summary>
        private void LDY_IMM_A0()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SetNZ(y = bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }
        
        /// <summary>
        /// Load a.
        /// </summary>
        private void LDA_XIND_A1()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load x.
        /// </summary>
        private void LDX_IMM_A2()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SetNZ(x = bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load y.
        /// </summary>
        private void LDY_ZPG_A4()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                SetNZ(y = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load y.
        /// </summary>
        private void LDA_ZPG_A5()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load x.
        /// </summary>
        private void LDX_ZPG_A6()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                SetNZ(x = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB2_ZPG_A7()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0000_0100), 2);
        }

        private void TAY_IMPL_A8()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(y = a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load a.
        /// </summary>
        private void LDA_IMM_A9()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SetNZ(a = bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }
        
        private void TAX_IMPL_AA()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(x = a);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load Y.
        /// </summary>
        private void LDY_ABS_AC()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                SetNZ(y = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load A.
        /// </summary>
        private void LDA_ABS_AD()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load X.
        /// </summary>
        private void LDX_ABS_AE()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                SetNZ(x = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS2_REL_AF()
        {
            BB(0b0000_0100, true);
        }

        /// <summary>
        /// Branch on carry set.
        /// </summary>
        private void BCS_REL_B0()
        {
            BR(C);
        }

        /// <summary>
        /// Load a.
        /// </summary>
        private void LDA_INDY_B1()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void LDA_ZPGIND_B2()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load y.
        /// </summary>
        private void LDY_ZPGX_B4()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                SetNZ(y = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load a.
        /// </summary>
        private void LDA_ZPGX_B5()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load x.
        /// </summary>
        private void LDX_ZPGY_B6()
        {
            if (ZPGI(y))
                return;

            if (step == 3)
            {
                SetNZ(x = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB3_ZPG_B7()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0000_1000), 2);
        }

        private void CLV_IMPL_B8()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                V = false;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load A.
        /// </summary>
        private void LDA_ABSY_B9()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void TSX_IMPL_BA()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(x = s);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load y.
        /// </summary>
        private void LDY_ABSX_BC()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                SetNZ(y = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load x.
        /// </summary>
        private void LDA_ABSX_BD()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                SetNZ(a = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Load x.
        /// </summary>
        private void LDX_ABSY_BE()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                SetNZ(x = bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS3_REL_BF()
        {
            BB(0b0000_1000, true);
        }

        /// <summary>
        /// Compare y.
        /// </summary>
        private void CPY_IMM_C0()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                CMP(y, bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_XIND_C1()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare y.
        /// </summary>
        private void CPY_ZPG_C4()
        {
            if (ZPG())
                return;

            else if (step == 2)
            {
                CMP(y, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_ZPG_C5()
        {
            if (ZPG())
                return;

            else if (step == 2)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Decrement.
        /// </summary>
        private void DEC_ZPG_C6()
        {
            if (ZPG())
                return;

            RMW(() => SetNZ(--aluTmp), 2);
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB4_ZPG_C7()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0001_0000), 2);
        }

        /// <summary>
        /// Increment y.
        /// </summary>
        private void INY_IMPL_C8()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(++y);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_IMM_C9()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                CMP(a, bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Decrement X.
        /// </summary>
        private void DEX_IMPL_CA()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(--x);

                step = NEXT_INSTR_STEP;
            }
        }

        private void WAI_IMPL_CB()
        {
            throw new NotImplementedException();
            if (step == 0)
            {
                step = WAI_STEP;
                pc++; // pc has already been incremented. As soon as an interrupt happens it will push this and then return to the next instruction to be executed.
            }
            else
                throw new InvalidOperationException();
        }

        private void CPY_ABS_CC()
        {
            if (ABS())
                return;

            if (step == 3)
            {
                CMP(y, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_ABS_CD()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Decrement.
        /// </summary>
        private void DEC_ABS_CE()
        {
            if (ABS())
                return;

            RMW(() => SetNZ(--aluTmp), 3);
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS4_REL_CF()
        {
            BB(0b0001_0000, true);
        }

        /// <summary>
        /// Branch on not equal (branch on Z = 0).
        /// </summary>
        private void BNE_REL_D0()
        {
            BR(!Z);
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_INDY_D1()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void CMP_ZPGIND_D2()
        {
            if (ZPGIND())
                return;
            
            if (step == 4)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_ZPGX_D5()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Decrement.
        /// </summary>
        private void DEC_ZPGX_D6()
        {
            if (ZPGI(x))
                return;

            RMW(() => SetNZ(--aluTmp), 3);
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB5_ZPG_D7()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0010_0000), 2);
        }

        private void CLD_IMPL_D8()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                D = false;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_ABSY_D9()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Push X.
        /// </summary>
        private void PHX_IMPL_DA()
        {
            PH(x);
        }

        private void STP_IMPL_DB()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Compare.
        /// </summary>
        private void CMP_ABSX_DD()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                CMP(a, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Decrement.
        /// </summary>
        private void DEC_ABSX_DE()
        {
            // TODO: According to http://www.6502.org/tutorials/65c02opcodes.html DEC ABSX always takes 7 cycles.
            if (ABSI(x))
                return;

            RMW(() => SetNZ(--aluTmp), 4);
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS5_REL_DF()
        {
            BB(0b0010_0000, true);
        }

        private void CPX_IMM_E0()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                CMP(x, bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_XIND_E1()
        {
            if (XIND())
                return;

            if (step == 5)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void CPX_ZPG_E4()
        {
            if (ZPG())
                return;

            if (step == 2)
            {
                CMP(x, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_ZPG_E5()
        {
            if (ZPG())
                return;

            else if (step == 2)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Increment.
        /// </summary>
        private void INC_ZPG_E6()
        {
            if (ZPG())
                return;

            RMW(() => SetNZ(++aluTmp), 2);
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB6_ZPG_E7()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b0100_0000), 2);
        }

        private void INX_IMPL_E8()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                SetNZ(++x);

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_IMM_E9()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                SBC(bc.ReadCycle(pc));
                pc++;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// NOP.
        /// </summary>
        private void NOP_IMPL_EA()
        {
            if (step == 0)
            {
                pc++;

                step++;
            } else if (step == 1)
            {
                // TODO: Does nop take 1 or 2 bytes (should we inc pc again)?
                _ = bc.ReadCycle(pc);

                step = NEXT_INSTR_STEP;
            }
        }

        private void CPX_ABS_EC()
        {
            if (ABS())
                return;

            if (step == 3)
            {
                CMP(x, bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Subtract with carry.
        /// </summary>
        private void SBC_ABS_ED()
        {
            if (ABS())
                return;

            else if (step == 3)
            {
                // TODO: Does the math happen this cycle or does it do a pipelining thing where it does the math next cycle while reading the next opcode?
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Increment.
        /// </summary>
        private void INC_ABS_EE()
        {
            if (ABS())
                return;

            RMW(() => SetNZ(++aluTmp), 3);
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS6_REL_EF()
        {
            BB(0b0100_0000, true);
        }

        /// <summary>
        /// Branch on equal (branch on Z = 1).
        /// </summary>
        private void BEQ_REL_F0()
        {
            BR(Z);
        }
        
        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_INDY_F1()
        {
            if (INDY())
                return;

            if (step == 5)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        private void SBC_ZPGIND_F2()
        {
            if (ZPGIND())
                return;

            if (step == 4)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_ZPGX_F5()
        {
            if (ZPGI(x))
                return;

            if (step == 3)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Increment.
        /// </summary>
        private void INC_ZPGX_F6()
        {
            if (ZPGI(x))
                return;

            RMW(() => SetNZ(++aluTmp), 3);
        }

        /// <summary>
        /// Set memory bit.
        /// </summary>
        private void SMB7_ZPG_F7()
        {
            if (ZPG())
                return;

            RMW(() => SMB(0b1000_0000), 2);
        }

        private void SED_IMPL_F8()
        {
            if (step == 0)
            {
                pc++;

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);
                D = true;

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_ABSY_F9()
        {
            if (ABSI(y))
                return;

            if (step == 4)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Pull X.
        /// </summary>
        private void PLX_IMPL_FA()
        {
            PL(ref x);
        }

        /// <summary>
        /// Subtract with borrow.
        /// </summary>
        private void SBC_ABSX_FD()
        {
            if (ABSI(x))
                return;

            if (step == 4)
            {
                SBC(bc.ReadCycle(effectiveAddress));

                step = NEXT_INSTR_STEP;
            }
        }

        /// <summary>
        /// Increment.
        /// </summary>
        private void INC_ABSX_FE()
        {
            // TODO: According to http://www.6502.org/tutorials/65c02opcodes.html INC ABSX always takes 7 cycles.
            if (ABSI(x))
                return;

            RMW(() => SetNZ(++aluTmp), 4);
        }

        /// <summary>
        /// Branch on bit set.
        /// </summary>
        private void BBS7_REL_FF()
        {
            BB(0b1000_0000, true);
        }

        private void NMI()
        {
            if (step == 0)
            {
                _ = bc.ReadCycle(pc); // We actully have to handle the bus on step 0 this time.

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                step++;
            }
            else if (step == 2)
            {
                bc.WriteCycle(ExpandedS, PcH);
                s--;

                step++;
            }
            else if (step == 3)
            {
                bc.WriteCycle(ExpandedS, PcL);
                s--;

                step++;
            }
            else if (step == 4)
            {
                bc.WriteCycle(ExpandedS, (byte)(p | R_MASK));
                s--;
                // Not sure when to do this. Definitely after we push P.
                I = true;
                D = false;

                step++;
            }
            else if (step == 5)
            {
                EffectiveAddressL = bc.VecCycle(0xFFFA);

                step++;
            }
            else if (step == 6)
            {
                EffectiveAddressH = bc.VecCycle(0xFFFB);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }

        private void IRQ()
        {
            if (step == 0)
            {
                _ = bc.ReadCycle(pc); // We actully have to handle the bus on step 0 this time.

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                step++;
            }
            else if (step == 2)
            {
                bc.WriteCycle(ExpandedS, PcH);
                s--;
                
                step++;
            }
            else if (step == 3)
            {
                bc.WriteCycle(ExpandedS, PcL);
                s--;

                step++;
            }
            else if (step == 4)
            {
                bc.WriteCycle(ExpandedS, (byte)(p | R_MASK));
                s--;
                // Not sure when to do this. Definitely after we push P.
                I = true;
                D = false;

                step++;
            }
            else if (step == 5)
            {
                EffectiveAddressL = bc.VecCycle(0xFFFE);

                step++;
            }
            else if (step == 6)
            {
                EffectiveAddressH = bc.VecCycle(0xFFFF);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }

        /* Note this:
        Response of the 65C02 to a reset is different from the 6502 in that the 65C02's program counter and status register are written to the stack.
        Additionally, the 65C02 decimal flag is cleared after reset or interrupt; its value is indeterminate after reset and not modified after interrupt on the 6502.
         */
        /// <summary>
        /// I am not 100% that every cycle does what actually is happening in the W65C02 but at least the most important
        /// things happen before the end of RST (I is set, B is cleared and PC is loaded with FFFC..FFFD).
        /// See: https://www.pagetable.com/?p=410
        /// </summary>
        private void RST()
        {
            if (step == 0)
            {
                _ = bc.ReadCycle(pc); // We actully have to handle the bus on step 0 this time.

                step++;
            }
            else if (step == 1)
            {
                _ = bc.ReadCycle(pc);

                step++;
            }
            else if (step == 2)
            {
                bc.WriteCycle(ExpandedS, PcH);
                s--;

                step++;
            }
            else if (step == 3)
            {
                bc.WriteCycle(ExpandedS, PcL);
                s--;

                step++;
            }
            else if (step == 4)
            {
                bc.WriteCycle(ExpandedS, (byte)(p | R_MASK));
                s--;
                // Not sure when to do this. Definitely after we push P.
                I = true;
                D = false;

                step++;
            }
            else if (step == 5)
            {
                EffectiveAddressL = bc.VecCycle(0xFFFC);

                step++;
            }
            else if (step == 6)
            {
                EffectiveAddressH = bc.VecCycle(0xFFFD);
                pc = effectiveAddress;

                step = NEXT_INSTR_STEP;
            }
        }
    }
}
