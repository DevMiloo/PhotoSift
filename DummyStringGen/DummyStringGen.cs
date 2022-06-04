using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DummyStringGen
{
    internal class DummyStringGen
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(System.Environment.CurrentDirectory);
            string srcDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../../PhotoSift"));
            string designerFilePath = Path.Combine(srcDir, "frmMain.Designer.cs");
            if (File.Exists(designerFilePath))
            {
                string dummyCsText = "";
                string lines = File.ReadAllText(designerFilePath);
                foreach (Match m in new Regex(@"this\..+?ToolStripMenuItem.Text = ""(.+?)"";").Matches(lines))
                {
                    dummyCsText += $@"_p(""menu"", ""{m.Groups[1].Value}"")" + ";\r\n";
                }
                foreach (Match m in new Regex(@"\.mnu.+?.Text = ""(.+?)"";").Matches(lines))
                {
                    dummyCsText += $@"_p(""menu"", ""{m.Groups[1].Value}"")" + ";\r\n";
                }                

                File.WriteAllText(Path.Combine(srcDir, "frmMain.Designer_mnuLabel.cs"), dummyCsText);
            }

        }
    }
}
