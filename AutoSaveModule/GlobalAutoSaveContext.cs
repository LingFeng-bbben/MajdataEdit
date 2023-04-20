/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.AutoSaveModule
{
    /// <summary>
    /// 全局自动保存上下文
    /// </summary>
    public class GlobalAutoSaveContext : IAutoSaveContext
    {
        public string GetSavePath()
        {
            return Environment.CurrentDirectory + "/.autosave";
        }
    }
}
