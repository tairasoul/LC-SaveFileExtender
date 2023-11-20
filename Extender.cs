using MelonLoader;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;

[assembly: MelonInfo(typeof(SaveFileExtender_LC.SaveFileExtensions), "LC.MoreSaveFiles", "1.0.0", "tairasoul")]
[assembly: MelonGame("ZeekerssRBLX", "Lethal Company")]

namespace SaveFileExtender_LC
{
    public class SaveFileExtensions: MelonMod
    {
        public static MelonLogger.Instance Logger;
        public static GameObject[] CustomSaveFiles = Array.Empty<GameObject>();
        [HarmonyPatch(typeof(MenuManager), "Start")]
        class MenuPatch
        {
            [HarmonyFinalizer]
            public static void Finalizer(MenuManager __instance)
            {
                for (int i = 3; i < 9; i++)
                {
                    string filePath = $"LCSaveFile{i}";
                    if (ES3.FileExists(filePath))
                    {
                        try
                        {
                            if (ES3.Load<int>("FileGameVers", filePath, 0) < GameNetworkManager.Instance.compatibleFileCutoffVersion)
                            {
                                Debug.Log(string.Format("file vers: {0} not compatible; {1}", ES3.Load<int>("FileGameVers", filePath, 0), GameNetworkManager.Instance.compatibleFileCutoffVersion));
                                __instance.filesCompatible[i] = false;
                            }
                        }
                        catch (Exception arg)
                        {
                            Debug.LogError(string.Format("Error loading file #{0}! Deleting file since it's likely corrupted. Error: {1}", i, arg));
                            ES3.DeleteFile(filePath);
                        }
                    }
                }
            }
        }
        /*
        class SaveFiles
        {

            GameObject files;
            public SaveFiles(GameObject FilesPanel)
            {
                files = FilesPanel;
            }
        }*/
        Rect window = new Rect(1270, 745, 600, 100);
        bool isInMenu = false;
        bool isInHostMenu = false;
        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;
            LoggerInstance.Msg("Patching MenuManager.");
            HarmonyInstance.PatchAll();
            base.OnInitializeMelon();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            isInMenu = sceneName == "MainMenu";
            base.OnSceneWasLoaded(buildIndex, sceneName);
        }

        GameObject FilesPanel;
        GameObject File1;

        public override void OnUpdate()
        {
            if (isInMenu)
            {
                try
                {
                    MenuManager manager = GameObject.FindFirstObjectByType<MenuManager>();
                    isInHostMenu = manager.HostSettingsScreen.activeSelf;
                    if (manager != null && GameObject.Find("LobbyHostSettings") != null && GameObject.Find("LobbyHostSettings").activeSelf)
                    {
                        try
                        {
                            FilesPanel = GameObject.Find("FilesPanel");
                            File1 = FilesPanel.transform.Find("File1").gameObject;
                        }
                        catch { }
                    }
                }
                catch { }
            }
            try
            {
                if (GameObject.Find("LobbyHostSettings") != null && GameObject.Find("LobbyHostSettings").activeSelf && FilesPanel != null && File1 != null)
                for (int i = 4; i < 9; i++)
                {
                    string filePath = $"LCSaveFile{i}";
                    string ChildName = $"File{i}";
                    if (FilesPanel.transform.Find(ChildName) == null)
                    {
                        Logger.Msg($"Creating file {ChildName}");
                        GameObject newFile = GameObject.Instantiate(File1);
                        newFile.transform.SetParent(FilesPanel.transform);
                        newFile.name = ChildName;
                        CustomSaveFiles = CustomSaveFiles.Append(newFile).ToArray();
                        SaveFileUISlot slot = newFile.GetComponent<SaveFileUISlot>();
                        slot.fileNum = i - 1;
                        FieldInfo fileStringField = typeof(SaveFileUISlot).GetField("fileString", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fileStringField != null)
                        {
                            Logger.Msg($"fileStringField isn't null, setting to LCSaveFile{i}");
                            fileStringField.SetValue(slot, filePath);
                        }
                        if (ES3.FileExists(filePath))
                        {
                            int num = ES3.Load<int>("GroupCredits", filePath, 30);
                            int num2 = ES3.Load<int>("Stats_DaysSpent", filePath, 0);
                            slot.fileStatsText.text = string.Format("${0}\nDays: {1}", num, num2);
                        }
                        else
                        {
                            slot.fileStatsText.text = "";
                        }
                        try
                        {
                            if (!GameObject.FindObjectOfType<MenuManager>().filesCompatible[slot.fileNum])
                            {
                                slot.fileNotCompatibleAlert.enabled = true;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            base.OnUpdate();
        }

        public override void OnGUI()
        {
            if (isInHostMenu) window = GUILayout.Window(1025, window, DrawWindow, "Extra Save Files");
        }

        public void DrawWindow(int windowId)
        {
            GUILayout.Label($"Currently selected slot: {GameNetworkManager.Instance.saveFileNum + 1}");
            foreach (GameObject CustomSaveFile in CustomSaveFiles)
            {
                SaveFileUISlot slot = CustomSaveFile.GetComponent<SaveFileUISlot>();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Save {slot.fileNum + 1}");
                GUILayout.Label($"Credits: {(slot.fileStatsText.text != "" ? slot.fileStatsText.text.Split("\n")[0] : "New save.")}");
                GUILayout.Label($"{(slot.fileStatsText.text != "" ? slot.fileStatsText.text.Split("\n")[1] : "Day 0")}");
                GUILayout.Label($"Compatible: {(slot.fileNotCompatibleAlert ? "Yes" : "No")}.");
                if (GUILayout.Button("Select slot."))
                {
                    slot.SetFileToThis();
                }
                GUILayout.EndHorizontal();
            }
            GUI.DragWindow(new Rect(0, 0, 50000, 50000));
        }
    }
}
