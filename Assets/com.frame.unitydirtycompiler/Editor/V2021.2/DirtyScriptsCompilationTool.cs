#if UNITY_EDITOR
#if UNITY_2021_2
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace FinalFrame.EditorTool {

    // 监控修改的代码文件
    // 编译Runtime DLL
    public class DirtyScriptsCompilationTool {

        const string SHORTCUT_KEY = "%_T";

        static System.Diagnostics.Stopwatch sw;

        static List<string> dirtyFiles;

        [InitializeOnLoadMethod]
        static void Setup() {

            dirtyFiles = new List<string>();

            EditorCompilationFileWatcher watcher = new EditorCompilationFileWatcher();
            watcher.Init(Application.dataPath);
            watcher.OnScriptDirtyHandle += OnScriptDirty;

        }

        static void OnScriptDirty(string dirtyScriptFilePath) {
            if (!dirtyFiles.Contains(dirtyScriptFilePath)) {
                dirtyFiles.Add(dirtyScriptFilePath);
            }
        }

        [MenuItem(MENU_ITEM_COLLECTION.EDITOR_TOOL_PATH + nameof(DirtyScriptsCompilationTool) + "/脏脚本编译 " + SHORTCUT_KEY)]
        public static void Compile() {

            sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // 1. 缓存所有修改且未编译的代码文件
            // this.dirtyFiles

            // 2. 获取所有Runtime程序集
            var asms = CompilationPipeline.GetAssemblies();
            var pathToAsmDic = new Dictionary<string, Assembly>();
            for (int i = 0; i < asms.Length; i += 1) {
                var asm = asms[i];
                for (int j = 0; j < asm.sourceFiles.Length; j += 1) {
                    var file = asm.sourceFiles[j];
                    string dirName = Path.GetDirectoryName(file);
                    string fileName = Path.GetFileName(file);
                    string path = Path.Combine(dirName, fileName);
                    pathToAsmDic.Add(path, asm);
                }
            }

            // 3. 查找所有有改动的程序集
            List<Assembly> dirtyAsms = new List<Assembly>();
            var arr = dirtyFiles.ToArray();
            for (int i = 0; i < arr.Length; i += 1) {
                var file = arr[i];
                bool hasAsm = pathToAsmDic.TryGetValue(file, out Assembly tarAsm);
                if (!hasAsm) {
                    Debug.LogWarning($"该文件 {file} 暂不存在于程序集内");
                } else {
                    if (!dirtyAsms.Contains(tarAsm)) {
                        dirtyAsms.Add(tarAsm);
                    }
                }
            }

            // 4. 编译
            int waitCompileCount = dirtyAsms.Count;
            if (waitCompileCount == 0) {
                Debug.Log("无程序集需要编译");
                return;
            }

            RecurCompile(dirtyAsms, waitCompileCount);

        }

        static void RecurCompile(List<Assembly> dirtyAsms, int waitCompileCount, int index = 0) {

            if (index < dirtyAsms.Count) {

                var asm = dirtyAsms[index];

                AssemblyBuilder builder = new AssemblyBuilder(asm.outputPath, asm.sourceFiles);
                builder.additionalDefines = asm.defines;
                builder.compilerOptions = asm.compilerOptions;
                builder.excludeReferences = new string[] { asm.outputPath };
                builder.additionalReferences = asm.compiledAssemblyReferences;

                builder.buildFinished += (dllFilePath, arr) => {

                    bool hasErr = false;
                    foreach (var msg in arr) {
                        if (msg.type == CompilerMessageType.Error) {
                            Debug.LogError(msg.message);
                            hasErr = true;
                        } else if (msg.type == CompilerMessageType.Warning) {
                            Debug.LogWarning(msg.message);
                        } else {
                            Debug.Log(msg.message);
                        }
                    }
                    if (hasErr) {
                        Debug.LogError($"程序集{dllFilePath}编译失败");
                        return;
                    }

                    waitCompileCount -= 1;
                    Debug.Log($"程序集{dllFilePath}编译成功");
                    if (waitCompileCount == 0) {
                        dirtyFiles.Clear();
                        sw.Stop();
                        Debug.Log($"[DSC编译完成]总程序集数:{dirtyAsms.Count}, 耗时:{sw.ElapsedMilliseconds}ms");
                    } else {
                        index += 1;
                        RecurCompile(dirtyAsms, waitCompileCount, index);
                    }

                };

                builder.Build();

            }
        }

    }

}

#endif
#endif