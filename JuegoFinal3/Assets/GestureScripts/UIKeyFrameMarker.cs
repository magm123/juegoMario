using System;
using System.IO;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// draws and handles the key frame marker interface
public class UIKeyFrameMarker
{
	private SkeletonRecorder m_skRecorder;

	/// the same data array in SkeletonAvatarController and SensorSkeletonReader
	private JointData[] m_avJointData;
	
	private string m_inputStr;

	private string m_KFCountStr;

	private string m_showStr;
	
	private int m_wheelSpeed = 10; // the minimum wheel turn is +-0.1 for my mouse
	
	public int m_curFrame = 0;

	private int m_lastTag = 0;
	
	private string m_strFrame = "";
	private string m_strFrameHeader = "Key frame list";

	private int KFCount
	{
		get { 
			try { return Convert.ToInt32(m_KFCountStr); }
			catch
			{
				m_showStr = "Key posture number are positive integers";
				return 0;
			}
		}
		set { m_KFCountStr = ""+value; }
	}

	public UIKeyFrameMarker (SkeletonRecorder skRecorder)
	{
		m_skRecorder = skRecorder;
	}
	
	public void Init()
	{
		m_inputStr = ConstDef.defaultFileName;
		m_KFCountStr = "0";
		m_lastTag = 0;
		m_avJointData = m_skRecorder.m_jointData;
		m_showStr = m_skRecorder.FrameCount == 0 ? "Skeleton file not opened" : "Skeleton data already exist";
		m_showStr += string.Format(". Enter the file name you want to open ({0}*{1}) or save ({0}*{2}) in the text field",
			ConstDef.skelRootPath, ConstDef.skelFileSuffix, ConstDef.KFFileSuffix);
		m_curFrame = 0;
		m_strFrame = m_strFrameHeader;
		CopyCurFrameJointDataRef(m_curFrame);
	}
	
	public void OnGUI()
	{
		GUILayout.BeginVertical ();
		GUILayout.TextArea("Use mouse wheel to go to the next/previous frame, number N to mark the N'th key frame " + 
			"(or use space key after inputing the key posture number), " +
			"left/right key to go to the next/previous key frame, delete key to delete the current key frame.");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button ("Open skeleton file"))
			OnOpenButton();
		if (GUILayout.Button ("Save to file"))
			OnSaveButton();
		m_inputStr = GUILayout.TextField(m_inputStr, GUILayout.Width(100));
		GUILayout.Label("  Key post. num.: ");
		m_KFCountStr = GUILayout.TextField(m_KFCountStr, GUILayout.Width(30));
		GUILayout.EndHorizontal();
		GUILayout.Label(m_showStr);
		GUILayout.EndVertical ();
	}

	public void OnOpenButton ()
	{
		string filename = m_inputStr;
		if(filename == "")
		{
			m_showStr = "Please enter the file name";
			return;
		}
		
		if(! filename.EndsWith(ConstDef.skelFileSuffix))
			filename += ConstDef.skelFileSuffix;
		try
		{
			using(StreamReader reader = new StreamReader(ConstDef.skelRootPath + filename))
			{
				m_skRecorder.LoadFileNew(reader);
			}
			m_showStr = filename + ": imported";
			m_curFrame = 0;
			CopyCurFrameJointDataRef(m_curFrame);
		}
		catch(Exception e)
		{
			Debug.LogException(e);
			m_showStr = filename + ": import error";
		}
		m_strFrame = "";
		ConstDef.defaultFileName = m_inputStr;
	}
	
	public void OnSaveButton ()
	{
		if(!Directory.Exists(ConstDef.skelRootPath))
			Directory.CreateDirectory(ConstDef.skelRootPath);
		string filename = m_inputStr;
		if(! filename.EndsWith(ConstDef.KFFileSuffix))
			filename += ConstDef.KFFileSuffix;
		if(m_inputStr == null || m_inputStr == ""
			/* || File.Exists(ConstDef.skelRootPath + filename)*/) // commented because we allow overwriting
			filename = "key frame data " + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss") + ConstDef.KFFileSuffix;
		
		try
		{
			int savedFrameCount;
			using(StreamWriter writer = new StreamWriter(ConstDef.skelRootPath + filename))
			{
				savedFrameCount = m_skRecorder.SaveFile(writer);
			}
			m_showStr = savedFrameCount + " frames (including " + m_skRecorder.TaggedFrameCount + 
				" key frames) has been saved to " + filename;
		}
		catch(Exception e)
		{
			Debug.LogException(e);
			m_showStr = filename + ": error occurred when saving";
		}
				
		ConstDef.defaultFileName = m_inputStr;
	}
	
	public void Update()
	{
		if(m_skRecorder.FrameCount == 0) return;
		
		float delta = Input.GetAxisRaw("Mouse ScrollWheel") * m_wheelSpeed;
		if(delta != 0)
		{
			m_curFrame = Mathf.Clamp(m_curFrame + (int)delta, 0, m_skRecorder.FrameCount-1);
			CopyCurFrameJointDataRef(m_curFrame);
		}
		else if(Input.GetKeyUp(KeyCode.RightArrow))
		{
			if(m_curFrame < m_skRecorder.FrameCount-1) m_curFrame++;
			while(m_skRecorder.m_tagList[m_curFrame] == 0 && m_curFrame < m_skRecorder.FrameCount-1)
				m_curFrame++;
			CopyCurFrameJointDataRef(m_curFrame);
		}
		else if(Input.GetKeyUp(KeyCode.LeftArrow))
		{
			if(m_curFrame > 0) m_curFrame--;
			while(m_skRecorder.m_tagList[m_curFrame] == 0 && m_curFrame > 0)
				m_curFrame--;
			CopyCurFrameJointDataRef(m_curFrame);
		}
		else if (Input.GetKeyUp(KeyCode.Space))
		{
			if (KFCount < 1)
			{
				m_showStr = "Key posture number is a positive integer";
				return;
			}
			m_skRecorder.m_tagList[m_curFrame] = m_lastTag + 1;
			m_lastTag = (m_lastTag + 1) % KFCount;
			RefreshFrameStr();
		}
		else if (Input.GetKeyDown(KeyCode.Delete))
		{
			m_skRecorder.m_tagList[m_curFrame] = 0;
			int i = m_skRecorder.FrameCount-1;
			while (i > 0 && m_skRecorder.m_tagList[i] == 0)
				i--;
			m_lastTag = m_skRecorder.m_tagList[i] % Mathf.Max(1, KFCount);
			RefreshFrameStr();
		}
		else if(Input.anyKey)
		{
			string key = Input.inputString;
			if(key != null && key != "" && key[0] <= '9' && key[0] > '0')
			{
				m_skRecorder.m_tagList[m_curFrame] = Convert.ToInt32(key);
				m_lastTag = Convert.ToInt32(key);
				KFCount = Mathf.Max(m_lastTag, KFCount);
				m_lastTag %= KFCount;
				RefreshFrameStr();
			}
		}
		
	}
	
	private void RefreshFrameStr()
	{
		m_strFrame = m_strFrameHeader + '\n';
		for(int i = 0; i < m_skRecorder.FrameCount; i++)
		{
			if(m_skRecorder.m_tagList[i] != 0)
				m_strFrame += string.Format("Frame {0}, tag = {1}\n", i+1, m_skRecorder.m_tagList[i]);
		}
	}
	
	// only the reference is copied from the recorded joint data to the joint array to control the avatar!
	private void CopyCurFrameJointDataRef(int frameIdx)
	{
		if(frameIdx > m_skRecorder.FrameCount-1)
			return;
		JointData[] jda = m_skRecorder.m_dataList[frameIdx];
		for(int i = 0; i < m_skRecorder.JointCount; i++)
		{
			Joint j = m_skRecorder.m_jointIdx[i];
			m_avJointData[(int)j] = jda[i];
		}
	}
	
	/// generate strings for UIInfoObserver
	public void GetObserverString(out string strData, out string strFrame)
	{
		strData = "Joint info\n";
		if(m_skRecorder.FrameCount > 0)
		{
			JointData[] jda = m_skRecorder.m_dataList[m_curFrame];
			for(int i = 0; i < m_skRecorder.JointCount; i++)
			{
				Joint j = m_skRecorder.m_jointIdx[i];
				Vector3 vec = jda[i].m_pos;
				strData += string.Format("{0}: {1}\n", j.ToString(), vec.ToString());
			}
			
			strData += string.Format("\nFrame {0}/{1}, time {2} s\n", m_curFrame+1, m_skRecorder.FrameCount, m_skRecorder.m_timeList[m_curFrame]);
			if(m_skRecorder.m_tagList[m_curFrame] != 0)
				strData += string.Format("key frame tag {0}", m_skRecorder.m_tagList[m_curFrame]);
		}
		
		strFrame = m_strFrame;
	}
}
