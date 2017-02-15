using System;
using System.IO;
using System.Windows.Forms;

public class TypeViewer
{
	[STAThread]
	public static void Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		
		try
		{
			if (args.Length > 0)
			{
				if (File.Exists(args[0]))
				{
					if (Path.GetExtension(args[0]).ToLower() == ".dll" || Path.GetExtension(args[0]).ToLower() == ".exe")
					{
						Application.Run(new Form1(args[0]));
					}
				}
			}
			else
			{
				Application.Run(new Form1());
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}
}