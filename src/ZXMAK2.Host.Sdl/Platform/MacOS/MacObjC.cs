using System;
using System.Runtime.InteropServices;

namespace ZXMAK2.Host.SdlBackend.Platform.MacOS
{
    /// <summary>Minimal Objective-C runtime + AppKit helpers for the native menu bar.</summary>
    internal static class MacObjC
    {
        private const string LibObjc = "/usr/lib/libobjc.A.dylib";

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, byte arg1);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, long arg1);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern long SendLong(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjc)]
        public static extern IntPtr sel_registerName(string name);

        [DllImport(LibObjc)]
        public static extern IntPtr objc_getClass(string name);

        [DllImport(LibObjc)]
        public static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, nuint extraBytes);

        [DllImport(LibObjc)]
        public static extern bool class_addMethod(IntPtr clazz, IntPtr selector, IntPtr imp, string types);

        [DllImport(LibObjc)]
        public static extern void objc_registerClassPair(IntPtr clazz);

        [DllImport(LibObjc)]
        public static extern IntPtr class_createInstance(IntPtr clazz, nuint extraBytes);

        public static IntPtr Sel(string name) => sel_registerName(name);

        public static IntPtr Class(string name) => objc_getClass(name);

        public static IntPtr NsString(string text)
        {
            if (text == null)
                text = string.Empty;
            var utf8 = Marshal.StringToCoTaskMemUTF8(text);
            try
            {
                return Send(Class("NSString"), Sel("stringWithUTF8String:"), utf8);
            }
            finally
            {
                Marshal.FreeCoTaskMem(utf8);
            }
        }

        public static IntPtr Alloc(string className)
            => Send(Class(className), Sel("alloc"));

        public static IntPtr Init(IntPtr obj)
            => Send(obj, Sel("init"));

        public static IntPtr AllocInit(string className)
            => Init(Alloc(className));

        /// <summary>NSApplicationActivationPolicyRegular</summary>
        public const long ActivationPolicyRegular = 0;

        /// <summary>NSControlStateValueOn / Off</summary>
        public const long ControlStateOn = 1;
        public const long ControlStateOff = 0;
    }
}
