namespace GHelper
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.RichTextBox textDocs;
        private UI.RButton buttonClose;

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
            textDocs = new System.Windows.Forms.RichTextBox();
            buttonClose = new UI.RButton();
            SuspendLayout();
            // 
            // textDocs
            // 
            textDocs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textDocs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            textDocs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            textDocs.Location = new System.Drawing.Point(20, 20);
            textDocs.Name = "textDocs";
            textDocs.ReadOnly = true;
            textDocs.Size = new System.Drawing.Size(760, 500);
            textDocs.TabIndex = 0;
            textDocs.Text = "";
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClose.BackColor = System.Drawing.SystemColors.ControlLight;
            buttonClose.BorderColor = System.Drawing.Color.Transparent;
            buttonClose.BorderRadius = 4;
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonClose.Location = new System.Drawing.Point(640, 540);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new System.Drawing.Size(140, 40);
            buttonClose.TabIndex = 1;
            buttonClose.Text = "Close";
            buttonClose.UseVisualStyleBackColor = false;
            // 
            // AboutForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 600);
            Controls.Add(buttonClose);
            Controls.Add(textDocs);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "G-Helper Documentation";
            ResumeLayout(false);
        }
    }
}
