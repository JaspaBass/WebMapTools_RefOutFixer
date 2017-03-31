using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    class Class1
    {
        public Class1()
        {
            var ssfdsa = new Form1();
            int varPar1 = 8;
            string varPar2 = "8";
        }
        public void Class123(ref string assd, out string dd)
        {
            var ssfdsa = new Form1();
            int varPar1 = 8;
            string varPar2 = "8";
            assd = "sdfs";
            dd = "dsfs";
            if (assd == "")
                return;
        }

        public void Class123(ref string assd, ref string dd2, out string dd)
        {
            var ssfdsa = new Form1();
            int varPar1 = 8;
            string varPar2 = "8";
            assd = "sdfs";
            dd = "dsfs";
            if (assd == "")
                return;
        }
    }
}
