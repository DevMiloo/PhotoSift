using System;
using System.Drawing;
using System.Windows.Forms;

public class HeicTestForm : Form
{
    public HeicTestForm()
    {
        this.Text = "HEIC Codec Test (64-bit)";
        this.Size = new Size(400, 200);
        this.StartPosition = FormStartPosition.CenterScreen;

        Button openButton = new Button();
        openButton.Text = "Open HEIC File";
        openButton.Font = new Font("Arial", 12, FontStyle.Bold);
        openButton.Dock = DockStyle.Fill;
        openButton.Click += OpenButton_Click;
        this.Controls.Add(openButton);
    }

    private void OpenButton_Click(object sender, EventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "HEIC Files|*.heic;*.heif|All Files|*.*";
        ofd.Title = "Select a HEIC file";

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // This is the core test. It uses the same method as PhotoSift's native path.
                using (var image = Image.FromFile(ofd.FileName))
                {
                    MessageBox.Show("Successfully loaded {ofd.SafeFileName}!\n\nDimensions: {image.Width}x{image.Height}\nCodec is working correctly.", 
                                    "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load {ofd.SafeFileName}.\n\nThis indicates the HEIC codec is not correctly installed or accessible for 64-bit applications.\n\nError: {ex.GetType().Name}\n{ex.Message}", 
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new HeicTestForm());
    }
}
