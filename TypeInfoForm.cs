using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

public class TypeInfoForm : Form
{
	private Form1 ParentFrm;
	private FormSnap snap;
	
	private TextBox AttributesBox;
	
	private Panel CheckBoxPanel;
	private List<StaticCheckBox> Boxes = new List<StaticCheckBox>();
	private List<PropertyInfo> BoolProps = new List<PropertyInfo>();
	
	public TypeInfoForm(Form1 parent)
	{
		this.ParentFrm = parent;
		this.ParentFrm.TextChanged += ParentFrm_TextChanged;
		this.ParentFrm.Activated += ParentFrm_GotFocus;
		this.ParentFrm.GotFocus += ParentFrm_GotFocus;
		
		InstalizeComponent();
		
		snap = new FormSnap(parent, this, SnapSides.Right);
	}
	
	private void InstalizeComponent()
	{
		this.Size = this.ParentFrm.Size;
		this.MinimumSize = this.Size;
		this.Text = this.ParentFrm.Text;
		this.ShowInTaskbar = false;
		this.ControlBox = false;
		
		AttributesBox = new TextBox();
		AttributesBox.Location = new Point(6, 6);
		AttributesBox.Size = new Size(this.ClientSize.Width - (AttributesBox.Location.X * 2), 100);
		AttributesBox.ReadOnly = true;
		AttributesBox.Multiline = true;
		
		CheckBoxPanel = new Panel();
		CheckBoxPanel.Location =  new Point(6, AttributesBox.Location.Y + (AttributesBox.Size.Height + 12));
		CheckBoxPanel.Size = new Size(this.ClientSize.Width - (CheckBoxPanel.Location.X * 2), this.ClientSize.Height - (CheckBoxPanel.Location.Y) - 6);
		CheckBoxPanel.MinimumSize = CheckBoxPanel.Size;
		CheckBoxPanel.AutoScroll = true;
		CheckBoxPanel.BorderStyle = BorderStyle.FixedSingle;
		InstalizeCheckBoxes();
		CheckBoxPanel.Controls.AddRange(this.Boxes.ToArray());
		
		this.Controls.Add(AttributesBox);
		this.Controls.Add(CheckBoxPanel);
	}
	
	private void InstalizeCheckBoxes()
	{
		Type SystemType = typeof(Type);
		Type BoolType = typeof(Boolean);
		PropertyInfo[] props = SystemType.GetProperties(Form1.AllFlags);
		
		bool Second = false;
		foreach (PropertyInfo prop in props)
		{
			if (prop.PropertyType == BoolType)
			{
				StaticCheckBox box = new StaticCheckBox();
				box.AutoSize = true;
				box.Checked = false;
				box.Text = prop.Name;
				box.Name = prop.Name;
				
				if (Boxes.Count == 0)
				{
					box.Location = new Point(6, 12);
				}
				else
				{
					if (Second == false)
					{
						box.Location = new Point(6, Boxes[Boxes.Count - 1].Location.Y + (Boxes[Boxes.Count - 1].Size.Height));
					}
					else
					{
						box.Location = new Point(Boxes[Boxes.Count - 1].Location.X + (Boxes[Boxes.Count - 1].ClientSize.Width + 60), Boxes[Boxes.Count - 1].Location.Y);
					}
				}
				
				BoolProps.Add(prop);
				Boxes.Add(box);
				
				Second = !Second;
			}
		}
		
	}
	
	protected override void OnSizeChanged (EventArgs e)
	{
		if (CheckBoxPanel != null)
		{
			CheckBoxPanel.Size = new Size(this.ClientSize.Width - (CheckBoxPanel.Location.X * 2), this.ClientSize.Height - (CheckBoxPanel.Location.Y) - 6);
		}
	}
	
	private bool IgnoreNext;
	
	private void ParentFrm_GotFocus(object sender, EventArgs e)
	{
		if (IgnoreNext == false)
		{
			this.DisplayWindow();
		}
		else
		{
			IgnoreNext = false;
		}
	}
	
	protected override void OnActivated(EventArgs e)
	{
	}
	
	protected override void OnGotFocus(EventArgs e) {}
	
	private void DisplayWindow()
	{
		this.BringToFront();
		
		if (ParentFrm != null)
		{
			if (ParentFrm.IsDisposed == false && ParentFrm.Disposing == false)
			{
				this.BringParentToFront();
			}
		}
		IgnoreNext = true;
	}
	
	private void BringParentToFront()
	{
		ParentFrm.BringToFront();
		IgnoreNext = true;
	}
	
	private void ParentFrm_TextChanged(object sender, EventArgs e)
	{
		this.Text = this.ParentFrm.Text;
	}
	
	public void UpdateSelectedType(Type t)
	{
		if (t != null)
		{
			AttributesBox.Text = t.Attributes.ToString();
			
			Type b = t.GetType();
			PropertyInfo[] props = b.GetProperties();
			
			foreach (StaticCheckBox box in this.Boxes)
			{
				box.Enabled = true;
				foreach (PropertyInfo prop in props)
				{
					if (prop.Name == box.Text)
					{
						object val = prop.GetValue(t, new object[] { });
						//MessageBox.Show(prop.Name + ": " + val.ToString());
						box.Checked = (bool)val;
						break;
					}
				}
			}
			
			// ContainsGenericParameters.Checked = t.ContainsGenericParameters;
			// HasElementType.Checked = t.HasElementType;
		}
		else
		{
			AttributesBox.Text = "No Type Selected!";
			foreach (StaticCheckBox box in this.Boxes)
			{
				box.Enabled = false;
				box.Checked = false;
			}
		}
	}
}

public class StaticCheckBox : CheckBox
{
	protected override void OnMouseEnter(EventArgs e) {}
	protected override void OnMouseMove(MouseEventArgs e) {}
	protected override void OnMouseUp(MouseEventArgs e) {}
	protected override void OnMouseDown(MouseEventArgs e) {}
	protected override void OnClick(EventArgs e) {}
}