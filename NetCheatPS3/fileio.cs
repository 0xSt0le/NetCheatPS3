using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace NetCheatPS3
{
    class fileio
    {

        /*
         * Opens a code database
         */
        public static Form1.CodeDB[] OpenFile(string file)
        {
            if (!File.Exists(file))
                return null;

            Form1.CodeDB[] ret = null;
            int z = 1;

            if (file == "" || file == null)
            {
                System.Windows.Forms.MessageBox.Show("Error: File path invalid!");
                return ret;
            }

            string codeFile = File.ReadAllText(file);
            string[] tempStr = codeFile.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int len = 0, y = 0, x = 0;
            bool isGPS3F = false;
            while (y < tempStr.Length)
            {
                if (tempStr[y] == "}")
                {
                    len++;
                    isGPS3F = false;
                }
                else if (tempStr[y] == "#")
                {
                    len++;
                    isGPS3F = true;
                }
                y++;
            }

            ret = new Form1.CodeDB[len];

            if (isGPS3F)
            {
                string[] gfArr = codeFile.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                for (x = 0; x < len; x++)
                {
                    string[] lines = gfArr[x].Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    ret[x].name = lines[0];
                    ret[x].state = lines[1] == "1";
                    ret[x].codes = String.Join(Environment.NewLine, lines, 2, lines.Length - 2);
                }
            }
            else
            {
                for (x = 0; z < tempStr.Length; x++)
                {
                    if (x >= ret.Length)
                        break;

                    ret[x].state = bool.Parse(tempStr[z]); z++;
                    ret[x].name = tempStr[z]; z++;
                    ret[x].codes = "";

                    while (tempStr[z] != "}")
                    {
                        ret[x].codes += tempStr[z] + "\r\n";
                        z++;
                    }

                    if (ret[x].codes != "")
                        ret[x].codes = ret[x].codes.Remove(ret[x].codes.Length - 1);
                    z += 2;
                }
            }

            return ret;
        }

        /*
         * Saves the code database save into file
         */
        public static void SaveFile(string file, Form1.CodeDB save)
        {
            if (file == "" || file == null)
            {
                System.Windows.Forms.MessageBox.Show("Error: File path invalid!");
                return;
            }

            //string[] str = { "{", save.state.ToString(), save.name, save.codes, "}\n" };
            string[] str = { save.name, save.state ? "1" : "0", save.codes, "#" };
            System.IO.File.WriteAllLines(file, str);
        }

        /*
         * Saves all codes into file
         */
        public static void SaveFileAll(string file)
        {
            if (file == "" || file == null)
            {
                System.Windows.Forms.MessageBox.Show("Error: File path invalid!");
                return;
            }

            if (File.Exists(file))
                File.Delete(file);
            using (System.IO.StreamWriter fd = new System.IO.StreamWriter(file, true))
            {
                for (int x = 0; x <= Form1.CodesCount; x++)
                {
                    /*
                    fd.WriteLine("{");
                    fd.WriteLine(Form1.Codes[x].state.ToString());
                    fd.WriteLine(Form1.Codes[x].name);
                    fd.WriteLine(Form1.Codes[x].codes);
                    fd.WriteLine("}");
                    */
                    fd.WriteLine(Form1.Codes[x].name);
                    fd.WriteLine(Form1.Codes[x].state ? "1" : "0");
                    fd.WriteLine(Form1.Codes[x].codes);
                    fd.WriteLine("#");
                }
            }
        }

        /*
         * Saves all codes into file
         */
        public static void SaveFileAll(string file, List<Form1.CodeDB> codes)
        {
            if (file == "" || file == null)
            {
                System.Windows.Forms.MessageBox.Show("Error: File path invalid!");
                return;
            }

            if (File.Exists(file))
                File.Delete(file);
            using (System.IO.StreamWriter fd = new System.IO.StreamWriter(file, true))
            {
                for (int x = 0; x < codes.Count; x++)
                {
                    /*
                    fd.WriteLine("{");
                    fd.WriteLine(codes[x].state.ToString());
                    fd.WriteLine(codes[x].name);
                    fd.WriteLine(codes[x].codes);
                    fd.WriteLine("}");
                    */
                    fd.WriteLine(codes[x].name);
                    fd.WriteLine(codes[x].state ? "1" : "0");
                    fd.WriteLine(codes[x].codes);
                    fd.WriteLine("#");
                }
            }
        }


        /*
         * Saves a NetCheat Memory Range file
         */
        public static void SaveRangeFile(string file, System.Windows.Forms.ListView save)
        {
            if (file == "" || file == null)
            {
                System.Windows.Forms.MessageBox.Show("Error: File path invalid!");
                return;
            }

            string[] str = new string[save.Items.Count * 2];
            int y = 0;
            for (int x = 0; x < (str.Length/2); x++)
            {
                str[y] = save.Items[x].SubItems[0].Text;
                str[y+1] = save.Items[x].SubItems[1].Text;
                y += 2;
            }
            System.IO.File.WriteAllLines(file, str);
        }

        /*
         * Opens a NetCheat Memory Range file
         */
        public static System.Windows.Forms.ListView OpenRangeFile(string file)
        {
            if (file == "" || file == null)
            {
                System.Windows.Forms.MessageBox.Show("Error: File path invalid!");
                return null;
            }

            System.Windows.Forms.ListView ret = new System.Windows.Forms.ListView();

            string[] fileArr = File.ReadAllLines(file);
            string[] str = new string[2];
            ret.Items.Clear();

            for (int x = 0; x < fileArr.Length; x += 2)
            {
                str[0] = fileArr[x];
                str[1] = fileArr[x+1];
                ListViewItem a = new ListViewItem(str);
                ret.Items.Add(a);
            }
            return ret;
        }

    }
}
