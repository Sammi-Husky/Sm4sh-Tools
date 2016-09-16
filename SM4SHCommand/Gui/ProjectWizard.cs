using SALT.PARAMS;
using SALT.Scripting.AnimCMD;
using SALT.Scripting.MSC;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Sm4shCommand.GUI
{
    public partial class ProjectWizard : Form
    {
        public ProjectWizard()
        {
            InitializeComponent();
            Project = new Project();
            txtDirectory.Text = Application.StartupPath + Path.DirectorySeparatorChar;
        }
        public Project Project { get; set; }
        public string FitFolder { get { return txtFighterFolder.Text; } }
        public string ACMDFolder { get { return txtACMD.Text; } }
        public string MSCFile { get { return txtMSC.Text; } }
        public string Param_vl { get { return txtParam_vl.Text; } }
        public string Fighter_param { get { return txtFighter_Param.Text; } }
        public string SoundFolder { get { return txtSound.Text; } }
        public string MotionFolder { get { return txtMotionFolder.Text; } }

        public string ProjectName { get { return txtName.Text; } }
        public string ProjectDirectory { get { return txtDirectory.Text; } }
        public Endianness Platform
        {
            get
            {
                if (chkWiiu.Checked)
                    return Endianness.Big;
                else
                    return Endianness.Little;
            }
        }

        private void btnFighterFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderSelectDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtFighterFolder.Text = dlg.SelectedPath;

                var path = Path.Combine(dlg.SelectedPath, "script", "animcmd", "body");
                if (Directory.Exists(path))
                    txtACMD.Text = path;

                path = Path.Combine(dlg.SelectedPath, "script", "msc");
                if (Directory.Exists(path))
                    txtMSC.Text = path;

                path = Path.Combine(dlg.SelectedPath, "motion");
                if (Directory.Exists(path))
                    txtMotionFolder.Text = path;

                path = Path.Combine(dlg.SelectedPath, "sound");
                if (Directory.Exists(path))
                    txtSound.Text = path;
            }
        }
        private void button9_Click(object sender, EventArgs e)
        {

            Directory.CreateDirectory(Path.GetDirectoryName(txtDirectory.Text));
            Project.ProjName = txtName.Text;

            Project.Fighter_Param_vl = new ParamFile(txtParam_vl.Text);
            Project.Attributes = new ParamFile(txtFighter_Param.Text);
            foreach (var path in Directory.EnumerateFiles(txtACMD.Text))
            {
                if (path.EndsWith(".bin", StringComparison.InvariantCultureIgnoreCase))
                    Project.ACMD_FILES.Add(Path.GetFileNameWithoutExtension(path), new ACMDFile(path));
                else if (path.EndsWith(".mtable", StringComparison.InvariantCultureIgnoreCase))
                    Project.MotionTable = new MTable(path, Platform);
            }

            var msc = Directory.EnumerateFiles(txtMSC.Text, "*.mscsb").First();
            Project.MSC_FILES.Add(Path.GetFileNameWithoutExtension(msc), new MSCFile(msc));

            // Animations
            Project.MotionFolder = txtMotionFolder.Text;
            Project.ANIM_FILES.Clear();
            var files = Directory.EnumerateFiles(txtMotionFolder.Text, "*.*", SearchOption.AllDirectories).
                Where(x => x.EndsWith(".pac", StringComparison.InvariantCultureIgnoreCase) ||
                x.EndsWith(".bch", StringComparison.InvariantCultureIgnoreCase)).Select(x => x);
            foreach (var f in files)
            {
                Project.ANIM_FILES.Add(Path.Combine("ANIM", f.Remove(0, txtMotionFolder.Text.Length + 1)));
            }

            Project.Populate();
            Project.Save(Path.GetDirectoryName(txtDirectory.Text));

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private void button10_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void btnParam_vl_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtParam_vl.Text = dlg.FileName;
        }
        private void btnFighter_Param_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtFighter_Param.Text = dlg.FileName;
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            txtDirectory.Text =
                Path.Combine(Application.StartupPath, txtName.Text, txtName.Text + ".fitproj");
        }
        private void chkSimple_CheckedChanged(object sender, EventArgs e)
        {
            pnlAdvanced.Enabled = !chkSimple.Checked;
            txtFighterFolder.Enabled =
            btnFighterFolder.Enabled =
                chkSimple.Checked;
        }

        private void btnMotionFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderSelectDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtMotionFolder.Text = dlg.SelectedPath;
        }
        private void btnACMD_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderSelectDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtACMD.Text = dlg.SelectedPath;
        }
        private void btnSound_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderSelectDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtSound.Text = dlg.SelectedPath;
        }
        private void btnMSC_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtMSC.Text = dlg.FileName;
        }
    }
}
