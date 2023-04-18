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
    class AutoSaveIndexManager : IAutoSaveIndexManager
    {
        bool isReady = false;
        string curPath;
        int maxAutoSaveCount;
        AutoSaveIndex index;

        public AutoSaveIndexManager()
        {
            this.maxAutoSaveCount = 5;
        }

        public AutoSaveIndexManager(int maxAutoSaveCount)
        {
            this.maxAutoSaveCount = maxAutoSaveCount;
        }

        public void ChangePath(string path)
        {
            if (path != this.curPath)
            {
                // 只有当新目录和之前设置的目录不同时，才会触发index文件读写
                this.curPath = path;
                this.LoadOrCreateIndexFile();
            }

            this.isReady = true;
        }

        public int GetFileCount()
        {
            if (!this.IsReady())
            {
                throw new AutoSaveIndexNotReadyException("AutoSaveIndexManager is not ready yet.");
            }

            return this.index.Count;
        }

        public List<AutoSaveIndex.FileInfo> GetFileInfos()
        {
            if (!this.IsReady())
            {
                throw new AutoSaveIndexNotReadyException("AutoSaveIndexManager is not ready yet.");
            }

            return this.index.FilesInfo;
        }

        public int GetMaxAutoSaveCount()
        {
            return this.maxAutoSaveCount;
        }

        public string GetNewAutoSaveFileName()
        {
            string path = this.curPath + "/autosave." + this.GetCurrentTimeString() + ".txt";

            AutoSaveIndex.FileInfo fileInfo = new AutoSaveIndex.FileInfo();
            fileInfo.FileName = path;
            fileInfo.SavedTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            this.index.FilesInfo.Add(fileInfo);

            this.index.Count++;

            // 将变更存储到index文件中
            this.UpdateIndexFile();

            return path;
        }

        public bool IsReady()
        {
            return this.isReady;
        }

        public void RefreshIndex()
        {
            // 从this.index.FileInfo的表头开始删除 直到Count小于等于maxAutoSaveCount
            while (this.index.Count > this.maxAutoSaveCount)
            {
                AutoSaveIndex.FileInfo fileInfo = this.index.FilesInfo[0];
                File.Delete(fileInfo.FileName);
                this.index.FilesInfo.RemoveAt(0);
                this.index.Count--;
            }

            // 将变更存储到index文件中
            this.UpdateIndexFile();
        }

        public void SetMaxAutoSaveCount(int maxAutoSaveCount)
        {
            this.maxAutoSaveCount = maxAutoSaveCount;
        }



        private void LoadOrCreateIndexFile()
        {
            this.CreateDirectoryIfNotExists(this.curPath);
            this.KeepDirectoryHidden(this.curPath);

            string indexFilePath = this.curPath + "/.index.json";
            if (!File.Exists(indexFilePath))
            {
                this.index = new AutoSaveIndex();
                this.UpdateIndexFile();
            }
            else
            {
                this.LoadIndexFromFile();
            }
        }


        /// <summary>
        /// 若文件夹不存在则创建
        /// </summary>
        /// <param name="dirPath"></param>
        private void CreateDirectoryIfNotExists(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        /// <summary>
        /// 保证文件夹处于隐藏状态
        /// </summary>
        /// <param name="dirPath"></param>
        private void KeepDirectoryHidden(string dirPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

            if ((dirInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            {
                dirInfo.Attributes = FileAttributes.Hidden;
            }
        }

        /// <summary>
        /// 将saveIndex存储到index文件中
        /// </summary>
        private void UpdateIndexFile()
        {
            string indexPath = this.curPath + "/.index.json";

            string jsonText = JsonConvert.SerializeObject(this.index);
            File.WriteAllText(indexPath, jsonText);
        }

        /// <summary>
        /// 从index文件读取saveIndex
        /// </summary>
        private void LoadIndexFromFile()
        {
            string indexPath = this.curPath + "/.index.json";

            string jsonText = File.ReadAllText(indexPath);
            this.index = JsonConvert.DeserializeObject<AutoSaveIndex>(jsonText);
        }

        /// <summary>
        /// 获取当前时间字符串
        /// </summary>
        /// <returns></returns>
        private string GetCurrentTimeString()
        {
            DateTime now = DateTime.Now;

            return now.Year.ToString() + "-" +
                   now.Month.ToString() + "-" +
                   now.Day.ToString() + "_" +
                   now.Hour.ToString() + "-" +
                   now.Minute.ToString() + "-" +
                   now.Second.ToString();
        }
    }
}
