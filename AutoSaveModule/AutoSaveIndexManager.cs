/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.IO;
using Newtonsoft.Json;

namespace MajdataEdit.AutoSaveModule;

internal class AutoSaveIndexManager : IAutoSaveIndexManager
{
    private string? curPath;
    private AutoSaveIndex? index;
    private bool isReady;
    private int maxAutoSaveCount;

    public AutoSaveIndexManager()
    {
        maxAutoSaveCount = 5;
    }

    public AutoSaveIndexManager(int maxAutoSaveCount)
    {
        this.maxAutoSaveCount = maxAutoSaveCount;
    }

    public void ChangePath(string path)
    {
        if (path != curPath)
        {
            // 只有当新目录和之前设置的目录不同时，才会触发index文件读写
            curPath = path;
            LoadOrCreateIndexFile();
        }

        isReady = true;
    }

    public int GetFileCount()
    {
        if (!IsReady()) throw new AutoSaveIndexNotReadyException("AutoSaveIndexManager is not ready yet.");

        return index!.Count;
    }

    public List<AutoSaveIndex.FileInfo> GetFileInfos()
    {
        if (!IsReady()) throw new AutoSaveIndexNotReadyException("AutoSaveIndexManager is not ready yet.");

        return index!.FilesInfo;
    }

    public int GetMaxAutoSaveCount()
    {
        return maxAutoSaveCount;
    }

    public string GetNewAutoSaveFileName()
    {
        var path = curPath + "/autosave." + GetCurrentTimeString() + ".txt";

        var fileInfo = new AutoSaveIndex.FileInfo
        {
            FileName = path,
            SavedTime = DateTimeOffset.Now.AddHours(8).ToUnixTimeSeconds(),
            RawPath = MainWindow.maidataDir
        };
        index!.FilesInfo.Add(fileInfo);

        index.Count++;

        // 将变更存储到index文件中
        UpdateIndexFile();

        return path;
    }

    public bool IsReady()
    {
        return isReady;
    }

    public void RefreshIndex()
    {
        // 先扫描一遍，如果有文件已经被删了就先移除掉
        for (var i = index!.Count - 1; i >= 0; i--)
        {
            var fileInfo = index.FilesInfo[i];
            if (!File.Exists(fileInfo.FileName))
            {
                index.FilesInfo.RemoveAt(i);
                index.Count--;
            }
        }

        // 然后从this.index.FileInfo的表头开始删除 直到保证自动保存文件的数量符合maxAutoSaveCount的要求
        while (index.Count > maxAutoSaveCount)
        {
            var fileInfo = index.FilesInfo[0];
            File.Delete(fileInfo.FileName!);
            index.FilesInfo.RemoveAt(0);
            index.Count--;
        }

        // 将变更存储到index文件中
        UpdateIndexFile();
    }

    public void SetMaxAutoSaveCount(int maxAutoSaveCount)
    {
        this.maxAutoSaveCount = maxAutoSaveCount;
        Console.WriteLine("maxAutoSaveCount:" + maxAutoSaveCount);
    }


    private void LoadOrCreateIndexFile()
    {
        CreateDirectoryIfNotExists(curPath!);
        KeepDirectoryHidden(curPath!);

        var indexFilePath = curPath + "/.index.json";
        if (!File.Exists(indexFilePath))
        {
            index = new AutoSaveIndex();
            UpdateIndexFile();
        }
        else
        {
            LoadIndexFromFile();
        }
    }


    /// <summary>
    ///     若文件夹不存在则创建
    /// </summary>
    /// <param name="dirPath"></param>
    private void CreateDirectoryIfNotExists(string dirPath)
    {
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
    }

    /// <summary>
    ///     保证文件夹处于隐藏状态
    /// </summary>
    /// <param name="dirPath"></param>
    private void KeepDirectoryHidden(string dirPath)
    {
        var dirInfo = new DirectoryInfo(dirPath);

        if ((dirInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            dirInfo.Attributes = FileAttributes.Hidden;
    }

    /// <summary>
    ///     将saveIndex存储到index文件中
    /// </summary>
    private void UpdateIndexFile()
    {
        var indexPath = curPath + "/.index.json";

        var jsonText = JsonConvert.SerializeObject(index);
        File.WriteAllText(indexPath, jsonText);
    }

    /// <summary>
    ///     从index文件读取saveIndex
    /// </summary>
    private void LoadIndexFromFile()
    {
        var indexPath = curPath + "/.index.json";

        var jsonText = File.ReadAllText(indexPath);
        index = JsonConvert.DeserializeObject<AutoSaveIndex>(jsonText);
    }

    /// <summary>
    ///     获取当前时间字符串
    /// </summary>
    /// <returns></returns>
    private string GetCurrentTimeString()
    {
        var now = DateTime.Now;

        return now.Year + "-" +
               now.Month + "-" +
               now.Day + "_" +
               now.Hour + "-" +
               now.Minute + "-" +
               now.Second;
    }
}