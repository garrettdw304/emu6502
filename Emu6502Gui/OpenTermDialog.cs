using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emu6502;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

namespace Emu6502Gui
{
    public partial class OpenTermDialog : Form
    {
        public OpenTermDialog()
        {
            InitializeComponent();
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            string? selected = (string?)portsDropdown.SelectedItem;
            if (selected == null || portsDropdown.SelectedIndex == 0)
            {
                MessageBox.Show("Select a port.");
                return;
            }

            Process.Start($"plink.exe", $"-serial {selected} -sercfg 9600,8,n,1,N");
            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void portsDropdown_DropDown(object sender, EventArgs e)
        {
            string? oldName = (string?)portsDropdown.SelectedItem;

            portsDropdown.Items.Clear();
            portsDropdown.Items.Add("None");
            portsDropdown.Items.AddRange(SerialPort.GetPortNames());
            if (oldName != null)
            {
                int index = portsDropdown.Items.IndexOf(oldName);
                if (index != -1)
                    portsDropdown.SelectedIndex = index;
                else
                    portsDropdown.SelectedIndex = 0;
            }
        }
    }
}
