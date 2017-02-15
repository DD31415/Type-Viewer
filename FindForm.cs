using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class FindForm : Form
{
	private TextBox Input;
	private Button FindNextBtn;
	private Button CancelBtn;
	private Label FindLbl;
	
	private Form1 ParentFrm;
	
	public FindForm(Form1 frm)
	{
		this.InstalizeComponent();
		this.ParentFrm = frm;
		this.ParentFrm.Activated += frm_Activated;
		
		this.KeyDown += FindForm_KeyDown;
		this.Input.KeyDown += FindForm_KeyDown;
	}
	
	private void InstalizeComponent()
	{
		this.Text = "Find";
		this.Size = new Size(370, 140);
		this.TopMost = true;
		this.MaximumSize = this.Size;
		this.MinimumSize = this.Size;
		//this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.SizeGripStyle = SizeGripStyle.Hide;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.ShowInTaskbar = false;
		this.ShowIcon = false;
		
		FindLbl = new Label();
		//FindLbl.AutoSize = true;
		FindLbl.Location = new Point(6, 6);
		FindLbl.Text = "Find what: ";
		FindLbl.Size = new Size(FindLbl.Size.Width - 40, FindLbl.Size.Height);
		
		Input = new TextBox();
		Input.Location = new Point((FindLbl.Location.X + FindLbl.Size.Width) + 12, 6);
		Input.Size = new Size(192, 20);
		Input.TextChanged += Input_TextChanged;
		
		FindNextBtn = new Button();
		FindNextBtn.Location = new Point((Input.Location.X + Input.Size.Width) + 6, 6);
		FindNextBtn.Text = "&Find Next";
		FindNextBtn.Enabled = false;
		FindNextBtn.Click += FindNextBtn_Click;
		
		CancelBtn = new Button();
		CancelBtn.Location = new Point(FindNextBtn.Location.X, (FindNextBtn.Location.Y + FindNextBtn.Size.Height) + 6);
		CancelBtn.Text = "Cancel";
		CancelBtn.Click += CancelBtn_Click;
		
		this.Controls.Add(FindLbl);
		this.Controls.Add(Input);
		this.Controls.Add(FindNextBtn);
		this.Controls.Add(CancelBtn);
	}
	
	private void FindNextBtn_Click(object sender, EventArgs e)
	{
		if (this.Input.Text != String.Empty) this.ParentFrm.Find(this.Input.Text);
	}
	
	private void CancelBtn_Click(object sender, EventArgs e)
	{
		this.Hide();
	}
	
	private void Input_TextChanged(object sender, EventArgs e)
	{
		if (this.Input.Text != String.Empty)
		{
			FindNextBtn.Enabled = true;
		}
		else
		{
			FindNextBtn.Enabled = false;
		}
	}
	
	private void FindForm_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
		{
			if (this.Input.Text != String.Empty) this.ParentFrm.Find(this.Input.Text);
		}
	}
	
	private void frm_Activated(object sender, EventArgs e)
	{
		if (this.Visible == true)
		{
			//this.BringToFront();
		}
	}
	
	protected override void OnSizeChanged(EventArgs e)
	{
		//this.Size = new Size(370, 140);
		base.OnSizeChanged(e);
	}
}