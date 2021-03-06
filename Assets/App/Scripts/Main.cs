﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRatel.Win32;
using System.IO;
using UnityEngine.UI;

namespace NRatel.TextureUnpacker
{
    public class Main : MonoBehaviour
    {
        public enum UnpackMode
        {
            JustSplit = 0,
            Restore = 1,
        }
        private UnpackMode currentUnpackMode;

        static public Main main;
        private string plistFilePath = "";
        private string pngFilePath = "";

        private bool isExecuting = false;

        private AppUI appUI;

        private Loader loader;
        private Plist plist;
        private Texture2D bigTexture;

        private ImageSpliter imageSpliter;

        private void Start()
        {
            Screen.SetResolution(800, 600, false);
            main = this;
            appUI = GetComponent<AppUI>();
            currentUnpackMode = (UnpackMode)appUI.m_Dropdown_SelectMode.value;

#if UNITY_EDITOR
            //测试用
            plistFilePath = @"C:\Users\Administrator\Desktop\999.plist";
            pngFilePath = @"C:\Users\Administrator\Desktop\999.png";
            StartCoroutine(LoadFiles());
#endif

            RegisterEvents();
        }

        private void RegisterEvents()
        {
            GetComponent<FilesOrFolderDragInto>().AddEventListener((List<string> aPathNames) =>
            {
                if (isExecuting)
                {
                    appUI.SetTip("正在执行，请等待结束");
                    return;
                }

                if (aPathNames.Count > 1)
                {
                    appUI.SetTip("只可拖入一个文件");
                    return;
                }
                else
                {
                    string path = aPathNames[0];
                    if (path.EndsWith(".plist"))
                    {
                        plistFilePath = path;
                        pngFilePath = Path.GetDirectoryName(path) + @"\" + Path.GetFileNameWithoutExtension(path) + ".png";
                        if (!File.Exists(pngFilePath))
                        {
                            appUI.SetTip("不存在与当前plist文件同名的png文件");
                            return;
                        }
                    }
                    else if (path.EndsWith(".png"))
                    {
                        pngFilePath = path;
                        plistFilePath = Path.GetDirectoryName(path) + @"\" + Path.GetFileNameWithoutExtension(path) + ".plist";
                        if (!File.Exists(plistFilePath))
                        {
                            appUI.SetTip("不存在与当前png文件同名的plist文件");
                            return;
                        }
                    }
                    else
                    {
                        appUI.SetTip("请放入plist 或 png 文件");
                        return;
                    }
                    StartCoroutine(LoadFiles());
                }
            });

            appUI.m_Btn_Excute.onClick.AddListener(() =>
            {
                if (isExecuting)
                {
                    appUI.SetTip("正在执行，请等待结束");
                    return;
                }

                if (loader == null || plist == null)
                {
                    appUI.SetTip("没有指定可执行的plist&png");
                    return;
                }

                isExecuting = true;
                imageSpliter = new ImageSpliter(this);
                StartCoroutine(Unpack());
            });

            appUI.m_Dropdown_SelectMode.onValueChanged.AddListener((value) =>
            {
                currentUnpackMode = (UnpackMode)value;
            });
        }
        
        private IEnumerator LoadFiles()
        {
            loader = Loader.LookingForLoader(plistFilePath);
            if (loader != null)
            {
                appUI.SetTip("可处理! \n【" + loader.GetType().Name.ToString().Replace("Loader_", "") + "】", false);
                plist = loader.LoadPlist(plistFilePath);
                bigTexture = loader.LoadTexture(pngFilePath, plist.metadata);
                appUI.SetImage(bigTexture);
            }
            else
            {
                appUI.SetTip("无法识别的plist类型，请联系作者");
            }
            yield return null;
        }

        private IEnumerator Unpack()
        {
            int total = plist.frames.Count;
            int count = 0;
            foreach (var frame in plist.frames)
            {
                if (currentUnpackMode == UnpackMode.JustSplit)
                {
                    imageSpliter.JustSplit(bigTexture, frame);
                }
                else if (currentUnpackMode == UnpackMode.Restore)
                {
                    imageSpliter.Restore(bigTexture, frame);
                }
                count += 1;
                appUI.SetTip("进度：" + count + "/" + total, false);
                yield return null;
            }
            isExecuting = false;
        }

        public string GetSaveDir()
        {
            return Path.GetDirectoryName(Main.main.plistFilePath) + @"\NRatel_" + Path.GetFileNameWithoutExtension(Main.main.plistFilePath);
        }
    }
}

