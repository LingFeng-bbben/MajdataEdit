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
    /// 全局自动保存
    /// 它将自动保存的文件存储在majdata的根目录中
    /// </summary>
    public class GlobalAutoSave : IAutoSave
    {
        private IAutoSaveContext saveContext = new GlobalAutoSaveContext();
        private IAutoSaveIndexManager indexManager = new AutoSaveIndexManager();

        public GlobalAutoSave()
        {
            this.indexManager.ChangePath(this.saveContext.GetSavePath());
            this.indexManager.SetMaxAutoSaveCount(AutoSaveManager.GLOBAL_AUTOSAVE_MAX_COUNT);
        }


        public bool DoAutoSave()
        {
            string newSaveFilePath = this.indexManager.GetNewAutoSaveFileName();

            SimaiProcess.SaveData(newSaveFilePath);

            this.indexManager.RefreshIndex();

            return true;
        }
    }
}
