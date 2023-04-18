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
    /// 本地自动保存上下文
    /// </summary>
    public class LocalAutoSaveContext : IAutoSaveContext
    {
        public string GetSavePath()
        {
            string maidataDir = MainWindow.maidataDir;
            if (maidataDir.Length == 0)
            {
                throw new LocalDirNotOpenYetException();
            }

            return MainWindow.maidataDir + "/.autosave";
        }
    }
}
