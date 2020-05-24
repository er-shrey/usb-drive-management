using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;

namespace SimpleDetector
{
    public partial class Hidden_Copy : Form
    {
        string path;
        public Hidden_Copy(string source_path)
        {
            path = source_path;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //SqlConnection con = new SqlConnection("Data Source=SHREYKUMARJAIN\\SHREYKUMARJAIN; Initial Catalog=usb_mgmt; Integrated Security=TRUE");
            //SqlCommand cmd;
            //con.Open();
            //SqlDataReader rd;
            //cmd = new SqlCommand("select * from H_copy", con);
            //rd = cmd.ExecuteReader();
            //CopyFolder(path, rd[0].ToString());
        }
        public void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }
    }
}
