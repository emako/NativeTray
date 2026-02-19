using System;
using System.Windows.Forms;

namespace NativeTray.Demo.WinForms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        //
        // MainForm
        //
        this.ClientSize = new System.Drawing.Size(800, 600);
        this.Name = "MainForm";
        this.Text = "NativeTray.Demo.WinForms";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ResumeLayout(false);
    }
}
