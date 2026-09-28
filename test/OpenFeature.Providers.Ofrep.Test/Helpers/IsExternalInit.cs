// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is a placeholder for the IsExternalInit class, which is used to support C# 9.0 init-only properties
// in projects targeting .NET Standard 2.0 and earlier. This file should not be included in projects targeting
// .NET 5.0 or later, as the IsExternalInit class is provided by the runtime in those versions.
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>
///     Reserved to be used by the compiler for tracking metadata.
///     This class should not be used by developers in source code.
/// </summary>
internal static class IsExternalInit
{
}
#endif
