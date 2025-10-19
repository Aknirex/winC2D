namespace winC2D
{
    partial class LogForm
    {
        private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.ListView listViewLog;
    private System.Windows.Forms.Button buttonRollback;
        private System.Windows.Forms.ColumnHeader columnTime;
        private System.Windows.Forms.ColumnHeader columnName;
        private System.Windows.Forms.ColumnHeader columnOldPath;
        private System.Windows.Forms.ColumnHeader columnNewPath;
        private System.Windows.Forms.ColumnHeader columnStatus;
        private System.Windows.Forms.ColumnHeader columnMsg;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listViewLog = new System.Windows.Forms.ListView();
            this.columnTime = new System.Windows.Forms.ColumnHeader();
            this.columnName = new System.Windows.Forms.ColumnHeader();
            this.columnOldPath = new System.Windows.Forms.ColumnHeader();
            this.columnNewPath = new System.Windows.Forms.ColumnHeader();
            this.columnStatus = new System.Windows.Forms.ColumnHeader();
            this.columnMsg = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // listViewLog
            // 
            this.listViewLog.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.columnTime, this.columnName, this.columnOldPath, this.columnNewPath, this.columnStatus, this.columnMsg});
            this.listViewLog.FullRowSelect = true;
            this.listViewLog.GridLines = true;
            this.listViewLog.Location = new System.Drawing.Point(12, 12);
            this.listViewLog.Name = "listViewLog";
            this.listViewLog.Size = new System.Drawing.Size(860, 400);
            this.listViewLog.TabIndex = 0;
            this.listViewLog.UseCompatibleStateImageBehavior = false;
            this.listViewLog.View = System.Windows.Forms.View.Details;
            // 
            // columnTime
            // 
            this.columnTime.Text = "时间";
            this.columnTime.Width = 140;
            // 
            // columnName
            // 
            this.columnName.Text = "软件名称";
            this.columnName.Width = 120;
            // 
            // columnOldPath
            // 
            this.columnOldPath.Text = "原路径";
            this.columnOldPath.Width = 180;
            // 
            // columnNewPath
            // 
            this.columnNewPath.Text = "新路径";
            this.columnNewPath.Width = 180;
            // 
            // columnStatus
            // 
            this.columnStatus.Text = "状态";
            this.columnStatus.Width = 80;
            // 
            // columnMsg
            // 
            this.columnMsg.Text = "信息";
            this.columnMsg.Width = 140;
            // 
            // buttonRollback
            // 
            this.buttonRollback = new System.Windows.Forms.Button();
            this.buttonRollback.Location = new System.Drawing.Point(12, 420);
            this.buttonRollback.Name = "buttonRollback";
            this.buttonRollback.Size = new System.Drawing.Size(120, 32);
            this.buttonRollback.TabIndex = 1;
            this.buttonRollback.Text = "回滚所选";
            this.buttonRollback.UseVisualStyleBackColor = true;
            this.buttonRollback.Click += new System.EventHandler(this.buttonRollback_Click);
            // 
            // LogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 461);
            this.Controls.Add(this.buttonRollback);
            this.Controls.Add(this.listViewLog);
            this.Name = "LogForm";
            this.Text = "迁移日志";
            this.ResumeLayout(false);
        }
    }
}