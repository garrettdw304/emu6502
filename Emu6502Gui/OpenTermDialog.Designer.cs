namespace Emu6502Gui
{
    partial class OpenTermDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            openBtn = new Button();
            cancelBtn = new Button();
            label1 = new Label();
            portsDropdown = new ComboBox();
            SuspendLayout();
            // 
            // openBtn
            // 
            openBtn.Location = new Point(108, 76);
            openBtn.Name = "openBtn";
            openBtn.Size = new Size(112, 34);
            openBtn.TabIndex = 0;
            openBtn.Text = "Open";
            openBtn.UseVisualStyleBackColor = true;
            openBtn.Click += openBtn_Click;
            // 
            // cancelBtn
            // 
            cancelBtn.Location = new Point(108, 116);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new Size(112, 34);
            cancelBtn.TabIndex = 1;
            cancelBtn.Text = "Cancel";
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += cancelBtn_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(57, 9);
            label1.Name = "label1";
            label1.Size = new Size(222, 25);
            label1.TabIndex = 2;
            label1.Text = "Select a port to connect to";
            // 
            // portsDropdown
            // 
            portsDropdown.FormattingEnabled = true;
            portsDropdown.Location = new Point(72, 37);
            portsDropdown.Name = "portsDropdown";
            portsDropdown.Size = new Size(182, 33);
            portsDropdown.TabIndex = 3;
            portsDropdown.DropDown += portsDropdown_DropDown;
            // 
            // OpenTermDialog
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(347, 163);
            Controls.Add(portsDropdown);
            Controls.Add(label1);
            Controls.Add(cancelBtn);
            Controls.Add(openBtn);
            Name = "OpenTermDialog";
            Text = "Open Terminal";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button openBtn;
        private Button cancelBtn;
        private Label label1;
        private ComboBox portsDropdown;
    }
}