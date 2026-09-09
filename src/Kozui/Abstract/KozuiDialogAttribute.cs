using System;

namespace Kozui.Abstract
{
    /// <summary>
    /// Options for Terminal/host presentation of a Kozui <see cref="ViewDescription{T}"/> dialog.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class KozuiDialogAttribute : Attribute
    {
        /// <summary>Capture emulator framebuffer behind the dialog (default true).</summary>
        public bool CaptureBackdrop { get; set; } = true;

        /// <summary>Host should supply an image painter (keyboard help, etc.).</summary>
        public bool RequireImagePainter { get; set; }
    }
}
