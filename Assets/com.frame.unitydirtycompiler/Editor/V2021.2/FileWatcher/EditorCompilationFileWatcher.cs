#if UNITY_EDITOR
#if UNITY_2021_2
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FinalFrame.EditorTool {

    // ---- 用于监控文件变化 ----
    [Serializable]
    public class EditorCompilationFileWatcher {

        FileSystemWatcher watcher;

        public delegate void OnScriptDirty(string scriptFilePath);
        public event OnScriptDirty OnScriptDirtyHandle; 

        public void Init(string watchDir) {
            watcher = new FileSystemWatcher();
            watcher.Path = watchDir;  
            watcher.Filter = "*.cs";
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.EnableRaisingEvents = true;
            watcher.Changed += FileChanged;
        }

        public void TearDown() {
            if (watcher != null) {
                watcher.Changed -= FileChanged; 
            }
        }

        void FileChanged(object sender, FileSystemEventArgs e) {

            try {
                
                /* 等待 Unity 修复后再使用
                string dirName = Path.GetDirectoryName(e.FullPath);
                string fileName = Path.GetFileName(e.FullPath);
                string path = Path.Combine(dirName, fileName);
                int index = path.IndexOf("Assets");
                path = path.Substring(index);
                */

                // 替代实现方案
                string path = FindFileWithExt(watcher.Path, e.Name.Replace(".cs", ""), "*.cs");
                int index = path.IndexOf("Assets");
                path = path.Substring(index); 
                if (OnScriptDirtyHandle != null) {
                    OnScriptDirtyHandle.Invoke(path);
                } else {
                    Debug.LogWarning("OnScriptDirtyHandle 未注册");
                }

            } catch(Exception err) {  

                Debug.LogError(err.ToString());

            }

        }

        // 找到某个文件
        static string FindFileWithExt(string rootPath, string fileName, string ext) {
            List<string> all = FindAllFileWithExt(rootPath, ext);
            return all.Find(value => value.Contains(fileName + ext.TrimStart('*')));
        }

        // 递归
        /// <summary>
        /// ext 参数格式举例："*.cs"
        /// </summary>
        static List<string> FindAllFileWithExt(string rootPath, string ext) {

            List<string> fileList = new List<string>();

            DirectoryInfo directoryInfo = new DirectoryInfo(rootPath);
            FileInfo[] allFiles = directoryInfo.GetFiles(ext);
            for (int i = 0; i < allFiles.Length; i += 1) {
                var file = allFiles[i];
                fileList.Add(file.FullName);
            }

            DirectoryInfo[] childrenDirs = directoryInfo.GetDirectories();
            for (int i = 0; i < childrenDirs.Length; i += 1) {
                var dir = childrenDirs[i];
                fileList.AddRange(FindAllFileWithExt(dir.FullName, ext));
            }

            return fileList;

        }

    }

}
#endif
#endif