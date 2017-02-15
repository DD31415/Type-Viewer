using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

public class Form1 : Form
{
	private enum ViewLevel : int
	{
		ReferencedAssembliesLevel,
		AssemblyLevel,
		NamespaceLevel,
		ConstructorLevel,
		MethodLevel,
		FieldLevel,
		PropertyLevel,
		EventLevel,
		ViewSelectionLevel,
		MSILLevel,
		MSILConstructorLevel
	}
	
	public static BindingFlags AllFlags = BindingFlags.Default | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty | BindingFlags.ExactBinding | BindingFlags.SuppressChangeType | BindingFlags.OptionalParamBinding | BindingFlags.IgnoreReturn;
	
	private static string[] ClassModifiers = new string[]
	{
		"public ",
		"private ",
		"enum ",
		"static ",
		"sealed ",
		"abstract ",
		"interface ",
		"class "
	};
	
	private static string ReferencedAssembliesString = "<Assembly References>";
	
	private MenuItem FileItem;
	
	private MenuItem EditItem;
	private MenuItem CopyItem1;
	private MenuItem FindItem;
	
	private MenuItem OpenItem;
	private MenuItem OpenUnsafeItem;
	private MenuItem ClearRecentAssemblyListItem;
	private MenuItem ExitItem;
	private MenuItem HelpItem;
	
	private MenuItem ToolsItem;
	private MenuItem OptionsItem;
	private MenuItem ShowInternalTypesItem;
	
	private ListBox TypeBox;
	private MenuItem CopyItem;
	private MenuItem CopyAllFieldsItem;
	private MenuItem CopyAllMSILItem;
	private ToolStrip menu;
	
	private ViewLevel _CurrentViewLevel;
	
	private ViewLevel CurrentViewLevel
	{
		get
		{
			return _CurrentViewLevel;
		}
		set
		{
			_CurrentViewLevel = value;
			
			if (TypeBox.Items.Count == 1)
			{
				TypeBox.SelectedIndex = 0;
			}
			
			if (this.Debug == true)
			{
				if (this.CurrentViewLevel == ViewLevel.NamespaceLevel && TypeBox.SelectedIndex != -1)
				{
					foreach (Type t in this.SelectedNamespace.Types)
					{
						if (TypeStr(t) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
						{
							typeInfoForm.UpdateSelectedType(t);
							break;
						}
					}
				}
				else if (this.CurrentViewLevel == ViewLevel.ConstructorLevel || this.CurrentViewLevel == ViewLevel.MethodLevel || this.CurrentViewLevel == ViewLevel.FieldLevel || this.CurrentViewLevel == ViewLevel.PropertyLevel || this.CurrentViewLevel == ViewLevel.EventLevel || this.CurrentViewLevel == ViewLevel.ViewSelectionLevel)
				{
					if (this.SelectedType != null)
					{
						typeInfoForm.UpdateSelectedType(this.SelectedType);
					}
				}
				else
				{
					typeInfoForm.UpdateSelectedType(null);
				}
			}
			
			switch (_CurrentViewLevel)
			{
				case ViewLevel.ReferencedAssembliesLevel:
				{
					CopyItem.Visible = false;
					CopyItem1.Enabled = false;
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.AssemblyLevel:
				{
					CopyItem1.Enabled = true;
					
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Namespace";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.NamespaceLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Type Name";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.ConstructorLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Constructor Name";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.MethodLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Method Name";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.FieldLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Field Name";
					
					if (this.SelectedType.BaseType != typeof(Object) && this.SelectedType.BaseType != null)
					{
						if (this.SelectedType.BaseType.FullName == "System.Enum")
						{
							CopyAllFieldsItem.Visible = true;
						}
					}
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.PropertyLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Property Name";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.EventLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy Event Name";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.ViewSelectionLevel:
				{
					CopyItem.Visible = false;
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = false;
					break;
				}
				case ViewLevel.MSILLevel:
				{
					CopyItem.Visible = true;
					CopyItem.Text = "Copy MSIL Instruction";
					
					CopyAllFieldsItem.Visible = false;
					CopyAllMSILItem.Visible = true;
					break;
				}
			}
		}
	}
	
	private Assembly SelectedAssembly;
	private Namespace SelectedNamespace;
	private MethodInfo SelectedMethod;
	private ConstructorInfo SelectedConstructor;
	
	private Type _SelectedType;
	
	private Type SelectedType
	{
		get
		{
			return _SelectedType;
		}
		set
		{
			_SelectedType = value;
		}
	}
	private List<string> Namespaces = new List<string>();
	private List<Namespace> NamespaceTypes = new List<Namespace>();
	
	private List<string> ExcludedMethods = new List<string>(); // This list contains property get/set and event add/remove/raise methods that should excluded from the method list.
	
	private Settings settings = new Settings();
	private TypeInfoForm typeInfoForm;
	private bool Debug = false;
	
	public Form1()
	{
		InstalizeComponent();
		if (settings.LastFileOpened != String.Empty) this.LoadAssembly(settings.LastFileOpened, true);
	}
	
	public Form1(string path)
	{
		InstalizeComponent();
		this.LoadAssembly(path, true);
	}
	
	private void InstalizeComponent()
	{
		this.Size = new Size(616, 438);
		this.MinimumSize = this.Size;
		this.Text = "Type Viewer";
		this.AllowDrop = true;
		this.DragDrop += Form1_DragDrop;
		this.DragOver += Form1_DragOver;
		this.StartPosition = FormStartPosition.CenterScreen;
		
		this.BuildClassicMenu();
		//this.BuildMenu();
		
		TypeBox = new ListBox();
		TypeBox.SelectedIndexChanged += TypeBox_SelectedIndexChanged;
		TypeBox.DoubleClick += TypeBox_DoubleClick;
		TypeBox.KeyUp += TypeBox_KeyUp;
		TypeBox.Sorted = true;
		TypeBox.AllowDrop = true;
		TypeBox.DragDrop += Form1_DragDrop;
		TypeBox.DragOver += Form1_DragOver;
		TypeBox.ScrollAlwaysVisible = true;
		TypeBox.HorizontalScrollbar = true;
		
		ContextMenu TypeBoxContextMenu = new ContextMenu();
		CopyItem = new MenuItem();
		CopyItem.Text = "Copy";
		CopyItem.Click += CopyContextMenuItem_Click;
		
		CopyAllFieldsItem = new MenuItem();
		CopyAllFieldsItem.Text = "Copy All Enum Fields";
		CopyAllFieldsItem.Click += CopyAllFieldsItem_Click;
		
		CopyAllMSILItem = new MenuItem();
		CopyAllMSILItem.Text = "Copy All MSIL Instructions";
		CopyAllMSILItem.Click += CopyAllMSILItem_Click;
		
		TypeBoxContextMenu.MenuItems.Add(CopyItem);
		TypeBoxContextMenu.MenuItems.Add(CopyAllFieldsItem);
		TypeBoxContextMenu.MenuItems.Add(CopyAllMSILItem);
		TypeBox.ContextMenu = TypeBoxContextMenu;
		
		TypeBox.Location = new Point(6, 6);
		//TypeBox.Size = new Size(this.ClientSize.Width - 12, this.ClientSize.Height - 36);
		this.ClientSizeChanged += Form1_ClientSizeChanged;
		Form1_ClientSizeChanged(null, null);
		//TypeBox.ListView = ListViewMode.Detail;
		
		this.Controls.Add(TypeBox);
		
		this.AllowDrop = true;
		TypeBox.AllowDrop = true;
		
		if (this.Debug == true)
		{
			typeInfoForm = new TypeInfoForm(this);
			typeInfoForm.Show();
		}
	}
	
	protected override void OnLoad(EventArgs e)
	{
		if (this.Debug == true)
		{
			this.DesktopLocation = new Point(this.DesktopLocation.X - (this.Size.Width / 2), this.DesktopLocation.Y);
		}
		base.OnLoad(e);
	}
	
	private void BuildMenu()
	{
		this.menu = new ToolStrip();
		ToolStripMenuItem File = new ToolStripMenuItem("File");
		
		ToolStripMenuItem Open = new ToolStripMenuItem("Open");
		Open.Click += OpenItem_Click;
		
		ToolStripMenuItem Exit = new ToolStripMenuItem("Exit");
		Exit.Click += ExitItem_Click;
		
		
		File.DropDownItems.Add(Open);
		File.DropDownItems.Add(new ToolStripSeparator());
		File.DropDownItems.Add(new ToolStripMenuItem("Dummy1"));
		File.DropDownItems.Add(new ToolStripSeparator());
		File.DropDownItems.Add(Exit);
		menu.GripStyle = ToolStripGripStyle.Hidden;
		menu.Items.Add(File);
		
		this.Controls.Add(menu);
	}
	
	private void BuildClassicMenu()
	{
		MainMenu menu = new MainMenu();
		FileItem = new MenuItem();
		EditItem = new MenuItem();
		MenuItem AboutItem = new MenuItem();
		FileItem.Text = "File";
		EditItem.Text = "Edit";
		AboutItem.Text = "About";
		
		OpenItem = new MenuItem();
		OpenItem.Text = "Open...";
		OpenItem.Click += OpenItem_Click;
		OpenItem.Shortcut = Shortcut.CtrlO;
		
		OpenUnsafeItem = new MenuItem();
		OpenUnsafeItem.Text = "Open Unsafe...";
		OpenUnsafeItem.Click += OpenUnsafeItem_Click;
		OpenUnsafeItem.Shortcut = Shortcut.CtrlShiftO;
		
		ClearRecentAssemblyListItem = new MenuItem();
		ClearRecentAssemblyListItem.Text = "Clear Recent Assembly List";
		ClearRecentAssemblyListItem.Click += Form1_ClearRecentAssemblyListItem;
		ClearRecentAssemblyListItem.Visible = false;
		
		HelpItem = new MenuItem();
		HelpItem.Text = "Help";
		HelpItem.Click += HelpItem_Click;
		
		ExitItem = new MenuItem();
		ExitItem.Text = "Exit";
		ExitItem.Click += ExitItem_Click;
		
		this.UpdateFileMenu();
		
		CopyItem1 = new MenuItem();
		CopyItem1.Text = "Copy";
		CopyItem1.Shortcut = Shortcut.CtrlC;
		CopyItem1.Click += CopyContextMenuItem_Click;
		
		FindItem = new MenuItem();
		FindItem.Text = "Find...";
		FindItem.Shortcut = Shortcut.CtrlF;
		FindItem.Click += FindItem_Click;
		
		EditItem.MenuItems.Add(CopyItem1);
		EditItem.MenuItems.Add(FindItem);
		
		AboutItem.MenuItems.Add(HelpItem);
		
		ToolsItem = new MenuItem();
		ToolsItem.Text = "Tools";
		
		OptionsItem = new MenuItem();
		OptionsItem.Text = "Options";
		
		ShowInternalTypesItem = new MenuItem();
		ShowInternalTypesItem.Text = "Show Internal Types";
		ShowInternalTypesItem.Click += ShowInternalTypesItem_Click;
		ShowInternalTypesItem.Checked = settings.ShowInternalTypes;
		
		OptionsItem.MenuItems.Add(ShowInternalTypesItem);
		ToolsItem.MenuItems.Add(OptionsItem);
		
		menu.MenuItems.Add(FileItem);
		menu.MenuItems.Add(EditItem);
		menu.MenuItems.Add(ToolsItem);
		menu.MenuItems.Add(AboutItem);
		this.Menu = menu;
	}
	
	private void TypeBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (this.CurrentViewLevel == ViewLevel.NamespaceLevel && TypeBox.SelectedIndex != -1 && this.Debug == true)
		{
			foreach (Type t in this.SelectedNamespace.Types)
			{
				if (TypeStr(t) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
				{
					typeInfoForm.UpdateSelectedType(t);
					break;
				}
			}
		}
		//this.Text = TypeBox.Items[TypeBox.SelectedIndex].ToString();
	}
	
	private void ShowInternalTypesItem_Click(object sender, EventArgs e)
	{
		settings.ShowInternalTypes = !settings.ShowInternalTypes;
		ShowInternalTypesItem.Checked = settings.ShowInternalTypes;
		settings.Save();
		
		if (this.CurrentViewLevel == ViewLevel.NamespaceLevel)
		{
			LoadTypes(this.SelectedNamespace);
		}
	}
	
	private void Form1_DragDrop(object sender, DragEventArgs e)
	{
		e.Effect = DragDropEffects.All;
		
		DataObject Data = (DataObject)e.Data;
		//StringCollection filesDroped = Data.GetFileDropList();
		string[] filesDroped = new string[Data.GetFileDropList().Count];
		
		if (filesDroped.Length == 1)
		{
			Data.GetFileDropList().CopyTo(filesDroped, 0);
			
			if (Path.GetExtension(filesDroped[0]) == ".exe" || Path.GetExtension(filesDroped[0]) == ".dll")
			{
				this.LoadAssembly(filesDroped[0], true);
			}
		}
	}
	
	private void Form1_DragOver(object sender, DragEventArgs e)
	{
		e.Effect = DragDropEffects.All;
	}
	
	private void Form1_ClientSizeChanged(object sender, EventArgs e)
	{
		// Just in case you want the debug form to always be the same size as the main form.
		// if (this.Debug == true && typeInfoForm != null)
		// {
			// typeInfoForm.Size = this.Size;
		// }
		
		TypeBox.Size = new Size(this.ClientSize.Width - 12, this.ClientSize.Height - 12);
	}
	
	private void OpenItem_Click(object sender, EventArgs e)
	{
		OpenFileDialog diag = new OpenFileDialog();
		if (this.SelectedAssembly != null)
		{
			diag.InitialDirectory = Path.GetDirectoryName(this.SelectedAssembly.Location);
		}
		
		diag.Filter = "Executable files (*.exe, *.dll)|*.exe;*.dll|Application Extension files (*.dll)|*.dll";
		
		diag.CustomPlaces.Add(new FileDialogCustomPlace(Path.GetDirectoryName(Application.ExecutablePath)));
		diag.CustomPlaces.Add(new FileDialogCustomPlace(RuntimeEnvironment.GetRuntimeDirectory()));
		if (diag.ShowDialog() == DialogResult.OK)
		{
			if (this.LoadAssembly(diag.FileName, false) == true) this.AddRecentFile(diag.FileName);
		}
	}
	
	private void OpenUnsafeItem_Click(object sender, EventArgs e)
	{
		OpenFileDialog diag = new OpenFileDialog();
		if (this.SelectedAssembly != null)
		{
			diag.InitialDirectory = Path.GetDirectoryName(this.SelectedAssembly.Location);
		}
		
		diag.Filter = "Executable files (*.exe, *.dll)|*.exe;*.dll|Application Extension files (*.dll)|*.dll";
		
		diag.CustomPlaces.Add(new FileDialogCustomPlace(Path.GetDirectoryName(Application.ExecutablePath)));
		diag.CustomPlaces.Add(new FileDialogCustomPlace(RuntimeEnvironment.GetRuntimeDirectory()));
		if (diag.ShowDialog() == DialogResult.OK)
		{
			if (this.LoadAssembly(diag.FileName, true) == true) this.AddRecentFile(diag.FileName);
		}
	}
	
	private void Form1_ClearRecentAssemblyListItem(object sender, EventArgs e)
	{
		this.settings.ClearRecentAssemblies();
		this.UpdateFileMenu();
	}
	
	private void RecentItem_Click(object sender, EventArgs e)
	{
		MenuItem RecentItem = sender as MenuItem;
		
		string[] s = RecentItem.Text.Split(new char[] { ':' });
		string Path = s[1] + s[2];
		Path = Path.Substring(1, Path.Length - 1);
		Path = Path[0].ToString() + ":" + s[2];
		if (this.LoadAssembly(Path, true) == true) AddRecentFile(Path);
	}
	
	private void AddRecentFile(string path)
	{
		this.settings.AddRecentAssembly(path);
		this.UpdateFileMenu();
	}
	
	private void FindItem_Click(object sender, EventArgs e)
	{
		FindForm diag = new FindForm(this);
		diag.Show();
	}
	
	int FindIndex = 0;
	public void Find(string text)
	{
		List<string> Items = new List<string>();
		
		foreach (string i in TypeBox.Items)
		{
			if (i.Contains(text) == true)
			{
				Items.Add(i);
			}
		}
		
		if (Items.Count == 0)
		{
			MessageBox.Show("Cannot find " + '"'.ToString() + text + '"'.ToString(), "Type Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
			FindIndex = 0;
			return;
		}
		
		if (FindIndex == Items.Count - 1)
		{
			FindIndex = 0;
		}
		else
		{
			FindIndex++;
		}
		
		this.SelectItem(Items[FindIndex]);
	}
	
	private void SelectItem(string Item)
	{
		for (int i = 0; i < TypeBox.Items.Count; i++)
		{
			if (TypeBox.Items[i].ToString() == Item)
			{
				TypeBox.SelectedIndex = i;
				return;
			}
		}
	}
	
	private void UpdateFileMenu()
	{
		FileItem.MenuItems.Clear();
		
		FileItem.MenuItems.Add(OpenItem);
		FileItem.MenuItems.Add(OpenUnsafeItem);
		
		for (int i = 0; i < settings.RecentAssemblyList.Count; i++)
		{
			MenuItem item = new MenuItem((i + 1).ToString() + ": " + settings.RecentAssemblyList[i]);
			item.Click += RecentItem_Click;
			FileItem.MenuItems.Add(item);
		}
		
		if (settings.RecentAssemblyList.Count > 0)
		{
			ClearRecentAssemblyListItem.Visible = true;
		}
		else
		{
			ClearRecentAssemblyListItem.Visible = false;
		}
		FileItem.MenuItems.Add(ClearRecentAssemblyListItem);
		FileItem.MenuItems.Add(ExitItem);
	}
	
	private bool LoadAssembly(string path, bool Unsafe)
	{
		if (Path.GetExtension(path).ToLower() == ".dll" || Path.GetExtension(path).ToLower() == ".exe")
		{
			try
			{
				if (Unsafe == true)
				{
					this.SelectedAssembly = Assembly.UnsafeLoadFrom(path);
				}
				else
				{
					this.SelectedAssembly = Assembly.LoadFile(path);
				}
				
				// It worked. Clear the way for the new Assembly.
				this.Namespaces.Clear();
				this.NamespaceTypes.Clear();
				this.SelectedType = null;
				this.SelectedMethod = null;
				this.SelectedConstructor = null;
				this.SelectedNamespace = null;
				this.ExcludedMethods.Clear();
				
				this.LoadNamespaces(this.SelectedAssembly);
				settings.LastFileOpened = path;
				return true;
			}
			catch (BadImageFormatException)
			{
				// Invalid assembly! Possibly not a managed assembly or is corrupt/invalid.
				MessageBox.Show("Assembly: " + Path.GetFileName(path) + " is not a valid managed assembly! Please choose a valid managed assembly (.exe or .dll)", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			} 
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				return false;
			}
		}
		return false;
	}

	
	private void HelpItem_Click(object sender, EventArgs e)
	{
		MessageBox.Show(@"To open an assembly, select " + '"'.ToString() + "Open" +  '"'.ToString() + @" from the file menu and choose an assembly to open from the dialog, or you can drag an .exe or .dll file over the main program window to open it.

To Naviagate an assembly, simply double click on an entry in the list, or highlight a selection and press enter.
If you want to go back, press the Backspace key.

To View methods, events, properties, and constructors,
you must select a Type and then select which category you want to view.

You can also view a Method and a Constructor's MSIL by double clicking, or pressing enter when selected on a constructor or method.
		", "Type Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}
	
	private void ExitItem_Click(object sender, EventArgs e)
	{
		this.Close();
	}
	
	private void CopyAllFieldsItem_Click(object sender, EventArgs e)
	{
		if (this.CurrentViewLevel == ViewLevel.FieldLevel)
		{
			string TextToCopy = String.Empty;
			FieldInfo[] fields = SelectedType.GetFields();
			
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].Name == "value__") continue;
				
				if (i == fields.Length - 1)
				{
					TextToCopy += SelectedType.Name + "." + fields[i].Name;
				}
				else
				{
					TextToCopy += SelectedType.Name + "." + fields[i].Name + " | ";
				}
			}
			
			this.CopyText(TextToCopy);
		}
	}
	
	private void CopyAllMSILItem_Click(object sender, EventArgs e)
	{
		if (this.CurrentViewLevel == ViewLevel.MSILLevel)
		{
			string Result = String.Empty;
			foreach (string item in TypeBox.Items)
			{
				string[] parts = item.Split(':');
				
				for (int i = 1; i < parts.Length; i++) Result += parts[i].TrimStart(new char[] { ' ' });
				
				Result += Environment.NewLine;
			}
			
			this.CopyText(Result);
		}
	}
	
	private void CopyContextMenuItem_Click(object sender, EventArgs e)
	{
		if (TypeBox.SelectedIndex != -1)
		{
			try
			{
				string TextToCopy = String.Empty;
				switch (CurrentViewLevel)
				{
					case ViewLevel.AssemblyLevel:
					{
						TextToCopy = TypeBox.Items[TypeBox.SelectedIndex].ToString();
						break;
					}
					case ViewLevel.NamespaceLevel:
					{
						TextToCopy = TypeBox.Items[TypeBox.SelectedIndex].ToString();
						
						foreach (string Modifier in Form1.ClassModifiers)
						{
							TextToCopy = TextToCopy.Replace(Modifier, String.Empty);
						}
						TextToCopy = TextToCopy.Split(':')[0].Trim();
						break;
					}
					case ViewLevel.ConstructorLevel:
					{
						TextToCopy = TypeBox.Items[TypeBox.SelectedIndex].ToString().Split('(')[0];
						
						break;
					}
					case ViewLevel.MethodLevel:
					{
						MethodInfo[] methods = SelectedType.GetMethods(AllFlags);
						foreach (MethodInfo method in methods)
						{
							if (MethodStr(method) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
							{
								TextToCopy = method.Name;
								break;
							}
						}
						// TODO: Fix. It isn't copying the correct name becuase the list is sorted alphbeticaly. 
						break;
					}
					case ViewLevel.FieldLevel:
					{
						FieldInfo[] fields = SelectedType.GetFields(AllFlags);
						foreach (FieldInfo field in fields)
						{
							if (FieldStr(field) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
							{
								TextToCopy = field.Name;
								break;
							}
						}
						break;
					}
					case ViewLevel.PropertyLevel:
					{
						PropertyInfo[] props = SelectedType.GetProperties(AllFlags);
						
						foreach (PropertyInfo prop in props)
						{
							if (PropStr(prop) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
							{
								TextToCopy = prop.Name;
								break;
							}
						}
						
						break;
					}
					case ViewLevel.EventLevel:
					{
						EventInfo[] events = SelectedType.GetEvents(AllFlags);
						
						foreach (EventInfo evt in events)
						{
							if (EventStr(evt) == TypeBox.Items[TypeBox.SelectedIndex].ToString().Replace("public ", "").Replace("private ", ""))
							{
								TextToCopy = evt.Name;
								break;
							}
						}
						break;
					}
				}
				
				this.CopyText(TextToCopy);
				
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Type Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
	
	private void CopyText(string TextToCopy)
	{
		int i = 0;
		while (true)
		{
			try
			{
				// Keep the app from hanging forever
				if (i == 100) break;
				
				Clipboard.SetText(TextToCopy);
				break;
			}
			catch (Exception)
			{
				i++;
				continue;
			}
		}
	}
	
	private void TypeBox_KeyUp(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Back)
		{
			DemoteLevel();
		}
		
		if (e.KeyCode == Keys.Return)
		{
			this.PromteLevel();
		}
	}
	
	private void TypeBox_DoubleClick(object sender, EventArgs e)
	{
		this.PromteLevel();
	}
	
	private void PromteLevel()
	{
		try
		{
			if (TypeBox.SelectedIndex != -1)
			{
				if (this.CurrentViewLevel == ViewLevel.AssemblyLevel)
				{
					LoadNamespace();
				}
				else if (this.CurrentViewLevel == ViewLevel.NamespaceLevel)
				{
					LoadType();
				}
				else if (this.CurrentViewLevel == ViewLevel.ViewSelectionLevel)
				{
					string selectedViewMode = TypeBox.Items[TypeBox.SelectedIndex].ToString();
					switch (selectedViewMode)
					{
						case "Fields":
						{
							TypeBox.Items.Clear();
							LoadFields(this.SelectedType);
							break;
						}
						case "Constructors":
						{
							TypeBox.Items.Clear();
							LoadConstructors(this.SelectedType);
							break;
						}
						case "Methods":
						{
							TypeBox.Items.Clear();
							LoadMethods(this.SelectedType);
							break;
						}
						case "Properties":
						{
							TypeBox.Items.Clear();
							LoadProperties(this.SelectedType);
							break;
						}
						case "Events":
						{
							TypeBox.Items.Clear();
							LoadEvents(this.SelectedType);
							break;
						}
					}
					
					if (SelectedType.Namespace == null || SelectedType.Namespace == String.Empty)
					{
						this.Text = "Type Viewer [<Module>." + this.SelectedType.Name + " " + selectedViewMode + "]";
					}
					else
					{
						this.Text = "Type Viewer [" + this.SelectedType.FullName + " " + selectedViewMode + "]";
					}
				}
				else if (this.CurrentViewLevel == ViewLevel.MethodLevel)
				{
					// For Prototype MSIL Viewer
					LoadMethod();
					
					if (SelectedType.Namespace == null || SelectedType.Namespace == String.Empty)
					{
						this.Text = "Type Viewer [<Module>." + this.SelectedType.FullName + "." + this.SelectedMethod.Name + " MSIL]";
					}
					else
					{
						this.Text = "Type Viewer [" + this.SelectedType.FullName + "." + this.SelectedMethod.Name + " MSIL]";
					}
				}
				else if (this.CurrentViewLevel == ViewLevel.ConstructorLevel)
				{
					// For Prototype MSIL Viewer
					LoadConstructor();
					
					if (SelectedType.Namespace == null || SelectedType.Namespace == String.Empty)
					{
						this.Text = "Type Viewer [<Module>." + this.SelectedType.FullName + "." +  ConstrStr(this.SelectedConstructor) + " MSIL]";
					}
					else
					{
						this.Text = "Type Viewer [" + this.SelectedType.FullName + "." + ConstrStr(this.SelectedConstructor) + " MSIL]";
					}
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}
	
	private void DemoteLevel()
	{
		if (this.CurrentViewLevel == ViewLevel.ReferencedAssembliesLevel)
		{
			LoadNamespaces(this.SelectedAssembly);
			
			// Highlights the previously selected namespace.
			if (this.SelectedNamespace != null)
			{
				TypeBox.SelectedItem = this.SelectedNamespace.Name;
			}
		}
		else if (this.CurrentViewLevel == ViewLevel.NamespaceLevel)
		{
			LoadNamespaces(this.SelectedAssembly);
			
			// Highlights the previously selected namespace.
			if (this.SelectedNamespace != null)
			{
				TypeBox.SelectedItem = this.SelectedNamespace.Name;
			}
		}
		else if (this.CurrentViewLevel == ViewLevel.ViewSelectionLevel)
		{
			LoadTypes(this.SelectedNamespace);
			
			// Highlights the previously selected type.
			if (this.SelectedType != null)
			{
				TypeBox.SelectedItem = TypeStr(this.SelectedType);
			}
			
			this.Text = "Type Viewer [" + this.SelectedNamespace.Name + "]";
		}
		else if (this.CurrentViewLevel == ViewLevel.ConstructorLevel || this.CurrentViewLevel == ViewLevel.MethodLevel || this.CurrentViewLevel == ViewLevel.FieldLevel || this.CurrentViewLevel == ViewLevel.PropertyLevel || this.CurrentViewLevel == ViewLevel.EventLevel)
		{
			if (SelectedType.Namespace == null || SelectedType.Namespace == String.Empty)
			{
				this.Text = "Type Viewer [<Module>." + this.SelectedType.Name + "]";
			}
			else
			{
				this.Text = "Type Viewer [" + this.SelectedType.FullName + "]";
			}

			TypeBox.Items.Clear();
			
			if (this.SelectedType.GetFields(AllFlags).Length > 0) TypeBox.Items.Add("Fields");
			if (this.SelectedType.GetConstructors(AllFlags).Length > 0) TypeBox.Items.Add("Constructors");
			if (this.SelectedType.GetMethods(AllFlags).Length > 0) TypeBox.Items.Add("Methods");
			if (this.SelectedType.GetProperties(AllFlags).Length > 0) TypeBox.Items.Add("Properties");
			if (this.SelectedType.GetEvents(AllFlags).Length > 0) TypeBox.Items.Add("Events");
			if (TypeBox.Items.Count > 0) TypeBox.SelectedIndex = 0;
			CurrentViewLevel = ViewLevel.ViewSelectionLevel;
		}
		else if (this.CurrentViewLevel == ViewLevel.MSILLevel)
		{
			TypeBox.Items.Clear();
			LoadMethods(this.SelectedType);
			
			if (SelectedType.Namespace == null || SelectedType.Namespace == String.Empty)
			{
				this.Text = "Type Viewer [<Module>." + this.SelectedType.Name + " Methods]";
			}
			else
			{
				this.Text = "Type Viewer [" + this.SelectedType.FullName + " Methods]";
			}
			
			if (this.SelectedMethod != null)
			{
				TypeBox.SelectedItem = MethodStr(this.SelectedMethod);
			}
		}
		else if (this.CurrentViewLevel == ViewLevel.MSILConstructorLevel)
		{
			TypeBox.Items.Clear();
			LoadConstructors(this.SelectedType);
			
			if (SelectedType.Namespace == null || SelectedType.Namespace == String.Empty)
			{
				this.Text = "Type Viewer [<Module>." + this.SelectedType.Name + " Methods]";
			}
			else
			{
				this.Text = "Type Viewer [" + this.SelectedType.FullName + " Methods]";
			}
			
			if (this.SelectedConstructor != null)
			{
				TypeBox.SelectedItem = ConstrStr(this.SelectedConstructor);
			}
		}
	}
	
	private void LoadMethod()
	{
		if (TypeBox.SelectedIndex != -1)
		{
			foreach (MethodInfo inf in this.SelectedType.GetMethods(AllFlags))
			{
				if (MethodStr(inf) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
				{
					this.SelectedMethod = inf;
				}
			}
		}
		
		LoadMethodMSIL();
		CurrentViewLevel = ViewLevel.MSILLevel;
	}
	
	private void LoadMethodMSIL()
	{	
		AssemblyDefinition asmdef = AssemblyDefinition.ReadAssembly(this.SelectedAssembly.Location);
		TypeDefinition selectedType = asmdef.MainModule.GetType(this.SelectedType.Namespace, this.SelectedType.Name);
		
		MethodDefinition method = null;
		foreach (MethodDefinition methodDef in selectedType.Methods)
		{
			if (methodDef != null)
			{
				if (MethodStr(methodDef) == MethodStr(this.SelectedMethod))
				{
					method = methodDef;
					break;
				}
			}
		}
		
		if (method != null)
		{
			TypeBox.Items.Clear();
			
			foreach (Instruction instr in method.Body.Instructions)
			{
				string[] parts = instr.ToString().Split(':');
				string Result = parts[0].ToUpper().Trim() + ": ";
				for (int i = 1; i < parts.Length; i++) Result += parts[i];
				
				TypeBox.Items.Add(Result);
			}
		}
	}

	private void LoadConstructor()
	{
		if (TypeBox.SelectedIndex != -1)
		{
			foreach (ConstructorInfo inf in this.SelectedType.GetConstructors(AllFlags))
			{
				if (ConstrStr(inf) == TypeBox.Items[TypeBox.SelectedIndex].ToString())
				{
					this.SelectedConstructor = inf;
				}
			}
		}
		
		LoadConstructorMSIL();
		CurrentViewLevel = ViewLevel.MSILConstructorLevel;
	}
	
	private void LoadConstructorMSIL()
	{	
		AssemblyDefinition asmdef = AssemblyDefinition.ReadAssembly(this.SelectedAssembly.Location);
		TypeDefinition selectedType = asmdef.MainModule.GetType(this.SelectedType.Namespace, this.SelectedType.Name);
		
		MethodDefinition method = null;
		foreach (MethodDefinition methodDef in selectedType.Methods)
		{
			if (methodDef != null)
			{
				if (ConstrStr(methodDef) == ConstrStr(this.SelectedConstructor) && methodDef.IsConstructor == true)
				{
					method = methodDef;
					break;
				}
			}
		}
		
		if (method != null)
		{
			TypeBox.Items.Clear();
			
			foreach (Instruction instr in method.Body.Instructions)
			{
				string[] parts = instr.ToString().Split(':');
				string Result = parts[0].ToUpper().Trim() + ": ";
				for (int i = 1; i < parts.Length; i++) Result += parts[i];
				
				TypeBox.Items.Add(Result);
			}
		}
	}

	private void LoadNamespaces(Assembly asm)
	{
		this.Text = "Type Viewer [" + Path.GetFileName(asm.Location) + "]";

		TypeBox.Items.Clear();
		
		if (this.SelectedAssembly.GetReferencedAssemblies().Length > 0) TypeBox.Items.Add(ReferencedAssembliesString);
		
		foreach (Type t in asm.GetTypes())
		{
			if (t.FullName.StartsWith("<") == false && t.FullName != "AssemblyRef" && t.FullName != "FXAssembly" && t.FullName != "ThisAssembly")
			{
				if (t.Namespace == null || t.Namespace == String.Empty)
				{
					if (this.Namespaces.Contains("<Module>") == false)
					{
						Namespace c = new Namespace("<Module>");
						c.Types.Add(t);
						this.Namespaces.Add("<Module>");
						this.NamespaceTypes.Add(c);
					}
					else
					{
						foreach (Namespace c in NamespaceTypes)
						{
							if (c.Name == "<Module>")
							{
								if (c.Types.Contains(t) == false)
								{
									c.Types.Add(t);
									break;
								}
							}
						}
					}
				}
				else
				{
					if (this.Namespaces.Contains(t.Namespace) == false)
					{
						this.Namespaces.Add(t.Namespace);
						Namespace c = new Namespace(t.Namespace);
						c.Types.Add(t);
						this.NamespaceTypes.Add(c);
					}
					else
					{
						foreach (Namespace c in NamespaceTypes)
						{
							if (c.Name == "<Module>")
							{
								if (t.Namespace == null || t.Namespace == String.Empty)
								{
									c.Types.Add(t);
									break;
								}
							}
							
							if (c.Name == t.Namespace)
							{
								if (c.Types.Contains(t) == false)
								{
									c.Types.Add(t);
									break;
								}
							}
						}
					}
				}
			}
		}
		
		foreach (Namespace c in this.NamespaceTypes)
		{
			TypeBox.Items.Add(c.Name);
		}
		CurrentViewLevel = ViewLevel.AssemblyLevel;
	}
	
	private void LoadNamespace()
	{
		if (TypeBox.Items[TypeBox.SelectedIndex].ToString() == ReferencedAssembliesString)
		{
			LoadReferencedAssemblies();
			return;
		}
		
		foreach (Namespace c in this.NamespaceTypes)
		{
			if (c.Name == TypeBox.Items[TypeBox.SelectedIndex].ToString())
			{
				this.Text = "Type Viewer [" + c.Name + "]";
				
				this.SelectedNamespace = c;
				
				LoadTypes(c);
				return;
			}
		}
	}
	
	private void LoadType()
	{
		ExcludedMethods.Clear();
		
		foreach (Type t in this.SelectedAssembly.GetTypes())
		{
			string NeededTypeString = TypeStr(t);
			
			if (NeededTypeString == TypeBox.Items[TypeBox.SelectedIndex].ToString())
			{
				if (t.Namespace == null || t.Namespace == String.Empty)
				{
					this.Text = "Type Viewer [<Module>." + t.Name + "]";
				}
				else
				{
					this.Text = "Type Viewer [" + t.FullName + "]";
				}
				TypeBox.Items.Clear();
				
				if (t.GetFields(AllFlags).Length > 0) TypeBox.Items.Add("Fields");
				if (t.GetConstructors(AllFlags).Length > 0) TypeBox.Items.Add("Constructors");
				if (t.GetMethods(AllFlags).Length > 0) TypeBox.Items.Add("Methods");
				if (t.GetProperties(AllFlags).Length > 0) TypeBox.Items.Add("Properties");
				if (t.GetEvents(AllFlags).Length > 0) TypeBox.Items.Add("Events");
				if (TypeBox.Items.Count > 0) TypeBox.SelectedIndex = 0;
				CurrentViewLevel = ViewLevel.ViewSelectionLevel;
				this.SelectedType = t;
				
				foreach (PropertyInfo Property in t.GetProperties(AllFlags))
				{
					foreach (MethodInfo accessor in Property.GetAccessors(true))
					{
						ExcludedMethods.Add(accessor.Name);
					}
				}
				
				foreach (EventInfo Event in t.GetEvents(AllFlags))
				{
					if (Event.GetAddMethod(true) != null)
					{
						ExcludedMethods.Add(Event.GetAddMethod(true).Name);
					}
					if (Event.GetRemoveMethod(true) != null)
					{
						ExcludedMethods.Add(Event.GetRemoveMethod(true).Name);
					}
					
					if (Event.GetRaiseMethod(true) != null)
					{
						ExcludedMethods.Add(Event.GetRaiseMethod(true).Name);
					}
				}
				
				break;
			}
		}
	}
	
	private string TypeStr(Type t)
	{
		string Modifiers = String.Empty;
		string res = "";
		if (t.BaseType != typeof(Object) && t.BaseType != null)
		{
			res = t.Name + " : " + t.BaseType.FullName;
		}
		else
		{
			res = t.Name;
		}
		
		if (t.IsPublic == true)
		{
			Modifiers += "public ";
		}
		else
		{
			Modifiers += "private ";
		}
		
		if (t.BaseType == typeof(Enum) && t.IsEnum == true)
		{
			Modifiers += "enum ";
			res = res.Split(':')[0].Trim();
		}
		if (t.IsSealed == true && t.IsAbstract == true && t.IsEnum == false) Modifiers += "static ";
		if (t.IsSealed == true && t.IsAbstract == false && t.IsInterface == false && t.IsEnum == false) Modifiers += "sealed ";
		if (t.IsAbstract == true && t.IsInterface == false && t.IsSealed == false && t.IsEnum == false) Modifiers += "abstract ";
		if (t.IsInterface == true && t.IsEnum == false) Modifiers += "interface ";
		
		if (t.IsInterface == false && t.IsEnum == false) Modifiers += "class ";
		
		return Modifiers + res;
	}
	
	private void LoadReferencedAssemblies()
	{
		if (this.SelectedAssembly != null)
		{
			TypeBox.Items.Clear();
			
			foreach (AssemblyName asm in this.SelectedAssembly.GetReferencedAssemblies())
			{
				TypeBox.Items.Add(asm.FullName);
			}
			this.CurrentViewLevel = ViewLevel.ReferencedAssembliesLevel;
		}
	}
	
	private void LoadTypes(Namespace space)
	{
		TypeBox.Items.Clear();
		foreach (Type t in space.Types)
		{
			if (t.IsPublic == false && settings.ShowInternalTypes == false) continue;
			
			TypeBox.Items.Add(TypeStr(t));
		}
		CurrentViewLevel = ViewLevel.NamespaceLevel;
	}
	
	private string ConstrStr(ConstructorInfo c)
	{
		string constructorRes = c.DeclaringType.Name + "(";
		
		if (c.IsPublic == true)
		{
			if (c.IsStatic == true)
			{
				constructorRes = "public static " + constructorRes;	
			}
			else
			{
				constructorRes = "public " + constructorRes;
			}
		}
		else if (c.IsStatic == true)
		{
			constructorRes = "static " + constructorRes;
		}
		else if (c.IsPrivate == true)
		{
			if (c.IsStatic == true)
			{
				constructorRes = "private static " + constructorRes;	
			}
			else
			{
				constructorRes = "private " + constructorRes;
			}
		}
		else if (c.IsAssembly == true)
		{
			if (c.IsStatic == true)
			{
				constructorRes = "internal static " + constructorRes;	
			}
			else
			{
				constructorRes = "internal " + constructorRes;
			}
		}
		
		if (c.GetParameters().Length > 0)
		{
			ParameterInfo[] inf = c.GetParameters();
			
			for (int i = 0; i < inf.Length; i++)
			{
				constructorRes += inf[i].ParameterType.Name + " ";
				
				if (i == inf.Length - 1)
				{
					constructorRes += inf[i].Name;
				}
				else
				{
					constructorRes += inf[i].Name + ", ";
				}
			}
			constructorRes += ")";
		}
		else
		{
			constructorRes += ")";
		}
		return constructorRes;
	}

	private string ConstrStr(MethodDefinition c)
	{
		string constructorRes = c.DeclaringType.Name + "(";
		
		if (c.IsPublic == true)
		{
			if (c.IsStatic == true)
			{
				constructorRes = "public static " + constructorRes;	
			}
			else
			{
				constructorRes = "public " + constructorRes;
			}
		}
		else if (c.IsStatic == true)
		{
			constructorRes = "static " + constructorRes;
		}
		else if (c.IsPrivate == true)
		{
			if (c.IsStatic == true)
			{
				constructorRes = "private static " + constructorRes;	
			}
			else
			{
				constructorRes = "private " + constructorRes;
			}
		}
		else if (c.IsAssembly == true)
		{
			if (c.IsStatic == true)
			{
				constructorRes = "internal static " + constructorRes;	
			}
			else
			{
				constructorRes = "internal " + constructorRes;
			}
		}
		
		if (c.Parameters.Count > 0)
		{
			ParameterDefinition[] inf = c.Parameters.ToArray();
			
			for (int i = 0; i < inf.Length; i++)
			{
				constructorRes += inf[i].ParameterType.Name + " ";
				
				if (i == inf.Length - 1)
				{
					constructorRes += inf[i].Name;
				}
				else
				{
					constructorRes += inf[i].Name + ", ";
				}
			}
			constructorRes += ")";
		}
		else
		{
			constructorRes += ")";
		}
		return constructorRes;
	}
	
	private void LoadConstructors(Type t)
	{
		foreach (ConstructorInfo c in t.GetConstructors(AllFlags))
		{
			TypeBox.Items.Add(ConstrStr(c));
		}
		CurrentViewLevel = ViewLevel.ConstructorLevel;
	}
	
	private string FieldStr(FieldInfo field)
	{
		string f = field.ToString();
		if (field.IsPublic == true)
		{
			if (field.IsStatic == true)
			{
				f = "public static " + f;	
			}
			else
			{
				f = "public " + f;
			}
		}
		if (field.IsPrivate == true)
		{
			if (field.IsStatic == true)
			{
				f = "private static " + f;	
			}
			else
			{
				f = "private " + f;
			}
		}
		if (field.IsFamily == true)
		{
			if (field.IsStatic == true)
			{
				f = "internal static " + f;	
			}
			else
			{
				f = "internal " + f;
			}
		}
		
		return f;
	}
	
	private void LoadFields(Type t)
	{
		foreach (FieldInfo field in t.GetFields(AllFlags))
		{
			string result = FieldStr(field);

			if (t.IsEnum == true)
			{
				if (result.Contains("value__") == true) continue;
				result += " = " + ((int)field.GetValue(field)).ToString();
			}
			
			TypeBox.Items.Add(result);
		}
		CurrentViewLevel = ViewLevel.FieldLevel;
	}
	
	private string MethodStr(MethodInfo Method)
	{
		string methodRes = String.Empty;
			
		if (Method.IsPublic == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "public static " + methodRes;	
			}
			else
			{
				methodRes = "public " + methodRes;
			}
		}
		if (Method.IsPrivate == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "private static " + methodRes;	
			}
			else
			{
				methodRes = "private " + methodRes;
			}
		}
		
		if (Method.IsAssembly == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "internal static " + methodRes;	
			}
			else
			{
				methodRes = "internal " + methodRes;
			}
		}
		
		if (Method.IsAbstract == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "abstract static " + methodRes;	
			}
			else
			{
				methodRes = "abstract " + methodRes;
			}
		}
		
		if (Method.IsFinal == true)
		{
			methodRes = "final " + methodRes;
		}
		
		if (Method.IsFamilyAndAssembly == true)
		{
			methodRes = "assembly family " + methodRes;
		}
		
		if (Method.ReturnType.Name.ToLower() == "void")
		{
			methodRes = methodRes + "void " + Method.Name + "(";
		}
		else
		{
			methodRes = methodRes + Method.ReturnType.Name + " " + Method.Name + "(";
		}
		
		if (Method.GetParameters().Length > 0)
		{
			ParameterInfo[] inf = Method.GetParameters();
			
			for (int i = 0; i < inf.Length; i++)
			{
				methodRes += inf[i].ParameterType.FullName + " ";
				
				if (i == inf.Length - 1)
				{
					methodRes += inf[i].Name;
				}
				else
				{
					methodRes += inf[i].Name + ", ";
				}
			}
			methodRes += ")";
		}
		else
		{
			methodRes += ")";
		}
		return methodRes;
	}

	private string MethodStr(MethodDefinition Method)
	{
		string methodRes = String.Empty;
			
		if (Method.IsPublic == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "public static " + methodRes;	
			}
			else
			{
				methodRes = "public " + methodRes;
			}
		}
		if (Method.IsPrivate == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "private static " + methodRes;	
			}
			else
			{
				methodRes = "private " + methodRes;
			}
		}
		
		if (Method.IsAssembly == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "internal static " + methodRes;	
			}
			else
			{
				methodRes = "internal " + methodRes;
			}
		}
		
		if (Method.IsAbstract == true)
		{
			if (Method.IsStatic == true)
			{
				methodRes = "abstract static " + methodRes;	
			}
			else
			{
				methodRes = "abstract " + methodRes;
			}
		}
		
		if (Method.IsFinal == true)
		{
			methodRes = "final " + methodRes;
		}
		
		if (Method.IsFamilyAndAssembly == true)
		{
			methodRes = "assembly family " + methodRes;
		}
		
		if (Method.ReturnType.Name.ToLower() == "void")
		{
			methodRes = methodRes + "void " + Method.Name + "(";
		}
		else
		{
			methodRes = methodRes + Method.ReturnType.Name + " " + Method.Name + "(";
		}
		
		if (Method.Parameters.Count > 0)
		{
			ParameterDefinition[] inf = Method.Parameters.ToArray();
			
			for (int i = 0; i < inf.Length; i++)
			{
				methodRes += inf[i].ParameterType.FullName + " ";
				
				if (i == inf.Length - 1)
				{
					methodRes += inf[i].Name;
				}
				else
				{
					methodRes += inf[i].Name + ", ";
				}
			}
			methodRes += ")";
		}
		else
		{
			methodRes += ")";
		}
		return methodRes;
	}
	
	private void LoadMethods(Type t)
	{
		foreach (MethodInfo Method in t.GetMethods(AllFlags))
		{
			bool SkipThisOne = false;
			foreach (string ExcludedMethod in this.ExcludedMethods)
			{
				if (Method.Name == ExcludedMethod)
				{
					SkipThisOne = true;
					break;
				}
			}
			
			if (SkipThisOne == true) continue;

			TypeBox.Items.Add(MethodStr(Method));
		}
		CurrentViewLevel = ViewLevel.MethodLevel;
	}
	
	private string PropStr(PropertyInfo Property)
	{
		string res = Property.PropertyType.FullName + " " + Property.Name;
		
		MethodInfo[] accessors = Property.GetAccessors(true);
		
		if (accessors.Length == 0)
		{
			MessageBox.Show(Property.ToString());
		}
		
		
		if (accessors[0].IsPublic == true)
		{
			if (accessors[0].IsStatic == true)
			{
				res = "public static " + res;	
			}
			else
			{
				res = "public " + res;
			}
		}
		if (accessors[0].IsPrivate == true)
		{
			if (accessors[0].IsStatic == true)
			{
				res = "private static " + res;	
			}
			else
			{
				res = "private " + res;
			}
		}
		if (accessors[0].IsAssembly == true)
		{
			if (accessors[0].IsStatic == true)
			{
				res = "internal static " + res;	
			}
			else
			{
				res = "internal " + res;
			}
		}

		if (Property.CanRead == true || Property.CanWrite == true)
		{
			res += " {";
			if (Property.CanRead == true && Property.CanWrite == false)
			{
				res += " get; }";
			}
			else if (Property.CanRead == false && Property.CanWrite == true)
			{
				res += " set; }";
			}
			else if (Property.CanRead == true && Property.CanWrite == true)
			{
				res += " get; set; }";
			}
		}
		return res;
	}
	
	private void LoadProperties(Type t)
	{
		foreach (PropertyInfo Property in t.GetProperties(AllFlags))
		{
			TypeBox.Items.Add(PropStr(Property));
		}
		CurrentViewLevel = ViewLevel.PropertyLevel;
	}
	
	private string EventStr(EventInfo Event)
	{
		string res = String.Empty;
		
		res = Event.ToString();
		
		return res;
	}
	
	private void LoadEvents(Type t)
	{
		foreach (EventInfo Event in t.GetEvents(AllFlags))
		{
			TypeBox.Items.Add(EventStr(Event));
		}
		CurrentViewLevel = ViewLevel.EventLevel;
	}
}

public class Namespace
{
	private string _Name;
	private List<Type> _Types;
	
	public string Name
	{
		get
		{
			return _Name;
		}
	}
	
	public List<Type> Types
	{
		get
		{
			return _Types;
		}
	}
	
	public Namespace(string name)
	{
		this._Name = name;
		this._Types = new List<Type>();
	}
	
	public void Add(Type t)
	{
		this.Types.Add(t);
	}
	
	public void Remove(Type t)
	{
		this.Types.Remove(t);
	}
	
	public void RemoveAt(int index)
	{
		this.Types.RemoveAt(index);
	}
	
	public Type this[int i]
	{
		get
		{
			return this.Types[i];
		}
	}
}