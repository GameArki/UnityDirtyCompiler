#if UNITY_EDITOR
#if UNITY_2019_4
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorAssembly = UnityEditor.Compilation.Assembly;

namespace FinalFrame.EditorTool {

    public class DirtyScriptsCompilationTool {

        const string SHOTCUT_KEY = "%_T";

        static System.Diagnostics.Stopwatch sw;

        static List<string> dirtyFiles;

        [InitializeOnLoadMethod]
        static void Setup() {

            dirtyFiles = new List<string>();

            EditorCompilationFileWatcher watcher = new EditorCompilationFileWatcher();
            watcher.Init(Application.dataPath);
            watcher.OnScriptDirtyHandle += OnScriptDirty;

        }

        static void OnScriptDirty(string scriptFilePath) {
            if (!dirtyFiles.Contains(scriptFilePath)) {
                dirtyFiles.Add(scriptFilePath);
            }
        }

        [MenuItem("Tools/DirtyScriptsCompilation/Compile " + SHOTCUT_KEY)]
        static void CompileDirtyScripts() {

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // 1. 缓存所有未编译的代码文件
            List<string> dirties = dirtyFiles;

            // 2. 获取所有Runtime程序集
            var asms = CompilationPipeline.GetAssemblies();
            var pathToAsmDic = new Dictionary<string, UnityEditorAssembly>();
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
            List<UnityEditorAssembly> dirtyAsms = new List<UnityEditorAssembly>();
            var arr = dirtyFiles.ToArray();
            for (int i = 0; i < arr.Length; i += 1) {
                var file = arr[i];
                bool hasAsm = pathToAsmDic.TryGetValue(file, out UnityEditorAssembly tarAsm);
                if (!hasAsm) {
                    Debug.LogWarning($"该文件 {file} 暂不存在于程序集内");
                } else {
                    if (!dirtyAsms.Contains(tarAsm)) {
                        dirtyAsms.Add(tarAsm);
                    }
                }
            }

            if (dirtyAsms.Count == 0) {
                Debug.Log("无程序集需要编译");
                return;
            }

            // 4. 编译
            // 找到触发编译完成的方法
            // 以及 out 方法参数 1
            MethodInfo invokeMethod = FindInvokerInEditorCompilation(out object invoker);

            // 方法参数 2
            Type type = System.Reflection.Assembly.Load("UnityEditor").GetType("UnityEditor.Scripting.Compilers.CompilerMessage");
            var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(type));

            foreach (var asm in dirtyAsms) {

                // 编译 asm
                EditorUtility.CompileCSharp(asm.sourceFiles, asm.allReferences, asm.defines, asm.outputPath);
                Debug.Log($"编译 asm: {asm.name}, sourceFilesCount: {asm.sourceFiles.Length.ToString()}, refsCount: {asm.allReferences.Length.ToString()}, outPath: {asm.outputPath}");

                invokeMethod.Invoke(invoker, new object[] { asm.outputPath, list });

            }

            stopwatch.Stop();

            // 无脚本
            if (dirties.Count == 0) {
                Debug.Log($"不存在脏脚本, 无需编译, 耗时: {stopwatch.ElapsedMilliseconds.ToString()}");
            } else {
                Debug.Log($"合计编译 DirtyScripts 数量: {dirties.Count}, 耗时: {stopwatch.ElapsedMilliseconds.ToString()}");
            }

            // 清空 DirtyScripts
            dirtyFiles.Clear();

        }

        static MethodInfo FindInvoker(out object invoker) {

            Type compilationPipeline = typeof(CompilationPipeline);

            // 找到事件
            EventInfo eventInfo = compilationPipeline.GetEvent("assemblyCompilationFinished", BindingFlags.Static | BindingFlags.Public);

            // 根据事件找到触发方法
            Type handlerType = eventInfo.EventHandlerType;
            MethodInfo invokeMethod = handlerType.GetMethod("Invoke");

            // 找到触发方法所需要的实例
            FieldInfo fi = compilationPipeline.GetField("assemblyCompilationFinished", BindingFlags.NonPublic | BindingFlags.Static);
            invoker = fi.GetValue(null);
            Debug.Log(invoker);

            return invokeMethod;

        }

        static MethodInfo FindInvokerInEditorCompilation(out object invoker) {

            Type[] allType = System.Reflection.Assembly.Load("UnityEditor").GetTypes();
            Type ecInterface = null;
            foreach (var type in allType) {
                if (type.Name.Contains("EditorCompilationInterface")) {
                    ecInterface = type;
                }
            }
            object ec = ecInterface.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);

            MethodInfo invokeMethod = ec.GetType().GetMethod("InvokeAssemblyCompilationFinished", BindingFlags.Public | BindingFlags.Instance);

            invoker = ec;

            return invokeMethod;

        }

    }

}
#endif
#endif