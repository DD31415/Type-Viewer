using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

public class Settings
{
	public static string CurrentProgramName;
	public static string CurrentDirectory;
	static Settings()
	{
		Settings.CurrentDirectory = Environment.CurrentDirectory;
		Settings.CurrentProgramName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
	}
	
	private string _LastFileOpened = String.Empty;
	private string SaveFilePath;
	private List<string> _RecentAssemblyList = new List<string>();
	private int _MaxRecentFiles = 10; 
	private bool _ShowInternalTypes = false;
	
	public string LastFileOpened
	{
		get
		{
			return _LastFileOpened;
		}
		set
		{
			_LastFileOpened = value;
			this.Save();
		}
	}
	
	public List<string> RecentAssemblyList
	{
		get
		{
			return _RecentAssemblyList;
		}
	}
	
	public int MaxRecentFiles
	{
		get
		{
			return _MaxRecentFiles;
		}
		set
		{
			_MaxRecentFiles = value;
		}
	}
	
	public bool ShowInternalTypes
	{
		get
		{
			return _ShowInternalTypes;
		}
		set
		{
			_ShowInternalTypes = value;
		}
	}
	
	public Settings()
	{
		if (this.EnsureSettingsFileExists() == true) this.Load();
	}
	
	public Settings(int maxRecentFiles)
	{
		this.MaxRecentFiles = maxRecentFiles;
		if (this.EnsureSettingsFileExists() == true) this.Load();
	}
	
	private bool EnsureSettingsFileExists()
	{
		SaveFilePath = CurrentDirectory + @"\" + Settings.CurrentProgramName + ".ini";
		if (File.Exists(SaveFilePath) == false)
		{
			File.Create(SaveFilePath).Close();
			return false;
		}
		return true;
	}
	
	public void AddRecentAssembly(string path)
	{
		if (RecentAssemblyList.Contains(path) == false)
		{
			if (RecentAssemblyList.Count == this.MaxRecentFiles)
			{
				RecentAssemblyList.RemoveAt(RecentAssemblyList.Count - 1);
			}
			RecentAssemblyList.Insert(0, path);
		}
		else
		{
			if (RecentAssemblyList.Count == this.MaxRecentFiles)
			{
				RecentAssemblyList.RemoveAt(RecentAssemblyList.Count - 1);
			}

			RecentAssemblyList.Remove(path);
			RecentAssemblyList.Insert(0, path);
		}
		this.Save();
	}
	
	public void ClearRecentAssemblies()
	{
		RecentAssemblyList.Clear();
		this.Save();
	}
	
	public void Save()
	{
		StringBuilder str = new StringBuilder();
		str.AppendLine(this.LastFileOpened);
		
		str.AppendLine("---Recent Files---");
		
		for (int i = 0; i < RecentAssemblyList.Count; i++)
		{
			str.AppendLine(RecentAssemblyList[i]);
		}
		
		str.AppendLine("---Bools---");
		str.Append("ShowInternalTypes:");
		str.AppendLine(this.ShowInternalTypes.ToString());
		
		File.WriteAllText(this.SaveFilePath, str.ToString());
	}
	
	public void Load()
	{
		try
		{
			string[] saveFileLines = File.ReadAllLines(SaveFilePath);
			
			if (saveFileLines.Length > 0)
			{
				if (saveFileLines[0].StartsWith("#") == false)
				{
					this.LastFileOpened = saveFileLines[0];
				}
				else
				{
					this.LastFileOpened = String.Empty;
				}
			}

			if (saveFileLines.Length > 2)
			{
				if (saveFileLines[1] == "---Recent Files---")
				{
					for (int i = 2, c = 0; i < saveFileLines.Length; i++, c++)
					{
						if (c > this.MaxRecentFiles || saveFileLines[i] == "---Bools---") break;
						
						this.RecentAssemblyList.Add(saveFileLines[i]);
					}
				}
			}
			
			int BoolsStart = -1;
			for (int i = 0; i < saveFileLines.Length; i++)
			{
				if (saveFileLines[i] == "---Bools---")
				{
					if (i + 1 < saveFileLines.Length)
					{
						BoolsStart = i + 1;
					}
				}
			}
			
			
			if (BoolsStart != -1)
			{
				
				if (saveFileLines[BoolsStart].StartsWith("ShowInternalTypes") == true)
				{
					this.ShowInternalTypes = bool.Parse(saveFileLines[BoolsStart].Split(':')[1]);
				}
				
				// for (int i = BoolsStart; i < saveFileLines.Length; i++)
				// {
						// For future use.
				// }
			}	
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "Type Viewer");
		}
	}
}