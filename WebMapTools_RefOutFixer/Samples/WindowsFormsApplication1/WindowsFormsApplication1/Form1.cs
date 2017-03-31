using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public int integer = 0;

        private void button1_Click(object sender, EventArgs e)
        {
            string hh = "";
            MessageBox.Show(ToUpper(ref integer, ref hh));
            int ss = 9;
            MessageBox.Show(ToLower(ref integer, ref hh));
            int ss2 = 9;
            ToLower2(ref integer, ref hh);
            int ss3 = 9;
            MessageBox.Show(integer.ToString());
            if (ToLowerBool(integer, ref integer, ref hh, integer))
            {
                MessageBox.Show(hh);
            }

            switch(ToUpper(ref ss, ref hh))
            {
                case "Uno" : MessageBox.Show("Uno");break;
                case "Dos": MessageBox.Show("Dos"); break;
                case "Tres": MessageBox.Show("Tres"); break;
            }

        }
        public string ToUpper(ref int count, ref string count2)
        {
            MessageBox.Show("Press Yes", "Hello World!!!", MessageBoxButtons.YesNo);
            count++;
            count2 = count.ToString();
            return txtText.Text.ToUpper();
        }
        public string ToLower(ref int count, ref string count2)
        {
            MessageBox.Show("Press Yes", "Hello World!!!", MessageBoxButtons.YesNo);
            count++;
            count2 = count.ToString();
            if (count == 0)
                return txtText.Text.ToLower();
            else
                return "dsfa";
        }
        public void ToLower2(ref int count12, ref string count212)
        {
            MessageBox.Show("Press Yes", "Hello World!!!", MessageBoxButtons.YesNo);
            count12++;
            count212 = count12.ToString();
        }
        public bool ToLowerBool(int ass, ref int count12, ref string count212, int assss)
        {
            MessageBox.Show("Press Yes", "Hello World!!!", MessageBoxButtons.YesNo);
            count12++;
            count212 = count12.ToString();
            Class1 cls = new Class1();
            string ss = "ssd";
            string ss1 = "ssd";
            cls.Class123(ref ss, ref ss, out ss1);
            return true;
        }
    }
}
