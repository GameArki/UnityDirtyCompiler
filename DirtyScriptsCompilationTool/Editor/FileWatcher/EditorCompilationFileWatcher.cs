using System;
using System.IO;
using UnityEngine;
using UnityEditor.Compilation;

namespace FinalFrame.EditorTool {

    // ---- 用于监控文件变化 ----
    [Serializable]
    public class EditorCompilationFileWatcher {

        static FileSystemWatcher watcher;

        public delegate void OnScriptDirty(string scriptFilePath);
        public static OnScriptDirty OnScriptDirtyHandle;

        public static void Init(string watchDir) {
            watcher = new FileSystemWatcher();
            watcher.Path = watchDir;
            watcher.Filter = "*.cs";
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.EnableRaisingEvents = true;
            watcher.Changed += FileChanged;
        }

        public static void TearDown() {
            if (watcher != null) {
                watcher.Changed -= FileChanged;
            }
        }

        static void FileChanged(object sender, FileSystemEventArgs e) {

            try {
                string dirName = Path.GetDirectoryName(e.FullPath);
                string fileName = Path.GetFileName(e.FullPath);
                string path = Path.Combine(dirName, fileName);
                int index = path.IndexOf("Assets");
                path = path.Substring(index);

                if (OnScriptDirtyHandle != null) {
                    OnScriptDirtyHandle.Invoke(path);
                } else {
                    Debug.Log("OnScriptDirtyHandle 未注册");
                }
            } catch(Exception err) {
                Debug.LogError(err.ToString());
            }

            // Assembly changedAsm = asmCacheModel.GetAssemblyWithPath(path);
            // Debug.Log($"程序集改变: " + changedAsm.name);
            // Debug.Log($"文件改变, Name: {e.Name}, Path: {e.FullPath}, ChangeType: {e.ChangeType.ToString()}");
        }

    }

}