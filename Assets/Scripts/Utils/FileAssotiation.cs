using CometUI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

class FileAssotiation : MonoBehaviour
{
    private void Update()
    {
        ProcessCommandLine();
        Destroy(this);
    }

    private void ProcessCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        try
        {
            for (int i = 1; i < args.Length; i++)
            {
                string ext = Path.GetExtension(args[i]).ToLower();
                var path = args[i];
                switch (ext)
                {
                    //case ".scene": SaverLoader.LoadScene(path); break;
                    //case ".cockpit": SaverLoader.LoadCockpitScene(path); break;
                    //case ".dxf": ExportCloudController.Instance.ImportDXF(path); break;
                }
            }
        }
        catch (Exception ex)
        {
            UIManager.ShowDialog(null, ex.Message, "Ok");
            Debug.LogException(ex);
        }
    }


#if WW
    void Start()
    {
        String App_Exe = Application.productName + ".exe";
        String App_Path = Path.Combine(Application.dataPath, @"..\" + App_Exe);// "%localappdata%";
        Debug.Log(App_Exe);
        Debug.Log(App_Path);
        SetAssociation_User("scene", App_Path, App_Exe);
    }

    public static void SetAssociation_User(string Extension, string OpenWith, string ExecutableName)
    {
        try
        {
            RegistryKey ApplicationAssociationToasts = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ApplicationAssociationToasts\\", true);

            using (RegistryKey User_Classes = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\", true))
            using (RegistryKey User_Ext = User_Classes.CreateSubKey("." + Extension))
            using (RegistryKey User_AutoFile = User_Classes.CreateSubKey(Extension + "_auto_file"))
            using (RegistryKey User_AutoFile_Command = User_AutoFile.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command"))
            using (RegistryKey User_Classes_Applications = User_Classes.CreateSubKey("Applications"))
            using (RegistryKey User_Classes_Applications_Exe = User_Classes_Applications.CreateSubKey(ExecutableName))
            using (RegistryKey User_Application_Command = User_Classes_Applications_Exe.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command"))
            using (RegistryKey User_Explorer = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\." + Extension))
            using (RegistryKey User_Choice = User_Explorer.OpenSubKey("UserChoice"))
            {
                User_Ext.SetValue("", Extension + "_auto_file", RegistryValueKind.String);
                User_Classes.SetValue("", Extension + "_auto_file", RegistryValueKind.String);
                User_Classes.CreateSubKey(Extension + "_auto_file");
                User_AutoFile_Command.SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
                if (ApplicationAssociationToasts == null)
                    ApplicationAssociationToasts = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ApplicationAssociationToasts\\", true);

                ApplicationAssociationToasts.SetValue(Extension + "_auto_file_." + Extension, 0);
                ApplicationAssociationToasts.SetValue(@"Applications\" + ExecutableName + "_." + Extension, 0);
                User_Application_Command.SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
                User_Explorer.CreateSubKey("OpenWithList").SetValue("a", ExecutableName);
                User_Explorer.CreateSubKey("OpenWithProgids").SetValue(Extension + "_auto_file", "0");
                if (User_Choice != null) User_Explorer.DeleteSubKey("UserChoice");
                User_Explorer.CreateSubKey("UserChoice").SetValue("ProgId", @"Applications\" + ExecutableName);
            }
            ApplicationAssociationToasts.Dispose();
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception excpt)
        {
            //Your code here
            Debug.LogException(excpt);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
#endif
}
