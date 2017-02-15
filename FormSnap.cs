/*
	Snaps a form to any side of a parent form.
*/
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class FormSnap
{
	private struct ChildForm
	{
		public Form ChildFrm;
		private SnapSides _Side;
		
		public SnapSides Side
		{
			get
			{
				return _Side;
			}
			set
			{
				_Side = value;
				
				// Update the child form.
				Instance.ParentForm_LocationChanged(null, null);
				Instance.ParentForm_LocationChanged(null, null);
			}
		}
		
		public bool ChildDontDoNextEvent;
		public bool ParentDontDoNextEvent;
		
		private FormSnap Instance;
		
		public ChildForm(Form child, SnapSides side, FormSnap instance)
		{
			this.Instance = instance;
			this._Side = side;
			this.ChildFrm = child;			
			this.ChildDontDoNextEvent = false;
			this.ParentDontDoNextEvent = false;
			
			this.Side = side;
		}
	}
	
	#region Public Fields
	public Form ParentForm;
	//public Form ChildForm;
	private List<ChildForm> ChildForms = new List<ChildForm>();
	
	public SnapSides SnappedSide
	{
		get
		{
			return _SnappedSide;
		}
		set
		{
			_SnappedSide = value;
			
			// Update the child form.
			ParentForm_LocationChanged(null, null);
			ParentForm_LocationChanged(null, null);
		}
	}
	#endregion
	
	#region Private Fields
	private SnapSides _SnappedSide;
	#endregion
	
	#region Constructors
	public FormSnap(Form Parent, Form Child)
	{
	
		this.ParentForm = Parent;
		
		this.ParentForm.LocationChanged += new EventHandler(this.ParentForm_LocationChanged);
		this.ParentForm.SizeChanged += new EventHandler(this.ParentForm_SizeChanged);
		this.ParentForm.Show();
	}
	
	public FormSnap(Form Parent, Form Child, SnapSides side)
	{
		this.ParentForm = Parent;
		//this.ChildForm = Child;
		//this._SnappedSide = side;
		
		this.ParentForm.LocationChanged += new EventHandler(this.ParentForm_LocationChanged);
		this.ParentForm.SizeChanged += new EventHandler(this.ParentForm_SizeChanged);
		this.ParentForm.Show();
		
		this.InitChildForm(new ChildForm(Child, side, this));
		
	}
	#endregion
	
	#region Private Methods
	private void InitChildForm(ChildForm frm)
	{
		#region Form Event Subscriptions
		frm.ChildFrm.LocationChanged += new EventHandler(this.ChildForm_LocationChanged);
		frm.ChildFrm.SizeChanged += new EventHandler(this.ChildForm_SizeChanged);
		#endregion
		
		#region Form Property Setup
		frm.ChildFrm.StartPosition = FormStartPosition.Manual;
		#endregion
		
		// Snap the child form to the parent form on start.
		ParentForm_LocationChanged(null, null);
		
		// Display Both Forms.
		frm.ChildFrm.Show();
		
		this.ChildForms.Add(frm);
		
		ParentForm_LocationChanged(null, null);
	}
	#endregion
	
	#region Private Event Handlers
	private void ParentForm_LocationChanged(object sender, EventArgs e)
    {
		foreach (ChildForm frm in this.ChildForms)
		{
			ParentFrmMoved(frm);
		}
    }
	
	private void ChildForm_LocationChanged(object sender, EventArgs e)
    {
		Form fm = (Form)sender;
		
		foreach (ChildForm frm in this.ChildForms)
		{
			if (frm.ChildFrm == fm)
			{
				ChildFrmMoved(frm);
				break;
			}
		}
    }
	
	private void ParentFrmMoved(ChildForm frm)
	{
		if (frm.ParentDontDoNextEvent == false)
		{
			switch (frm.Side)
			{
				case SnapSides.Left:
				{
					frm.ChildFrm.Location = new Point(this.ParentForm.Location.X - frm.ChildFrm.Size.Width, this.ParentForm.Location.Y);
					break;
				}
				case SnapSides.Right:
				{
					frm.ChildFrm.Location = new Point(this.ParentForm.Location.X + this.ParentForm.Size.Width, this.ParentForm.Location.Y);
					break;
				}
				case SnapSides.Top:
				{
					frm.ChildFrm.Location = new Point(this.ParentForm.Location.X, this.ParentForm.Location.Y - frm.ChildFrm.Size.Height);
					break;
				}
				case SnapSides.Bottom:
				{
					frm.ChildFrm.Location = new Point(this.ParentForm.Location.X, this.ParentForm.Location.Y + this.ParentForm.Size.Height);
					break;
				}
			}
			frm.ParentDontDoNextEvent = true;
		}
		else
		{
			frm.ParentDontDoNextEvent = false;
		}
	}
	
	private void ChildFrmMoved(ChildForm frm)
	{
		if (frm.ChildDontDoNextEvent == false)
		{
			switch (frm.Side)
			{
				case SnapSides.Left:
				{
					this.ParentForm.Location = new Point(frm.ChildFrm.Location.X + frm.ChildFrm.Size.Width, frm.ChildFrm.Location.Y);
					break;
				}
				case SnapSides.Right:
				{
					this.ParentForm.Location = new Point(frm.ChildFrm.Location.X - this.ParentForm.Size.Width, frm.ChildFrm.Location.Y);
					break;
				}
				case SnapSides.Top:
				{
					this.ParentForm.Location = new Point(frm.ChildFrm.Location.X, frm.ChildFrm.Location.Y + frm.ChildFrm.Size.Height);
					break;
				}
				case SnapSides.Bottom:
				{
					this.ParentForm.Location = new Point(frm.ChildFrm.Location.X, frm.ChildFrm.Location.Y - this.ParentForm.Size.Height);
					break;
				}
			}
			frm.ParentDontDoNextEvent = true;
		}
		else
		{
			frm.ChildDontDoNextEvent = false;
		}
	}
	
	private void ParentForm_SizeChanged(object sender, EventArgs e)
	{
		ParentForm_LocationChanged(null, null);
	}
	private void ChildForm_SizeChanged(object sender, EventArgs e)
	{
		ChildForm_LocationChanged(null, null);
	}
	#endregion
	
	#region Public Methods
	public void ForceWindowPositionUpdate()
	{
		ParentForm_LocationChanged(null, null);
	}
	
	public void Add(Form Child, SnapSides ChildSnapSide)
	{
		foreach (ChildForm frm in this.ChildForms)
		{
			if (frm.Side == ChildSnapSide)
			{
				throw new Exception("Already have another child for snapped to this side!");
			}
			if (frm.ChildFrm == Child)
			{
				throw new Exception("Already have the same child for snapped!");
			}
		}
		
		ChildForm f = new ChildForm(Child, ChildSnapSide, this);
		
		this.InitChildForm(f);
		
	}
	#endregion
}

#region public Enums
public enum SnapSides : int
{
	Left,
	Right,
	Top,
	Bottom
}
#endregion