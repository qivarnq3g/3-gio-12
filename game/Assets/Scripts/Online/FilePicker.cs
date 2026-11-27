using System;
using System.Runtime.InteropServices;

namespace BaGio12.Online
{
    public static class FilePicker
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner, hInstance;
            public string lpstrFilter, lpstrCustomFilter;
            public int nMaxCustFilter, nFilterIndex;
            public IntPtr lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir, lpstrTitle;
            public int Flags;
            public short nFileOffset, nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData, lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved, FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetOpenFileNameW(ref OpenFileName ofn);

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        public static bool Supported => true;

        public static string PickImage(string title)
        {
            const int chars = 4096;
            IntPtr buffer = Marshal.AllocHGlobal(chars * 2);
            try
            {
                Marshal.Copy(new byte[chars * 2], 0, buffer, chars * 2);
                var ofn = new OpenFileName
                {
                    lStructSize = Marshal.SizeOf(typeof(OpenFileName)),
                    hwndOwner = GetActiveWindow(),
                    lpstrFilter = "Ảnh JPEG (*.jpg, *.jpeg)\0*.jpg;*.jpeg\0Mọi tệp (*.*)\0*.*\0\0",
                    nFilterIndex = 1,
                    lpstrFile = buffer,
                    nMaxFile = chars,
                    lpstrTitle = title,
                    Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008,
                };
                return GetOpenFileNameW(ref ofn) ? Marshal.PtrToStringUni(buffer) : null;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
#else
        public static bool Supported => false;

        public static string PickImage(string title) => null;
#endif
    }
}
