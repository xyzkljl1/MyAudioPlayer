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
            this.SelectCurrentButton = new System.Windows.Forms.Button();
            this.FavButton = new System.Windows.Forms.Button();
            this.OpenWebButton = new System.Windows.Forms.Button();
            this.OpenLocalButton = new System.Windows.Forms.Button();
            this.sliderLabel = new System.Windows.Forms.Label();
            this.volumeSlider = new System.Windows.Forms.TrackBar();
            this.playSlider = new System.Windows.Forms.TrackBar();
            this.DelButton = new System.Windows.Forms.Button();
            this.DelPartButton = new System.Windows.Forms.Button();
            this.NextButton = new System.Windows.Forms.Button();
            this.PrevButton = new System.Windows.Forms.Button();
            this.PauseButton = new System.Windows.Forms.Button();
            this.PlayButton = new System.Windows.Forms.Button();
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
            this.UpPanel.Controls.Add(this.SelectCurrentButton);
            this.UpPanel.Controls.Add(this.FavButton);
            this.UpPanel.Controls.Add(this.OpenWebButton);
            this.UpPanel.Controls.Add(this.OpenLocalButton);
            this.UpPanel.Controls.Add(this.sliderLabel);
            this.UpPanel.Controls.Add(this.volumeSlider);
            this.UpPanel.Controls.Add(this.playSlider);
            this.UpPanel.Controls.Add(this.DelButton);
            this.UpPanel.Controls.Add(this.DelPartButton);
            this.UpPanel.Controls.Add(this.NextButton);
            this.UpPanel.Controls.Add(this.PrevButton);
            this.UpPanel.Controls.Add(this.PauseButton);
            this.UpPanel.Controls.Add(this.PlayButton);
            this.UpPanel.Controls.Add(this.titleBox);
            this.UpPanel.Location = new System.Drawing.Point(12, 12);
            this.UpPanel.Name = "UpPanel";
            this.UpPanel.Size = new System.Drawing.Size(1286, 192);
            this.UpPanel.TabIndex = 0;
            // 
            // SelectCurrentButton
            // 
            this.SelectCurrentButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SelectCurrentButton.Location = new System.Drawing.Point(940, 127);
            this.SelectCurrentButton.Name = "SelectCurrentButton";
            this.SelectCurrentButton.Size = new System.Drawing.Size(80, 62);
            this.SelectCurrentButton.TabIndex = 13;
            this.SelectCurrentButton.Text = "选中播放的曲目";
            this.SelectCurrentButton.UseVisualStyleBackColor = true;
            // 
            // FavButton
            // 
            this.FavButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.FavButton.Location = new System.Drawing.Point(1026, 127);
            this.FavButton.Name = "FavButton";
            this.FavButton.Size = new System.Drawing.Size(80, 62);
            this.FavButton.TabIndex = 12;
            this.FavButton.Text = "Fav";
            this.FavButton.UseVisualStyleBackColor = true;
            // 
            // OpenWebButton
            // 
            this.OpenWebButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.OpenWebButton.Location = new System.Drawing.Point(1112, 0);
            this.OpenWebButton.Name = "OpenWebButton";
            this.OpenWebButton.Size = new System.Drawing.Size(80, 60);
            this.OpenWebButton.TabIndex = 11;
            this.OpenWebButton.Text = "Web";
            this.OpenWebButton.UseVisualStyleBackColor = true;
            // 
            // OpenLocalButton
            // 
            this.OpenLocalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.OpenLocalButton.Location = new System.Drawing.Point(1198, 0);
            this.OpenLocalButton.Name = "OpenLocalButton";
            this.OpenLocalButton.Size = new System.Drawing.Size(80, 60);
            this.OpenLocalButton.TabIndex = 10;
            this.OpenLocalButton.Text = "Local";
            this.OpenLocalButton.UseVisualStyleBackColor = true;
            // 
            // sliderLabel
            // 
            this.sliderLabel.AutoSize = true;
            this.sliderLabel.Location = new System.Drawing.Point(894, 132);
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
            this.playSlider.Size = new System.Drawing.Size(488, 56);
            this.playSlider.TabIndex = 7;
            this.playSlider.TickFrequency = 60;
            // 
            // DelButton
            // 
            this.DelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DelButton.Location = new System.Drawing.Point(1112, 127);
            this.DelButton.Name = "DelButton";
            this.DelButton.Size = new System.Drawing.Size(80, 62);
            this.DelButton.TabIndex = 6;
            this.DelButton.Text = "Del";
            this.DelButton.UseVisualStyleBackColor = true;
            // 
            // DelPartButton
            // 
            this.DelPartButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DelPartButton.Location = new System.Drawing.Point(1198, 127);
            this.DelPartButton.Name = "DelPartButton";
            this.DelPartButton.Size = new System.Drawing.Size(80, 62);
            this.DelPartButton.TabIndex = 5;
            this.DelPartButton.Text = "DelFile";
            this.DelPartButton.UseVisualStyleBackColor = true;
            // 
            // NextButton
            // 
            this.NextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.NextButton.Location = new System.Drawing.Point(290, 127);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(94, 61);
            this.NextButton.TabIndex = 4;
            this.NextButton.Text = "Next";
            this.NextButton.UseVisualStyleBackColor = true;
            // 
            // PrevButton
            // 
            this.PrevButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PrevButton.Location = new System.Drawing.Point(190, 127);
            this.PrevButton.Name = "PrevButton";
            this.PrevButton.Size = new System.Drawing.Size(94, 61);
            this.PrevButton.TabIndex = 3;
            this.PrevButton.Text = "Prev";
            this.PrevButton.UseVisualStyleBackColor = true;
            // 
            // PauseButton
            // 
            this.PauseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PauseButton.Location = new System.Drawing.Point(103, 127);
            this.PauseButton.Name = "PauseButton";
            this.PauseButton.Size = new System.Drawing.Size(81, 61);
            this.PauseButton.TabIndex = 2;
            this.PauseButton.Text = "Pause";
            this.PauseButton.UseVisualStyleBackColor = true;
            // 
            // PlayButton
            // 
            this.PlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PlayButton.Location = new System.Drawing.Point(3, 127);
            this.PlayButton.Name = "PlayButton";
            this.PlayButton.Size = new System.Drawing.Size(94, 62);
            this.PlayButton.TabIndex = 1;
            this.PlayButton.Text = "Play";
            this.PlayButton.UseVisualStyleBackColor = true;
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
            this.titleBox.Text = "";
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
        private Button DelPartButton;
        private Button NextButton;
        private Button PrevButton;
        private Button PauseButton;
        private Button PlayButton;
        private Button DelButton;
        private TrackBar volumeSlider;
        private TrackBar playSlider;
        private Label sliderLabel;
        private Button OpenWebButton;
        private Button OpenLocalButton;
        private Button FavButton;
        private Button SelectCurrentButton;
    }
}