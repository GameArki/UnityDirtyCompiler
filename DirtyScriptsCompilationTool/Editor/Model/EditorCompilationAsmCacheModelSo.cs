using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Compilation;

namespace FinalFrame.EditorTool {

    // ---- 用于存储程序集 ----
    [Serializable]
    public class EditorCompilationAsmCacheModelSo : ScriptableObject {

        [SerializeField]
        SerializableDictionary<string, Assembly> pathToAsmDic;

        [SerializeField]
        List<string> dirtyFiles;

        public EditorCompilationAsmCacheModelSo() {

            if (pathToAsmDic == null) {
                pathToAsmDic = new SerializableDictionary<string, Assembly>();
            }

            if (dirtyFiles == null) {
                dirtyFiles = new List<string>();
            }

        }

        public void Init() {
            Assembly[] asmArr = CompilationPipeline.GetAssemblies();
            foreach (var asm in asmArr) {
                CacheAsm(asm);
            }
        }

        public void TearDown() {
            pathToAsmDic.Clear();
            dirtyFiles.Clear();
        }

        void CacheAsm(Assembly asm) {
            // file is Full Path
            foreach (var file in asm.sourceFiles) {
                string dirName = Path.GetDirectoryName(file);
                string fileName = Path.GetFileName(file);
                string path = Path.Combine(dirName, fileName);
                pathToAsmDic.Add(path, asm);
            }
        }

        public void SetDirty(string file) {
            int index = dirtyFiles.IndexOf(file);
            if (index == -1) {
                dirtyFiles.Add(file);
            }
        }

        public List<string> GetDirtyScripts() {
            return dirtyFiles;
        }

        public void CleanDirtyScripts() {
            dirtyFiles.Clear();
        }

        public Assembly GetAssemblyWithPath(string path) {
            
            pathToAsmDic.TryGetValue(path, out Assembly asm);
            // Debug.Log($"尝试获取文件{path}的程序集, {path.GetHashCode()}");
            return asm;
        }

    }

}