namespace MinecraftModUpdater
{
    partial class ModUpdaterForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Button installJavaForgeButton;
        private System.Windows.Forms.Button adminInstallButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox consoleOutput;

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
            if (disposing)
            {
                modUpdater?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.updateButton = new System.Windows.Forms.Button();
            this.installJavaForgeButton = new System.Windows.Forms.Button();
            this.adminInstallButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.consoleOutput = new System.Windows.Forms.TextBox();
            this.SuspendLayout();

            // 
            // updateButton
            // 
            this.updateButton.BackColor = System.Drawing.Color.FromArgb(46, 204, 113);
            this.updateButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.updateButton.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.updateButton.ForeColor = System.Drawing.Color.White;
            this.updateButton.Location = new System.Drawing.Point(20, 20);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(120, 35);
            this.updateButton.TabIndex = 0;
            this.updateButton.Text = "Update Mods";
            this.updateButton.UseVisualStyleBackColor = false;
            this.updateButton.Click += new System.EventHandler(this.UpdateButton_Click);

            // 
            // installJavaForgeButton
            // 
            this.installJavaForgeButton.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.installJavaForgeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.installJavaForgeButton.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.installJavaForgeButton.ForeColor = System.Drawing.Color.White;
            this.installJavaForgeButton.Location = new System.Drawing.Point(150, 20);
            this.installJavaForgeButton.Name = "installJavaForgeButton";
            this.installJavaForgeButton.Size = new System.Drawing.Size(160, 35);
            this.installJavaForgeButton.TabIndex = 1;
            this.installJavaForgeButton.Text = "Install Java / Forge";
            this.installJavaForgeButton.UseVisualStyleBackColor = false;

            // 
            // adminInstallButton
            // 
            this.adminInstallButton.BackColor = System.Drawing.Color.FromArgb(255, 0, 0); // Purple
            this.adminInstallButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.adminInstallButton.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.adminInstallButton.ForeColor = System.Drawing.Color.White;
            this.adminInstallButton.Location = new System.Drawing.Point(320, 20);
            this.adminInstallButton.Name = "adminInstallButton";
            this.adminInstallButton.Size = new System.Drawing.Size(160, 35);
            this.adminInstallButton.TabIndex = 2;
            this.adminInstallButton.Text = "Admin Install Only";
            this.adminInstallButton.UseVisualStyleBackColor = false;
            this.adminInstallButton.Click += new System.EventHandler(this.OnAdminInstallButtonClicked);


            // 
            // statusLabel
            // 
            this.statusLabel.Font = new System.Drawing.Font("Arial", 9F);
            this.statusLabel.ForeColor = System.Drawing.Color.White;
            this.statusLabel.Location = new System.Drawing.Point(20, 60);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(540, 20);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "Ready to update mods";

            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 90);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(540, 25);
            this.progressBar.TabIndex = 4;

            // 
            // consoleOutput
            // 
            this.consoleOutput.BackColor = System.Drawing.Color.Black;
            this.consoleOutput.Font = new System.Drawing.Font("Consolas", 9F);
            this.consoleOutput.ForeColor = System.Drawing.Color.Lime;
            this.consoleOutput.Location = new System.Drawing.Point(20, 130);
            this.consoleOutput.Multiline = true;
            this.consoleOutput.Name = "consoleOutput";
            this.consoleOutput.ReadOnly = true;
            this.consoleOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleOutput.Size = new System.Drawing.Size(540, 300);
            this.consoleOutput.TabIndex = 5;
            this.consoleOutput.Text = "=== Mod Updater Console ===\r\n";

            // 
            // ModUpdaterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 461);
            this.Controls.Add(this.consoleOutput);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.adminInstallButton);
            this.Controls.Add(this.installJavaForgeButton);
            this.Controls.Add(this.updateButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ModUpdaterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Shadows Mod Updater";
            this.BackColor = System.Drawing.Color.FromArgb(40, 0, 60); // Dark purple shade
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
