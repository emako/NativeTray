using System.Windows.Forms;

namespace WinFormsApp;

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
        this.Text = "WinFormsApp";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ResumeLayout(false);
    }
}
