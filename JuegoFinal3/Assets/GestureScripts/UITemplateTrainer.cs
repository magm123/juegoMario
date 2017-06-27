using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// draws and handles the skeleton player interface
public class UITemplateTrainer
{
	private SkeletonRecorder m_skRecorder;
	
	private string m_inputStr;
	
	private string m_showStr = "";
	
	private string m_eventStr = "";

	/// control the training process
	public TemplateTrainer m_tpTrainer;

	private bool m_isOverride = false;
	
	
	public UITemplateTrainer (SkeletonRecorder skRecorder, JointFeature postFeature)
	{
		m_skRecorder = skRecorder;
		m_tpTrainer = new TemplateTrainer(skRecorder, postFeature);
	}

	public void Init()
	{
		m_inputStr = ConstDef.defaultFileName;
		m_showStr = string.Format("Training config not loaded. Enter the name of the gesture you want to load (in {0}{1}) ",
			ConstDef.skelRootPath, ConstDef.trainConfigListName);
	}
	
	public void OnGUI()
	{
		GUILayout.BeginVertical ();
		GUILayout.TextArea(string.Format("The training config file is {0}{1}. Check the file for configuration methods. " +
			"The trained gesture template is a {2} file with the same file name in {3}. To use the template, edit {3} {4}.",
			ConstDef.skelRootPath, ConstDef.trainConfigListName, ConstDef.tmplFileSuffix,
			ConstDef.tmplRootPath, ConstDef.gestureTemplateListName));
		GUILayout.BeginHorizontal();
		if (GUILayout.Button ("Load train. config"))
			OnOpenButton();
		if (GUILayout.Button ("Train and save templ."))
			OnTrainButton();
		m_inputStr = GUILayout.TextField(m_inputStr, GUILayout.Width(100));
		m_isOverride = GUILayout.Toggle(m_isOverride, "Overwrite");
		GUILayout.EndHorizontal ();
		GUILayout.Label(m_showStr);
		GUILayout.EndVertical ();
	}

	public void OnOpenButton ()
	{
		string gestName = m_inputStr;
		if (gestName == "")
		{
			m_showStr = "Please enter the name of the gesture";
			return;
		}
		
		try
		{
			int loaded = 0;
			using(StreamReader reader = new StreamReader(ConstDef.skelRootPath + ConstDef.trainConfigListName))
			{
				loaded = LoadTrainConfigFile(reader, gestName);
			}
			m_showStr = loaded + " key frames loaded for " + gestName;
		}
		catch(Exception e)
		{
			Debug.LogException(e);
			m_showStr = gestName + ": error occurred when loading training config";
		}
				
		ConstDef.defaultFileName = m_inputStr;
	}

	public int LoadTrainConfigFile (StreamReader reader, string gestName)
	{
		string keyFrameFilename;
		int lastTotalLoaded = 0, totalLoaded = 0;
		m_skRecorder.Reset();

		while (reader.Peek() > 0 && reader.ReadLine() != "@" + gestName) ;

		if (reader.Peek() <= 0)
		{
			throw new Exception("Gesture " + gestName + " not found in the train config file!");
		}

		LoadJointIdx(reader.ReadLine());
		LoadAlgorithmName(reader.ReadLine());
		while(reader.Peek() > 0)
		{
			keyFrameFilename = reader.ReadLine();
			if(keyFrameFilename == null || keyFrameFilename == "") break;
			if(! keyFrameFilename.EndsWith(ConstDef.KFFileSuffix))
				keyFrameFilename += ConstDef.KFFileSuffix;
			try
			{
				using(StreamReader reader1 = new StreamReader(ConstDef.skelRootPath + keyFrameFilename))
				{
					m_skRecorder.LoadFileAppend(reader1);
					totalLoaded = m_skRecorder.TaggedFrameCount;
				}
				m_eventStr += (totalLoaded-lastTotalLoaded) + " key frames loaded from " + keyFrameFilename + "\n";
				lastTotalLoaded = totalLoaded;
			}
			catch(Exception e)
			{
				Debug.LogException(e);
				m_eventStr += keyFrameFilename + ": error occurred when loading file\n";
			}
		}
		return totalLoaded;
	}

	/// parse the algorithm indicator in the config file
	private void LoadAlgorithmName(string str)
	{
		if (str == "" || str.ToLower() == "absolute")
			m_tpTrainer.m_isUseRelativeFt = false;
		else if (str.ToLower() == "relative")
			m_tpTrainer.m_isUseRelativeFt = true;
		else if (str.ToLower() == "auto")
			m_tpTrainer.m_isUseRelativeFt = null;
		else
			throw new Exception("The algorithm " + str + " is unknown!");
	}

	/// parse the joint indices in the config file
	private void LoadJointIdx(string str)
	{
		if(str == "") return;
		string[] d = str.Split(',',' ');
		List<Joint> jl = new List<Joint>();

		for (int i = 0; i < d.Length; i++)
		{
			try
			{
				jl.Add((Joint)Enum.Parse(typeof(Joint), d[i]));
			}
			catch
			{
				throw new Exception("The joint name " + d[i] + " is unknown!");
			}
		}
		m_tpTrainer.m_jointIdx = jl.ToArray();
	}
	
	public void OnTrainButton ()
	{
		if(m_skRecorder.TaggedFrameCount == 0)
		{
			m_showStr = "Please load skeleton data with key frames before training.";
			return;
		}
		
		GestureDetector gestDet = m_tpTrainer.Train(m_inputStr);
		
		if(!Directory.Exists(ConstDef.tmplRootPath))
			Directory.CreateDirectory(ConstDef.tmplRootPath);
		string filename = m_inputStr;
		if(! filename.EndsWith(ConstDef.tmplFileSuffix))
			filename += ConstDef.tmplFileSuffix;
		if(!m_isOverride && File.Exists(ConstDef.tmplRootPath + filename))
			filename = "gesture template " + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss") + ConstDef.tmplFileSuffix;
		string strOverride = (m_isOverride && File.Exists(ConstDef.tmplRootPath + filename)) ? "overwritten" : "saved";
		
		try
		{
			using(StreamWriter writer = new StreamWriter(ConstDef.tmplRootPath + filename, false))
			{
				gestDet.SaveToFile(writer);
				m_showStr = "The gesture template has been trained and " + strOverride + " to " + filename;
			}
		}
		catch(Exception e)
		{
			Debug.LogException(e);
			m_showStr = filename + ": error occurred when saving template";
		}
				
		ConstDef.defaultFileName = m_inputStr;
	}
	
	public void GetObserverString(out string strData, out string strEvent)
	{
		strData = "";
		strEvent = m_eventStr;
	}
	
}
