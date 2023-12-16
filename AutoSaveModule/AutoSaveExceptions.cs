/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.Runtime.Serialization;

namespace MajdataEdit.AutoSaveModule;

internal class AutoSaveIndexNotReadyException : Exception
{
    public AutoSaveIndexNotReadyException()
    {
    }

    public AutoSaveIndexNotReadyException(string message) : base(message)
    {
    }

    public AutoSaveIndexNotReadyException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected AutoSaveIndexNotReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public override string Message => base.Message;
}

internal class LocalDirNotOpenYetException : Exception
{
    public LocalDirNotOpenYetException()
    {
    }

    public LocalDirNotOpenYetException(string message) : base(message)
    {
    }

    public LocalDirNotOpenYetException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected LocalDirNotOpenYetException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public override string Message => base.Message;
}