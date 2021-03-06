﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MD5FileSearch
{
        public partial class Form1 : Form
    {
        private string buildPath = "";
        private string[] searchPaths;
        private string dataMapPath = System.IO.Path.GetTempPath() + "MD5FileSearch.data";
        public BuildList buildListObj = new BuildList();
        private bool isTreeNodeSelected = false;
        private bool isBuildFinish = true;
        private delegate void setLabelText(string value);
        private delegate void addCombox();

        public Form1()
        {
            InitializeComponent();
            Read();
        }

        private void Write()
        {
            FileStream fileStream = new FileStream(dataMapPath, FileMode.Create);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(fileStream, buildListObj);
            fileStream.Close();
        }

        private void Read()
        {
            if (!File.Exists(dataMapPath))
            {
                return;
            }

            FileStream fileStream = new FileStream(dataMapPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if(fileStream.Length <= 0)
            {
                fileStream.Close();
                return;
            }
            BinaryFormatter b = new BinaryFormatter();
            buildListObj = b.Deserialize(fileStream) as BuildList;
            fileStream.Close();

            foreach(var item in buildListObj.buildList)
            {
                comboBox1.Items.Insert(0, item.Key);
                comboBox1.SelectedIndex = 0;
                buildPath = item.Key;
            }
        }

        private List<FileInfo> GetDirectorys(DirectoryInfo dir)//搜索文件夹中的文件
        {
            List<FileInfo> FileList = new List<FileInfo>();
            FileList.AddRange(dir.GetFiles());

            DirectoryInfo[] allDir = dir.GetDirectories();
            foreach (DirectoryInfo d in allDir)
            {
                FileList.AddRange(GetDirectorys(d));
            }
            return FileList;
        }

        private List<FileInfo> GetFiles(string strPath)
        {
            List<FileInfo> FileList = new List<FileInfo>();
            if (File.Exists(strPath))
            {
                FileList.Add(new FileInfo(strPath));
            }
            else if (Directory.Exists(strPath))
            {
                FileList.AddRange(GetDirectorys(new DirectoryInfo(strPath)));
            }
            return FileList;
        }

        private string getHash(string path)
        {
            FileStream file;
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            file = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] targetData = md5.ComputeHash(file);
            string str = "";
            for (int i = 0; i < targetData.Length; i++)
            {
                str += targetData[i].ToString("x");
            }
            file.Close();
            return str;
        }

        private void setLabel2Text(string value)
        {
            label2.Text = value;
        }

        private void addCombox1()
        {
            comboBox1.Items.Insert(0, buildPath);
            comboBox1.SelectedIndex = 0;
        }

        private void BuildData(List<FileInfo> fileList)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            FileStream file;
            int count = 0;
            int total = fileList.Count();
            bool needRebuild = true;
            Dictionary<string, FileUnit> dateMap = buildListObj.buildList[buildPath].dateMap;
            Dictionary<string, List<string>> md5Map = buildListObj.buildList[buildPath].md5Map;
            Dictionary<string, FileUnit> deleteMap = new Dictionary<string, FileUnit>(dateMap);


            foreach (FileInfo info in fileList)
            {
                needRebuild = true;
                if (dateMap.ContainsKey(info.FullName))
                {
                    if(dateMap[info.FullName].dateTime == info.LastWriteTime)
                    {
                        needRebuild = false;
                    }
                    deleteMap.Remove(info.FullName);
                }
                else
                {
                    dateMap[info.FullName] = new FileUnit();
                    dateMap[info.FullName].dateTime = info.LastWriteTime;
                }

                if(needRebuild)
                {
                    file = new FileStream(info.FullName, FileMode.Open, FileAccess.Read);
                    byte[] targetData = md5.ComputeHash(file);
                    string hashValue = "";
                    for (int i = 0; i < targetData.Length; i++)
                    {
                        hashValue += targetData[i].ToString("x");
                    }

                    if(!md5Map.ContainsKey(hashValue))
                    {
                        md5Map[hashValue] = new List<string>();
                    }

                    md5Map[hashValue].Add(info.FullName);
                    dateMap[info.FullName].hashValue = hashValue;
                    file.Close();
                }

                count++;
                string value = "重建进度:" + count + "/" + total;
                this.Invoke(new setLabelText(setLabel2Text), value);
            }

            foreach(var item in deleteMap)
            {
                dateMap.Remove(item.Key);
                md5Map.Remove(item.Value.hashValue);
            }
        }

        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            textBox2.Text = "";
            searchPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in searchPaths)
            {
                textBox2.Text += path + ";";
            }
        }

        private void threadRun()
        {
            if (!buildListObj.buildList.ContainsKey(buildPath))
            {
                buildListObj.buildList[buildPath] = new BuildUnit();
                this.Invoke(new addCombox(addCombox1));
            }
            List<FileInfo> buildFileList = GetFiles(buildPath);
            BuildData(buildFileList);
            Write();
            isBuildFinish = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(buildPath.Length == 0 || !isBuildFinish)
            {
                return;
            }
            isBuildFinish = false;
            Thread t1 = new Thread(new ThreadStart(threadRun));
            t1.IsBackground = true;
            t1.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(searchPaths == null || buildPath == "")
            {
                return;
            }
            treeView1.Nodes.Clear();
            TreeNode rootNode =  treeView1.Nodes.Add(buildPath);
            List<FileInfo> fileList = new List<FileInfo>();
            foreach (string path in searchPaths)
            {
                fileList.AddRange(GetFiles(path));
            }
             
            foreach (FileInfo info in fileList)
            {
                TreeNode secondNode =  rootNode.Nodes.Add(info.FullName);
                string hashValue = getHash(info.FullName);
                if (buildListObj.buildList[buildPath].md5Map.ContainsKey(hashValue))
                {
                    foreach(string result in buildListObj.buildList[buildPath].md5Map[hashValue])
                    {
                        secondNode.Nodes.Add(result);
                    }
                }
            }
            treeView1.ExpandAll();
            isTreeNodeSelected = false;
        }

        private void comboBox1_DragDrop(object sender, DragEventArgs e)
        {
            buildPath = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            comboBox1.Text = buildPath;
        }

        private void comboBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            buildPath = comboBox1.Text;
        }

        private void OpenFolderAndSelectFile(String fileFullName)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe");
            psi.Arguments = "/e,/select," + fileFullName;
            System.Diagnostics.Process.Start(psi);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            isTreeNodeSelected = true;
            Clipboard.SetDataObject(treeView1.SelectedNode.Text);
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (isTreeNodeSelected && treeView1.SelectedNode != null)
            {
                isTreeNodeSelected = false;
                OpenFolderAndSelectFile(treeView1.SelectedNode.Text);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(comboBox1.SelectedIndex < 0 )
            {
                return;
            }
            if(comboBox1.Items.Count > 0)
            {
                buildListObj.buildList.Remove(comboBox1.Text);
                comboBox1.Items.RemoveAt(comboBox1.SelectedIndex);
                Write();
            }
            if (comboBox1.Items.Count == 0)
            {
                buildPath = "";
                comboBox1.Text = "";
                label2.Text = "重建进度:";
            }
            else if(comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }
    }

    [Serializable]
    public class FileUnit
    {
        public DateTime dateTime;
        public string hashValue;
    }

    [Serializable]
    public class BuildUnit
    {
        public Dictionary<string, List<string>> md5Map = new Dictionary<string, List<string>>();
        public Dictionary<string, FileUnit> dateMap = new Dictionary<string, FileUnit>();
    }

    [Serializable]
    public class BuildList
    {
        public Dictionary<string, BuildUnit> buildList = new Dictionary<string, BuildUnit>();
    }

}
