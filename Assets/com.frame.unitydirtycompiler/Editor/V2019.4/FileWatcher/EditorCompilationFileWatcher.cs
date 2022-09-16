#if UNITY_EDITOR
#if UNITY_2019_4
using System;
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
                
                string dirName = Path.GetDirectoryName(e.FullPath);
                string fileName = Path.GetFileName(e.FullPath);
                string path = Path.Combine(dirName, fileName);
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

    }

}
#endif
#endif