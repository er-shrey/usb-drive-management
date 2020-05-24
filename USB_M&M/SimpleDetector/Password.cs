using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;

namespace SimpleDetector
{
    public partial class Password : Form
    {
        public Password()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text=="")
            {
                MessageBox.Show("Enter Password");
                textBox1.Focus();
            }
            else
            {
                SqlConnection con = new SqlConnection("Data Source=SHREYKUMARJAIN\\SHREYKUMARJAIN; Initial Catalog=usb_mgmt; Integrated Security=TRUE");
                SqlCommand cmd;
                con.Open();
                SqlDataReader rd;
                cmd = new SqlCommand("select * from password", con);
                rd = cmd.ExecuteReader();
                if (rd.Read() == true)
                {
                    if (textBox1.Text == rd[0].ToString())
                    {
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid Password");
                        textBox1.Clear();
                        textBox1.Focus();
                    }

                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            try
            {
                FileInfo f1 = new FileInfo("c:\\cancle_check.shrey");
                StreamWriter sw = f1.CreateText();
                sw.WriteLine("1");
                sw.Close();
            }
            catch (IOException e1)
            {
                Console.WriteLine("an exception=>" + e1);
            }
        }
    }
}
