/* 
 *  Copyright 2008-2018 Alex Makeev
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
 *  Description: DirectX native wrapper (C# replacement for NativeHelper.il)
 *  Date: 10.07.2018
 */
using System;
using System.Runtime.CompilerServices;


namespace ZXMAK2.DirectX.Native
{
    public static class NativeHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void INITBLK(void* dst, byte value, int length)
        {
            Unsafe.InitBlockUnaligned(dst, value, (uint)length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CPBLK(void* dst, void* src, int length)
        {
            Unsafe.CopyBlockUnaligned(dst, src, (uint)length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : struct
        {
            return Unsafe.SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void* VtableSlot(int slot, void* nativePointer)
        {
            return ((void**)(*(void**)nativePointer))[slot];
        }

        //I00
        public static unsafe int CalliInt32(int slot, void* nativePointer)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer);
        }

        //I01
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, void* arg1)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1);
        }

        //I02
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, void* arg1, void* arg2)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2);
        }

        //I03
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, int arg1)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1);
        }

        //I04
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, int arg1, void* arg2)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, int, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2);
        }

        //I05
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0);
        }

        //I06
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            int arg0, int arg1, void* arg2, void* arg3, void* arg4, void* arg5, int arg6)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, void*, void*, void*, void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        //I06-2
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, void* arg6, void* arg7)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, int, int, int, int, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        //I07
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, int arg1, int arg2)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2);
        }

        //I08
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0);
        }

        //I09
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, int arg1, void* arg2, int arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, int, void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I10
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, void* arg1)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1);
        }

        //I11
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, void* arg1, void* arg2)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2);
        }

        //I11-2
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, void* arg1, void* arg2, int arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, void*, void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I12
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, int arg1, void* arg2)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2);
        }

        //I12-2
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, int arg1)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1);
        }

        //I13
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, int arg1, void* arg2, void* arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, int, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I14
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            int arg0, int arg1, void* arg2, int arg3, void* arg4, void* arg5)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, void*, int, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        //I15
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            int arg0, void* arg1, int arg2, int arg3, float arg4, int arg5)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, void*, int, int, float, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        //I16
        public static unsafe int CalliInt32(int slot, void* nativePointer, void* arg0, void* arg1, void* arg2, void* arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I17
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            void* arg0, void* arg1, void* arg2, void* arg3, int arg4)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4);
        }

        //I18
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            void* arg0, void* arg1, int arg2, void* arg3, int arg4, int arg5)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, void*, void*, int, void*, int, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        //I19
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, int arg1, void* arg2, int arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, void*, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I20
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, int arg1, int arg2, void* arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, int, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I21
        public static unsafe int CalliInt32(int slot, void* nativePointer, int arg0, void* arg1, int arg2, int arg3)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, void*, int, int, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3);
        }

        //I22
        public static unsafe int CalliInt32(
            int slot, void* nativePointer,
            int arg0, int arg1, int arg2, int arg3, void* arg4, void* arg5)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, int, int, int, int, void*, void*, int>)VtableSlot(slot, nativePointer);
            return fn(nativePointer, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        //IP00
        public static unsafe IntPtr CalliIntPtr(int slot, void* nativePointer)
        {
            var fn = (delegate* unmanaged[Stdcall]<void*, IntPtr>)VtableSlot(slot, nativePointer);
            return fn(nativePointer);
        }
    }
}
