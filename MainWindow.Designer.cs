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
            UpPanel = new Panel();
            SelectCurrentButton = new Button();
            FavButton = new Button();
            OpenWebButton = new Button();
            OpenLocalButton = new Button();
            sliderLabel = new Label();
            volumeSlider = new TrackBar();
            playSlider = new TrackBar();
            DelButton = new Button();
            DelPartButton = new Button();
            NextButton = new Button();
            PrevButton = new Button();
            PlayButton = new Button();
            titleBox = new RichTextBox();
            DownPanel = new Panel();
            PlayListTab = new TabControl();
            UpPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)volumeSlider).BeginInit();
            ((System.ComponentModel.ISupportInitialize)playSlider).BeginInit();
            DownPanel.SuspendLayout();
            SuspendLayout();
            // 
            // UpPanel
            // 
            UpPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            UpPanel.Controls.Add(SelectCurrentButton);
            UpPanel.Controls.Add(FavButton);
            UpPanel.Controls.Add(OpenWebButton);
            UpPanel.Controls.Add(OpenLocalButton);
            UpPanel.Controls.Add(sliderLabel);
            UpPanel.Controls.Add(volumeSlider);
            UpPanel.Controls.Add(playSlider);
            UpPanel.Controls.Add(DelButton);
            UpPanel.Controls.Add(DelPartButton);
            UpPanel.Controls.Add(NextButton);
            UpPanel.Controls.Add(PrevButton);
            UpPanel.Controls.Add(PlayButton);
            UpPanel.Controls.Add(titleBox);
            UpPanel.Location = new Point(17, 17);
            UpPanel.Margin = new Padding(4);
            UpPanel.Name = "UpPanel";
            UpPanel.Size = new Size(1858, 269);
            UpPanel.TabIndex = 0;
            // 
            // SelectCurrentButton
            // 
            SelectCurrentButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            SelectCurrentButton.Location = new Point(1358, 178);
            SelectCurrentButton.Margin = new Padding(4);
            SelectCurrentButton.Name = "SelectCurrentButton";
            SelectCurrentButton.Size = new Size(116, 87);
            SelectCurrentButton.TabIndex = 13;
            SelectCurrentButton.Text = "选中播放的曲目";
            SelectCurrentButton.UseVisualStyleBackColor = true;
            // 
            // FavButton
            // 
            FavButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            FavButton.Location = new Point(1482, 178);
            FavButton.Margin = new Padding(4);
            FavButton.Name = "FavButton";
            FavButton.Size = new Size(116, 87);
            FavButton.TabIndex = 12;
            FavButton.Text = "FavCur";
            FavButton.UseVisualStyleBackColor = true;
            // 
            // OpenWebButton
            // 
            OpenWebButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenWebButton.Location = new Point(1606, 0);
            OpenWebButton.Margin = new Padding(4);
            OpenWebButton.Name = "OpenWebButton";
            OpenWebButton.Size = new Size(116, 84);
            OpenWebButton.TabIndex = 11;
            OpenWebButton.Text = "Web";
            OpenWebButton.UseVisualStyleBackColor = true;
            // 
            // OpenLocalButton
            // 
            OpenLocalButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenLocalButton.Location = new Point(1730, 0);
            OpenLocalButton.Margin = new Padding(4);
            OpenLocalButton.Name = "OpenLocalButton";
            OpenLocalButton.Size = new Size(116, 84);
            OpenLocalButton.TabIndex = 10;
            OpenLocalButton.Text = "Local";
            OpenLocalButton.UseVisualStyleBackColor = true;
            // 
            // sliderLabel
            // 
            sliderLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sliderLabel.AutoSize = true;
            sliderLabel.Location = new Point(1235, 176);
            sliderLabel.Margin = new Padding(4, 0, 4, 0);
            sliderLabel.Name = "sliderLabel";
            sliderLabel.Size = new Size(115, 28);
            sliderLabel.TabIndex = 9;
            sliderLabel.Text = "0:00/00:00";
            // 
            // volumeSlider
            // 
            volumeSlider.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            volumeSlider.Location = new Point(1577, 90);
            volumeSlider.Margin = new Padding(4);
            volumeSlider.Maximum = 100;
            volumeSlider.Name = "volumeSlider";
            volumeSlider.Size = new Size(270, 80);
            volumeSlider.TabIndex = 8;
            // 
            // playSlider
            // 
            playSlider.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            playSlider.Location = new Point(436, 178);
            playSlider.Margin = new Padding(4);
            playSlider.Maximum = 0;
            playSlider.Name = "playSlider";
            playSlider.Size = new Size(791, 80);
            playSlider.TabIndex = 7;
            playSlider.TickFrequency = 60;
            // 
            // DelButton
            // 
            DelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            DelButton.Location = new Point(1606, 178);
            DelButton.Margin = new Padding(4);
            DelButton.Name = "DelButton";
            DelButton.Size = new Size(116, 87);
            DelButton.TabIndex = 6;
            DelButton.Text = "DelCur";
            DelButton.UseVisualStyleBackColor = true;
            // 
            // DelPartButton
            // 
            DelPartButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            DelPartButton.Location = new Point(1730, 178);
            DelPartButton.Margin = new Padding(4);
            DelPartButton.Name = "DelPartButton";
            DelPartButton.Size = new Size(116, 87);
            DelPartButton.TabIndex = 5;
            DelPartButton.Text = "DelPartCur";
            DelPartButton.UseVisualStyleBackColor = true;
            // 
            // NextButton
            // 
            NextButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            NextButton.Location = new Point(292, 180);
            NextButton.Margin = new Padding(4);
            NextButton.Name = "NextButton";
            NextButton.Size = new Size(136, 85);
            NextButton.TabIndex = 4;
            NextButton.Text = "Next";
            NextButton.UseVisualStyleBackColor = true;
            // 
            // PrevButton
            // 
            PrevButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            PrevButton.Location = new Point(148, 179);
            PrevButton.Margin = new Padding(4);
            PrevButton.Name = "PrevButton";
            PrevButton.Size = new Size(136, 85);
            PrevButton.TabIndex = 3;
            PrevButton.Text = "Prev";
            PrevButton.UseVisualStyleBackColor = true;
            // 
            // PlayButton
            // 
            PlayButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            PlayButton.Location = new Point(4, 178);
            PlayButton.Margin = new Padding(4);
            PlayButton.Name = "PlayButton";
            PlayButton.Size = new Size(136, 87);
            PlayButton.TabIndex = 1;
            PlayButton.Text = "Play";
            PlayButton.UseVisualStyleBackColor = true;
            // 
            // titleBox
            // 
            titleBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            titleBox.BorderStyle = BorderStyle.FixedSingle;
            titleBox.Location = new Point(10, 4);
            titleBox.Margin = new Padding(4);
            titleBox.Name = "titleBox";
            titleBox.ReadOnly = true;
            titleBox.Size = new Size(1557, 164);
            titleBox.TabIndex = 0;
            titleBox.Text = "";
            // 
            // DownPanel
            // 
            DownPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            DownPanel.Controls.Add(PlayListTab);
            DownPanel.Location = new Point(17, 294);
            DownPanel.Margin = new Padding(4);
            DownPanel.Name = "DownPanel";
            DownPanel.Size = new Size(1858, 951);
            DownPanel.TabIndex = 1;
            // 
            // PlayListTab
            // 
            PlayListTab.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PlayListTab.Location = new Point(10, 4);
            PlayListTab.Margin = new Padding(4);
            PlayListTab.Name = "PlayListTab";
            PlayListTab.SelectedIndex = 0;
            PlayListTab.Size = new Size(1843, 942);
            PlayListTab.TabIndex = 0;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1892, 1261);
            Controls.Add(DownPanel);
            Controls.Add(UpPanel);
            Margin = new Padding(4);
            Name = "MainWindow";
            Text = "万万静听";
            UpPanel.ResumeLayout(false);
            UpPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)volumeSlider).EndInit();
            ((System.ComponentModel.ISupportInitialize)playSlider).EndInit();
            DownPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel UpPanel;
        private Panel DownPanel;
        private TabControl PlayListTab;
        private RichTextBox titleBox;
        private Button DelPartButton;
        private Button NextButton;
        private Button PrevButton;
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