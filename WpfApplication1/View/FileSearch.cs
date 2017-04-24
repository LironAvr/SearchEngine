using Search_Engine.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WpfApplication1.View
{
    public partial class FileSearch : Form
    {
        public FileSearch()
        {
            InitializeComponent();
            label1.BackColor = Color.Transparent;
            label2.BackColor = Color.Transparent;
            pictureBox1.BackColor = Color.Transparent;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            ConValues.Queries = dialog.FileName;
            queries_path.Text = ConValues.Queries;
            queries_path.Modified = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            ConValues.Results = dialog.SelectedPath;
            results_path.Text = ConValues.Results;
            results_path.Modified = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (results_path.Modified & queries_path.Modified)
            {
                DialogResult = DialogResult.Yes;
                this.Close();
            }
            else System.Windows.MessageBox.Show("Invalid Information, Please choose a valid Queries file and Results path", "Warning");
        }
    }
}
