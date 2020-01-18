﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace LPLauncher
{
    public partial class Form1 : Form
    {
        private List<CMod> m_ModsAvailable = new List<CMod>();
        private List<CMod> m_ModsInstalled = new List<CMod>();

        private const string MOD_SUBFOLDER = @"LifePlay\Content\Modules\";
        private const string DISABLED_MOD_SUBFOLDER = @"LifePlay\Content\Modules\";
        private const string BASE_REPO_URL = "https://raw.githubusercontent.com/NickNo-dev/LP-Mods/master/";
        
        private string m_sPath = "";


        public Form1()
        {
            InitializeComponent();

            int cnt = 0;
            FileInfo fi = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            m_sPath = fi.DirectoryName;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                switch(cnt++)
                {
                    case 1: // argument 0 is alternative installation path
                        m_sPath = arg;
                        m_sPath = m_sPath.Trim('"');
                        break;
                }
            }  
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateRepo();
            GetInstalledMods();

            RenderAvailModList();
            RenderInstModList();
        }

        private void RenderInstModList()
        {
            lbInst.Items.Clear();
            foreach (CMod mod in m_ModsInstalled)
            {
                int idx = lbInst.Items.Add(mod);
            }
        }

        private void RenderAvailModList()
        {
            lbAvail.Items.Clear();
            foreach (CMod mod in m_ModsAvailable)
            {
                int idx = lbAvail.Items.Add(mod);
            }            
        }

        private void GetInstalledMods()
        {
            m_ModsInstalled.Clear();

            try
            {
                if (!m_sPath.EndsWith("\\"))
                    m_sPath += "\\";
                 
                DirectoryInfo directory = new DirectoryInfo(m_sPath + MOD_SUBFOLDER);
                DirectoryInfo[] directories = directory.GetDirectories();

                foreach (DirectoryInfo folder in directories)
                {
                    // Read enabled mods
                    try
                    {
                        FileInfo modFileInfo = folder.GetFiles("*.lpmod")[0];

                        string modFile = folder.Name;
                        string modPath = modFileInfo.DirectoryName;

                        CMod instMod = new CMod(modFile, modPath);
                        instMod.setFileName(modFileInfo.FullName);
                        instMod.readModInfo();
                        m_ModsInstalled.Add(instMod);
                    }
                    catch(Exception) {}

                    // Read disabled mods
                    try
                    {
                        FileInfo modFileInfo = folder.GetFiles("*.disabled")[0];

                        string modFile = folder.Name;
                        string modPath = modFileInfo.DirectoryName;

                        CMod instMod = new CMod(modFile, modPath);
                        instMod.setFileName(modFileInfo.FullName);
                        instMod.readModInfo();
                        m_ModsInstalled.Add(instMod);
                    }
                    catch(Exception) { }
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void UpdateRepo()
        {
            bool canContinue = true;

            m_ModsAvailable.Clear();

            try
            {
                using (WebClient c = new WebClient())
                {
                    c.DownloadFile(BASE_REPO_URL + "repo.xml", "lprepo.xml");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Cannot download mod repository. Check your connection and/or if a newer version of the mod manager is avalilable.", "Ooops!");
                canContinue = false;
            }

            XmlDocument repo = new XmlDocument();
            if(canContinue)
            {
                try
                {
                    repo.Load("lprepo.xml");
                    XmlNode root = repo.DocumentElement.SelectSingleNode("/Repo");
                    foreach (XmlNode modXml in root.ChildNodes)
                    {
                        string name = modXml.Attributes["Name"].InnerText;
                        string ver = modXml.Attributes["Version"].InnerText;
                        string id = modXml.Attributes["Id"].InnerText;
                        string path = BASE_REPO_URL + modXml.Attributes["Path"].InnerText;

                        // Test for optional message
                        string devMsg = null;
                        if( modXml.Attributes.GetNamedItem("Msg") != null )
                            devMsg = modXml.Attributes["Msg"].InnerText;

                        CMod curMod = new CMod(id, name, ver, path);
                        curMod.setDevMessage(devMsg);
                        m_ModsAvailable.Add(curMod);
                        
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(this, "The repo could not be downloaded. Please check if a newer version of the mod manager is avalilable.", "Ooops!");
                    canContinue = false;
                }
            }

            if (File.Exists("lprepo.xml"))
                File.Delete("lprepo.xml");

        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            Process.Start(m_sPath + "\\lifeplay.exe");
        }

        private void lbInst_DoubleClick(object sender, EventArgs e)
        {
            if (lbInst.SelectedItem != null)
            {
                CMod selMod = (CMod)lbInst.SelectedItem;

                if (!selMod.isBaseMod())
                {
                    selMod.toggleActive();

                    lbInst.Items.Remove(selMod);
                    lbInst.Items.Add(selMod);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnDelMod_Click(object sender, EventArgs e)
        {
            if (lbInst.SelectedItem != null)
            {
                DialogResult dr = MessageBox.Show("Are you sure you want to delete the mod?","Delete mod",MessageBoxButtons.YesNo);
                if (dr == System.Windows.Forms.DialogResult.Yes)
                {
                    CMod mod = (CMod)lbInst.SelectedItem;
                    if (!mod.isBaseMod())
                    {
                        bool success = mod.delete();

                        if (success)
                        {
                            lbInst.Items.Remove(mod);
                            m_ModsInstalled.Remove(mod);
                        }
                    }
                }
            }
        }

        private void lbInst_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbInst.SelectedItem != null)
            {
                CMod mod = (CMod)lbInst.SelectedItem;

                if (!mod.isBaseMod())
                    btnDelMod.Enabled = true;
                else
                    btnDelMod.Enabled = false;
            }
            else
                btnDelMod.Enabled = false;
        }

        private void lbAvail_DoubleClick(object sender, EventArgs e)
        {
            if (lbAvail.SelectedItem != null)
            {
                CMod mod = (CMod)lbAvail.SelectedItem;
                
                InstallMod(mod, true);
                
                GetInstalledMods();
                RenderInstModList();
            }
        }

        private void InstallMod(CMod mod, bool askToReplace = false)
        {
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.DownloadFile(mod.getFileName(), "lpmgr.tmp");

                    if (Directory.Exists("mmTemp"))
                        Directory.Delete("mmTemp", true);

                    Directory.CreateDirectory("mmTemp");

                    System.IO.Compression.ZipFile.ExtractToDirectory("lpmgr.tmp", "mmTemp");

                    string[] subFolders = Directory.GetDirectories("mmTemp");
                    if (subFolders.Length == 1)    // I do not want to spread junk, mods need to keep to 1 subfolder
                    {
                        string name = Path.GetFileName(subFolders[0]);

                        // Test if already installed
                        bool install = true;
                        string dest = m_sPath + MOD_SUBFOLDER + name;
                        if (Directory.Exists(dest))
                        {
                            DialogResult dr = System.Windows.Forms.DialogResult.Yes;
                            if(askToReplace)
                            {
                                dr = MessageBox.Show("Replace existing mod?", "Already installed!", MessageBoxButtons.YesNo);
                            }

                            if (dr == System.Windows.Forms.DialogResult.Yes)
                                Directory.Delete(dest, true);
                            else
                                install = false;
                        }

                        if (install)
                        {
                            Directory.Move(subFolders[0], dest);

                            if( mod.getDevMessage() != null )
                            {
                                MessageBox.Show(mod.getDevMessage(), "Message from mod " + mod.getName());
                            }
                        }

                    }

                    if (Directory.Exists("mmTemp"))
                        Directory.Delete("mmTemp", true);

                    File.Delete("lpmgr.tmp");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem during installation:\n" + ex.Message, "Ooops...");
                }
            }
        }

        private void btnUpdateAll_Click(object sender, EventArgs e)
        {
            // Compare avail with installed
            List<CMod> modsToUpdate = new List<CMod>();
            string sModsToUpdate = "";

            foreach( CMod modAvail in m_ModsAvailable )
            {
                CMod found = findInstalledModWithId(modAvail.getId());
                if( found != null )
                {
                    if( found.getVersion() != modAvail.getVersion() )
                    {
                        // Looks like changed
                        modsToUpdate.Add(modAvail);
                        sModsToUpdate += modAvail.ToString() + "\n";
                    }
                }
            }

            if (modsToUpdate.Count > 0)
            {
                DialogResult dr = MessageBox.Show("The following updates are available:\n\n"+sModsToUpdate +"\nInstall them now?", "Updates found", MessageBoxButtons.YesNo);
                if( dr == System.Windows.Forms.DialogResult.Yes )
                {
                    foreach(CMod instMod in modsToUpdate)
                    {
                        InstallMod(instMod);
                    }

                    UpdateRepo();
                    GetInstalledMods();

                    RenderAvailModList();
                    RenderInstModList();
                }
            }
            else
                MessageBox.Show("No updates found...");

        }

        private CMod findInstalledModWithId(string id)
        {
            // TODO: If LP sometime has 1000+ mods we could switch this to a dictonary with key = id
            foreach (CMod modInst in m_ModsAvailable)
            {
                if (modInst.getId() == id)
                    return modInst;
            }
            return null;
        }
    }
}
