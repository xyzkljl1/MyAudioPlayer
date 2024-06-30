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
            components = new System.ComponentModel.Container();
            DownPanel = new Panel();
            PlayListTab = new TabControl();
            MiddlePanel = new Panel();
            MiddlePanelFlowLayoutPanel = new FlowLayoutPanel();
            DelPartButton = new Button();
            DelButton = new Button();
            FavButton = new Button();
            SelectCurrentButton = new Button();
            sliderLabel = new Label();
            PrevButton = new Button();
            PlayButton = new Button();
            NextButton = new Button();
            playSlider = new TrackBar();
            UpPanel = new Panel();
            OpenWebButton = new Button();
            OpenLocalButton = new Button();
            volumeSlider = new TrackBar();
            titleBox = new RichTextBox();
            mainTableLayoutPanel = new TableLayoutPanel();
            toolTip = new ToolTip(components);
            DownPanel.SuspendLayout();
            MiddlePanel.SuspendLayout();
            MiddlePanelFlowLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)playSlider).BeginInit();
            UpPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)volumeSlider).BeginInit();
            mainTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // DownPanel
            // 
            DownPanel.Controls.Add(PlayListTab);
            DownPanel.Dock = DockStyle.Fill;
            DownPanel.Location = new Point(4, 295);
            DownPanel.Margin = new Padding(4);
            DownPanel.MinimumSize = new Size(0, 500);
            DownPanel.Name = "DownPanel";
            DownPanel.Size = new Size(1365, 701);
            DownPanel.TabIndex = 1;
            // 
            // PlayListTab
            // 
            PlayListTab.Dock = DockStyle.Fill;
            PlayListTab.Location = new Point(0, 0);
            PlayListTab.Margin = new Padding(4);
            PlayListTab.Name = "PlayListTab";
            PlayListTab.SelectedIndex = 0;
            PlayListTab.Size = new Size(1365, 701);
            PlayListTab.TabIndex = 0;
            // 
            // MiddlePanel
            // 
            MiddlePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            MiddlePanel.CausesValidation = false;
            MiddlePanel.Controls.Add(MiddlePanelFlowLayoutPanel);
            MiddlePanel.Controls.Add(PrevButton);
            MiddlePanel.Controls.Add(PlayButton);
            MiddlePanel.Controls.Add(NextButton);
            MiddlePanel.Controls.Add(playSlider);
            MiddlePanel.Location = new Point(3, 193);
            MiddlePanel.Name = "MiddlePanel";
            MiddlePanel.Size = new Size(1367, 95);
            MiddlePanel.TabIndex = 2;
            MiddlePanel.DoubleClick += onMainWindowMouseDoubleClick;
            MiddlePanel.MouseDown += onMainWindowMouseDown;
            // 
            // MiddlePanelFlowLayoutPanel
            // 
            MiddlePanelFlowLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            MiddlePanelFlowLayoutPanel.AutoSize = true;
            MiddlePanelFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MiddlePanelFlowLayoutPanel.Controls.Add(DelPartButton);
            MiddlePanelFlowLayoutPanel.Controls.Add(DelButton);
            MiddlePanelFlowLayoutPanel.Controls.Add(FavButton);
            MiddlePanelFlowLayoutPanel.Controls.Add(SelectCurrentButton);
            MiddlePanelFlowLayoutPanel.Controls.Add(sliderLabel);
            MiddlePanelFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
            MiddlePanelFlowLayoutPanel.Location = new Point(870, 3);
            MiddlePanelFlowLayoutPanel.Margin = new Padding(0);
            MiddlePanelFlowLayoutPanel.Name = "MiddlePanelFlowLayoutPanel";
            MiddlePanelFlowLayoutPanel.Size = new Size(491, 95);
            MiddlePanelFlowLayoutPanel.TabIndex = 14;
            MiddlePanelFlowLayoutPanel.WrapContents = false;
            MiddlePanelFlowLayoutPanel.DoubleClick += onMainWindowMouseDoubleClick;
            MiddlePanelFlowLayoutPanel.MouseDown += onMainWindowMouseDown;
            // 
            // DelPartButton
            // 
            DelPartButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            DelPartButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            DelPartButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Underline, GraphicsUnit.Point);
            DelPartButton.Location = new Point(403, 4);
            DelPartButton.Margin = new Padding(4);
            DelPartButton.Name = "DelPartButton";
            DelPartButton.Size = new Size(84, 87);
            DelPartButton.TabIndex = 5;
            DelPartButton.Text = "␡";
            DelPartButton.UseVisualStyleBackColor = true;
            // 
            // DelButton
            // 
            DelButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            DelButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            DelButton.Location = new Point(311, 4);
            DelButton.Margin = new Padding(4);
            DelButton.Name = "DelButton";
            DelButton.Size = new Size(84, 87);
            DelButton.TabIndex = 6;
            DelButton.Text = "␡";
            DelButton.UseVisualStyleBackColor = true;
            // 
            // FavButton
            // 
            FavButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            FavButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FavButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            FavButton.Location = new Point(219, 4);
            FavButton.Margin = new Padding(4);
            FavButton.Name = "FavButton";
            FavButton.Size = new Size(84, 87);
            FavButton.TabIndex = 12;
            FavButton.Text = "♥";
            FavButton.UseVisualStyleBackColor = true;
            // 
            // SelectCurrentButton
            // 
            SelectCurrentButton.Anchor = AnchorStyles.Top;
            SelectCurrentButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            SelectCurrentButton.Location = new Point(127, 4);
            SelectCurrentButton.Margin = new Padding(4);
            SelectCurrentButton.Name = "SelectCurrentButton";
            SelectCurrentButton.Size = new Size(84, 87);
            SelectCurrentButton.TabIndex = 13;
            SelectCurrentButton.Text = "→";
            SelectCurrentButton.UseVisualStyleBackColor = true;
            // 
            // sliderLabel
            // 
            sliderLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sliderLabel.AutoSize = true;
            sliderLabel.Location = new Point(4, 0);
            sliderLabel.Margin = new Padding(4, 0, 4, 0);
            sliderLabel.Name = "sliderLabel";
            sliderLabel.Size = new Size(115, 28);
            sliderLabel.TabIndex = 9;
            sliderLabel.Text = "0:00/00:00";
            // 
            // PrevButton
            // 
            PrevButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            PrevButton.FlatAppearance.BorderSize = 0;
            PrevButton.FlatStyle = FlatStyle.Flat;
            PrevButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            PrevButton.Location = new Point(92, 4);
            PrevButton.Margin = new Padding(4);
            PrevButton.Name = "PrevButton";
            PrevButton.Size = new Size(76, 87);
            PrevButton.TabIndex = 3;
            PrevButton.Text = "⏮︎";
            PrevButton.UseVisualStyleBackColor = true;
            // 
            // PlayButton
            // 
            PlayButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            PlayButton.FlatAppearance.BorderSize = 0;
            PlayButton.FlatStyle = FlatStyle.Flat;
            PlayButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            PlayButton.Location = new Point(4, 4);
            PlayButton.Margin = new Padding(4);
            PlayButton.Name = "PlayButton";
            PlayButton.Size = new Size(80, 87);
            PlayButton.TabIndex = 1;
            PlayButton.Text = "⏸︎";
            PlayButton.UseVisualStyleBackColor = true;
            // 
            // NextButton
            // 
            NextButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            NextButton.BackColor = Color.Transparent;
            NextButton.FlatAppearance.BorderSize = 0;
            NextButton.FlatStyle = FlatStyle.Flat;
            NextButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            NextButton.Location = new Point(175, 4);
            NextButton.Margin = new Padding(0);
            NextButton.Name = "NextButton";
            NextButton.Size = new Size(78, 87);
            NextButton.TabIndex = 4;
            NextButton.Text = "⏭︎";
            NextButton.TextImageRelation = TextImageRelation.TextBeforeImage;
            NextButton.UseMnemonic = false;
            NextButton.UseVisualStyleBackColor = false;
            // 
            // playSlider
            // 
            playSlider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            playSlider.Location = new Point(257, 4);
            playSlider.Margin = new Padding(4);
            playSlider.Maximum = 0;
            playSlider.Name = "playSlider";
            playSlider.Size = new Size(609, 80);
            playSlider.TabIndex = 7;
            playSlider.TickFrequency = 60;
            // 
            // UpPanel
            // 
            UpPanel.Controls.Add(OpenWebButton);
            UpPanel.Controls.Add(OpenLocalButton);
            UpPanel.Controls.Add(volumeSlider);
            UpPanel.Controls.Add(titleBox);
            UpPanel.Dock = DockStyle.Fill;
            UpPanel.Location = new Point(0, 0);
            UpPanel.Margin = new Padding(0);
            UpPanel.Name = "UpPanel";
            UpPanel.Size = new Size(1373, 190);
            UpPanel.TabIndex = 0;
            // 
            // OpenWebButton
            // 
            OpenWebButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenWebButton.Font = new Font("Malgun Gothic Semilight", 24F, FontStyle.Regular, GraphicsUnit.Point);
            OpenWebButton.Location = new Point(1121, 4);
            OpenWebButton.Margin = new Padding(4);
            OpenWebButton.Name = "OpenWebButton";
            OpenWebButton.Size = new Size(116, 84);
            OpenWebButton.TabIndex = 11;
            OpenWebButton.Text = "🌐";
            OpenWebButton.UseVisualStyleBackColor = true;
            // 
            // OpenLocalButton
            // 
            OpenLocalButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenLocalButton.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            OpenLocalButton.Location = new Point(1244, 4);
            OpenLocalButton.Margin = new Padding(4);
            OpenLocalButton.Name = "OpenLocalButton";
            OpenLocalButton.Size = new Size(116, 84);
            OpenLocalButton.TabIndex = 10;
            OpenLocalButton.Text = "📁";
            OpenLocalButton.TextAlign = ContentAlignment.MiddleRight;
            OpenLocalButton.UseVisualStyleBackColor = true;
            // 
            // volumeSlider
            // 
            volumeSlider.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            volumeSlider.Location = new Point(1121, 104);
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
            titleBox.Size = new Size(1103, 180);
            titleBox.TabIndex = 0;
            titleBox.Text = "";
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.AutoSize = true;
            mainTableLayoutPanel.ColumnCount = 1;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(UpPanel, 0, 0);
            mainTableLayoutPanel.Controls.Add(DownPanel, 0, 2);
            mainTableLayoutPanel.Controls.Add(MiddlePanel, 0, 1);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Margin = new Padding(0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 3;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 101F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 618F));
            mainTableLayoutPanel.Size = new Size(1373, 1000);
            mainTableLayoutPanel.TabIndex = 3;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1373, 1000);
            Controls.Add(mainTableLayoutPanel);
            Margin = new Padding(4);
            Name = "MainWindow";
            Text = "万万静听";
            DoubleClick += onMainWindowMouseDoubleClick;
            MouseDown += onMainWindowMouseDown;
            DownPanel.ResumeLayout(false);
            MiddlePanel.ResumeLayout(false);
            MiddlePanel.PerformLayout();
            MiddlePanelFlowLayoutPanel.ResumeLayout(false);
            MiddlePanelFlowLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)playSlider).EndInit();
            UpPanel.ResumeLayout(false);
            UpPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)volumeSlider).EndInit();
            mainTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private TableLayoutPanel mainTableLayoutPanel;
        private ToolTip toolTip;
        private FlowLayoutPanel MiddlePanelFlowLayoutPanel;
    }
}