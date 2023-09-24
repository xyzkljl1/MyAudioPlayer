namespace MyAudioPlayer
{
    partial class MainWindow
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
            this.UpPanel = new System.Windows.Forms.Panel();
            this.openWebButton = new System.Windows.Forms.Button();
            this.openLocalButton = new System.Windows.Forms.Button();
            this.sliderLabel = new System.Windows.Forms.Label();
            this.volumeSlider = new System.Windows.Forms.TrackBar();
            this.playSlider = new System.Windows.Forms.TrackBar();
            this.favButton = new System.Windows.Forms.Button();
            this.delButton = new System.Windows.Forms.Button();
            this.nextButton = new System.Windows.Forms.Button();
            this.prevButton = new System.Windows.Forms.Button();
            this.pauseButton = new System.Windows.Forms.Button();
            this.playButton = new System.Windows.Forms.Button();
            this.titleBox = new System.Windows.Forms.RichTextBox();
            this.DownPanel = new System.Windows.Forms.Panel();
            this.PlayListTab = new System.Windows.Forms.TabControl();
            this.UpPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.volumeSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.playSlider)).BeginInit();
            this.DownPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // UpPanel
            // 
            this.UpPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UpPanel.Controls.Add(this.openWebButton);
            this.UpPanel.Controls.Add(this.openLocalButton);
            this.UpPanel.Controls.Add(this.sliderLabel);
            this.UpPanel.Controls.Add(this.volumeSlider);
            this.UpPanel.Controls.Add(this.playSlider);
            this.UpPanel.Controls.Add(this.favButton);
            this.UpPanel.Controls.Add(this.delButton);
            this.UpPanel.Controls.Add(this.nextButton);
            this.UpPanel.Controls.Add(this.prevButton);
            this.UpPanel.Controls.Add(this.pauseButton);
            this.UpPanel.Controls.Add(this.playButton);
            this.UpPanel.Controls.Add(this.titleBox);
            this.UpPanel.Location = new System.Drawing.Point(12, 12);
            this.UpPanel.Name = "UpPanel";
            this.UpPanel.Size = new System.Drawing.Size(1286, 192);
            this.UpPanel.TabIndex = 0;
            // 
            // openWebButton
            // 
            this.openWebButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openWebButton.Location = new System.Drawing.Point(1112, 0);
            this.openWebButton.Name = "openWebButton";
            this.openWebButton.Size = new System.Drawing.Size(80, 60);
            this.openWebButton.TabIndex = 11;
            this.openWebButton.Text = "Web";
            this.openWebButton.UseVisualStyleBackColor = true;
            // 
            // openLocalButton
            // 
            this.openLocalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openLocalButton.Location = new System.Drawing.Point(1198, 0);
            this.openLocalButton.Name = "openLocalButton";
            this.openLocalButton.Size = new System.Drawing.Size(80, 60);
            this.openLocalButton.TabIndex = 10;
            this.openLocalButton.Text = "Local";
            this.openLocalButton.UseVisualStyleBackColor = true;
            // 
            // sliderLabel
            // 
            this.sliderLabel.AutoSize = true;
            this.sliderLabel.Location = new System.Drawing.Point(1024, 132);
            this.sliderLabel.Name = "sliderLabel";
            this.sliderLabel.Size = new System.Drawing.Size(40, 20);
            this.sliderLabel.TabIndex = 9;
            this.sliderLabel.Text = "0:00";
            // 
            // volumeSlider
            // 
            this.volumeSlider.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.volumeSlider.Location = new System.Drawing.Point(1092, 64);
            this.volumeSlider.Maximum = 100;
            this.volumeSlider.Name = "volumeSlider";
            this.volumeSlider.Size = new System.Drawing.Size(187, 56);
            this.volumeSlider.TabIndex = 8;
            // 
            // playSlider
            // 
            this.playSlider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playSlider.Location = new System.Drawing.Point(390, 132);
            this.playSlider.Maximum = 0;
            this.playSlider.Name = "playSlider";
            this.playSlider.Size = new System.Drawing.Size(628, 56);
            this.playSlider.TabIndex = 7;
            this.playSlider.TickFrequency = 60;
            // 
            // favButton
            // 
            this.favButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.favButton.Location = new System.Drawing.Point(1112, 127);
            this.favButton.Name = "favButton";
            this.favButton.Size = new System.Drawing.Size(80, 62);
            this.favButton.TabIndex = 6;
            this.favButton.Text = "Fav";
            this.favButton.UseVisualStyleBackColor = true;
            // 
            // delButton
            // 
            this.delButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.delButton.Location = new System.Drawing.Point(1198, 127);
            this.delButton.Name = "delButton";
            this.delButton.Size = new System.Drawing.Size(80, 60);
            this.delButton.TabIndex = 5;
            this.delButton.Text = "Del";
            this.delButton.UseVisualStyleBackColor = true;
            // 
            // nextButton
            // 
            this.nextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.nextButton.Location = new System.Drawing.Point(290, 127);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(94, 61);
            this.nextButton.TabIndex = 4;
            this.nextButton.Text = "Next";
            this.nextButton.UseVisualStyleBackColor = true;
            // 
            // prevButton
            // 
            this.prevButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.prevButton.Location = new System.Drawing.Point(190, 127);
            this.prevButton.Name = "prevButton";
            this.prevButton.Size = new System.Drawing.Size(94, 61);
            this.prevButton.TabIndex = 3;
            this.prevButton.Text = "Prev";
            this.prevButton.UseVisualStyleBackColor = true;
            // 
            // pauseButton
            // 
            this.pauseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pauseButton.Location = new System.Drawing.Point(103, 127);
            this.pauseButton.Name = "pauseButton";
            this.pauseButton.Size = new System.Drawing.Size(81, 61);
            this.pauseButton.TabIndex = 2;
            this.pauseButton.Text = "Pause";
            this.pauseButton.UseVisualStyleBackColor = true;
            // 
            // playButton
            // 
            this.playButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.playButton.Location = new System.Drawing.Point(3, 127);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(94, 62);
            this.playButton.TabIndex = 1;
            this.playButton.Text = "Play";
            this.playButton.UseVisualStyleBackColor = true;
            // 
            // titleBox
            // 
            this.titleBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titleBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.titleBox.Location = new System.Drawing.Point(7, 3);
            this.titleBox.Name = "titleBox";
            this.titleBox.ReadOnly = true;
            this.titleBox.Size = new System.Drawing.Size(1079, 118);
            this.titleBox.TabIndex = 0;
            this.titleBox.Text = "titleBox";
            // 
            // DownPanel
            // 
            this.DownPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DownPanel.Controls.Add(this.PlayListTab);
            this.DownPanel.Location = new System.Drawing.Point(12, 210);
            this.DownPanel.Name = "DownPanel";
            this.DownPanel.Size = new System.Drawing.Size(1286, 679);
            this.DownPanel.TabIndex = 1;
            // 
            // PlayListTab
            // 
            this.PlayListTab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PlayListTab.Location = new System.Drawing.Point(7, 3);
            this.PlayListTab.Name = "PlayListTab";
            this.PlayListTab.SelectedIndex = 0;
            this.PlayListTab.Size = new System.Drawing.Size(1276, 673);
            this.PlayListTab.TabIndex = 0;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1310, 901);
            this.Controls.Add(this.DownPanel);
            this.Controls.Add(this.UpPanel);
            this.Name = "MainWindow";
            this.Text = "万万静听";
            this.UpPanel.ResumeLayout(false);
            this.UpPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.volumeSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.playSlider)).EndInit();
            this.DownPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Panel UpPanel;
        private Panel DownPanel;
        private TabControl PlayListTab;
        private RichTextBox titleBox;
        private Button delButton;
        private Button nextButton;
        private Button prevButton;
        private Button pauseButton;
        private Button playButton;
        private Button favButton;
        private TrackBar volumeSlider;
        private TrackBar playSlider;
        private Label sliderLabel;
        private Button openWebButton;
        private Button openLocalButton;
    }
}