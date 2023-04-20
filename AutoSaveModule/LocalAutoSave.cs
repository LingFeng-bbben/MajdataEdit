/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.AutoSaveModule
{
    /// <summary>
    /// 本地自动保存
    /// 它将自动保存的文件存储在当前谱面的目录中
    /// </summary>
    public class LocalAutoSave : IAutoSave
    {
        private IAutoSaveContext saveContext = new LocalAutoSaveContext();
        private IAutoSaveIndexManager indexManager = new AutoSaveIndexManager();

        public LocalAutoSave()
        {
            this.indexManager.SetMaxAutoSaveCount(AutoSaveManager.LOCAL_AUTOSAVE_MAX_COUNT);
        }


        public bool DoAutoSave()
        {
            // 本地自动保存前 总是尝试将当前目录更新到目前打开的文件夹上
            this.indexManager.ChangePath(this.saveContext.GetSavePath());

            string newSaveFilePath = this.indexManager.GetNewAutoSaveFileName();

            SimaiProcess.SaveData(newSaveFilePath);

            this.indexManager.RefreshIndex();

            return true;
        }
    }
}
