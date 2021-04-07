using System;
using System.Windows.Forms;

namespace jjget
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
        }

        private string getAssemblyVersion(Type t)
        {
            var a = System.Reflection.Assembly.GetAssembly(t);
            if(a == System.Reflection.Assembly.GetAssembly(typeof(frmAbout)))
                return "(bundled)";
            return a.GetName().Version.ToString();
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {
            label3.Text =  "v" + Application.ProductVersion;
            
            label6.Text =
@"HtmlAgilityPack " + getAssemblyVersion(typeof(HtmlAgilityPack.HtmlDocument)) + @"
 - https://html-agility-pack.net/
Newtonsoft.Json " + getAssemblyVersion(typeof(Newtonsoft.Json.JsonConverter)) + @"
 - https://www.newtonsoft.com/json
";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel1.Text);
        }
    }
}
