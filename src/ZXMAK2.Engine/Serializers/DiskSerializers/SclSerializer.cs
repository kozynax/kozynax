using System;
using System.IO;
using System.Text;

using ZXMAK2.Model.Disk;
using ZXMAK2.Dependency;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;


namespace ZXMAK2.Serializers.DiskSerializers
{
    public class SclSerializer : HobetaSerializer
    {
        public SclSerializer(DiskImage diskImage)
            : base(diskImage)
        {
        }


        #region FormatSerializer

        public override string FormatName { get { return "SCL disk image"; } }
        public override string FormatExtension { get { return "SCL"; } }
        public override bool CanDeserialize { get { return true; } }

        public override void Deserialize(Stream stream)
        {
            loadFromStream(stream);
            _diskImage.ModifyFlag = ModifyFlag.None;
            _diskImage.Present = true;
        }

        public override void SetSource(string fileName)
        {
            _diskImage.FileName = fileName;
        }

        public override void SetReadOnly(bool readOnly)
        {
            _diskImage.IsWP = readOnly;
        }

        #endregion


        private void loadFromStream(Stream stream)
        {
            if (stream.Length < 9)
            {
                Locator.Resolve<IUserMessage>()
                    .Error("SCL loader\n\nInvalid SCL file size!");
                return;
            }

            byte[] fbuf = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(fbuf, 0, (int)stream.Length);

            if (Encoding.ASCII.GetString(fbuf, 0, 8) != "SINCLAIR")
            {
                Locator.Resolve<IUserMessage>()
                    .Error("SCL loader\n\nCorrupted SCL file!");
                return;
            }

            int fileCount = fbuf[8];
            if (fileCount > 128)
            {
                Locator.Resolve<IUserMessage>()
                    .Error("SCL loader\n\nCorrupted SCL file!");
                return;
            }

            int dirSize = 9 + 14 * fileCount;
            if (fbuf.Length < dirSize)
            {
                Locator.Resolve<IUserMessage>()
                    .Error("SCL loader\n\nCorrupted SCL file!");
                return;
            }

            int size = 0;
            for (int i = 0; i < fileCount; i++)
                size += fbuf[9 + 14 * i + 13];

            // Data may be followed by an optional 4-byte checksum (and rare padding).
            int dataSize = dirSize + size * 256;
            if (fbuf.Length < dataSize)
            {
                Locator.Resolve<IUserMessage>()
                    .Error("SCL loader\n\nInvalid SCL file size!");
                return;
            }

            bool needFormat = true;
            if (_diskImage.IsConnected && _diskImage.Present)
            {
                var service = Locator.Resolve<IUserQuery>();
                DlgResult dlgRes = DlgResult.No;
                if (service != null)
                {
                    dlgRes = service.Show(
                        "Do you want to append file(s) to existing disk?\n\nPlease click 'Yes' to append file(s).\nOr click 'No' to create new disk...",
                        "SCL loader",
                        DlgButtonSet.YesNoCancel,
                        DlgIcon.Question);
                }
                if (dlgRes == DlgResult.Cancel)
                    return;
                if (dlgRes == DlgResult.Yes)
                    needFormat = false;
            }
            if (needFormat)
            {
                // Standard TR-DOS is 80 cyl × 2 sides (2544 free sectors).
                // Some party intros use oversized SCLs that need more tracks.
                int cylCount = 80;
                if (size > 2544)
                {
                    int lastPos = 16 + size - 1; // data starts after track 0 (16 sectors)
                    cylCount = (lastPos / 32) + 1;
                }
                _diskImage.SetPhysics(cylCount, 2);
                _diskImage.FormatTrdos();
            }

            int dataIndex = dirSize;
            for (int i = 0; i < fileCount; i++)
            {
                if (!addFile(fbuf, 9 + 14 * i, dataIndex))
                    return;
                dataIndex += fbuf[9 + 14 * i + 13] * 0x100;
            }
        }
    }
}
