using Diagram.View.MSAGL;

namespace DsWebApp.Simulatior
{
    partial class FormDocViewSim
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDocViewSim));
            tabControl = new System.Windows.Forms.TabControl();
            button4 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            button_Play = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            button_Reset = new System.Windows.Forms.Button();
            comboBox_Real = new System.Windows.Forms.ComboBox();
            button_Start = new System.Windows.Forms.Button();
            panel1 = new System.Windows.Forms.Panel();
            label_log = new System.Windows.Forms.Label();
            button_Step = new System.Windows.Forms.Button();
            button_Pause = new System.Windows.Forms.Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Alignment = System.Windows.Forms.TabAlignment.Left;
            tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            tabControl.Location = new System.Drawing.Point(0, 0);
            tabControl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabControl.Multiline = true;
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new System.Drawing.Size(721, 779);
            tabControl.TabIndex = 5;
            tabControl.SelectedIndexChanged += tabControl_SelectedIndexChanged;
            // 
            // button4
            // 
            button4.BackColor = System.Drawing.Color.RoyalBlue;
            button4.Enabled = false;
            button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button4.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button4.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            button4.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button4.Location = new System.Drawing.Point(575, 8);
            button4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(72, 30);
            button4.TabIndex = 5;
            button4.Text = "종료";
            button4.UseVisualStyleBackColor = false;
            // 
            // button3
            // 
            button3.BackColor = System.Drawing.Color.Goldenrod;
            button3.Enabled = false;
            button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button3.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button3.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            button3.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button3.Location = new System.Drawing.Point(508, 8);
            button3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(72, 30);
            button3.TabIndex = 5;
            button3.Text = "진행";
            button3.UseVisualStyleBackColor = false;
            // 
            // button2
            // 
            button2.BackColor = System.Drawing.SystemColors.ActiveBorder;
            button2.Enabled = false;
            button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button2.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button2.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            button2.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button2.Location = new System.Drawing.Point(642, 8);
            button2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(72, 30);
            button2.TabIndex = 5;
            button2.Text = "복귀";
            button2.UseVisualStyleBackColor = false;
            // 
            // button1
            // 
            button1.BackColor = System.Drawing.Color.DarkGreen;
            button1.Enabled = false;
            button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button1.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button1.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            button1.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button1.Location = new System.Drawing.Point(441, 8);
            button1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(72, 30);
            button1.TabIndex = 5;
            button1.Text = "준비";
            button1.UseVisualStyleBackColor = false;
            // 
            // button_Play
            // 
            button_Play.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button_Play.Image = (System.Drawing.Image)resources.GetObject("button_Play.Image");
            button_Play.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button_Play.Location = new System.Drawing.Point(15, 14);
            button_Play.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button_Play.Name = "button_Play";
            button_Play.Size = new System.Drawing.Size(58, 51);
            button_Play.TabIndex = 4;
            button_Play.Text = "PLAY";
            button_Play.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            button_Play.UseVisualStyleBackColor = true;
            button_Play.Click += button_Play_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("맑은 고딕", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            label1.ForeColor = System.Drawing.Color.CornflowerBlue;
            label1.Location = new System.Drawing.Point(434, 37);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(284, 37);
            label1.TabIndex = 3;
            label1.Text = "Dualsoft® Simulation";
            // 
            // button_Reset
            // 
            button_Reset.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button_Reset.Image = (System.Drawing.Image)resources.GetObject("button_Reset.Image");
            button_Reset.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            button_Reset.Location = new System.Drawing.Point(332, 41);
            button_Reset.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button_Reset.Name = "button_Reset";
            button_Reset.Size = new System.Drawing.Size(103, 27);
            button_Reset.TabIndex = 2;
            button_Reset.Text = "Work Reset";
            button_Reset.UseVisualStyleBackColor = true;
            button_Reset.Click += button_Reset_Click;
            // 
            // comboBox_Real
            // 
            comboBox_Real.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBox_Real.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            comboBox_Real.FormattingEnabled = true;
            comboBox_Real.Location = new System.Drawing.Point(225, 10);
            comboBox_Real.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            comboBox_Real.Name = "comboBox_Real";
            comboBox_Real.Size = new System.Drawing.Size(210, 25);
            comboBox_Real.TabIndex = 6;
            // 
            // button_Start
            // 
            button_Start.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button_Start.Image = (System.Drawing.Image)resources.GetObject("button_Start.Image");
            button_Start.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            button_Start.Location = new System.Drawing.Point(225, 41);
            button_Start.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button_Start.Name = "button_Start";
            button_Start.Size = new System.Drawing.Size(101, 27);
            button_Start.TabIndex = 0;
            button_Start.Text = "Work Start";
            button_Start.UseVisualStyleBackColor = true;
            button_Start.Click += button_Start_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(button4);
            panel1.Controls.Add(comboBox_Real);
            panel1.Controls.Add(button3);
            panel1.Controls.Add(button_Start);
            panel1.Controls.Add(button2);
            panel1.Controls.Add(button_Reset);
            panel1.Controls.Add(button1);
            panel1.Controls.Add(label_log);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(button_Play);
            panel1.Controls.Add(button_Step);
            panel1.Controls.Add(button_Pause);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 690);
            panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(721, 89);
            panel1.TabIndex = 6;
            // 
            // label_log
            // 
            label_log.AutoSize = true;
            label_log.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            label_log.ForeColor = System.Drawing.Color.Teal;
            label_log.Location = new System.Drawing.Point(18, 68);
            label_log.Name = "label_log";
            label_log.Size = new System.Drawing.Size(52, 15);
            label_log.TabIndex = 3;
            label_log.Text = "Last Log";
            // 
            // button_Step
            // 
            button_Step.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button_Step.Image = (System.Drawing.Image)resources.GetObject("button_Step.Image");
            button_Step.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button_Step.Location = new System.Drawing.Point(145, 14);
            button_Step.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button_Step.Name = "button_Step";
            button_Step.Size = new System.Drawing.Size(58, 51);
            button_Step.TabIndex = 1;
            button_Step.Text = "STEP";
            button_Step.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            button_Step.UseVisualStyleBackColor = true;
            button_Step.Click += button_Step_Click;
            // 
            // button_Pause
            // 
            button_Pause.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            button_Pause.Image = (System.Drawing.Image)resources.GetObject("button_Pause.Image");
            button_Pause.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            button_Pause.Location = new System.Drawing.Point(80, 14);
            button_Pause.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button_Pause.Name = "button_Pause";
            button_Pause.Size = new System.Drawing.Size(58, 51);
            button_Pause.TabIndex = 5;
            button_Pause.Text = "PAUSE";
            button_Pause.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            button_Pause.UseVisualStyleBackColor = true;
            button_Pause.Click += button_Pause_Click;
            // 
            // FormDocViewSim
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(721, 779);
            Controls.Add(panel1);
            Controls.Add(tabControl);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "FormDocViewSim";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Dualsoft Language  Graph";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.ComboBox comboBox_Real;
        private System.Windows.Forms.Button button_Start;
        private System.Windows.Forms.Button button_Reset;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_Play;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button_Pause;
        private System.Windows.Forms.Button button_Step;
        private System.Windows.Forms.Label label_log;
    }
}