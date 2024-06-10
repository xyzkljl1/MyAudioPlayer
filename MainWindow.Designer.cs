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
            DownPanel = new Panel();
            PlayListTab = new TabControl();
            MiddlePanel = new Panel();
            SelectCurrentButton = new Button();
            PrevButton = new Button();
            FavButton = new Button();
            PlayButton = new Button();
            NextButton = new Button();
            DelPartButton = new Button();
            sliderLabel = new Label();
            DelButton = new Button();
            playSlider = new TrackBar();
            UpPanel = new Panel();
            OpenWebButton = new Button();
            OpenLocalButton = new Button();
            volumeSlider = new TrackBar();
            titleBox = new RichTextBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            DownPanel.SuspendLayout();
            MiddlePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)playSlider).BeginInit();
            UpPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)volumeSlider).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // DownPanel
            // 
            DownPanel.Controls.Add(PlayListTab);
            DownPanel.Dock = DockStyle.Fill;
            DownPanel.Location = new Point(4, 378);
            DownPanel.Margin = new Padding(4);
            DownPanel.MinimumSize = new Size(0, 500);
            DownPanel.Name = "DownPanel";
            DownPanel.Size = new Size(1996, 668);
            DownPanel.TabIndex = 1;
            // 
            // PlayListTab
            // 
            PlayListTab.Dock = DockStyle.Fill;
            PlayListTab.Location = new Point(0, 0);
            PlayListTab.Margin = new Padding(4);
            PlayListTab.Name = "PlayListTab";
            PlayListTab.SelectedIndex = 0;
            PlayListTab.Size = new Size(1996, 668);
            PlayListTab.TabIndex = 0;
            // 
            // MiddlePanel
            // 
            MiddlePanel.Controls.Add(SelectCurrentButton);
            MiddlePanel.Controls.Add(PrevButton);
            MiddlePanel.Controls.Add(FavButton);
            MiddlePanel.Controls.Add(PlayButton);
            MiddlePanel.Controls.Add(NextButton);
            MiddlePanel.Controls.Add(DelPartButton);
            MiddlePanel.Controls.Add(sliderLabel);
            MiddlePanel.Controls.Add(DelButton);
            MiddlePanel.Controls.Add(playSlider);
            MiddlePanel.Dock = DockStyle.Fill;
            MiddlePanel.Location = new Point(3, 235);
            MiddlePanel.Name = "MiddlePanel";
            MiddlePanel.Size = new Size(1998, 136);
            MiddlePanel.TabIndex = 2;
            // 
            // SelectCurrentButton
            // 
            SelectCurrentButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            SelectCurrentButton.Location = new Point(1500, 4);
            SelectCurrentButton.Margin = new Padding(4);
            SelectCurrentButton.Name = "SelectCurrentButton";
            SelectCurrentButton.Size = new Size(116, 87);
            SelectCurrentButton.TabIndex = 13;
            SelectCurrentButton.Text = "选中播放的曲目";
            SelectCurrentButton.UseVisualStyleBackColor = true;
            // 
            // PrevButton
            // 
            PrevButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            PrevButton.Location = new Point(147, 8);
            PrevButton.Margin = new Padding(4);
            PrevButton.Name = "PrevButton";
            PrevButton.Size = new Size(136, 85);
            PrevButton.TabIndex = 3;
            PrevButton.Text = "Prev";
            PrevButton.UseVisualStyleBackColor = true;
            // 
            // FavButton
            // 
            FavButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            FavButton.Location = new Point(1624, 4);
            FavButton.Margin = new Padding(4);
            FavButton.Name = "FavButton";
            FavButton.Size = new Size(116, 87);
            FavButton.TabIndex = 12;
            FavButton.Text = "FavCur";
            FavButton.UseVisualStyleBackColor = true;
            // 
            // PlayButton
            // 
            PlayButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            PlayButton.Location = new Point(4, 8);
            PlayButton.Margin = new Padding(4);
            PlayButton.Name = "PlayButton";
            PlayButton.Size = new Size(136, 87);
            PlayButton.TabIndex = 1;
            PlayButton.Text = "Play";
            PlayButton.UseVisualStyleBackColor = true;
            // 
            // NextButton
            // 
            NextButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            NextButton.Location = new Point(291, 8);
            NextButton.Margin = new Padding(4);
            NextButton.Name = "NextButton";
            NextButton.Size = new Size(136, 85);
            NextButton.TabIndex = 4;
            NextButton.Text = "Next";
            NextButton.UseVisualStyleBackColor = true;
            // 
            // DelPartButton
            // 
            DelPartButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            DelPartButton.Location = new Point(1872, 4);
            DelPartButton.Margin = new Padding(4);
            DelPartButton.Name = "DelPartButton";
            DelPartButton.Size = new Size(116, 87);
            DelPartButton.TabIndex = 5;
            DelPartButton.Text = "DelPartCur";
            DelPartButton.UseVisualStyleBackColor = true;
            // 
            // sliderLabel
            // 
            sliderLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sliderLabel.AutoSize = true;
            sliderLabel.Location = new Point(1377, 8);
            sliderLabel.Margin = new Padding(4, 0, 4, 0);
            sliderLabel.Name = "sliderLabel";
            sliderLabel.Size = new Size(115, 28);
            sliderLabel.TabIndex = 9;
            sliderLabel.Text = "0:00/00:00";
            // 
            // DelButton
            // 
            DelButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            DelButton.Location = new Point(1748, 4);
            DelButton.Margin = new Padding(4);
            DelButton.Name = "DelButton";
            DelButton.Size = new Size(116, 87);
            DelButton.TabIndex = 6;
            DelButton.Text = "DelCur";
            DelButton.UseVisualStyleBackColor = true;
            // 
            // playSlider
            // 
            playSlider.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            playSlider.Location = new Point(435, 13);
            playSlider.Margin = new Padding(4);
            playSlider.Maximum = 0;
            playSlider.Name = "playSlider";
            playSlider.Size = new Size(934, 80);
            playSlider.TabIndex = 7;
            playSlider.TickFrequency = 60;
            // 
            // UpPanel
            // 
            UpPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            UpPanel.Controls.Add(OpenWebButton);
            UpPanel.Controls.Add(OpenLocalButton);
            UpPanel.Controls.Add(volumeSlider);
            UpPanel.Controls.Add(titleBox);
            UpPanel.Location = new Point(4, 43);
            UpPanel.Margin = new Padding(4);
            UpPanel.MinimumSize = new Size(100, 100);
            UpPanel.Name = "UpPanel";
            UpPanel.Size = new Size(1996, 146);
            UpPanel.TabIndex = 0;
            // 
            // OpenWebButton
            // 
            OpenWebButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenWebButton.Location = new Point(1671, 0);
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
            OpenLocalButton.Location = new Point(1795, 0);
            OpenLocalButton.Margin = new Padding(4);
            OpenLocalButton.Name = "OpenLocalButton";
            OpenLocalButton.Size = new Size(116, 84);
            OpenLocalButton.TabIndex = 10;
            OpenLocalButton.Text = "Local";
            OpenLocalButton.UseVisualStyleBackColor = true;
            // 
            // volumeSlider
            // 
            volumeSlider.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            volumeSlider.Location = new Point(1671, 62);
            volumeSlider.Margin = new Padding(4);
            volumeSlider.Maximum = 100;
            volumeSlider.Name = "volumeSlider";
            volumeSlider.Size = new Size(248, 80);
            volumeSlider.TabIndex = 8;
            // 
            // titleBox
            // 
            titleBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            titleBox.BorderStyle = BorderStyle.FixedSingle;
            titleBox.Location = new Point(10, 4);
            titleBox.Margin = new Padding(4);
            titleBox.Name = "titleBox";
            titleBox.ReadOnly = true;
            titleBox.Size = new Size(1727, 126);
            titleBox.TabIndex = 0;
            titleBox.Text = "";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(UpPanel, 0, 0);
            tableLayoutPanel1.Controls.Add(DownPanel, 0, 2);
            tableLayoutPanel1.Controls.Add(MiddlePanel, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 62.0320854F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 37.9679146F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 675F));
            tableLayoutPanel1.Size = new Size(2004, 1050);
            tableLayoutPanel1.TabIndex = 3;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2004, 1050);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4);
            Name = "MainWindow";
            Text = "万万静听";
            DownPanel.ResumeLayout(false);
            MiddlePanel.ResumeLayout(false);
            MiddlePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)playSlider).EndInit();
            UpPanel.ResumeLayout(false);
            UpPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)volumeSlider).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel DownPanel;
        private TabControl PlayListTab;
        private Panel MiddlePanel;
        private Button SelectCurrentButton;
        private Button PrevButton;
        private Button FavButton;
        private Button PlayButton;
        private Button NextButton;
        private Button DelPartButton;
        private Label sliderLabel;
        private Button DelButton;
        private TrackBar playSlider;
        private Panel UpPanel;
        private Button OpenWebButton;
        private Button OpenLocalButton;
        private TrackBar volumeSlider;
        private RichTextBox titleBox;
        private TableLayoutPanel tableLayoutPanel1;
    }
}