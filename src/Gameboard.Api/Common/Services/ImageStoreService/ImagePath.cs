// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;

namespace Gameboard.Api.Common.Services;

public static class ImagePath
{
    /// <summary>
    /// Combines a stored image file name with the image folder it lives in, keeping only the file
    /// name portion of the stored value.
    /// </summary>
    /// <remarks>
    /// Image file names originate from uploads, so a plain Path.Combine would let a value like
    /// "../../appsettings.json" resolve outside the image folder. Always resolve stored image
    /// names through here rather than combining them directly.
    /// </remarks>
    public static string Resolve(string imageFolder, string fileName)
    {
        // note: string.IsNullOrWhiteSpace rather than IsEmpty(), which binds to the IEnumerable
        // overload from this namespace and would let a whitespace-only name through
        var safeFileName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeFileName) ? imageFolder : Path.Combine(imageFolder, safeFileName);
    }
}
