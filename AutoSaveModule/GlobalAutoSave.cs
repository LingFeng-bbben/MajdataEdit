/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit.AutoSaveModule;

/// <summary>
///     全局自动保存
///     它将自动保存的文件存储在majdata的根目录中
/// </summary>
public class GlobalAutoSave : IAutoSave
{
    private readonly IAutoSaveIndexManager indexManager = new AutoSaveIndexManager();
    private readonly IAutoSaveContext saveContext = new GlobalAutoSaveContext();

    public GlobalAutoSave()
    {
        indexManager.ChangePath(saveContext.GetSavePath());
        indexManager.SetMaxAutoSaveCount(AutoSaveManager.GLOBAL_AUTOSAVE_MAX_COUNT);
    }


    public bool DoAutoSave()
    {
        var newSaveFilePath = indexManager.GetNewAutoSaveFileName();

        SimaiProcess.SaveData(newSaveFilePath);

        indexManager.RefreshIndex();

        return true;
    }
}