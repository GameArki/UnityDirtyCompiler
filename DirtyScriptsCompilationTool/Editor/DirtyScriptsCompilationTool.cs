using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace FinalFrame.EditorTool {

    [InitializeOnLoad]
    public class DirtyScriptsCompilationTool {

        static EditorCompilationAsmCacheModelSo asmCacheModel;
        static volatile Queue<Action> dirtyHandleQueue = new Queue<Action>();

        static DirtyScriptsCompilationTool() {
            Reset();
            EditorApplication.update += Execute;
        }

        [MenuItem("Tools/DirtyScriptsCompilation/Reset")]
        static void Reset() {

            // 清空
            if (asmCacheModel != null) {
                asmCacheModel.TearDown();
                asmCacheModel = null;
            }

            var models = Resources.FindObjectsOfTypeAll<EditorCompilationAsmCacheModelSo>();
            foreach (var model in models) {
                GameObject.DestroyImmediate(model);
            }

            EditorCompilationFileWatcher.TearDown();

            // 初始化
            EditorCompilationFileWatcher.Init(Application.dataPath);
            EditorCompilationFileWatcher.OnScriptDirtyHandle = (path) => dirtyHandleQueue.Enqueue(() => OnScriptDirty(path));

            UnityEngine.Debug.Log("DirtyScriptsCompilationTool 初始化完成");

        }

        static void Execute() {

            if (dirtyHandleQueue == null) {
                return;
            }

            while (dirtyHandleQueue.Count > 0) {
                Action action = dirtyHandleQueue.Dequeue();
                if (action != null) {
                    action.Invoke();
                }
            }

        }

        static void OnScriptDirty(string scriptFilePath) {
            EditorCompilationAsmCacheModelSo cacheModelSo = GetOrCreateAsmCacheModelSo();
            cacheModelSo.SetDirty(scriptFilePath);
        }

        static EditorCompilationAsmCacheModelSo GetOrCreateAsmCacheModelSo() {
            try {
                if (asmCacheModel == null) {
                    EditorCompilationAsmCacheModelSo[] models = Resources.FindObjectsOfTypeAll<EditorCompilationAsmCacheModelSo>();
                    if (models != null) {
                        asmCacheModel = models.FirstOrDefault();
                    }
                    if (asmCacheModel == null) {
                        asmCacheModel = ScriptableObject.CreateInstance<EditorCompilationAsmCacheModelSo>();
                    } else {
                        // UnityEngine.Debug.Log("找到已存在 So");
                    }
                    asmCacheModel.hideFlags = HideFlags.DontSave;
                    asmCacheModel.Init();
                }
                return asmCacheModel;
            } catch {
                throw;
            }
        }

        [MenuItem("Tools/DirtyScriptsCompilation/Compile %_T")]
        static void CompileDirtyScripts() {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Type compilationPipeline = typeof(CompilationPipeline);

            // 找到事件
            EventInfo eventInfo = compilationPipeline.GetEvent("assemblyCompilationFinished", BindingFlags.Static | BindingFlags.Public);

            // 根据事件找到触发方法
            Type handlerType = eventInfo.EventHandlerType;
            MethodInfo invokeMethod = handlerType.GetMethod("Invoke");

            // 找到触发方法所需要的实例
            object invoker = null;
            MemberInfo[] memberInfos = compilationPipeline.GetMembers(BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var mem in memberInfos) {
                if (mem.Name == "assemblyCompilationFinished") {
                    FieldInfo fieldInfo = mem as FieldInfo;
                    invoker = fieldInfo.GetValue(null);
                }
            }

            var model = GetOrCreateAsmCacheModelSo();

            List<string> dirties = model.GetDirtyScripts();

            // 编译
            foreach (var path in dirties) {

                var asm = model.GetAssemblyWithPath(path);

                // 编译 asm
                EditorUtility.CompileCSharp(asm.sourceFiles, asm.allReferences, asm.defines, asm.outputPath);
                UnityEngine.Debug.Log($"编译 asm: {asm.name}, sourceFilesCount: {asm.sourceFiles.Length.ToString()}, refsCount: {asm.allReferences.Length.ToString()}, outPath: {asm.outputPath}");

                invokeMethod.Invoke(invoker, new object[] { asm.outputPath, new CompilerMessage[0] });

            }

            stopwatch.Stop();

            // 无脚本
            if (dirties.Count == 0) {
                UnityEngine.Debug.Log($"不存在脏脚本, 无需编译, 耗时: {stopwatch.ElapsedMilliseconds.ToString()}");
            } else {
                UnityEngine.Debug.Log($"合计编译 DirtyScripts 数量: {dirties.Count}, 耗时: {stopwatch.ElapsedMilliseconds.ToString()}");
            }

            // 清空 DirtyScripts
            dirtyHandleQueue.Clear();
            model.CleanDirtyScripts();

        }

    }

}