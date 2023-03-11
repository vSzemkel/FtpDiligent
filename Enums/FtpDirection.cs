
// -----------------------------------------------------------------------
// <copyright file="FtpDirection.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;

/// <summary>
/// Kierunki transferu dla endpointu
/// </summary>
[Flags]
public enum eFtpDirection : byte
{
    Get = 1,
    Put = 2,
    HotfolderPut = 4
}
