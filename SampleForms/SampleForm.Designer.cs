namespace Ha2ne2.DBSimple.Forms
{
    partial class SampleForm {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this.btnGetByReflection = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnGetByDapper = new System.Windows.Forms.Button();
            this.btnGetByBijector = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnORMap = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGetByReflection
            // 
            this.btnGetByReflection.Location = new System.Drawing.Point(12, 12);
            this.btnGetByReflection.Name = "btnGetByReflection";
            this.btnGetByReflection.Size = new System.Drawing.Size(120, 23);
            this.btnGetByReflection.TabIndex = 0;
            this.btnGetByReflection.Text = "リフレクションで取得";
            this.btnGetByReflection.UseVisualStyleBackColor = true;
            this.btnGetByReflection.Click += new System.EventHandler(this.btnGetByReflection_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 47);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.Size = new System.Drawing.Size(776, 391);
            this.dataGridView1.TabIndex = 1;
            // 
            // btnGetByDapper
            // 
            this.btnGetByDapper.Location = new System.Drawing.Point(264, 12);
            this.btnGetByDapper.Name = "btnGetByDapper";
            this.btnGetByDapper.Size = new System.Drawing.Size(120, 23);
            this.btnGetByDapper.TabIndex = 2;
            this.btnGetByDapper.Text = "Dapperで取得";
            this.btnGetByDapper.UseVisualStyleBackColor = true;
            this.btnGetByDapper.Click += new System.EventHandler(this.btnGetByDapper_Click);
            // 
            // btnGetByBijector
            // 
            this.btnGetByBijector.Location = new System.Drawing.Point(138, 12);
            this.btnGetByBijector.Name = "btnGetByBijector";
            this.btnGetByBijector.Size = new System.Drawing.Size(120, 23);
            this.btnGetByBijector.TabIndex = 3;
            this.btnGetByBijector.Text = "DBSimpleで取得";
            this.btnGetByBijector.UseVisualStyleBackColor = true;
            this.btnGetByBijector.Click += new System.EventHandler(this.btnGetByDBSimple_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(390, 12);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(66, 23);
            this.btnClear.TabIndex = 4;
            this.btnClear.Text = "クリア";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnORMap
            // 
            this.btnORMap.BackColor = System.Drawing.Color.OrangeRed;
            this.btnORMap.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnORMap.ForeColor = System.Drawing.Color.White;
            this.btnORMap.Location = new System.Drawing.Point(542, 6);
            this.btnORMap.Name = "btnORMap";
            this.btnORMap.Size = new System.Drawing.Size(120, 35);
            this.btnORMap.TabIndex = 5;
            this.btnORMap.Text = "ORMap : Author";
            this.btnORMap.UseVisualStyleBackColor = false;
            this.btnORMap.Click += new System.EventHandler(this.btnORMap_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.OrangeRed;
            this.button1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(668, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(120, 35);
            this.button1.TabIndex = 6;
            this.button1.Text = "ORMap : Book";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // SampleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnORMap);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnGetByBijector);
            this.Controls.Add(this.btnGetByDapper);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.btnGetByReflection);
            this.Name = "SampleForm";
            this.Text = "SampleForm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnGetByReflection;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnGetByDapper;
        private System.Windows.Forms.Button btnGetByBijector;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnORMap;
        private System.Windows.Forms.Button button1;
    }
}

