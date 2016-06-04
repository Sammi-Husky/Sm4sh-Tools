//=========================================================================\\
// Taken from BrawlLib's source code, credit goes to devs who worked on it.\\
//          (Kryal, Bero, BlackJax96, LibertyErnie, Sammi Husky)           \\
//              My deepest apologies to anyone who i've missed             \\
//=========================================================================\\

using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace System.IO
{
    public abstract class FileMap : IDisposable
    {
        protected VoidPtr _addr;
        protected int _length;
        protected string _path;
        protected FileStream _baseStream;

        public VoidPtr Address { get { return this._addr; } }
        public int Length { get { return this._length; } set { this._length = value; } }
        public string FilePath { get { return this._path; } }

        ~FileMap() { this.Dispose(); }
        public virtual void Dispose()
        {
            if (this._baseStream != null)
            {
                this._baseStream.Close();
                this._baseStream.Dispose();
                this._baseStream = null;
            }

            //#if DEBUG
            //            Console.WriteLine("Closing file map: {0}", _path);
            //#endif
            GC.SuppressFinalize(this);
        }

        public static FileMap FromFile(string path) { return FromFile(path, FileMapProtect.ReadWrite, 0, 0); }
        public static FileMap FromFile(string path, FileMapProtect prot) { return FromFile(path, prot, 0, 0); }
        public static FileMap FromFile(string path, FileMapProtect prot, int offset, int length) { return FromFile(path, prot, 0, 0, FileOptions.RandomAccess); }
        public static FileMap FromFile(string path, FileMapProtect prot, int offset, int length, FileOptions options)
        {
            FileStream stream;
            FileMap map;
            try { stream = new FileStream(path, FileMode.Open, (prot == FileMapProtect.ReadWrite) ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read, 8, options); }
            catch //File is currently in use, but we can copy it to a temp location and read that
            {
                string tempPath = Path.GetTempFileName();
                File.Copy(path, tempPath, true);
                stream = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 8, options | FileOptions.DeleteOnClose);
            }

            try { map = FromStreamInternal(stream, prot, offset, length); }
            catch { stream.Dispose(); throw; }
            map._path = path; //In case we're using a temp file
            stream.Dispose();
            return map;
        }

        public static FileMap FromTempFile(int length)
        {
            FileStream stream = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 8, FileOptions.RandomAccess | FileOptions.DeleteOnClose);
            try { FileMap m = FromStreamInternal(stream, FileMapProtect.ReadWrite, 0, length); stream.Dispose(); return m; }
            catch { stream.Dispose(); throw; }
        }

        public static FileMap FromStream(FileStream stream) { return FromStream(stream, FileMapProtect.ReadWrite, 0, 0); }
        public static FileMap FromStream(FileStream stream, FileMapProtect prot) { return FromStream(stream, prot, 0, 0); }
        public static FileMap FromStream(FileStream stream, FileMapProtect prot, int offset, int length)
        {
            //FileStream newStream = new FileStream(stream.Name, FileMode.Open, prot == FileMapProtect.Read ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, 8, FileOptions.RandomAccess);
            //try { return FromStreamInternal(newStream, prot, offset, length); }
            //catch (Exception x) { newStream.Dispose(); throw x; }

            if (length == 0)
                length = (int)stream.Length;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return new wFileMap(stream.SafeFileHandle.DangerousGetHandle(), prot, offset, (uint)length) { _path = stream.Name };
                default:
                    return new cFileMap(stream, prot, offset, length) { _path = stream.Name };
            }

            //#if DEBUG
            //            Console.WriteLine("Opening file map: {0}", stream.Name);
            //#endif
        }

        public static FileMap FromStreamInternal(FileStream stream, FileMapProtect prot, int offset, int length)
        {
            if (length == 0)
                length = (int)stream.Length;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return new wFileMap(stream.SafeFileHandle.DangerousGetHandle(), prot, offset, (uint)length) { _baseStream = stream, _path = stream.Name };
                default:
                    return new cFileMap(stream, prot, offset, length) { _baseStream = stream, _path = stream.Name };
            }

            //#if DEBUG
            //            Console.WriteLine("Opening file map: {0}", stream.Name);
            //#endif
        }
    }

    public enum FileMapProtect : uint
    {
        Read = 0x01,
        ReadWrite = 0x02
    }

    public class wFileMap : FileMap
    {
        internal wFileMap(VoidPtr hFile, FileMapProtect protect, long offset, uint length)
        {
            long maxSize = offset + length;
            uint maxHigh = (uint)(maxSize >> 32);
            uint maxLow = (uint)maxSize;
            Win32._FileMapProtect mProtect; Win32._FileMapAccess mAccess;
            if (protect == FileMapProtect.ReadWrite)
            {
                mProtect = Win32._FileMapProtect.ReadWrite;
                mAccess = Win32._FileMapAccess.Write;
            }
            else
            {
                mProtect = Win32._FileMapProtect.ReadOnly;
                mAccess = Win32._FileMapAccess.Read;
            }

            using (Win32.SafeHandle h = Win32.CreateFileMapping(hFile, null, mProtect, maxHigh, maxLow, null))
            {
                h.ErrorCheck();
                this._addr = Win32.MapViewOfFile(h.Handle, mAccess, (uint)(offset >> 32), (uint)offset, length);
                if (!this._addr) Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                this._length = (int)length;
            }
        }

        public override void Dispose()
        {
            if (this._addr)
            {
                Win32.FlushViewOfFile(this._addr, 0);
                Win32.UnmapViewOfFile(this._addr);
                this._addr = null;
            }

            base.Dispose();
        }
    }


    public unsafe class cFileMap : FileMap
    {
        protected MemoryMappedFile _mappedFile;
        protected MemoryMappedViewAccessor _mappedFileAccessor;

        public cFileMap(FileStream stream, FileMapProtect protect, int offset, int length)
        {
            MemoryMappedFileAccess cProtect = (protect == FileMapProtect.ReadWrite) ? MemoryMappedFileAccess.ReadWrite : MemoryMappedFileAccess.Read;
            this._length = length;
            this._mappedFile = MemoryMappedFile.CreateFromFile(stream, stream.Name, this._length, cProtect, null, HandleInheritability.None, true);
            this._mappedFileAccessor = this._mappedFile.CreateViewAccessor(offset, this._length, cProtect);
            this._addr = this._mappedFileAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
        }

        public override void Dispose()
        {
            if (this._mappedFile != null)
                this._mappedFile.Dispose();
            if (this._mappedFileAccessor != null)
                this._mappedFileAccessor.Dispose();
            base.Dispose();
        }
    }
}
