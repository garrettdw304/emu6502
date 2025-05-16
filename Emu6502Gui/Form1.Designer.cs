namespace Emu6502Gui
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            continueBtn = new Button();
            cycleBtn = new Button();
            loadRomBtn = new Button();
            cpuStatusLbl = new Label();
            romFileDialog = new OpenFileDialog();
            outputLbl = new Label();
            clockRateTB = new TextBox();
            label1 = new Label();
            statusTimer = new System.Windows.Forms.Timer(components);
            memDisplay = new Label();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            scrollToTB = new TextBox();
            scrollToBtn = new Button();
            stackLbl = new Label();
            groupBox3 = new GroupBox();
            rstBtn = new Button();
            irqBtn = new Button();
            nmiBtn = new Button();
            groupBox4 = new GroupBox();
            label2 = new Label();
            uartDropdown = new ComboBox();
            label3 = new Label();
            graphicsBtn = new Button();
            terminalBtn = new Button();
            drivePathBtn = new Button();
            drivePathDialog = new FolderBrowserDialog();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            SuspendLayout();
            // 
            // continueBtn
            // 
            continueBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            continueBtn.Location = new Point(710, 448);
            continueBtn.Margin = new Padding(2);
            continueBtn.Name = "continueBtn";
            continueBtn.Size = new Size(90, 27);
            continueBtn.TabIndex = 0;
            continueBtn.Text = "Continue";
            continueBtn.UseVisualStyleBackColor = true;
            continueBtn.Click += continueBtn_Click;
            // 
            // cycleBtn
            // 
            cycleBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cycleBtn.Location = new Point(710, 416);
            cycleBtn.Margin = new Padding(2);
            cycleBtn.Name = "cycleBtn";
            cycleBtn.Size = new Size(90, 27);
            cycleBtn.TabIndex = 1;
            cycleBtn.Text = "Cycle";
            cycleBtn.UseVisualStyleBackColor = true;
            cycleBtn.Click += cycleBtn_Click;
            // 
            // loadRomBtn
            // 
            loadRomBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            loadRomBtn.Location = new Point(214, 448);
            loadRomBtn.Margin = new Padding(2);
            loadRomBtn.Name = "loadRomBtn";
            loadRomBtn.Size = new Size(90, 27);
            loadRomBtn.TabIndex = 2;
            loadRomBtn.Text = "Load ROM";
            loadRomBtn.UseVisualStyleBackColor = true;
            loadRomBtn.Click += loadRomBtn_Click;
            // 
            // cpuStatusLbl
            // 
            cpuStatusLbl.AutoSize = true;
            cpuStatusLbl.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cpuStatusLbl.Location = new Point(5, 22);
            cpuStatusLbl.Margin = new Padding(2, 0, 2, 0);
            cpuStatusLbl.Name = "cpuStatusLbl";
            cpuStatusLbl.Size = new Size(62, 170);
            cpuStatusLbl.TabIndex = 6;
            cpuStatusLbl.Text = "A:\r\nX:\r\nY:\r\nS:\r\nP:\r\nP:\r\nPC:\r\nStep:\r\nInstr:\r\nCycle:";
            // 
            // outputLbl
            // 
            outputLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            outputLbl.AutoSize = true;
            outputLbl.Location = new Point(309, 452);
            outputLbl.Margin = new Padding(2, 0, 2, 0);
            outputLbl.Name = "outputLbl";
            outputLbl.Size = new Size(53, 20);
            outputLbl.TabIndex = 7;
            outputLbl.Text = "output";
            // 
            // clockRateTB
            // 
            clockRateTB.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clockRateTB.Location = new Point(710, 387);
            clockRateTB.Margin = new Padding(2);
            clockRateTB.Name = "clockRateTB";
            clockRateTB.Size = new Size(90, 27);
            clockRateTB.TabIndex = 8;
            clockRateTB.Text = "1000000";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(710, 363);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(27, 20);
            label1.TabIndex = 9;
            label1.Text = "Hz";
            // 
            // statusTimer
            // 
            statusTimer.Enabled = true;
            statusTimer.Tick += statusTimer_Tick;
            // 
            // memDisplay
            // 
            memDisplay.AutoSize = true;
            memDisplay.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            memDisplay.Location = new Point(5, 22);
            memDisplay.Margin = new Padding(2, 0, 2, 0);
            memDisplay.Name = "memDisplay";
            memDisplay.Size = new Size(476, 289);
            memDisplay.TabIndex = 10;
            memDisplay.Text = "      0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F\r\n0000\r\n0010\r\n0020\r\n0030\r\n0040\r\n0050\r\n0060\r\n0070\r\n0080\r\n0090\r\n00A0\r\n00B0\r\n00C0\r\n00D0\r\n00E0\r\n00F0";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cpuStatusLbl);
            groupBox1.Location = new Point(10, 10);
            groupBox1.Margin = new Padding(2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(2);
            groupBox1.Size = new Size(200, 350);
            groupBox1.TabIndex = 11;
            groupBox1.TabStop = false;
            groupBox1.Text = "Processor Status";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(scrollToTB);
            groupBox2.Controls.Add(scrollToBtn);
            groupBox2.Controls.Add(memDisplay);
            groupBox2.Location = new Point(321, 10);
            groupBox2.Margin = new Padding(2);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(2);
            groupBox2.Size = new Size(479, 350);
            groupBox2.TabIndex = 12;
            groupBox2.TabStop = false;
            groupBox2.Text = "Memory";
            // 
            // scrollToTB
            // 
            scrollToTB.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            scrollToTB.Location = new Point(398, 320);
            scrollToTB.Margin = new Padding(2);
            scrollToTB.Name = "scrollToTB";
            scrollToTB.Size = new Size(77, 27);
            scrollToTB.TabIndex = 13;
            scrollToTB.Text = "0x0000";
            // 
            // scrollToBtn
            // 
            scrollToBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            scrollToBtn.Location = new Point(304, 318);
            scrollToBtn.Margin = new Padding(2);
            scrollToBtn.Name = "scrollToBtn";
            scrollToBtn.Size = new Size(90, 27);
            scrollToBtn.TabIndex = 13;
            scrollToBtn.Text = "Scroll To";
            scrollToBtn.UseVisualStyleBackColor = true;
            scrollToBtn.Click += scrollToBtn_Click;
            // 
            // stackLbl
            // 
            stackLbl.AutoSize = true;
            stackLbl.Location = new Point(5, 22);
            stackLbl.Margin = new Padding(2, 0, 2, 0);
            stackLbl.Name = "stackLbl";
            stackLbl.Size = new Size(87, 320);
            stackLbl.TabIndex = 13;
            stackLbl.Text = "000 <- 0xF0\r\n000 <- SP\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000\r\n000 <- 0xFF\r\n";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(stackLbl);
            groupBox3.Location = new Point(214, 10);
            groupBox3.Margin = new Padding(2);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(2);
            groupBox3.Size = new Size(102, 350);
            groupBox3.TabIndex = 14;
            groupBox3.TabStop = false;
            groupBox3.Text = "Stack";
            // 
            // rstBtn
            // 
            rstBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            rstBtn.Location = new Point(616, 448);
            rstBtn.Margin = new Padding(2);
            rstBtn.Name = "rstBtn";
            rstBtn.Size = new Size(90, 27);
            rstBtn.TabIndex = 15;
            rstBtn.Text = "RST";
            rstBtn.UseVisualStyleBackColor = true;
            rstBtn.Click += rstBtn_Click;
            // 
            // irqBtn
            // 
            irqBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            irqBtn.Location = new Point(616, 416);
            irqBtn.Margin = new Padding(2);
            irqBtn.Name = "irqBtn";
            irqBtn.Size = new Size(90, 27);
            irqBtn.TabIndex = 16;
            irqBtn.Text = "IRQ";
            irqBtn.UseVisualStyleBackColor = true;
            irqBtn.Click += irqBtn_Click;
            // 
            // nmiBtn
            // 
            nmiBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            nmiBtn.Location = new Point(616, 386);
            nmiBtn.Margin = new Padding(2);
            nmiBtn.Name = "nmiBtn";
            nmiBtn.Size = new Size(90, 27);
            nmiBtn.TabIndex = 17;
            nmiBtn.Text = "NMI";
            nmiBtn.UseVisualStyleBackColor = true;
            nmiBtn.Click += nmiBtn_Click;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(label2);
            groupBox4.Location = new Point(10, 365);
            groupBox4.Margin = new Padding(2);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(2);
            groupBox4.Size = new Size(200, 110);
            groupBox4.TabIndex = 18;
            groupBox4.TabStop = false;
            groupBox4.Text = "Memory Map";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(5, 22);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(179, 85);
            label2.TabIndex = 0;
            label2.Text = "DEVICE BASE  LENGTH\r\nRAM    $0000 $8000\r\nUART   $B000 $0002\r\nDRIVE  $B100 $0002\r\nTIMER  $B200 $0004";
            // 
            // uartDropdown
            // 
            uartDropdown.FormattingEnabled = true;
            uartDropdown.Location = new Point(214, 385);
            uartDropdown.Margin = new Padding(2);
            uartDropdown.Name = "uartDropdown";
            uartDropdown.Size = new Size(146, 28);
            uartDropdown.TabIndex = 19;
            uartDropdown.DropDown += serialPortDropdown_DropDown;
            uartDropdown.SelectedIndexChanged += uartDropdown_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(214, 363);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(116, 20);
            label3.TabIndex = 20;
            label3.Text = "UART Serial Port";
            // 
            // graphicsBtn
            // 
            graphicsBtn.Location = new Point(214, 416);
            graphicsBtn.Margin = new Padding(2);
            graphicsBtn.Name = "graphicsBtn";
            graphicsBtn.Size = new Size(90, 27);
            graphicsBtn.TabIndex = 21;
            graphicsBtn.Text = "Graphics";
            graphicsBtn.UseVisualStyleBackColor = true;
            graphicsBtn.Click += graphicsBtn_Click;
            // 
            // terminalBtn
            // 
            terminalBtn.Location = new Point(365, 384);
            terminalBtn.Margin = new Padding(2);
            terminalBtn.Name = "terminalBtn";
            terminalBtn.Size = new Size(90, 27);
            terminalBtn.TabIndex = 22;
            terminalBtn.Text = "Terminal";
            terminalBtn.UseVisualStyleBackColor = true;
            terminalBtn.Click += terminalBtn_Click;
            // 
            // drivePathBtn
            // 
            drivePathBtn.Location = new Point(309, 415);
            drivePathBtn.Name = "drivePathBtn";
            drivePathBtn.Size = new Size(94, 29);
            drivePathBtn.TabIndex = 23;
            drivePathBtn.Text = "Drive Path";
            drivePathBtn.UseVisualStyleBackColor = true;
            drivePathBtn.Click += drivePathBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(810, 485);
            Controls.Add(drivePathBtn);
            Controls.Add(terminalBtn);
            Controls.Add(graphicsBtn);
            Controls.Add(label3);
            Controls.Add(uartDropdown);
            Controls.Add(groupBox4);
            Controls.Add(nmiBtn);
            Controls.Add(irqBtn);
            Controls.Add(rstBtn);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(label1);
            Controls.Add(clockRateTB);
            Controls.Add(outputLbl);
            Controls.Add(loadRomBtn);
            Controls.Add(cycleBtn);
            Controls.Add(continueBtn);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(2);
            MaximizeBox = false;
            Name = "Form1";
            Text = "6502 Emulator";
            Load += Form1_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button continueBtn;
        private Button cycleBtn;
        private Button loadRomBtn;
        private Label cpuStatusLbl;
        private OpenFileDialog romFileDialog;
        private Label outputLbl;
        private TextBox clockRateTB;
        private Label label1;
        private System.Windows.Forms.Timer statusTimer;
        private Label memDisplay;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private TextBox scrollToTB;
        private Button scrollToBtn;
        private Label stackLbl;
        private GroupBox groupBox3;
        private Button rstBtn;
        private Button irqBtn;
        private Button nmiBtn;
        private GroupBox groupBox4;
        private Label label2;
        private ComboBox uartDropdown;
        private Label label3;
        private Button graphicsBtn;
        private Button terminalBtn;
        private Button drivePathBtn;
        private FolderBrowserDialog drivePathDialog;
    }
}
