using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetStoreTools
{
    public static class MainAssetsUtil
    {
        private static IAssetBundler Bundler
        {
            get
            {
                if (MainAssetsUtil.s_Bundler == null)
                {
                    MainAssetsUtil.s_Bundler = AssetBundlerFactory.GetBundler();
                }
                return MainAssetsUtil.s_Bundler;
            }
        }

        public static bool CanGenerateBundles
        {
            get
            {
                return MainAssetsUtil.Bundler.CanGenerateBundles();
            }
        }

        public static bool CanPreview
        {
            get
            {
                return MainAssetsUtil.Bundler.CanPreview();
            }
        }

        public static void ShowManager(string rootPath, List<string> mainAssets, FileSelector.DoneCallback onFinishChange)
        {
            FileSelector.Show(rootPath, mainAssets, onFinishChange);
        }

        public static string CreateBundle(string mainAssetPath)
        {
            if (!MainAssetsUtil.CanGenerateBundles)
            {
                DebugUtils.LogWarning("This version os Unity cannot generate Previews");
                return null;
            }
            string text = "Temp/AssetBundle_" + mainAssetPath.Trim(new char[]
            {
            '/'
            }).Replace('/', '_') + ".unity3d";
            UnityEngine.Object @object = AssetDatabase.LoadMainAssetAtPath(mainAssetPath);
            if (@object == null)
            {
                DebugUtils.LogWarning(string.Format("Unable to find asset at: {0}", mainAssetPath));
                return null;
            }
            Module module = @object.GetType().Module;
            Type type = module.GetType("UnityEditor.SubstanceArchive");
            module = Vector3.zero.GetType().Module;
            Type type2 = module.GetType("UnityEngine.ProceduralMaterial");
            if (type != null && type2 != null && type.IsInstanceOfType(@object))
            {
                Object[] array = AssetDatabase.LoadAllAssetsAtPath(mainAssetPath);
                foreach (Object object2 in array)
                {
                    if (type2.IsInstanceOfType(object2))
                    {
                        @object = object2;
                        break;
                    }
                }
            }
            if (@object == null)
            {
                DebugUtils.LogWarning("Unable to find the Asset");
            }
            bool flag = MainAssetsUtil.Bundler.CreateBundle(@object, text);
            if (flag)
            {
                DebugUtils.Log("bundleResut true");
            }
            else
            {
                DebugUtils.Log("bundleResut false");
            }
            if (!flag)
            {
                return null;
            }
            return text;
        }

        public static void Preview(string assetpath)
        {
            MainAssetsUtil.Bundler.Preview(assetpath);
        }

        public static List<string> GetMainAssetsByTag(string folder)
        {
            List<string> list = new List<string>();
            string[] files = Directory.GetFiles(Application.dataPath + folder);
            foreach (string text in files)
            {
                bool flag = false;
                string text2 = text.Substring(Application.dataPath.Length + 1);
                text2 = text2.Replace("\\", "/");
                text2 = "Assets/" + text2;
                Regex regex = new Regex(".*[/][.][^/]*");
                if (!regex.Match(text2).Success)
                {
                    UnityEngine.Object @object = AssetDatabase.LoadMainAssetAtPath(text2);
                    if (!(@object == null))
                    {
                        string[] labels = AssetDatabase.GetLabels(@object);
                        foreach (string a in labels)
                        {
                            if (a == "MainAsset")
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            list.Add(text2);
                        }
                    }
                }
            }
            string[] directories = Directory.GetDirectories(Application.dataPath + folder);
            foreach (string text3 in directories)
            {
                string folder2 = text3.Substring(Application.dataPath.Length);
                List<string> mainAssetsByTag = MainAssetsUtil.GetMainAssetsByTag(folder2);
                list.AddRange(mainAssetsByTag);
            }
            return list;
        }

        private static IAssetBundler s_Bundler;
    }

}