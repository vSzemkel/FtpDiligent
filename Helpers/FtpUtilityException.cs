
// -----------------------------------------------------------------------
// <copyright file="FtpUtilityException.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Specjalizowany wyjątek skojarzony z klasą FtpUtility. W komunikacie diagnostycznym uwzględnia datę wystąpienia, przekazany w wywolaniu komunikat oraz treść komunikatu systemowego skojarzonego z wywołaniem GetLastError() pobieraną z kernel32.dll. Lepiej użyć gotowej klasy System.ComponentModel.Win32Exception
    /// </summary>
    public class FtpUtilityException : Exception
    {
        private static readonly uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        #region reflection
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FormatMessage(UInt32 dwFlags, IntPtr lpSource, Int32 dwMessageId, UInt32 dwLanguageId, StringBuilder lpBuffer, Int32 dwSize, IntPtr Arguments);

        [DllImport("wininet.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternetGetLastResponseInfo(out int errorCode, StringBuilder buffer, ref int bufferLength);
        #endregion

        #region fields
        private int _iWin32Error;
        private string _sFtpMessage;
        private static readonly int iSysMsgLength = 1024;
        #endregion

        #region properties
        public int iWin32Error {
            get { return _iWin32Error; }
        }

        public override string Message {
            get { return _sFtpMessage; }
        }
        #endregion

        #region constructor
        public FtpUtilityException(string sMsg)
        {
            _iWin32Error = Marshal.GetLastWin32Error();
            var sbSystemMsg = new StringBuilder(iSysMsgLength);
            FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, _iWin32Error, 0, sbSystemMsg, iSysMsgLength, IntPtr.Zero);
            _sFtpMessage = $"{DateTime.Now:dd/MM/yyyy HH:mm)} {sMsg}, {sbSystemMsg}";
        }

        public FtpUtilityException(string sMsg, int iWin32Error)
        {
            var sbSystemMsg = new StringBuilder(iSysMsgLength);
            if (iWin32Error == 12003) { // ERROR_INTERNET_EXTENDED_ERROR
                int length = iSysMsgLength;
                InternetGetLastResponseInfo(out _iWin32Error, sbSystemMsg, ref length);
            } else
                _iWin32Error = iWin32Error;
            FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, _iWin32Error, 0, sbSystemMsg, iSysMsgLength, IntPtr.Zero);
            _sFtpMessage = $"{DateTime.Now:dd/MM/yyyy HH:mm} {sMsg}, {sbSystemMsg}";
        }
        #endregion
    }
}