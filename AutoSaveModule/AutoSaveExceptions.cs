/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.AutoSaveModule
{
    class AutoSaveIndexNotReadyException : Exception
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

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }
    }

    class LocalDirNotOpenYetException : Exception
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

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }
    }
}
