using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SimpleDetector
{
    public partial class Tip : Form
    {
        int choose=1;
        public Tip()
        {
            InitializeComponent();
        }

        private void Tip_Load(object sender, EventArgs e)
        {
            // randomize choose;
            
            switch (choose)
            {
                case 1: textBox1.Text = "You can also add Password for your personnel settings. So that no one else can change your settings.";
                    break;
                case 2: textBox1.Text = "Icons can be added as Default icon for the Removable Disks not only for this PC but for all other too.";
                    break;
                case 3: textBox1.Text = "After Setting icon to any Removal Disk then Don not format it else you'll loose the icon.";
                    break;
                case 4: textBox1.Text = "For getting idea whats inside the Removable disk you can click on Scan to get details in TREE formate";
                    break;
                case 5: textBox1.Text = "Log of insertion of Removable disk will be only Saved untill the software is Open";
                    break;
                default: textBox1.Text = "You can also set a Path to copy all data from any Removable drive automatically. That too in Hidden form.";
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            choose++;
            if (choose > 6)
            {
                choose = 1;
            }
            Tip_Load(sender, e);
        }
    }
}
