namespace DataTanker.Examples.WinForms
{
    partial class MainForm
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
            this.btnStartGeneration = new System.Windows.Forms.Button();
            this.nudStartKey = new System.Windows.Forms.NumericUpDown();
            this.nudKeyCount = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nudKey = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.btnGet = new System.Windows.Forms.Button();
            this.tbValue = new System.Windows.Forms.TextBox();
            this.btnSet = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.btnRemove = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblState = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartKey)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudKeyCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudKey)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartGeneration
            // 
            this.btnStartGeneration.Location = new System.Drawing.Point(189, 16);
            this.btnStartGeneration.Name = "btnStartGeneration";
            this.btnStartGeneration.Size = new System.Drawing.Size(67, 23);
            this.btnStartGeneration.TabIndex = 0;
            this.btnStartGeneration.Text = "Start";
            this.btnStartGeneration.UseVisualStyleBackColor = true;
            this.btnStartGeneration.Click += new System.EventHandler(this.btnStartGeneration_Click);
            // 
            // nudStartKey
            // 
            this.nudStartKey.Location = new System.Drawing.Point(63, 19);
            this.nudStartKey.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.nudStartKey.Name = "nudStartKey";
            this.nudStartKey.Size = new System.Drawing.Size(120, 20);
            this.nudStartKey.TabIndex = 1;
            // 
            // nudKeyCount
            // 
            this.nudKeyCount.Location = new System.Drawing.Point(63, 45);
            this.nudKeyCount.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.nudKeyCount.Name = "nudKeyCount";
            this.nudKeyCount.Size = new System.Drawing.Size(120, 20);
            this.nudKeyCount.TabIndex = 2;
            this.nudKeyCount.ThousandsSeparator = true;
            this.nudKeyCount.Value = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Count";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Start key";
            // 
            // nudKey
            // 
            this.nudKey.Location = new System.Drawing.Point(63, 19);
            this.nudKey.Maximum = new decimal(new int[] {
            2000000000,
            0,
            0,
            0});
            this.nudKey.Name = "nudKey";
            this.nudKey.Size = new System.Drawing.Size(120, 20);
            this.nudKey.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(32, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Key";
            // 
            // btnGet
            // 
            this.btnGet.Location = new System.Drawing.Point(189, 16);
            this.btnGet.Name = "btnGet";
            this.btnGet.Size = new System.Drawing.Size(67, 23);
            this.btnGet.TabIndex = 7;
            this.btnGet.Text = "Get";
            this.btnGet.UseVisualStyleBackColor = true;
            this.btnGet.Click += new System.EventHandler(this.btnGet_Click);
            // 
            // tbValue
            // 
            this.tbValue.Location = new System.Drawing.Point(62, 45);
            this.tbValue.Multiline = true;
            this.tbValue.Name = "tbValue";
            this.tbValue.ShortcutsEnabled = false;
            this.tbValue.Size = new System.Drawing.Size(331, 56);
            this.tbValue.TabIndex = 8;
            // 
            // btnSet
            // 
            this.btnSet.Location = new System.Drawing.Point(262, 16);
            this.btnSet.Name = "btnSet";
            this.btnSet.Size = new System.Drawing.Size(63, 23);
            this.btnSet.TabIndex = 9;
            this.btnSet.Text = "Set";
            this.btnSet.UseVisualStyleBackColor = true;
            this.btnSet.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Value";
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(331, 16);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(62, 23);
            this.btnRemove.TabIndex = 11;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblState);
            this.groupBox1.Controls.Add(this.nudStartKey);
            this.groupBox1.Controls.Add(this.btnStartGeneration);
            this.groupBox1.Controls.Add(this.nudKeyCount);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(409, 105);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Generate random values";
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(8, 79);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(16, 13);
            this.lblState.TabIndex = 12;
            this.lblState.Text = "   ";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tbValue);
            this.groupBox2.Controls.Add(this.nudKey);
            this.groupBox2.Controls.Add(this.btnRemove);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.btnGet);
            this.groupBox2.Controls.Add(this.btnSet);
            this.groupBox2.Location = new System.Drawing.Point(12, 123);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(409, 115);
            this.groupBox2.TabIndex = 13;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Operations";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 246);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "DataTanker Windows Forms Example";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.nudStartKey)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudKeyCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudKey)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnStartGeneration;
        private System.Windows.Forms.NumericUpDown nudStartKey;
        private System.Windows.Forms.NumericUpDown nudKeyCount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudKey;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnGet;
        private System.Windows.Forms.TextBox tbValue;
        private System.Windows.Forms.Button btnSet;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblState;
    }
}

