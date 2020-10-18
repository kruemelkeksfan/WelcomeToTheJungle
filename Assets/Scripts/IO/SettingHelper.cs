using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SettingHelper
{
	private static Dictionary<string, string[]> files = new Dictionary<string, string[]>();

	public static string ReadSettingString(string filename, string settingname)
	{
		if(!files.ContainsKey(filename))
		{
			files[filename] = File.ReadAllLines(filename);
		}

		foreach(string line in files[filename])
		{
			string[] setting = line.Split('=');
			if(setting[0] == settingname)
			{
				return setting[1];
			}
		}

		Debug.LogWarning("Unable to find Setting " + settingname + " in File " + filename + "!");
		return "";
	}

	public static int ReadSettingInt(string filename, string settingname)
	{
		string settingText = ReadSettingString(filename, settingname);
		int value;
		if(int.TryParse(settingText, out value))
		{
			return value;
		}
		else
		{
			Debug.LogWarning("Invalid Value " + settingText + " for Setting " + settingname + " in File " + filename + "!");
			return 0;
		}
	}

	public static bool ReadSettingBool(string filename, string settingname)
	{
		string settingText = ReadSettingString(filename, settingname);
		bool value;
		if(bool.TryParse(settingText, out value))
		{
			return value;
		}
		else
		{
			Debug.LogWarning("Invalid Value " + settingText + " for Setting " + settingname + " in File " + filename + "!");
			return false;
		}
	}
}
