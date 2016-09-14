using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MD5FileSearch
{
    public partial class Form1 : Form
    {
        private string buildPath;
        private string[] searchPaths;
        private string dataMapPath = System.IO.Path.GetTempPath() + "MD5FileSearch.data";
        private Dictionary<string, string> dataMap = new Dictionary<string, string>();
        private Dictionary<string, bool> dropList = new Dictionary<string, bool>();
        private bool isTreeNodeSelected = false;
        public Form1()
        {
            InitializeComponent();
            Read();
        }

        private void Write()
        {
            StreamWriter sw = new StreamWriter(dataMapPath);
            sw.WriteLine(buildPath);
            foreach (var item in dataMap)
            {
                sw.WriteLine(item.Key + " " + item.Value);
            }
            sw.Flush();
            sw.Close();
        }

        private void Read()
        {
            if (!File.Exists(dataMapPath))
            {
                return;
            }
            StreamReader sr = new StreamReader(dataMapPath);
            String line = sr.ReadLine();
            if (line != null)
            {
                comboBox1.Text = line;
                buildPath = line;
            }
            while ((line = sr.ReadLine()) != null)
            {
                string[] split = line.Split(new Char[] {' '});
                dataMap[split[0]] = split[1];
            }
            sr.Close();
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

        private void BuildData(List<FileInfo> fileList)
        {
            dataMap.Clear();
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            FileStream file;
            int count = 0;
            int doEventCount = 0;
            int total = fileList.Count();
            foreach (FileInfo info in fileList)
            {
                doEventCount++;
                if(doEventCount > 4)
                {
                    Application.DoEvents();
                    doEventCount = 0;
                }
                file = new FileStream(info.FullName, FileMode.Open, FileAccess.Read);
                byte[] targetData = md5.ComputeHash(file);
                string hashValue = "";
                for   (int   i=0;   i<targetData.Length;   i++)     
                {
                    hashValue += targetData[i].ToString("x");  
                }
                if(dataMap.ContainsKey(hashValue))
                {
                    dataMap[hashValue] += ";" + info.FullName;
                }else
                {
                    dataMap[hashValue] = info.FullName;
                }
                file.Close();
                count++;
                label2.Text = "重建进度:" + count + "/" + total;
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

        private void button1_Click(object sender, EventArgs e)
        {
            if(buildPath.Length == 0)
            {
                return;
            }
            List<FileInfo> buildFileList = GetFiles(buildPath);
            BuildData(buildFileList);
            Write();
            if (!dropList.ContainsKey(buildPath))
            {
                dropList[buildPath] = true;
                comboBox1.Items.Insert(0, buildPath);
                comboBox1.SelectedIndex = 0;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(searchPaths == null)
            {
                return;
            }
            treeView1.Nodes.Clear();
            TreeNode rootNode =  treeView1.Nodes.Add("Root");
            List<FileInfo> fileList = new List<FileInfo>();
            foreach (string path in searchPaths)
            {
                fileList.AddRange(GetFiles(path));
            }
            foreach(FileInfo info in fileList)
            {
                TreeNode secondNode =  rootNode.Nodes.Add(info.FullName);
                string hashValue = getHash(info.FullName);
                if (dataMap.ContainsKey(hashValue))
                {
                    hashValue = dataMap[hashValue] + "\r\n";
                    string[] split = hashValue.Split(new Char[] { ';' });
                    foreach(string result in split)
                    {
                        secondNode.Nodes.Add(result);
                    }
                }
            }
            treeView1.ExpandAll();
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
            if (isTreeNodeSelected)
            {
                isTreeNodeSelected = false;
                OpenFolderAndSelectFile(treeView1.SelectedNode.Text);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(comboBox1.Items.Count > 0)
            {
                comboBox1.Items.RemoveAt(comboBox1.SelectedIndex);
                dropList.Remove(comboBox1.Text);
            }
            if (comboBox1.Items.Count == 0)
            {
                buildPath = "";
                comboBox1.Text = "";
            }
            else if(comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }
    }
}
