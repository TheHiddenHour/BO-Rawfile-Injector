using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BO_Rawfile_Injector
{
    public partial class Form1 : Form
    {
        string mod_path;
        int progress = 0;
        uint xassetpool = 0x00e921f8;// = 0x00e921f8;//0x1186DC0;

        public Form1()
        {
            InitializeComponent();

            try
            {
                Mem.Connect();
            }
            catch
            {
                MessageBox.Show("Could not connect and attach process");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e) //Select custom GSCs
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "Select a modded gsc folder (pc format)"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                mod_path = dialog.SelectedPath;
                textBox1.Text = mod_path;
            }
            dialog.Dispose();
        }

        private void Goto(RichTextBox myRichTextBox, Int32 lineToGo)
        {
            Int32 j = 0;
            String text = myRichTextBox.Text;
            for (Int32 i = 1; i < lineToGo; i++)
            {
                j = text.IndexOf('\n', j + 1);
                if (j == -1) break;
            }
            myRichTextBox.Select(j + 1, 0);
            myRichTextBox.ScrollToCaret();
        }

        private void button2_Click(object sender, EventArgs e) //Inject custom GSCs
        {
            richTextBox1.Text = "";
            if (mod_path == null || !Directory.Exists(mod_path)) //stop if no folder selected
                return;

            //toolStripProgressBar1.Value = 0;
            progress = 0;
            RawPool pool = new RawPool(xassetpool);

            pool.ReadPoolData();
            pool.ReadFree();
            pool.ReadRawfiles();

            for (int i = 0; i < pool.rawfiles.Count - 1; i++)
            {
                pool.rawfiles[i].name = PS3.ReadCString(pool.rawfiles[i].name_ptr);
                richTextBox1.Text += "[ 0x" + pool.rawfiles[i].index.ToString("X") + " ] Found: " + pool.rawfiles[i].name + "\n";
                //toolStripProgressBar1.Value = Convert.ToInt32((progress / pool.rawfiles.Count) * RawPool.PoolMax);
                progress++;
                Goto(richTextBox1, progress);
            }

            //read local files.
            string output = pool.ReadLocalFiles(mod_path);

            richTextBox1.Text += output;
            Goto(richTextBox1, richTextBox1.Lines.Count() - 1);

            //start injecting..
            for (int i = 0; i < pool.rawfiles.Count; i++)
            {
                if(pool.rawfiles[i].requireOverwrite)
                {
                    pool.writeRawfile(pool.rawfiles[i]);
                }
            }

            //update the freehead.
            //pool.updateFreeHead();
        }

        public int getLastFree(RawPool pool)
        {
            int index = pool.freeIndices[0];
            pool.freeIndices.RemoveAt(0);
            return index;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            mod_path = textBox1.Text;
        }

        private void button3_Click(object sender, EventArgs e) //Dump GSCs
        {
            richTextBox1.Text = "";
            FolderBrowserDialog fb = new FolderBrowserDialog();
            if (fb.ShowDialog() == DialogResult.OK)
            {
                RawPool pool = new RawPool(xassetpool);

                pool.ReadPoolData();
                pool.ReadFree();
                pool.ReadRawfiles();

                for (int i = 0; i < pool.rawfiles.Count - 1; i++)
                {
                    string name = PS3.ReadCString(pool.rawfiles[i].name_ptr);
                    if(!String.IsNullOrEmpty(name))
                    {
                        byte[] buffer = PS3.GetMemory(pool.rawfiles[i].buffer_ptr, (int)pool.rawfiles[i].length);

                        if (name.Contains("/"))
                        {
                            name = name.Replace("/", @"\");
                            String[] lel = name.Replace(@"\", "|").Split('|');
                            String directory = fb.SelectedPath + @"\" + name.Replace(lel[lel.Length - 1], "");
                            if (!Directory.Exists(directory))
                                Directory.CreateDirectory(directory);
                        }
                        String path = name.Replace("//", @"\").Replace("/", @"\");
                        File.WriteAllBytes(fb.SelectedPath + @"\" + path, buffer);
                        richTextBox1.Text += "Dump " + name + "\n";
                        Goto(richTextBox1, richTextBox1.Lines.Count() - 1);
                    }
                }
            }
        }

        private void sPZMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mPToolStripMenuItem.Checked = false;
            sPZMToolStripMenuItem.Checked = true;
            xassetpool = 0x00e921f8;
        }

        private void mPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mPToolStripMenuItem.Checked = true;
            sPZMToolStripMenuItem.Checked = false;
            xassetpool = 0x1186DC0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RawPool pool = new RawPool(xassetpool);

            pool.ReadPoolData();
            pool.ReadFree();
            pool.ReadRawfiles();
        }
    }
}
