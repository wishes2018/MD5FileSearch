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
        public string buildPath;
        public string[] searchPaths;
        public string dataMapPath = System.IO.Path.GetTempPath() + "MD5FileSearch.data";
        public Dictionary<string, string> dataMap = new Dictionary<string, string>();
        public Form1()
        {
            InitializeComponent();
            Read();
        }

        public void Write()
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

        public void Read()
        {
            if (!File.Exists(dataMapPath))
            {
                return;
            }
            StreamReader sr = new StreamReader(dataMapPath);
            String line = sr.ReadLine();
            if (line != null)
            {
                textBox1.Text = line;
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
            foreach (FileInfo info in fileList)
            {
                file = new FileStream(info.FullName, FileMode.Open, FileAccess.Read);
                byte[] targetData = md5.ComputeHash(file);
                string str = "";
                for   (int   i=0;   i<targetData.Length;   i++)     
                {
                    str += targetData[i].ToString("x");  
                }
                dataMap[str] = info.FullName;
                file.Close();
            }
        }


        private void textBox1_DragEnter(object sender, DragEventArgs e)
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

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            buildPath = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            textBox1.Text = buildPath + ";";
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
            List<FileInfo> fileList = GetFiles(buildPath);
            BuildData(fileList);
            Write();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(searchPaths == null)
            {
                return;
            }
            textBox3.Text = "";
            List<FileInfo> fileList = new List<FileInfo>();
            foreach (string path in searchPaths)
            {
                fileList.AddRange(GetFiles(path));
            }
            foreach(FileInfo info in fileList)
            {
                string hashValue = getHash(info.FullName);
                if (dataMap.ContainsKey(hashValue))
                {
                    textBox3.Text += dataMap[hashValue] + "\r\n";
                }
            }
        }
    }
}
